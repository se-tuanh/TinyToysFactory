using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OrderBoardUI — Displays all active orders as rows.
/// Each row shows: order name, required products, deadline bar, Deliver button.
/// Attach to an empty GameObject inside the Canvas. Assign slotPrefab in Inspector.
/// </summary>
public class OrderBoardUI : MonoBehaviour
{
    [Header("References")]
    public Transform  slotsContainer; // parent for order slot rows
    public GameObject slotPrefab;     // prefab with the structure below

    // Prefab structure expected:
    //   OrderSlot
    //   ├── OrderNameText   (TextMeshProUGUI)
    //   ├── RequiredText    (TextMeshProUGUI)
    //   ├── DeadlineBar     (Image — fillable)
    //   └── DeliverButton   (Button)

    private readonly Dictionary<OrderData, GameObject> _slots = new();

    private void Start()
    {
        var om = OrderManager.Instance;
        om.OnOrderStarted  .AddListener(OnOrderStarted);
        om.OnOrderCompleted.AddListener(order => RemoveSlot(order));
        om.OnOrderFailed   .AddListener(order => RemoveSlot(order));
        om.OnOrderExpired  .AddListener(order => RemoveSlot(order));
        om.OnDeadlineTick  .AddListener(OnDeadlineTick);
    }

    // ── Slot lifecycle ────────────────────────────────────────────────────
    private void OnOrderStarted(OrderData order)
    {
        if (!slotPrefab || !slotsContainer) return;
        if (_slots.ContainsKey(order)) return;

        var slot = Instantiate(slotPrefab, slotsContainer);
        _slots[order] = slot;

        // Name
        var nameText = slot.transform.Find("OrderNameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText) nameText.text = $"📦 {order.orderName}";

        // The required text and Deliver button state are now updated dynamically in Update()

        // Deliver button setup
        var btn = slot.transform.Find("DeliverButton")?.GetComponent<Button>();
        if (btn)
        {
            var capturedOrder = order;
            btn.onClick.AddListener(() => OrderManager.Instance.FulfillOrder(capturedOrder));
        }
    }

    private void Update()
    {
        if (ResourceManager.Instance == null) return;

        foreach (var kvp in _slots)
        {
            var order = kvp.Key;
            var slot = kvp.Value;

            var reqText = slot.transform.Find("RequiredText")?.GetComponent<TextMeshProUGUI>();
            var btn = slot.transform.Find("DeliverButton")?.GetComponent<Button>();

            bool allMet = true;
            if (reqText)
            {
                var lines = new System.Text.StringBuilder();
                foreach (var req in order.requiredProducts)
                {
                    int current = ResourceManager.Instance.GetProductCount(req.product);
                    lines.AppendLine($"  • {req.product.productName} {current}/{req.quantity}");
                    if (current < req.quantity) allMet = false;
                }
                reqText.text = lines.ToString().TrimEnd();
            }

            if (btn)
            {
                btn.interactable = allMet;
            }
        }
    }

    private void OnDeadlineTick(OrderData order, float remaining, float total)
    {
        if (!_slots.TryGetValue(order, out var slot)) return;

        var bar = slot.transform.Find("DeadlineBar")?.GetComponent<Image>();
        if (bar)
        {
            float ratio = Mathf.Clamp01(remaining / total);
            bar.fillAmount = ratio;
            bar.color = ratio < 0.2f ? new Color(0.9f, 0.15f, 0.15f)
                      : ratio < 0.5f ? new Color(0.9f, 0.60f, 0.10f)
                      :                new Color(0.2f, 0.72f, 0.46f);
        }
    }

    private void RemoveSlot(OrderData order)
    {
        if (_slots.TryGetValue(order, out var slot))
        {
            Destroy(slot);
            _slots.Remove(order);
        }
    }
}
