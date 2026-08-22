using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// savetest sinifi, kayit ve yukleme akislarinda kullanilan veri veya yonetim davranisini saglar.
/// </summary>
public class savetest : MonoBehaviour
{
    [Header("Legacy Direct Input")]
    [Tooltip("Kapali tut. Kaydet/yukle giris noktasi artik SaveCoordinator.")]
    [SerializeField] private bool allowLegacyDirectHotkeys = false;
    [SerializeField] private KeyCode saveKey = KeyCode.V;
    [SerializeField] private KeyCode loadKey = KeyCode.L;

    [Header("Resources klasörleri (Assets/Resources/<folder>)")]
    [SerializeField] private string resourcesBuildFolder = "build";
    [SerializeField] private string resourcesCarsFolder  = "cars";
    [SerializeField] private string resourcesToolsFolder = "tools";

    private const string SEEDDATA_RES_PATH      = "data/items";
    private const string OLD_SEEDDATA_RES_PATH  = "data/seeds";

    [Header("Eski kayıtlar için fallback yakınlık toleransı (metre)")]
    [SerializeField] private float positionTolerance = 0.5f;

    [Header("Inventory (envanter)")]
    [Tooltip("InventoryManager'da görünen isimler")]
    [SerializeField] private string backpackInventoryName = "backpack";
    [SerializeField] private string toolbarInventoryName  = "toolbar";

    /// <summary>
    /// ToolRec sinifi, kayit sistemiyle ilgili davranisi yonetir.
    /// </summary>
    [Serializable]
    private struct ToolRec
    {
        public string id; public string name; public Vector3 pos; public Quaternion rot; public Vector3 scale; public float duration; public int price; public int amount;
    }
    [Serializable] private class ToolsSave { public List<ToolRec> tools = new(); }

    /// <summary>
    /// BuildingRec sinifi, kayit sistemiyle ilgili davranisi yonetir.
    /// </summary>
    [Serializable]
    private struct BuildingRec
    {
        public string id; public string name; public Vector3 pos; public Quaternion rot; public Vector3 scale;
    }
    [Serializable] private class BuildingsSave { public List<BuildingRec> buildings = new(); }

    /// <summary>
    /// CarRec sinifi, kayit sistemiyle ilgili davranisi yonetir.
    /// </summary>
    [Serializable]
    private struct CarRec
    {
        public string id; public string name; public Vector3 pos; public Quaternion rot; public Vector3 scale; public float duration; public float fuel; public int price;
    }
    [Serializable] private class CarsSave { public List<CarRec> cars = new(); }

    /// <summary>
    /// SeedRec sinifi, kayit sistemiyle ilgili davranisi yonetir.
    /// </summary>
    [Serializable]
    private struct SeedRec
    {
        public string id;
        public string name;
        public Vector3 pos;
        public string seedDataName;
        public SeedPointData data;
    }
    [Serializable] private class SeedsSave { public List<SeedRec> seeds = new(); }

    [Header("Muhasebe")]
    [SerializeField] private Muhasebeci muhasebeci;
    [Serializable] private class MoneySave { public int money; }

    [Serializable]
    private struct InvSlotRec
    {
        public string itemName;
        public int count;
    }
    [Serializable]
    private class InvSave
    {
        public List<InvSlotRec> slots = new();
        public int selectedIndex;
    }

    private string MoneyPath        => Path.Combine(Application.persistentDataPath, "money_save.json");
    private string ToolsPath        => Path.Combine(Application.persistentDataPath, "tools_save.json");
    private string BuildingsPath    => Path.Combine(Application.persistentDataPath, "buildings_save.json");
    private string CarsPath         => Path.Combine(Application.persistentDataPath, "cars_save.json");
    private string SeedsPath        => Path.Combine(Application.persistentDataPath, "seeds_save.json");
    private string InvBackpackPath  => Path.Combine(Application.persistentDataPath, "inv_backpack.json");
    private string InvToolbarPath   => Path.Combine(Application.persistentDataPath, "inv_toolbar.json");

