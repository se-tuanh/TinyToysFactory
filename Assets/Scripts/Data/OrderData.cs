using UnityEngine;

/// <summary>
/// ScriptableObject defining an order (client, required products, deadline).
/// Create via: Assets > Create > TinyToysFactory > OrderData
/// </summary>
[CreateAssetMenu(fileName = "NewOrder", menuName = "TinyToysFactory/OrderData")]
public class OrderData : ScriptableObject
{
    [Header("Client Info")]
    public string orderName;
    public ClientFaction faction;
    public Sprite clientIcon;

    [Header("Required Products")]
    public ProductRequirement[] requiredProducts;

    [Header("Time")]
    public float baseDeadline = 180f; // seconds (3:00 default)

    [Header("Rewards")]
    public int creditsReward;
    public int reputationReward;
    public int bonusCreditsIfEarly; // bonus if delivered with >20% time left

    [Header("Difficulty")]
    public int minPressureTier = 1; // minimum pressure tier to unlock this order
}

[System.Serializable]
public struct ProductRequirement
{
    public ProductData product;
    public int quantity;
}

public enum ClientFaction
{
    ToyKingdom,    // High volume, short deadlines, conveyor jam events
    PrestigyPlay,  // Quality checks, low volume, high reward
    FlashDeals     // Random bulk spikes, bonus if fast
}
