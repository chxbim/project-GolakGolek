using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Muncul saat player masuk proximity zone rak.
/// Nampilin nama item di dalam teks BtnAmbil sesuai GameMode.
///
/// ----------------------------------------------------------------
/// HIERARCHY (di PlayerCanvas, default: inactive):
///
///   ItemInteractUI        ← GameObject, pasang script ini
///   └── Panel             ← Image background (opsional)
///       └── BtnAmbil      ← Button
///           └── [TMP]     ← TextMeshProUGUI, teks tombol sekaligus nama item
///
/// ----------------------------------------------------------------
/// INSPECTOR SETUP:
///   btnAmbil        → drag Button BtnAmbil
///   listingBarang   → drag ListingBarang dari Managers
///
/// ----------------------------------------------------------------
/// PATCH ShelfUnit.cs — tambahkan:
///   public ItemInteractUI itemInteractUI;
///
///   Di SetPlayerInRange(bool inRange):
///     if (inRange) itemInteractUI?.ShowForShelf(this, currentItemData);
///     else         itemInteractUI?.Hide();
/// </summary>
public class ItemInteractUI : MonoBehaviour
{
    [Header("References")]
    public Button btnAmbil;
    public ListingBarang listingBarang;

    private TextMeshProUGUI btnLabel;
    private ShelfUnit currentShelf;

    void Awake()
    {
        gameObject.SetActive(false);

        if (btnAmbil != null)
        {
            btnLabel = btnAmbil.GetComponentInChildren<TextMeshProUGUI>();
            btnAmbil.onClick.AddListener(OnAmbilPressed);
        }
        else
        {
            Debug.LogError("[ItemInteractUI] btnAmbil belum di-assign di Inspector!");
        }
    }

    public void ShowForShelf(ShelfUnit shelf, GameItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning($"[ItemInteractUI] itemData null dari {shelf?.name} — UI tidak ditampilkan.");
            return;
        }

        currentShelf = shelf;

        if (btnLabel != null)
            btnLabel.text = GetLabel(itemData, GameSession.SelectedMode);

        bool sudahDiambil = listingBarang != null && listingBarang.SudahDiambil(itemData.id);
        btnAmbil.interactable = !sudahDiambil;

        gameObject.SetActive(true);

        Debug.Log($"[ItemInteractUI] Tampil: \"{btnLabel?.text}\" | SudahDiambil: {sudahDiambil}");
    }

    public void Hide()
    {
        currentShelf = null;
        gameObject.SetActive(false);
    }

    private void OnAmbilPressed()
    {
        if (currentShelf == null) return;

        currentShelf.Interact();
        btnAmbil.interactable = false;
    }

    private string GetLabel(GameItemData item, GameMode mode)
    {
        return mode switch
        {
            GameMode.golek => item.displayname,
            GameMode.time_attack => string.IsNullOrEmpty(item.varian)
                                    ? item.namaItem
                                    : $"{item.namaItem} {item.varian}",
            _ => item.namaItem
        };
    }
}