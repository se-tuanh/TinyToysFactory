using UnityEngine;

/// <summary>
/// SaveSystem — Persists per-player best run data via PlayerPrefs.
/// High Score = best (Credits + Reputation) across all completed runs.
/// Call SaveHighScore() at session end; LoadHighScore() for display.
/// </summary>
public static class SaveSystem
{
    private const string KEY_BEST_CREDITS = "hs_credits";
    private const string KEY_BEST_REP     = "hs_reputation";
    private const string KEY_BEST_SCORE   = "hs_total";      // credits + rep combined

    // ── Save ─────────────────────────────────────────────────────────────────
    /// <summary>Save if this run beat the previous best combined score.</summary>
    public static bool SaveHighScore(int credits, int reputation)
    {
        int combined = credits + reputation;
        int best     = PlayerPrefs.GetInt(KEY_BEST_SCORE, 0);

        if (combined > best)
        {
            PlayerPrefs.SetInt(KEY_BEST_CREDITS, credits);
            PlayerPrefs.SetInt(KEY_BEST_REP,     reputation);
            PlayerPrefs.SetInt(KEY_BEST_SCORE,   combined);
            PlayerPrefs.Save();
            Debug.Log($"[SaveSystem] New high score! Credits={credits} Rep={reputation} Total={combined}");
            return true;
        }

        Debug.Log($"[SaveSystem] Score {combined} did not beat best {best}.");
        return false;
    }

    // ── Load ─────────────────────────────────────────────────────────────────
    public static (int credits, int reputation) LoadHighScore()
    {
        return (PlayerPrefs.GetInt(KEY_BEST_CREDITS, 0),
                PlayerPrefs.GetInt(KEY_BEST_REP,     0));
    }

    public static int LoadBestTotal() => PlayerPrefs.GetInt(KEY_BEST_SCORE, 0);

    // ── Reset (debug / settings) ─────────────────────────────────────────────
    public static void ClearHighScore()
    {
        PlayerPrefs.DeleteKey(KEY_BEST_CREDITS);
        PlayerPrefs.DeleteKey(KEY_BEST_REP);
        PlayerPrefs.DeleteKey(KEY_BEST_SCORE);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] High score cleared.");
    }
}
