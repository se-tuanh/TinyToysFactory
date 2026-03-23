using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OrderBoardUI — Displays pending orders (waiting for player to accept)
/// and active orders (deadline counting, production progress, Deliver button).
///
/// Prefab structures expected:
///
/// PendingSlot prefab:
///   PendingSlot
///   ├── OrderNameText     (TextMeshProUGUI)
///   ├── RequiredText      (TextMeshProUGUI)
///   ├── DeadlineHintText  (TextMeshProUGUI)   — e.g. "Hạn: 3:00"
///   └── AcceptButton      (Button)
///
/// ActiveSlot prefab:
///   ActiveSlot
///   ├── OrderNameText     (TextMeshProUGUI)
///   ├── RequiredText      (TextMeshProUGUI)
///   ├── DeadlineBar       (Image — fillable)
///   ├── DeadlineText      (TextMeshProUGUI)   — remaining time mm:ss
///   ├── QueueButton       (Button)            — "📋 Sản xuất"
///   └── DeliverButton     (Button)
/// </summary>
public class OrderBoardUI : MonoBehaviour
{
    [Header("Pending Orders (waiting for player to accept)")]
    public Transform  pendingContainer; // parent for pending order rows
    public GameObject pendingSlotPrefab;

    [Header("Active Orders (deadline running)")]
    public Transform  activeContainer;  // parent for active order rows
    public GameObject activeSlotPrefab;

    // ── Expired Toast ──────────────────────────────────────────────────────
    [Header("Expired Order Toast")]
    public GameObject      expiredToastPanel; // red flash panel (hidden by default)
    public TextMeshProUGUI expiredToastText;

    // ── Slot tracking ─────────────────────────────────────────────────────
    private readonly Dictionary<OrderData, GameObject> _pendingSlots = new();
    private readonly Dictionary<OrderData, GameObject> _activeSlots  = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Start()
    {
        var om = OrderManager.Instance;
        om.OnOrderStarted           .AddListener(OnOrderStarted);
        om.OnOrderCompleted         .AddListener(order => RemoveActiveSlot(order));
        om.OnOrderFailed            .AddListener(order => RemoveActiveSlot(order));
        om.OnOrderCancelled         .AddListener(order => RemoveActiveSlot(order));
        om.OnOrderExpired           .AddListener(order => { RemoveActiveSlot(order); ShowExpiredToast(order.orderName); });
        om.OnDeadlineTick           .AddListener(OnDeadlineTick);
        om.OnPendingOrdersChanged   .AddListener(RefreshPendingList);

        if (expiredToastPanel) expiredToastPanel.SetActive(false);

        // Populate initial pending pool
        RefreshPendingList();
    }

    // ── Pending pool ──────────────────────────────────────────────────────
    private void RefreshPendingList()
    {
        var om = OrderManager.Instance;

        // Remove slots for orders no longer in pending
        var toRemove = new List<OrderData>();
        foreach (var kvp in _pendingSlots)
            if (!om.PendingOrders.Contains(kvp.Key))
                toRemove.Add(kvp.Key);

        foreach (var o in toRemove) RemovePendingSlot(o);

        // Add slots for new pending orders
        foreach (var order in om.PendingOrders)
            CreatePendingSlot(order);
    }

