using UnityEngine;

/// <summary>
/// Static helper — tidak perlu attach ke GameObject.
/// Simpan dan baca session login dari PlayerPrefs.
/// Dipanggil oleh AuthManager (save) dan SceneController (cek).
/// </summary>
public static class SessionCache
{
    // ── Keys ────────────────────────────────────────────────────
    private const string KEY_USER_ID = "session_user_id";
    private const string KEY_USER_NAME = "session_user_name";
    private const string KEY_ACCESS_TOKEN = "session_access_token";
    private const string KEY_REFRESH_TOKEN = "session_refresh_token";
    private const string KEY_EXPIRY = "session_expiry";       // Unix timestamp (string)

    // ── Properties ──────────────────────────────────────────────
    public static string UserId => PlayerPrefs.GetString(KEY_USER_ID, "");
    public static string UserName => PlayerPrefs.GetString(KEY_USER_NAME, "");
    public static string AccessToken => PlayerPrefs.GetString(KEY_ACCESS_TOKEN, "");
    public static string RefreshToken => PlayerPrefs.GetString(KEY_REFRESH_TOKEN, "");

    // ── Save ────────────────────────────────────────────────────

    /// <summary>
    /// Simpan session setelah login berhasil.
    /// expiresInSeconds diambil langsung dari field "expires_in" di response JSON.
    /// </summary>
    public static void SaveSession(string userId, string userName,
                                   string accessToken, string refreshToken,
                                   long expiresInSeconds)
    {
        long expiryTimestamp = DateTimeToUnix(System.DateTime.UtcNow) + expiresInSeconds;

        PlayerPrefs.SetString(KEY_USER_ID, userId);
        PlayerPrefs.SetString(KEY_USER_NAME, userName);
        PlayerPrefs.SetString(KEY_ACCESS_TOKEN, accessToken);
        PlayerPrefs.SetString(KEY_REFRESH_TOKEN, refreshToken);
        PlayerPrefs.SetString(KEY_EXPIRY, expiryTimestamp.ToString());
        PlayerPrefs.Save(); // flush ke disk Android
    }

    // ── Validity ────────────────────────────────────────────────

    /// <summary>
    /// True kalau session ada dan belum expired.
    /// Kalau expired, auto ClearSession().
    /// </summary>
    public static bool IsSessionValid()
    {
        if (!PlayerPrefs.HasKey(KEY_ACCESS_TOKEN)) return false;
        if (!PlayerPrefs.HasKey(KEY_EXPIRY)) return false;

        string expiryStr = PlayerPrefs.GetString(KEY_EXPIRY, "0");
        if (!long.TryParse(expiryStr, out long expiryTimestamp)) return false;

        long now = DateTimeToUnix(System.DateTime.UtcNow);
        if (now >= expiryTimestamp)
        {
            ClearSession();
            return false;
        }

        return true;
    }

    // ── Clear ───────────────────────────────────────────────────

    /// <summary>
    /// Hapus semua data session. Dipanggil saat logout atau session expired.
    /// </summary>
    public static void ClearSession()
    {
        PlayerPrefs.DeleteKey(KEY_USER_ID);
        PlayerPrefs.DeleteKey(KEY_USER_NAME);
        PlayerPrefs.DeleteKey(KEY_ACCESS_TOKEN);
        PlayerPrefs.DeleteKey(KEY_REFRESH_TOKEN);
        PlayerPrefs.DeleteKey(KEY_EXPIRY);
        PlayerPrefs.Save();
    }

    // ── Helper ──────────────────────────────────────────────────

    private static long DateTimeToUnix(System.DateTime dt)
    {
        return (long)(dt - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc))
               .TotalSeconds;
    }
}