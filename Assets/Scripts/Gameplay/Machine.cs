using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Machine — Represents a single Assembly or Paint machine in the factory.
/// Click to start a production batch. Delegates state changes to ProductionManager.
/// </summary>
public class Machine : MonoBehaviour
{
    public enum MachineType { AssemblyA, PaintPackB }
    public enum MachineState { Idle, Working, WaitingInput, Warning }

    [Header("Config")]
    public MachineType machineType;
    public ProductData assignedProduct;
    public int batchQuantity = 1;

    [Header("Visuals")]
    public SpriteRenderer statusLight;    // green=idle, yellow=waiting, red=warning, blue=working
    public Animator machineAnimator;
    public ParticleSystem dustParticles;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip runningSound;
    public AudioClip completeSound;
    public AudioClip blockedSound;

    // ── State ─────────────────────────────────────────────────────────────
    public MachineState CurrentState { get; private set; }
    public bool IsBlocked { get; private set; }

    private Coroutine _currentCycle;
    private Coroutine _pulseCycle;
    private Vector3 _baseScale;

    public bool HasWorker { get; private set; }
    public float Progress => _totalDuration > 0f ? Mathf.Clamp01(_elapsedTime / _totalDuration) : 0f;

    private float _elapsedTime;
    private float _totalDuration;

    // ── Events ────────────────────────────────────────────────────────────
    public UnityEvent OnMachineStarted;
    public UnityEvent OnMachineCompleted;
    public UnityEvent OnMachineBlocked;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Start()
    {
        _baseScale = transform.localScale;
        SetState(MachineState.Idle);

        var pm = ProductionManager.Instance;
        if (pm != null)
        {
            pm.RegisterMachine(this);
            pm.OnProductionABlocked.AddListener(() =>
            {
                if (machineType == MachineType.AssemblyA) SetBlocked(true);
            });
            pm.OnProductionBBlocked.AddListener(() =>
            {
                if (machineType == MachineType.PaintPackB) SetBlocked(true);
            });
        }
    }

    private void OnDestroy()
    {
        if (HasWorker) ResourceManager.Instance?.ReleaseWorker();
        ProductionManager.Instance?.UnregisterMachine(this);
        StopCurrentCycle();
    }

    // ── Interaction ───────────────────────────────────────────────────────
    public void ToggleWorker()
    {
        if (HasWorker)
        {
            ResourceManager.Instance.ReleaseWorker();
            HasWorker = false;
            if (CurrentState == MachineState.Working) SetState(MachineState.WaitingInput); // pause visuals
            Debug.Log($"[Machine:{name}] Worker removed.");
        }
        else
        {
            if (ResourceManager.Instance.AssignWorker())
            {
                HasWorker = true;
                if (_currentCycle != null) SetState(MachineState.Working); // resume visuals
                else TryStartBatch(); // Idle -> try to find work immediately
                Debug.Log($"[Machine:{name}] Worker assigned.");
            }
        }
    }

    public void SpeedUp()
    {
        if (CurrentState == MachineState.Working && HasWorker)
        {
            _elapsedTime += 0.5f; // Jump forward 0.5s per click
            // Optional: spawn particle or flash
        }
    }

    public bool TryStartBatch()
    {
        if (IsBlocked || !HasWorker || CurrentState == MachineState.Working) return false;

        // 🔥 ĐỘ MÁY LẮP RÁP (A): Xin đơn hàng mới nhất trên bảng thông báo
        if (machineType == MachineType.AssemblyA)
        {
            var task = ProductionManager.Instance?.DequeueTask(null); // null = lấy đơn bất kỳ
            if (task != null)
            {
                assignedProduct = task.product; // Tự động thay đổi khuôn đúc
            }
            else
            {
                Debug.Log($"[Machine:{name}] Hết việc! Đang chờ đơn mới rớt xuống.");
                return false;
            }
        }

        // Đảm bảo máy đã có khuôn mới được chạy
        if (assignedProduct == null && machineType == MachineType.AssemblyA) return false;

        Debug.Log($"[Machine:{name}] Starting {machineType} for '{assignedProduct?.productName}'...");
        return machineType == MachineType.AssemblyA ? StartAssemblyCycle() : StartPaintCycle();
    }

    // ── Production Cycles ─────────────────────────────────────────────────
    private bool StartAssemblyCycle()
    {
        var rm = ResourceManager.Instance;
        var pm = ProductionManager.Instance;

        int woodCost = Mathf.RoundToInt(assignedProduct.woodPlasticCost * batchQuantity * pm.GetCostMultiplier());
        int powerCost = assignedProduct.powerPerAssembly * batchQuantity;

        if (rm.WoodPlastic < woodCost || rm.Power < powerCost)
        {
            Debug.LogWarning($"[Machine:{name}] Not enough resources for Assembly.");
            SetState(MachineState.Warning);
            return false;
        }

        rm.ConsumeWoodPlastic(woodCost);
        rm.ConsumePower(powerCost);

        _currentCycle = StartCoroutine(AssemblyCycleRoutine());
        return true;
    }

    private IEnumerator AssemblyCycleRoutine()
    {
        SetState(MachineState.Working);

        _totalDuration = assignedProduct.assemblyTime
                       * batchQuantity
                       * ProductionManager.Instance.GetProductionTimeMultiplier()
                       / ProductionManager.Instance.GetSpeedMultiplier();
        _elapsedTime = 0f;

        while (_elapsedTime < _totalDuration)
        {
            if (HasWorker) _elapsedTime += Time.deltaTime;
            yield return null;
        }

        var job = new BatchJob(assignedProduct, batchQuantity, _totalDuration);
        ProductionManager.Instance.ReceiveWIP(job);   // → fires OnBatchCompletedA

        _currentCycle = null;
        OnBatchDone();
    }

