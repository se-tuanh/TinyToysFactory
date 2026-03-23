using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UIManager — Master HUD controller.
/// Wires resource/state changes from managers to UI elements.
/// All references assigned in the Inspector.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Timer")]
    public TextMeshProUGUI timerText;
    public Image           timerFillBar;

    [Header("Resources")]
    public TextMeshProUGUI woodPlasticText;
    public TextMeshProUGUI paintFabricText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI workerText;
    public Image           woodFillBar;
    public Image           paintFillBar;
    public Image           powerFillBar;

    [Header("Economy")]
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI reputationText;

    [Header("Pressure Tier")]
    public TextMeshProUGUI tierText;
    public Image[]         tierIndicators; // 5 images, light up per tier

    [Header("Production Status")]
    public TextMeshProUGUI processAStatusText;
    public TextMeshProUGUI processBStatusText;
    public TextMeshProUGUI bufferCountText;

    [Header("Order Progress")]
    public Transform  orderProgressContainer;
    public GameObject orderProgressRowPrefab;

    [Header("Win/Lose Panels")]
    public GameObject      winPanel;
    public GameObject      losePanel;
    public TextMeshProUGUI winCreditsText;

    [Header("Buttons")]
    public Button pauseButton;
    public Button emergencyBuyButton;

    [Header("HUD Toggling")]
    public GameObject      tabRow;
    public TextMeshProUGUI hudToggleText;
    private bool           _hudHidden;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private float _totalTime;

    private void Start()
    {
        var gm = GameManager.Instance;
        var rm = ResourceManager.Instance;
        var pm = ProductionManager.Instance;
        var pd = PressureDirector.Instance;

        _totalTime = gm.sessionDuration;

        // Timer
        gm.OnTimeChanged.AddListener(UpdateTimer);

        // Resources
        rm.OnWoodPlasticChanged.AddListener(v => UpdateResourceBar(woodPlasticText, woodFillBar, v, rm.maxWoodPlastic, "Wood: "));
        rm.OnPaintFabricChanged.AddListener(v => UpdateResourceBar(paintFabricText, paintFillBar, v, rm.maxPaintFabric, "Paint: "));
        rm.OnPowerChanged      .AddListener(v => UpdateResourceBar(powerText,       powerFillBar, v, rm.maxPower, "Power: "));
        rm.OnWorkersChanged    .AddListener(v => { if (workerText) workerText.text = $"Workers: {v}/{rm.maxWorkers}"; });
        rm.OnMaterialCritical  .AddListener(() => StartCoroutine(FlashBars()));

        // Economy
        gm.OnCreditsChanged   .AddListener(v => { if (creditsText)    creditsText.text    = $"${v}";     });
        gm.OnReputationChanged.AddListener(v => { if (reputationText) reputationText.text = $"Rep: {v}"; });

        // Pressure tier
        pd.OnTierChanged.AddListener(UpdateTierUI);

        // Production status — refresh whenever a batch finishes or buffer changes
        pm.OnBatchCompletedA.AddListener(_ => UpdateProductionStatus());
        pm.OnBatchCompletedB.AddListener(_ => UpdateProductionStatus());
        pm.OnBufferChanged  .AddListener(v => { if (bufferCountText) bufferCountText.text = $"Buffer: {v}/{pm.maxBufferSize}"; });

        // Win / Lose
        gm.OnGameWin .AddListener(() =>
        {
            winPanel?.SetActive(true);
            if (winCreditsText) winCreditsText.text = $"Credits Earned: ${gm.Credits}";
        });
        gm.OnGameLose.AddListener(() => losePanel?.SetActive(true));

        // Buttons
        pauseButton?.onClick.AddListener(() =>
        {
            if (gm.CurrentState == GameManager.GameState.Playing) gm.PauseGame();
            else gm.ResumeGame();
        });
        emergencyBuyButton?.onClick.AddListener(() => rm.EmergencyBuyMaterials());

        // Initial state
        winPanel ?.SetActive(false);
        losePanel?.SetActive(false);
        UpdateTierUI(1);
        UpdateProductionStatus();
    }

    // ── Update Handlers ───────────────────────────────────────────────────
    private void UpdateTimer(float remaining)
    {
        if (!timerText) return;
        int m = Mathf.FloorToInt(remaining / 60f);
        int s = Mathf.FloorToInt(remaining % 60f);
        timerText.text  = $"{m:00}:{s:00}";
        timerText.color = remaining < 30f ? Color.red : Color.white;
        if (timerFillBar) timerFillBar.fillAmount = remaining / _totalTime;
    }

    private void UpdateResourceBar(TextMeshProUGUI label, Image bar, int value, int max, string prefix)
    {
        if (label) label.text = $"{prefix}{value}/{max}";
        if (bar)   bar.fillAmount = (float)value / max;
    }

    private void UpdateTierUI(int tier)
    {
        if (tierText) tierText.text = $"Pressure T{tier}";
        for (int i = 0; i < tierIndicators.Length; i++)
            if (tierIndicators[i])
                tierIndicators[i].color = i < tier ? Color.red : Color.gray;
    }

    private void UpdateProductionStatus()
    {
        var pm = ProductionManager.Instance;
        if (processAStatusText)
            processAStatusText.text = pm.IsProcessABlocked ? "A: BLOCKED"
                                    : pm.IsProcessARunning ? "A: Running…"
                                    :                        "A: Idle";
        if (processBStatusText)
            processBStatusText.text = pm.IsProcessBBlocked ? "B: BLOCKED"
                                    : pm.IsProcessBRunning ? "B: Running…"
                                    :                        "B: Idle";
    }

    // ── Critical Flash ────────────────────────────────────────────────────────
    private IEnumerator FlashBars()
    {
        Color warn = new Color(0.95f, 0.20f, 0.20f);
        for (int i = 0; i < 4; i++)
        {
            if (woodFillBar)  woodFillBar.color  = warn;
            if (paintFillBar) paintFillBar.color = warn;
            yield return new WaitForSecondsRealtime(0.18f);
            if (woodFillBar)  woodFillBar.color  = new Color(0.80f, 0.95f, 0.55f);
            if (paintFillBar) paintFillBar.color = new Color(0.95f, 0.65f, 0.90f);
            yield return new WaitForSecondsRealtime(0.18f);
        }
    }

    public void ToggleHUD()
    {
        _hudHidden = !_hudHidden;
        if (tabRow) tabRow.SetActive(!_hudHidden);
        if (hudToggleText) hudToggleText.text = _hudHidden ? "[ + ]" : "[ - ]";
    }
}
