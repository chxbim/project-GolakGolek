using UnityEngine;

/// <summary>
/// Data satu item dari JSON/API.
/// Field names harus cocok persis dengan key di Shelves.json (setelah RemapJsonKeys).
/// </summary>
[System.Serializable]
public class GameItemData
{
    public string id;
    public string namaItem;           // dari: nama_item
    public string kategoriBarang;     // dari: kategori_barang
    public string varian;
    public string hargaRaw;           // dari: harga
    public string objectFileName;     // dari: object_file_name
    public string objectKategori;     // dari: object_kategori
    public string objectSubKategori;  // dari: object_sub_kategori
    public float posisiX;             // dari: posisi_x
    public float posisiY;             // dari: posisi_y
    public float posisiZ;             // dari: posisi_z
    public float jarakVertikal;       // dari: jarak_vertikal
    public float jarakHorizontal;     // dari: jarak_horizontal
    public int totalPerRak;           // dari: total_per_rak
    public int urutanRak;             // dari: urutan_rak  ← kunci matching ke ShelfUnit
    public int jumlahBaris;           // dari: jumlah_baris

    // ── Field non-API (di-set runtime, tidak dari JSON) ──────
    /// <summary>
    /// Icon untuk Inventory UI. Di-assign setelah fetch via Resources.Load
    /// atau IconRegistry. JsonUtility mengabaikan field ini secara otomatis
    /// karena Sprite tidak serializable oleh JsonUtility.
    /// </summary>
    [System.NonSerialized] public Sprite icon;

    /// <summary>
    /// Maksimal stack per slot inventory. Default 99.
    /// Bisa di-override oleh IconRegistry jika perlu per-item config.
    /// </summary>
    [System.NonSerialized] public int maxStackSize = 1;

    // ── Computed ─────────────────────────────────────────────
    public float Harga
    {
        get
        {
            if (float.TryParse(hargaRaw, out float result)) return result;
            return 0f;
        }
    }

    public override string ToString()
        => $"{namaItem} [{kategoriBarang}] {varian} – Rp {Harga:N0} (rak ke-{urutanRak})";
}

// ── Wrapper untuk JsonUtility (tidak bisa parse array langsung) ──
[System.Serializable]
internal class GameItemDataList
{
    public GameItemData[] items;
}