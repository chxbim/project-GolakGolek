using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton yang menyimpan semua item yang player masukkan ke keranjang.
/// Tidak perlu attach ke GameObject yang spesifik — bisa di GameObject manapun di scene.
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

    // ── Events (subscribe dari UI nanti) ─────────────────────

    /// <summary>Dipanggil setiap kali isi cart berubah.</summary>
    public event System.Action OnCartChanged;

    // ── Public API ───────────────────────────────────────────

    /// <summary>Tambah satu item ke cart. Jika sudah ada, tambah quantity.</summary>
    public void AddItem(GameItemData item)
    {
        if (item == null) return;

        CartEntry existing = entries.Find(e => e.Item.namaItem == item.namaItem);
        if (existing != null)
        {
            existing.Quantity++;
            Debug.Log($"[Cart] +1 {item.namaItem} → qty {existing.Quantity}");
        }
        else
        {
            entries.Add(new CartEntry(item, 1));
            Debug.Log($"[Cart] Ditambahkan: {item.namaItem}");
        }

        PrintCartSummary();
        OnCartChanged?.Invoke();
    }

    /// <summary>Kurangi quantity. Jika 0, hapus dari cart.</summary>
    public void RemoveItem(GameItemData item)
    {
        if (item == null) return;

        CartEntry existing = entries.Find(e => e.Item.namaItem == item.namaItem);
        if (existing == null)
        {
            Debug.LogWarning($"[Cart] Item tidak ada di cart: {item.namaItem}");
            return;
        }

        existing.Quantity--;
        if (existing.Quantity <= 0)
        {
            entries.Remove(existing);
            Debug.Log($"[Cart] Dihapus dari cart: {item.namaItem}");
        }
        else
        {
            Debug.Log($"[Cart] -{1} {item.namaItem} → qty {existing.Quantity}");
        }

        OnCartChanged?.Invoke();
    }

    /// <summary>Hapus semua item dari cart.</summary>
    public void ClearCart()
    {
        entries.Clear();
        Debug.Log("[Cart] Cart dikosongkan.");
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

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("[Cart] ── Isi Keranjang ──────────────────────");
        sb.AppendLine($"  {"Nama Barang",-28} {"Harga",12}  {"Qty",5}  {"Subtotal",12}");

        foreach (CartEntry e in entries)
        {
            float sub = e.Item.Harga * e.Quantity;
            sb.AppendLine($"  {e.Item.namaItem,-28} {"Rp " + e.Item.Harga.ToString("N0"),12}  {e.Quantity,5}  {"Rp " + sub.ToString("N0"),12}");
        }

        sb.AppendLine($"  {"",28} {"",12}  {"",5}  {"──────────",12}");
        sb.AppendLine($"  {"TOTAL",-28} {"",12}  {"",5}  {"Rp " + GetTotal().ToString("N0"),12}");
        sb.AppendLine("[Cart] ─────────────────────────────────────");

        Debug.Log(sb.ToString());
    }

    // ── Build CartPayload untuk API POST ─────────────────────

    public CartPayload BuildPayload(string playerId)
    {
        var itemPayloads = new System.Collections.Generic.List<CartItemPayload>();
        foreach (CartEntry e in entries)
        {
            itemPayloads.Add(new CartItemPayload
            {
                namaItem = e.Item.namaItem,
                kategori = e.Item.kategori,
                harga = e.Item.Harga,
                quantity = e.Quantity
            });
        }

        return new CartPayload
        {
            playerId = playerId,
            items = itemPayloads,
            totalHarga = GetTotal(),
            timestamp = System.DateTime.UtcNow.ToString("o")
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