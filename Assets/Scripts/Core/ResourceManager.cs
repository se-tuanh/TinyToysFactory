using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ResourceManager — Tracks Materials, Power, and Workers.
/// Subscribe to events to react when resources change.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // ── Config ───────────────────────────────────────────────────────────
    [Header("Materials")]
    public int maxWoodPlastic = 100;
    public int maxPaintFabric = 100;
    public int restockAmount = 30;
    public float autoRestockInterval = 60f; // seconds
    public int emergencyBuyCost = 50;
    public int emergencyBuyAmount = 25;

    [Header("Power")]
    public int maxPower = 100;

    [Header("Workers")]
    public int maxWorkers = 3;

    [Header("Product Inventory")]
    public int maxInventoryCapacity = 50;

    // ── State (read-only externally) ─────────────────────────────────────
    public int WoodPlastic { get; private set; }
    public int PaintFabric { get; private set; }
    public int Power { get; private set; }
    public int AvailableWorkers { get; private set; }

    private System.Collections.Generic.Dictionary<ProductData, int> _inventory = new System.Collections.Generic.Dictionary<ProductData, int>();

    // ── Events ───────────────────────────────────────────────────────────
    public UnityEvent<int> OnWoodPlasticChanged;
    public UnityEvent<int> OnPaintFabricChanged;
    public UnityEvent<int> OnPowerChanged;
    public UnityEvent<int> OnWorkersChanged;
    public UnityEvent OnMaterialCritical;   // < 20% threshold
    public UnityEvent OnPowerCritical;
    public UnityEvent<ProductData, int> OnInventoryChanged;

    private float _restockTimer;

    // ── Lifecycle ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Start with half stock
        WoodPlastic = maxWoodPlastic / 2;
        PaintFabric = maxPaintFabric / 2;
        Power = maxPower;
        AvailableWorkers = maxWorkers;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        _restockTimer += Time.deltaTime;
        if (_restockTimer >= autoRestockInterval)
        {
            _restockTimer = 0f;
            AutoRestock();
        }
    }

    // ── Materials ────────────────────────────────────────────────────────
    public bool ConsumeWoodPlastic(int amount)
    {
        if (WoodPlastic < amount) return false;
        WoodPlastic -= amount;
        OnWoodPlasticChanged?.Invoke(WoodPlastic);
        CheckMaterialCritical();
        return true;
    }

    public bool ConsumePaintFabric(int amount)
    {
        if (PaintFabric < amount) return false;
        PaintFabric -= amount;
        OnPaintFabricChanged?.Invoke(PaintFabric);
        CheckMaterialCritical();
        return true;
    }

    public void AddWoodPlastic(int amount)
    {
        WoodPlastic = Mathf.Min(WoodPlastic + amount, maxWoodPlastic);
        OnWoodPlasticChanged?.Invoke(WoodPlastic);
    }

    public void AddPaintFabric(int amount)
    {
        PaintFabric = Mathf.Min(PaintFabric + amount, maxPaintFabric);
        OnPaintFabricChanged?.Invoke(PaintFabric);
    }

    /// <summary>Emergency buy: spend credits for instant stock top-up.</summary>
    public bool EmergencyBuyMaterials()
    {
        if (!GameManager.Instance.SpendCredits(emergencyBuyCost)) return false;
        AddWoodPlastic(emergencyBuyAmount);
        AddPaintFabric(emergencyBuyAmount);
        Debug.Log("[ResourceManager] Emergency restock purchased.");
        return true;
    }

    // ── Power ────────────────────────────────────────────────────────────
    public bool ConsumePower(int amount)
    {
        if (Power < amount) return false;
        Power -= amount;
        OnPowerChanged?.Invoke(Power);
        if ((float)Power / maxPower < 0.2f) OnPowerCritical?.Invoke();
        return true;
    }

    public void RestorePower(int amount)
    {
        Power = Mathf.Min(Power + amount, maxPower);
        OnPowerChanged?.Invoke(Power);
    }

    // ── Workers ──────────────────────────────────────────────────────────
    public bool AssignWorker()
    {
        if (AvailableWorkers <= 0) return false;
        AvailableWorkers--;
        OnWorkersChanged?.Invoke(AvailableWorkers);
        return true;
    }

    public void ReleaseWorker()
    {
        AvailableWorkers = Mathf.Min(AvailableWorkers + 1, maxWorkers);
        OnWorkersChanged?.Invoke(AvailableWorkers);
    }

    public void UpgradeWorkerSlot()
    {
        maxWorkers++;
        AvailableWorkers++;
        OnWorkersChanged?.Invoke(AvailableWorkers);
    }

    // ── Product Inventory ────────────────────────────────────────────────
    public bool IsInventoryFull()
    {
        int total = 0;
        foreach (var kvp in _inventory) total += kvp.Value;
        return total >= maxInventoryCapacity;
    }

    public void AddProduct(ProductData product, int amount)
    {
        if (product == null || amount <= 0) return;
        
        if (IsInventoryFull())
        {
            Debug.LogWarning("[ResourceManager] Inventory full! Cannot add product.");
            return;
        }

        int current = _inventory.ContainsKey(product) ? _inventory[product] : 0;
        
        // Prevent exceeding total capacity
        int total = 0;
        foreach (var kvp in _inventory) total += kvp.Value;
        int maxCanAdd = maxInventoryCapacity - total;
        int actualAdd = Mathf.Min(amount, maxCanAdd);

        _inventory[product] = current + actualAdd;
        OnInventoryChanged?.Invoke(product, _inventory[product]);
    }

    public bool HasProduct(ProductData product, int amount)
    {
        return _inventory.ContainsKey(product) && _inventory[product] >= amount;
    }

    public int GetProductCount(ProductData product)
    {
        return _inventory.ContainsKey(product) ? _inventory[product] : 0;
    }

    public bool RemoveProduct(ProductData product, int amount)
    {
        if (!HasProduct(product, amount)) return false;
        
        _inventory[product] -= amount;
        OnInventoryChanged?.Invoke(product, _inventory[product]);
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private void AutoRestock()
    {
        AddWoodPlastic(restockAmount);
        AddPaintFabric(restockAmount);
        Debug.Log("[ResourceManager] Auto-restock delivered.");
    }

    private void CheckMaterialCritical()
    {
        float woodRatio = (float)WoodPlastic / maxWoodPlastic;
        float paintRatio = (float)PaintFabric / maxPaintFabric;
        if (woodRatio < 0.2f || paintRatio < 0.2f)
            OnMaterialCritical?.Invoke();
    }

    public float GetLowestResourceRatio()
    {
        return Mathf.Min(
            (float)WoodPlastic / maxWoodPlastic,
            (float)PaintFabric / maxPaintFabric,
            (float)Power / maxPower,
            (float)AvailableWorkers / maxWorkers
        );
    }
}
