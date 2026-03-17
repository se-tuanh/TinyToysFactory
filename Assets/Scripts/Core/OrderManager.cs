using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// OrderManager — Manages multiple concurrent active orders with deadline countdowns.
/// Faction bonuses applied on FulfillOrder(): FlashDeals, PrestigyPlay, ToyKingdom.
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

    // ── State ─────────────────────────────────────────────────────────────
    // Active order wrapper tracking deadline
    private class ActiveOrder
    {
        public OrderData order;
        public float     timeRemaining;
        public Dictionary<string, int> shipped = new();
    }

    private List<ActiveOrder> _active      = new List<ActiveOrder>();
    private int               _maxActive   => config ? config.maxActiveOrders : 3;
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
        GameManager.Instance.OnGameLose.AddListener(() => _active.Clear());
        AutoAcceptNextOrder();
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
                // Try to fill the slot
                AutoAcceptNextOrder();
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

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

    /// <summary>Auto-fill active order slots from the pool.</summary>
    public void AutoAcceptNextOrder()
    {
        if (_active.Count >= _maxActive) return;
        int tier = PressureDirector.Instance ? PressureDirector.Instance.CurrentTier : 1;

        foreach (var order in availableOrders)
        {
            if (_active.Count >= _maxActive) break;
            if (_active.Exists(a => a.order == order)) continue;
            if (order.minPressureTier <= tier)
                AcceptOrder(order);
        }
    }

    /// <summary>Try to fulfill an order using current inventory.</summary>
    public bool FulfillOrder(OrderData order)
    {
        var ao = _active.Find(a => a.order == order);
        if (ao == null) { Debug.LogWarning($"[OrderManager] {order.orderName} not active."); return false; }

        var rm = ResourceManager.Instance;
        // Check
        foreach (var req in order.requiredProducts)
            if (!rm.HasProduct(req.product, req.quantity)) { Debug.LogWarning($"[OrderManager] Not enough {req.product.productName}."); return false; }

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
                // ToyKingdom bulk bonus: bonus if ALL products in one go (inventory had all at once)
                credits += config ? config.toyKingdomBulkBonus : 30;
                break;
        }

        // Early bonus (all factions get base bonus)
        if (ratio > 0.2f) credits += order.bonusCreditsIfEarly;

        GameManager.Instance.AddCredits(credits);
        GameManager.Instance.AddReputation(order.reputationReward);

        _consecutiveGoodDeliveries++;
        // Tell PressureDirector about good streak
        PressureDirector.Instance?.RegisterGoodDelivery();

        _active.Remove(ao);
        OnOrderCompleted?.Invoke(order);
        Debug.Log($"[OrderManager] Fulfilled: {order.orderName} | +{credits} credits");

        // Fill the vacated slot
        AutoAcceptNextOrder();
        return true;
    }

    // ── Private ───────────────────────────────────────────────────────────
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
        int penalty = config ? config.orderExpireRepPenalty : 10;
        GameManager.Instance.AddReputation(-penalty);
        _consecutiveGoodDeliveries = 0;
        PressureDirector.Instance?.RegisterLateOrder();
        OnOrderExpired?.Invoke(ao.order);
        Debug.LogWarning($"[OrderManager] EXPIRED: {ao.order.orderName} | -{penalty} rep");
    }
}
