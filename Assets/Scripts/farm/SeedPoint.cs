using UnityEngine;

public enum SeedType { None, Wheat, Corn, Tomato, Potato, Carrot, Pumpkin, Cabbage, Eggplant, Radish, Lettuce }

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

public class SeedPoint : MonoBehaviour, ISaveable
{
    [Header("Seed Configuration")]
    [Tooltip("ScriptableObject ile tohum verisi (varsa buradan okunur)")]
    public SeedData seedData;

    [Header("Seed State")]
    public bool hasSeed = false;
    public bool isWatered = false;
    public SeedType currentSeed = SeedType.None;

    [Header("Dry Logic (fallback)")]
    [Tooltip("SeedData yoksa burası kullanılır")]
    public int maxDryDays = 2;

    [Header("Planted Prefab (fallback)")]
    [Tooltip("SeedData/growthStages yoksa kullanılacak tek prefab")]
    public GameObject plantPrefab;
    private GameObject plantedInstance;

    [Header("Growth (fallback)")]
    [Tooltip("SeedData yoksa burası kullanılır (0..n aşamalar)")]
    public GameObject[] growthStages; // 5 prefab varsayımı
    private int currentGrowthStage = 0;
    private int dryDayCount = 0;
    public bool isPesticideApplied = false; // opsiyonel

    // Aktif verileri SeedData varsa oradan, yoksa yerelden okuyan yardımcılar
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

    // ===================== SAVE/LOAD =====================

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
        isWatered = data.isWatered;
        dryDayCount = data.dryDayCount;
        currentGrowthStage = data.growthStage;
        isPesticideApplied = data.isPesticideApplied;

        if (plantedInstance != null)
            Destroy(plantedInstance);

        if (hasSeed)
            SpawnStage(currentGrowthStage);
    }

    // ===================== CORE MEKANİK =====================

    /// <summary> Tohum eker, aşama 0’ı spawn eder. </summary>
    public void PlantSeed(SeedType type)
    {
        if (hasSeed) return;

        currentSeed = type;

        // SeedData atanmışsa ve tiple uyumluysa, growthStages ve diğer değerleri kopyala
        if (seedData != null)
        {
            if (seedData.seedType != SeedType.None && seedData.seedType != type)
            {
                Debug.LogWarning($"[SeedPoint] SeedData.seedType ({seedData.seedType}) ile ekilen tip ({type}) farklı.");
            }

            // >>> SENKRON: SO -> komponent
            if (seedData.growthStages != null && seedData.growthStages.Length > 0)
                growthStages = seedData.growthStages;

            maxDryDays = seedData.maxDryDays;   // istersen kopyala
                                                // sellPrice gibi başka alanları da burada alabilirsin (SeedPoint'te varsa)

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this); // Inspector'da güncellemeyi işaretle
#endif
        }
        else
        {
            Debug.LogWarning("[SeedPoint] PlantSeed çağrıldı ama seedData atanmış değil. Fallback (growthStages) kullanılacak.");
        }

        hasSeed = true;
        isWatered = false;
        dryDayCount = 0;
        currentGrowthStage = 0;

        SpawnStage(0);
    }

    /// <summary> Bu noktada sulama yapıldı olarak işaretle. </summary>
    public void Water()
    {
        if (!hasSeed) return;
        isWatered = true;
    }

    /// <summary>
    /// Her yeni gün tetiklendiğinde sulama kontrolü; sulandıysa büyür,
    /// sulanmadıysa kurur ve MaxDryDays'e ulaşınca ölür.
    /// </summary>
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

        // Gün sonunda sıfırla
        isWatered = false;
    }

    /// <summary> Bir sonraki growth stage’ine geçer ve prefab’ı yeniler. </summary>
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
            // en son aşamadaysa burada hasat/loot/flag vs. ekleyebilirsin
        }
        else
        {
            // Fallback tek prefab
            if (plantPrefab != null)
                SpawnPrefab(plantPrefab);
        }
    }

    /// <summary> Bitkiyi yok eder ve tüm state’i sıfırlar. </summary>
    private void KillCrop()
    {
        hasSeed = false;
        currentSeed = SeedType.None;
        isWatered = false;
        dryDayCount = 0;
        currentGrowthStage = 0;

        if (plantedInstance != null)
            Destroy(plantedInstance);
    }

    /// <summary> Verilen aşamaya uygun prefab’ı instantiate eder. </summary>
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
            // SeedData yoksa ve stages boşsa fallback olarak tek prefab
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
