using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UIManager — Master HUD controller. Connects all resource/state changes to UI elements.
/// Assign all references in Inspector.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Timer")]
    public TextMeshProUGUI timerText;
    public Image timerFillBar;

    [Header("Resources")]
    public TextMeshProUGUI woodPlasticText;
    public TextMeshProUGUI paintFabricText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI workerText;
    public Image woodFillBar;
    public Image paintFillBar;
    public Image powerFillBar;

    [Header("Economy")]
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI reputationText;

    [Header("Pressure Tier")]
    public TextMeshProUGUI tierText;
    public Image[] tierIndicators; // 5 images, light up per tier

    [Header("Production Status")]
    public TextMeshProUGUI processAStatusText;
    public TextMeshProUGUI processBStatusText;
    public TextMeshProUGUI bufferCountText;

    [Header("Order Progress")]
    public Transform orderProgressContainer; // parent for order progress rows
    public GameObject orderProgressRowPrefab;

    [Header("Win/Lose Panels")]
    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI winCreditsText;

    [Header("Buttons")]
    public Button pauseButton;
    public Button emergencyBuyButton;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Start()
    {
        var gm  = GameManager.Instance;
        var rm  = ResourceManager.Instance;
        var pm  = ProductionManager.Instance;
        var pd  = PressureDirector.Instance;

        // Timer
        gm.OnTimeChanged.AddListener(UpdateTimer);
        _totalTime = gm.sessionDuration;

        // Resources
        rm.OnWoodPlasticChanged.AddListener(v => UpdateResourceBar(woodPlasticText, woodFillBar, v, rm.maxWoodPlastic));
        rm.OnPaintFabricChanged.AddListener(v => UpdateResourceBar(paintFabricText, paintFillBar, v, rm.maxPaintFabric));
        rm.OnPowerChanged      .AddListener(v => UpdateResourceBar(powerText,       powerFillBar, v, rm.maxPower));
        rm.OnWorkersChanged    .AddListener(v => { if (workerText) workerText.text = $"Workers: {v}/{rm.maxWorkers}"; });

        // Economy
        gm.OnCreditsChanged   .AddListener(v => { if (creditsText)    creditsText.text    = $"${v}"; });
        gm.OnReputationChanged.AddListener(v => { if (reputationText) reputationText.text = $"Rep: {v}"; });

        // Pressure
        pd.OnTierChanged.AddListener(UpdateTierUI);

        // Production
        pm.OnBatchCompletedA.AddListener(_ => UpdateProductionStatus());
        pm.OnBatchCompletedB.AddListener(_ => UpdateProductionStatus());
        pm.OnBufferChanged  .AddListener(v => { if (bufferCountText) bufferCountText.text = $"Buffer: {v}/{pm.maxBufferSize}"; });

        // Win/Lose
        gm.OnGameWin .AddListener(() => { winPanel?.SetActive(true);  winCreditsText.text = $"Credits Earned: ${gm.Credits}"; });
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
    }

    // ── Update handlers ───────────────────────────────────────────────────
    private float _totalTime;

    private void UpdateTimer(float remaining)
    {
        if (!timerText) return;
        int m = Mathf.FloorToInt(remaining / 60f);
        int s = Mathf.FloorToInt(remaining % 60f);
        timerText.text = $"{m:00}:{s:00}";
        if (timerFillBar) timerFillBar.fillAmount = remaining / _totalTime;

        // Flash red when < 30s
        timerText.color = remaining < 30f ? Color.red : Color.white;
    }

    private void UpdateResourceBar(TextMeshProUGUI label, Image bar, int value, int max)
    {
        if (label) label.text = $"{value}/{max}";
        if (bar)   bar.fillAmount = (float)value / max;
    }

    private void UpdateTierUI(int tier)
    {
        if (tierText) tierText.text = $"Pressure T{tier}";
        for (int i = 0; i < tierIndicators.Length; i++)
        {
            if (tierIndicators[i])
                tierIndicators[i].color = i < tier ? Color.red : Color.gray;
        }
    }

    private void UpdateProductionStatus()
    {
        var pm = ProductionManager.Instance;
        if (processAStatusText)
            processAStatusText.text = pm.IsProcessABlocked ? "A: BLOCKED" : pm.IsProcessARunning ? "A: Running..." : "A: Idle";
        if (processBStatusText)
            processBStatusText.text = pm.IsProcessBBlocked ? "B: BLOCKED" : pm.IsProcessBRunning ? "B: Running..." : "B: Idle";
    }
}
