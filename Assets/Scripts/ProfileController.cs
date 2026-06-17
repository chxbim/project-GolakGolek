// ProfileController.cs
// Attach ke GameObject di ProfileScene (misal di KartuAnggota atau parent-nya).
//
// Tanggung jawab:
//   - Baca data user dari SessionCache (UserName, UserEmail) saat Start()
//   - Baca koin player dari PlayerPrefs lokal (key "player_koin")
//   - Tampilkan ke 3 TextMeshProUGUI: namaUser, emailUser, koinUser
//   - Nama dipotong maksimal 2 kata pertama (sisanya di-handle TMPro overflow)
//
// Yang TIDAK dilakukan script ini:
//   - Ambil/update koin dari hasil gameplay (itu tanggung jawab CartSystem,
//     yang akan menulis ke key PlayerPrefs "player_koin" terpisah)
//   - Login/logout/session validity (itu AuthManager/SceneController/SessionCache)

using UnityEngine;
using TMPro;

public class ProfileController : MonoBehaviour
{
    [Header("Profile — TMPro Fields")]
    [SerializeField] private TextMeshProUGUI namaUser;
    [SerializeField] private TextMeshProUGUI emailUser;
    [SerializeField] private TextMeshProUGUI koinUser;

    // Key PlayerPrefs lokal untuk koin player.
    // Sama dengan key yang nanti ditulis CartSystem setiap kali menang
    // (koin += 25 per sesi sukses), supaya ProfileController tinggal baca.
    private const string KEY_PLAYER_KOIN = "player_koin";

    private void Start()
    {
        DisplayUserInfo();
        DisplayKoin();
    }

    // ================================================================
    //  NAMA + EMAIL
    // ================================================================

    private void DisplayUserInfo()
    {
        if (namaUser != null)
            namaUser.text = GetDisplayName(SessionCache.UserName);

        if (emailUser != null)
            emailUser.text = SessionCache.UserEmail;
    }

    /// <summary>
    /// Ambil maksimal 2 kata pertama dari nama lengkap.
    /// Tidak dipotong manual ke 26 karakter — itu sudah ditangani
    /// setting overflow TMPro di Inspector.
    /// </summary>
    private string GetDisplayName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "";

        string[] words = fullName.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        if (words.Length <= 2) return fullName.Trim();

        return $"{words[0]} {words[1]}";
    }

    // ================================================================
    //  KOIN
    // ================================================================

    private void DisplayKoin()
    {
        if (koinUser == null) return;

        int koin = PlayerPrefs.GetInt(KEY_PLAYER_KOIN, 0);
        koinUser.text = koin.ToString();
    }
}