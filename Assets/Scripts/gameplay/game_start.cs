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

    private bool isMidNight = false;

    [Header("Sun (Directional Light)")]
    public Light sunLight;

    [Header("Sun Rotation Settings")]
    public Vector3 sunriseRotation = new Vector3(20, 30, 0); // 06:00
    public Vector3 sunsetRotation = new Vector3(200, 30, 0); // 18:00

    private void Start()
    {
        fadePanel.gameObject.SetActive(true);
        StartCoroutine(FadeIn());
        GameObject sliderObj = GameObject.Find("UniStorm Canvas/Time Slider");
        if (sliderObj != null)
            timeSlider = sliderObj.GetComponent<Slider>();
        StartCoroutine(CheckMidNight());
    }

    private IEnumerator FadeIn()
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

    private IEnumerator CheckMidNight()
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
                    saveobjevt();
                    UniStormSystem.Instance.Morning();
                    StartCoroutine(FadeIn());
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator MidNight()
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
    }

    void saveobjevt()
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

