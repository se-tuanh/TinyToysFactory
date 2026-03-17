using UnityEngine;

/// <summary>
/// FactoryTestRunner — Bootstraps mock data and machines for quick in-Editor testing.
/// Attach to any GameObject in a test scene. All UI buttons are optional.
/// </summary>
public class FactoryTestRunner : MonoBehaviour
{
    [Header("UI Binding (optional)")]
    public UnityEngine.UI.Button addWoodButton;
    public UnityEngine.UI.Button addPowerButton;
    public UnityEngine.UI.Button startAssemblyButton;
    public UnityEngine.UI.Button startPaintButton;

    private ProductData _toyCar;
    private Machine     _assemblyMachine;
    private Machine     _paintMachine;

    private void Start()
    {
        // 1. Create mock ScriptableObject data at runtime
        _toyCar = ScriptableObject.CreateInstance<ProductData>();
        _toyCar.productName       = "Test Toy Car";
        _toyCar.tier              = ProductTier.Simple;
        _toyCar.woodPlasticCost   = 10;
        _toyCar.assemblyTime      = 2f;
        _toyCar.paintFabricCost   = 5;
        _toyCar.paintPackTime     = 2f;
        _toyCar.powerPerAssembly  = 10;
        _toyCar.powerPerPaint     = 10;
        _toyCar.workersRequired   = 1;

        // 2. Ensure Managers exist (guards for test scenes without scene builder)
        if (GameManager.Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        if (ResourceManager.Instance == null)
            new GameObject("ResourceManager").AddComponent<ResourceManager>();

        if (ProductionManager.Instance == null)
            new GameObject("ProductionManager").AddComponent<ProductionManager>();

        // 3. Create Machines
        var assemblyGO = new GameObject("Machine_Assembly");
        _assemblyMachine = assemblyGO.AddComponent<Machine>();
        _assemblyMachine.machineType      = Machine.MachineType.AssemblyA;
        _assemblyMachine.assignedProduct  = _toyCar;
        _assemblyMachine.batchQuantity    = 1;

        var paintGO = new GameObject("Machine_Paint");
        _paintMachine = paintGO.AddComponent<Machine>();
        _paintMachine.machineType     = Machine.MachineType.PaintPackB;
        _paintMachine.assignedProduct = _toyCar;
        _paintMachine.batchQuantity   = 1;

        // 4. Bind UI Buttons
        if (addWoodButton)       addWoodButton.onClick.AddListener(() => ResourceManager.Instance.AddWoodPlastic(50));
        if (addPowerButton)      addPowerButton.onClick.AddListener(() => ResourceManager.Instance.RestorePower(50));
        if (startAssemblyButton) startAssemblyButton.onClick.AddListener(() => _assemblyMachine.TryStartBatch());
        if (startPaintButton)    startPaintButton.onClick.AddListener(() => _paintMachine.TryStartBatch());

        Debug.Log("<color=green>[FactoryTestRunner] Mock data and Machines initialized!</color>");
    }
}
