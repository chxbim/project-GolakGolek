using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Config ───────────────────────────────────────────────
    // FIX: const tidak bisa [SerializeField] — pakai string biasa
    [Header("API Config")]
    [SerializeField] private string baseUrl = "https://plus.jtv.co.id/Apigame/game_object";

    // ── GET ──────────────────────────────────────────────────

    public void FetchItems(Action<List<GameItemData>> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetItemsRoutine(onSuccess, onError));
    }

    private IEnumerator GetItemsRoutine(Action<List<GameItemData>> onSuccess, Action<string> onError)
    {
        using UnityWebRequest req = UnityWebRequest.Get(baseUrl);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[APIManager] GET gagal: {req.error}");
            onError?.Invoke(req.error);
            yield break;
        }

        string rawJson = req.downloadHandler.text;
        Debug.Log($"[APIManager] Raw JSON: {rawJson}");

        // FIX: Remap key snake_case → camelCase supaya JsonUtility bisa baca
        string remapped = RemapJsonKeys(rawJson);

        // FIX: JsonUtility tidak bisa parse array langsung — bungkus dulu
        string wrapped = "{\"items\":" + remapped + "}";

        GameItemDataList parsed;
        try
        {
            parsed = JsonUtility.FromJson<GameItemDataList>(wrapped);
        }
        catch (Exception e)
        {
            Debug.LogError($"[APIManager] Parse error: {e.Message}");
            onError?.Invoke(e.Message);
            yield break;
        }

        if (parsed?.items == null || parsed.items.Length == 0)
        {
            Debug.LogError("[APIManager] Parse result kosong.");
            onError?.Invoke("Parse result null/empty");
            yield break;
        }

        Debug.Log($"[APIManager] Berhasil parse {parsed.items.Length} item(s).");
        onSuccess?.Invoke(new List<GameItemData>(parsed.items));
    }

    // ── POST ─────────────────────────────────────────────────

    public void PostCart(CartPayload payload, Action<string> onSuccess = null, Action<string> onError = null)
    {
        StartCoroutine(PostCartRoutine(payload, onSuccess, onError));
    }

    private IEnumerator PostCartRoutine(CartPayload payload, Action<string> onSuccess, Action<string> onError)
    {
        string bodyJson = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);

        using UnityWebRequest req = new UnityWebRequest(baseUrl, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[APIManager] POST gagal: {req.error}");
            onError?.Invoke(req.error);
            yield break;
        }

        string response = req.downloadHandler.text;
        Debug.Log($"[APIManager] POST berhasil: {response}");
        onSuccess?.Invoke(response);
    }

    // ── Remap ─────────────────────────────────────────────────

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
    }
}

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