using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;

//lgsg pilih scene aja kalo udah login, simpen di PlayerPref
//point di Hub, coin di game

public class AuthManager : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────
    [Header("Input Fields")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passwordLoginField;

    [Header("Feedback (Image GameObjects)")]
    public GameObject warningImage;    // Image asset TxtWarning — default inactive di hierarchy
    public GameObject confirmImage;    // Image asset TxtConfirm — default active, di-hide pas Start

    [Header("UI")]
    public UnityEngine.UI.Button loginButton;

    [Header("Scene Target")]
    [Tooltip("Scene yang dibuka setelah login berhasil")]
    public string sceneAfterLogin = "SelectMode";

    // ── Endpoint ─────────────────────────────────────────────────
    private const string LOGIN_URL = "https://sso.jtv.co.id/api/login";

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
        // Sembunyikan kedua image feedback di awal
        if (warningImage != null) warningImage.SetActive(false);
        if (confirmImage != null) confirmImage.SetActive(false);
    }

    // ── Public — dipanggil Button onClick ────────────────────────
    public void Login()
    {
        string email = emailLoginField.text.Trim();
        string password = passwordLoginField.text;

        // Validasi input kosong sebelum hit API
        if (string.IsNullOrEmpty(email))
        {
            SetWarning(true);
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            SetWarning(true);
            return;
        }

        SetWarning(false);
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
        form.AddField("device_uuid", "67");           // 67 — hardcoded, bypass SystemInfo.deviceUniqueIdentifier
        form.AddField("device_name", "yes");          // yes — hardcoded static, sama kek uuid
        form.AddField("platform", "Android");
        form.AddField("app_id", "game_GolakGolek");

        using UnityWebRequest req = UnityWebRequest.Post(LOGIN_URL, form);
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        SetLoadingState(false);

        if (req.result != UnityWebRequest.Result.Success)
        {
            // Network error atau server error (4xx/5xx)
            SetWarning(true);
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
            SetWarning(true);
            Debug.LogError($"[AuthManager] Parse error: {ex.Message}");
            yield break;
        }

        // Cek apakah token ada (guard kalau server kirim 200 tapi response salah)
        if (response == null || string.IsNullOrEmpty(response.access_token))
        {
            SetWarning(true);
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

        SetConfirm(true); // tampilkan image "Login berhasil"

        // Jeda singkat supaya user sempat baca konfirmasi
        yield return new WaitForSeconds(0.4f);

        SceneManager.LoadScene("SelectMode");
    }

    // ── Helpers ──────────────────────────────────────────────────
    private void SetLoadingState(bool loading)
    {
        if (loginButton != null) loginButton.interactable = !loading;
        SetConfirm(loading);
    }

    private void SetWarning(bool show)
    {
        if (warningImage != null) warningImage.SetActive(show);
    }

    private void SetConfirm(bool show)
    {
        if (confirmImage != null) confirmImage.SetActive(show);
    }
}
