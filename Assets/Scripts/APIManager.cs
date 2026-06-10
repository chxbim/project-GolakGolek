using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance { get; private set; }
    private const string GAME_SESSION_URL = "https://lomba.jtv.co.id/api/v1/game-session";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("API Config")]
    [SerializeField] private string baseUrl = "https://plus.jtv.co.id/Apigame/game_object";

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

        string remapped = RemapJsonKeys(rawJson);
        string wrapped  = "{\"items\":" + remapped + "}";

        GameItemDataList parsed;
        try { parsed = JsonUtility.FromJson<GameItemDataList>(wrapped); }
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

    /// <summary>
    /// POST CartPayload ke endpoint game-session.
    /// onSuccess dipanggil dengan raw response string kalau HTTP 200.
    /// onError dipanggil dengan pesan error kalau gagal / non-200.
    /// </summary>
    public void PostCart(CartPayload payload,
                         System.Action<string> onSuccess,
                         System.Action<string> onError)
    {
        StartCoroutine(PostCartCoroutine(payload, onSuccess, onError));
    }

    private IEnumerator PostCartCoroutine(CartPayload payload,
                                          System.Action<string> onSuccess,
                                          System.Action<string> onError)
    {
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        Debug.Log($"[APIManager] POST game-session → {GAME_SESSION_URL}");
        Debug.Log($"[APIManager] Payload JSON: {json}");

        using UnityEngine.Networking.UnityWebRequest req =
            new UnityEngine.Networking.UnityWebRequest(GAME_SESSION_URL,
                UnityEngine.Networking.UnityWebRequest.kHttpVerbPOST);

        req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept", "application/json");

        // Sertakan Bearer token dari session kalau ada
        string token = SessionCache.AccessToken;
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string response = req.downloadHandler.text;
            Debug.Log($"[APIManager] POST berhasil ({req.responseCode}). Response: {response}");
            onSuccess?.Invoke(response);
        }
        else
        {
            string error = $"HTTP {req.responseCode} — {req.error}. Body: {req.downloadHandler?.text}";
            Debug.LogError($"[APIManager] POST gagal: {error}");
            onError?.Invoke(error);
        }
    }

    private IEnumerator PostCartRoutine(CartPayload payload, Action<string> onSuccess, Action<string> onError)
    {
        string bodyJson = JsonUtility.ToJson(payload);
        byte[] bodyRaw  = Encoding.UTF8.GetBytes(bodyJson);

        using UnityWebRequest req = new UnityWebRequest(baseUrl, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
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

    private static string RemapJsonKeys(string json)
    {
        return json
            .Replace("\"nama_item\"",           "\"namaItem\"")
            .Replace("\"display_name\"",         "\"displayname\"")
            .Replace("\"kategori_barang\"",      "\"kategoriBarang\"")
            .Replace("\"harga\"",                "\"hargaRaw\"")
            .Replace("\"object_file_name\"",     "\"objectFileName\"")
            .Replace("\"object_kategori\"",      "\"objectKategori\"")
            .Replace("\"object_sub_kategori\"",  "\"objectSubKategori\"")
            .Replace("\"posisi_x\"",             "\"posisiXRaw\"")
            .Replace("\"posisi_y\"",             "\"posisiYRaw\"")
            .Replace("\"posisi_z\"",             "\"posisiZRaw\"")
            .Replace("\"jarak_vertikal\"",       "\"jarakVertikalRaw\"")
            .Replace("\"jarak_horizontal\"",     "\"jarakHorizontalRaw\"")
            .Replace("\"total_per_rak\"",        "\"totalPerRakRaw\"")
            .Replace("\"urutan_rak\"",           "\"urutanRakRaw\"")
            .Replace("\"jumlah_baris\"",         "\"jumlahBarisRaw\"")
            .Replace("\"scale_x\"",              "\"scaleXRaw\"")
            .Replace("\"scale_y\"",              "\"scaleYRaw\"")
            .Replace("\"scale_z\"",              "\"scaleZRaw\"")
            .Replace("\"rotate_x\"",             "\"rotateXRaw\"")
            .Replace("\"rotate_y\"",             "\"rotateYRaw\"")
            .Replace("\"rotate_z\"",             "\"rotateZRaw\"");
    }
}

