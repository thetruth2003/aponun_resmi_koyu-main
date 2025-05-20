using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class NPCInteraction : MonoBehaviour
{
    public NPCDialogData dialogData;
    public TextMeshProUGUI dialogText;
    public GameObject diyolog;
    public GameObject player;
    public SC_FPSController fpsController;
    public Camera currentCamera;
    public GameObject[] storedElements;
    public int currentSectionIndex = 0;

    public QuestEditorAsset linkedAsset;

    private List<DialogLine> currentLines = new List<DialogLine>();
    private int currentLine = 0;
    private bool isDialogActive = false;
    private AudioSource audioSource;

    private float originalFOV = 60f;
    public float zoomFOV = 45f;
    public float fovLerpSpeed = 20f;
    private bool isZoomFOVActive = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        fpsController = player.GetComponent<SC_FPSController>();
        if (currentCamera == null)
            currentCamera = Camera.main;
        originalFOV = currentCamera.fieldOfView;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    TalkToNPCStep FindMatchingStep()
    {
        if (linkedAsset == null) return null;
        var tracked = ActiveQuestSystem.Instance?.GetTracked(linkedAsset);
        if (tracked == null) return null;

        var container = tracked.GetActiveStep();
        var step = container?.GetStepInstance() as TalkToNPCStep;

        if (step != null && step.npcObject == this.gameObject)
            return step;

        return null;
    }

    public void StartDialog()
    {
        var step = FindMatchingStep();
        if (step == null)
        {
            Debug.LogWarning("[NPCInteraction] NPC’ye ait aktif adım bulunamadı.");
            return;
        }

        int sectionIndex = step.dialogSectionIndex;

        if (dialogData == null || sectionIndex < 0 || sectionIndex >= dialogData.sections.Count)
        {
            Debug.LogWarning("[NPCInteraction] Geçersiz diyalog index: " + sectionIndex);
            return;
        }

        currentLines = dialogData.sections[sectionIndex].lines;
        if (currentLines.Count == 0)
        {
            Debug.LogWarning("[NPCInteraction] Diyalog boş.");
            return;
        }

        currentLine = 0;
        isDialogActive = true;
        diyolog.SetActive(true);
        currentSectionIndex = sectionIndex;

        fpsController.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (GameObject obj in storedElements)
            if (obj != null) obj.SetActive(false);

        isZoomFOVActive = true;

        PlayCurrentLine();
    }

    void PlayCurrentLine()
    {
        var line = currentLines[currentLine];

        dialogText.text = line.text;

        if (audioSource.isPlaying)
            audioSource.Stop();

        if (line.voiceClip != null)
        {
            audioSource.clip = line.voiceClip;
            audioSource.Play();
            Debug.Log($"🔊 Ses oynatılıyor: {line.voiceClip.name}");
        }
    }

    void Update()
    {
        if (isDialogActive && Input.GetKeyDown(KeyCode.Space))
        {
            audioSource.Stop(); // geçerken sesi durdur

            currentLine++;
            if (currentLine < currentLines.Count)
                PlayCurrentLine();
            else
                EndDialog();
        }

        if (isZoomFOVActive && currentCamera.fieldOfView > zoomFOV)
            currentCamera.fieldOfView = Mathf.Lerp(currentCamera.fieldOfView, zoomFOV, Time.deltaTime * fovLerpSpeed);
        else if (!isZoomFOVActive && currentCamera.fieldOfView < originalFOV)
            currentCamera.fieldOfView = Mathf.Lerp(currentCamera.fieldOfView, originalFOV, Time.deltaTime * fovLerpSpeed);
    }

    void EndDialog()
    {
        isDialogActive = false;
        fpsController.enabled = true;
        diyolog.SetActive(false);
        audioSource.Stop();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (GameObject obj in storedElements)
            if (obj != null) obj.SetActive(true);

        isZoomFOVActive = false;

        // ✅ Diyalog tamamlandıktan sonra flag kaydet
        string npcName = gameObject.name.ToLower();
        string key = $"{npcName}_{currentSectionIndex}";
        GameStateTracker.Instance.SetFlag(key, true);
        Debug.Log($"[NPCInteraction] Diyalog tamamlandı, flag set: {key}");
    }
}
