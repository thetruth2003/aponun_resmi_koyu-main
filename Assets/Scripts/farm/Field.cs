using UnityEngine;

public class Field : MonoBehaviour
{
    public GameObject[] seedPoints;

    public void WaterAll()
    {
        foreach (var sp in seedPoints)
        {
            var seedPointScript = sp.GetComponent<SeedPoint>();
            if (seedPointScript != null)
                seedPointScript.Water();
        }
    }

    public void PlantAll(SeedType seedType)
    {
        foreach (var sp in seedPoints)
        {
            var seedPointScript = sp.GetComponent<SeedPoint>();
            if (seedPointScript != null)
                seedPointScript.PlantSeed(seedType);
        }
    }
}