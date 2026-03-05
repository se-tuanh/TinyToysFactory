using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// OrderManager — Handles incoming orders, tracks progress, and ships completed batches.
/// </summary>
public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

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

    /// <summary>Accept a new order. Only one active order at a time (MVP).</summary>
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
    }
}
