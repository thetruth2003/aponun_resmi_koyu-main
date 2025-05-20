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

    public QuestEditorAsset linkedAsset;

    private List<DialogLine> currentLines = new List<DialogLine>();
    private int currentLine = 0;
    private bool isDialogActive = false;
    private int currentSectionIndex = 0;

    private float originalFOV = 60f;
    public float zoomFOV = 45f;
    public float fovLerpSpeed = 20f;
    private bool isZoomFOVActive = false;

    private AudioSource audioSource;

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

    public void StartDialog()
    {
        if (dialogData == null)
        {
            Debug.LogWarning("[NPCInteraction] dialogData boş.");
            return;
        }

        string npcKey = this.gameObject.name.ToLower();
        currentSectionIndex = GameStateTracker.Instance.GetDialogIndex(npcKey);


        if (currentSectionIndex >= dialogData.sections.Count)
        {
            Debug.LogWarning("[NPCInteraction] Geçersiz dialog bölümü!");
            return;
        }

        currentLines = dialogData.sections[currentSectionIndex].lines;
        if (currentLines.Count == 0)
        {
            Debug.LogWarning("[NPCInteraction] Diyalog bölümü boş.");
            return;
        }

        currentLine = 0;
        isDialogActive = true;
        diyolog.SetActive(true);

        fpsController.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (GameObject obj in storedElements)
            if (obj != null) obj.SetActive(false);

        isZoomFOVActive = true;

        PlayCurrentLine();
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
            Debug.Log($"🔊 Ses başlatıldı: {line.voiceClip.name}", line.voiceClip);
        }
        else
        {
            Debug.LogWarning($"⚠️ voiceClip atanmadı: currentLine = {currentLine}");
        }
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

        // ✅ viewKey'e göre step'i işaretle
        string viewKey = dialogData.sections[currentSectionIndex].viewKey;
        if (!string.IsNullOrEmpty(viewKey) && !GameStateTracker.Instance.GetFlag(viewKey))
        {
            GameStateTracker.Instance.SetFlag(viewKey, true);
            Debug.Log($"✅ Görev viewKey'e göre tamamlandı: {viewKey}");
        }
    }
}
