using UnityEngine;

/// <summary>
/// GameplayConfig — ScriptableObject holding all tuneable balance values.
/// Assigned to GameManager in Inspector. Change values without rebuilding.
/// Create via: Assets > Create > TinyToysFactory > GameplayConfig
/// </summary>
[CreateAssetMenu(fileName = "GameplayConfig", menuName = "TinyToysFactory/GameplayConfig")]
public class GameplayConfig : ScriptableObject
{
    [Header("Session")]
    public float sessionDuration      = 180f; // seconds per game
    public int   startingCredits      = 200;
    public int   startingReputation   = 100;
    public int   reputationLoseAt     = 0;

    [Header("Resources")]
    public int   startingWoodPlastic  = 50;
    public int   startingPaintFabric  = 50;
    public int   maxWoodPlastic       = 100;
    public int   maxPaintFabric       = 100;
    public int   maxPower             = 100;
    public int   maxWorkers           = 3;
    public int   maxInventory         = 50;
    public float autoRestockInterval  = 60f;
    public int   restockAmount        = 30;
    public int   emergencyBuyCost     = 50;
    public int   emergencyBuyAmount   = 25;

    [Header("Orders")]
    [Tooltip("Max simultaneous active orders")]
    public int   maxActiveOrders      = 3;
    [Tooltip("Reputation penalty when an order expires")]
    public int   orderExpireRepPenalty = 10;

    [Header("Pressure Director — Tier Thresholds (elapsed seconds)")]
    public float tier2At = 60f;
    public float tier3At = 120f;
    public float tier4At = 180f;
    public float tier5At = 240f;

    [Header("Pressure Director — Tier Up/Down Rules")]
    public int   lateOrdersToTierUp       = 2;   // expired orders before tier goes up
    public int   goodDeliveriesForTierDown = 3;   // consecutive good deliveries before tier goes down
    public int   repThresholdForTierUp     = 50;  // reputation below this triggers tier up

    [Header("Pressure Director — Event Intervals per Tier (seconds)")]
    public float tier1Interval = 60f;
    public float tier2Interval = 45f;
    public float tier3Interval = 30f;
    public float tier4Interval = 20f;
    public float tier5Interval = 12f;

    [Header("Events")]
    public float graceAfterTierUp      = 10f;
    public int   maxConcurrentEvents   = 2;
    public float eventCooldownAfterResolve = 5f;

    [Header("FlashDeals Bonus")]
    [Tooltip("Fraction of time remaining to count as 'early' delivery")]
    public float flashDealsEarlyThreshold = 0.30f;
    public float flashDealsEarlyBonusMult = 1.5f; // 50% extra credits

    [Header("PrestigyPlay Bonus")]
    public int   prestigyPlayRepBonus  = 5; // extra reputation per delivery

    [Header("ToyKingdom Bonus")]
    public int   toyKingdomBulkBonus   = 30; // extra credits if all items delivered at once
}
