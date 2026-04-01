using System.Collections.Generic;
using UnityEngine;

public enum ShelfType
{
    JajanKering,   // Rak 1 – snack kering
    Kulkas,        // Rak 2 – minuman dingin
    Showcase,      // Rak 3 – minuman showcase
    RakSusu        // Rak 4 – susu & dairy
}

/// <summary>
/// Script utama rak. Pasang di TriggerZone cube (isTrigger = true).
///
/// Alur:
///   1. APIManager.FetchItems() selesai → ShelfManager mendistribusi GameItemData ke tiap ShelfUnit
///   2. Player masuk TriggerZone → playerInRange = true
///   3. Player tap → PlayerController.TryInteract() → raycast kena collider ini
///      → ShelfUnit.Interact() → CartSystem.AddItem()
///
/// Setup Inspector:
///   • Shelf Number    → nomor rak (1-4)
///   • Shelf Type      → enum jenis rak
///   • Item (runtime)  → diisi otomatis oleh ShelfManager, ATAU manual di Inspector
///                        untuk testing sebelum API ready
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShelfUnit : MonoBehaviour, IInteractable
{
    // ── Inspector ────────────────────────────────────────────

    [Header("Shelf Identity")]
    [SerializeField] private int shelfNumber = 1;
    [SerializeField] private ShelfType shelfType = ShelfType.JajanKering;

    [Header("Fallback / Manual (untuk testing)")]
    [Tooltip("Isi manual jika ingin test tanpa API. Dikosongkan saat ShelfManager inject dari API.")]
    [SerializeField] private string fallbackNamaItem = "Item Test";
    [SerializeField] private float fallbackHarga = 5000f;
    [SerializeField] private string fallbackKategori = "Makanan";

    // ── Runtime ──────────────────────────────────────────────

    /// <summary>Item yang ada di rak ini. Diisi oleh ShelfManager atau manual testing.</summary>
    public GameItemData ItemData { get; private set; }

    private bool playerInRange = false;

    // ── IInteractable ────────────────────────────────────────

    public string DisplayName => $"Rak {shelfNumber} ({shelfType})";

    public void Interact()
    {
        if (!playerInRange)
        {
            Debug.Log($"[ShelfUnit] Player tidak dalam jangkauan {DisplayName}.");
            return;
        }

        if (ItemData == null)
        {
            Debug.LogWarning($"[ShelfUnit] {DisplayName} belum punya item data! " +
                             "Tunggu API load atau set fallback.");
            return;
        }

        // ── Log ke console ──────────────────────────────────
        Debug.Log(
            $"──────────────────────────────────────\n" +
            $"[ShelfUnit] Player ambil item!\n" +
            $"  Nomor Rak   : {shelfNumber}\n" +
            $"  Tipe Rak    : {shelfType}\n" +
            $"  Nama Barang : {ItemData.namaItem}\n" +
            $"  Kategori    : {ItemData.kategori}\n" +
            $"  Model Type  : {ItemData.modelType}\n" +
            $"  Harga       : Rp {ItemData.Harga:N0}\n" +
            $"──────────────────────────────────────"
        );

        // ── Kirim ke CartSystem ─────────────────────────────
        if (CartSystem.Instance != null)
            CartSystem.Instance.AddItem(ItemData);
        else
            Debug.LogError("[ShelfUnit] CartSystem tidak ditemukan di scene!");
    }

    // ── Item Injection (dari ShelfManager) ───────────────────

    /// <summary>
    /// Dipanggil oleh ShelfManager setelah API fetch selesai.
    /// </summary>
    public void SetItemData(GameItemData data)
    {
        ItemData = data;
        Debug.Log($"[ShelfUnit] {DisplayName} → item diset: {data}");
    }

    // ── Init Fallback ─────────────────────────────────────────

    private void Start()
    {
        // Jika belum ada data dari API, pakai fallback untuk testing
        if (ItemData == null)
        {
            ItemData = new GameItemData
            {
                namaItem = fallbackNamaItem,
                hargaRaw = fallbackHarga.ToString(),
                kategori = fallbackKategori,
                modelType = "Unknown",
                sausVarian = ""
            };
            Debug.Log($"[ShelfUnit] {DisplayName} menggunakan fallback item: {ItemData}");
        }
    }

    // ── Proximity ─────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        Debug.Log($"[ShelfUnit] Player masuk → {DisplayName}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        Debug.Log($"[ShelfUnit] Player keluar → {DisplayName}");
    }

    // ── Gizmo ─────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = playerInRange
            ? new Color(0.2f, 1f, 0.3f, 0.2f)
            : new Color(1f, 0.8f, 0.1f, 0.1f);

        Collider col = GetComponent<Collider>();
        if (col != null)
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}