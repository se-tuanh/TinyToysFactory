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
        var allMachines = GameObject.FindObjectsByType<Machine>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var allMachines = GameObject.FindObjectsOfType<Machine>(true);
#endif
        Debug.Log($"[UpgradeShop] Found {allMachines.Length} total machines to use as potential templates.");
        var assemblyList = new List<Machine>();
        var paintList = new List<Machine>();
        var addedProdsA = new HashSet<ProductData>();
        var addedProdsB = new HashSet<ProductData>();

        foreach (var m in allMachines)
        {
            if (m.machineType == Machine.MachineType.AssemblyA)
            {
                if (m.assignedProduct != null && !addedProdsA.Contains(m.assignedProduct))
                {
                    assemblyList.Add(m);
                    addedProdsA.Add(m.assignedProduct);
                }
                else if (m.assignedProduct == null && assemblyList.Count == 0) // fallback for generic
                {
                    assemblyList.Add(m);
                }
            }
            else if (m.machineType == Machine.MachineType.PaintPackB)
            {
                if (m.assignedProduct != null && !addedProdsB.Contains(m.assignedProduct))
                {
                    paintList.Add(m);
                    addedProdsB.Add(m.assignedProduct);
                }
                else if (m.assignedProduct == null && paintList.Count == 0) // fallback
                {
                    paintList.Add(m);
                }
            }
        }
        _assemblyTemplates = assemblyList.ToArray();
        _paintTemplates = paintList.ToArray();

        // Clear and Wire Upgradable Buttons
        if (workerUpgradeBtn) { workerUpgradeBtn.onClick.RemoveAllListeners(); workerUpgradeBtn.onClick.AddListener(BuyWorker); }
        if (bufferUpgradeBtn) { bufferUpgradeBtn.onClick.RemoveAllListeners(); bufferUpgradeBtn.onClick.AddListener(BuyBuffer); }
        if (powerRestoreBtn)  { powerRestoreBtn.onClick.RemoveAllListeners();  powerRestoreBtn.onClick.AddListener(BuyPower); }
        
        // Clear and Loop through provided Machine buttons
        for (int i = 0; i < assemblyButtons.Length; i++)
        {
            if (assemblyButtons[i] == null) continue;
            assemblyButtons[i].onClick.RemoveAllListeners();
            if (i >= _assemblyTemplates.Length) continue;
            
            Machine captured = _assemblyTemplates[i];
            assemblyButtons[i].onClick.AddListener(() => BuyMachine(captured));
        }
        for (int i = 0; i < paintButtons.Length; i++)
        {
            if (paintButtons[i] == null) continue;
            paintButtons[i].onClick.RemoveAllListeners();
            if (i >= _paintTemplates.Length) continue;
            
            Machine captured = _paintTemplates[i];
            paintButtons[i].onClick.AddListener(() => BuyMachine(captured));
        }

        // Refresh affordability whenever credits change
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCreditsChanged.AddListener(_ => RefreshButtons());
            RefreshButtons();
        }
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
        if (pm == null || pm.GetMachineStatus() == null) return 9999;
        int count = pm.GetMachineStatus().FindAll(m => m != null && m.machineType == type).Count;
        return (type == Machine.MachineType.AssemblyA ? baseAssemblyCost : basePaintCost) + (count * 50);
    }

    private void BuyMachine(Machine template)
    {
        Debug.Log($"[UpgradeShop] Trying to buy machine for template: {(template != null ? template.name : "NULL")}");
        if (template == null) {
            Debug.LogError("[UpgradeShop] Cannot buy machine: template is null!");
            return;
        }
        int cost = GetMachineCost(template.machineType);
        if (GameManager.Instance == null) { Debug.LogError("GameManager.Instance is null!"); return; }
        
        if (!GameManager.Instance.SpendCredits(cost)) {
            Debug.LogWarning($"[UpgradeShop] Not enough credits to buy {template.name}. Need {cost}");
            return;
        }
        
        SpawnMachine(template, template.machineType);
        RefreshButtons();
    }

    private void SpawnMachine(Machine template, Machine.MachineType type)
    {
        var pm = ProductionManager.Instance;
        if (pm == null || pm.GetMachineStatus() == null) {
            Debug.LogError("[UpgradeShop] ProductionManager not ready!");
            return;
        }

        // 1. Find the lowest Y among existing ACTIVE machines of the same type to stack below them
        // If none are active, use default column positions from SceneBuilder.
        var allMachines = pm.GetMachineStatus();
        float startX = (type == Machine.MachineType.AssemblyA) ? -6.5f : -0.5f;
        float lowestY = (type == Machine.MachineType.AssemblyA) ? 2.5f : -2.5f;
        int activeInCol = 0;

        foreach (var m in allMachines)
        {
            if (m != null && m.gameObject.activeInHierarchy && m.machineType == type)
            {
                if (m.transform.position.y < lowestY) lowestY = m.transform.position.y;
                activeInCol++;
            }
        }

        if (template == null || template.gameObject == null) return;
        
        // 2. Instantiate and Activate
        var newObj = Instantiate(template.gameObject, template.transform.parent);
        newObj.SetActive(true); // CRITICAL: Templates for Robot/Doll are inactive!
        
        // 3. Position below the lowest active machine in the column
        float spawnY = (activeInCol > 0) ? (lowestY - 1.35f) : lowestY;
        newObj.transform.position = new Vector3(startX, spawnY, 0);
        newObj.name = $"{template.gameObject.name}_Purchased_{activeInCol + 1}";
        
        var newMachine = newObj.GetComponent<Machine>();
        // Ensure new machine starts fresh without inherited worker state if any
        if (newMachine != null && newMachine.HasWorker) newMachine.ToggleWorker();
        
        Debug.Log($"[UpgradeShop] Purchased and Activated new {type} machine: {newObj.name} at {newObj.transform.position}");
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
        SetLabel(powerRestoreLabel,  $"Power +{powerRestoreAmount}\n${powerRestoreCost}");

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
            
            string prodName = (template != null && template.assignedProduct != null) ? template.assignedProduct.productName : "Machine";
            if (assemblyLabels != null && i < assemblyLabels.Length && assemblyLabels[i] != null) 
                SetLabel(assemblyLabels[i], $"+1 Ráp {prodName}\n${cost}");
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
            
            string prodName = (template != null && template.assignedProduct != null) ? template.assignedProduct.productName : "Machine";
            if (paintLabels != null && i < paintLabels.Length && paintLabels[i] != null) 
                SetLabel(paintLabels[i], $"+1 Sơn {prodName}\n${cost}");
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
