using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Satis slotlarindaki itemleri toplayip toplam kazanci Muhasebeci uzerinden oyuncuya ekler.
/// </summary>
public class Sales_UI : MonoBehaviour
{
    [FormerlySerializedAs("inventory_u\u0131")]
    public Inventory_UI inventoryUI;
    public GameObject panel;
    public List<Slot_UI> saleSlots;
    public TextMeshProUGUI totalMoneyText;
    public Muhasebeci money;

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
        Debug.Log("ConfirmSale calisti!");
        if (saleSlots.Count == 0)
        {
            Debug.LogWarning("Satis yapilacak slot bulunamadi!");
            return;
        }

        int currentMoney = money.playerMoney;
        foreach (Slot_UI slot in saleSlots)
        {
            if (slot.IsEmpty())
            {
                continue;
            }

            int count = slot.inventorySlot.count;
            int pricePerItem = slot.inventorySlot.item.sellPrice;
            int slotTotal = count * pricePerItem;
            string itemId = NormalizeItemId(slot.inventorySlot.item.itemName);

            currentMoney += slotTotal;
            Debug.Log($"Satildi: {count} x {slot.inventorySlot.item.itemName} -> {slotTotal} TL");

            if (GameStateTracker.Instance != null && !string.IsNullOrWhiteSpace(itemId))
            {
                GameStateTracker.Instance.IncrementCount($"Sold_{itemId}", count);
            }

            slot.inventorySlot.Clear();
            slot.Clear();
            money.AddMoney(slotTotal);
        }

        totalEarnings = currentMoney;
    }

    private static string NormalizeItemId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
