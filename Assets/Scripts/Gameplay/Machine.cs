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
    private Coroutine _autoStartRetryCoroutine;
    private Vector3 _baseScale;

    public bool HasWorker { get; private set; }
    public float Progress => _totalDuration > 0f ? Mathf.Clamp01(_elapsedTime / _totalDuration) : 0f;

    private float _elapsedTime;
    private float _totalDuration;
    private bool _isFreeProduction = false;  // Track if current job is free production
    private int _consecutiveClicks = 0; // number of rapid clicks
    private float _lastClickTime = 0f;
    private const float CLICK_WINDOW = 1f; // time window to count consecutive clicks (seconds)
    private const float BASE_FAIL_CHANCE = 0.10f; // base failure chance on a single SpeedUp
    private const float FAIL_PER_EXTRA_CLICK = 0.07f; // additional failure chance per extra click

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
        if (_autoStartRetryCoroutine != null) StopCoroutine(_autoStartRetryCoroutine);
    }

    // ── Interaction ───────────────────────────────────────────────────────
    public void ToggleWorker()
    {
        if (HasWorker)
        {
            ResourceManager.Instance.ReleaseWorker();
            HasWorker = false;
            if (_autoStartRetryCoroutine != null) { StopCoroutine(_autoStartRetryCoroutine); _autoStartRetryCoroutine = null; }
            if (CurrentState == MachineState.Working) SetState(MachineState.WaitingInput); // pause visuals
            // reset click tracking when worker removed
            _consecutiveClicks = 0;
            _lastClickTime = 0f;
            Debug.Log($"[Machine:{name}] Worker removed.");
        }
        else
        {
            if (ResourceManager.Instance.AssignWorker())
            {
                HasWorker = true;
                if (_currentCycle != null) SetState(MachineState.Working); // resume visuals
                else 
                {
                    // Auto-start production immediately when worker assigned
                    if (!TryStartBatch())
                    {
                        // If first attempt fails (no resources/tasks), start retry loop
                        if (_autoStartRetryCoroutine != null) StopCoroutine(_autoStartRetryCoroutine);
                        _autoStartRetryCoroutine = StartCoroutine(AutoStartRetryLoop());
                    }
                }
                Debug.Log($"[Machine:{name}] Worker assigned.");
            }
        }
    }

    // Nút Tăng Tốc giờ đã biến thành Nút Ép Xung Nhân Phẩm!
    public void SpeedUp()
    {
        if (CurrentState != MachineState.Working || !HasWorker) return;

        var rm = ResourceManager.Instance;

        // Each SpeedUp click consumes a small amount of extra power
        int overclockPowerCost = 8; // slightly lower per-click cost
        if (!rm.ConsumePower(overclockPowerCost))
        {
            Debug.LogWarning($"[Machine:{name}] Sập nguồn! Không đủ điện để ép xung.");
            return;
        }

        // Track consecutive clicks within a short window
        if (Time.time - _lastClickTime <= CLICK_WINDOW) _consecutiveClicks++; else _consecutiveClicks = 1;
        _lastClickTime = Time.time;

        // Failure chance increases with rapid clicks
        float failChance = BASE_FAIL_CHANCE + FAIL_PER_EXTRA_CLICK * (_consecutiveClicks - 1);
        failChance = Mathf.Clamp01(failChance);
        if (Random.value < failChance)
        {
            Debug.LogError($"[Machine:{name}] QUÁ TẢI! CHÁY MÁY RỒI!!! (clicks={_consecutiveClicks}, failChance={failChance:F2})");
            StartCoroutine(OverloadRecoveryRoutine()); 
            return;
        }

        // Boost progress scaled by consecutive clicks (each click adds 15% of total duration)
        float perClickBoost = 0.15f; // 15% per click
        float boostAmount = _totalDuration * perClickBoost * _consecutiveClicks;
        _elapsedTime += boostAmount;
        if (_elapsedTime >= _totalDuration) _elapsedTime = _totalDuration;
    }

    // CÁI HÀM BỊ SẾP XÓA MẤT NẰM Ở ĐÂY NÀY:
    public bool TryStartBatch()
    {
        if (IsBlocked || !HasWorker || CurrentState == MachineState.Working) return false;

        // Bắt buộc phải có Product mới chạy
        if (assignedProduct == null)
        {
            Debug.LogWarning($"[Machine:{name}] LỖI: Chưa gắn ProductData vào ô Assigned Product!");
            return false;
        }

        if (machineType == MachineType.AssemblyA)
        {
            // Try to get a production task first
            var task = ProductionManager.Instance?.DequeueTask(assignedProduct);
            if (task == null)
            {
                // No task available - allow free production for player choice
                Debug.Log($"[Machine:{name}] Không có đơn, nhưng vẫn cho phép sản xuất tự do {assignedProduct.productName}");
            }
            // Continue regardless - either with task or free production
        }

        Debug.Log($"[Machine:{name}] Starting {machineType} for '{assignedProduct.productName}'...");
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

        var pm = ProductionManager.Instance;
        _totalDuration = assignedProduct.assemblyTime
                       * batchQuantity
                       * (pm != null ? pm.GetProductionTimeMultiplier() : 1f)
                       / (pm != null ? pm.GetSpeedMultiplier() : 1f);
        _elapsedTime = 0f;

        while (_elapsedTime < _totalDuration)
        {
            if (HasWorker) _elapsedTime += Time.deltaTime;
            yield return null;
        }

        var job = new BatchJob(assignedProduct, batchQuantity, _totalDuration);
        if (pm != null) pm.ReceiveWIP(job);   // → fires OnBatchCompletedA

        _currentCycle = null;
        OnBatchDone();
    }

    private bool StartPaintCycle()
    {
        var rm = ResourceManager.Instance;
        var pm = ProductionManager.Instance;

        // Try to dequeue WIP from buffer
        BatchJob job = pm.DequeueWIP(assignedProduct);
        _isFreeProduction = false;
        
        if (job == null)
        {
            // Buffer is empty - allow free production if player wants to paint anyway
            Debug.Log($"[Machine:{name}] Rổ trống, sơn tự do {assignedProduct.productName}");
            // Create a dummy batch job for free production
            job = new BatchJob(assignedProduct, batchQuantity, 0f);
            _isFreeProduction = true;
        }

        int paintCost = Mathf.RoundToInt(job.product.paintFabricCost * job.quantity * pm.GetCostMultiplier());
        int powerCost = job.product.powerPerPaint * job.quantity;

        if (rm.PaintFabric < paintCost || rm.Power < powerCost)
        {
            Debug.LogWarning($"[Machine:{name}] Not enough resources for Paint.");
            SetState(MachineState.Warning);
            // Only return WIP if it came from buffer (not free production)
            if (!_isFreeProduction && pm != null && pm.CanReceiveWIP()) pm.ReturnWIP(job);
            return false;
        }

        if (rm.IsInventoryFull())
        {
            Debug.LogWarning($"[Machine:{name}] Inventory full — Paint blocked.");
            SetState(MachineState.Warning);
            // Only return WIP if it came from buffer (not free production)
            if (!_isFreeProduction && pm != null) pm.ReturnWIP(job);
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

        var rm = ResourceManager.Instance;
        if (rm != null) rm.AddProduct(job.product, job.quantity);

        var pm = ProductionManager.Instance;
        if (pm != null)
        {
            if (pm.currentMode == ProductionManager.ProductionMode.Quality)
            {
                var gm = GameManager.Instance;
                if (gm != null) gm.AddReputation(pm.qualityReputationBonus * job.quantity);
            }

            // Only notify ProductionManager if this was a real job from buffer (not free production)
            if (!_isFreeProduction)
            {
                pm.NotifyBatchCompletedB(job);   // → fires OnBatchCompletedB (consumed by OrderManager & UIManager)
            }
        }

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
            // reset click tracking when machine breaks
            _consecutiveClicks = 0;
            _lastClickTime = 0f;
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

    private IEnumerator OverloadRecoveryRoutine()
    {
        SetBlocked(true);
    
        float recoveryDelay = Random.Range(3f, 5f);
        Debug.Log($"[Machine:{name}] ĐANG QUÁ TẢI! Cần {recoveryDelay:F1}s để hạ nhiệt...");

        yield return new WaitForSeconds(recoveryDelay);

        SetBlocked(false);
        Debug.Log($"[Machine:{name}] Máy đã nguội, sẵn sàng hoạt động lại!");

        if (HasWorker)
        {
            TryStartBatch();
        }
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

    // ── Auto-Start Retry ──────────────────────────────────────────────────
    /// <summary>
    /// Continuously tries to start batch production until it succeeds.
    /// This ensures production starts automatically as soon as resources become available.
    /// </summary>
    private IEnumerator AutoStartRetryLoop()
    {
        float maxRetryDuration = 10f; // Give up after 10 seconds
        float retryStartTime = Time.time;

        while (HasWorker && CurrentState != MachineState.Working)
        {
            if (Time.time - retryStartTime > maxRetryDuration)
            {
                Debug.Log($"[Machine:{name}] Auto-start retry timed out after {maxRetryDuration}s. Worker idle.");
                _autoStartRetryCoroutine = null;
                break;
            }

            if (TryStartBatch())
            {
                Debug.Log($"[Machine:{name}] Auto-start succeeded after {Time.time - retryStartTime:F2}s");
                _autoStartRetryCoroutine = null;
                break;
            }

            // Retry every 0.5 seconds
            yield return new WaitForSeconds(0.5f);
        }
    }
}