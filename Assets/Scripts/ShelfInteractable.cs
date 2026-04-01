using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────
//  DATA TYPES
// ─────────────────────────────────────────────────────────────


[System.Serializable]
public class ShelfItem
{
    [Tooltip("Nama produk yang tampil di rak")]
    public string itemName = "Produk";

    [Tooltip("Harga satuan dalam Rupiah")]
    public float price = 5000f;

    /// <summary>Jumlah kali player mengambil item ini. Tidak perlu diisi di Inspector.</summary>
    [HideInInspector] public int takeCount = 0;
}

// ─────────────────────────────────────────────────────────────
//  SHELF INTERACTABLE
//  Pasang script ini di GameObject cube (isTrigger = true)
//  yang menutupi area depan rak.
//
//  Setup Inspector:
//    • Shelf Number  → nomor rak (misal: 1, 2, 3, 4)
//    • Shelf Type    → enum sesuai jenis rak
//    • Items         → list barang + harga
//    • Interact Layer → layer cube (biar bisa di-raycast)
// ─────────────────────────────────────────────────────────────

[RequireComponent(typeof(Collider))]
public class ShelfInteractable : MonoBehaviour, IInteractable
{
    // ── Inspector ───────────────────────────────────────────

    [Header("Shelf Identity")]
    [SerializeField] private int shelfNumber = 1;
    [SerializeField] private ShelfType shelfType = ShelfType.JajanKering;

    [Header("Items on this Shelf")]
    [SerializeField] private List<ShelfItem> items = new List<ShelfItem>();

    [Header("Settings")]
    [Tooltip("Index item yang diambil saat player interact (0 = item pertama).\n" +
             "Ganti ke -1 untuk auto-pilih item pertama yang stok-nya ada.")]
    [SerializeField] private int defaultPickIndex = 0;

    // ── State ────────────────────────────────────────────────

    private bool playerInRange = false;

    // ── IInteractable ────────────────────────────────────────

    public string DisplayName => $"Rak {shelfNumber} ({shelfType})";

    /// <summary>
    /// Dipanggil oleh PlayerController.TryInteract() ketika raycast kena collider ini.
    /// </summary>
    public void Interact()
    {
        if (!playerInRange)
        {
            Debug.Log($"[Shelf] Player belum berada di dekat {DisplayName}.");
            return;
        }

        if (items == null || items.Count == 0)
        {
            Debug.LogWarning($"[Shelf] {DisplayName} tidak punya item sama sekali!");
            return;
        }

        // ── Tentukan item yang diambil ──────────────────────
        int pickIdx = defaultPickIndex;

        // Jika -1, cari item pertama yang belum "habis" (opsional, bisa kamu extend)
        if (pickIdx < 0 || pickIdx >= items.Count)
            pickIdx = 0;

        ShelfItem picked = items[pickIdx];
        picked.takeCount++;

        // ── Log ke Console ──────────────────────────────────
        Debug.Log(
            $"─────────────────────────────────────\n" +
            $"[Shelf Interact]\n" +
            $"  Nomor Rak   : {shelfNumber}\n" +
            $"  Tipe Rak    : {shelfType}\n" +
            $"  Barang      : {picked.itemName}\n" +
            $"  Harga       : Rp {picked.price:N0}\n" +
            $"  Diambil ke  : {picked.takeCount}x\n" +
            $"─────────────────────────────────────"
        );

        // ── (Opsional) Log semua isi rak ───────────────────
        PrintShelfContents();
    }

    // ── Proximity Detection ──────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        Debug.Log($"[Shelf] Player masuk jangkauan → {DisplayName}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        Debug.Log($"[Shelf] Player keluar dari jangkauan → {DisplayName}");
    }

    // ── Utility ──────────────────────────────────────────────

    /// <summary>Cetak semua item di rak ini ke Console.</summary>
    public void PrintShelfContents()
    {
        if (items == null || items.Count == 0)
        {
            Debug.Log($"[Shelf] {DisplayName} kosong.");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"[Shelf] Isi {DisplayName}:");
        sb.AppendLine($"  {"No",-4} {"Nama Barang",-25} {"Harga",12}  {"Diambil",10}");
        sb.AppendLine($"  {"──",-4} {"─────────────────────────",-25} {"──────────",12}  {"───────",10}");

        for (int i = 0; i < items.Count; i++)
        {
            ShelfItem it = items[i];
            sb.AppendLine($"  {i + 1,-4} {it.itemName,-25} {"Rp " + it.price.ToString("N0"),12}  {it.takeCount + "x",10}");
        }

        Debug.Log(sb.ToString());
    }

    /// <summary>Reset semua take count (misal untuk sesi baru).</summary>
    public void ResetAllTakeCounts()
    {
        foreach (ShelfItem item in items)
            item.takeCount = 0;

        Debug.Log($"[Shelf] {DisplayName} – semua takeCount direset.");
    }

    // ── Gizmo (tampil di Scene View) ─────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = playerInRange
            ? new Color(0f, 1f, 0f, 0.25f)
            : new Color(1f, 1f, 0f, 0.15f);

        Collider col = GetComponent<Collider>();
        if (col != null)
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}