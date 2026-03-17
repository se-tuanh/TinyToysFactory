using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tutorial — 3-step overlay shown to first-time players.
/// Stored in PlayerPrefs so it only shows once.
/// Wire tutorialPanel, stepText, nextButton in Inspector.
/// </summary>
public class Tutorial : MonoBehaviour
{
    [Header("UI References")]
    public GameObject     tutorialPanel;
    public TextMeshProUGUI stepText;
    public TextMeshProUGUI stepCounterText;
    public Button          nextButton;
    public Button          skipButton;

    private static readonly string[] Steps =
    {
        "🏭 Step 1 of 3\n\nClick <b>Machine A (Assembly)</b> to start producing parts.\nMake sure you have enough Wood/Plastic and Power.",
        "🎨 Step 2 of 3\n\nAfter Assembly completes, a WIP item enters the <b>Buffer</b>.\nClick <b>Machine B (Paint & Pack)</b> to finish the product.",
        "📦 Step 3 of 3\n\nWhen you have enough finished products, press\n<b>Deliver</b> on an active Order to fulfill it and earn Credits!"
    };

    private int _step = 0;

    private void Start()
    {
        if (PlayerPrefs.GetInt("tutorial_done", 0) == 1)
        {
            if (tutorialPanel) tutorialPanel.SetActive(false);
            return;
        }

        if (tutorialPanel) tutorialPanel.SetActive(true);
        if (nextButton) nextButton.onClick.AddListener(OnNext);
        if (skipButton) skipButton.onClick.AddListener(CompleteTutorial);
        ShowStep(0);
    }

    private void ShowStep(int index)
    {
        _step = index;
        if (stepText)    stepText.text    = Steps[index];
        if (stepCounterText) stepCounterText.text = $"{index+1}/{Steps.Length}";
        if (nextButton)
        {
            var lbl = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) lbl.text = index < Steps.Length - 1 ? "Next →" : "Let's Go! 🚀";
        }
    }

    private void OnNext()
    {
        if (_step < Steps.Length - 1)
            ShowStep(_step + 1);
        else
            CompleteTutorial();
    }

    private void CompleteTutorial()
    {
        PlayerPrefs.SetInt("tutorial_done", 1);
        PlayerPrefs.Save();
        if (tutorialPanel) tutorialPanel.SetActive(false);
    }

    /// <summary>Reset tutorial flag (call from Debug menu or Settings).</summary>
    public static void ResetTutorial() => PlayerPrefs.DeleteKey("tutorial_done");
}
