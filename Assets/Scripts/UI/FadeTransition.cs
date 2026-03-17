using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// FadeTransition — DontDestroyOnLoad singleton. 
/// Fades screen to black, loads scene, fades back in.
/// Attach to a persistent "FadeTransition" GameObject with a full-screen black Image child.
/// </summary>
public class FadeTransition : MonoBehaviour
{
    public static FadeTransition Instance { get; private set; }

    [Header("Settings")]
    public float fadeDuration = 0.4f;
    public UnityEngine.UI.Image fadeImage; // full-screen black Image

    private bool _isFading;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage)
        {
            fadeImage.color = Color.black;
            StartCoroutine(FadeIn());
        }
    }

    // ── Public API ────────────────────────────────────────────────────────
    public void FadeTo(string sceneName)
    {
        if (!_isFading) StartCoroutine(FadeOutThenLoad(sceneName));
    }

    // ── Coroutines ────────────────────────────────────────────────────────
    private IEnumerator FadeOutThenLoad(string sceneName)
    {
        _isFading = true;
        yield return StartCoroutine(Fade(0f, 1f));            // fade to black
        SceneManager.LoadScene(sceneName);
        yield return null;                                     // wait one frame for scene init
        yield return StartCoroutine(FadeIn());
        _isFading = false;
    }

    private IEnumerator FadeIn()
    {
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float from, float to)
    {
        if (!fadeImage) yield break;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;             // unscaled so it works when paused
            float a = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeImage.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, to);
    }
}
