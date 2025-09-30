using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class Sales_UI : MonoBehaviour
{
    public Inventory_UI inventory_uı; // Envanter UI referansı
    public GameObject panel;
    public List<Slot_UI> saleSlots;
    public TextMeshProUGUI totalMoneyText;
    public Muhasebeci money; // Para UI referansı    

    private int totalEarnings;

    public void Open()
    {
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }
    public void ConfirmSale()
    {
        Debug.Log("ConfirmSale çalıştı!");
        if (saleSlots.Count == 0)
        {
            Debug.LogWarning("Satış yapılacak slot bulunamadı!");
            return;
        }
        int currentMoney = money.playerMoney; // Mevcut para miktarını al
        foreach (var slot in saleSlots)
        {
            if (!slot.IsEmpty())
            {
                int count = slot.inventorySlot.count;
                int pricePerItem = slot.inventorySlot.item.sellPrice;
                int slotTotal = count * pricePerItem;

                currentMoney += slotTotal;

                Debug.Log($"✔ Satıldı: {count} × {slot.inventorySlot.item.itemName} → {slotTotal}₺");

                slot.inventorySlot.Clear(); // Gerçek envanteri temizle
                slot.Clear(); // UI'den de temizle
                money.AddMoney(slotTotal); // Yeni para sistemi ile para ekle
            }
        }
    }
}

