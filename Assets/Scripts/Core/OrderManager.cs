using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// OrderManager — Manages pending and active orders.
/// • Pending pool: orders shown on the board waiting for the player to accept.
/// • Active orders: deadline is counting down; player must deliver before time runs out.
/// If config.allowManualOnlyAccept is false, old auto-accept behaviour is preserved.
/// </summary>
public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }
    [Header("Mission Spawner")]
    public float spawnInterval = 30f;     // Cứ 30s xả 1 đơn
    private float _spawnTimer = 5f;       // Vào game cho 5s chuẩn bị rồi mới xả
    public int expireMoneyPenalty = 50;   // Trừ 50$ nếu để rớt đơn

    [Header("Order Pool")]
    public List<OrderData> availableOrders;

    [Header("Config (assign GameplayConfig SO)")]
    public GameplayConfig config;

    // ── Events ────────────────────────────────────────────────────────────
    public UnityEvent<OrderData>              OnOrderStarted;
    public UnityEvent<OrderData>              OnOrderCompleted;
    public UnityEvent<OrderData>              OnOrderFailed;
    public UnityEvent<OrderData>              OnOrderExpired;   // deadline hit 0
    // (orderData, timeRemaining, totalTime) — for deadline bar UI
    public UnityEvent<OrderData, float, float> OnDeadlineTick;
    public UnityEvent<string, int, int>        OnProgressUpdated; // productName, shipped, required
    /// <summary>Fires whenever the pending pool changes so UI can refresh.</summary>
    public UnityEvent                          OnPendingOrdersChanged;

    // ── State ─────────────────────────────────────────────────────────────
    // Active order wrapper tracking deadline
    private class ActiveOrder
    {
        public OrderData order;
        public float     timeRemaining;
        public Dictionary<string, int> shipped = new();
    }

    private List<ActiveOrder> _active      = new List<ActiveOrder>();
    private List<OrderData>   _pending     = new List<OrderData>(); // waiting for player to accept

    private int _maxActive   => config ? config.maxActiveOrders  : 3;
    private int _maxPending  => config ? config.maxPendingOrders  : 4;
    private bool _manualOnly => config ? config.allowManualOnlyAccept : true;

    private int _consecutiveGoodDeliveries = 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ProductionManager.Instance.OnBatchCompletedB.AddListener(OnBatchReady);
        GameManager.Instance.OnGameLose.AddListener(() => { _active.Clear(); _pending.Clear(); });
        RefreshPendingPool();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

    /// <summary>Accept a new order. Only one active order at a time (MVP).</summary>
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
public List<OrderData> GetActiveOrders()
    {
        var list = new List<OrderData>(_active.Count);
        foreach (var ao in _active) list.Add(ao.order);
        return list;
    }

    public bool AcceptOrderManually(OrderData order)
    {
        // Logic này để dự phòng nếu sau này fen muốn quay lại cách chơi cũ
        if (_active.Count >= (config ? config.maxActiveOrders : 3)) return false;
        return AcceptOrder(order);
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

    public bool AcceptOrder(OrderData order)
    {
        if (_active.Count >= (config ? config.maxActiveOrders : 3)) return false;
        if (_active.Exists(a => a.order == order)) return false; 

        var ao = new ActiveOrder
        {
            order = order,
            timeRemaining = order.baseDeadline
        };
        foreach (var req in order.requiredProducts)
            ao.shipped[req.product.productName] = 0;

        _active.Add(ao);
        OnOrderStarted?.Invoke(order);
        Debug.Log($"[Overcooked] Đã ép đơn: {order.orderName}");
        return true;
    }

    public bool FulfillOrder(OrderData order)
    {
        var ao = _active.Find(a => a.order == order);
        if (ao == null) return false;

        // Logic check nguyên liệu và trả thưởng
        int credits = order.creditsReward;
        GameManager.Instance.AddCredits(credits);
        GameManager.Instance.AddReputation(order.reputationReward);

        _active.Remove(ao);
        OnOrderCompleted?.Invoke(order);
        return true;
    }

    private void OnBatchReady(BatchJob job)
    {
        foreach (var ao in _active)
        {
            string name = job.product.productName;
            if (!ao.shipped.ContainsKey(name)) continue;
            ao.shipped[name] += job.quantity;
        }
    }

    private void ExpireOrder(ActiveOrder ao)
    {
        // Đây là phần sửa lỗi: Thay vì TriggerWin (Thắng), mình dùng Penalty (Phạt) của ông Tuan-Anh
        int penalty = config ? config.orderExpireRepPenalty : 10;
        GameManager.Instance.AddReputation(-penalty);
        
        _active.Remove(ao);
        OnOrderExpired?.Invoke(ao.order);
        Debug.LogWarning($"[Overcooked] Hết hạn: {ao.order.orderName} | Bị trừ {penalty} uy tín");
    }
    }
}
