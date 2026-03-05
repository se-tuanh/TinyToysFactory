using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// EventPopupUI — Shows the Decision Triad (Prioritize / Repair / Trade) when an event fires.
/// Assign this to a Canvas Panel that is hidden by default.
/// Requires: TextMeshPro
/// </summary>
public class EventPopupUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public TextMeshProUGUI eventNameText;
    public TextMeshProUGUI eventDescText;
    public Image eventIcon;

    [Header("Choice Buttons")]
    public Button prioritizeButton;
    public Button repairButton;
    public Button tradeButton;

    public TextMeshProUGUI prioritizeLabel;
    public TextMeshProUGUI repairLabel;
    public TextMeshProUGUI tradeLabel;

    public TextMeshProUGUI prioritizeDesc;
    public TextMeshProUGUI repairDesc;
    public TextMeshProUGUI tradeDesc;

    // ── State ─────────────────────────────────────────────────────────────
    private RandomEventData _currentEvent;
    public UnityEvent<EventChoice> OnChoiceMade;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Start()
    {
        popupPanel.SetActive(false);

        // Wire buttons
        prioritizeButton.onClick.AddListener(() => MakeChoice(_currentEvent.prioritize));
        repairButton    .onClick.AddListener(() => MakeChoice(_currentEvent.repair));
        tradeButton     .onClick.AddListener(() => MakeChoice(_currentEvent.trade));

        // Subscribe to director
        PressureDirector.Instance.OnEventTriggered.AddListener(ShowEvent);
    }

    // ── Public API ────────────────────────────────────────────────────────
    public void ShowEvent(RandomEventData evt)
    {
        _currentEvent = evt;

        // Populate UI
        eventNameText.text  = evt.eventName;
        eventDescText.text  = evt.eventDescription;
        if (evt.eventIcon != null) eventIcon.sprite = evt.eventIcon;

        SetChoiceButton(prioritizeLabel, prioritizeDesc, evt.prioritize);
        SetChoiceButton(repairLabel,     repairDesc,     evt.repair);
        SetChoiceButton(tradeLabel,      tradeDesc,      evt.trade);

        popupPanel.SetActive(true);
        Time.timeScale = 0f; // pause while player decides
    }

    // ── Private ───────────────────────────────────────────────────────────
    private void SetChoiceButton(TextMeshProUGUI label, TextMeshProUGUI desc, EventChoice choice)
    {
        label.text = choice.choiceLabel;
        string costInfo = "";
        if (choice.timeCost > 0)    costInfo += $" | -{choice.timeCost}s";
        if (choice.creditCost > 0)  costInfo += $" | -{choice.creditCost}¢";
        if (choice.materialCost > 0)costInfo += $" | -mat {choice.materialCost}";
        desc.text = choice.choiceDescription + costInfo;
    }

    private void MakeChoice(EventChoice choice)
    {
        popupPanel.SetActive(false);
        Time.timeScale = 1f; // resume game
        PressureDirector.Instance.ResolveEvent(choice);
        OnChoiceMade?.Invoke(choice);
    }
}
