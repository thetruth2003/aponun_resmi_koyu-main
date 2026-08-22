using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyuncunun tasidigi slotlari, esya ekleme-cikarma ve slot tasima islerini yonetir.
/// </summary>
[System.Serializable]
public class Inventory
{
    /// <summary>
    /// Slot sinifi, oyuncu tarafindaki ilgili davranis veya veriyi yonetir.
    /// </summary>
    [System.Serializable]
    public class Slot
    {
        public ItemData item;
        public string itemName;
        public int count;
        public int maxAllowed;
        public GameObject itemPrefab;
        public Sprite icon;
        public GameObject itemUsedPrefab;

        public Slot(ItemData item, int count)
        {
            this.item = item;
            this.count = count;
        }

        public Slot()
        {
            item = null;
            itemName = "";
            count = 0;
            maxAllowed = 99;
            itemPrefab = null;
            itemUsedPrefab = null;
        }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(itemName) && count == 0;
            }
        }

        public void Clear()
        {
            item = null;
            itemName = "";
            count = 0;
            maxAllowed = 99;
            itemPrefab = null;
            itemUsedPrefab = null;
        }

        public bool CanAddItem(string itemName)
        {
            return this.itemName == itemName && count < maxAllowed;
        }

        public void AddItem(ItemData item, string itemName, Sprite icon, int maxAllowed, GameObject itemPrefab, GameObject itemUsedPrefab)
        {
            if (this.itemName == itemName)
            {
                count++;
            }
            else
            {
                this.item = item;
                this.itemName = itemName;
                this.icon = icon;
                this.maxAllowed = maxAllowed;
                this.itemPrefab = itemPrefab;
                this.itemUsedPrefab = itemUsedPrefab;
                count = 1;
            }
        }

        public bool RemoveItem()
        {
            if (count > 0)
            {
                count--;
                if (count == 0)
                {
                    item = null;
                    icon = null;
                    itemName = "";
                    itemPrefab = null;
                    itemUsedPrefab = null;
                }

                return true;
            }

            return false;
        }
    }

    public List<Slot> slots = new List<Slot>();
    public Slot selectedSlot = null;

    public Inventory(int numSlots)
    {
        for (int i = 0; i < numSlots; i++)
        {
            slots.Add(new Slot());
        }
    }

    /// <summary>
    /// Eşyayı envantere ekler. Aynı türden bir eşya varsa sadece sayıyı artırır, yoksa boş slota yerleştirir.
    /// </summary>
    public void Add(Item item)
    {
        if (item == null || item.data == null)
        {
            Debug.LogWarning("[Inventory] Gecersiz item eklenmeye calisildi.");
            return;
        }

        foreach (Slot slot in slots)
        {
            if (slot.CanAddItem(item.data.itemName))
            {
                slot.AddItem(item.data, item.data.itemName, item.data.icon, item.data.maxAllowed, item.data.itemPrefab, item.data.itemUsedPrefab);
                return;
            }
        }

        foreach (Slot slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.AddItem(item.data, item.data.itemName, item.data.icon, item.data.maxAllowed, item.data.itemPrefab, item.data.itemUsedPrefab);
                return;
            }
        }

        Debug.LogWarning("Inventory is full! Cannot add the item.");
    }

    public void Remove(int index)
    {
        if (index >= 0 && index < slots.Count)
        {
            slots[index].RemoveItem();
        }
        else
        {
            Debug.LogWarning("Geçersiz slot indeksi!");
        }
    }

    public void Remove(int index, int count)
    {
        if (index >= 0 && index < slots.Count && slots[index].count >= count)
        {
            for (int i = 0; i < count; i++)
            {
                Remove(index);
            }
        }
        else
        {
            Debug.LogWarning("Geçersiz işlem veya yetersiz eşya!");
        }
    }

    public void MoveSlot(int fromIndex, int toIndex, Inventory toInventory, int numToMove = 1)
    {
        if (slots == null || toInventory == null)
        {
            return;
        }

        if (fromIndex < 0 || fromIndex >= slots.Count)
        {
            Debug.LogWarning($"MoveSlot: Geçersiz fromIndex: {fromIndex}");
            return;
        }

        if (toIndex < 0 || toIndex >= toInventory.slots.Count)
        {
            Debug.LogWarning($"MoveSlot: Geçersiz toIndex: {toIndex}");
            return;
        }

        Slot fromSlot = slots[fromIndex];
        Slot toSlot = toInventory.slots[toIndex];

        for (int i = 0; i < numToMove; i++)
        {
            if (toSlot.IsEmpty || toSlot.CanAddItem(fromSlot.itemName))
            {
                toSlot.AddItem(fromSlot.item, fromSlot.itemName, fromSlot.icon, fromSlot.maxAllowed, fromSlot.itemPrefab, fromSlot.itemUsedPrefab);
                fromSlot.RemoveItem();
            }
        }
    }

    public bool TryConsumeSelectedSlot(int amount = 1)
    {
        if (selectedSlot == null)
        {
            Debug.LogWarning("[Inventory] selectedSlot null, azaltılamadı.");
            return false;
        }

        if (selectedSlot.count <= 0)
        {
            Debug.LogWarning("[Inventory] Seçili slotta item yok veya miktar 0.");
            return false;
        }

        selectedSlot.RemoveItem();
        Inventory_UI.instance?.Refresh();
        return true;
    }

    public void SelectSlot(int index)
    {
        if (slots != null && slots.Count > 0 && index >= 0 && index < slots.Count)
        {
            selectedSlot = slots[index];
        }
        else
        {
            Debug.LogWarning("Geçersiz slot indeksi seçildi!");
        }
    }
}
