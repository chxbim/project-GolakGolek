using UnityEngine;

public enum ShelfType
{
    Rak_01,
    Rak_02,
    Rak_03,
    Kulkas_01,
    Kulkas_02
}

public class ShelfUnit : MonoBehaviour, IInteractable
{
    // ── Inspector ────────────────────────────────────────────

    [Header("Shelf Identity")]
    [SerializeField] private int shelfId = 0;
    // PENTING: shelfId harus sama dengan urutan_rak di API
    // urutan_rak 0 = Saus Berisik, 1 = Saus Huha, dst
    [SerializeField] private ShelfType shelfType = ShelfType.Rak_01;

    [Header("Fallback — dipakai hanya jika API tidak bisa dijangkau")]
    [SerializeField] private string fallbackNamaItem = "Item Test";
    [SerializeField] private float fallbackHarga = 5000f;
    [SerializeField] private string fallbackKategori = "Umum";

    // ── Runtime ──────────────────────────────────────────────

    public GameItemData ItemData { get; private set; }
    public int ShelfId => shelfId;          // dibaca ShelfManager
    public string DisplayName => $"Rak {shelfId} ({shelfType})";

    private bool playerInRange = false;

    // ── IInteractable ────────────────────────────────────────

    public void Interact()
    {
        if (!playerInRange)
        {
            Debug.Log($"[ShelfUnit] Player belum dalam jangkauan {DisplayName}.");
            return;
        }

        if (ItemData == null)
        {
            Debug.LogWarning($"[ShelfUnit] {DisplayName} tidak ada ItemData.");
            return;
        }

        Debug.Log(
            "──────────────────────────────────────\n" +
            "[ShelfUnit] Player ambil item!\n" +
            $"  Shelf ID    : {shelfId}\n" +
            $"  Tipe Rak    : {shelfType}\n" +
            $"  Nama Barang : {ItemData.namaItem}\n" +
            $"  Kategori    : {ItemData.kategoriBarang}\n" +
            $"  Varian      : {ItemData.varian}\n" +
            $"  FBX         : {ItemData.objectFileName}\n" +
            $"  Harga       : Rp {ItemData.Harga:N0}\n" +
            "──────────────────────────────────────"
        );

        CartSystem.Instance?.AddItem(ItemData);
    }

    // ── Dipanggil ShelfManager setelah API fetch ─────────────

    public void SetItemData(GameItemData data)
    {
        ItemData = data;
        Debug.Log($"[ShelfUnit] {DisplayName} → data dari API: {data}");
    }

    // ── Dipanggil ProximityDetector (child) ──────────────────

    public void SetPlayerInRange(bool inRange)
    {
        playerInRange = inRange;
        Debug.Log($"[ShelfUnit] {DisplayName} → player in range: {inRange}");
    }

    // ── Lifecycle ─────────────────────────────────────────────

    private void Start()
    {
        // Fallback hanya jalan kalau API gagal dan SetItemData gagal dipanggil.
        // Karena API fetch async, Start() jalan duluan — fallback ini akan
        // di-override oleh SetItemData() begitu ShelfManager selesai fetch.
        if (ItemData == null)
        {
            ItemData = new GameItemData
            {
                namaItem = fallbackNamaItem,
                hargaRaw = fallbackHarga.ToString(),
                kategoriBarang = fallbackKategori,
                varian = "-",
                objectFileName = "Unknown"
            };
            Debug.Log($"[ShelfUnit] {DisplayName} → fallback sementara (menunggu API)...");
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