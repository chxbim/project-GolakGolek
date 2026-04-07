using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Singleton yang mengelola semua komunikasi ke API JTV.
/// Tidak butuh Newtonsoft — JsonUtility dipakai dengan key remapping manual.
/// Letakkan di scene sebagai satu GameObject "APIManager".
/// </summary>
public class APIManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────

    public static APIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Config ───────────────────────────────────────────────

    [Header("API Config")]
    [SerializeField] private string baseUrl = "https://plus.jtv.co.id/Apigame/game_object";

    // Satu endpoint untuk GET (ambil produk) dan POST (kirim cart).
    // Konfirmasi ke tim JTV apakah game_object support POST method
    private string ItemsEndpoint => $"{baseUrl}/game_object";

    // CartEndpoint belum tersedia/gtw yh gmn di backend JTV.
    // Nanti isi URL-nya setelah dikonfirmasi ke tim backend.
    private string CartEndpoint => $"{baseUrl}/game_object";

    // ── Public: Fetch Items ──────────────────────────────────

    /// <summary>
    /// GET semua item dari API.
    /// onSuccess dipanggil dengan List GameItemData jika berhasil.
    /// onError dipanggil dengan pesan error jika gagal.
    /// </summary>
    public void FetchItems(Action<List<GameItemData>> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetItemsRoutine(onSuccess, onError));
    }

    private IEnumerator GetItemsRoutine(Action<List<GameItemData>> onSuccess, Action<string> onError)
    {
        using UnityWebRequest req = UnityWebRequest.Get(ItemsEndpoint);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = $"[APIManager] GET gagal: {req.error}";
            Debug.LogError(err);
            onError?.Invoke(err);
            yield break;
        }

        string rawJson = req.downloadHandler.text;
        Debug.Log($"[APIManager] Raw JSON: {rawJson}");

        // ── Remap key → field name yang bisa dibaca JsonUtility ──
        string remapped = RemapJsonKeys(rawJson);
        Debug.Log($"[APIManager] Remapped JSON: {remapped}");

        // ── JsonUtility tidak bisa parse array langsung, bungkus dulu ──
        string wrapped = "{\"items\":" + remapped + "}";


        GameItemDataList parsed;
        try
        {
            parsed = JsonUtility.FromJson<GameItemDataList>(wrapped);
        }
        catch (Exception e)
        {
            string err = $"[APIManager] JSON parse error: {e.Message}";
            Debug.LogError(err);
            onError?.Invoke(err);
            yield break;
        }

        if (parsed == null || parsed.items == null)
        {
            string err = "[APIManager] Parse result null — cek format JSON dari API.";
            Debug.LogError(err);
            onError?.Invoke(err);
            yield break;
        }

        List<GameItemData> result = new List<GameItemData>(parsed.items);
        Debug.Log($"[APIManager] Berhasil fetch {result.Count} item(s).");
        onSuccess?.Invoke(result);
    }

    // ── Public: Post Cart ────────────────────────────────────

    /// <summary>
    /// POST data cart ke API.
    /// JsonUtility.ToJson() dipakai — tidak butuh Newtonsoft.
    /// </summary>
    public void PostCart(CartPayload payload, Action<string> onSuccess = null, Action<string> onError = null)
    {
        StartCoroutine(PostCartRoutine(payload, onSuccess, onError));
    }

    private IEnumerator PostCartRoutine(CartPayload payload, Action<string> onSuccess, Action<string> onError)
    {
        string bodyJson = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);

        using UnityWebRequest req = new UnityWebRequest(CartEndpoint, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = $"[APIManager] POST gagal: {req.error}";
            Debug.LogError(err);
            onError?.Invoke(err);
            yield break;
        }

        string response = req.downloadHandler.text;
        Debug.Log($"[APIManager] POST cart berhasil. Response: {response}");
        onSuccess?.Invoke(response);
    }

    // ── Key Remapper ─────────────────────────────────────────

    /// <summary>
    /// Ganti JSON key dari API (punya spasi / trailing space) ke nama field
    /// yang valid untuk JsonUtility. Urutan replace penting — lebih spesifik dulu.
    /// Kalau API nambah field baru, cukup tambah baris Replace di sini.
    /// </summary>
    private static string RemapJsonKeys(string json)
    {
        return json
            .Replace("\"nama_item\"", "\"namaItem\"")
            .Replace("\"kategori_barang\"", "\"kategoriBarang\"")
            .Replace("\"harga\"", "\"hargaRaw\"")
            .Replace("\"object_file_name\"", "\"objectFileName\"")
            .Replace("\"object_kategori\"", "\"objectKategori\"")
            .Replace("\"object_sub_kategori\"", "\"objectSubKategori\"")
            .Replace("\"posisi_x\"", "\"posisiX\"")
            .Replace("\"posisi_y\"", "\"posisiY\"")
            .Replace("\"posisi_z\"", "\"posisiZ\"")
            .Replace("\"jarak_vertikal\"", "\"jarakVertikal\"")
            .Replace("\"jarak_horizontal\"", "\"jarakHorizontal\"")
            .Replace("\"total_per_rak\"", "\"totalPerRak\"")
            .Replace("\"urutan_rak\"", "\"urutanRak\"")
            .Replace("\"jumlah_baris\"", "\"jumlahBaris\"");
        // "id", "varian", "created_at", "updated_at" tidak perlu diremap
    }
}

// ── Cart Payload ─────────────────────────────────────────────

/// <summary>
/// Struktur data yang dikirim ke API saat player checkout.
/// Sesuaikan field names dengan dokumentasi API cart JTV nanti.
/// </summary>
[System.Serializable]
public class CartPayload
{
    public string playerId;
    public List<CartItemPayload> items;
    public float totalHarga;
    public string timestamp;
}

[System.Serializable]
public class CartItemPayload
{
    public string namaItem;
    public string kategori;
    public float harga;
    public int quantity;
}