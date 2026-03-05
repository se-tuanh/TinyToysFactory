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

    // ── State ────────────────────────────────────────────────────────────
    public bool IsProcessARunning  { get; private set; }
    public bool IsProcessBRunning  { get; private set; }
    public bool IsProcessABlocked  { get; private set; } // event-blocked
    public bool IsProcessBBlocked  { get; private set; }

    private Queue<BatchJob> _bufferQueue = new Queue<BatchJob>(); // A → B buffer
    private List<BatchJob> _completedBatches = new List<BatchJob>(); // ready to ship

    // ── Events ───────────────────────────────────────────────────────────
    public UnityEvent<BatchJob> OnBatchCompletedA;   // batch ready for B
    public UnityEvent<BatchJob> OnBatchCompletedB;   // batch ready to ship
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

    /// <summary>Start a batch through Quy trinh A.</summary>
    public bool StartBatchA(ProductData product, int quantity)
    {
        if (IsProcessARunning || IsProcessABlocked) return false;

        var rm = ResourceManager.Instance;

        // Calculate costs with mode multiplier
        int woodCost  = Mathf.RoundToInt(product.woodPlasticCost * quantity * GetCostMultiplier());
        int powerCost = product.powerPerAssembly * quantity;

        if (!rm.ConsumeWoodPlastic(woodCost)) { Debug.LogWarning("[Production] Not enough Wood/Plastic!"); return false; }
        if (!rm.ConsumePower(powerCost))      { Debug.LogWarning("[Production] Not enough Power!"); return false; }
        if (!rm.AssignWorker())               { Debug.LogWarning("[Production] No available workers!"); return false; }

        float duration = product.assemblyTime * quantity / GetSpeedMultiplier();
        var job = new BatchJob(product, quantity, duration);

        IsProcessARunning = true;
        StartCoroutine(RunProcessA(job));
        return true;
    }

    /// <summary>Block/unblock Quy trinh A (e.g., broken machine event).</summary>
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

    public List<BatchJob> CollectCompletedBatches()
    {
        var result = new List<BatchJob>(_completedBatches);
        _completedBatches.Clear();
        return result;
    }

    public void SetProductionMode(ProductionMode mode)
    {
        currentMode = mode;
        Debug.Log($"[Production] Mode set to {mode}");
    }

    // ── Coroutines ───────────────────────────────────────────────────────
    private IEnumerator RunProcessA(BatchJob job)
    {
        yield return new WaitForSeconds(job.duration);

        ResourceManager.Instance.ReleaseWorker();
        IsProcessARunning = false;

        if (_bufferQueue.Count < maxBufferSize)
        {
            _bufferQueue.Enqueue(job);
            OnBatchCompletedA?.Invoke(job);
            OnBufferChanged?.Invoke(_bufferQueue.Count);

            // Auto-start B if not running
            if (!IsProcessBRunning && !IsProcessBBlocked)
                StartCoroutine(RunProcessB());
        }
        else
        {
            Debug.LogWarning("[Production] Buffer full! Batch from A lost.");
        }
    }

    private IEnumerator RunProcessB()
    {
        while (_bufferQueue.Count > 0)
        {
            if (IsProcessBBlocked) { yield return new WaitForSeconds(0.5f); continue; }

            var job = _bufferQueue.Dequeue();
            OnBufferChanged?.Invoke(_bufferQueue.Count);

            var rm = ResourceManager.Instance;
            int paintCost = Mathf.RoundToInt(job.product.paintFabricCost * job.quantity * GetCostMultiplier());
            int powerCost = job.product.powerPerPaint * job.quantity;

            rm.ConsumePaintFabric(paintCost);
            rm.ConsumePower(powerCost);
            rm.AssignWorker();

            IsProcessBRunning = true;
            float duration = job.product.paintPackTime * job.quantity / GetSpeedMultiplier();
            yield return new WaitForSeconds(duration);

            rm.ReleaseWorker();

            // Quality mode: add reputation bonus
            if (currentMode == ProductionMode.Quality)
                GameManager.Instance.AddReputation(qualityReputationBonus * job.quantity);

            _completedBatches.Add(job);
            OnBatchCompletedB?.Invoke(job);
        }
        IsProcessBRunning = false;
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private float GetSpeedMultiplier()
    {
        return currentMode switch
        {
            ProductionMode.Fast    => fastSpeedMultiplier,
            ProductionMode.Quality => qualitySpeedMultiplier,
            _                      => 1f
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
