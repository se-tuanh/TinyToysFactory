using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MainMenuController — Drives the Main Menu scene.
/// Attach to a GameObject in the MainMenu scene.
/// Wire Play and Quit buttons in the Inspector.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button quitButton;

    [Header("Version Info (optional)")]
    public TextMeshProUGUI versionText;

    private void Start()
    {
        if (playButton) playButton.onClick.AddListener(OnPlay);
        if (quitButton) quitButton.onClick.AddListener(OnQuit);
        if (versionText) versionText.text = "v0.1 — Milestone 1";
    }

    private void OnPlay()
    {
        if (FadeTransition.Instance)
            FadeTransition.Instance.FadeTo("GameScene");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
