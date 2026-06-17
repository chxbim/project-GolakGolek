using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach ke GameObject di setiap scene.
/// Handles:
///   1. Navigasi antar scene via tombol UI
///   2. Cek session di HomeScene — redirect ke Login atau SelectMode
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // ── Session Check ────────────────────────────────────────────

    /// <summary>
    /// Dipanggil di Start() HomeScene.
    /// Kalau session masih valid, langsung ke SelectMode.
    /// Kalau tidak, biarkan user di HomeScene (tombol Mulai → LoginScene).
    /// </summary>
    public void CheckSessionAndRoute()
    {
        if (SessionCache.IsSessionValid())
        {
            Debug.Log($"[SceneLoader] Session valid. User: {SessionCache.UserName} — langsung ke SelectMode.");
            SceneManager.LoadScene("SelectMode");
        }
        else
        {
            Debug.Log("[SceneLoader] Session tidak ada atau expired. Tunggu input user.");
            // Tidak redirect — user tetap di HomeScene, tombol Mulai ke LoginScene
        }
    }

    [Header("Session")]
    [SerializeField] private bool checkSessionOnStart = false;

    private void Start()
    {
        if (checkSessionOnStart)
            CheckSessionAndRoute();
    }

    // ── Navigation ───────────────────────────────────────────────

    public void LoadMainGame()
    {
        SceneManager.LoadScene("MainGameScene");
    }

    public void LoadSelectMode()
    {
        SceneManager.LoadScene("SelectMode");
    }

    public void LoadLoginScene()
    {
        SceneManager.LoadScene("LoginScene");
    }

    public void LoadProfileScene()
    {
        SceneManager.LoadScene("ProfileScene");
    }

    public void LoadSettingScene()
    {
        SceneManager.LoadScene("SettingScene");
    }

    public void LoadHomeScene()
    {
        SceneManager.LoadScene("HomeScene");
    }

    public void LoadMainGameAsTimeAttack()
    {
        GameSession.SelectedMode = GameMode.time_attack;
        SceneManager.LoadScene("MainGameScene");
    }

    public void LoadMainGameAsGolek()
    {
        GameSession.SelectedMode = GameMode.golek;
        SceneManager.LoadScene("MainGameScene");
    }

    // ── Session Utility ──────────────────────────────────────────

    /// <summary>
    /// Dipanggil tombol Logout di scene manapun.
    /// </summary>
    public void Logout()
    {
        SessionCache.ClearSession();
        SceneManager.LoadScene("HomeScene");
    }

    public void Kepencet()
    {
        Debug.Log("KEPENCET");
    }
}