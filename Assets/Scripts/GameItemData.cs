using UnityEngine;
using System.Globalization;

[System.Serializable]
public class GameItemData
{
    public string id;
    public string namaItem;
    public string displayName;          // riddle name untuk mode Golek ← display_name
    public string kategoriBarang;
    public string varian;
    public string hargaRaw;
    public string objectFileName;
    public string objectKategori;
    public string objectSubKategori;

    // Disimpan sebagai string karena API return JSON string, bukan JSON number
    // JsonUtility tidak bisa auto-parse string → float
    public string posisiXRaw;
    public string posisiYRaw;
    public string posisiZRaw;
    public string jarakVertikalRaw;
    public string jarakHorizontalRaw;
    public string totalPerRakRaw;
    public string urutanRakRaw;
    public string jumlahBarisRaw;
    public string scaleXRaw;            // ← scale_x
    public string scaleYRaw;            // ← scale_y
    public string scaleZRaw;            // ← scale_z
    public string rotateXRaw;           // ← rotate_x  (Euler degrees)
    public string rotateYRaw;           // ← rotate_y
    public string rotateZRaw;           // ← rotate_z

    // ── Parsed float properties ───────────────────────────────

    public float Harga => Parse(hargaRaw);
    public float posisiX => Parse(posisiXRaw);
    public float posisiY => Parse(posisiYRaw);
    public float posisiZ => Parse(posisiZRaw);
    public float jarakVertikal => Parse(jarakVertikalRaw);
    public float jarakHorizontal => Parse(jarakHorizontalRaw);
    public int totalPerRak => (int)Parse(totalPerRakRaw);
    public int urutanRak => (int)Parse(urutanRakRaw);
    public int jumlahBaris => (int)Parse(jumlahBarisRaw);
    public float scaleX => Parse(scaleXRaw);
    public float scaleY => Parse(scaleYRaw);
    public float scaleZ => Parse(scaleZRaw);
    public float rotateX => Parse(rotateXRaw);
    public float rotateY => Parse(rotateYRaw);
    public float rotateZ => Parse(rotateZRaw);

    private static float Parse(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0f;
        if (float.TryParse(s, NumberStyles.Float,
            CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        Debug.LogWarning($"[GameItemData] Gagal parse: '{s}'");
        return 0f;
    }

    /// <summary>
    /// Icon sprite untuk UI slot inventory.
    /// Tidak datang dari API — diisi oleh IconRegistry.cs setelah fetch,
    /// atau di-assign manual via ScriptableObject lookup.
    /// Boleh null — Slots.cs sudah handle kasus icon null.
    /// </summary>
    [System.NonSerialized] public Sprite icon;

    public override string ToString()
        => $"{namaItem} [{kategoriBarang}] {varian} – Rp {Harga:N0} (rak ke-{urutanRak})";
}

[System.Serializable]
internal class GameItemDataList
{
    public GameItemData[] items;
}