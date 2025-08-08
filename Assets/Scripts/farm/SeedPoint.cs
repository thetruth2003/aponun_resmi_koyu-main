using UnityEngine;

public enum SeedType { None, Wheat, Corn, Tomato }

[System.Serializable]
public struct SeedPointData
{
    public bool hasSeed;
    public SeedType seedType;
    public bool isWatered;
    public int dryDayCount;
    public int growthStage;
    public bool isPesticideApplied;
}

public class SeedPoint : MonoBehaviour
{
    [Header("Seed Configuration")]
    [Tooltip("ScriptableObject ile tohum verisi")]
    public SeedData seedData;
    
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

    [Header("Growth")]
    public GameObject[] growthStages; // 5 prefab
    private int currentGrowthStage = 0;

    public bool isPesticideApplied = false; // opsiyonel

    private void Start()
    {
        if (GameTime.Instance != null)
            GameTime.Instance.OnNewDay += HandleNewDay;
    }

    // Kullanım yeri: FieldSave, durumu toplarken çağırır
    public SeedPointData GetState()
    {
        return new SeedPointData
        {
            hasSeed            = hasSeed,
            seedType           = currentSeed,
            isWatered          = isWatered,
            dryDayCount        = dryDayCount,
            growthStage        = currentGrowthStage,
            isPesticideApplied = isPesticideApplied
        };
    }

    // Kullanım yeri: FieldSave, yüklendiğinde çağırır
    public void SetState(in SeedPointData data)
    {
        hasSeed            = data.hasSeed;
        currentSeed        = data.seedType;
        isWatered          = data.isWatered;
        dryDayCount        = data.dryDayCount;
        currentGrowthStage = data.growthStage;
        isPesticideApplied = data.isPesticideApplied;

        // Mevcut örneği temizle
        if (plantedInstance != null)
            Destroy(plantedInstance);

        // Yeniden instantiate
        if (hasSeed)
        {
            GameObject prefabToSpawn = (growthStages != null && growthStages.Length > 0)
                ? growthStages[currentGrowthStage]
                : plantPrefab;

            if (prefabToSpawn != null)
                plantedInstance = Instantiate(prefabToSpawn, transform.position, Quaternion.identity, transform);
        }
    }

    public void PlantSeed(SeedType type) { /* ... */ }
    public void Water()            { /* ... */ }
    private void HandleNewDay()    { /* ... */ }
    private void AdvanceGrowth()  { /* ... */ }
    private void KillCrop()       { /* ... */ }

    private void OnDestroy()
    {
        if (GameTime.Instance != null)
            GameTime.Instance.OnNewDay -= HandleNewDay;
    }
}