    private void CreatePendingSlot(OrderData order)
    {
        if (order == null) return;
        if (!pendingSlotPrefab || !pendingContainer) return;
        if (_pendingSlots.ContainsKey(order)) return;

        var slot = Instantiate(pendingSlotPrefab, pendingContainer);
        _pendingSlots[order] = slot;

        // Name
        var nameText = slot.transform.Find("OrderNameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText) nameText.text = $"Order: {order.orderName}";

        // Required products
        var reqText = slot.transform.Find("RequiredText")?.GetComponent<TextMeshProUGUI>();
        if (reqText)
        {
            reqText.text = "Items: ";
            foreach (var req in order.requiredProducts)
                reqText.text += $"{req.quantity}x {req.product.productName}, ";
        }

        // Deadline hint
        var hintText = slot.transform.Find("DeadlineHintText")?.GetComponent<TextMeshProUGUI>();
        if (hintText)
        {
            int mins = Mathf.FloorToInt(order.baseDeadline / 60f);
            int secs = Mathf.FloorToInt(order.baseDeadline % 60f);
            hintText.text = $"Deadline: {mins}:{secs:00}";
        }

        // Accept button
        var btn = slot.transform.Find("AcceptButton")?.GetComponent<Button>();
        if (btn)
        {
            var capturedOrder = order;
            btn.onClick.AddListener(() =>
            {
                bool ok = OrderManager.Instance.AcceptOrderManually(capturedOrder);
                if (!ok)
                    Debug.LogWarning("[OrderBoardUI] Cannot accept order — slots full or order not pending.");
            });
        }
    }

    private void RemovePendingSlot(OrderData order)
    {
        if (_pendingSlots.TryGetValue(order, out var slot))
        {
            Destroy(slot);
            _pendingSlots.Remove(order);
        }
    }

    // ── Active orders ─────────────────────────────────────────────────────
    private void OnOrderStarted(OrderData order)
    {
        // Remove from pending view first
        RemovePendingSlot(order);

        if (!activeSlotPrefab || !activeContainer) return;
        if (_activeSlots.ContainsKey(order)) return;

        var slot = Instantiate(activeSlotPrefab, activeContainer);
        _activeSlots[order] = slot;

        // Name
        var nameText = slot.transform.Find("OrderNameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText) nameText.text = $"Order: {order.orderName}";

        // Queue button — enqeue production tasks for all required products
        var queueBtn = slot.transform.Find("QueueButton")?.GetComponent<Button>();
        if (queueBtn)
        {
            var capturedOrder = order;
            queueBtn.onClick.AddListener(() => EnqueueProductionForOrder(capturedOrder));
        }

        // Deliver button
        var deliverBtn = slot.transform.Find("DeliverButton")?.GetComponent<Button>();
        if (deliverBtn)
        {
            var capturedOrder = order;
            deliverBtn.onClick.AddListener(() => OrderManager.Instance.FulfillOrder(capturedOrder));
        }

        // Cancel button
        var cancelBtn = slot.transform.Find("CancelButton")?.GetComponent<Button>();
        if (cancelBtn)
        {
            var capturedOrder = order;
            cancelBtn.onClick.AddListener(() => OrderManager.Instance.CancelOrder(capturedOrder));
        }
    }

    private void EnqueueProductionForOrder(OrderData order)
    {
        var pm = ProductionManager.Instance;
        var om = OrderManager.Instance;

        foreach (var req in order.requiredProducts)
        {
            int remaining = om.GetRemainingQuantity(order, req.product);
            if (remaining <= 0) continue;

            var task = new ProductionTask(order, req.product, remaining);
            pm.EnqueueTask(task);
        }

        Debug.Log($"[OrderBoardUI] Production tasks queued for order '{order.orderName}'.");
    }

    private void Update()
    {
        if (ResourceManager.Instance == null) return;

        foreach (var kvp in _activeSlots)
        {
            var order = kvp.Key;
            var slot  = kvp.Value;

            // Required text with live inventory count
            var reqText = slot.transform.Find("RequiredText")?.GetComponent<TextMeshProUGUI>();
            var deliverBtn = slot.transform.Find("DeliverButton")?.GetComponent<Button>();

            bool allMet = true;
            if (reqText)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var req in order.requiredProducts)
                {
                    int current = ResourceManager.Instance.GetProductCount(req.product);
                    int remaining = OrderManager.Instance.GetRemainingQuantity(order, req.product);
                    string status = current >= req.quantity ? "Done" : "Pending";
                    sb.AppendLine($"  {status} {req.product.productName}  {current}/{req.quantity}  (needed: {remaining})");
                    if (current < req.quantity) allMet = false;
                }
                reqText.text = sb.ToString().TrimEnd();
            }

            if (deliverBtn) deliverBtn.interactable = allMet;
        }
    }

    private void OnDeadlineTick(OrderData order, float remaining, float total)
    {
        if (!_activeSlots.TryGetValue(order, out var slot)) return;

        // Bar
        var bar = slot.transform.Find("DeadlineBar")?.GetComponent<Image>();
        if (bar)
        {
            float ratio = Mathf.Clamp01(remaining / total);
            bar.fillAmount = ratio;
            bar.color = ratio < 0.2f ? new Color(0.9f, 0.15f, 0.15f)
                      : ratio < 0.5f ? new Color(0.9f, 0.60f, 0.10f)
                      :                new Color(0.2f, 0.72f, 0.46f);
        }

        // Time text
        var timeText = slot.transform.Find("DeadlineText")?.GetComponent<TextMeshProUGUI>();
        if (timeText)
        {
            int mins = Mathf.FloorToInt(remaining / 60f);
            int secs = Mathf.FloorToInt(remaining % 60f);
            timeText.text = $"Time: {mins}:{secs:00}";

            // Urgent blink when < 20%
            bool urgent = remaining / total < 0.2f;
            timeText.color = urgent ? new Color(0.9f, 0.15f, 0.15f) : Color.white;
        }
    }

    private void RemoveActiveSlot(OrderData order)
    {
        if (_activeSlots.TryGetValue(order, out var slot))
        {
            Destroy(slot);
            _activeSlots.Remove(order);
        }
    }

    // ── Expired Toast ─────────────────────────────────────────────────────
    private Coroutine _toastCoroutine;

    private void ShowExpiredToast(string orderName)
    {
        if (expiredToastPanel == null) return;
        if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
        _toastCoroutine = StartCoroutine(ToastRoutine(orderName));
    }

    private IEnumerator ToastRoutine(string orderName)
    {
        if (expiredToastText) expiredToastText.text = $"❌ Đơn '{orderName}' đã hết hạn!";
        expiredToastPanel.SetActive(true);

        // Fade in quickly, hold, then fade out
        var canvasGroup = expiredToastPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = expiredToastPanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        float t = 0f;
        while (t < 0.3f) { t += Time.deltaTime; canvasGroup.alpha = t / 0.3f; yield return null; }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(2.5f);

        t = 0f;
        while (t < 0.5f) { t += Time.deltaTime; canvasGroup.alpha = 1f - t / 0.5f; yield return null; }
        expiredToastPanel.SetActive(false);
        _toastCoroutine = null;
    }
}
