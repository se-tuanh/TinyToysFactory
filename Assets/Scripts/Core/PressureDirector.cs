using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PressureDirector — Manages the 5-tier pressure system.
/// Tier increases on late orders / low reputation and decreases on streaks of good deliveries.
/// Triggers random events at intervals based on current tier.
/// Max 2 simultaneous events; cooldown after each resolution.
/// </summary>
public class PressureDirector : MonoBehaviour
{
    public static PressureDirector Instance { get; private set; }

    [Header("Config (assign GameplayConfig SO)")]
    public GameplayConfig config;

    [Header("Event Pool")]
    public List<RandomEventData> eventPool;

    // ── Events ────────────────────────────────────────────────────────────
    public UnityEvent<int>    OnTierChanged;      // new tier (1-5)
    public UnityEvent<RandomEventData> OnEventTriggered;
    public UnityEvent<RandomEventData, EventChoice, bool> OnEventResolved; // event, choice, success

    // ── State ─────────────────────────────────────────────────────────────
    public  int CurrentTier { get; private set; } = 1;
    private int _activeEventCount    = 0;
    private int _consecutiveGood     = 0;
    private int _lateOrderCount      = 0;
    private float _nextEventTimer;
    private bool  _graceActive;
    private const int MIN_TIER = 1;
    private const int MAX_TIER = 5;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        SetTier(1);
        GameManager.Instance.OnGameLose.AddListener(() => enabled = false);
        GameManager.Instance.OnGameWin .AddListener(() => enabled = false);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (_graceActive) return;

        _nextEventTimer -= Time.deltaTime;
        if (_nextEventTimer <= 0f) TriggerRandomEvent();
    }

    // ── Tier callbacks (called by OrderManager) ───────────────────────────
    public void RegisterLateOrder()
    {
        _lateOrderCount++;
        _consecutiveGood = 0;
        int threshold = config ? config.lateOrdersToTierUp : 2;
        if (_lateOrderCount >= threshold)
        {
            _lateOrderCount = 0;
            TierUp();
        }
    }

    public void RegisterGoodDelivery()
    {
        _consecutiveGood++;
        _lateOrderCount = Mathf.Max(0, _lateOrderCount - 1); // partial forgive
        int threshold = config ? config.goodDeliveriesForTierDown : 3;
        if (_consecutiveGood >= threshold)
        {
            _consecutiveGood = 0;
            TierDown();
        }
    }

    public void CheckReputationTierUp(int currentRep)
    {
        int threshold = config ? config.repThresholdForTierUp : 50;
        if (currentRep < threshold) TierUp();
    }

    // ── Event resolution ──────────────────────────────────────────────────
    /// <summary>Called when player picks a choice from EventPopupUI.</summary>
    public void ResolveEvent(RandomEventData ev, EventChoice choice)
    {
        // Apply credit cost
        if (choice.creditCost > 0) GameManager.Instance.AddCredits(-choice.creditCost);
        // Apply rep change
        if (choice.reputationChange != 0) GameManager.Instance.AddReputation(choice.reputationChange);
        // Apply production stop
        if (choice.productionStopDuration > 0) StartCoroutine(StopProductionFor(choice.productionStopDuration));

        // Roll fail chance
        float failRoll = Random.value;
        bool failed = choice.failChance > 0f && failRoll < choice.failChance;
        Debug.Log($"[PressureDirector] Resolved '{ev.eventName}' via '{choice.choiceLabel}' — " +
                  $"rollResult={failRoll:F2} vs failChance={choice.failChance:F2} → {(failed ? "FAILED" : "SUCCESS")}");

        if (failed)
        {
            // Apply fail consequence
            if (choice.failConsequencePenalty != 0)
                GameManager.Instance.AddCredits(choice.failConsequencePenalty); // negative = lose credits
            GameManager.Instance.AddReputation(-5); // extra rep hit on failure
            Debug.LogWarning($"[PressureDirector] Choice failed! Penalty applied.");
        }

        _activeEventCount = Mathf.Max(0, _activeEventCount - 1);
        OnEventResolved?.Invoke(ev, choice, !failed);

        // Cooldown before next event
        float cooldown = config ? config.eventCooldownAfterResolve : 5f;
        StartCoroutine(EventCooldown(cooldown));
    }

    // ── Internal ──────────────────────────────────────────────────────────
    private void TierUp()
    {
        if (CurrentTier >= MAX_TIER) return;
        SetTier(CurrentTier + 1);
        StartCoroutine(GracePeriod(config ? config.graceAfterTierUp : 10f));
    }

    private void TierDown()
    {
        if (CurrentTier <= MIN_TIER) return;
        SetTier(CurrentTier - 1);
    }

    private void SetTier(int t)
    {
        CurrentTier = Mathf.Clamp(t, MIN_TIER, MAX_TIER);
        _nextEventTimer = IntervalForTier(CurrentTier);
        OnTierChanged?.Invoke(CurrentTier);
        Debug.Log($"[PressureDirector] Tier → {CurrentTier}");
    }

    private void TriggerRandomEvent()
    {
        int maxEvents = config ? config.maxConcurrentEvents : 2;
        if (_activeEventCount >= maxEvents) { _nextEventTimer = 10f; return; }

        // Filter events eligible for current tier
        var eligible = eventPool.FindAll(e => e.minPressureTier <= CurrentTier);
        if (eligible.Count == 0) { _nextEventTimer = IntervalForTier(CurrentTier); return; }

        // Weighted random pick
        float totalWeight = 0f;
        foreach (var e in eligible) totalWeight += Mathf.Max(e.triggerProbabilityWeight, 0.01f);
        float roll = Random.value * totalWeight;
        RandomEventData chosen = eligible[0];
        foreach (var e in eligible)
        {
            roll -= Mathf.Max(e.triggerProbabilityWeight, 0.01f);
            if (roll <= 0f) { chosen = e; break; }
        }

        _activeEventCount++;
        _nextEventTimer = IntervalForTier(CurrentTier);
        OnEventTriggered?.Invoke(chosen);
        Debug.Log($"[PressureDirector] Event triggered: {chosen.eventName} (Tier {CurrentTier})");

        // Play telegraph if needed
        if (chosen.telegraphDuration > 0f) StartCoroutine(TelegraphDelay(chosen));
    }

    private IEnumerator TelegraphDelay(RandomEventData ev)
    {
        yield return new WaitForSeconds(ev.telegraphDuration);
        // Second fire (with telegraph flag) handled by EventPopupUI listener
    }

    private IEnumerator GracePeriod(float seconds)
    {
        _graceActive = true;
        yield return new WaitForSeconds(seconds);
        _graceActive = false;
    }

    private IEnumerator EventCooldown(float seconds)
    {
        _graceActive = true;
        yield return new WaitForSeconds(seconds);
        _graceActive = false;
        _nextEventTimer = IntervalForTier(CurrentTier);
    }

    private IEnumerator StopProductionFor(float seconds)
    {
        ProductionManager.Instance?.BlockAll(true);
        yield return new WaitForSeconds(seconds);
        ProductionManager.Instance?.BlockAll(false);
    }

    private float IntervalForTier(int tier)
    {
        if (!config) return 40f - (tier - 1) * 8f;
        return tier switch
        {
            1 => config.tier1Interval,
            2 => config.tier2Interval,
            3 => config.tier3Interval,
            4 => config.tier4Interval,
            _ => config.tier5Interval,
        };
    }
}
