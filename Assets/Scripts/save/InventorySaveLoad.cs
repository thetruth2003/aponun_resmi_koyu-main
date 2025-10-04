using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InventorySaveLoad : MonoBehaviour
{
    [Header("References")]
    public Inventory target;                 // Kaydedilecek envanter

    [Header("Item kaynağı (ItemDatabase yerine)")]
    [Tooltip("İçinde Item component'i ve data'sı setli PREFABLAR. İsim eşleşmesi item.data.itemName ile yapılır.")]
    public List<Item> itemCatalog = new List<Item>();

    [Tooltip("Katalogta bulunamazsa Resources'tan da dene. Örn: Assets/Resources/items/<name>.prefab")]
    public bool fallbackToResources = true;

    [Tooltip("Resources içindeki klasör adı (Item prefabı ya da ItemData arar).")]
    public string resourcesFolder = "items";

    [Header("Test hotkeys (opsiyonel)")]
    public bool enableHotkeys = true;
    public KeyCode saveKey = KeyCode.I;   // I = Save
    public KeyCode loadKey = KeyCode.O;   // O = Load

    [Header("Dosya adı")]
    public string fileName = "inventory_player.json";

    [Serializable]
    private struct SlotRec
    {
        public int index;
        public string itemName;
        public int count;
    }

    [Serializable]
    private class FileModel
    {
        public int capacity;
        public List<SlotRec> slots = new List<SlotRec>();
    }

    private string SavePath => Path.Combine(Application.persistentDataPath, fileName);

    private void Update()
    {
        if (!enableHotkeys) return;
        if (Input.GetKeyDown(saveKey)) Save();
        if (Input.GetKeyDown(loadKey)) Load();
    }

    // ---------- SAVE ----------
    public void Save()
    {
        if (target == null) { Debug.LogWarning("[InventorySave] target yok"); return; }

        var fm = new FileModel { capacity = target.slots.Count };

        for (int i = 0; i < target.slots.Count; i++)
        {
            var s = target.slots[i];
            if (!string.IsNullOrEmpty(s.itemName) && s.count > 0)
            {
                fm.slots.Add(new SlotRec
                {
                    index = i,
                    itemName = s.itemName,
                    count = s.count
                });
            }
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(fm));
        Debug.Log($"[InventorySave] {fm.slots.Count} slot kaydedildi → {SavePath}");
    }

    // ---------- LOAD ----------
    public void Load()
    {
        if (target == null) { Debug.LogWarning("[InventoryLoad] target yok"); return; }
        if (!File.Exists(SavePath)) { Debug.LogWarning("[InventoryLoad] dosya yok"); return; }

        var fm = JsonUtility.FromJson<FileModel>(File.ReadAllText(SavePath));

        // Kapasiteyi uydur
        EnsureCapacity(target, fm.capacity);

        // Temizle
        for (int i = 0; i < target.slots.Count; i++)
            target.slots[i].Clear();

        // Kayıtları uygula
        foreach (var rec in fm.slots)
        {
            if (rec.index < 0 || rec.index >= target.slots.Count) continue;

            // ItemData'yı Item üzerinden bul
            ItemData data = GetItemDataByName(rec.itemName);

            var s = target.slots[rec.index];
            s.itemName = rec.itemName;
            s.count = rec.count;

            if (data != null)
            {
                s.item = data;
                s.icon = data.icon;
                s.maxAllowed = data.maxAllowed;
                s.itemPrefab = data.itemPrefab;
                s.itemUsedPrefab = data.itemUsedPrefab;
            }
            else
            {
                // Bulunamazsa en azından isim + adet kalsın
                s.item = null;
                s.icon = null;
                if (s.maxAllowed <= 0) s.maxAllowed = 99;
            }
        }

        RefreshAllSlotUI(target);
        Debug.Log($"[InventoryLoad] {fm.slots.Count} slot yüklendi ← {SavePath}");
    }

    // ---------- Helpers ----------
    private ItemData GetItemDataByName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return null;
        string key = itemName.Trim();

        // 1) Katalogta ara
        for (int i = 0; i < itemCatalog.Count; i++)
        {
            var it = itemCatalog[i];
            if (!it || it.data == null) continue;
            if (string.Equals(it.data.itemName?.Trim(), key, StringComparison.OrdinalIgnoreCase))
                return it.data;
        }

        if (!fallbackToResources) return null;

        // 2) Resources'ta önce Item prefabında Item.data ara
        var itemPrefab = Resources.Load<GameObject>($"{resourcesFolder}/{key}");
        if (itemPrefab)
        {
            var comp = itemPrefab.GetComponent<Item>();
            if (comp && comp.data != null) return comp.data;
        }

        // 3) Olmazsa doğrudan ItemData asset dene
        var data = Resources.Load<ItemData>($"{resourcesFolder}/{key}");
        if (data) return data;

        Debug.LogWarning($"[InventoryLoad] Item bulunamadı: '{key}' (katalog + Resources/{resourcesFolder})");
        return null;
    }

    private static void EnsureCapacity(Inventory inv, int targetCount)
    {
        if (targetCount <= 0) return;
        while (inv.slots.Count < targetCount)
            inv.slots.Add(new Inventory.Slot());
        // Küçültme genelde gerekmez; istersen burada yapabilirsin.
    }

    private static void RefreshAllSlotUI(Inventory inv)
    {
        var all = GameObject.FindObjectsOfType<Slot_UI>(includeInactive: true);
        for (int i = 0; i < all.Length; i++)
        {
            var ui = all[i];
            if (ui.inventory != inv) continue;
            int idx = ui.slotID;
            if (idx < 0 || idx >= inv.slots.Count) { ui.SetEmpty(); continue; }

            var slot = inv.slots[idx];
            if (slot == null || slot.IsEmpty) ui.SetEmpty();
            else ui.SetItem(slot);
        }
    }
}
