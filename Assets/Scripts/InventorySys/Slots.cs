using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slots : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool hovering;
    private GameItemData holdItem;   // ← was: ItemSO
    private int itemAmount;

    private Image iconImage;
    private TextMeshProUGUI amountTxt;

    private void Awake()
    {
        iconImage = transform.GetChild(0).GetComponent<Image>();
        amountTxt = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
    }

    public GameItemData GetItem() => holdItem;   // ← was: ItemSO
    public int GetAmount() => itemAmount;

    public void SetItem(GameItemData item, int amount = 1)   // ← was: ItemSO
    {
        holdItem = item;
        itemAmount = amount;
        UpdateSlot();
    }

    public void UpdateSlot()
    {
        if (holdItem != null)
        {
            // icon bisa null kalau belum di-assign oleh IconRegistry —
            // dalam kasus itu slot tetap tampil tapi tanpa gambar
            iconImage.enabled = holdItem.icon != null;
            if (holdItem.icon != null)
                iconImage.sprite = holdItem.icon;

            amountTxt.text = itemAmount > 1 ? itemAmount.ToString() : "";
        }
        else
        {
            iconImage.enabled = false;
            amountTxt.text = "";
        }
    }

    public int AddAmount(int amountToAdd)
    {
        itemAmount += amountToAdd;
        UpdateSlot();
        return itemAmount;
    }

    public int RemoveAmount(int amountToRemove)
    {
        itemAmount -= amountToRemove;
        if (itemAmount <= 0)
            ClearSlot();
        else
            UpdateSlot();
        return itemAmount;
    }

    public void ClearSlot()
    {
        holdItem = null;
        itemAmount = 0;
        UpdateSlot();
    }

    public bool HasItem() => holdItem != null;

    public void OnPointerEnter(PointerEventData eventData) => hovering = true;
    public void OnPointerExit(PointerEventData eventData) => hovering = false;
}