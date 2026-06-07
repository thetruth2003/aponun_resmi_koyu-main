using System.Collections;
using TMPro;
using UniStorm;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEditor;

/// <summary>
/// Oyun gun akisini, fade gecislerini ve saat arayuzunu yonetir.
/// </summary>
public class game_start : MonoBehaviour
{
    public int dayCount = 1;
    public Image fadePanel;
    public float fadeDuration = 2f;
    public Slider timeSlider;
    public TextMeshProUGUI hourText;
    public TextMeshProUGUI minuteText;
    public TextMeshProUGUI dayCounterText;
    public bool isMidNight = false;

    [Header("Sun (Directional Light)")]
    public Light sunLight;

    [Header("Sun Rotation Settings")]
    public Vector3 sunriseRotation = new Vector3(20, 30, 0);
    public Vector3 sunsetRotation = new Vector3(200, 30, 0);

    public static event System.Action OnDayChanged;

    private int lastShownDay = -1;
    private float sliderLookupTimer;

    public void Start()
    {
        fadePanel.gameObject.SetActive(true);
        dayCount = PlayerPrefs.GetInt("DayCount", 1);
        UpdateDayCounter(dayCount);
        StartCoroutine(CheckMidNight());
    }

    public IEnumerator FadeIn()
    {
        fadePanel.gameObject.SetActive(true);
        Color startColor = fadePanel.color;
        float timer = 0f;
        fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            timer += Time.deltaTime;
            yield return null;
        }

        fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
    }

    public IEnumerator FadeOut()
    {
        fadePanel.gameObject.SetActive(true);
        Color startColor = fadePanel.color;
        float timer = 0f;
        fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, 1f);

        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            timer += Time.deltaTime;
            yield return null;
        }

        fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        fadePanel.gameObject.SetActive(false);
    }

    public IEnumerator CheckMidNight()
    {
        while (true)
        {
            if (UniStormSystem.Instance != null)
            {
                int hour = UniStormSystem.Instance.Hour;
                int minute = UniStormSystem.Instance.Minute;

                if (hour == 0 && minute == 0 && !isMidNight)
                {
                    isMidNight = true;
                    Debug.Log("[CheckMidNight] >>> GECE YARISI TETIKLENDI <<<");

                    yield return StartCoroutine(FadeIn());

                    dayCount++;
                    UpdateDayCounter(dayCount);
                    PlayerPrefs.SetInt("DayCount", dayCount);
                    PlayerPrefs.Save();
                    UniStormSystem.Instance.Morning();
                    UniStormSystem.Instance.UpdateTimeSlider();
                    Debug.Log("New day started at midnight.");
                    OnDayChanged?.Invoke();
                    yield return StartCoroutine(FadeOut());
                }
                else if ((hour != 0 || minute != 0) && isMidNight)
                {
                    isMidNight = false;
                    Debug.Log("[CheckMidNight] isMidNight sifirlandi.");
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator MidNight()
    {
        fadePanel.gameObject.SetActive(true);
        float timer = 0f;
        Color startColor = fadePanel.color;

        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            timer += Time.deltaTime;
            yield return null;
        }

        fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
    }

    void Update()
    {
        if (UniStormSystem.Instance != null)
        {
            int hour = UniStormSystem.Instance.Hour;
            int minute = UniStormSystem.Instance.Minute;

            hourText.text = hour.ToString("00");
            minuteText.text = minute.ToString("00");

            UpdateSunRotation(hour, minute);
        }

        TryBindTimeSlider();

        if (GameTime.Instance != null)
        {
            UpdateDayCounter(GameTime.Instance.dayCount);
        }
    }

    void saveobjevt()
    {
        ISaveable[] allSaveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();

        foreach (var save in allSaveables)
        {
            save.SaveData();
        }
    }

    void UpdateSunRotation(int hour, int minute)
    {
        if (sunLight == null)
        {
            return;
        }

        float normalizedTime = (hour * 60f + minute) / (24f * 60f);
        Vector3 targetRotation = Vector3.Lerp(sunriseRotation, sunsetRotation, normalizedTime);
        sunLight.transform.rotation = Quaternion.Euler(targetRotation);
    }

    private void TryBindTimeSlider()
    {
        if (timeSlider != null)
        {
            return;
        }

        sliderLookupTimer -= Time.deltaTime;
        if (sliderLookupTimer > 0f)
        {
            return;
        }

        sliderLookupTimer = 1f;
        GameObject sliderObj = GameObject.Find("UniStorm Canvas/Time Slider");
        if (sliderObj != null)
        {
            timeSlider = sliderObj.GetComponent<Slider>();
        }
    }

    private void UpdateDayCounter(int value)
    {
        if (dayCounterText == null || lastShownDay == value)
        {
            return;
        }

        lastShownDay = value;
        dayCounterText.text = $"Day {value}";
    }
}