    private bool StartPaintCycle()
    {
        var rm = ResourceManager.Instance;
        var pm = ProductionManager.Instance;

        // Nhặt đại 1 cái phôi bất kỳ đang có trong rổ ra sơn
        BatchJob job = pm.DequeueWIP(null);
        if (job == null)
        {
            Debug.LogWarning($"[Machine:{name}] Rổ trống! Chưa có phôi nào để sơn.");
            SetState(MachineState.WaitingInput);
            return false;
        }

        // Tự động chuyển màu sơn theo món hàng vừa nhặt được
        assignedProduct = job.product;

        // 🔥 Đã xóa khai báo trùng lặp ở đây
        int paintCost = Mathf.RoundToInt(job.product.paintFabricCost * job.quantity * pm.GetCostMultiplier());
        int powerCost = job.product.powerPerPaint * job.quantity;

        if (rm.PaintFabric < paintCost || rm.Power < powerCost)
        {
            Debug.LogWarning($"[Machine:{name}] Not enough resources for Paint.");
            SetState(MachineState.Warning);
            pm.ReturnWIP(job);
            return false;
        }

        if (rm.IsInventoryFull())
        {
            Debug.LogWarning($"[Machine:{name}] Inventory full — Paint blocked.");
            SetState(MachineState.Warning);
            pm.ReturnWIP(job);
            return false;
        }

        rm.ConsumePaintFabric(paintCost);
        rm.ConsumePower(powerCost);

        job.duration = job.product.paintPackTime * job.quantity
                     * pm.GetProductionTimeMultiplier()
                     / pm.GetSpeedMultiplier();

        _currentCycle = StartCoroutine(PaintCycleRoutine(job));
        return true;
    }

    private IEnumerator PaintCycleRoutine(BatchJob job)
    {
        SetState(MachineState.Working);
        _totalDuration = job.duration;
        _elapsedTime = 0f;

        while (_elapsedTime < _totalDuration)
        {
            if (HasWorker) _elapsedTime += Time.deltaTime;
            yield return null;
        }

        ResourceManager.Instance.AddProduct(job.product, job.quantity);

        var pm = ProductionManager.Instance;
        if (pm.currentMode == ProductionManager.ProductionMode.Quality)
            GameManager.Instance.AddReputation(pm.qualityReputationBonus * job.quantity);

        pm.NotifyBatchCompletedB(job);   // → fires OnBatchCompletedB (consumed by OrderManager & UIManager)

        _currentCycle = null;
        OnBatchDone();
    }

    // ── Private Helpers ───────────────────────────────────────────────────
    private void OnBatchDone()
    {
        SetState(MachineState.Idle);
        StopSound();
        PlaySound(completeSound, loop: false);
        OnMachineCompleted?.Invoke();

        Debug.Log($"[Machine:{name}] Xong lô hàng! Đi xin việc tiếp...");
        TryStartBatch();
    }

    public void SetBlocked(bool blocked)
    {
        IsBlocked = blocked;
        if (blocked)
        {
            StopCurrentCycle();
            SetState(MachineState.Warning);
            PlaySound(blockedSound, loop: false);
            OnMachineBlocked?.Invoke();
        }
        else
        {
            SetState(MachineState.Idle);
        }
    }

    private void StopCurrentCycle()
    {
        if (_currentCycle == null) return;
        StopCoroutine(_currentCycle);
        _currentCycle = null;
    }

    private void SetState(MachineState newState)
    {
        CurrentState = newState;

        string animState = "Idle";
        Color lightColor = Color.green;

        switch (newState)
        {
            case MachineState.Working:
                animState = "Running";
                lightColor = Color.blue;
                if (dustParticles) dustParticles.Play();
                PlaySound(runningSound, loop: true);
                StartPulse();
                OnMachineStarted?.Invoke();
                break;

            case MachineState.Warning:
                animState = "Idle";
                lightColor = Color.red;
                if (dustParticles) dustParticles.Stop();
                StopSound();
                break;

            case MachineState.WaitingInput:
                animState = "Idle";
                lightColor = Color.yellow;
                if (dustParticles) dustParticles.Stop();
                StopSound();
                StopPulse();
                break;

            case MachineState.Idle:
                animState = "Idle";
                lightColor = Color.green;
                if (dustParticles) dustParticles.Stop();
                StopSound();
                StopPulse();
                break;
        }

        if (machineAnimator) machineAnimator.Play(animState);
        if (statusLight) statusLight.color = lightColor;
    }

    private void PlaySound(AudioClip clip, bool loop)
    {
        if (!audioSource || !clip) return;
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();
    }

    private void StopSound()
    {
        if (audioSource) audioSource.Stop();
    }

    // ── Scale Pulse ───────────────────────────────────────────────────────
    private void StartPulse()
    {
        StopPulse();
        _pulseCycle = StartCoroutine(PulseScale());
    }

    private void StopPulse()
    {
        if (_pulseCycle != null) { StopCoroutine(_pulseCycle); _pulseCycle = null; }
        transform.localScale = _baseScale;
    }

    private IEnumerator PulseScale()
    {
        const float PULSE_TIME = 0.4f;
        const float PEAK = 1.07f;
        while (true)
        {
            // scale up
            float t = 0f;
            while (t < PULSE_TIME / 2f)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(1f, PEAK, t / (PULSE_TIME / 2f));
                transform.localScale = _baseScale * s;
                yield return null;
            }
            // scale down
            t = 0f;
            while (t < PULSE_TIME / 2f)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(PEAK, 1f, t / (PULSE_TIME / 2f));
                transform.localScale = _baseScale * s;
                yield return null;
            }
        }
    }
}