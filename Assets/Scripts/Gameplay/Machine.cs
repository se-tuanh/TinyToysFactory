using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Machine — Represents a single Assembly or Paint machine in the factory.
/// Can be clicked to start a production batch. Shows visual state (idle/running/blocked).
/// </summary>
public class Machine : MonoBehaviour
{
    public enum MachineType { AssemblyA, PaintPackB }
    public enum MachineState { Idle, Working, WaitingInput, Warning }

    [Header("Config")]
    public MachineType machineType;
    public ProductData assignedProduct; // set in Inspector or at runtime
    public int batchQuantity = 1;

    [Header("Visuals")]
    public SpriteRenderer statusLight; // green = idle, yellow = running, red = blocked
    public Animator machineAnimator;   // "Idle", "Running", "Broken" animation states
    public ParticleSystem dustParticles;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip runningSound;
    public AudioClip completeSound;
    public AudioClip blockedSound;

    // ── State ─────────────────────────────────────────────────────────────
    public MachineState CurrentState { get; private set; }
    public bool IsBlocked { get; private set; }

    // WIP item for PaintPackB, simulated queue if needed
    public int WipQuantity { get; set; } = 0;

    private Coroutine _currentCycleRoutine;

    public UnityEvent OnMachineStarted;
    public UnityEvent OnMachineCompleted;
    public UnityEvent OnMachineBlocked;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Start()
    {
        SetState(MachineState.Idle);

        // Subscribe to block events
        var pm = ProductionManager.Instance;
        if (pm != null)
        {
            pm.RegisterMachine(this);
            pm.OnProductionABlocked.AddListener(() => { if (machineType == MachineType.AssemblyA)  SetBlocked(true); });
            pm.OnProductionBBlocked.AddListener(() => { if (machineType == MachineType.PaintPackB) SetBlocked(true); });
        }
    }

    private void OnDestroy()
    {
        var pm = ProductionManager.Instance;
        if (pm != null)
        {
            pm.UnregisterMachine(this);
        }
        StopCurrentCycle();
    }

    // ── Interaction (click to start) ──────────────────────────────────────
    private void OnMouseDown()
    {
        TryStartBatch();
    }

    public bool TryStartBatch()
    {
        if (CurrentState == MachineState.Working || IsBlocked || assignedProduct == null) return false;

        if (machineType == MachineType.AssemblyA)
        {
            return StartAssemblyCycle();
        }
        else if (machineType == MachineType.PaintPackB)
        {
            return StartPaintCycle();
        }

        return false;
    }

    // ── Production Cycles ─────────────────────────────────────────────────
    private bool StartAssemblyCycle()
    {
        var rm = ResourceManager.Instance;
        var pm = ProductionManager.Instance;

        int woodCost  = Mathf.RoundToInt(assignedProduct.woodPlasticCost * batchQuantity * pm.GetCostMultiplier());
        int powerCost = assignedProduct.powerPerAssembly * batchQuantity;

        if (rm.WoodPlastic < woodCost || rm.Power < powerCost || rm.AvailableWorkers <= 0)
        {
            Debug.LogWarning($"[Machine: {gameObject.name}] Not enough resources (Wood/Power/Worker) for Assembly.");
            SetState(MachineState.Warning);
            return false;
        }

        // Consume Resources
        rm.ConsumeWoodPlastic(woodCost);
        rm.ConsumePower(powerCost);
        rm.AssignWorker();

        _currentCycleRoutine = StartCoroutine(AssemblyCycleRoutine());
        return true;
    }

    private IEnumerator AssemblyCycleRoutine()
    {
        SetState(MachineState.Working);
        float duration = assignedProduct.assemblyTime * batchQuantity * ProductionManager.Instance.GetProductionTimeMultiplier() / ProductionManager.Instance.GetSpeedMultiplier();
        
        yield return new WaitForSeconds(duration);
        
        // Output WIP item (For MVP, we just send to ProductionManager's Buffer)
        ProductionManager.Instance.ReceiveWIP(new BatchJob(assignedProduct, batchQuantity, duration));
        ResourceManager.Instance.ReleaseWorker();
        _currentCycleRoutine = null;
        OnBatchDone();
    }

