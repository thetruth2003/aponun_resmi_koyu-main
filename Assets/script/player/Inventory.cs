using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

[System.Serializable]
public class Inventory
{
    [System.Serializable]
    public class Slot
    {
        public string itemName;
        public int count;
        public int maxAllowed;
        public GameObject itemPrefab;
        public Sprite icon;
        public GameObject itemUsedPrefab;

        public Slot()
        {
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

        public bool CanAddItem(string itemName)
        {
            // Aynı item türündense ve mevcut slotta maksimum sınırı aşmıyorsa eklenebilir
            return this.itemName == itemName && count < maxAllowed;
        }

        public void AddItem(string itemName, Sprite icon, int maxAllowed, GameObject itemPrefab, GameObject itemUsedPrefab)
        {
            // Aynı item türündeyse sayıyı artır, yoksa yeni item ekle
            if (this.itemName == itemName)
            {
                count++; // Aynı türden ekleniyorsa count artırılır
            }
            else
            {
                this.itemName = itemName;
                this.icon = icon;
                this.maxAllowed = maxAllowed;
                this.itemPrefab = itemPrefab;
                this.itemUsedPrefab = itemUsedPrefab;
                count = 1; // Yeni item geldiğinde count 1 olur
            }
        }

        public void RemoveItem()
        {
            if (count > 0)
            {
                count--;

                if (count == 0)
                {
                    icon = null;
                    itemName = "";
                    itemPrefab = null;
                    itemUsedPrefab = null;
                }
            }
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
    /// Eşyayı envantere ekler. Aynı türden bir eşya varsa, sadece sayıyı artırır. Yoksa yeni bir slot açar.
    /// </summary>
    public void Add(Item item)
    {
        Debug.Log($"Adding item: {item.data.itemName}");

        // 1. Aynı türden bir item bul ve o slota ekle
        foreach (Slot slot in slots)
        {
            if (slot.CanAddItem(item.data.itemName)) // Aynı itemName'e ve kapasiteye bakar
            {
                slot.AddItem(item.data.itemName, item.data.icon, item.data.maxAllowed, item.data.itemPrefab, item.data.itemUsedPrefab);
                Debug.Log($"Item added to existing slot: {slot.itemName}, Count: {slot.count}");
                return;
            }
        }

        // 2. Eğer aynı türde bir item yoksa, boş bir slot bul ve ekle
        foreach (Slot slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.AddItem(item.data.itemName, item.data.icon, item.data.maxAllowed, item.data.itemPrefab, item.data.itemUsedPrefab);
                Debug.Log($"Item added to empty slot: {slot.itemName}, Count: {slot.count}");
                return;
            }
        }

        // 3. Eğer boş bir slot da yoksa, hata mesajı yazdır (isteğe bağlı)
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
        if (slots == null || toInventory == null) return;

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
                toSlot.AddItem(fromSlot.itemName, fromSlot.icon, fromSlot.maxAllowed, fromSlot.itemPrefab, fromSlot.itemUsedPrefab);
                fromSlot.RemoveItem();
            }
        }
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
