using UnityEngine;

/// <summary>
/// SeedType sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
public enum SeedType {
    None, Wheat, Corn, Tomato, Potato, Carrot, Pumpkin, Cabbage, Eggplant, Radish, Lettuce,
    Cucumber, Grape, Pepper, Bean, Chilli, Onion, Melon, Watermelon
}

/// <summary>
/// SeedPointData sinifi, ilgili veriyi tanimlamak ve tasimak icin kullanilir.
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
/// SeedPoint sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
public class SeedPoint : MonoBehaviour, ISaveable
{
    [Header("Seed Configuration")]
    [Tooltip("ScriptableObject ile tohum verisi (varsa buradan okunur)")]
    public SeedData seedData;
    [Header("Save/Load")]
    [SerializeField] public string persistentId;

    [Header("Watering Indicator")]
    [Tooltip("Sulanm???±≈ü g√∂rseli (mavi √ßember)")]
    public GameObject wateringEffectPrefab;
    private GameObject wateringEffectInstance;

    [Tooltip("Mavi √ßemberin dikey offset'i (negatif ‚Üí biraz a≈üa???ü???±)")]
    public float waterIndicatorYOffset = -0.02f;

    [Header("Seed State")]
    public bool hasSeed = false;
    public bool isWatered = false;
    public SeedType currentSeed = SeedType.None;

    [Header("Dry Logic (fallback)")]
    [Tooltip("SeedData yoksa buras???± kullan???±l???±r")]
    public int maxDryDays = 2;

    [Header("Planted Prefab (fallback)")]
    [Tooltip("SeedData/growthStages yoksa kullan???±lacak tek prefab")]
    public GameObject plantPrefab;
    private GameObject plantedInstance;

    [Header("Growth (fallback)")]
    [Tooltip("SeedData yoksa buras???± kullan???±l???±r (0..n a≈üamalar)")]
    public GameObject[] growthStages;
    private int currentGrowthStage = 0;
    private int dryDayCount = 0;
    public bool isPesticideApplied = false;

    private GameObject[] ActiveGrowthStages =>
        (seedData != null && seedData.growthStages != null && seedData.growthStages.Length > 0)
            ? seedData.growthStages
            : growthStages;

    private int MaxDryDays =>
        (seedData != null) ? seedData.maxDryDays : maxDryDays;

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

        if (hasSeed)
            SpawnStage(currentGrowthStage);

        SetWatered(data.isWatered);
    }

    /// <summary>Tohum eker, a≈üama 0‚Ä???????± spawn eder.</summary>
    public void PlantSeed(SeedType type)
    {
        if (hasSeed) return;

        currentSeed = type;

        if (seedData != null)
        {
            if (seedData.seedType != SeedType.None && seedData.seedType != type)
                Debug.LogWarning($"[SeedPoint] SeedData.seedType ({seedData.seedType}) ile ekilen tip ({type}) farkl???±.");

            if (seedData.growthStages != null && seedData.growthStages.Length > 0)
                growthStages = seedData.growthStages;

            maxDryDays = seedData.maxDryDays;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        else
        {
            Debug.LogWarning("[SeedPoint] PlantSeed √ßa???ür???±ld???± ama seedData atanm???±≈ü de???üil. Fallback (growthStages) kullan???±lacak.");
        }

        hasSeed = true;
        dryDayCount = 0;
        currentGrowthStage = 0;

        SetWatered(false);
        SpawnStage(0);
    }

    /// <summary>Oyuncu sulad???±: isWatered = true ve halkay???± y√∂net.</summary>
    public void Water() => SetWatered(true);

    /// <summary>
    /// isWatered durumunu merkezi y√∂netir (true‚Üíspawn, false‚Üídestroy).
    /// Ya???ümur, kova, sprinkler hepsi bunu √ßa???ü???±rmal???±.
    /// </summary>
    public void SetWatered(bool state)
    {
        if (state && !hasSeed) return;

        isWatered = state;
        SyncWaterObject();
    }

    /// <summary>Yeni g√ºn: suland???±ysa b√ºy√ºt, de???üilse kurut; sonunda sulama s???±f???±rlan???±r.</summary>
    private void HandleNewDay()
    {
        if (!hasSeed) return;

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
                Debug.Log($"{name} kurudu!");
                KillCrop();
                return;
            }
        }

        SetWatered(false);
    }

    private void AdvanceGrowth()
    {
        var stages = ActiveGrowthStages;
        if (stages != null && stages.Length > 0)
        {
            if (currentGrowthStage < stages.Length - 1)
            {
                currentGrowthStage++;
                SpawnStage(currentGrowthStage);
            }
        }
        else
        {
            if (plantPrefab != null)
                SpawnPrefab(plantPrefab);
        }
    }

    private void KillCrop()
    {
        SetWatered(false);

        hasSeed = false;
        currentSeed = SeedType.None;
        dryDayCount = 0;
        currentGrowthStage = 0;

        if (plantedInstance != null)
            Destroy(plantedInstance);
    }

    private void SpawnStage(int stageIndex)
    {
        var stages = ActiveGrowthStages;

        GameObject prefab = null;

        if (stages != null && stages.Length > 0)
        {
            int idx = Mathf.Clamp(stageIndex, 0, stages.Length - 1);
            prefab = stages[idx];
        }
        else
        {
            prefab = plantPrefab;
        }

        SpawnPrefab(prefab);
    }

    private void SpawnPrefab(GameObject prefab)
    {
        if (prefab == null) return;

        if (plantedInstance != null)
            Destroy(plantedInstance);

        plantedInstance = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        plantedInstance.transform.SetParent(transform, worldPositionStays: true);
    }

    /// <summary>isWatered == true ise halkay???± spawnlar, false ise varsa destroy eder.</summary>
    private void SyncWaterObject()
    {
        if (isWatered)
        {
            Vector3 basePos = transform.position;
            Vector3 finalPos = basePos + new Vector3(0f, -1.0f, 0f);

            if (wateringEffectPrefab != null && wateringEffectInstance == null)
            {
                wateringEffectInstance = Instantiate(
                    wateringEffectPrefab,
                    finalPos,
                    Quaternion.identity,
                    null
                );
            }
        }
        else
        {
            if (wateringEffectInstance != null)
            {
                Destroy(wateringEffectInstance);
                wateringEffectInstance = null;
            }
        }
    }

    public string GetUniqueID() => transform.GetInstanceID().ToString();

    public string UniqueID { get; }
    public void SaveData()
    {
        PlayerPrefs.SetInt(GetUniqueID() + "_HasSeed", hasSeed ? 1 : 0);
        PlayerPrefs.SetString(GetUniqueID() + "_SeedType", currentSeed.ToString());
        PlayerPrefs.SetInt(GetUniqueID() + "_IsWatered", isWatered ? 1 : 0);
        PlayerPrefs.SetInt(GetUniqueID() + "_DryDayCount", dryDayCount);
        PlayerPrefs.SetInt(GetUniqueID() + "_GrowthStage", currentGrowthStage);
        PlayerPrefs.SetInt(GetUniqueID() + "_IsPesticideApplied", isPesticideApplied ? 1 : 0);
        throw new System.NotImplementedException();
    }

    public void LoadData()
    {
        throw new System.NotImplementedException();
    }
}
