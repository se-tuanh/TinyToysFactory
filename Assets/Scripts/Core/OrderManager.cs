using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// OrderManager — Handles incoming orders, tracks progress, and ships completed batches.
/// </summary>
public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }
    [Header("Mission Spawner")]
    public float spawnInterval = 30f;     // Cứ 30s xả 1 đơn
    private float _spawnTimer = 5f;       // Vào game cho 5s chuẩn bị rồi mới xả
    public int expireMoneyPenalty = 50;   // Trừ 50$ nếu để rớt đơn

    [Header("Order Pool")]
    public List<OrderData> availableOrders; // assign in Inspector

    [Header("Active Order")]
    public OrderData ActiveOrder { get; private set; }

    // progress: productName → shipped quantity
    private Dictionary<string, int> _shippedQuantities = new Dictionary<string, int>();
    private bool _orderActive = false;

    // ── Events ───────────────────────────────────────────────────────────
    public UnityEvent<OrderData> OnOrderStarted;
    public UnityEvent<OrderData> OnOrderCompleted;
    public UnityEvent<OrderData> OnOrderFailed;
    public UnityEvent<string, int, int> OnProgressUpdated; // productName, shipped, required

    // ── Lifecycle ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe to production completing
        ProductionManager.Instance.OnBatchCompletedB.AddListener(OnBatchReady);
        GameManager.Instance.OnGameLose.AddListener(() => _orderActive = false);
    }

    // ── Public API ───────────────────────────────────────────────────────

<<<<<<< Updated upstream
    /// <summary>Accept a new order. Only one active order at a time (MVP).</summary>
=======
        // 1. ĐỒNG HỒ XẢ ĐƠN: Cứ 30s tự động ép 1 đơn mới vào danh sách Active
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = spawnInterval; // Bơm lại 30s

            // Lấy ngẫu nhiên 1 đơn và ép thẳng vào hàm AcceptOrder (Bỏ qua khâu chờ)
            if (availableOrders != null && availableOrders.Count > 0)
            {
                OrderData randomOrder = availableOrders[Random.Range(0, availableOrders.Count)];
                AcceptOrder(randomOrder);
            }
        }

        // 2. ĐỒNG HỒ ĐẾM NGƯỢC: Tụt thời gian của các đơn đang chạy
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ao = _active[i];
            ao.timeRemaining -= Time.deltaTime;
            OnDeadlineTick?.Invoke(ao.order, Mathf.Max(ao.timeRemaining, 0f), ao.order.baseDeadline);

            // Nếu hết thời gian -> Phạt!
            if (ao.timeRemaining <= 0f)
            {
                ExpireOrder(ao);
                _active.RemoveAt(i);
                RefreshPendingPool();
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Expose the current pending pool (read-only view).</summary>
    public IReadOnlyList<OrderData> PendingOrders => _pending;

    /// <summary>Expose the active orders' OrderData (read-only).</summary>
    public IReadOnlyList<OrderData> GetActiveOrderData()
    {
        var list = new List<OrderData>(_active.Count);
        foreach (var ao in _active) list.Add(ao.order);
        return list;
    }

    /// <summary>
    /// Player manually accepts an order from the pending pool.
    /// Deadline starts now.
    /// </summary>
    public bool AcceptOrderManually(OrderData order)
    {
        if (!_pending.Contains(order))
        {
            Debug.LogWarning($"[OrderManager] {order.orderName} is not in pending pool.");
            return false;
        }
        if (_active.Count >= _maxActive)
        {
            Debug.LogWarning("[OrderManager] Active order slots full. Finish or wait for an order to expire.");
            return false;
        }

        _pending.Remove(order);
        OnPendingOrdersChanged?.Invoke();

        return AcceptOrder(order);
    }

    /// <summary>
    /// Returns how many units of a product still need to be delivered for the given order.
    /// Returns -1 if order is not active.
    /// </summary>
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

    /// <summary>Accept a new order if capacity allows.</summary>
>>>>>>> Stashed changes
    public bool AcceptOrder(OrderData order)
    {
        if (_orderActive) return false;

        ActiveOrder = order;
        _shippedQuantities.Clear();

        foreach (var req in order.requiredProducts)
            _shippedQuantities[req.product.productName] = 0;

        _orderActive = true;
        OnOrderStarted?.Invoke(order);
        Debug.Log($"[OrderManager] Order accepted: {order.orderName}");
        return true;
    }

    /// <summary>Auto-accept the first available order matching pressure tier.</summary>
    public void AutoAcceptNextOrder()
    {
        int tier = PressureDirector.Instance.CurrentTier;
        foreach (var order in availableOrders)
        {
            if (order.minPressureTier <= tier)
            {
                AcceptOrder(order);
                return;
            }
        }
    }

    /// <summary>Called when a batch finishes Quy trinh B and is ready to ship.</summary>
    private void OnBatchReady(BatchJob job)
    {
        if (!_orderActive || ActiveOrder == null) return;

        string name = job.product.productName;
        if (!_shippedQuantities.ContainsKey(name)) return; // not part of this order

        _shippedQuantities[name] += job.quantity;

        // Find requirement for progress event
        foreach (var req in ActiveOrder.requiredProducts)
        {
            if (req.product.productName == name)
            {
                int shipped  = Mathf.Min(_shippedQuantities[name], req.quantity);
                OnProgressUpdated?.Invoke(name, shipped, req.quantity);
                break;
            }
        }

        CheckOrderCompletion();
    }

    private void CheckOrderCompletion()
    {
<<<<<<< Updated upstream
        foreach (var req in ActiveOrder.requiredProducts)
        {
            if (_shippedQuantities[req.product.productName] < req.quantity)
                return; // not done yet
        }

        // All requirements met!
        float timeLeft = GameManager.Instance.TimeRemaining;
        float totalTime = GameManager.Instance.sessionDuration;
        float timeLeftRatio = timeLeft / totalTime;

        int totalCredits = ActiveOrder.creditsReward;
        if (timeLeftRatio > 0.2f) totalCredits += ActiveOrder.bonusCreditsIfEarly;

        GameManager.Instance.AddCredits(totalCredits);
        GameManager.Instance.AddReputation(ActiveOrder.reputationReward);

        _orderActive = false;
        OnOrderCompleted?.Invoke(ActiveOrder);
        Debug.Log($"[OrderManager] Order completed: {ActiveOrder.orderName} | Credits: {totalCredits}");

        // Check win condition or queue next order
        GameManager.Instance.TriggerWin();
=======
        // Hủy việc công nhân đang làm dở cho đơn này
        ProductionManager.Instance?.CancelTasksForOrder(ao.order);

        // Trừ uy tín (luật cũ của dev)
        int penalty = config ? config.orderExpireRepPenalty : 10;
        GameManager.Instance.AddReputation(-penalty);

        // TRỪ TIỀN PHẠT (Luật Overcooked mới)
        GameManager.Instance.AddCredits(-expireMoneyPenalty);

        _consecutiveGoodDeliveries = 0;
        PressureDirector.Instance?.RegisterLateOrder();
        OnOrderExpired?.Invoke(ao.order);
        Debug.LogWarning($"[OrderManager] Rớt đơn {ao.order.orderName}! Bị phạt {expireMoneyPenalty}$");
>>>>>>> Stashed changes
    }
}
