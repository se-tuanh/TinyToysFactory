using UnityEngine;

/// <summary>
/// ScriptableObject defining a random event with 3-choice Decision Triad.
/// Create via: Assets > Create > TinyToysFactory > RandomEventData
/// </summary>
[CreateAssetMenu(fileName = "NewEvent", menuName = "TinyToysFactory/RandomEventData")]
public class RandomEventData : ScriptableObject
{
    [Header("Identity")]
    public string eventName;
    public string eventDescription;
    public Sprite eventIcon;
    public EventType eventType;

    [Header("Trigger")]
    public int minPressureTier = 1;
    public float triggerProbabilityWeight = 1f; // relative weight for random selection

    [Header("Decision Triad")]
    public EventChoice prioritize; // Push through
    public EventChoice repair;     // Fix the problem
    public EventChoice trade;      // Spend credits to resolve

    [Header("Telegraph")]
    public float telegraphDuration = 1.5f; // warning time before event fires
    public AudioClip telegraphSound;
    public AudioClip resolveSound;
}

[System.Serializable]
public struct EventChoice
{
    public string choiceLabel;        // e.g. "Push Through"
    public string choiceDescription;  // e.g. "Keep going, risk fatigue"

    [Header("Costs")]
    public float timeCost;            // seconds lost
    public int materialCost;
    public int creditCost;
    public int workerCost;            // workers tied up

    [Header("Effects")]
    public float productionStopDuration; // how long production halts
    public int reputationChange;
    public int pressureTierChange;    // +1 escalates, -1 de-escalates

    [Header("Risk")]
    [Range(0f, 1f)] public float failChance;         // chance choice backfires
    public int    failConsequencePenalty;            // extra credits lost on fail (negative = lose)
    public string failConsequenceDescription;
}

public enum EventType
{
    MachineBreakdown,    // Conveyor jam / machine halt
    MaterialShortage,    // Run out of raw materials
    BulkOrderSpike,      // Surprise extra order arrives
    PowerSurge,          // Electricity spike / cut
    WorkerExhaustion,    // Workers tired, slower output
    QualityInspection    // Inspector checks product quality (PrestigePlay faction)
}
