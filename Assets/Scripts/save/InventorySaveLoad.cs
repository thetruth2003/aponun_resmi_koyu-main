using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// InventorySaveLoad, Inventory icindeki slot verisini JSON olarak kaydedip
/// yuklerken ItemData referanslarini katalog veya Resources uzerinden tekrar kurar.
/// </summary>
public class InventorySaveLoad : MonoBehaviour
{
    [Header("References")]
    public Inventory target;

    [Header("Item kaynagi (ItemDatabase yerine)")]
    [Tooltip("Icinde Item component'i ve data'si setli PREFABLAR. Isim eslesmesi item.data.itemName ile yapilir.")]
    public List<Item> itemCatalog = new List<Item>();

    [Tooltip("Katalogta bulunamazsa Resources'tan da dene. Orn: Assets/Resources/items/<name>.prefab")]
    public bool fallbackToResources = true;

    [Tooltip("Resources icindeki klasor adi (Item prefab'i ya da ItemData arar).")]
    public string resourcesFolder = "items";

    [Header("Test hotkeys (opsiyonel)")]
    [Tooltip("Kapali tut. Runtime save/load giris noktasi SaveCoordinator olmali.")]
    public bool enableHotkeys = false;
    public KeyCode saveKey = KeyCode.I;
    public KeyCode loadKey = KeyCode.O;

    [Header("Dosya adi")]
    public string fileName = "inventory_player.json";

    /// <summary>
    /// SlotRec sinifi, kayit sistemiyle ilgili davranisi yonetir.
    /// </summary>
    [Serializable]
    private struct SlotRec
    {
        public int index;
        public string itemName;
        public int count;
    }

    /// <summary>
    /// FileModel sinifi, kayit sistemiyle ilgili davranisi yonetir.
    /// </summary>
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

    public void Save()
    {
        if (target == null) { Debug.LogWarning("[InventorySave] target yok"); return; }

        var fm = new FileModel { capacity = target.slots.Count };

        for (int i = 0; i < target.slots.Count; i++)
        {
            var s = target.slots[i];
            // Bos slotlari yazmiyoruz; load tarafinda once tum slotlar sifirlanip
            // sonra sadece kayitli dolu slotlar geri yerlestiriliyor.
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
        Debug.Log($"[InventorySave] {fm.slots.Count} slot kaydedildi -> {SavePath}");
    }

    public void Load()
    {
        if (target == null) { Debug.LogWarning("[InventoryLoad] target yok"); return; }
        if (!File.Exists(SavePath)) { Debug.LogWarning("[InventoryLoad] dosya yok"); return; }

        var fm = JsonUtility.FromJson<FileModel>(File.ReadAllText(SavePath));

        // Kaydedilen kapasite mevcut listeden buyukse slotlari onceden olustur.
        EnsureCapacity(target, fm.capacity);

        for (int i = 0; i < target.slots.Count; i++)
            target.slots[i].Clear();

        foreach (var rec in fm.slots)
        {
            if (rec.index < 0 || rec.index >= target.slots.Count) continue;

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
                s.item = null;
                s.icon = null;
                if (s.maxAllowed <= 0) s.maxAllowed = 99;
            }
        }

        RefreshAllSlotUI(target);
        Debug.Log($"[InventoryLoad] {fm.slots.Count} slot yuklendi <- {SavePath}");
    }

    private ItemData GetItemDataByName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return null;
        string key = itemName.Trim();

        // Ilk tercih inspector'dan verilen katalog; isim eslesirse en saglam referans bu olur.
        for (int i = 0; i < itemCatalog.Count; i++)
        {
            var it = itemCatalog[i];
            if (!it || it.data == null) continue;
            if (string.Equals(it.data.itemName?.Trim(), key, StringComparison.OrdinalIgnoreCase))
                return it.data;
        }

        if (!fallbackToResources) return null;

        // Katalogta bulunamayan item icin prefab veya dogrudan ItemData aramasi yap.
        var itemPrefab = Resources.Load<GameObject>($"{resourcesFolder}/{key}");
        if (itemPrefab)
        {
            var comp = itemPrefab.GetComponent<Item>();
            if (comp && comp.data != null) return comp.data;
        }

        var data = Resources.Load<ItemData>($"{resourcesFolder}/{key}");
        if (data) return data;

        Debug.LogWarning($"[InventoryLoad] Item bulunamadi: '{key}' (katalog + Resources/{resourcesFolder})");
        return null;
    }

    private static void EnsureCapacity(Inventory inv, int targetCount)
    {
        if (targetCount <= 0) return;
        while (inv.slots.Count < targetCount)
            inv.slots.Add(new Inventory.Slot());
    }

    private static void RefreshAllSlotUI(Inventory inv)
    {
        // Save/load dogrudan data listesine dokundugu icin tum Slot_UI'lari son veriye gore yenile.
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
