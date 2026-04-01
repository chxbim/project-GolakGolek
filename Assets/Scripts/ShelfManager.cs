using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrator: fetch data dari API, lalu assign ke masing-masing ShelfUnit.
///
/// Setup Inspector:
///   • Klik "+" di list Shelf Assignments
///   • Tiap entry: shelfNumber (cocokkan dengan ShelfUnit) + namaItemKey
///     (nama item di API yang mau diassign ke rak itu)
///
/// Cara pakai:
///   1. Pasang ShelfManager di scene (1x, boleh di GameObject "Managers")
///   2. Assign semua ShelfUnit yang ada di scene via Inspector
///   3. Jalankan game → ShelfManager fetch API → distribute ke rak
/// </summary>
public class ShelfManager : MonoBehaviour
{
    [System.Serializable]
    public class ShelfAssignment
    {
        [Tooltip("Cocokkan dengan shelfNumber di ShelfUnit")]
        public int shelfNumber;

        [Tooltip("Nama item dari API (field 'Nama Item') yang diassign ke rak ini")]
        public string namaItemKey;

        [Tooltip("Drag ShelfUnit dari scene ke sini")]
        public ShelfUnit targetShelf;
    }

    [Header("Shelf Assignments")]
    [SerializeField] private List<ShelfAssignment> assignments = new List<ShelfAssignment>();

    [Header("Debug")]
    [SerializeField] private bool logAllItemsOnFetch = true;

    // ── Lifecycle ────────────────────────────────────────────

    private void Start()
    {
        if (APIManager.Instance == null)
        {
            Debug.LogError("[ShelfManager] APIManager tidak ada di scene!");
            return;
        }

        APIManager.Instance.FetchItems(OnItemsFetched, OnFetchError);
    }

    // ── Callbacks ────────────────────────────────────────────

    private void OnItemsFetched(List<GameItemData> items)
    {
        if (logAllItemsOnFetch)
        {
            Debug.Log($"[ShelfManager] Fetch berhasil, total {items.Count} item:");
            foreach (GameItemData item in items)
                Debug.Log($"   • {item}");
        }

        // Build lookup dictionary: NamaItem → GameItemData
        Dictionary<string, GameItemData> lookup = new Dictionary<string, GameItemData>();
        foreach (GameItemData item in items)
        {
            if (!string.IsNullOrEmpty(item.namaItem))
                lookup[item.namaItem] = item;
        }

        // Distribute ke ShelfUnit sesuai assignment
        foreach (ShelfAssignment assignment in assignments)
        {
            if (assignment.targetShelf == null)
            {
                Debug.LogWarning($"[ShelfManager] Rak #{assignment.shelfNumber}: targetShelf belum diassign di Inspector!");
                continue;
            }

            if (lookup.TryGetValue(assignment.namaItemKey, out GameItemData data))
            {
                assignment.targetShelf.SetItemData(data);
            }
            else
            {
                Debug.LogWarning($"[ShelfManager] Item '{assignment.namaItemKey}' tidak ditemukan di API response.");
            }
        }
    }

    private void OnFetchError(string error)
    {
        Debug.LogError($"[ShelfManager] Gagal fetch items: {error}\n" +
                       "ShelfUnit akan jatuh ke fallback data yang diset di Inspector.");
    }
}