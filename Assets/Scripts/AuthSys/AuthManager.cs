using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;

public class AuthManager : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────
    [Header("Input Fields")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passwordLoginField;

    [Header("Feedback")]
    public TMP_Text warningLoginText;   // error message — default hidden
    public TMP_Text confirmLoginText;   // "Memuat..." — default hidden

    [Header("UI")]
    public UnityEngine.UI.Button loginButton;

    [Header("Scene Target")]
    [Tooltip("Scene yang dibuka setelah login berhasil")]
    public string sceneAfterLogin = "SelectMode";

    // ── Endpoint ─────────────────────────────────────────────────
    private const string LOGIN_URL = "https://hub.jtv.co.id/api/login";

    // ── Response model ───────────────────────────────────────────
    [System.Serializable]
    private class LoginUser
    {
        public int id;
        public string name;
        public string email;
    }

    [System.Serializable]
    private class LoginResponse
    {
        public LoginUser user;
        public string access_token;
        public string refresh_token;
        public string token_type;
        public long expires_in;
    }

    // ── Unity ────────────────────────────────────────────────────
    private void Start()
    {
        // Sembunyikan feedback text di awal
        SetWarning("");
        SetConfirm("");
    }

    // ── Public — dipanggil Button onClick ────────────────────────
    public void Login()
    {
        string email = emailLoginField.text.Trim();
        string password = passwordLoginField.text;

        // Validasi input kosong sebelum hit API
        if (string.IsNullOrEmpty(email))
        {
            SetWarning("Email tidak boleh kosong.");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            SetWarning("Password tidak boleh kosong.");
            return;
        }

        SetWarning("");
        StartCoroutine(PostLogin(email, password));
    }

    // ── Coroutine ────────────────────────────────────────────────
    private IEnumerator PostLogin(string email, string password)
    {
        SetLoadingState(true);

        // Body: x-www-form-urlencoded
        WWWForm form = new WWWForm();
        form.AddField("email", email);
        form.AddField("password", password);
        form.AddField("device_name", "GolakGolek");
        form.AddField("platform", "Android");
        form.AddField("app_id", "game_GolakGolek");

        using UnityWebRequest req = UnityWebRequest.Post(LOGIN_URL, form);
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        SetLoadingState(false);

        if (req.result != UnityWebRequest.Result.Success)
        {
            // Network error atau server error (4xx/5xx)
            SetWarning("Koneksi gagal. Coba lagi.");
            Debug.LogWarning($"[AuthManager] HTTP Error: {req.responseCode} — {req.error}");
            yield break;
        }

        // Parse JSON
        string json = req.downloadHandler.text;
        Debug.Log($"[AuthManager] Response: {json}");

        LoginResponse response = null;
        try
        {
            response = JsonUtility.FromJson<LoginResponse>(json);
        }
        catch (System.Exception ex)
        {
            SetWarning("Terjadi kesalahan. Coba lagi.");
            Debug.LogError($"[AuthManager] Parse error: {ex.Message}");
            yield break;
        }

        // Cek apakah token ada (guard kalau server kirim 200 tapi response salah)
        if (response == null || string.IsNullOrEmpty(response.access_token))
        {
            SetWarning("Email atau password salah.");
            yield break;
        }

        // Simpan session
        SessionCache.SaveSession(
            userId: response.user.id.ToString(),
            userName: response.user.name,
            accessToken: response.access_token,
            refreshToken: response.refresh_token,
            expiresInSeconds: response.expires_in
        );

        Debug.Log($"[AuthManager] Login berhasil. User: {response.user.name} (id: {response.user.id})");

        SetConfirm("Login berhasil! Memuat...");

        // Jeda singkat supaya user sempat baca konfirmasi
        yield return new WaitForSeconds(0.8f);

        SceneManager.LoadScene(sceneAfterLogin);
    }

    // ── Helpers ──────────────────────────────────────────────────
    private void SetLoadingState(bool loading)
    {
        if (loginButton != null) loginButton.interactable = !loading;
        SetConfirm(loading ? "Memuat..." : "");
    }

    private void SetWarning(string msg)
    {
        if (warningLoginText == null) return;
        warningLoginText.text = msg;
        warningLoginText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
    }

    private void SetConfirm(string msg)
    {
        if (confirmLoginText == null) return;
        confirmLoginText.text = msg;
        confirmLoginText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
    }
}