    void Update()
    {
        if (!allowLegacyDirectHotkeys)
            return;

        if (Input.GetKeyDown(saveKey))
        {
            SaveCoordinator coordinator = SaveCoordinator.EnsureInstance();
            if (coordinator != null)
            {
                coordinator.SaveGame("hotkey save");
                Debug.Log("[Save] Coordinator ile tam save alindi.");
            }
            else
            {
                SaveTools(); SaveMoney(); SaveBuildings(); SaveCars(); SaveSeeds();
                SaveInventoryBoth();
                Debug.Log("[Save] Fallback world save bitti.");
            }
        }
        if (Input.GetKeyDown(loadKey))
        {
            SaveCoordinator coordinator = SaveCoordinator.EnsureInstance();
            if (coordinator != null)
            {
                coordinator.LoadLastSaveNow();
                Debug.Log("[Load] Coordinator ile son save yukleniyor.");
            }
            else
            {
                LoadTools(); LoadMoney(); LoadBuildings(); LoadCars(); LoadSeeds();
                LoadInventoryBoth();
                Debug.Log("[Load] Fallback world load bitti.");
            }
        }
    }

    private void SaveTools()
    {
        var sf = new ToolsSave();
        foreach (var t in FindObjectsOfType<Tools>(includeInactive: true))
        {
            var tr = t.transform;
            sf.tools.Add(new ToolRec
            {
                id = t.persistentId,
                name = string.IsNullOrWhiteSpace(t.itemName) ? Clean(t.name) : t.itemName,
                pos = tr.position,
                rot = tr.rotation,
                scale = tr.localScale,
                duration = t.duration,
                price = t.price,
                amount = t.amount
            });
        }
        File.WriteAllText(ToolsPath, JsonUtility.ToJson(sf));
        Debug.Log($"[Save Tools] {sf.tools.Count} kayıt › {ToolsPath}");
    }

    private void LoadTools()
    {
        if (!File.Exists(ToolsPath)) { Debug.LogWarning("[Load Tools] Dosya yok."); return; }
        var sf = JsonUtility.FromJson<ToolsSave>(File.ReadAllText(ToolsPath));

        var all = FindObjectsOfType<Tools>(includeInactive: true);
        var byId = new Dictionary<string, Tools>();
        foreach (var t in all) if (!string.IsNullOrEmpty(t.persistentId)) byId[t.persistentId] = t;

        float tolSqr = positionTolerance * positionTolerance;
        int updated = 0, spawned = 0, missingPrefab = 0;

        foreach (var rec in sf.tools)
        {
            if (!string.IsNullOrEmpty(rec.id) && byId.TryGetValue(rec.id, out var tool))
            {
                ApplyTRS(tool.transform, rec.pos, rec.rot, rec.scale);
                tool.duration = rec.duration; tool.price = rec.price; tool.amount = rec.amount;
                if (string.IsNullOrWhiteSpace(tool.itemName)) tool.itemName = rec.name;
                updated++; continue;
            }

            tool = FindMatchTool(new List<Tools>(all), rec.name, rec.pos, tolSqr);
            if (tool != null)
            {
                ApplyTRS(tool.transform, rec.pos, rec.rot, rec.scale);
                tool.duration = rec.duration; tool.price = rec.price; tool.amount = rec.amount;
                if (string.IsNullOrWhiteSpace(tool.itemName)) tool.itemName = rec.name;
                if (string.IsNullOrEmpty(tool.persistentId)) tool.persistentId = string.IsNullOrEmpty(rec.id) ? Guid.NewGuid().ToString("N") : rec.id;
                updated++; continue;
            }

            var prefab = LoadFromResources(resourcesToolsFolder, rec.name);
            if (!prefab) { missingPrefab++; continue; }

            var inst = Instantiate(prefab, rec.pos, rec.rot);
            inst.transform.localScale = rec.scale;

            tool = inst.GetComponent<Tools>();
            if (tool)
            {
                tool.itemName = rec.name;
                tool.duration = rec.duration;
                tool.price = rec.price;
                tool.amount = rec.amount;
                tool.persistentId = string.IsNullOrEmpty(rec.id) ? Guid.NewGuid().ToString("N") : rec.id;
            }
            spawned++;
        }

        Debug.Log($"[Load Tools] Güncellendi: {updated}, Spawn: {spawned}, Prefab Eksik: {missingPrefab}. Toplam {sf.tools.Count}");
    }

