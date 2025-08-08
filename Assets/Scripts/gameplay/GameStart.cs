using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniStorm;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEditor;

public class game_start : MonoBehaviour
{
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
    public Vector3 sunriseRotation = new Vector3(20, 30, 0); // 06:00
    public Vector3 sunsetRotation = new Vector3(200, 30, 0); // 18:00

    private void Start()
    {
        // Fade panel başlangıcı
        fadePanel.gameObject.SetActive(true);
        StartCoroutine(FadeOut());

        // Time Slider referansını al
        GameObject sliderObj = GameObject.Find("UniStorm Canvas/Time Slider");
        if (sliderObj != null)
            timeSlider = sliderObj.GetComponent<Slider>();

        // *** Gün sayısını yükle (Continue seçildiyse) ***
        // PlayerPrefs.GetInt("DayCount",1) → kayıt yoksa 1. günden başla
        GameTime.Instance.dayCount = PlayerPrefs.GetInt("DayCount", 1);
        dayCounterText.text = $"Day {GameTime.Instance.dayCount}";

        StartCoroutine(CheckMidNight());
    }

    public IEnumerator FadeOut()
    {
        float timer = 0f;
        Color startColor = fadePanel.color;

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
                    StartCoroutine(MidNight());
                    yield return new WaitForSeconds(fadeDuration);
                    saveobject();
                    UniStormSystem.Instance.Morning();
                    StartCoroutine(FadeOut());
                    GameTime.Instance.dayCount++;
                    PlayerPrefs.SetInt("DayCount", GameTime.Instance.dayCount);
                    dayCounterText.text = $"Day {GameTime.Instance.dayCount}";
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
        GameObject sliderObj = GameObject.Find("UniStorm Canvas/Time Slider");
        if (sliderObj != null)
            timeSlider = sliderObj.GetComponent<Slider>();
                // Güncellenmiş gün sayısını göster
        if (GameTime.Instance != null)
            dayCounterText.text = $"Day {GameTime.Instance.dayCount}";
    }

    void saveobject()
    {
        ISaveable[] allSaveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();

        foreach (var Save in allSaveables)
        {
            Save.SaveData();
        }
    }

    void UpdateSunRotation(int hour, int minute)
    {
        if (sunLight == null) return;

        // Gün içindeki zamanı 0.0 - 1.0 arası normalize et
        float normalizedTime = (hour * 60f + minute) / (24f * 60f);

        // Güneşin açısını sabah-akşam arasında Lerp’le döndür
        Vector3 targetRotation = Vector3.Lerp(sunriseRotation, sunsetRotation, normalizedTime);
        sunLight.transform.rotation = Quaternion.Euler(targetRotation);
    }
}