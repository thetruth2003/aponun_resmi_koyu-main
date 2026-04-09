/*
Bu dosya su an aktif kullanilmayan eski NPC dukkan prototipini tutuyor.
Yeni sisteme tasinmadigi icin yorum blogu olarak birakildi.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPCShopU sinifi, oyuncu tarafindaki ilgili davranis veya veriyi yonetir.
/// </summary>
public class NPCShopUI : MonoBehaviour
{
    public GameObject panel;
    public List<Slot_UI> npcSlots;
    public Inventory playerInventory;
    public int slotCount = 10;

    private Inventory npcInventory;

    void Start()
    {
        npcInventory = new Inventory(slotCount);
        SeedShop();
        RefreshUI();
        panel.SetActive(false);
    }

    public void Open()
    {
        RefreshUI();
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    void RefreshUI()
    {
        for (int i = 0; i < npcSlots.Count; i++)
        {
            if (i < npcInventory.slots.Count && !npcInventory.slots[i].IsEmpty)
            {
                npcSlots[i].SetItem(npcInventory.slots[i]);
                npcSlots[i].onClick = () => TryBuyItem(npcInventory.slots[i]);
            }
            else
            {
                npcSlots[i].SetEmpty();
                npcSlots[i].onClick = null;
            }
        }
    }

    void TryBuyItem(Inventory.Slot slot)
    {
        int price = slot.item.sellPrice;

        if (GameManager.instance.player.money >= price)
        {
            GameManager.instance.player.DecreaseMoney(price);
            GameObject itemObj = GameObject.Instantiate(slot.item.itemPrefab);
            playerInventory.Add(itemObj.GetComponent<Item>());
            Debug.Log($"Satýn alýndý: {slot.item.itemName} - {price}?");
        }
        else
        {
            Debug.Log("Yetersiz para!");
        }
    }

    void SeedShop()
    {
        var itemDB = GameManager.instance.itemDatabase;
        for (int i = 0; i < slotCount; i++)
        {
            if (i < itemDB.items.Count)
            {
                var data = itemDB.items[i];
                npcInventory.slots[i].AddItem(data, data.itemName, data.icon, data.maxAllowed, data.itemPrefab, data.itemUsedPrefab);
            }
        }
    }
}
*/