    private void SaveBuildings()
    {
        var sf = new BuildingsSave();
        foreach (var b in FindObjectsOfType<Building>(includeInactive: true))
        {
            var tr = b.transform;
            sf.buildings.Add(new BuildingRec
            {
                id = b.persistentId,
                name = string.IsNullOrEmpty(b.building_name) ? Clean(b.name) : b.building_name,
                pos = tr.position,
                rot = tr.rotation,
                scale = tr.localScale
            });
        }
        File.WriteAllText(BuildingsPath, JsonUtility.ToJson(sf));
        Debug.Log($"[Save Buildings] {sf.buildings.Count} kayıt › {BuildingsPath}");
    }

    private void LoadBuildings()
    {
        if (!File.Exists(BuildingsPath)) { Debug.LogWarning("[Load Buildings] Dosya yok."); return; }
        var sf = JsonUtility.FromJson<BuildingsSave>(File.ReadAllText(BuildingsPath));

        var all = FindObjectsOfType<Building>(includeInactive: true);
        var byId = new Dictionary<string, Building>();
        foreach (var b in all) if (!string.IsNullOrEmpty(b.persistentId)) byId[b.persistentId] = b;

        float tolSqr = positionTolerance * positionTolerance;
        int updated = 0, spawned = 0, missingPrefab = 0;

        foreach (var rec in sf.buildings)
        {
            if (!string.IsNullOrEmpty(rec.id) && byId.TryGetValue(rec.id, out var b))
            {
                ApplyTRS(b.transform, rec.pos, rec.rot, rec.scale);
                if (string.IsNullOrEmpty(b.building_name)) b.building_name = rec.name;
                updated++; continue;
            }

            b = FindMatchBuilding(new List<Building>(all), rec.name, rec.pos, tolSqr);
            if (b != null)
            {
                ApplyTRS(b.transform, rec.pos, rec.rot, rec.scale);
                if (string.IsNullOrEmpty(b.building_name)) b.building_name = rec.name;
                if (string.IsNullOrEmpty(b.persistentId)) b.persistentId = string.IsNullOrEmpty(rec.id) ? Guid.NewGuid().ToString("N") : rec.id;
                updated++; continue;
            }

            var prefab = LoadFromResources(resourcesBuildFolder, rec.name);
            if (!prefab) { missingPrefab++; continue; }

            var inst = Instantiate(prefab, rec.pos, rec.rot);
            inst.transform.localScale = rec.scale;

            var nb = inst.GetComponent<Building>();
            if (nb)
            {
                nb.building_name = rec.name;
                nb.persistentId  = string.IsNullOrEmpty(rec.id) ? Guid.NewGuid().ToString("N") : rec.id;
            }
            spawned++;
        }

        Debug.Log($"[Load Buildings] Güncellendi: {updated}, Spawn: {spawned}, Prefab Eksik: {missingPrefab}. Toplam {sf.buildings.Count}");
    }

    private void SaveCars()
    {
        var sf = new CarsSave();
        foreach (var c in FindObjectsOfType<Car>(includeInactive: true))
        {
            var tr = c.transform;
            sf.cars.Add(new CarRec
            {
                id = c.persistentId,
                name = string.IsNullOrWhiteSpace(c.car_name) ? Clean(c.name) : c.car_name,
                pos = tr.position,
                rot = tr.rotation,
                scale = tr.localScale,
                duration = c.duration,
                fuel = c.Fuel,
                price = c.price
            });
        }
        File.WriteAllText(CarsPath, JsonUtility.ToJson(sf));
        Debug.Log($"[Save Cars] {sf.cars.Count} kayıt › {CarsPath}");
    }

