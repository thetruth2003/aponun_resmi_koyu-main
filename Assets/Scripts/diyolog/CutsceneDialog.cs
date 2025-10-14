using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CutsceneDialog : MonoBehaviour
{
    [Header("Data & UI")]
    public NPCDialogData dialogData;          // ScriptableObject: sections -> lines
    public TextMeshProUGUI dialogText;        // Ekranda altyazı yazılacak yer
    public GameObject DialogTextPanel;        // Ekranda altyazı yazılacak yer

    [Header("Play Mode")]
    public bool playOnAwake = true;           // Awake'te otomatik başlat
    public bool autoAdvanceAllSections = true;// Tüm section'ları sırayla bitir

    [Header("Timing (no voice)")]
    [Tooltip("Metin hızına göre satır süresi tahmini (karakter/sn).")]
    public float charsPerSecond = 18f;
    public float minLineDuration = 1.1f;
    public float maxLineDuration = 6.0f;
    public float punctuationPause = 0.25f;    // . , ! ? başına ek pause

    [Header("Timing (voice)")]
    [Tooltip("Ses klibi bittikten sonra beklenen ek süre.")]
    public float extraVoicePadding = 0.3f;

    [Header("Optional Camera/Control")]
    public Camera targetCamera;
    public float cutsceneFOV = 45f;
    public float fovLerpSpeed = 20f;
    public Behaviour playerControllerToDisable; // SC_FPSController gibi bir component
    public GameObject[] hideWhilePlaying;       // HUD vb. gizlemek için

    // runtime
    private int sectionIndex = 0;
    private int lineIndex = 0;
    private List<DialogSection> sections;
    private AudioSource audioSource;
    private float originalFOV = 60f;
    private float lineTimer = 0f;
    private bool isPlaying = false;

    void Awake()
    {
        if (dialogText == null)
        {
            var tagObj = GameObject.FindGameObjectWithTag("DialogText");
            if (tagObj != null) dialogText = tagObj.GetComponent<TextMeshProUGUI>();
        }

        targetCamera = targetCamera ?? Camera.main;
        if (targetCamera != null) originalFOV = targetCamera.fieldOfView;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (playOnAwake) StartCutscene();
    }

    public void StartCutscene()
    {
        DialogTextPanel.SetActive(true);
        if (dialogData == null || dialogData.sections == null || dialogData.sections.Count == 0)
        {
            Debug.LogWarning("[CutsceneDialog] DialogData boş.");
            return;
        }

        sections = dialogData.sections;
        sectionIndex = 0;
        lineIndex = 0;

        // environment setup
        if (playerControllerToDisable != null) playerControllerToDisable.enabled = false;
        if (hideWhilePlaying != null)
            foreach (var go in hideWhilePlaying) if (go) go.SetActive(false);

        if (dialogText != null) dialogText.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isPlaying = true;
        BeginSection(sectionIndex);
        PlayCurrentLine();
    }

    void BeginSection(int idx)
    {
        // Eğer bir sinematik/view sistemin varsa, buradan tetikle:
        // var key = sections[idx].viewKey;
        // if (!string.IsNullOrEmpty(key)) ViewSystem.Show(key);
        lineIndex = 0;
    }

    void PlayCurrentLine()
    {
        var sec = sections[sectionIndex];
        if (sec.lines == null || sec.lines.Count == 0) { AdvanceSectionOrEnd(); return; }

        var line = sec.lines[lineIndex];
        if (dialogText != null) dialogText.text = line.text ?? string.Empty;

        if (audioSource.isPlaying) audioSource.Stop();

        if (line.voiceClip != null)
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

        // otomatik akış (input yok)
        if (lineTimer > 0f) lineTimer -= Time.deltaTime;
        if (lineTimer <= 0f) AdvanceLine();

        // yumuşak FOV
        if (targetCamera != null)
        {
            float target = cutsceneFOV;
            if (!isPlaying) target = originalFOV;
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, target, Time.deltaTime * fovLerpSpeed);
        }
    }

    void AdvanceLine()
    {
        // mevcut ses çalıyorsa durdur
        if (audioSource.isPlaying) audioSource.Stop();

        lineIndex++;
        var sec = sections[sectionIndex];
        if (lineIndex < sec.lines.Count)
        {
            PlayCurrentLine();
            return;
        }

        // bölüm bitti
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

        EndCutscene();
    }

    public void EndCutscene()
    {
        isPlaying = false;

        if (audioSource.isPlaying) audioSource.Stop();
        if (dialogText != null) dialogText.gameObject.SetActive(false);

        if (playerControllerToDisable != null) playerControllerToDisable.enabled = true;
        if (hideWhilePlaying != null)
            foreach (var go in hideWhilePlaying) if (go) go.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        DialogTextPanel.SetActive(false);
        // burada istersen bir event tetikleyebilirsin (Quest ilerletmek vb.)
    }
}
