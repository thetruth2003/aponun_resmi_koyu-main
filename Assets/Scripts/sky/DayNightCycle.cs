using UnityEngine;
public class DayNightCycle : MonoBehaviour
{
    public Light sunLight; // Yeryüzü ışığı
    public Light skyLight; // Gökyüzü ışığı
    public float dayDuration = 120f; // 1 tam günün saniye cinsinden süresi
    private float currentTime = 0f; // Döngüdeki mevcut zaman

    void Update()
    {
        UpdateDayNightCycle();
    }

    private void UpdateDayNightCycle()
    {
        currentTime += Time.deltaTime;
        float timeNormalized = (currentTime % dayDuration) / dayDuration;

        // Işığın yönünü ve yoğunluğunu değiştir
        UpdateSunLight(timeNormalized);
        UpdateSkyLight(timeNormalized);
    }

    private void UpdateSunLight(float timeNormalized)
    {
        // Güneşi döndür (ör. sabah doğudan yükselir, akşam batıya gider)
        sunLight.transform.rotation = Quaternion.Euler(new Vector3((timeNormalized * 360f) - 90f, 170f, 0f));

        // Yoğunluk ve renk değişimi
        if (timeNormalized <= 0.5f) // Gündüz
        {
            sunLight.intensity = Mathf.Lerp(0, 1f, timeNormalized * 2);
            sunLight.color = Color.Lerp(new Color(1f, 0.95f, 0.8f), Color.white, timeNormalized * 2);
        }
        else // Gece
        {
            sunLight.intensity = Mathf.Lerp(1f, 0, (timeNormalized - 0.5f) * 2);
            sunLight.color = Color.Lerp(Color.white, new Color(0.3f, 0.3f, 0.5f), (timeNormalized - 0.5f) * 2);
        }
    }

    private void UpdateSkyLight(float timeNormalized)
    {
        // Gökyüzü ışığının yoğunluğu
        if (timeNormalized <= 0.5f) // Gündüz
        {
            skyLight.intensity = Mathf.Lerp(0.2f, 0.8f, timeNormalized * 2);
        }
        else // Gece
        {
            skyLight.intensity = Mathf.Lerp(0.8f, 0.2f, (timeNormalized - 0.5f) * 2);
        }
    }

}
