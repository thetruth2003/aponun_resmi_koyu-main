using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Inventory_UI : MonoBehaviour, ISaveable
{
    public string inventoryName;
    public List<Slot_UI> slots = new List<Slot_UI>();
    public Canvas canvas;
    public TextMeshProUGUI money_text;
    private Inventory inventory;

    private void Start()
    {
        money_text.text = "100"; // Başlangıçta 100 birim para
        canvas = FindObjectOfType<Canvas>();
        inventory = GameManager.instance.player.inventoryManager.GetInventoryByName(inventoryName);
        SetupSlots();
        Refresh();
    }

    public void Refresh()
    {
        if (slots.Count == inventory.slots.Count)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].slotID < 0)
                {
                    slots[i].slotID = i;
                }

                // Eğer slot boş değilse, itemi slot'a set et
                if (!inventory.slots[i].IsEmpty)
                {
                    slots[i].SetItem(inventory.slots[i]);
                }
                else
                {
                    slots[i].SetEmpty();
                }
            }
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
        {
            MoveToMousePosition(UI_Manager.draggedIcon.gameObject);
        }
    }

    public void SlotEndDrag()
    {
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
            UI_Manager.draggedSlot.inventory.MoveSlot(UI_Manager.draggedSlot.slotID, slot.slotID, slot.inventory, UI_Manager.draggedSlot.inventory.slots[UI_Manager.draggedSlot.slotID].count);
        }

        GameManager.instance.uiManager.RefreshAll();
    }

    private void MoveToMousePosition(GameObject toMove)
    {
        if (canvas != null)
        {
            Vector2 position;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, Input.mousePosition, null, out position);

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

    public string GetUniqueID()
    {
        return "31";
    }

    public void SaveData()
    {
        PlayerPrefs.SetString(GetUniqueID() + "_para", money_text.text.ToString());
        Debug.Log("save para");
    }

    public void LoadData()
    {
        money_text.text = PlayerPrefs.GetString(GetUniqueID() + "_para", "69").ToString();
        Debug.Log("load para");
    }
}
