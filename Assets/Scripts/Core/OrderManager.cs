using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    [Header("Configuration")]
    public GameplayConfig config;

    [Header("Settings")]
    public float spawnInterval = 5f;
    private float _spawnTimer = 0f;
    public int expireMoneyPenalty = 50;
    public int _maxActive = 7;

    [Header("Pools")]
    public List<OrderData> availableOrders;
    private List<ActiveOrder> _active = new List<ActiveOrder>();
    private List<OrderData> _pending = new List<OrderData>();

    // ── Events ───────────────────────────────────────────────────────────
    public UnityEvent<OrderData> OnOrderStarted;
    public UnityEvent<OrderData> OnOrderCompleted;
    public UnityEvent<OrderData> OnOrderFailed;
    public UnityEvent<OrderData> OnOrderExpired;
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
                ExpireOrder(ao);
                _active.RemoveAt(i);
            }
        }
    }

    public void SpawnRandomOrder()
    {
        if (availableOrders == null || availableOrders.Count == 0) return;
        OrderData newOrder = availableOrders[Random.Range(0, availableOrders.Count)];

        int limit = config ? config.maxActiveOrders : _maxActive;

        if (!_pending.Contains(newOrder) && !_active.Exists(a => a.order == newOrder))
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

    // HÀM QUAN TRỌNG ĐỂ FIX LỖI UI
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
        GameManager.Instance.AddCredits(order.creditsReward);
        GameManager.Instance.AddReputation(order.reputationReward);
        OnOrderCompleted?.Invoke(order);
        _active.Remove(ao);
        return true;
    }

    private void OnBatchReady(BatchJob job)
    {
        foreach (var ao in _active)
        {
            if (ao.shipped.ContainsKey(job.product.productName))
            {
                ao.shipped[job.product.productName] += job.quantity;

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
        bool done = true;
        foreach (var req in ao.order.requiredProducts)
        {
            if (ao.shipped[req.product.productName] < req.quantity) { done = false; break; }
        }
        if (done) FulfillOrder(ao.order);
    }

    private void ExpireOrder(ActiveOrder ao)
    {
        int repPenalty = config ? config.orderExpireRepPenalty : 10;
        GameManager.Instance.AddCredits(-expireMoneyPenalty);
        GameManager.Instance.AddReputation(-repPenalty);

        OnOrderFailed?.Invoke(ao.order);
        OnOrderExpired?.Invoke(ao.order);
    }
}

[System.Serializable]
public class ActiveOrder
{
    public OrderData order;
    public float timeRemaining;
    public Dictionary<string, int> shipped = new Dictionary<string, int>();
}