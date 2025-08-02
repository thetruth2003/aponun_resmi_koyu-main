using UnityEngine;

public enum SeedType
{
    None,
    Wheat,
    Corn,
    Tomato,
    Grape,
    Carrot
}


public class SeedPoint : MonoBehaviour
{
    [Header("Seed State")]
    public bool hasSeed = false;
    public bool isWatered = false;
    public SeedType currentSeed = SeedType.None;

    [Header("Dry Logic")]
    public int dryDayCount = 0;
    public int maxDryDays = 2;

    [Header("Planted Prefab")]
    public GameObject plantPrefab;
    private GameObject plantedInstance;

    private void Start()
    {
        if (GameTime.Instance != null)
            GameTime.Instance.OnNewDay += HandleNewDay;
    }

    public void PlantSeed(SeedType type)
    {
        if (hasSeed) return;

        currentSeed = type;
        hasSeed = true;
        isWatered = false;
        dryDayCount = 0;

        if (plantPrefab != null)
        {
            plantedInstance = Instantiate(plantPrefab, transform.position, Quaternion.identity, transform);
        }
    }

    public void Water()
    {
        if (!hasSeed) return;
        isWatered = true;
    }

    private void HandleNewDay()
    {
        if (!hasSeed) return;

        if (!isWatered)
        {
            dryDayCount++;

            if (dryDayCount >= maxDryDays)
            {
                Debug.Log($"{name} kurudu!");
                KillCrop();
                return;
            }
        }
        else
        {
            dryDayCount = 0;
        }

        isWatered = false; // Gün başında sıfırlanır
    }
    public SeedType GetSeedTypeFromPrefabName(string prefabName)
    {
        prefabName = prefabName.ToLower();

        if (prefabName.Contains("wheat")) return SeedType.Wheat;
        if (prefabName.Contains("corn")) return SeedType.Corn;
        if (prefabName.Contains("tomato")) return SeedType.Tomato;
        if (prefabName.Contains("grape")) return SeedType.Grape;
        if (prefabName.Contains("carrot")) return SeedType.Carrot; // Eğer Carrot eklemek istersen

        return SeedType.None;
    }

    private void KillCrop()
    {
        hasSeed = false;
        currentSeed = SeedType.None;
        isWatered = false;
        dryDayCount = 0;

        if (plantedInstance != null)
            Destroy(plantedInstance);
    }

    private void OnDestroy()
    {
        if (GameTime.Instance != null)
            GameTime.Instance.OnNewDay -= HandleNewDay;
    }

    public SeedType DetectSeedTypeFromToolbar()
    {
        Toolbar_UI toolbar = GameObject.Find("Toolbar")?.GetComponent<Toolbar_UI>();
        if (toolbar == null)
        {
            Debug.LogWarning("Toolbar bulunamadı!");
            return SeedType.None;
        }

        string prefabName = toolbar.GetSelectedUsedPrefab(); // Örn: "tomato_seed"
        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogWarning("Prefab adı boş!");
            return SeedType.None;
        }

        prefabName = prefabName.ToLower();

        if (prefabName.Contains("wheat")) return SeedType.Wheat;
        if (prefabName.Contains("corn")) return SeedType.Corn;
        if (prefabName.Contains("tomato")) return SeedType.Tomato;
        if (prefabName.Contains("grape")) return SeedType.Grape;
        if (prefabName.Contains("carrot")) return SeedType.Carrot; // Eğer Carrot eklemek istersen

        return SeedType.None;
    }
    public void PlantSeed()
    {
        SeedType type = DetectSeedTypeFromToolbar();
        Debug.Log("PlantSeed metodu çağrıldı.");
        Toolbar_UI toolbar = GameObject.Find("Toolbar")?.GetComponent<Toolbar_UI>();
        // Elimizdeki prefab adını al
        string selectedItemUsedPrefab = toolbar.GetSelectedUsedPrefab();
        // Prefab'ı Resources klasöründen yükle
        GameObject newItem = Resources.Load<GameObject>($"Prefabs/foods/{selectedItemUsedPrefab}");
        Debug.Log($"Seed ekildi: {type} ({selectedItemUsedPrefab})");
        Debug.Log("PlantSeed çalıştı");
        Debug.Log($"toolbar: {GameObject.Find("Toolbar")}");
        Debug.Log($"Yüklenmeye çalışılan prefab adı: {selectedItemUsedPrefab}");
        Debug.Log($"Yüklenen prefab: {newItem.name}");
        // Pozisyon ve rotasyon ayarla
        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = Quaternion.identity;

        // Prefab'ı Instantiate et
        plantedInstance = Instantiate(newItem, spawnPosition, spawnRotation, transform);
        plantPrefab = newItem;
        currentSeed = type;
        hasSeed = true;
        isWatered = false;
        dryDayCount = 0;
    }
}
