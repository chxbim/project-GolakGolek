using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auto-discover semua ShelfUnit di scene.
/// Cocokkan shelfId (Inspector) dengan urutan_rak (API).
/// Sekaligus panggil ItemSpawner di GameObject yang sama jika ada.
/// </summary>
public class ShelfManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool logAllItemsOnFetch = true;

    private void Start()
    {
        if (APIManager.Instance == null)
        {
            Debug.LogError("[ShelfManager] APIManager tidak ada di scene!");
            return;
        }
        APIManager.Instance.FetchItems(OnItemsFetched, OnFetchError);
    }

    private void OnItemsFetched(List<GameItemData> items)
    {
        if (logAllItemsOnFetch)
        {
            Debug.Log($"[ShelfManager] Fetch berhasil — {items.Count} item:");
            foreach (var item in items)
                Debug.Log($"   • {item}");
        }

        // Build lookup: urutan_rak → GameItemData
        var lookup = new Dictionary<int, GameItemData>();
        foreach (var item in items)
        {
            if (!lookup.ContainsKey(item.urutanRak))
                lookup[item.urutanRak] = item;
            else
                Debug.LogWarning($"[ShelfManager] urutan_rak {item.urutanRak} duplikat — '{item.namaItem}' diskip.");
        }

        // Auto-discover semua ShelfUnit
        ShelfUnit[] allShelves = FindObjectsOfType<ShelfUnit>();
        Debug.Log($"[ShelfManager] Ditemukan {allShelves.Length} ShelfUnit di scene.");

        int matched = 0;
        foreach (ShelfUnit shelf in allShelves)
        {
            if (!lookup.TryGetValue(shelf.ShelfId, out GameItemData data))
            {
                Debug.LogWarning($"[ShelfManager] ShelfUnit '{shelf.DisplayName}' (id={shelf.ShelfId}) " +
                                 "tidak ada padanannya di API — pakai fallback.");
                continue;
            }

            // Inject ke ShelfUnit (interaksi player)
            shelf.SetItemData(data);

            // Inject ke ItemSpawner di GameObject yang sama (placement mesh)
            // ItemSpawner opsional — tidak error kalau tidak ada
            IItemSpawner spawner = shelf.GetComponent<IItemSpawner>();
            if (spawner != null)
                spawner.SetItemDataAndSpawn(data);

            matched++;
        }

        Debug.Log($"[ShelfManager] {matched}/{allShelves.Length} ShelfUnit berhasil dapat data API.");
    }

    private void OnFetchError(string error)
    {
        Debug.LogError($"[ShelfManager] Gagal fetch API: {error}\n" +
                       "Semua ShelfUnit pakai fallback Inspector.");
    }
}