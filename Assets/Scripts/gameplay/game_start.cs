using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniStorm;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class game_start : MonoBehaviour
{
    public Image fadePanel;
    public float fadeDuration = 2f;
    public Slider time;
    public TextMeshProUGUI hourText;
    public TextMeshProUGUI minuteText;

    private bool isMidNight = false;

    private void Start()
    {
        // Başlangıçta ekran siyah olacak ve sonra yavaşça şeffaflaşacak
        fadePanel.gameObject.SetActive(true);
        StartCoroutine(FadeIn());
        StartCoroutine(CheckMidNight());
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;
        Color startColor = fadePanel.color;

        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration); // Ekranı şeffaflaştır
            fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            timer += Time.deltaTime;
            yield return null;
        }

        // Tamamen şeffaf yap
        fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        fadePanel.gameObject.SetActive(false); // Paneli kapat
    }

    private IEnumerator CheckMidNight()
    {
        while (true)
        {
            if (UniStormSystem.Instance != null)
            {
                int hour = UniStormSystem.Instance.Hour;
                int minute = UniStormSystem.Instance.Minute;

                if (hour == 0 && minute == 0 && !isMidNight)  // 00:00 kontrolü
                {
                    isMidNight = true;
                    StartCoroutine(MidNight());
                    yield return new WaitForSeconds(fadeDuration); // Fade tamamlanana kadar bekle
                    saveobjevt(); // Veriyi kaydet
                    UniStormSystem.Instance.Morning();
                    StartCoroutine(FadeIn());
                }
            }
            yield return new WaitForSeconds(1f); // 1 saniyede bir kontrol et
        }
    }
    private IEnumerator MidNight()
    {
        fadePanel.gameObject.SetActive(true); // Paneli tekrar aç
        float timer = 0f;
        Color startColor = fadePanel.color;

        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration); // Ekranı siyahla
            fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            timer += Time.deltaTime;
            yield return null;
        }

        // Tamamen siyah yap
        fadePanel.color = new Color(startColor.r, startColor.g, startColor.b, 1f);

    }

    void Update()
    {
        if (UniStormSystem.Instance != null)
        {
            int hour = UniStormSystem.Instance.Hour;
            int minute = UniStormSystem.Instance.Minute;

            hourText.text = hour.ToString("00");   // 01, 02, ..., 23 gibi gösterim
            minuteText.text = minute.ToString("00");
        }
    }

    void saveobjevt()
    {
        ISaveable[] allSaveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();

        foreach (var Save in allSaveables)
        {
            Save.SaveData();
        }
    }
}
