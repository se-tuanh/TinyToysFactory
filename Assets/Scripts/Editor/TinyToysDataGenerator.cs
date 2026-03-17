#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// TinyToysDataGenerator — Creates starter ScriptableObject assets under Assets/ScriptableObjects/.
/// Run via: TinyToys ▶ Generate Starter Data
/// </summary>
public class TinyToysDataGenerator : EditorWindow
{
    [MenuItem("TinyToys / Generate Starter Data")]
    public static void GenerateData()
    {
        CreateFolderStructure();

        // ── 1. Products ───────────────────────────────────────────────────
        // CreateProduct(fileName, productName, woodCost, paintCost, assemblyTime, paintTime,
        //               powerPerAssembly, powerPerPaint, baseCredits, repValue, tier)
        ProductData toyCar = CreateProduct(
            "Product_ToyCar",  "Toy Car",      10, 8,  8f,  6f,  30, 20, 50,  5, ProductTier.Simple);
        ProductData robot = CreateProduct(
            "Product_Robot",   "Robot",        15, 12, 12f, 10f, 40, 30, 90,  8, ProductTier.Medium);
        ProductData doll = CreateProduct(
            "Product_Doll",    "Luxury Doll",  20, 18, 18f, 14f, 50, 40, 140, 12, ProductTier.Premium);

        // ── 2. Orders ────────────────────────────────────────────────────
        CreateOrder("Order_ToyKingdom1", "Toy Kingdom Rush",  ClientFaction.ToyKingdom,  120f,
            new[] { new ProductRequirement { product = toyCar, quantity = 3 } },
            credits: 200, rep: 10, bonus: 50, tier: 1);

        CreateOrder("Order_Prestige1", "Prestige Collection", ClientFaction.PrestigyPlay, 150f,
            new[] { new ProductRequirement { product = robot, quantity = 2 } },
            credits: 300, rep: 15, bonus: 80, tier: 2);

        CreateOrder("Order_FlashDeal1", "Flash Bundle",        ClientFaction.FlashDeals,  90f,
            new[]
            {
                new ProductRequirement { product = doll,   quantity = 1 },
                new ProductRequirement { product = toyCar, quantity = 2 }
            },
            credits: 250, rep: 8, bonus: 60, tier: 1);

        // ── 3. Random Events ─────────────────────────────────────────────
        CreateEvent("Event_PowerSurge",       "Power Surge",
            "A sudden surge damaged our generators!",   EventType.PowerSurge,       tier: 1);
        CreateEvent("Event_MaterialShortage", "Material Shortage",
            "Supply chain issues! Materials are low.",  EventType.MaterialShortage, tier: 1);
        CreateEvent("Event_MachineBreakdown", "Machine Breakdown",
            "A core machine has jammed, output halted!", EventType.MachineBreakdown, tier: 2);
        CreateEvent("Event_BulkOrderSpike",   "Bulk Order Spike",
            "Surprise order! Extra products needed fast.", EventType.BulkOrderSpike, tier: 3);
        CreateEvent("Event_WorkerExhaustion", "Worker Exhaustion",
            "The team is tired. Production slowing down.", EventType.WorkerExhaustion, tier: 2);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TinyToysDataGenerator] Starter data generated in Assets/ScriptableObjects/");
    }

    // ── Folder Setup ──────────────────────────────────────────────────────
    private static void CreateFolderStructure()
    {
        EnsureFolder("Assets", "ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects", "Products");
        EnsureFolder("Assets/ScriptableObjects", "Orders");
        EnsureFolder("Assets/ScriptableObjects", "Events");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    // ── Factory Methods ───────────────────────────────────────────────────
    private static ProductData CreateProduct(
        string fileName, string prodName,
        int woodCost, int paintCost,
        float assemblyTime, float paintTime,
        int powerA, int powerB,
        int baseCredits, int repValue,
        ProductTier tier)
    {
        var inst = ScriptableObject.CreateInstance<ProductData>();
        inst.productName      = prodName;
        inst.tier             = tier;
        inst.woodPlasticCost  = woodCost;
        inst.paintFabricCost  = paintCost;
        inst.assemblyTime     = assemblyTime;
        inst.paintPackTime    = paintTime;
        inst.powerPerAssembly = powerA;
        inst.powerPerPaint    = powerB;
        inst.baseCredits      = baseCredits;
        inst.reputationValue  = repValue;

        AssetDatabase.CreateAsset(inst, $"Assets/ScriptableObjects/Products/{fileName}.asset");
        return inst;
    }

    private static void CreateOrder(
        string fileName, string orderName,
        ClientFaction faction, float deadline,
        ProductRequirement[] reqs,
        int credits, int rep, int bonus, int tier)
    {
        var inst = ScriptableObject.CreateInstance<OrderData>();
        inst.orderName           = orderName;
        inst.faction             = faction;
        inst.baseDeadline        = deadline;
        inst.requiredProducts    = reqs;
        inst.creditsReward       = credits;
        inst.reputationReward    = rep;
        inst.bonusCreditsIfEarly = bonus;
        inst.minPressureTier     = tier;

        AssetDatabase.CreateAsset(inst, $"Assets/ScriptableObjects/Orders/{fileName}.asset");
    }

    private static void CreateEvent(
        string fileName, string evName, string desc,
        EventType type, int tier = 1,
        float telegraphDuration = 1.5f, float weight = 1f)
    {
        var inst = ScriptableObject.CreateInstance<RandomEventData>();
        inst.eventName                  = evName;
        inst.eventDescription           = desc;
        inst.eventType                  = type;
        inst.minPressureTier            = tier;
        inst.triggerProbabilityWeight   = weight;
        inst.telegraphDuration          = telegraphDuration;

        // Default Decision Triad
        inst.prioritize = new EventChoice
        {
            choiceLabel       = "Push Through",
            choiceDescription = "Keep going — risk some damage.",
            reputationChange  = -5,
            failChance        = 0.3f
        };
        inst.repair = new EventChoice
        {
            choiceLabel            = "Stop & Repair",
            choiceDescription      = "Fix it properly.",
            productionStopDuration = 10f,
            reputationChange       = 5
        };
        inst.trade = new EventChoice
        {
            choiceLabel       = "Spend Credits",
            choiceDescription = "Pay to resolve instantly.",
            creditCost        = 50,
            reputationChange  = 0
        };

        AssetDatabase.CreateAsset(inst, $"Assets/ScriptableObjects/Events/{fileName}.asset");
    }
}
#endif
