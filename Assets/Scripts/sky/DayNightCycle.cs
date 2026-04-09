using UnityEngine;
/// <summary>
/// DayNightCycle sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    public Light sunLight;
    public Light skyLight;
    public float dayDuration = 120f;
    private float currentTime = 0f;

    void Update()
    {
        UpdateDayNightCycle();
    }

    private void UpdateDayNightCycle()
    {
        currentTime += Time.deltaTime;
        float timeNormalized = (currentTime % dayDuration) / dayDuration;

        UpdateSunLight(timeNormalized);
        UpdateSkyLight(timeNormalized);
    }

    private void UpdateSunLight(float timeNormalized)
    {
        sunLight.transform.rotation = Quaternion.Euler(new Vector3((timeNormalized * 360f) - 90f, 170f, 0f));

        if (timeNormalized <= 0.5f)
        {
            sunLight.intensity = Mathf.Lerp(0, 1f, timeNormalized * 2);
            sunLight.color = Color.Lerp(new Color(1f, 0.95f, 0.8f), Color.white, timeNormalized * 2);
        }
        else
        {
            sunLight.intensity = Mathf.Lerp(1f, 0, (timeNormalized - 0.5f) * 2);
            sunLight.color = Color.Lerp(Color.white, new Color(0.3f, 0.3f, 0.5f), (timeNormalized - 0.5f) * 2);
        }
    }

    private void UpdateSkyLight(float timeNormalized)
    {
        if (timeNormalized <= 0.5f)
        {
            skyLight.intensity = Mathf.Lerp(0.2f, 0.8f, timeNormalized * 2);
        }
        else
        {
            skyLight.intensity = Mathf.Lerp(0.8f, 0.2f, (timeNormalized - 0.5f) * 2);
        }
    }

}
