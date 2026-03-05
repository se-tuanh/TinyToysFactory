using UnityEngine;

/// <summary>
/// ScriptableObject defining a product type (Toy Car, Robot, Doll).
/// Create via: Assets > Create > TinyToysFactory > ProductData
/// </summary>
[CreateAssetMenu(fileName = "NewProduct", menuName = "TinyToysFactory/ProductData")]
public class ProductData : ScriptableObject
{
    [Header("Identity")]
    public string productName;
    public Sprite productIcon;
    public ProductTier tier; // Simple, Medium, Premium

    [Header("Quy trinh A — Assembly Costs")]
    public int woodPlasticCost;   // units of Wood/Plastic
    public float assemblyTime;    // seconds for Process A

    [Header("Quy trinh B — Paint & Pack Costs")]
    public int paintFabricCost;   // units of Paint/Fabric
    public float paintPackTime;   // seconds for Process B

    [Header("Power & Labour")]
    public int powerPerAssembly;  // power units consumed during A
    public int powerPerPaint;     // power units consumed during B
    public int workersRequired;   // workers needed to run batch

    [Header("Economy")]
    public int baseCredits;       // credits earned when shipped
    public int reputationValue;   // reputation gained per batch
}

public enum ProductTier
{
    Simple,   // Toy Car  — low cost, low reward
    Medium,   // Robot    — balanced
    Premium   // Doll     — high cost, high reward
}
