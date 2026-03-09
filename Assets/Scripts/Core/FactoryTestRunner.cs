using UnityEngine;

/// <summary>
/// A simple script to automatically create test ProductData instances
/// and wire up the factory components so Tuấn Anh can test the logic
/// without waiting for the rest of the team's assets.
/// </summary>
public class FactoryTestRunner : MonoBehaviour
{
    [Header("UI Binding")]
    public UnityEngine.UI.Button addWoodButton;
    public UnityEngine.UI.Button addPowerButton;
    public UnityEngine.UI.Button startAssemblyButton;
    public UnityEngine.UI.Button startPaintButton;

    private ProductData _toyCar;
    private Machine _assemblyMachine;
    private Machine _paintMachine;

    private void Start()
    {
        // 1. Create Mock ScriptableObject Data
        _toyCar = ScriptableObject.CreateInstance<ProductData>();
        _toyCar.productName = "Test Toy Car";
        _toyCar.tier = ProductTier.Simple;
        _toyCar.woodPlasticCost = 10;
        _toyCar.assemblyTime = 2f;
        _toyCar.paintFabricCost = 5;
        _toyCar.paintPackTime = 2f;
        _toyCar.powerPerAssembly = 10;
        _toyCar.powerPerPaint = 10;
        _toyCar.workersRequired = 1;

        // 2. Setup Managers
        if (ResourceManager.Instance == null)
        {
            var rmGo = new GameObject("ResourceManager");
            rmGo.AddComponent<ResourceManager>();
        }

        if (ProductionManager.Instance == null)
        {
            var pmGo = new GameObject("ProductionManager");
            pmGo.AddComponent<ProductionManager>();
        }

        // 3. Setup Machines
        var amGo = new GameObject("Machine_Assembly");
        _assemblyMachine = amGo.AddComponent<Machine>();
        _assemblyMachine.machineType = Machine.MachineType.AssemblyA;
        _assemblyMachine.assignedProduct = _toyCar;
        _assemblyMachine.batchQuantity = 1;

        var pmGo2 = new GameObject("Machine_Paint");
        _paintMachine = pmGo2.AddComponent<Machine>();
        _paintMachine.machineType = Machine.MachineType.PaintPackB;
        _paintMachine.assignedProduct = _toyCar;
        _paintMachine.batchQuantity = 1;

        // 4. Bind UI (If provided)
        if (addWoodButton) addWoodButton.onClick.AddListener(() => ResourceManager.Instance.AddWoodPlastic(50));
        if (addPowerButton) addPowerButton.onClick.AddListener(() => ResourceManager.Instance.RestorePower(50));
        if (startAssemblyButton) startAssemblyButton.onClick.AddListener(() => _assemblyMachine.TryStartBatch());
        if (startPaintButton) startPaintButton.onClick.AddListener(() => _paintMachine.TryStartBatch());

        Debug.Log("<color=green>[FactoryTestRunner] Mock data and Machines initialized successfully!</color>");
        Debug.Log("Check the Inspector or UI buttons to test the flow.");
    }
}
