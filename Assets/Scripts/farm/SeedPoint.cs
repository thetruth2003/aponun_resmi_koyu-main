using UnityEngine;

public enum SeedType { None, Wheat, Corn, Tomato }

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
    [Header("Growth")]
    public GameObject[] growthStages; // 5 prefab
    private int currentGrowthStage = 0;
    public bool isPesticideApplied = false; // opsiyonel
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
            AdvanceGrowth(); // → Eğer sulandıysa büyüsün
        }

        isWatered = false;
        isPesticideApplied = false;
    }

    void AdvanceGrowth()
    {
        if (growthStages.Length == 0 || currentGrowthStage >= growthStages.Length - 1)
            return;

        currentGrowthStage++;
        if (plantedInstance != null)
            Destroy(plantedInstance);

        plantedInstance = Instantiate(growthStages[currentGrowthStage], transform.position, Quaternion.identity, transform);
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
}
