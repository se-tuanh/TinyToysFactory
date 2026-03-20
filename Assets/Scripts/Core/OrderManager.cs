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

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ao = _active[i];
            ao.timeRemaining -= Time.deltaTime;
            OnDeadlineTick?.Invoke(ao.order, Mathf.Max(ao.timeRemaining, 0f), ao.order.baseDeadline);

            if (ao.timeRemaining <= 0f)
            {
                ExpireOrder(ao);
                _active.RemoveAt(i);
                // Replenish pending pool
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
    public bool AcceptOrder(OrderData order)
    {
        if (_active.Count >= _maxActive) return false;
        if (_active.Exists(a => a.order == order)) return false; // already active

        var ao = new ActiveOrder
        {
            order = order,
            timeRemaining = order.baseDeadline
        };
        foreach (var req in order.requiredProducts)
            ao.shipped[req.product.productName] = 0;

        _active.Add(ao);
        OnOrderStarted?.Invoke(order);
        Debug.Log($"[OrderManager] Accepted: {order.orderName} (deadline {order.baseDeadline}s)");
        return true;
    }

    /// <summary>Try to fulfill an order using current inventory.</summary>
    public bool FulfillOrder(OrderData order)
    {
        var ao = _active.Find(a => a.order == order);
        if (ao == null) { Debug.LogWarning($"[OrderManager] {order.orderName} not active."); return false; }

        var rm = ResourceManager.Instance;
        // Check
        foreach (var req in order.requiredProducts)
            if (!rm.HasProduct(req.product, req.quantity))
            {
                Debug.LogWarning($"[OrderManager] Not enough {req.product.productName}.");
                return false;
            }

        // Consume
        foreach (var req in order.requiredProducts)
            rm.RemoveProduct(req.product, req.quantity);

        // Credits
        int credits = order.creditsReward;
        float ratio = ao.timeRemaining / order.baseDeadline;

        switch (order.faction)
        {
            case ClientFaction.FlashDeals:
                float earlyThreshold = config ? config.flashDealsEarlyThreshold : 0.30f;
                float earlyMult      = config ? config.flashDealsEarlyBonusMult  : 1.5f;
                if (ratio > earlyThreshold) credits = Mathf.RoundToInt(credits * earlyMult);
                break;

            case ClientFaction.PrestigyPlay:
                int repBonus = config ? config.prestigyPlayRepBonus : 5;
                GameManager.Instance.AddReputation(repBonus);
                break;

            case ClientFaction.ToyKingdom:
                credits += config ? config.toyKingdomBulkBonus : 30;
                break;
        }

        // Early bonus (all factions get base bonus)
        if (ratio > 0.2f) credits += order.bonusCreditsIfEarly;

        GameManager.Instance.AddCredits(credits);
        GameManager.Instance.AddReputation(order.reputationReward);

        _consecutiveGoodDeliveries++;
        PressureDirector.Instance?.RegisterGoodDelivery();

        _active.Remove(ao);
        OnOrderCompleted?.Invoke(order);
        Debug.Log($"[OrderManager] Fulfilled: {order.orderName} | +{credits} credits");

        // Replenish pending pool
        RefreshPendingPool();
        return true;
    }

    // ── Private ───────────────────────────────────────────────────────────

    /// <summary>
    /// Fill the pending pool from availableOrders.
    /// If allowManualOnlyAccept == false, also auto-accepts into active slots (legacy mode).
    /// </summary>
    public void RefreshPendingPool()
    {
        int tier = PressureDirector.Instance ? PressureDirector.Instance.CurrentTier : 1;

        foreach (var order in availableOrders)
        {
            if (_pending.Count >= _maxPending) break;
            if (_pending.Contains(order)) continue;
            if (_active.Exists(a => a.order == order)) continue;
            if (order.minPressureTier > tier) continue;

            if (_manualOnly)
            {
                // Add to pending pool — player decides when to accept
                _pending.Add(order);
                OnPendingOrdersChanged?.Invoke();
            }
            else
            {
                // Legacy: auto-accept into active
                if (_active.Count < _maxActive)
                    AcceptOrder(order);
            }
        }
    }

    private void OnBatchReady(BatchJob job)
    {
        foreach (var ao in _active)
        {
            string name = job.product.productName;
            if (!ao.shipped.ContainsKey(name)) continue;

            ao.shipped[name] += job.quantity;
            foreach (var req in ao.order.requiredProducts)
            {
                if (req.product.productName == name)
                {
                    int shipped = Mathf.Min(ao.shipped[name], req.quantity);
                    OnProgressUpdated?.Invoke(name, shipped, req.quantity);
                    break;
                }
            }
        }
    }

    private void ExpireOrder(ActiveOrder ao)
    {
        // Cancel any pending production tasks for this order
        ProductionManager.Instance?.CancelTasksForOrder(ao.order);

        int penalty = config ? config.orderExpireRepPenalty : 10;
        GameManager.Instance.AddReputation(-penalty);
        _consecutiveGoodDeliveries = 0;
        PressureDirector.Instance?.RegisterLateOrder();
        OnOrderExpired?.Invoke(ao.order);
        Debug.LogWarning($"[OrderManager] EXPIRED: {ao.order.orderName} | -{penalty} rep");
    }
}
