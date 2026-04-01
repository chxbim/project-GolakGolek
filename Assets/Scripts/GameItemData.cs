/// <summary>
/// Data satu item dari API. Tidak butuh Newtonsoft sama sekali.
///
/// Field names di sini sengaja pakai camelCase karena JsonUtility
/// tidak bisa handle key dengan spasi. Remapping dilakukan di
/// APIManager.RemapJsonKeys() sebelum di-parse.
/// </summary>
[System.Serializable]
public class GameItemData
{
    // Nama field HARUS cocok persis dengan hasil RemapJsonKeys() di APIManager
    public string namaItem;
    public string kategori;
    public string modelType;
    public string sausVarian;
    public string hargaRaw;     // raw string dari API, parse ke float lewat property

    // ── Computed ────────────────────────────────────────────

    /// <summary>Harga sebagai float, siap untuk kalkulasi di CartSystem.</summary>
    public float Harga
    {
        get
        {
            if (float.TryParse(hargaRaw, out float result))
                return result;
            return 0f;
        }
    }

    public override string ToString()
        => $"{namaItem} [{kategori}] – Rp {Harga:N0}";
}

/// <summary>
/// Wrapper karena JsonUtility.FromJson tidak bisa langsung parse array JSON.
/// Dipakai internal di APIManager saja.
/// </summary>
[System.Serializable]
internal class GameItemDataList
{
    public GameItemData[] items;
}