using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Inventory_UI : MonoBehaviour
{
    [Header("Inventory Settings")]
    public string inventoryName;                // örn: "backpack"
    public List<Slot_UI> slots = new List<Slot_UI>();
    public Canvas canvas;
    private Inventory inventory;
    public static Inventory_UI instance;
    public Muhasebeci money;                         // Para UI referansı

    public string UniqueID => GetUniqueID();

    private void Awake()
    {
        instance = this;
        // Canvas otomatik bul
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        // InventoryManager hazırsa inventory çek
        var invManager = GameManager.instance?.player?.inventoryManager;
        if (invManager != null)
            inventory = invManager.GetInventoryByName(inventoryName);

        // Slot listesini boşsa initialize et
        if (slots == null)
            slots = new List<Slot_UI>();
    }

    private void Start()
    {
        // Envanteri Start'ta da tekrar çekelim (bazı durumlarda Awake sırasında GameManager hazır değil)
        if (inventory == null && GameManager.instance != null)
        {
            var invManager = GameManager.instance.player.inventoryManager;
            inventory = invManager.GetInventoryByName(inventoryName);
        }

        SetupSlots();
        Refresh();
    }

    /// <summary>
    /// Envanter UI'ını yeniler
    /// </summary>
    public void Refresh()
    {
        if (inventory == null || inventory.slots == null)
        {
            Debug.LogError($"[Inventory_UI.Refresh] inventory veya inventory.slots = NULL — UI objesi: {gameObject.name}", this);
            return;
        }

        if (slots.Count != inventory.slots.Count)
        {
            Debug.LogWarning($"[Inventory_UI.Refresh] Slot sayısı uyuşmuyor ({slots.Count} vs {inventory.slots.Count}) — {inventoryName}");
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slotID < 0)
                slots[i].slotID = i;

            if (!inventory.slots[i].IsEmpty)
                slots[i].SetItem(inventory.slots[i]);
            else
                slots[i].SetEmpty();
        }
    }

    public void Remove()
    {
        if (UI_Manager.draggedSlot != null)
        {
            Item itemToDrop = GameManager.instance.itemManager.GetItemByName(inventory.slots[UI_Manager.draggedSlot.slotID].itemName);

            if (itemToDrop != null)
            {
                if (UI_Manager.dragSingle)
                {
                    GameManager.instance.player.DropItem(itemToDrop);
                    inventory.Remove(UI_Manager.draggedSlot.slotID);
                }
                else
                {
                    GameManager.instance.player.DropItem(itemToDrop, inventory.slots[UI_Manager.draggedSlot.slotID].count);
                    inventory.Remove(UI_Manager.draggedSlot.slotID, inventory.slots[UI_Manager.draggedSlot.slotID].count);
                }

                Refresh();
            }
        }

        UI_Manager.draggedSlot = null;
    }

    public void SlotBeginDrag(Slot_UI slot)
    {
        UI_Manager.draggedSlot = slot;
        UI_Manager.draggedIcon = Instantiate(UI_Manager.draggedSlot.itemIcon);
        UI_Manager.draggedIcon.transform.SetParent(canvas.transform);
        UI_Manager.draggedIcon.raycastTarget = false;
        UI_Manager.draggedIcon.rectTransform.sizeDelta = new Vector2(50, 50);
        MoveToMousePosition(UI_Manager.draggedIcon.gameObject);
    }

    public void SlotDrag()
    {
        if (UI_Manager.draggedSlot != null)
            MoveToMousePosition(UI_Manager.draggedIcon.gameObject);
    }

    public void SlotEndDrag()
    {
        if (UI_Manager.draggedIcon != null)
            Destroy(UI_Manager.draggedIcon.gameObject);
        UI_Manager.draggedIcon = null;
    }

    public void SlotDrop(Slot_UI slot)
    {
        if (UI_Manager.dragSingle)
        {
            UI_Manager.draggedSlot.inventory.MoveSlot(UI_Manager.draggedSlot.slotID, slot.slotID, slot.inventory);
        }
        else
        {
            UI_Manager.draggedSlot.inventory.MoveSlot(
                UI_Manager.draggedSlot.slotID,
                slot.slotID,
                slot.inventory,
                UI_Manager.draggedSlot.inventory.slots[UI_Manager.draggedSlot.slotID].count
            );
        }

        GameManager.instance.uiManager.RefreshAll();
    }

    private void MoveToMousePosition(GameObject toMove)
    {
        if (canvas != null)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                null,
                out position
            );
            toMove.transform.position = canvas.transform.TransformPoint(position);
        }
    }

    private void SetupSlots()
    {
        int counter = 0;
        foreach (Slot_UI slot in slots)
        {
            slot.slotID = counter;
            slot.inventory = inventory;
            counter++;
        }
    }
    // Inventory_UI class'ının İÇİNE ekle (herhangi bir yere, ama class kapanışından önce)
    public bool InventoryIsReady()
    {
        // 'inventory' alanın zaten sınıf içinde var.
        // 'slots' bazen null olabilir; onu da kontrol edelim.
        return inventory != null && inventory.slots != null;
    }

    public string GetUniqueID()
    {
        return "31";
    }

    public void SaveData()
    {
        PlayerPrefs.SetString(GetUniqueID() + "_para", money.playerMoney.ToString());
        Debug.Log("save para");
    }

    public void LoadData()
    {
        string paraStr = PlayerPrefs.GetString(GetUniqueID() + "_para", "69");
        if (int.TryParse(paraStr, out int para))
        {
            money.playerMoney = para;
            if (money.moneyText != null)
                money.moneyText.text = money.playerMoney.ToString();
        }
        Debug.Log("load para");
    }
}
