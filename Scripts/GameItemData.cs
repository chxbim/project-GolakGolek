using UnityEngine;

/// <summary>
/// Data satu item dari JSON/API.
/// Field names harus cocok persis dengan key di Shelves.json (setelah RemapJsonKeys).
/// </summary>
[System.Serializable]
public class GameItemData
{
    public string namaItem;
    public string kategori;
    public string varian;       // generik — menggantikan sausVarian
    public string modelType;
    public string hargaRaw;

    public float Harga
    {
        get
        {
            if (float.TryParse(hargaRaw, out float result)) return result;
            return 0f;
        }
    }

    public override string ToString()
        => $"{namaItem} [{kategori}] – Rp {Harga:N0}";
}

// ── Wrapper untuk flat API response (game_object endpoint) ───
// Dibutuhkan APIManager karena JsonUtility tidak bisa parse
// array JSON langsung — harus dibungkus object dulu.

[System.Serializable]
internal class GameItemDataList
{
    public GameItemData[] items;
}

// ── Struktur JSON bertingkat (Shelves.json lokal) ─────────────

[System.Serializable]
public class ObjectData
{
    public string fbxName;
    public string kategoriObject;
    public string subKategori;
}

[System.Serializable]
public class PosisiData
{
    public float x;
    public float y;
    public float z;
    public float jarakHorizontal;
    public int totalPerLevel;
    public int jumlahBaris;
}

[System.Serializable]
public class ShelfItemEntry
{
    public string namaItem;
    public string kategori;
    public string varian;
    public float harga;
    public ObjectData objectData;
    public PosisiData posisi;

    /// <summary>Konversi ke GameItemData untuk dipakai ShelfUnit dan CartSystem.</summary>
    public GameItemData ToGameItemData()
    {
        return new GameItemData
        {
            namaItem = namaItem,
            kategori = kategori,
            varian = varian,
            modelType = objectData?.fbxName ?? "Unknown",
            hargaRaw = harga.ToString()
        };
    }
}

[System.Serializable]
public class ShelfLevelData
{
    public int levelIndex;
    public ShelfItemEntry[] items;
}

[System.Serializable]
public class ShelfData
{
    public int shelfId;
    public string shelfType;
    public int urutanRak;
    public ShelfLevelData[] levels;
}

[System.Serializable]
public class ShelvesRoot
{
    public ShelfData[] shelves;
}