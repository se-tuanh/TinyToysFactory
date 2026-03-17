using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ProductionManager — Controls production flow between Process A (Assembly) and B (Paint & Pack).
/// Fires batch-complete events consumed by OrderManager and UIManager.
/// </summary>
public class ProductionManager : MonoBehaviour
{
    public static ProductionManager Instance { get; private set; }

    public enum ProductionMode { Fast, Safe, Quality }

    [Header("Production Mode")]
    public ProductionMode currentMode = ProductionMode.Safe;

    [Header("Multipliers per Mode")]
    [Tooltip("Fast: 1.4x speed, 1.2x resource cost | Safe: 1x | Quality: 0.8x speed, extra rep")]
    public float fastSpeedMultiplier    = 1.4f;
    public float fastCostMultiplier     = 1.2f;
    public float qualitySpeedMultiplier = 0.8f;
    public int   qualityReputationBonus = 2;

    [Header("Storage Buffer (A → B)")]
    public int maxBufferSize = 5; // batches waiting between A and B

    // ── Management State ─────────────────────────────────────────────────
    private List<Machine>   _registeredMachines = new List<Machine>();
    private Queue<BatchJob> _wipBuffer          = new Queue<BatchJob>(); // A → B buffer

    // ── Process State ────────────────────────────────────────────────────
    public bool IsProcessABlocked { get; private set; }
    public bool IsProcessBBlocked { get; private set; }
    public bool IsProcessARunning => GetMachineRunning(Machine.MachineType.AssemblyA);
    public bool IsProcessBRunning => GetMachineRunning(Machine.MachineType.PaintPackB);

    // ── Events ───────────────────────────────────────────────────────────
    public UnityEvent<int>      OnBufferChanged;       // buffer count changed
    public UnityEvent<BatchJob> OnBatchCompletedA;     // Assembly batch done → sends to buffer
    public UnityEvent<BatchJob> OnBatchCompletedB;     // Paint&Pack batch done → product in inventory
    public UnityEvent           OnProductionABlocked;
    public UnityEvent           OnProductionBBlocked;

    // ── Lifecycle ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Machine Registry ─────────────────────────────────────────────────
    public void RegisterMachine(Machine machine)
    {
        if (!_registeredMachines.Contains(machine))
            _registeredMachines.Add(machine);
    }

    public void UnregisterMachine(Machine machine)
    {
        _registeredMachines.Remove(machine);
    }

    public List<Machine> GetMachineStatus() => _registeredMachines;

    // ── WIP Buffer Management (A → B) ────────────────────────────────────
    public bool CanReceiveWIP() => _wipBuffer.Count < maxBufferSize;

    public void ReceiveWIP(BatchJob job)
    {
        if (_wipBuffer.Count >= maxBufferSize)
        {
            Debug.LogWarning("[ProductionManager] Buffer full! WIP item from Assembly lost.");
            return;
        }
        _wipBuffer.Enqueue(job);
        OnBufferChanged?.Invoke(_wipBuffer.Count);
        OnBatchCompletedA?.Invoke(job);
        Debug.Log($"[ProductionManager] WIP received: {job.product.productName}. Buffer: {_wipBuffer.Count}/{maxBufferSize}");
    }

    public BatchJob DequeueWIP(ProductData requiredProduct)
    {
        // Seek the first matching product type in the buffer
        var tempList = new List<BatchJob>(_wipBuffer);
        for (int i = 0; i < tempList.Count; i++)
        {
            if (tempList[i].product == requiredProduct)
            {
                var job = tempList[i];
                tempList.RemoveAt(i);
                _wipBuffer.Clear();
                foreach (var j in tempList) _wipBuffer.Enqueue(j);
                OnBufferChanged?.Invoke(_wipBuffer.Count);
                return job;
            }
        }
        return null; // Not found
    }

    public void ReturnWIP(BatchJob job)
    {
        _wipBuffer.Enqueue(job);
        OnBufferChanged?.Invoke(_wipBuffer.Count);
    }

    /// <summary>Called by Machine.PaintPackB when a batch finishes.</summary>
    public void NotifyBatchCompletedB(BatchJob job)
    {
        OnBatchCompletedB?.Invoke(job);
    }

    // ── Blocking ─────────────────────────────────────────────────────────
    public void SetProcessABlocked(bool blocked)
    {
        IsProcessABlocked = blocked;
        if (blocked) OnProductionABlocked?.Invoke();
    }

    public void SetProcessBBlocked(bool blocked)
    {
        IsProcessBBlocked = blocked;
        if (blocked) OnProductionBBlocked?.Invoke();
    }

    // ── Production Mode ───────────────────────────────────────────────────
    public void SetProductionMode(ProductionMode mode)
    {
        currentMode = mode;
        Debug.Log($"[Production] Mode set to {mode}");
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    public float GetSpeedMultiplier()
    {
        return currentMode switch
        {
            ProductionMode.Fast    => fastSpeedMultiplier,
            ProductionMode.Quality => qualitySpeedMultiplier,
            _                      => 1f
        };
    }

    public float GetCostMultiplier()
    {
        return currentMode == ProductionMode.Fast ? fastCostMultiplier : 1f;
    }

    public float GetProductionTimeMultiplier()
    {
        if (PressureDirector.Instance == null) return 1f;
        return PressureDirector.Instance.CurrentTier switch
        {
            1 => 1.0f,
            2 => 1.1f,
            3 => 1.25f,
            4 => 1.4f,
            5 => 1.6f,
            _ => 1.0f
        };
    }

    private bool GetMachineRunning(Machine.MachineType type)
    {
        foreach (var m in _registeredMachines)
            if (m.machineType == type && m.CurrentState == Machine.MachineState.Working)
                return true;
        return false;
    }

    /// <summary>Block or unblock all registered machines (called by PressureDirector during breakdown events).</summary>
    public void BlockAll(bool blocked)
    {
        foreach (var m in _registeredMachines)
            m.SetBlocked(blocked);
    }
}

// ── BatchJob ──────────────────────────────────────────────────────────────────
[System.Serializable]
public class BatchJob
{
    public ProductData product;
    public int         quantity;
    public float       duration;

    public BatchJob(ProductData product, int quantity, float duration)
    {
        this.product  = product;
        this.quantity = quantity;
        this.duration = duration;
    }
}
