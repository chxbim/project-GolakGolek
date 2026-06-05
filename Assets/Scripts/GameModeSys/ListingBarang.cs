//manggil variabel item dari GameItemData
//semua barang punya display name.
//randomize?
//[namaItem][varian]: GameMode TimeAttacks
//[displayName]: GameMode Golek

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

    // Dipanggil dari DataManager/APIManager setelah fetch selesai
    public void InitDariDatabase(List<GameItemData> semuaItem)
    {
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

        // Ambil mode langsung dari GameSession — sumber of truth nya
        shoppingListUI.TampilkanList(itemAktif, GameSession.SelectedMode);
    }

    // Dipanggil ShelfUnit saat player ambil barang
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
        // GameModeController akan handle win condition via CartSystem
    }
}