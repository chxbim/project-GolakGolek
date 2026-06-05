using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShoppingListUI : MonoBehaviour
{
    [Header("References")]
    public GameObject itemRowPrefab;
    public Transform listContainer;

    private Dictionary<string, GameObject> rowMap = new();

    public void TampilkanList(List<GameItemData> items, GameMode mode)
    {
        foreach (Transform child in listContainer)
            Destroy(child.gameObject);
        rowMap.Clear();

        foreach (var item in items)
        {
            var row = Instantiate(itemRowPrefab, listContainer);

            var label = row.GetComponentInChildren<TextMeshProUGUI>();
            label.text = GetLabel(item, mode);

            var centang = row.transform.Find("Centang")?.gameObject;
            if (centang != null) centang.SetActive(false);

            rowMap[item.id] = row;
        }
    }

    private string GetLabel(GameItemData item, GameMode mode)
    {
        return mode switch
        {
            // Golekno: pakai riddle name
            GameMode.Golek => item.displayName,

            // TimeAttack: namaItem + varian, trim kalau varian kosong
            GameMode.TimeAttack => string.IsNullOrEmpty(item.varian)
                                    ? item.namaItem
                                    : $"{item.namaItem} {item.varian}",

            _ => item.namaItem
        };
    }

    public void SetCentang(string itemId, bool aktif)
    {
        if (!rowMap.TryGetValue(itemId, out var row)) return;

        var centang = row.transform.Find("Centang")?.gameObject;
        if (centang != null) centang.SetActive(aktif);
    }
}