using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class savetest : MonoBehaviour
{
    [Header("Hotkeys")]
    [SerializeField] private KeyCode saveKey = KeyCode.V;
    [SerializeField] private KeyCode loadKey = KeyCode.L;

    [Header("Resources klasörleri (Assets/Resources/<folder>)")]
    [SerializeField] private string resourcesBuildFolder = "build";
    [SerializeField] private string resourcesCarsFolder = "cars";
    [SerializeField] private string resourcesToolsFolder = "tools";

    [Header("Eski kayıtlar için fallback yakınlık toleransı (metre)")]
    [SerializeField] private float positionTolerance = 0.5f;

    // ---------- SAVE FORMAT ----------
    [Serializable]
    private struct ToolRec
    {
        public string id; public string name; public Vector3 pos; public Quaternion rot; public Vector3 scale; public float duration; public int price; public int amount;
    }
    [Serializable] private class ToolsSave { public List<ToolRec> tools = new(); }

    [Serializable]
    private struct BuildingRec
    {
        public string id; public string name; public Vector3 pos; public Quaternion rot; public Vector3 scale;
    }
    [Serializable] private class BuildingsSave { public List<BuildingRec> buildings = new(); }

    [Serializable]
    private struct CarRec
    {
        public string id; public string name; public Vector3 pos; public Quaternion rot; public Vector3 scale; public float duration; public float fuel; public int price;
    }
    [Serializable] private class CarsSave { public List<CarRec> cars = new(); }
    [Header("Muhasebe")]
    [SerializeField] private Muhasebeci muhasebeci;

    private string MoneyPath => Path.Combine(Application.persistentDataPath, "money_save.json");

    [Serializable] private class MoneySave { public int money; }


    private string ToolsPath => Path.Combine(Application.persistentDataPath, "tools_save.json");
    private string BuildingsPath => Path.Combine(Application.persistentDataPath, "buildings_save.json");
    private string CarsPath => Path.Combine(Application.persistentDataPath, "cars_save.json");

    void Update()
    {
        if (Input.GetKeyDown(saveKey)) { SaveTools(); SaveMoney(); SaveBuildings(); SaveCars(); Debug.Log("[Save] Bitti."); }
        if (Input.GetKeyDown(loadKey)) { LoadTools(); LoadMoney(); LoadBuildings(); LoadCars(); Debug.Log("[Load] Bitti."); }
    }

    // =============== TOOLS ===============
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
        Debug.Log($"[Save Tools] {sf.tools.Count} kayıt → {ToolsPath}");
    }

    private void LoadTools()
    {
        if (!File.Exists(ToolsPath)) { Debug.LogWarning("[Load Tools] Dosya yok."); return; }

        var sf = JsonUtility.FromJson<ToolsSave>(File.ReadAllText(ToolsPath));

        // Mevcutları ID -> Tools haritası
        var all = FindObjectsOfType<Tools>(includeInactive: true);
        var byId = new Dictionary<string, Tools>();
        foreach (var t in all) if (!string.IsNullOrEmpty(t.persistentId)) byId[t.persistentId] = t;

        float tolSqr = positionTolerance * positionTolerance;
        int updated = 0, spawned = 0, missingPrefab = 0;

        foreach (var rec in sf.tools)
        {
            // 1) ID ile bul
            if (!string.IsNullOrEmpty(rec.id) && byId.TryGetValue(rec.id, out var tool))
            {
                ApplyTRS(tool.transform, rec.pos, rec.rot, rec.scale);
                tool.duration = rec.duration; tool.price = rec.price; tool.amount = rec.amount;
                if (string.IsNullOrWhiteSpace(tool.itemName)) tool.itemName = rec.name;
                updated++;
                continue;
            }

            // 2) Eski kayıt uyumu: isim + yakınlık (opsiyonel)
            tool = FindMatchTool(new List<Tools>(all), rec.name, rec.pos, tolSqr);
            if (tool != null)
            {
                ApplyTRS(tool.transform, rec.pos, rec.rot, rec.scale);
                tool.duration = rec.duration; tool.price = rec.price; tool.amount = rec.amount;
                if (string.IsNullOrWhiteSpace(tool.itemName)) tool.itemName = rec.name;
                if (string.IsNullOrEmpty(tool.persistentId)) tool.persistentId = string.IsNullOrEmpty(rec.id) ? System.Guid.NewGuid().ToString("N") : rec.id;
                updated++;
                continue;
            }

            // 3) Yoksa Resources/tools/<name>’den prefab yükle ve spawn et
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
                tool.persistentId = string.IsNullOrEmpty(rec.id) ? System.Guid.NewGuid().ToString("N") : rec.id;
            }
            spawned++;
        }

        Debug.Log($"[Load Tools] Güncellendi: {updated}, Spawn: {spawned}, Prefab Eksik: {missingPrefab}. Toplam {sf.tools.Count}");
    }

    // =============== BUILDINGS ===============
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
        Debug.Log($"[Save Buildings] {sf.buildings.Count} kayıt → {BuildingsPath}");
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
            // 1) ID ile bul
            if (!string.IsNullOrEmpty(rec.id) && byId.TryGetValue(rec.id, out var b))
            {
                ApplyTRS(b.transform, rec.pos, rec.rot, rec.scale);
                if (string.IsNullOrEmpty(b.building_name)) b.building_name = rec.name;
                updated++;
                continue;
            }

            // 2) Eski kayıt uyumu: isim + yakınlık
            b = FindMatchBuilding(new List<Building>(all), rec.name, rec.pos, tolSqr);
            if (b != null)
            {
                ApplyTRS(b.transform, rec.pos, rec.rot, rec.scale);
                if (string.IsNullOrEmpty(b.building_name)) b.building_name = rec.name;
                if (string.IsNullOrEmpty(b.persistentId)) b.persistentId = string.IsNullOrEmpty(rec.id) ? System.Guid.NewGuid().ToString("N") : rec.id;
                updated++;
                continue;
            }

            // 3) Yoksa Resources/build/<name>’den prefab yükle ve spawn et
            var prefab = LoadFromResources(resourcesBuildFolder, rec.name);
            if (!prefab) { missingPrefab++; continue; }

            var inst = Instantiate(prefab, rec.pos, rec.rot);
            inst.transform.localScale = rec.scale;

            var nb = inst.GetComponent<Building>();
            if (nb)
            {
                nb.building_name = rec.name;
                nb.persistentId = string.IsNullOrEmpty(rec.id) ? System.Guid.NewGuid().ToString("N") : rec.id;
            }
            spawned++;
        }

        Debug.Log($"[Load Buildings] Güncellendi: {updated}, Spawn: {spawned}, Prefab Eksik: {missingPrefab}. Toplam {sf.buildings.Count}");
    }

    // =============== CARS ===============
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
        Debug.Log($"[Save Cars] {sf.cars.Count} kayıt → {CarsPath}");
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
            // 1) ID ile bul
            if (!string.IsNullOrEmpty(rec.id) && byId.TryGetValue(rec.id, out var car))
            {
                ApplyTRS(car.transform, rec.pos, rec.rot, rec.scale);
                car.duration = rec.duration; car.Fuel = rec.fuel; car.price = rec.price;
                if (string.IsNullOrWhiteSpace(car.car_name)) car.car_name = rec.name;
                updated++;
                continue;
            }

            // 2) Eski kayıt uyumu: isim + yakınlık
            car = FindMatchCar(new List<Car>(all), rec.name, rec.pos, tolSqr);
            if (car != null)
            {
                ApplyTRS(car.transform, rec.pos, rec.rot, rec.scale);
                car.duration = rec.duration; car.Fuel = rec.fuel; car.price = rec.price;
                if (string.IsNullOrWhiteSpace(car.car_name)) car.car_name = rec.name;
                if (string.IsNullOrEmpty(car.persistentId)) car.persistentId = string.IsNullOrEmpty(rec.id) ? System.Guid.NewGuid().ToString("N") : rec.id;
                updated++;
                continue;
            }

            // 3) Yoksa Resources/cars/<name>’den prefab yükle ve spawn et
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
                nc.persistentId = string.IsNullOrEmpty(rec.id) ? System.Guid.NewGuid().ToString("N") : rec.id;
            }
            spawned++;
        }

        Debug.Log($"[Load Cars] Güncellendi: {updated}, Spawn: {spawned}, Prefab Eksik: {missingPrefab}. Toplam {sf.cars.Count}");
    }
    private void SaveMoney()
    {
        if (!muhasebeci) muhasebeci = FindObjectOfType<Muhasebeci>();
        if (!muhasebeci) { Debug.LogWarning("[Save Money] Muhasebeci bulunamadı."); return; }

        var ms = new MoneySave { money = muhasebeci.GetMoney() };
        File.WriteAllText(MoneyPath, JsonUtility.ToJson(ms));
        Debug.Log($"[Save Money] {ms.money} → {MoneyPath}");
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

    // =============== HELPERS ===============
    private static void ApplyTRS(Transform t, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        var rb = t.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;     // << düzeltildi (linearVelocity değil)
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            rb.rotation = rot;
            t.localScale = scale;
        }
        else
        {
            t.SetPositionAndRotation(pos, rot);
            t.localScale = scale;
        }
    }

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
            if (!string.Equals(Clean(b.building_name), Clean(name), StringComparison.OrdinalIgnoreCase)) continue;
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
