using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// GameManager — Singleton. Controls game state, global timer, win/lose.
/// Attach to a persistent GameObject in your main scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── State ────────────────────────────────────────────────────────────
    public enum GameState { MainMenu, Playing, Paused, Win, Lose }
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    [Header("Config (assign GameplayConfig SO)")]
    public GameplayConfig config;

    [Header("Session Config (used if no config SO)")]
    public float sessionDuration = 180f;
    public bool  autoStart       = true;
    public float TimeRemaining { get; private set; }

    [Header("Economy")]
    public int Credits { get; private set; } = 200;
    public int Reputation { get; private set; } = 100;
    public int reputationLoseThreshold = 0; // lose if reputation hits 0

    // ── Events (subscribe from UI / other systems) ────────────────────────
    public UnityEvent<GameState> OnGameStateChanged;
    public UnityEvent<float> OnTimeChanged;       // remaining time
    public UnityEvent<int> OnCreditsChanged;
    public UnityEvent<int> OnReputationChanged;
    public UnityEvent OnGameWin;
    public UnityEvent OnGameLose;

    // ── Lifecycle ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Auto-start for MVP testing — set autoStart = false to use MainMenuController trigger
        if (autoStart) StartGame();
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        TimeRemaining -= Time.deltaTime;
        OnTimeChanged?.Invoke(TimeRemaining);

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            TriggerLose("Time ran out!");
        }
    }

    // ── Public API ───────────────────────────────────────────────────────
    public void StartGame()
    {
        // Read from config SO if assigned
        TimeRemaining = config ? config.sessionDuration  : sessionDuration;
        Credits       = config ? config.startingCredits  : 200;
        Reputation    = config ? config.startingReputation : 100;
        ChangeState(GameState.Playing);
        Debug.Log("[GameManager] Game started");
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            Time.timeScale = 0f;
            ChangeState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            Time.timeScale = 1f;
            ChangeState(GameState.Playing);
        }
    }

    public void AddCredits(int amount)
    {
        Credits += amount;
        OnCreditsChanged?.Invoke(Credits);
    }

    public bool SpendCredits(int amount)
    {
        if (Credits < amount) return false;
        Credits -= amount;
        OnCreditsChanged?.Invoke(Credits);
        return true;
    }

    public void AddReputation(int amount)
    {
        Reputation = Mathf.Clamp(Reputation + amount, 0, 200);
        OnReputationChanged?.Invoke(Reputation);
        if (Reputation <= reputationLoseThreshold)
            TriggerLose("Reputation hit zero!");
    }

    public void TriggerWin()
    {
        ChangeState(GameState.Win);
        SaveSystem.SaveHighScore(Credits, Reputation);
        OnGameWin?.Invoke();
    }

    public void TriggerLose(string reason)
    {
        Debug.Log($"[GameManager] LOSE: {reason}");
        ChangeState(GameState.Lose);
        SaveSystem.SaveHighScore(Credits, Reputation); // save even on lose
        OnGameLose?.Invoke();
    }

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
}
