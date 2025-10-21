using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class CutsceneDialog : MonoBehaviour
{
    [Header("Data & UI")]
    public NPCDialogData dialogData;
    public TextMeshProUGUI dialogText;
    public GameObject DialogTextPanel;

    [Header("Play Mode")]
    public bool playOnAwake = true;
    public bool autoAdvanceAllSections = true;

    [Header("Timing (no voice)")]
    public float charsPerSecond = 18f;
    public float minLineDuration = 1.1f;
    public float maxLineDuration = 6.0f;
    public float punctuationPause = 0.25f;

    [Header("Timing (voice)")]
    public float extraVoicePadding = 0.3f;

    [Header("Optional Camera/Control")]
    public Camera targetCamera;
    public float cutsceneFOV = 45f;
    public float fovLerpSpeed = 20f;
    public Behaviour playerControllerToDisable;
    public GameObject[] hideWhilePlaying;

    [Header("Fade Panel")]
    [Tooltip("Full-screen siyah panel. CanvasGroup varsa onu, yoksa Image alpha’yı kullanır.")]
    public GameObject fadePanel;
    public float fadeInDuration  = 0.35f;  // 1 → 0 (açılma)
    public float fadeOutDuration = 0.35f;  // 0 → 1 (kapanma)
    public float fadeHold = 0.05f;         // küçük bekleme (realtime)
    public bool disableSelfOnEnd = true;

    // runtime
    private int sectionIndex = 0;
    private int lineIndex = 0;
    private List<DialogSection> sections;
    private AudioSource audioSource;           // opsiyonel
    private float originalFOV = 60f;
    private float lineTimer = 0f;
    private bool isPlaying = false;
    private Coroutine currentCo;

    // cached fade drivers
    private CanvasGroup fadeCg;
    private Image       fadeImg;

    void Awake()
    {
        if (dialogText == null)
        {
            var tagObj = GameObject.FindGameObjectWithTag("DialogText");
            if (tagObj) dialogText = tagObj.GetComponent<TextMeshProUGUI>();
        }

        targetCamera ??= Camera.main;
        if (targetCamera) originalFOV = targetCamera.fieldOfView;

        audioSource = GetComponent<AudioSource>(); // opsiyonel

        PrepareFadePanel();

        if (playOnAwake) StartCutscene();
    }

    void PrepareFadePanel()
    {
        if (!fadePanel) return;

        fadeCg  = fadePanel.GetComponent<CanvasGroup>();
        fadeImg = fadePanel.GetComponent<Image>();

        if (!fadeCg && !fadeImg) fadeCg = fadePanel.AddComponent<CanvasGroup>();

        var canvas = fadePanel.GetComponentInParent<Canvas>();
        if (canvas)
        {
            canvas.overrideSorting = true;
            if (canvas.sortingOrder < 5000) canvas.sortingOrder = 5000;
        }

        fadePanel.SetActive(true);
        SetFadeAlpha(1f); // güvenli başlangıç: siyah
    }

    public void StartCutscene()
    {
        if (currentCo != null) StopCoroutine(currentCo);
        currentCo = StartCoroutine(CoStartCutscene());
    }

    IEnumerator CoStartCutscene()
    {
        // === BAŞLANGIÇ: tek fade-in (1→0) ===
        if (fadePanel)
        {
            fadePanel.SetActive(true);
            SetFadeAlpha(1f);
            yield return FadeTo(0f, fadeInDuration);
            if (fadeHold > 0f) yield return new WaitForSecondsRealtime(fadeHold);
            fadePanel.SetActive(false);
        }

        // === ORTAMI HAZIRLA & DİYALOGU BAŞLAT ===
        if (playerControllerToDisable) playerControllerToDisable.enabled = false;
        if (hideWhilePlaying != null) foreach (var go in hideWhilePlaying) if (go) go.SetActive(false);

        if (dialogText) dialogText.gameObject.SetActive(true);
        if (DialogTextPanel) DialogTextPanel.SetActive(true);

        if (dialogData == null || dialogData.sections == null || dialogData.sections.Count == 0)
        {
            Debug.LogWarning("[CutsceneDialog] DialogData boş.");
            yield break;
        }

        sections = dialogData.sections;
        sectionIndex = 0;
        lineIndex = 0;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isPlaying = true;
        BeginSection(sectionIndex);
        PlayCurrentLine();
    }

    void BeginSection(int idx) => lineIndex = 0;

    void PlayCurrentLine()
    {
        var sec = sections[sectionIndex];
        if (sec.lines == null || sec.lines.Count == 0) { AdvanceSectionOrEnd(); return; }

        var line = sec.lines[lineIndex];
        if (dialogText) dialogText.text = line.text ?? string.Empty;

        if (audioSource && audioSource.isPlaying) audioSource.Stop();

        if (audioSource && line.voiceClip)
        {
            audioSource.clip = line.voiceClip;
            audioSource.Play();
            lineTimer = audioSource.clip.length + extraVoicePadding;
        }
        else
        {
            float t = EstimateTextDuration(line.text);
            lineTimer = Mathf.Clamp(t, minLineDuration, maxLineDuration);
        }
    }

    float EstimateTextDuration(string text)
    {
        if (string.IsNullOrEmpty(text)) return minLineDuration;
        float baseTime = text.Length / Mathf.Max(1f, charsPerSecond);
        int punct = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '.' || c == ',' || c == '!' || c == '?') punct++;
        }
        baseTime += punct * punctuationPause;
        return baseTime;
    }

    void Update()
    {
        if (!isPlaying) return;

        if (lineTimer > 0f) lineTimer -= Time.deltaTime;
        if (lineTimer <= 0f) AdvanceLine();

        if (targetCamera)
        {
            float target = cutsceneFOV;
            if (!isPlaying) target = originalFOV;
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, target, Time.deltaTime * fovLerpSpeed);
        }
    }

    void AdvanceLine()
    {
        if (audioSource && audioSource.isPlaying) audioSource.Stop();

        lineIndex++;
        var sec = sections[sectionIndex];
        if (lineIndex < sec.lines.Count) { PlayCurrentLine(); return; }

        AdvanceSectionOrEnd();
    }

    void AdvanceSectionOrEnd()
    {
        if (autoAdvanceAllSections)
        {
            sectionIndex++;
            if (sectionIndex < sections.Count)
            {
                BeginSection(sectionIndex);
                PlayCurrentLine();
                return;
            }
        }
        if (currentCo != null) StopCoroutine(currentCo);
        currentCo = StartCoroutine(CoEndCutscene());
    }

    IEnumerator CoEndCutscene()
    {
        isPlaying = false;
        if (fadePanel)
        {
            fadePanel.SetActive(true);
            SetFadeAlpha(0f);
            yield return FadeTo(1f, fadeOutDuration);
            if (fadeHold > 0f) yield return new WaitForSecondsRealtime(fadeHold);
        }


        if (audioSource && audioSource.isPlaying) audioSource.Stop();

        if (dialogText) dialogText.gameObject.SetActive(false);
        if (DialogTextPanel) DialogTextPanel.SetActive(false);

        if (playerControllerToDisable) playerControllerToDisable.enabled = true;
        if (hideWhilePlaying != null) foreach (var go in hideWhilePlaying) if (go) go.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // Manager'a "oynadı" işaretini gönder
        GetComponent<CutsceneClip>()?.Finish();

        if (disableSelfOnEnd) gameObject.SetActive(false);
        fadePanel.SetActive(false);
    }

    // ---------- Fade helpers ----------
    void SetFadeAlpha(float a)
    {
        if (!fadePanel) return;
        if (fadeCg)      fadeCg.alpha = a;
        else if (fadeImg){ var c = fadeImg.color; c.a = a; fadeImg.color = c; }
    }

    IEnumerator FadeTo(float target, float duration)
    {
        if (!fadePanel) yield break;

        float start;
        if      (fadeCg)  start = fadeCg.alpha;
        else if (fadeImg) start = fadeImg.color.a;
        else yield break;

        if (duration <= 0f) { SetFadeAlpha(target); yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // timescale'den bağımsız
            float k = Mathf.Clamp01(t / duration);
            SetFadeAlpha(Mathf.Lerp(start, target, k));
            yield return null;
        }
        SetFadeAlpha(target);
    }

    public void EndCutscene()
    {
        if (currentCo != null) StopCoroutine(currentCo);
        currentCo = StartCoroutine(CoEndCutscene());
    }
}
