using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Suruklenen envanter slotunu kabul edip satis icin onizleme ve onay akisini yonetir.
/// </summary>
public class TestSaleDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image previewIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI unitPriceText;
    [SerializeField] private TextMeshProUGUI totalPriceText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image dropHighlight;
    [SerializeField] private Sprite emptyPreviewSprite;
    [SerializeField] private Color emptyPreviewColor = Color.white;

    private Slot_UI boundSlot;
    private int pendingAmount;

    public bool HasSelection => boundSlot != null && !boundSlot.IsEmpty() && pendingAmount > 0;

    public Slot_UI BoundSlot => boundSlot;
    public int PendingAmount => pendingAmount;

    private void Awake()
    {
        if (previewIcon != null)
        {
            emptyPreviewSprite = previewIcon.sprite;
            emptyPreviewColor = previewIcon.color;
        }

        ClearPreview("Buraya slot surukle.");
    }

    public void Setup(
        Image preview,
        TextMeshProUGUI itemName,
        TextMeshProUGUI quantity,
        TextMeshProUGUI unitPrice,
        TextMeshProUGUI totalPrice,
        TextMeshProUGUI info,
        Image highlight)
    {
        previewIcon = preview;
        itemNameText = itemName;
        quantityText = quantity;
        unitPriceText = unitPrice;
        totalPriceText = totalPrice;
        statusText = info;
        dropHighlight = highlight;
    }

    public void OnDrop(PointerEventData eventData)
    {
        TryCaptureDraggedSlot();
        SetHighlight(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
    }

    public void TryCaptureDraggedSlot()
    {
        Slot_UI draggedSlot = UI_Manager.draggedSlot;
        if (draggedSlot == null || draggedSlot.IsEmpty())
        {
            ClearPreview("Gecerli bir slot suruklenmedi.");
            return;
        }

        if (draggedSlot.inventorySlot == null || draggedSlot.inventorySlot.item == null)
        {
            ClearPreview("Slotta satilabilir veri yok.");
            return;
        }

        boundSlot = draggedSlot;
        pendingAmount = UI_Manager.dragSingle ? 1 : draggedSlot.inventorySlot.count;

        if (previewIcon != null)
        {
            previewIcon.sprite = draggedSlot.inventorySlot.icon;
            previewIcon.color = draggedSlot.inventorySlot.icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (itemNameText != null)
        {
            itemNameText.text = draggedSlot.inventorySlot.item.itemName;
        }

        if (quantityText != null)
        {
            quantityText.text = "Miktar: " + pendingAmount;
        }

        if (unitPriceText != null)
        {
            unitPriceText.text = "Birim Fiyat: " + draggedSlot.inventorySlot.item.sellPrice + " TL";
        }

        if (totalPriceText != null)
        {
            totalPriceText.text = "Toplam: " + (pendingAmount * draggedSlot.inventorySlot.item.sellPrice) + " TL";
        }

        if (statusText != null)
        {
            statusText.text = "Hazir. Sat butonuna bas.";
        }
    }

    public int ConfirmSale(Muhasebeci muhasebeci)
    {
        if (!HasSelection || muhasebeci == null)
        {
            return 0;
        }

        int unitPrice = boundSlot.inventorySlot.item.sellPrice;
        int total = unitPrice * pendingAmount;
        string itemId = NormalizeItemId(boundSlot.inventorySlot.item.itemName);

        boundSlot.inventory.Remove(boundSlot.slotID, pendingAmount);
        muhasebeci.AddMoney(total);

        if (GameStateTracker.Instance != null && !string.IsNullOrWhiteSpace(itemId))
        {
            GameStateTracker.Instance.IncrementCount($"Sold_{itemId}", pendingAmount);
        }

        if (GameManager.instance != null && GameManager.instance.uiManager != null)
        {
            GameManager.instance.uiManager.RefreshAll();
        }

        ClearPreview("Satis tamamlandi. +" + total + " TL");
        return total;
    }

    public void ClearPreview(string message)
    {
        boundSlot = null;
        pendingAmount = 0;

        if (previewIcon != null)
        {
            previewIcon.sprite = emptyPreviewSprite;
            previewIcon.color = emptyPreviewSprite != null ? emptyPreviewColor : new Color(1f, 1f, 1f, 0f);
        }

        if (itemNameText != null) itemNameText.text = "Bos";
        if (quantityText != null) quantityText.text = "Miktar: -";
        if (unitPriceText != null) unitPriceText.text = "Birim Fiyat: -";
        if (totalPriceText != null) totalPriceText.text = "Toplam: -";
        if (statusText != null) statusText.text = message;
    }

    private void SetHighlight(bool isActive)
    {
        if (dropHighlight != null)
        {
            dropHighlight.color = isActive
                ? new Color(0.29f, 0.63f, 0.35f, 0.85f)
                : new Color(0.18f, 0.2f, 0.24f, 0.85f);
        }
    }

    private static string NormalizeItemId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
