using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Slot_UI : MonoBehaviour
{
    public int slotID = -1;
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    public GameObject highlight;
    public Inventory.Slot slot; // Slot referansını ekliyoruz
    public Inventory inventory; // Inventory referansını ekliyoruz
    public Inventory.Slot inventorySlot; // inventorySlot referansı
    public string itemName; // Item ismi için özellik


    private void Awake()
    {
        // Null referans kontrolleri
        if (itemIcon == null)
        {
            Debug.LogError("Slot_UI: itemIcon atanmamış!");
        }
        if (quantityText == null)
        {
            Debug.LogError("Slot_UI: quantityText atanmamış!");
        }
    }

    public void SetItem(Inventory.Slot slot)
    {
        inventorySlot = slot; // inventorySlot'u burada güncelle

        if (itemIcon != null)
        {
            itemIcon.sprite = slot.icon;
            itemIcon.color = new Color(1, 1, 1, 1);
        }
        if (quantityText != null)
        {
            quantityText.text = slot.count.ToString();
        }
    }

    public void SetEmpty()
    {
        inventorySlot = null; // inventorySlot'u boşalt

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = new Color(1, 1, 1, 0);
        }
        if (quantityText != null)
        {
            quantityText.text = "";
        }
    }

    public void SetHighlight(bool isOn)
    {
        if (highlight != null)
        {
            highlight.SetActive(isOn);
        }
        else
        {
            Debug.LogWarning("highlight nesnesi atanmadı.");
        }
    }
}
