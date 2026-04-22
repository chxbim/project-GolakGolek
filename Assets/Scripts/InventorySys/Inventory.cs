using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public ItemSO TestItem;
    public GameObject inventorySlotParent;

    private List<Slots> inventorySlots = new List<Slots>();
    private List<Slots> allSlots = new List<Slots>();

    private void Awake()
    {
        inventorySlots.AddRange(inventorySlotParent.GetComponentInChildren<Slots>());

        allSlots.AddRange(inventorySlots);
    }

    private void Update()
    {
        //aku gtw ini dikasih apa untuk system add barang (seenggaknya dari TestItem dulu)
        //well... total item e pasti uakeh se..demn..
    }

    public void AddItem(ItemSO itemToAdd, int amount)
    {
        int remaining = amount;

        foreach (Slots slot in allSlots)
        {
            if (slot.HasItem() && slot.GetItem() == itemToAdd)
            {
                int currentAmount = slot.GetAmount();
                int maxStack = itemToAdd.maxStackSize;

                if (currentAmount > maxStack)
                {
                    int spaceLeft = maxStack - currentAmount;
                    int amountToAdd = Mathf.Min(spaceLeft, remaining);

                    slot.SetItem(itemToAdd, currentAmount + amountToAdd);
                    remaining -= amountToAdd;

                    if (remaining <= 0)
                        return;
                }
            }
        }

        foreach(Slots slot in allSlots)
        {
            if (!slot.HasItem())
            {
                int amountToPlace = Mathf.Min(itemToAdd.maxStackSize, remaining);
                slot.SetItem(itemToAdd, amountToPlace);

                if(remaining <= 0)
                {
                    Debug.Log("Inventory penuh, tidak bisa menambahkan " + remaining + itemToAdd.namaItem); //refer ke GameItemData coba nnti
                }
            }
        }
    }
}
