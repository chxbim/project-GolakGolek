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
    [SerializeField] private const string BASE_URL = "https://plus.jtv.co.id/Apigame/game_object";

    // CartEndpoint belum tersedia/gtw yh gmn di backend JTV.
    // Nanti isi URL-nya setelah dikonfirmasi ke tim backend.

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

    private bool HasInternetConnection()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    private IEnumerator GetItemsRoutine(Action<List<GameItemData>> onSuccess, Action<string> onError)
    {

        UnityWebRequest req = UnityWebRequest.Get(BASE_URL);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            onError?.Invoke(req.error);
        }
        else
        {
            string json = req.downloadHandler.text;
            Debug.Log("Request berhasil");
            Debug.Log(json);

            // parsing JSON di sini
        }
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

        using UnityWebRequest req = new UnityWebRequest(BASE_URL, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

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