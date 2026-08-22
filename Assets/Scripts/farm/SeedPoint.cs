using UnityEngine;

/// <summary>
/// Seed types used by farm sockets.
/// </summary>
public enum SeedType
{
    None, Wheat, Corn, Tomato, Potato, Carrot, Pumpkin, Cabbage, Eggplant, Radish, Lettuce,
    Cucumber, Grape, Pepper, Bean, Chilli, Onion, Melon, Watermelon
}

/// <summary>
/// Serializable runtime state for one farm socket.
/// </summary>
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

/// <summary>
/// Owns planting, watering, growth and harvest flow for a single farm socket.
/// </summary>
public class SeedPoint : MonoBehaviour, ISaveable
{
    [Header("Seed Configuration")]
    [Tooltip("Optional scriptable seed config. When assigned, growth stages and dry logic come from here.")]
    public SeedData seedData;

    [Header("Save/Load")]
    [SerializeField] public string persistentId;

    [Header("Watering Indicator")]
    [Tooltip("Optional visual shown while this socket is watered.")]
    public GameObject wateringEffectPrefab;
    private GameObject wateringEffectInstance;

    [Tooltip("Vertical offset for the watering indicator.")]
    public float waterIndicatorYOffset = -0.02f;

    [Header("Seed State")]
    public bool hasSeed = false;
    public bool isWatered = false;
    public SeedType currentSeed = SeedType.None;

    [Header("Dry Logic (fallback)")]
    [Tooltip("Used only when SeedData is missing.")]
    public int maxDryDays = 2;

    [Header("Planted Prefab (fallback)")]
    [Tooltip("Used only when there are no growth stages.")]
    public GameObject plantPrefab;
    private GameObject plantedInstance;

    [Header("Growth (fallback)")]
    [Tooltip("Used only when SeedData is missing. Index 0 is the planted stage.")]
    public GameObject[] growthStages;
    private int currentGrowthStage = 0;
    private int dryDayCount = 0;
    public bool isPesticideApplied = false;
    private int lastProcessedDayStamp = int.MinValue;

    private GameObject[] ActiveGrowthStages =>
        (seedData != null && seedData.growthStages != null && seedData.growthStages.Length > 0)
            ? seedData.growthStages
            : growthStages;

    private int MaxDryDays =>
        seedData != null ? seedData.maxDryDays : maxDryDays;