    private void LoadCars()
    {
        if (!File.Exists(CarsPath)) { Debug.LogWarning("[Load Cars] Dosya yok."); return; }
        var sf = JsonUtility.FromJson<CarsSave>(File.ReadAllText(CarsPath));

        var all = FindObjectsOfType<Car>(includeInactive: true);
        var byId = new Dictionary<string, Car>();
        foreach (var c in all) if (!string.IsNullOrEmpty(c.persistentId)) byId[c.persistentId] = c;

        float tolSqr = positionTolerance * positionTolerance;
        int updated = 0, spawned = 0, missingPrefab = 0;

        foreach (var rec in sf.cars)
        {
            if (!string.IsNullOrEmpty(rec.id) && byId.TryGetValue(rec.id, out var car))
            {
                ApplyTRS(car.transform, rec.pos, rec.rot, rec.scale);
                car.duration = rec.duration; car.Fuel = rec.fuel; car.price = rec.price;
                if (string.IsNullOrWhiteSpace(car.car_name)) car.car_name = rec.name;
                updated++; continue;
            }

            car = FindMatchCar(new List<Car>(all), rec.name, rec.pos, tolSqr);
            if (car != null)
            {
                ApplyTRS(car.transform, rec.pos, rec.rot, rec.scale);
                car.duration = rec.duration; car.Fuel = rec.fuel; car.price = rec.price;
                if (string.IsNullOrWhiteSpace(car.car_name)) car.car_name = rec.name;
                if (string.IsNullOrEmpty(car.persistentId)) car.persistentId = string.IsNullOrEmpty(rec.id) ? Guid.NewGuid().ToString("N") : rec.id;
                updated++; continue;
            }

            var prefab = LoadFromResources(resourcesCarsFolder, rec.name);
            if (!prefab) { missingPrefab++; continue; }

            var inst = Instantiate(prefab, rec.pos, rec.rot);
            inst.transform.localScale = rec.scale;

            var nc = inst.GetComponent<Car>();
            if (nc)
            {
                nc.car_name = rec.name;
                nc.duration = rec.duration;
                nc.Fuel = rec.fuel;
                nc.price = rec.price;
                nc.persistentId = string.IsNullOrEmpty(rec.id) ? Guid.NewGuid().ToString("N") : rec.id;
            }
            spawned++;
        }

        Debug.Log($"[Load Cars] Güncellendi: {updated}, Spawn: {spawned}, Prefab Eksik: {missingPrefab}. Toplam {sf.cars.Count}");
    }

    private void SaveSeeds()
    {
        var sf = new SeedsSave();

        foreach (var sp in FindObjectsOfType<SeedPoint>(includeInactive: true))
        {
            if (string.IsNullOrEmpty(sp.persistentId))
                sp.persistentId = Guid.NewGuid().ToString("N");

            sf.seeds.Add(new SeedRec
            {
                id   = sp.persistentId,
                name = sp.name,
                pos  = sp.transform.position,
                seedDataName = sp.seedData ? sp.seedData.name : string.Empty,
                data = sp.GetState()
            });
        }

        File.WriteAllText(SeedsPath, JsonUtility.ToJson(sf));
        Debug.Log($"[Save Seeds] {sf.seeds.Count} kayıt › {SeedsPath}");
    }

    private void LoadSeeds()
    {
        if (!File.Exists(SeedsPath)) { Debug.LogWarning("[Load Seeds] Dosya yok."); return; }
        var sf = JsonUtility.FromJson<SeedsSave>(File.ReadAllText(SeedsPath));

        var all = FindObjectsOfType<SeedPoint>(includeInactive: true);

        var byId = new Dictionary<string, SeedPoint>();
        foreach (var sp in all)
        {
            if (string.IsNullOrEmpty(sp.persistentId))
                sp.persistentId = Guid.NewGuid().ToString("N");
            byId[sp.persistentId] = sp;
        }

        float tolSqr = positionTolerance * positionTolerance;
        int applied = 0, matchedByPos = 0, missing = 0;

        foreach (var rec in sf.seeds)
        {
            SeedPoint sp = null;

            if (!string.IsNullOrEmpty(rec.id) && byId.TryGetValue(rec.id, out sp))
            {
                ApplySeedRecordToSeedPoint(sp, in rec);
                applied++;
                continue;
            }

            float best = float.MaxValue;
            foreach (var cand in all)
            {
                if (!cand) continue;
                if (!string.Equals(cand.name, rec.name, StringComparison.OrdinalIgnoreCase)) continue;
                float d = (cand.transform.position - rec.pos).sqrMagnitude;
                if (d <= tolSqr && d < best) { best = d; sp = cand; }
            }

            if (sp != null)
            {
                if (string.IsNullOrEmpty(sp.persistentId))
                    sp.persistentId = string.IsNullOrEmpty(rec.id) ? Guid.NewGuid().ToString("N") : rec.id;

                ApplySeedRecordToSeedPoint(sp, in rec);
                matchedByPos++;
                applied++;
            }
            else
            {
                missing++;
            }
        }

        Debug.Log($"[Load Seeds] Uygulanan: {applied} (pos-match: {matchedByPos}), Bulunamayan: {missing}, Toplam: {sf.seeds.Count}");
    }

