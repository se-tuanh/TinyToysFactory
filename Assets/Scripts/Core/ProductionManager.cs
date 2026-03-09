using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ProductionManager — Controls Quy trinh A (Assembly) and Quy trinh B (Paint & Pack).
/// Queue-based: batches enter A, then B, then go to shipping buffer.
/// </summary>
public class ProductionManager : MonoBehaviour
{
    public static ProductionManager Instance { get; private set; }

    public enum ProductionMode { Fast, Safe, Quality }

    [Header("Production Mode")]
    public ProductionMode currentMode = ProductionMode.Safe;

    [Header("Multipliers per Mode")]
    [Tooltip("Fast: 1.4x speed, 1.2x resource cost | Safe: 1x | Quality: 0.8x speed, extra rep")]
    public float fastSpeedMultiplier = 1.4f;
    public float fastCostMultiplier  = 1.2f;
    public float qualitySpeedMultiplier  = 0.8f;
    public int   qualityReputationBonus  = 2;

    [Header("Storage Buffer (A → B)")]
    public int maxBufferSize = 5; // batches waiting between A and B

    // ── Management State ─────────────────────────────────────────────────
    private List<Machine> _registeredMachines = new List<Machine>();
    private Queue<BatchJob> _wipBuffer = new Queue<BatchJob>(); // A → B buffer

    // ── Events ───────────────────────────────────────────────────────────
    public UnityEvent<int>      OnBufferChanged;      // buffer count changed
    public UnityEvent           OnProductionABlocked;
    public UnityEvent           OnProductionBBlocked;

    // ── Lifecycle ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ───────────────────────────────────────────────────────

    // ── Machine Registry ─────────────────────────────────────────────────
    public void RegisterMachine(Machine machine)
    {
        if (!_registeredMachines.Contains(machine))
        {
            _registeredMachines.Add(machine);
        }
    }

    public void UnregisterMachine(Machine machine)
    {
        if (_registeredMachines.Contains(machine))
        {
            _registeredMachines.Remove(machine);
        }
    }

    public List<Machine> GetMachineStatus()
    {
        return _registeredMachines;
    }

    // ── WIP Buffer Management (A → B) ────────────────────────────────────
    public void ReceiveWIP(BatchJob job)
    {
        if (_wipBuffer.Count >= maxBufferSize)
        {
            Debug.LogWarning("[ProductionManager] Buffer full! WIP item from Assembly lost.");
            return;
        }

        _wipBuffer.Enqueue(job);
        OnBufferChanged?.Invoke(_wipBuffer.Count);
        Debug.Log($"[ProductionManager] Received WIP {job.product.productName}. Buffer: {_wipBuffer.Count}/{maxBufferSize}");
    }

    public BatchJob DequeueWIP(ProductData requiredProduct)
    {
        // Find the first job that matches the required product type
        // Note: For MVP we might just take the first, but to be safe we should find a match.
        // Queue isn't great for finding specific items, but for MVP let's assume factories 
        // usually process what's in order, or we use a List as a buffer if needed.
        // Let's implement a simple seek for now.
        
        List<BatchJob> tempList = new List<BatchJob>(_wipBuffer);
        for (int i = 0; i < tempList.Count; i++)
        {
            if (tempList[i].product == requiredProduct)
            {
                var job = tempList[i];
                tempList.RemoveAt(i);
                
                // Rebuild queue
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
        // Put it back at the front essentially, or just enqueue if we don't care about strict order
        _wipBuffer.Enqueue(job);
        OnBufferChanged?.Invoke(_wipBuffer.Count);
    }


    // ── Public API ───────────────────────────────────────────────────────

    public void SetProcessABlocked(bool blocked)
    {
        if (blocked) OnProductionABlocked?.Invoke();
    }

    public void SetProcessBBlocked(bool blocked)
    {
        if (blocked) OnProductionBBlocked?.Invoke();
    }

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

        // Tier high -> slow down production slightly to increase pressure
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

    private float GetCostMultiplier()
    {
        return currentMode == ProductionMode.Fast ? fastCostMultiplier : 1f;
    }
}

[System.Serializable]
public class BatchJob
{
    public ProductData product;
    public int quantity;
    public float duration;

    public BatchJob(ProductData product, int quantity, float duration)
    {
        this.product  = product;
        this.quantity = quantity;
        this.duration = duration;
    }
}
