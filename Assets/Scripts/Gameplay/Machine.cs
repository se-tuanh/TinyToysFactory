using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Machine — Represents a single Assembly or Paint machine in the factory.
/// Can be clicked to start a production batch. Shows visual state (idle/running/blocked).
/// </summary>
public class Machine : MonoBehaviour
{
    public enum MachineType { AssemblyA, PaintPackB }

    [Header("Config")]
    public MachineType machineType;
    public ProductData assignedProduct; // set in Inspector or at runtime
    public int batchQuantity = 1;

    [Header("Visuals")]
    public SpriteRenderer statusLight; // green = idle, yellow = running, red = blocked
    public Animator machineAnimator;   // "Idle", "Running", "Broken" animation states
    public ParticleSystem dustParticles;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip runningSound;
    public AudioClip completeSound;
    public AudioClip blockedSound;

    // ── State ─────────────────────────────────────────────────────────────
    public bool IsRunning { get; private set; }
    public bool IsBlocked { get; private set; }

    public UnityEvent OnMachineStarted;
    public UnityEvent OnMachineCompleted;
    public UnityEvent OnMachineBlocked;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Start()
    {
        SetVisualState("Idle");

        // Subscribe to block events
        var pm = ProductionManager.Instance;
        pm.OnProductionABlocked.AddListener(() => { if (machineType == MachineType.AssemblyA)  SetBlocked(true); });
        pm.OnProductionBBlocked.AddListener(() => { if (machineType == MachineType.PaintPackB) SetBlocked(true); });
        pm.OnBatchCompletedA   .AddListener(_ => { if (machineType == MachineType.AssemblyA)  OnBatchDone(); });
        pm.OnBatchCompletedB   .AddListener(_ => { if (machineType == MachineType.PaintPackB) OnBatchDone(); });
    }

    // ── Interaction (click to start) ──────────────────────────────────────
    private void OnMouseDown()
    {
        TryStartBatch();
    }

    public bool TryStartBatch()
    {
        if (IsRunning || IsBlocked || assignedProduct == null) return false;

        bool started = false;
        if (machineType == MachineType.AssemblyA)
            started = ProductionManager.Instance.StartBatchA(assignedProduct, batchQuantity);

        if (started)
        {
            IsRunning = true;
            SetVisualState("Running");
            PlaySound(runningSound, loop: true);
            OnMachineStarted?.Invoke();
        }
        return started;
    }

    // ── Private ───────────────────────────────────────────────────────────
    private void OnBatchDone()
    {
        IsRunning = false;
        SetVisualState("Idle");
        StopSound();
        PlaySound(completeSound, loop: false);
        OnMachineCompleted?.Invoke();
    }

    private void SetBlocked(bool blocked)
    {
        IsBlocked = blocked;
        SetVisualState(blocked ? "Broken" : "Idle");
        if (blocked)
        {
            PlaySound(blockedSound, loop: false);
            OnMachineBlocked?.Invoke();
        }
    }

    private void SetVisualState(string state)
    {
        if (machineAnimator) machineAnimator.Play(state);
        if (statusLight)
        {
            statusLight.color = state switch
            {
                "Running" => Color.yellow,
                "Broken"  => Color.red,
                _         => Color.green
            };
        }
        if (dustParticles)
        {
            if (state == "Running") dustParticles.Play();
            else dustParticles.Stop();
        }
    }

    private void PlaySound(AudioClip clip, bool loop)
    {
        if (!audioSource || !clip) return;
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();
    }

    private void StopSound()
    {
        if (audioSource) audioSource.Stop();
    }
}