    private void ApplySeedRecordToSeedPoint(SeedPoint sp, in SeedRec rec)
    {
        SeedData loaded = null;
        if (!string.IsNullOrEmpty(rec.seedDataName))
        {
            loaded = Resources.Load<SeedData>($"{SEEDDATA_RES_PATH}/{rec.seedDataName}");
            if (!loaded)
                loaded = Resources.Load<SeedData>($"{OLD_SEEDDATA_RES_PATH}/{rec.seedDataName}");
        }

        if (!loaded && rec.data.seedType != SeedType.None)
        {
            loaded = Resources.Load<SeedData>($"{SEEDDATA_RES_PATH}/{rec.data.seedType} Seed");
            if (!loaded)
                loaded = Resources.Load<SeedData>($"{OLD_SEEDDATA_RES_PATH}/{rec.data.seedType} Seed");
        }

        if (loaded) sp.seedData = loaded;
        else if (!string.IsNullOrEmpty(rec.seedDataName) || rec.data.seedType != SeedType.None)
            Debug.LogWarning($"[Load Seeds] SeedData bulunamadı: {SEEDDATA_RES_PATH}/{rec.seedDataName} (fallback: {OLD_SEEDDATA_RES_PATH})");

        sp.SetState(rec.data);
    }

    private void SaveInventoryBoth()
    {
        SaveInventoryByName(backpackInventoryName, InvBackpackPath);
        SaveInventoryByName(toolbarInventoryName,  InvToolbarPath);
    }

    private void LoadInventoryBoth()
    {
        LoadInventoryByName(backpackInventoryName, InvBackpackPath, applySelected:false);
        LoadInventoryByName(toolbarInventoryName,  InvToolbarPath,  applySelected:true);
        GameManager.instance?.uiManager?.RefreshAll();
    }

    private void SaveInventoryByName(string invName, string path)
    {
        var inv = GameManager.instance?.player?.inventoryManager?.GetInventoryByName(invName);
        if (inv == null || inv.slots == null) { Debug.LogWarning($"[Save Inventory] Envanter yok: {invName}"); return; }

        var data = new InvSave();
        for (int i = 0; i < inv.slots.Count; i++)
        {
            var s = inv.slots[i];
            if (s.IsEmpty)
                data.slots.Add(new InvSlotRec { itemName = "", count = 0 });
            else
                data.slots.Add(new InvSlotRec { itemName = s.itemName, count = s.count });
        }

        var ui = GameManager.instance?.uiManager;
        data.selectedIndex = ui ? ui.GetToolbarSelectedIndex() : -1;

        File.WriteAllText(path, JsonUtility.ToJson(data));
        Debug.Log($"[Save Inventory] {invName} › {path}");
    }

    private void LoadInventoryByName(string invName, string path, bool applySelected)
    {
        var inv = GameManager.instance?.player?.inventoryManager?.GetInventoryByName(invName);
        if (inv == null || inv.slots == null) { Debug.LogWarning($"[Load Inventory] Envanter yok: {invName}"); return; }
        if (!File.Exists(path)) { Debug.LogWarning($"[Load Inventory] Dosya yok: {path}"); return; }

        var data = JsonUtility.FromJson<InvSave>(File.ReadAllText(path));
        if (data == null) { Debug.LogWarning($"[Load Inventory] Json boş: {path}"); return; }

        for (int i = 0; i < inv.slots.Count; i++)
        {
            inv.slots[i].itemName = "";
            inv.slots[i].count = 0;
            inv.slots[i].icon = null;
            inv.slots[i].maxAllowed = 0;
            inv.slots[i].itemPrefab = null;
            inv.slots[i].itemUsedPrefab = null;
        }

        int n = Mathf.Min(inv.slots.Count, data.slots.Count);
        for (int i = 0; i < n; i++)
        {
            var r = data.slots[i];
            if (string.IsNullOrEmpty(r.itemName) || r.count <= 0) continue;

            ItemData itemData = Resources.Load<ItemData>($"data/items/{r.itemName}");

            if (itemData == null)
            {
                var itemObj = GameManager.instance?.itemManager?.GetItemByName(r.itemName);
                itemData = itemObj ? itemObj.data : null;
            }

            if (itemData == null)
            {
                Debug.LogWarning($"[Load Inventory] ItemData bulunamadı: {r.itemName}");
                continue;
            }

            var s = inv.slots[i];
            s.itemName = r.itemName;
            s.count = r.count;

            s.icon           = itemData.icon;
            s.maxAllowed     = itemData.maxAllowed;
            s.itemPrefab     = itemData.itemPrefab;
            s.itemUsedPrefab = itemData.itemUsedPrefab;
        }

        if (applySelected && data.selectedIndex >= 0)
        {
            var ui = GameManager.instance?.uiManager;
            int idx = Mathf.Clamp(data.selectedIndex, 0, Mathf.Max(0, inv.slots.Count - 1));
            if (ui) ui.SelectToolbarSlot(idx);
            else    inv.SelectSlot(idx);
        }

        Debug.Log($"[Load Inventory] {invName} ‹ {path}");
    }

