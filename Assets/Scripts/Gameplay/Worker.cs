using UnityEngine;

/// <summary>
/// Worker — Visual representation of a worker in the factory.
/// Moves between stations and shows fatigue state.
/// Drag-and-drop assignment is handled by WorkerAssignmentUI (future feature).
/// </summary>
public class Worker : MonoBehaviour
{
    [Header("Config")]
    public float fatigueThreshold = 90f; // seconds of continuous work before tired
    public float restDuration = 20f;

    [Header("Visuals")]
    public SpriteRenderer workerSprite;
    public Sprite normalSprite;
    public Sprite tiredSprite;
    public Sprite restingSprite;

    // ── State ─────────────────────────────────────────────────────────────
    public bool IsFatigued { get; private set; }
    public bool IsResting  { get; private set; }

    private float _workTimer = 0f;
    private float _restTimer = 0f;
    private bool  _assigned  = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (_assigned && !IsResting)
        {
            _workTimer += Time.deltaTime;

            // Apply fatigue penalty: slow production (communicated via ProductionManager multiplier in future)
            if (_workTimer >= fatigueThreshold && !IsFatigued)
            {
                IsFatigued = true;
                SetSprite(tiredSprite);
                Debug.Log($"[Worker] {name} is fatigued!");
            }
        }

        if (IsResting)
        {
            _restTimer -= Time.deltaTime;
            if (_restTimer <= 0f)
            {
                IsResting  = false;
                IsFatigued = false;
                _workTimer = 0f;
                SetSprite(normalSprite);
                ResourceManager.Instance.ReleaseWorker(); // back to pool
                Debug.Log($"[Worker] {name} rested and returned.");
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────
    public void AssignToStation()
    {
        if (IsResting) return;
        _assigned = true;
        SetSprite(IsFatigued ? tiredSprite : normalSprite);
    }

    public void UnassignFromStation()
    {
        _assigned = false;
    }

    /// <summary>Player chooses to rest this worker (costs production time, resets fatigue).</summary>
    public void SendToRest()
    {
        if (IsResting) return;
        IsResting  = true;
        _assigned  = false;
        _restTimer = restDuration;
        SetSprite(restingSprite);
        ResourceManager.Instance.AssignWorker(); // take from pool while resting
        Debug.Log($"[Worker] {name} sent to rest for {restDuration}s.");
    }

    private void SetSprite(Sprite s)
    {
        if (workerSprite && s) workerSprite.sprite = s;
    }
}
