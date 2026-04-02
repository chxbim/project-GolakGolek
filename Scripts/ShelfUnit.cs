using UnityEngine;

/// <summary>
/// Enum semua jenis rak di scene.
/// Cocokkan dengan nama parent GameObject di hierarchy.
/// </summary>
public enum ShelfType
{
    JajanCiki,       // rak jajan ciki 1-4
    MinumanSusu,     // rak minuman susu 1-4
    PelengkapDapur,  // rak pelengkap dapur 1-2
    Kulkas,          // kulkas 1-2
    Showcase,        // showcase 1-2
    Vending          // vending 1
}

/// <summary>
/// Pasang di GameObject PARENT rak (misal: "rak jajan ciki1").
/// Parent sudah punya BoxCollider solid (blocker) — ShelfUnit numpang di sana.
/// Proximity diurus oleh ProximityDetector di child isTrigger.
///
/// Hierarchy yang benar:
///   rak jajan ciki1          <- ShelfUnit.cs di sini (parent)
///   └── isTrigger rak ...    <- ProximityDetector.cs di sini (child)
///
/// Ketika player tap, raycast kena collider parent atau child ->
/// GetComponentInParent naik ke parent -> ketemu ShelfUnit -> Interact().
/// </summary>
public class ShelfUnit : MonoBehaviour, IInteractable
{
    // ── Inspector ────────────────────────────────────────────

    [Header("Shelf Identity")]
    [SerializeField] private int shelfId = 1;
    [SerializeField] private ShelfType shelfType = ShelfType.JajanCiki;

    [Header("Fallback (testing tanpa JSON)")]
    [Tooltip("Dipakai saat ShelfManager belum inject data dari JSON/API.")]
    [SerializeField] private string fallbackNamaItem = "Item Test";
    [SerializeField] private float fallbackHarga = 5000f;
    [SerializeField] private string fallbackKategori = "Umum";

    // ── Runtime ──────────────────────────────────────────────

    public GameItemData ItemData { get; private set; }

    private bool playerInRange = false;

    // ── IInteractable ────────────────────────────────────────

    public string DisplayName => $"Rak {shelfId} ({shelfType})";

    public void Interact()
    {
        if (!playerInRange)
        {
            Debug.Log($"[ShelfUnit] Player belum dalam jangkauan {DisplayName}.");
            return;
        }

        if (ItemData == null)
        {
            Debug.LogWarning($"[ShelfUnit] {DisplayName} belum ada item data.");
            return;
        }

        Debug.Log(
            "──────────────────────────────────────\n" +
            "[ShelfUnit] Player ambil item!\n" +
            $"  Shelf ID    : {shelfId}\n" +
            $"  Tipe Rak    : {shelfType}\n" +
            $"  Nama Barang : {ItemData.namaItem}\n" +
            $"  Kategori    : {ItemData.kategori}\n" +
            $"  Varian      : {ItemData.varian}\n" +
            $"  Harga       : Rp {ItemData.Harga:N0}\n" +
            "──────────────────────────────────────"
        );

        CartSystem.Instance?.AddItem(ItemData);
    }

    // ── Dipanggil oleh ProximityDetector (child) ─────────────

    /// <summary>
    /// Dipanggil ProximityDetector saat player masuk/keluar isTrigger child.
    /// </summary>
    public void SetPlayerInRange(bool inRange)
    {
        playerInRange = inRange;
        Debug.Log($"[ShelfUnit] {DisplayName} -> player in range: {inRange}");
    }

    // ── Dipanggil oleh ShelfManager ──────────────────────────

    public void SetItemData(GameItemData data)
    {
        ItemData = data;
        Debug.Log($"[ShelfUnit] {DisplayName} -> item diset: {data}");
    }

    // ── Lifecycle ─────────────────────────────────────────────

    private void Start()
    {
        if (ItemData == null)
        {
            ItemData = new GameItemData
            {
                namaItem = fallbackNamaItem,
                hargaRaw = fallbackHarga.ToString(),
                kategori = fallbackKategori,
                varian = "-",
                modelType = "Unknown"
            };
            Debug.Log($"[ShelfUnit] {DisplayName} pakai fallback: {ItemData}");
        }
    }

    // ── Gizmo ─────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = playerInRange
            ? new Color(0.2f, 1f, 0.3f, 0.25f)
            : new Color(1f, 0.85f, 0.1f, 0.1f);

        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}