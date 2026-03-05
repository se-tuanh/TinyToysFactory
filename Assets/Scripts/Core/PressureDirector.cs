using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PressureDirector — Controls difficulty tier (1-5) based on time elapsed
/// and schedules random events. The "Heat Director" equivalent for this game.
/// </summary>
public class PressureDirector : MonoBehaviour
{
    public static PressureDirector Instance { get; private set; }

    [Header("Event Pool")]
    public List<RandomEventData> eventPool; // assign in Inspector

    [Header("Tier Thresholds (time elapsed in seconds)")]
    public float tier2At = 60f;
    public float tier3At = 120f;
    public float tier4At = 180f;
    public float tier5At = 240f;

    [Header("Event Intervals per Tier (seconds between events)")]
    public float tier1Interval = 60f;
    public float tier2Interval = 45f;
    public float tier3Interval = 30f;
    public float tier4Interval = 20f;
    public float tier5Interval = 12f;

    [Header("Grace period after tier-up (seconds — no event)")]
    public float graceAfterTierUp = 10f;

    // ── State ─────────────────────────────────────────────────────────────
    public int CurrentTier { get; private set; } = 1;
    public bool EventActive { get; private set; } = false;

    private float _timeElapsed = 0f;
    private float _nextEventTimer = 0f;
    private float _graceTimer = 0f;
    private bool  _inGrace = false;

    // ── Events ────────────────────────────────────────────────────────────
    public UnityEvent<int>             OnTierChanged;   // new tier value
    public UnityEvent<RandomEventData> OnEventTriggered;
    public UnityEvent                  OnEventResolved;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _nextEventTimer = GetCurrentInterval();
        GameManager.Instance.OnGameStateChanged.AddListener(OnGameStateChanged);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (EventActive) return;

        _timeElapsed += Time.deltaTime;
        UpdateTier();

        if (_inGrace)
        {
            _graceTimer -= Time.deltaTime;
            if (_graceTimer <= 0f) _inGrace = false;
            return;
        }

        _nextEventTimer -= Time.deltaTime;
        if (_nextEventTimer <= 0f)
        {
            TriggerRandomEvent();
            _nextEventTimer = GetCurrentInterval();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Call after player resolves an event (from EventPopupUI).</summary>
    public void ResolveEvent(EventChoice choice)
    {
        EventActive = false;

        // Apply choice effects
        if (choice.timeCost > 0)
            GameManager.Instance.AddReputation(0); // no time API yet — handled via game timer

        if (choice.creditCost > 0)
            GameManager.Instance.SpendCredits(choice.creditCost);

        if (choice.reputationChange != 0)
            GameManager.Instance.AddReputation(choice.reputationChange);

        if (choice.pressureTierChange != 0)
            ForceSetTier(Mathf.Clamp(CurrentTier + choice.pressureTierChange, 1, 5));

        if (choice.productionStopDuration > 0)
            StartCoroutine(BlockProductionFor(choice.productionStopDuration));

        OnEventResolved?.Invoke();
        Debug.Log($"[PressureDirector] Event resolved: {choice.choiceLabel}");
    }

    public void ForceSetTier(int tier)
    {
        int clamped = Mathf.Clamp(tier, 1, 5);
        if (clamped == CurrentTier) return;
        CurrentTier = clamped;
        _inGrace = true;
        _graceTimer = graceAfterTierUp;
        _nextEventTimer = GetCurrentInterval();
        OnTierChanged?.Invoke(CurrentTier);
        Debug.Log($"[PressureDirector] Tier → {CurrentTier}");
    }

    // ── Private ───────────────────────────────────────────────────────────
    private void UpdateTier()
    {
        int newTier = 1;
        if      (_timeElapsed >= tier5At) newTier = 5;
        else if (_timeElapsed >= tier4At) newTier = 4;
        else if (_timeElapsed >= tier3At) newTier = 3;
        else if (_timeElapsed >= tier2At) newTier = 2;

        if (newTier != CurrentTier) ForceSetTier(newTier);
    }

    private void TriggerRandomEvent()
    {
        var eligible = eventPool.FindAll(e => e.minPressureTier <= CurrentTier);
        if (eligible.Count == 0) return;

        // Weighted random selection
        float totalWeight = 0f;
        foreach (var e in eligible) totalWeight += e.triggerProbabilityWeight;
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        RandomEventData selected = eligible[0];
        foreach (var e in eligible)
        {
            cumulative += e.triggerProbabilityWeight;
            if (roll <= cumulative) { selected = e; break; }
        }

        EventActive = true;
        StartCoroutine(TelegraphThenFire(selected));
    }

    private IEnumerator TelegraphThenFire(RandomEventData evt)
    {
        yield return new WaitForSeconds(evt.telegraphDuration);
        OnEventTriggered?.Invoke(evt);
        Debug.Log($"[PressureDirector] Event fired: {evt.eventName}");
    }

    private IEnumerator BlockProductionFor(float duration)
    {
        ProductionManager.Instance.SetProcessABlocked(true);
        ProductionManager.Instance.SetProcessBBlocked(true);
        yield return new WaitForSeconds(duration);
        ProductionManager.Instance.SetProcessABlocked(false);
        ProductionManager.Instance.SetProcessBBlocked(false);
    }

    private float GetCurrentInterval() => CurrentTier switch
    {
        1 => tier1Interval,
        2 => tier2Interval,
        3 => tier3Interval,
        4 => tier4Interval,
        _ => tier5Interval
    };

    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing) _timeElapsed = 0f;
    }
}
