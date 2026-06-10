using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ListingBarang : MonoBehaviour
{
    [Header("References")]
    public ShoppingListUI shoppingListUI;

    [Header("Config")]
    public int jumlahItemPerSesi = 5;

    public List<GameItemData> itemAktif { get; private set; } = new();
    private HashSet<string> itemSudahDiambil = new();

    // Dipanggil ShelfManager setelah fetch + distribute ke ShelfUnit selesai
    public void InitDariDatabase(List<GameItemData> semuaItem)
    {
        if (semuaItem == null || semuaItem.Count == 0)
        {
            Debug.LogError("[ListingBarang] InitDariDatabase dipanggil dengan list kosong!");
            return;
        }

        if (shoppingListUI == null)
        {
            Debug.LogError("[ListingBarang] shoppingListUI belum di-assign di Inspector!");
            return;
        }

        Debug.Log($"[ListingBarang] InitDariDatabase: {semuaItem.Count} item masuk, ambil {jumlahItemPerSesi}");
        RandomizeListBelanja(semuaItem);
    }

    private void RandomizeListBelanja(List<GameItemData> semuaItem)
    {
        itemAktif.Clear();
        itemSudahDiambil.Clear();

        var pool = new List<GameItemData>(semuaItem);

        // Fisher-Yates shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        itemAktif = pool.Take(jumlahItemPerSesi).ToList();

        Debug.Log($"[ListingBarang] Mode: {GameSession.SelectedMode} — memanggil TampilkanList dengan {itemAktif.Count} item");
        shoppingListUI.TampilkanList(itemAktif, GameSession.SelectedMode);
    }

    public bool CekDanCentangItem(string itemId)
    {
        var item = itemAktif.FirstOrDefault(i => i.id == itemId);

        if (item == null) return false;
        if (itemSudahDiambil.Contains(itemId)) return false;

        itemSudahDiambil.Add(itemId);
        shoppingListUI.SetCentang(itemId, true);

        if (itemSudahDiambil.Count >= jumlahItemPerSesi)
            OnSemuaBarangTerkumpul();

        return true;
    }

    public bool SudahDiambil(string itemId) => itemSudahDiambil.Contains(itemId);

    private void OnSemuaBarangTerkumpul()
    {
        Debug.Log("[ListingBarang] Semua barang terkumpul!");
    }
}