using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// fetch data dari API,lalu assign ke ShelfUnit berdasarkan urutan_rak.
///
/// Setup Inspector:
///   • Isi list Shelf Units — drag semua ShelfUnit dari scene
///   • Index di list = urutan_rak dari API (0-based)
///     Contoh: index 0 → rak "Saus Berisik" (urutan_rak: 0)
///             index 1 → rak "Saus Huha"    (urutan_rak: 1)
/// </summary>
public class ShelfManager : MonoBehaviour
{
    [Header("Drag semua ShelfUnit dari scene — urut sesuai urutan_rak API")]
    [SerializeField] private List<ShelfUnit> shelfUnits = new List<ShelfUnit>();

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

        // Build dictionary: urutanRak → GameItemData
        var lookup = new Dictionary<int, GameItemData>();
        foreach (var item in items)
        {
            if (!lookup.ContainsKey(item.urutanRak))
                lookup[item.urutanRak] = item;
            else
                Debug.LogWarning($"[ShelfManager] urutan_rak {item.urutanRak} duplikat — '{item.namaItem}' diskip.");
        }

        // Assign ke ShelfUnit — index list = urutanRak
        for (int i = 0; i < shelfUnits.Count; i++)
        {
            if (shelfUnits[i] == null)
            {
                Debug.LogWarning($"[ShelfManager] ShelfUnit index {i} null, skip.");
                continue;
            }

            if (lookup.TryGetValue(i, out GameItemData data))
            {
                shelfUnits[i].SetItemData(data);
            }
            else
            {
                Debug.LogWarning($"[ShelfManager] Tidak ada item dengan urutan_rak {i} " +
                                 $"untuk '{shelfUnits[i].DisplayName}'. Pakai fallback.");
            }
        }
    }

    private void OnFetchError(string error)
    {
        Debug.LogError($"[ShelfManager] Gagal fetch: {error}\n" +
                       "Semua ShelfUnit akan pakai fallback data dari Inspector.");
    }
}