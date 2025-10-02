using UnityEngine;

public class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance;

    [Header("State")]
    public bool IsRaining;

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        // Her yeni günde yağmur devam ediyorsa tekrar sula
        game_start.OnDayChanged += OnDayChanged;
    }

    private void OnDisable()
    {
        game_start.OnDayChanged -= OnDayChanged;
    }

    private void OnDayChanged()
    {
        if (IsRaining)
            WaterAllSeedPoints();
    }

    public void StartRain()
    {
        IsRaining = true;
        WaterAllSeedPoints(); // yağmur başlar başlamaz hepsini sula
    }

    public void StopRain()
    {
        IsRaining = false;
        // İstersen hemen kapatma; SeedPoint zaten gün sonunda SetWatered(false) yapacak.
        // Hemen kaldırmak istersen: foreach (var sp in FindObjectsOfType<SeedPoint>(true)) sp.SetWatered(false);
    }

    private void WaterAllSeedPoints()
    {
        var all = FindObjectsOfType<SeedPoint>(true);
        foreach (var sp in all)
        {
            if (sp.hasSeed)
                sp.SetWatered(true);   // Spawn/destroy işini SeedPoint kendi yapıyor
        }
    }
}
