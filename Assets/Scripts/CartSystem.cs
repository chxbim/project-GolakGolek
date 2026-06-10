using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton yang menyimpan semua item yang player masukkan ke keranjang.
/// Tidak perlu attach ke GameObject yang spesifik — bisa di GameObject manapun di scene.
///
/// ALUR YANG BENAR:
///   ShelfUnit.Interact()
///     → ListingBarang.CekDanCentangItem(item.id)   ← matching logic ada di sini
///       → kalau true → CartSystem.Instance.AddItem(item)
///
/// CartSystem TIDAK perlu tau soal GameMode atau matching —
/// dia hanya menerima item yang sudah divalidasi oleh ListingBarang.
/// </summary>
public class CartSystem : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────

    public static CartSystem Instance { get; private set; }

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

    // ── Data ─────────────────────────────────────────────────

    private readonly List<CartEntry> entries = new List<CartEntry>();

    // ── Events ───────────────────────────────────────────────

    /// <summary>Dipanggil setiap kali isi cart berubah.</summary>
    public event System.Action OnCartChanged;

    // ── Public API ───────────────────────────────────────────

    /// <summary>
    /// Tambah item ke cart. Dedup by item.id.
    /// Panggil ini hanya setelah ListingBarang.CekDanCentangItem() return true.
    /// </summary>
    public void AddItem(GameItemData item)
    {
        if (item == null) return;

        CartEntry existing = entries.Find(e => e.Item.id == item.id);
        if (existing != null)
        {
            existing.Quantity++;
            Debug.Log($"[Cart] +1 {item.namaItem} ({item.varian}) → qty {existing.Quantity}");
        }
        else
        {
            entries.Add(new CartEntry(item, 1));
            Debug.Log($"[Cart] Ditambahkan: {item.namaItem} ({item.varian})");
        }

        PrintCartSummary();
        OnCartChanged?.Invoke();
    }

    /// <summary>Total harga semua item di cart.</summary>
    public float GetTotal()
    {
        float total = 0f;
        foreach (CartEntry e in entries)
            total += e.Item.Harga * e.Quantity;
        return total;
    }

    /// <summary>Jumlah jenis item unik di cart (bukan total quantity).</summary>
    public int GetItemCount() => entries.Count;

    /// <summary>Read-only list semua entry di cart.</summary>
    public IReadOnlyList<CartEntry> GetEntries() => entries.AsReadOnly();

    // ── Debug ────────────────────────────────────────────────

    public void PrintCartSummary()
    {
        if (entries.Count == 0)
        {
            Debug.Log("[Cart] Keranjang kosong.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[Cart] ── Isi Keranjang ──────────────────────────────────");
        sb.AppendLine($"  {"Nama Barang",-20} {"Varian",-12} {"Harga",12}  {"Qty",5}  {"Subtotal",12}");

        foreach (CartEntry e in entries)
        {
            float sub = e.Item.Harga * e.Quantity;
            string varian = string.IsNullOrEmpty(e.Item.varian) ? "-" : e.Item.varian;
            sb.AppendLine($"  {e.Item.namaItem,-20} {varian,-12} {"Rp " + e.Item.Harga.ToString("N0"),12}  {e.Quantity,5}  {"Rp " + sub.ToString("N0"),12}");
        }

        sb.AppendLine($"  {"TOTAL",-20} {"",12}  {"",12}  {"",5}  {"Rp " + GetTotal().ToString("N0"),12}");
        sb.AppendLine("[Cart] ────────────────────────────────────────────────");

        Debug.Log(sb.ToString());
    }

    // ── Build CartPayload untuk API POST ─────────────────────

    /// <summary>
    /// Build payload POST sesuai schema backend.
    /// Dipanggil dari KasirUI / GameModeController saat sesi selesai.
    /// itemDitemukan diisi dari cart sendiri (entries.Count) kecuali di-override.
    /// </summary>
    /// <param name="sessionData">Data sesi dari GameCashierData.</param>
    /// <param name="totalObjective">Jumlah item objective sesi ini (dari ListingBarang.jumlahItemPerSesi).</param>
    public CartPayload BuildPayload(GameCashierData sessionData, int totalObjective)
    {
        var itemPayloads = new List<CartItemPayload>();
        foreach (CartEntry e in entries)
        {
            itemPayloads.Add(new CartItemPayload
            {
                nama_item = e.Item.namaItem,
                kategori = e.Item.kategoriBarang,
                varian = e.Item.varian,
                urutan_rak = e.Item.urutanRak,
                harga = e.Item.hargaRaw,
                quantity = e.Quantity.ToString()
            });
        }

        bool semuaTerpenuhi = entries.Count >= totalObjective;

        return new CartPayload
        {
            player_id = sessionData.playerId,
            mode = sessionData.mode,
            waktu_selesai = sessionData.waktuSelesai,
            item_ditemukan = entries.Count,
            total_harga = GetTotal(),
            koin = semuaTerpenuhi ? 25 : 0,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            items = itemPayloads
        };
    }
}

// ── CartEntry ────────────────────────────────────────────────

/// <summary>Satu baris di cart: item + quantity.</summary>
public class CartEntry
{
    public GameItemData Item { get; }
    public int Quantity { get; set; }

    public CartEntry(GameItemData item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }
}

// ── Payload classes (sesuai schema POST backend) ─────────────

[System.Serializable]
public class CartPayload
{
    public string player_id;
    public string mode;
    public float waktu_selesai;
    public int item_ditemukan;
    public float total_harga;
    public float koin;
    public string timestamp;
    public List<CartItemPayload> items;
}

[System.Serializable]
public class CartItemPayload
{
    public string nama_item;
    public string kategori;
    public string varian;
    public int urutan_rak;
    public string harga;
    public string quantity;
}