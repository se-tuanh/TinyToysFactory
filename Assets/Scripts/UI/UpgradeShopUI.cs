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

    [Header("Upgrade: Expand Factory")]
    public Button          buyAssemblyBtn;
    public TextMeshProUGUI buyAssemblyLabel;
    public int             baseAssemblyCost = 250;

    public Button          buyPaintBtn;
    public TextMeshProUGUI buyPaintLabel;
    public int             basePaintCost = 350;

    private Machine templateAssemblyMachine;
    private Machine templatePaintMachine;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Start()
    {
        if (shopPanel) shopPanel.SetActive(false);
        if (shopButton) shopButton.onClick.AddListener(ToggleShop);

        // Find Templates
        var ta = GameObject.Find("MachineA_Assembly");
        if (ta) templateAssemblyMachine = ta.GetComponent<Machine>();
        var tp = GameObject.Find("MachineB_Paint");
        if (tp) templatePaintMachine = tp.GetComponent<Machine>();

        // Wire buttons
        if (workerUpgradeBtn) workerUpgradeBtn.onClick.AddListener(BuyWorker);
        if (bufferUpgradeBtn) bufferUpgradeBtn.onClick.AddListener(BuyBuffer);
        if (powerRestoreBtn)  powerRestoreBtn .onClick.AddListener(BuyPower);
        if (buyAssemblyBtn)   buyAssemblyBtn  .onClick.AddListener(BuyAssembly);
        if (buyPaintBtn)      buyPaintBtn     .onClick.AddListener(BuyPaint);

        // Update labels
        string aProd = templateAssemblyMachine != null && templateAssemblyMachine.assignedProduct != null ? templateAssemblyMachine.assignedProduct.productName : "Assembly";
        string pProd = templatePaintMachine != null && templatePaintMachine.assignedProduct != null ? templatePaintMachine.assignedProduct.productName : "Paint";

        SetLabel(workerUpgradeLabel, $"+1 Worker\n${workerUpgradeCost}");
        SetLabel(bufferUpgradeLabel, $"+2 Buffer\n${bufferUpgradeCost}");
        SetLabel(powerRestoreLabel,  $"⚡ Power +{powerRestoreAmount}\n${powerRestoreCost}");
        SetLabel(buyAssemblyLabel,   $"+1 Ráp {aProd}\n${GetMachineCost(Machine.MachineType.AssemblyA)}");
        SetLabel(buyPaintLabel,      $"+1 Sơn {pProd}\n${GetMachineCost(Machine.MachineType.PaintPackB)}");

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

    private int GetMachineCost(Machine.MachineType type)
    {
        var pm = ProductionManager.Instance;
        if (pm == null) return 9999;
        int count = pm.GetMachineStatus().FindAll(m => m.machineType == type).Count;
        return (type == Machine.MachineType.AssemblyA ? baseAssemblyCost : basePaintCost) + (count * 50);
    }

    private void BuyAssembly()
    {
        int cost = GetMachineCost(Machine.MachineType.AssemblyA);
        if (!GameManager.Instance.SpendCredits(cost)) return;
        if (templateAssemblyMachine == null) return;

        SpawnMachine(templateAssemblyMachine, Machine.MachineType.AssemblyA);
        RefreshButtons();
    }

    private void BuyPaint()
    {
        int cost = GetMachineCost(Machine.MachineType.PaintPackB);
        if (!GameManager.Instance.SpendCredits(cost)) return;
        if (templatePaintMachine == null) return;

        SpawnMachine(templatePaintMachine, Machine.MachineType.PaintPackB);
        RefreshButtons();
    }

    private void SpawnMachine(Machine template, Machine.MachineType type)
    {
        var pm = ProductionManager.Instance;
        int count = pm.GetMachineStatus().FindAll(m => m.machineType == type).Count;
        
        var newObj = Instantiate(template.gameObject, template.transform.parent);
        newObj.transform.position = template.transform.position + new Vector3(0, -1.35f * count, 0);
        newObj.name = $"{template.gameObject.name}_{count + 1}";
        
        var newMachine = newObj.GetComponent<Machine>();
        if (newMachine.HasWorker) newMachine.ToggleWorker(); // Release copied worker flag without interacting with ResourceManager
        // The new machine's Start() will register it to ProductionManager automatically
        
        SetLabel(type == Machine.MachineType.AssemblyA ? buyAssemblyLabel : buyPaintLabel, 
                $"+1 {(type == Machine.MachineType.AssemblyA ? "Assembly" : "Paint")}\n${GetMachineCost(type)}");
        Debug.Log($"[UpgradeShop] Purchased new {type} machine!");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private void RefreshButtons()
    {
        int credits = GameManager.Instance.Credits;
        SetInteractable(workerUpgradeBtn, credits >= workerUpgradeCost);
        SetInteractable(bufferUpgradeBtn, credits >= bufferUpgradeCost);
        SetInteractable(powerRestoreBtn,  credits >= powerRestoreCost);
        
        SetInteractable(buyAssemblyBtn,   credits >= GetMachineCost(Machine.MachineType.AssemblyA));
        SetInteractable(buyPaintBtn,      credits >= GetMachineCost(Machine.MachineType.PaintPackB));
        
        string aProd = templateAssemblyMachine != null && templateAssemblyMachine.assignedProduct != null ? templateAssemblyMachine.assignedProduct.productName : "Assembly";
        string pProd = templatePaintMachine != null && templatePaintMachine.assignedProduct != null ? templatePaintMachine.assignedProduct.productName : "Paint";

        SetLabel(buyAssemblyLabel, $"+1 Ráp {aProd}\n${GetMachineCost(Machine.MachineType.AssemblyA)}");
        SetLabel(buyPaintLabel,    $"+1 Sơn {pProd}\n${GetMachineCost(Machine.MachineType.PaintPackB)}");
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
