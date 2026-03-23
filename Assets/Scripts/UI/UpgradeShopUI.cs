using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UpgradeShopUI — In-game upgrade panel toggled with Tab key or shopButton.
/// Three upgrades: extra worker, bigger WIP buffer, emergency power restore.
/// Buttons auto-disable when the player can't afford them.
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

    [Header("Upgrade: Expand Factory (Arrays)")]
    public Button[] assemblyButtons;
    public TextMeshProUGUI[] assemblyLabels;
    
    public Button[] paintButtons;
    public TextMeshProUGUI[] paintLabels;

    public int baseAssemblyCost = 250;
    public int basePaintCost = 350;

    private Machine[] _assemblyTemplates;
    private Machine[] _paintTemplates;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Start()
    {
        if (shopPanel) shopPanel.SetActive(false);
        if (shopButton) shopButton.onClick.AddListener(ToggleShop);

        // Find Templates (find all unique machines in scene)
#if UNITY_2022_2_OR_NEWER
        var allMachines = GameObject.FindObjectsByType<Machine>(FindObjectsSortMode.None);
#else
        var allMachines = GameObject.FindObjectsOfType<Machine>();
#endif
        var assemblyList = new List<Machine>();
        var paintList = new List<Machine>();
        var addedProdsA = new HashSet<ProductData>();
        var addedProdsP = new HashSet<ProductData>();

        foreach(var m in allMachines)
        {
            if (m.machineType == Machine.MachineType.AssemblyA && !addedProdsA.Contains(m.assignedProduct))
            {
                assemblyList.Add(m);
                addedProdsA.Add(m.assignedProduct);
            }
            else if (m.machineType == Machine.MachineType.PaintPackB && !addedProdsP.Contains(m.assignedProduct))
            {
                paintList.Add(m);
                addedProdsP.Add(m.assignedProduct);
            }
        }
        _assemblyTemplates = assemblyList.ToArray();
        _paintTemplates = paintList.ToArray();

        // Wire Upgradable Buttons
        if (workerUpgradeBtn) workerUpgradeBtn.onClick.AddListener(BuyWorker);
        if (bufferUpgradeBtn) bufferUpgradeBtn.onClick.AddListener(BuyBuffer);
        if (powerRestoreBtn)  powerRestoreBtn .onClick.AddListener(BuyPower);
        
        // Loop through provided Machine buttons and wire them
        for (int i = 0; i < assemblyButtons.Length; i++)
        {
            if (i >= _assemblyTemplates.Length || assemblyButtons[i] == null) continue;
            Machine captured = _assemblyTemplates[i];
            assemblyButtons[i].onClick.AddListener(() => BuyMachine(captured));
        }
        for (int i = 0; i < paintButtons.Length; i++)
        {
            if (i >= _paintTemplates.Length || paintButtons[i] == null) continue;
            Machine captured = _paintTemplates[i];
            paintButtons[i].onClick.AddListener(() => BuyMachine(captured));
        }

        // Refresh affordability whenever credits change
        if (GameManager.Instance != null)
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
        
        if (GameManager.Instance == null) return;
        if (next) GameManager.Instance.PauseGame();
        else      GameManager.Instance.ResumeGame();
    }

    // ── Purchases ────────────────────────────────────────────────────────────
    private void BuyWorker()
    {
        if (!GameManager.Instance.SpendCredits(workerUpgradeCost)) return;
        ResourceManager.Instance.UpgradeWorkerSlot();
        RefreshButtons();
    }

    private void BuyBuffer()
    {
        if (!GameManager.Instance.SpendCredits(bufferUpgradeCost)) return;
        ProductionManager.Instance.maxBufferSize += 2;
        RefreshButtons();
    }

    private void BuyPower()
    {
        if (!GameManager.Instance.SpendCredits(powerRestoreCost)) return;
        ResourceManager.Instance.RestorePower(powerRestoreAmount);
        RefreshButtons();
    }

    private int GetMachineCost(Machine.MachineType type)
    {
        var pm = ProductionManager.Instance;
        if (pm == null) return 9999;
        int count = pm.GetMachineStatus().FindAll(m => m.machineType == type).Count;
        return (type == Machine.MachineType.AssemblyA ? baseAssemblyCost : basePaintCost) + (count * 50);
    }

    private void BuyMachine(Machine template)
    {
        int cost = GetMachineCost(template.machineType);
        if (!GameManager.Instance.SpendCredits(cost)) return;
        SpawnMachine(template, template.machineType);
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
        if (newMachine != null && newMachine.HasWorker) newMachine.ToggleWorker();
        
        Debug.Log($"[UpgradeShop] Purchased new {type} machine!");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    public void RefreshButtons()
    {
        if (GameManager.Instance == null) return;
        int credits = GameManager.Instance.Credits;
        
        SetInteractable(workerUpgradeBtn, credits >= workerUpgradeCost);
        SetInteractable(bufferUpgradeBtn, credits >= bufferUpgradeCost);
        SetInteractable(powerRestoreBtn,  credits >= powerRestoreCost);
        
        SetLabel(workerUpgradeLabel, $"+1 Worker\n${workerUpgradeCost}");
        SetLabel(bufferUpgradeLabel, $"+2 Buffer\n${bufferUpgradeCost}");
        SetLabel(powerRestoreLabel,  $"⚡ Power +{powerRestoreAmount}\n${powerRestoreCost}");

        // Refresh Dynamic Assembly Buttons
        for (int i = 0; i < assemblyButtons.Length; i++)
        {
            if (assemblyButtons[i] == null) continue;
            
            if (i >= _assemblyTemplates.Length) {
                assemblyButtons[i].gameObject.SetActive(false);
                continue;
            }
            
            var template = _assemblyTemplates[i];
            int cost = GetMachineCost(Machine.MachineType.AssemblyA);
            SetInteractable(assemblyButtons[i], credits >= cost);
            
            string prodName = template.assignedProduct != null ? template.assignedProduct.productName : "Machine";
            if (i < assemblyLabels.Length) SetLabel(assemblyLabels[i], $"+1 Ráp {prodName}\n${cost}");
        }

        // Refresh Dynamic Paint Buttons
        for (int i = 0; i < paintButtons.Length; i++)
        {
            if (paintButtons[i] == null) continue;
            
            if (i >= _paintTemplates.Length) {
                paintButtons[i].gameObject.SetActive(false);
                continue;
            }
            
            var template = _paintTemplates[i];
            int cost = GetMachineCost(Machine.MachineType.PaintPackB);
            SetInteractable(paintButtons[i], credits >= cost);
            
            string prodName = template.assignedProduct != null ? template.assignedProduct.productName : "Machine";
            if (i < paintLabels.Length) SetLabel(paintLabels[i], $"+1 Sơn {prodName}\n${cost}");
        }
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