    public int CurrentGrowthStage => currentGrowthStage;
    public bool IsFullyGrown => HasReachedFinalStage();
    public bool IsHarvestReady => hasSeed && HasReachedFinalStage() && GetHarvestCollectable() != null;
    public string UniqueID => GetUniqueID();

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(persistentId))
            persistentId = System.Guid.NewGuid().ToString("N");
    }

    private void Start()
    {
        if (GameTime.Instance != null)
            GameTime.Instance.OnNewDay += HandleNewDay;

        SyncWaterObject();
    }

    private void OnEnable()
    {
        game_start.OnDayChanged += HandleNewDay;
    }

    private void OnDisable()
    {
        game_start.OnDayChanged -= HandleNewDay;
    }

    private void OnDestroy()
    {
        if (GameTime.Instance != null)
            GameTime.Instance.OnNewDay -= HandleNewDay;
    }

    public SeedPointData GetState()
    {
        return new SeedPointData
        {
            hasSeed = hasSeed,
            seedType = currentSeed,
            isWatered = isWatered,
            dryDayCount = dryDayCount,
            growthStage = currentGrowthStage,
            isPesticideApplied = isPesticideApplied
        };
    }

    public void SetState(in SeedPointData data)
    {
        hasSeed = data.hasSeed;
        currentSeed = data.seedType;
        dryDayCount = data.dryDayCount;
        currentGrowthStage = data.growthStage;
        isPesticideApplied = data.isPesticideApplied;

        if (plantedInstance != null)
            Destroy(plantedInstance);

        plantedInstance = null;

        if (hasSeed)
            SpawnStage(currentGrowthStage);

        SetWatered(data.isWatered);
    }

    /// <summary>
    /// Backward-compatible entry point for older callers.
    /// </summary>
    public void PlantSeed(SeedType type)
    {
        PlantSeedInternal(type);
    }

    public bool TryPlant(SeedData newSeedData)
    {
        if (newSeedData == null)
        {
            Debug.LogWarning($"[SeedPoint:{name}] TryPlant called with null SeedData.");
            return false;
        }

        seedData = newSeedData;
        return PlantSeedInternal(newSeedData.seedType);
    }

    public void Water()
    {
        SetWatered(true);
    }

    public bool TryWater()
    {
        if (!hasSeed)
        {
            return false;
        }

        if (isWatered)
            return false;

        SetWatered(true);
        return true;
    }

    public bool TryHarvest()
    {
        if (!IsHarvestReady)
            return false;

        Collectable collectable = GetHarvestCollectable();
        if (collectable == null)
        {
            Debug.LogWarning($"[SeedPoint:{name}] Final stage has no Collectable component.");
            return false;
        }

        collectable.Collect();
        ResetCropState(destroyVisual: true);
        return true;
    }

    public void SetWatered(bool state)
    {
        if (state && !hasSeed)
            return;

        isWatered = state;
        SyncWaterObject();
    }

    public string GetUniqueID()
    {
        return !string.IsNullOrEmpty(persistentId) ? persistentId : transform.GetInstanceID().ToString();
    }

    public void SaveData()
    {
        string id = GetUniqueID();
        PlayerPrefs.SetInt(id + "_HasSeed", hasSeed ? 1 : 0);
        PlayerPrefs.SetString(id + "_SeedType", currentSeed.ToString());
        PlayerPrefs.SetInt(id + "_IsWatered", isWatered ? 1 : 0);
        PlayerPrefs.SetInt(id + "_DryDayCount", dryDayCount);
        PlayerPrefs.SetInt(id + "_GrowthStage", currentGrowthStage);
        PlayerPrefs.SetInt(id + "_IsPesticideApplied", isPesticideApplied ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        string id = GetUniqueID();
        if (!PlayerPrefs.HasKey(id + "_HasSeed"))
            return;

        hasSeed = PlayerPrefs.GetInt(id + "_HasSeed", 0) == 1;

        string seedTypeName = PlayerPrefs.GetString(id + "_SeedType", SeedType.None.ToString());
        if (!System.Enum.TryParse(seedTypeName, out currentSeed))
            currentSeed = SeedType.None;

        isWatered = PlayerPrefs.GetInt(id + "_IsWatered", 0) == 1;
        dryDayCount = PlayerPrefs.GetInt(id + "_DryDayCount", 0);
        currentGrowthStage = PlayerPrefs.GetInt(id + "_GrowthStage", 0);
        isPesticideApplied = PlayerPrefs.GetInt(id + "_IsPesticideApplied", 0) == 1;

        if (plantedInstance != null)
            Destroy(plantedInstance);

        plantedInstance = null;

        if (hasSeed)
            SpawnStage(currentGrowthStage);

        SetWatered(isWatered);
    }

    private bool PlantSeedInternal(SeedType type)
    {
        if (type == SeedType.None)
        {
            Debug.LogWarning($"[SeedPoint:{name}] SeedType.None cannot be planted.");
            return false;
        }

        if (hasSeed)
        {
            return false;
        }

        currentSeed = type;

        if (seedData != null)
        {
            if (seedData.seedType != SeedType.None && seedData.seedType != type)
            {
                Debug.LogWarning($"[SeedPoint:{name}] SeedData type ({seedData.seedType}) and planted type ({type}) do not match.");
            }

            if (seedData.growthStages != null && seedData.growthStages.Length > 0)
                growthStages = seedData.growthStages;

            maxDryDays = seedData.maxDryDays;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        else
        {
            Debug.LogWarning($"[SeedPoint:{name}] No SeedData assigned. Fallback growthStages will be used.");
        }

        hasSeed = true;
        dryDayCount = 0;
        currentGrowthStage = 0;
        isPesticideApplied = false;

        SetWatered(false);
        SpawnStage(0);
        return true;
    }

    private void HandleNewDay()
    {
        if (!ShouldProcessNewDay())
            return;

        if (!hasSeed)
            return;

        if (isWatered)
        {
            AdvanceGrowth();
            dryDayCount = 0;
        }
        else
        {
            dryDayCount++;
            if (dryDayCount >= MaxDryDays)
            {
                KillCrop();
                return;
            }
        }

        SetWatered(false);
    }

    private void AdvanceGrowth()
    {
        GameObject[] stages = ActiveGrowthStages;
        if (stages != null && stages.Length > 0)
        {
            if (currentGrowthStage < stages.Length - 1)
            {
                currentGrowthStage++;
                SpawnStage(currentGrowthStage);
            }
        }
        else if (plantPrefab != null)
        {
            SpawnPrefab(plantPrefab);
        }
    }

    private void KillCrop()
    {
        ResetCropState(destroyVisual: true);
    }

    private void ResetCropState(bool destroyVisual)
    {
        SetWatered(false);

        hasSeed = false;
        currentSeed = SeedType.None;
        dryDayCount = 0;
        currentGrowthStage = 0;
        isPesticideApplied = false;

        GameObject previousInstance = plantedInstance;
        plantedInstance = null;

        if (destroyVisual && previousInstance != null)
            Destroy(previousInstance);
    }

    private void SpawnStage(int stageIndex)
    {
        GameObject[] stages = ActiveGrowthStages;
        GameObject prefab = null;

        if (stages != null && stages.Length > 0)
        {
            int index = Mathf.Clamp(stageIndex, 0, stages.Length - 1);
            prefab = stages[index];
        }
        else
        {
            prefab = plantPrefab;
        }

        SpawnPrefab(prefab);
    }

    private void SpawnPrefab(GameObject prefab)
    {
        if (prefab == null)
            return;

        if (plantedInstance != null)
            Destroy(plantedInstance);

        plantedInstance = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        plantedInstance.transform.SetParent(transform, worldPositionStays: true);
    }

    private void SyncWaterObject()
    {
        if (isWatered)
        {
            if (wateringEffectInstance == null)
            {
                Transform existing = transform.Find("WaterIndicator");
                if (existing != null)
                    wateringEffectInstance = existing.gameObject;
            }

            if (wateringEffectPrefab != null && wateringEffectInstance == null)
            {
                Vector3 finalPos = transform.position + new Vector3(0f, waterIndicatorYOffset, 0f);
                wateringEffectInstance = Instantiate(wateringEffectPrefab, finalPos, Quaternion.identity, transform);
                wateringEffectInstance.name = "WaterIndicator";
            }
        }
        else if (wateringEffectInstance != null)
        {
            Destroy(wateringEffectInstance);
            wateringEffectInstance = null;
        }
    }

    private bool HasReachedFinalStage()
    {
        GameObject[] stages = ActiveGrowthStages;
        if (stages == null || stages.Length == 0)
            return plantedInstance != null;

        return currentGrowthStage >= stages.Length - 1;
    }

    private Collectable GetHarvestCollectable()
    {
        return plantedInstance != null ? plantedInstance.GetComponentInChildren<Collectable>(true) : null;
    }

    private bool ShouldProcessNewDay()
    {
        int dayStamp = ResolveCurrentDayStamp();
        if (dayStamp == int.MinValue)
            return true;

        if (dayStamp == lastProcessedDayStamp)
            return false;

        lastProcessedDayStamp = dayStamp;
        return true;
    }

    private int ResolveCurrentDayStamp()
    {
        int prefDay = PlayerPrefs.GetInt("DayCount", int.MinValue);
        int gameTimeDay = GameTime.Instance != null ? GameTime.Instance.dayCount : int.MinValue;
        return Mathf.Max(prefDay, gameTimeDay);
    }
}
