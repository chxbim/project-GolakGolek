using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Mengatur tampilan daftar belanja di ListingUI.
/// Pakai slot fixed (List1–List5 + List1Selesai–List5Selesai) sesuai hierarchy.
///
/// PENTING — cara assign di Inspector:
///   labelSlots[0–4]   → drag komponen TextMeshProUGUI dari List1–List5
///                        (bukan GameObject-nya, tapi komponen TMP-nya)
///   centangSlots[0–4] → drag GameObject List1Selesai–List5Selesai
/// </summary>
public class ShoppingListUI : MonoBehaviour
{
    [Header("Label Slots — drag komponen TMP dari List1–List5")]
    public TextMeshProUGUI[] labelSlots = new TextMeshProUGUI[5];

    [Header("Centang Slots — drag GameObject List1Selesai–List5Selesai")]
    public GameObject[] centangSlots = new GameObject[5];

    // item.id → index slot, untuk lookup O(1) di SetCentang
    private Dictionary<string, int> idToSlot = new();

    void Awake()
    {
        // Pastikan semua centang hidden dari awal, tanpa tunggu TampilkanList
        for (int i = 0; i < centangSlots.Length; i++)
        {
            if (centangSlots[i] != null)
                centangSlots[i].SetActive(false);
        }
    }

    /// <summary>
    /// Dipanggil ListingBarang setelah randomize objective.
    /// Hanya update TEXT — tidak mematikan GameObject List1–List5.
    /// Centang di-reset semua ke hidden.
    /// </summary>
    public void TampilkanList(List<GameItemData> items, GameMode mode)
    {
        idToSlot.Clear();

        for (int i = 0; i < labelSlots.Length; i++)
        {
            // Reset centang
            if (centangSlots[i] != null)
                centangSlots[i].SetActive(false);

            if (labelSlots[i] == null)
            {
                Debug.LogWarning($"[ShoppingListUI] labelSlots[{i}] belum di-assign di Inspector!");
                continue;
            }

            if (i < items.Count)
            {
                // Slot terpakai: set text sesuai mode
                labelSlots[i].text = GetLabel(items[i], mode);
                idToSlot[items[i].id] = i;

                Debug.Log($"[ShoppingListUI] Slot {i} → \"{labelSlots[i].text}\" (id: {items[i].id})");
            }
            else
            {
                // Slot tidak terpakai: kosongkan text (jangan matikan GameObject)
                labelSlots[i].text = string.Empty;
            }
        }
    }

    /// <summary>
    /// Aktifkan centang untuk item yang sudah berhasil diambil player.
    /// Dipanggil ListingBarang.CekDanCentangItem().
    /// </summary>
    public void SetCentang(string itemId, bool aktif)
    {
        if (!idToSlot.TryGetValue(itemId, out int idx))
        {
            Debug.LogWarning($"[ShoppingListUI] SetCentang: itemId '{itemId}' tidak ada di idToSlot.");
            return;
        }

        if (centangSlots[idx] != null)
            centangSlots[idx].SetActive(aktif);
    }

    private string GetLabel(GameItemData item, GameMode mode)
    {
        return mode switch
        {
            GameMode.Golek => item.displayname,
            GameMode.TimeAttack => string.IsNullOrEmpty(item.varian)
                                    ? item.namaItem
                                    : $"{item.namaItem} {item.varian}",
            _ => item.namaItem
        };
    }
}