using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Envanterdeki tek bir slotun ikon, miktar ve secim gorselini yonetir.
/// </summary>
public class Slot_UI : MonoBehaviour
{
    public int slotID = -1;
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    public GameObject highlight;
    public Inventory inventory;
    public Inventory.Slot inventorySlot;
    public string itemName;

    private void Awake()
    {
        if (itemIcon == null)
        {
            Debug.LogError("Slot_UI: itemIcon atanmamýþ!");
        }

        if (quantityText == null)
        {
            Debug.LogError("Slot_UI: quantityText atanmamýþ!");
        }
    }

    public void SetItem(Inventory.Slot slot)
    {
        inventorySlot = slot;

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

    public int GetTotalSellValue()
    {
        return inventorySlot.count * inventorySlot.item.sellPrice;
    }

    public void SetEmpty()
    {
        inventorySlot = null;

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

    public bool IsEmpty()
    {
        return inventorySlot == null || inventorySlot.item == null;
    }

    public void Clear()
    {
        SetEmpty();
    }

    public void SetHighlight(bool isOn)
    {
        if (highlight != null)
        {
            highlight.SetActive(isOn);
        }
        else
        {
            Debug.LogWarning("highlight nesnesi atanmadý.");
        }
    }
}
