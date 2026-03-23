using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    [Header("Configuration")]
    public GameplayConfig config;

    [Header("Settings")]
    public float spawnInterval = 30f;
    private float _spawnTimer = 30f;
    public int expireMoneyPenalty = 50;
    public int cancelMoneyPenalty = 20;
    public int _maxActive = 10;     

    [Header("Pools")]
    public List<OrderData> availableOrders;
    private List<ActiveOrder> _active = new List<ActiveOrder>();
    private List<OrderData> _pending = new List<OrderData>();

    // ── Events ───────────────────────────────────────────────────────────
    public UnityEvent<OrderData> OnOrderStarted;
    public UnityEvent<OrderData> OnOrderCompleted;
    public UnityEvent<OrderData> OnOrderFailed;
    public UnityEvent<OrderData> OnOrderExpired;
    public UnityEvent<OrderData> OnOrderCancelled;
    public UnityEvent<OrderData, float, float> OnDeadlineTick;
    public UnityEvent OnPendingOrdersChanged;
    public UnityEvent<string, int, int> OnProgressUpdated;

    public IReadOnlyList<OrderData> PendingOrders => _pending;

    private void Awake() { Instance = this; }

    private void Start()
    {
        if (ProductionManager.Instance != null)
            ProductionManager.Instance.OnBatchCompletedB.AddListener(OnBatchReady);
    }

    private void Update()
    {
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = spawnInterval;
            SpawnRandomOrder();
        }

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ao = _active[i];
            ao.timeRemaining -= Time.deltaTime;
            OnDeadlineTick?.Invoke(ao.order, ao.timeRemaining, ao.order.baseDeadline);

            if (ao.timeRemaining <= 0f)
            {
                ao.isExpired = true;  // Mark expired FIRST
                _active.RemoveAt(i);  // Remove BEFORE firing events
                
                // Now fire penalty after removal
                int repPenalty = config ? config.orderExpireRepPenalty : 10;
                GameManager.Instance.SpendCredits(expireMoneyPenalty);
                GameManager.Instance.AddReputation(-repPenalty);

                OnOrderFailed?.Invoke(ao.order);
                OnOrderExpired?.Invoke(ao.order);
            }
        }
    }

    public void SpawnRandomOrder()
    {
        if (availableOrders == null || availableOrders.Count == 0) return;
        OrderData newOrder = availableOrders[Random.Range(0, availableOrders.Count)];

        int limit = config ? config.maxActiveOrders : _maxActive;

        if (newOrder != null && !_pending.Contains(newOrder) && !_active.Exists(a => a.order == newOrder))
        {
            _pending.Add(newOrder);
            OnPendingOrdersChanged?.Invoke();
            if (_active.Count < limit) AcceptOrderManually(newOrder);
        }
    }

    public bool AcceptOrderManually(OrderData order)
    {
        int limit = config ? config.maxActiveOrders : _maxActive;
        if (_active.Count >= limit) return false;

        if (_pending.Contains(order)) _pending.Remove(order);

        ActiveOrder ao = new ActiveOrder
        {
            order = order,
            timeRemaining = order.baseDeadline
        };

        foreach (var req in order.requiredProducts)
            ao.shipped[req.product.productName] = 0;

        _active.Add(ao);
        OnOrderStarted?.Invoke(order);
        OnPendingOrdersChanged?.Invoke();
        return true;
    }

    public int GetRemainingQuantity(OrderData order, ProductData product)
    {
        var ao = _active.Find(a => a.order == order);
        if (ao == null) return -1;

        foreach (var req in order.requiredProducts)
        {
            if (req.product == product)
            {
                int shipped = ao.shipped.TryGetValue(product.productName, out int s) ? s : 0;
                return Mathf.Max(0, req.quantity - shipped);
            }
        }
        return 0;
    }

    public bool FulfillOrder(OrderData order)
    {
        var ao = _active.Find(a => a.order == order);
        if (ao == null) return false;
        if (ao.isExpired) return false;
        // Consume products from inventory when delivering
        var rm = ResourceManager.Instance;
        if (rm != null)
        {
            foreach (var req in order.requiredProducts)
            {
                int needed = req.quantity;
                bool ok = rm.RemoveProduct(req.product, needed);
                if (!ok)
                {
                    Debug.LogWarning($"[OrderManager] Not enough inventory to remove {needed}x {req.product.productName} when fulfilling '{order.orderName}'. Removing available amount instead.");
                    int available = rm.GetProductCount(req.product);
                    if (available > 0) rm.RemoveProduct(req.product, available);
                }
            }
        }

        GameManager.Instance.AddCredits(order.creditsReward);
        GameManager.Instance.AddReputation(order.reputationReward);
        OnOrderCompleted?.Invoke(order);
        _active.Remove(ao);
        return true;
    }

    public void CancelOrder(OrderData order)
    {
        var ao = _active.Find(a => a.order == order);
        if (ao == null) return;

        // Penalty
        GameManager.Instance.SpendCredits(cancelMoneyPenalty);
        
        // Cleanup production
        ProductionManager.Instance.CancelTasksForOrder(order);

        OnOrderCancelled?.Invoke(order);
        _active.Remove(ao);
        Debug.Log($"[OrderManager] Order '{order.orderName}' cancelled manually. Penalty -{cancelMoneyPenalty} credits.");
    }

    private void OnBatchReady(BatchJob job)
    {
        Debug.Log($"[OrderManager.OnBatchReady] Batch ready: {job.product.productName}x{job.quantity}");
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ao = _active[i];

            // Skip if order has expired
            if (ao.isExpired || ao.timeRemaining <= 0f)
            {
                Debug.Log($"[OrderManager.OnBatchReady] Skipping expired order '{ao.order.orderName}' - isExpired={ao.isExpired}, timeRemaining={ao.timeRemaining}");
                continue;
            }

            if (ao.shipped.ContainsKey(job.product.productName))
            {
                ao.shipped[job.product.productName] += job.quantity;
                Debug.Log($"[OrderManager.OnBatchReady] Added batch to order '{ao.order.orderName}': {job.product.productName}x{job.quantity}");

                // Cập nhật tiến độ UI
                foreach (var req in ao.order.requiredProducts)
                {
                    if (req.product.productName == job.product.productName)
                    {
                        int current = Mathf.Min(ao.shipped[job.product.productName], req.quantity);
                        OnProgressUpdated?.Invoke(job.product.productName, current, req.quantity);
                    }
                }

                CheckCompletion(ao);
            }
        }
    }

    private void CheckCompletion(ActiveOrder ao)
    {
        // Don't complete if order has expired
        if (ao.isExpired || ao.timeRemaining <= 0f)
        {
            Debug.Log($"[OrderManager.CheckCompletion] Skipped expired order '{ao.order.orderName}' - isExpired={ao.isExpired}, timeRemaining={ao.timeRemaining}");
            return;
        }

        bool done = true;
        foreach (var req in ao.order.requiredProducts)
        {
            if (ao.shipped[req.product.productName] < req.quantity) { done = false; break; }
        }
        if (done)
        {
            Debug.Log($"[OrderManager.CheckCompletion] Order '{ao.order.orderName}' completed! Fulfilling...");
            FulfillOrder(ao.order);
        }
    }

    // Expired orders are now handled directly in Update() loop
    // This method is kept for backward compatibility but shouldn't be called directly
    [System.Obsolete("Use Update loop instead")]
    private void ExpireOrder(ActiveOrder ao)
    {
        Debug.LogWarning("[OrderManager.ExpireOrder] This method should not be called directly! Use Update loop instead.");
    }
}

[System.Serializable]
public class ActiveOrder
{
    public OrderData order;
    public float timeRemaining;
    public bool isExpired = false;  // Flag to prevent completing expired orders
    public Dictionary<string, int> shipped = new Dictionary<string, int>();
}