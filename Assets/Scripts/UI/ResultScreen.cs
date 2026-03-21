using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ResultScreen — Shown on Win or Lose.
/// Wire all fields in Inspector (or via TinyToysSceneBuilder).
/// </summary>
public class ResultScreen : MonoBehaviour
{
    [Header("Outcome Text")]
    public TextMeshProUGUI outcomeLabel;   // "ORDER COMPLETE!" or "FACTORY FAILED"
    public TextMeshProUGUI creditsLabel;
    public TextMeshProUGUI ordersLabel;
    public TextMeshProUGUI reputationLabel;
    public TextMeshProUGUI highScoreLabel; // shows all-time best

    [Header("Buttons")]
    public Button retryButton;
    public Button mainMenuButton;

    [Header("Panels")]
    public GameObject panel; // root panel, hidden by default

    // Tracked values across session (set by GameManager on game end)
    private int _ordersCompleted = 0;

    private void Start()
    {
        if (panel) panel.SetActive(false);

        if (retryButton)   retryButton.onClick.AddListener(OnRetry);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(OnMainMenu);

        // Subscribe to game end events
        GameManager.Instance.OnGameWin .AddListener(() => ShowResult(won: true));
        GameManager.Instance.OnGameLose.AddListener(() => ShowResult(won: false));

        // Track completed orders
        OrderManager.Instance.OnOrderCompleted.AddListener(_ => _ordersCompleted++);
    }

    // ── Public API ────────────────────────────────────────────────────────
    public void ShowResult(bool won)
    {
        if (panel) panel.SetActive(true);
        Time.timeScale = 0f; // pause

        var gm = GameManager.Instance;
        if (outcomeLabel)    outcomeLabel.text    = won ? "✅ ORDER COMPLETE!" : "❌ FACTORY FAILED";
        if (outcomeLabel)    outcomeLabel.color   = won ? new Color(0.2f,0.8f,0.4f) : new Color(0.9f,0.2f,0.2f);
        if (creditsLabel)    creditsLabel.text    = $"Credits Earned: ${gm.Credits}";
        if (ordersLabel)     ordersLabel.text     = $"Orders Completed: {_ordersCompleted}";
        if (reputationLabel) reputationLabel.text = $"Final Reputation: {gm.Reputation}";

        var (bestCr, bestRep) = SaveSystem.LoadHighScore();
        if (highScoreLabel)
            highScoreLabel.text = $"🏆 Best Run: ${bestCr} | Rep {bestRep}";
    }

    // ── Buttons ───────────────────────────────────────────────────────────
    private void OnRetry()
    {
        Time.timeScale = 1f;
        if (FadeTransition.Instance) FadeTransition.Instance.FadeTo("GameScene");
        else UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    private void OnMainMenu()
    {
        Time.timeScale = 1f;
        if (FadeTransition.Instance) FadeTransition.Instance.FadeTo("MainMenu");
        else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
