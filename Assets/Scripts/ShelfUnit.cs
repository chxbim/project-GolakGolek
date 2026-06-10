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
    public static event System.Action<ShelfUnit, bool> OnPlayerRangeChanged;

    // ── Inspector ────────────────────────────────────────────

    [Header("Shelf Identity")]
    [SerializeField] private int shelfId = 0;
    [SerializeField] public ShelfType shelfType = ShelfType.Rak_01;

    [Header("Btn ItemDiambil")]
    public ItemInteractUI itemInteractUI;

    [Header("Fallback — dipakai hanya jika API tidak bisa dijangkau")]
    [SerializeField] private string fallbackNamaItem = "Item Test";
    [SerializeField] private float fallbackHarga = 5000f;
    [SerializeField] private string fallbackKategori = "Umum";

    // ── Runtime ──────────────────────────────────────────────

    public GameItemData ItemData { get; private set; }
    private GameItemData currentItemData;

    public int ShelfId => shelfId;
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
            $"  Nama Barang : {ItemData.displayname}\n" +
            $"  Kategori    : {ItemData.kategoriBarang}\n" +
            $"  Varian      : {ItemData.varian}\n" +
            $"  Harga       : Rp {ItemData.Harga:N0}\n" +
            "──────────────────────────────────────"
        );

        // Cek dulu ke ListingBarang — kalau match, dia yang centang + trigger cart
        var listing = FindFirstObjectByType<ListingBarang>();
        if (listing == null)
        {
            Debug.LogError("[ShelfUnit] ListingBarang tidak ditemukan!");
            return;
        }

        bool cocok = listing.CekDanCentangItem(ItemData.id);

        if (cocok)
        {
            CartSystem.Instance?.AddItem(ItemData);
            Debug.Log($"[ShelfUnit] ✅ {ItemData.namaItem} cocok dengan list — masuk cart.");
        }
        else
        {
            Debug.Log($"[ShelfUnit] ❌ {ItemData.namaItem} tidak ada di list objective atau sudah diambil.");
        }
    }

    // ── Dipanggil ShelfManager setelah API fetch ─────────────

    public void SetItemData(GameItemData data)
    {
        ItemData = data;
        currentItemData = data;   // ← fix: sync dua-duanya
        Debug.Log($"[ShelfUnit] {DisplayName} → data dari API: {data}");
    }

    // ── Dipanggil ProximityDetector (child) ──────────────────

    public void SetPlayerInRange(bool inRange)
    {
        playerInRange = inRange;
        Debug.Log($"[ShelfUnit] {DisplayName} → player in range: {inRange}");

        OnPlayerRangeChanged?.Invoke(this, inRange);

        if (itemInteractUI == null) return;   // ← guard di ATAS, satu kali

        if (inRange && currentItemData != null)
            itemInteractUI.ShowForShelf(this, currentItemData);
        else
            itemInteractUI.Hide();
    }

    // ── Lifecycle ─────────────────────────────────────────────

    private void Start()
    {
        if (ItemData == null)
        {
            var fallback = new GameItemData
            {
                namaItem = fallbackNamaItem,
                hargaRaw = fallbackHarga.ToString(),
                kategoriBarang = fallbackKategori,
                varian = "-",
                objectFileName = "Unknown"
            };
            ItemData = fallback;
            currentItemData = fallback;   // ← fix: currentItemData juga dapat fallback
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