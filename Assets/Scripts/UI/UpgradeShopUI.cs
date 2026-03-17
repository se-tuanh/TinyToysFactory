using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UpgradeShopUI — In-game upgrade panel toggled with Tab key or shopButton.
/// Three upgrades: extra worker, bigger WIP buffer, emergency power restore.
/// Buttons auto-disable when the player can't afford them.
/// Wire all fields in Inspector or let TinyToysSceneBuilder do it.
/// </summary>
public class UpgradeShopUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject shopPanel;  // root panel, hidden by default
    public Button     shopButton; // HUD toggle button (optional)

    [Header("Upgrade: +1 Worker")]
    public Button          workerUpgradeBtn;
    public TextMeshProUGUI workerUpgradeLabel;
    public int             workerUpgradeCost = 80;

    [Header("Upgrade: +2 Buffer Slots")]
    public Button          bufferUpgradeBtn;
    public TextMeshProUGUI bufferUpgradeLabel;
    public int             bufferUpgradeCost = 60;

    [Header("Upgrade: Emergency Power")]
    public Button          powerRestoreBtn;
    public TextMeshProUGUI powerRestoreLabel;
    public int             powerRestoreCost   = 40;
    public int             powerRestoreAmount = 50;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Start()
    {
        if (shopPanel) shopPanel.SetActive(false);
        if (shopButton) shopButton.onClick.AddListener(ToggleShop);

        // Wire buttons
        if (workerUpgradeBtn) workerUpgradeBtn.onClick.AddListener(BuyWorker);
        if (bufferUpgradeBtn) bufferUpgradeBtn.onClick.AddListener(BuyBuffer);
        if (powerRestoreBtn)  powerRestoreBtn .onClick.AddListener(BuyPower);

        // Update labels
        SetLabel(workerUpgradeLabel, $"+1 Worker\n${workerUpgradeCost}");
        SetLabel(bufferUpgradeLabel, $"+2 Buffer\n${bufferUpgradeCost}");
        SetLabel(powerRestoreLabel,  $"⚡ Power +{powerRestoreAmount}\n${powerRestoreCost}");

        // Refresh affordability whenever credits change
        GameManager.Instance.OnCreditsChanged.AddListener(_ => RefreshButtons());
        RefreshButtons();
    }

    private void Update()
    {
        // Toggle with Tab key
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleShop();
    }

    // ── Toggle ───────────────────────────────────────────────────────────────
    public void ToggleShop()
    {
        if (!shopPanel) return;
        bool next = !shopPanel.activeSelf;
        shopPanel.SetActive(next);
        // Pause while browsing upgrades
        if (next) GameManager.Instance.PauseGame();
        else      GameManager.Instance.ResumeGame();
    }

    // ── Purchases ────────────────────────────────────────────────────────────
    private void BuyWorker()
    {
        if (!GameManager.Instance.SpendCredits(workerUpgradeCost)) return;
        ResourceManager.Instance.UpgradeWorkerSlot();
        Debug.Log("[UpgradeShop] Bought extra worker slot.");
        RefreshButtons();
    }

    private void BuyBuffer()
    {
        if (!GameManager.Instance.SpendCredits(bufferUpgradeCost)) return;
        ProductionManager.Instance.maxBufferSize += 2;
        Debug.Log($"[UpgradeShop] Buffer expanded → {ProductionManager.Instance.maxBufferSize}");
        RefreshButtons();
    }

    private void BuyPower()
    {
        if (!GameManager.Instance.SpendCredits(powerRestoreCost)) return;
        ResourceManager.Instance.RestorePower(powerRestoreAmount);
        Debug.Log($"[UpgradeShop] Emergency power restored (+{powerRestoreAmount}).");
        RefreshButtons();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private void RefreshButtons()
    {
        int credits = GameManager.Instance.Credits;
        SetInteractable(workerUpgradeBtn, credits >= workerUpgradeCost);
        SetInteractable(bufferUpgradeBtn,  credits >= bufferUpgradeCost);
        SetInteractable(powerRestoreBtn,   credits >= powerRestoreCost);
    }

    private static void SetInteractable(Button btn, bool on)
    {
        if (btn) btn.interactable = on;
    }

    private static void SetLabel(TextMeshProUGUI lbl, string txt)
    {
        if (lbl) lbl.text = txt;
    }
}
