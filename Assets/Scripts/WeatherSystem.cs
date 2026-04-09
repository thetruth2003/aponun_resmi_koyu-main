using UnityEngine;

/// <summary>
/// WeatherSystem sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
public class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance;

    [Header("State")]
    public bool IsRaining;

    private void Awake() => Instance = this;

    private void OnEnable()
    {
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
        WaterAllSeedPoints();
    }

    public void StopRain()
    {
        IsRaining = false;
    }

    private void WaterAllSeedPoints()
    {
        var all = FindObjectsOfType<SeedPoint>(true);
        foreach (var sp in all)
        {
            if (sp.hasSeed)
                sp.SetWatered(true);
        }
    }
}