    private bool StartPaintCycle()
    {
        var rm = ResourceManager.Instance;
        var pm = ProductionManager.Instance;

        // B needs WIP item from Buffer (Or we can use WipQuantity if managing locally. Let's use PM's Buffer for now)
        BatchJob job = pm.DequeueWIP(assignedProduct);
        if (job == null)
        {
            SetState(MachineState.WaitingInput);
            Debug.LogWarning($"[Machine: {gameObject.name}] No WIP items in buffer for Paint.");
            return false;
        }

        int paintCost = Mathf.RoundToInt(job.product.paintFabricCost * job.quantity * pm.GetCostMultiplier());
        int powerCost = job.product.powerPerPaint * job.quantity;

        if (rm.PaintFabric < paintCost || rm.Power < powerCost || rm.AvailableWorkers <= 0)
        {
            Debug.LogWarning($"[Machine: {gameObject.name}] Not enough resources (Paint/Power/Worker) for Paint.");
            SetState(MachineState.Warning);
            pm.ReturnWIP(job); // return it to the queue since we couldn't process it
            return false;
        }

        if (rm.IsInventoryFull())
        {
            Debug.LogWarning($"[Machine: {gameObject.name}] Inventory is full! Blocking Paint production.");
            SetState(MachineState.Warning);
            pm.ReturnWIP(job);
            return false;
        }

        // Consume Resources
        rm.ConsumePaintFabric(paintCost);
        rm.ConsumePower(powerCost);
        rm.AssignWorker();

        job.duration = job.product.paintPackTime * job.quantity * pm.GetProductionTimeMultiplier() / pm.GetSpeedMultiplier();
        _currentCycleRoutine = StartCoroutine(PaintCycleRoutine(job));
        return true;
    }

    private IEnumerator PaintCycleRoutine(BatchJob job)
    {
        SetState(MachineState.Working);
        
        yield return new WaitForSeconds(job.duration);
        
        ResourceManager.Instance.ReleaseWorker();
        ResourceManager.Instance.AddProduct(job.product, job.quantity);

        if (ProductionManager.Instance.currentMode == ProductionManager.ProductionMode.Quality)
        {
            GameManager.Instance.AddReputation(ProductionManager.Instance.qualityReputationBonus * job.quantity);
        }

        _currentCycleRoutine = null;
        OnBatchDone();
    }

    // ── Private ───────────────────────────────────────────────────────────
    private void OnBatchDone()
    {
        SetState(MachineState.Idle);
        StopSound();
        PlaySound(completeSound, loop: false);
        OnMachineCompleted?.Invoke();
    }

    private void SetBlocked(bool blocked)
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
        if (_currentCycleRoutine != null)
        {
            StopCoroutine(_currentCycleRoutine);
            _currentCycleRoutine = null;
            // Note: If interrupted, worker/resources are lost as penalty (design choice).
            // We could refund them here if desired.
        }
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
                lightColor = Color.green; // Blinking logic or pulse can be handled by Animator
                if (dustParticles) dustParticles.Play();
                PlaySound(runningSound, loop: true);
                OnMachineStarted?.Invoke();
                break;
            case MachineState.Warning:
                animState = "Idle"; // or specific Warning anim
                lightColor = Color.red;
                if (dustParticles) dustParticles.Stop();
                StopSound();
                break;
            case MachineState.WaitingInput:
                animState = "Idle";
                lightColor = Color.yellow;
                if (dustParticles) dustParticles.Stop();
                StopSound();
                break;
            case MachineState.Idle:
                animState = "Idle";
                lightColor = Color.gray; // Using Gray for Idle as requested
                if (dustParticles) dustParticles.Stop();
                StopSound();
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
}