    private void SaveMoney()
    {
        if (!muhasebeci) muhasebeci = FindObjectOfType<Muhasebeci>();
        if (!muhasebeci) { Debug.LogWarning("[Save Money] Muhasebeci bulunamadı."); return; }

        var ms = new MoneySave { money = muhasebeci.GetMoney() };
        File.WriteAllText(MoneyPath, JsonUtility.ToJson(ms));
        Debug.Log($"[Save Money] {ms.money} › {MoneyPath}");
    }

    private void LoadMoney()
    {
        if (!File.Exists(MoneyPath)) { Debug.LogWarning("[Load Money] Dosya yok."); return; }
        if (!muhasebeci) muhasebeci = FindObjectOfType<Muhasebeci>();
        if (!muhasebeci) { Debug.LogError("[Load Money] Muhasebeci yok."); return; }

        var ms = JsonUtility.FromJson<MoneySave>(File.ReadAllText(MoneyPath));
        muhasebeci.SetMoney(ms.money);
        Debug.Log($"[Load Money] {ms.money}");
    }

    private static ApplyTRSResult ApplyTRS(Transform t, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        var rb = t.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            rb.rotation = rot;
            t.localScale = scale;
            Physics.SyncTransforms();
            return ApplyTRSResult.WithRigidbody;
        }
        else
        {
            t.SetPositionAndRotation(pos, rot);
            t.localScale = scale;
            Physics.SyncTransforms();
            return ApplyTRSResult.NoRigidbody;
        }
    }

    /// <summary>
    /// ApplyTRSResult sinifi, kayit sistemiyle ilgili davranisi yonetir.
    /// </summary>
    private enum ApplyTRSResult { WithRigidbody, NoRigidbody }

    private static GameObject LoadFromResources(string folder, string name)
    {
        string path = string.IsNullOrEmpty(folder) ? name : $"{folder}/{name}";
        return Resources.Load<GameObject>(path);
    }

    private static string Clean(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("(Clone)", "").Trim();

    private static Tools FindMatchTool(List<Tools> list, string name, Vector3 pos, float tolSqr)
    {
        Tools best = null; float bestD = float.MaxValue;
        foreach (var t in list)
        {
            if (!t) continue;
            if (!string.Equals(Clean(t.itemName), Clean(name), StringComparison.OrdinalIgnoreCase)) continue;
            float d = (t.transform.position - pos).sqrMagnitude;
            if (d <= tolSqr && d < bestD) { bestD = d; best = t; }
        }
        return best;
    }

    private static Building FindMatchBuilding(List<Building> list, string name, Vector3 pos, float tolSqr)
    {
        Building best = null; float bestD = float.MaxValue;
        foreach (var b in list)
        {
            if (!b) continue;
            if (!string.IsNullOrEmpty(b.building_name))
            {
                if (!string.Equals(Clean(b.building_name), Clean(name), StringComparison.OrdinalIgnoreCase)) continue;
            }
            else
            {
                if (!string.Equals(Clean(b.name), Clean(name), StringComparison.OrdinalIgnoreCase)) continue;
            }

            float d = (b.transform.position - pos).sqrMagnitude;
            if (d <= tolSqr && d < bestD) { bestD = d; best = b; }
        }
        return best;
    }

    private static Car FindMatchCar(List<Car> list, string name, Vector3 pos, float tolSqr)
    {
        Car best = null; float bestD = float.MaxValue;
        foreach (var c in list)
        {
            if (!c) continue;
            if (!string.Equals(Clean(c.car_name), Clean(name), StringComparison.OrdinalIgnoreCase)) continue;
            float d = (c.transform.position - pos).sqrMagnitude;
            if (d <= tolSqr && d < bestD) { bestD = d; best = c; }
        }
        return best;
    }
}
