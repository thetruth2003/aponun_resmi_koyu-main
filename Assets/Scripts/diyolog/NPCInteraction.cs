using TMPro;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(UniversalIdentifier))]
public class NPCInteraction : MonoBehaviour
{
    public string npcID; // Inspector’dan girilebilir, ama Start'ta da UniversalIdentifier üzerinden çözülür
    public TextMeshProUGUI dialogText;
    public GameObject player;
    public SC_FPSController fpsController;
    public Camera currentCamera;
    public GameObject[] storedElements;
    public QuestEditorAsset linkedAsset;

    public NPCDialogData dialogData;
    public List<DialogLine> currentLines = new List<DialogLine>();
    public int currentLine = 0;
    public bool isDialogActive = false;
    public AudioSource audioSource;

    private float originalFOV = 60f;
    public float zoomFOV = 45f;
    public float fovLerpSpeed = 20f;
    private bool isZoomFOVActive = false;
    private int currentSectionIndex = 0;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        fpsController = player.GetComponent<SC_FPSController>();
        currentCamera = currentCamera ?? Camera.main;
        originalFOV = currentCamera.fieldOfView;

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        // Universal ID çözümleme
        if (string.IsNullOrEmpty(npcID))
            npcID = GetComponent<UniversalIdentifier>()?.ID;

        if (string.IsNullOrEmpty(npcID))
        {
            Debug.LogWarning($"{gameObject.name} → npcID atanamamış!");
            return;
        }

        // ⛳️ TAG ile TextMeshPro yazısını bul
        if (dialogText == null)
        {
            GameObject dialogObj = GameObject.FindGameObjectWithTag("DialogText");
            if (dialogObj != null)
            {
                dialogText = dialogObj.GetComponent<TextMeshProUGUI>();
            }

            if (dialogText == null)
            {
                Debug.LogError("[NPCInteraction] 'DialogText' tagine sahip bir TextMeshProUGUI bulunamadı!");
            }
        }
        if (dialogText != null)
        Debug.Log("✅ DialogText bulundu: " + dialogText.gameObject.name);
        else
        Debug.LogError("❌ DialogText bulunamadı, metin gösterilmeyecek.");

    }


    TalkToNPCStep FindMatchingStep()
    {
        if (linkedAsset == null) return null;
        var tracked = ActiveQuestSystem.Instance?.GetTracked(linkedAsset);
        if (tracked == null) return null;

        var container = tracked.GetActiveStep();
        var step = container?.GetStepInstance() as TalkToNPCStep;

        if (step != null && step.npcID == npcID)
            return step;

        return null;
    }

    public void StartDialog()
    {
        var step = FindMatchingStep();
        if (step == null)
        {
            Debug.LogWarning("[NPCInteraction] Bu NPC'ye ait aktif TalkToNPC adımı yok.");
            return;
        }

        int sectionIndex = step.dialogSectionIndex;
        if (dialogData == null || sectionIndex < 0 || sectionIndex >= dialogData.sections.Count)
        {
            Debug.LogWarning($"[NPCInteraction] Geçersiz diyalog indexi: {sectionIndex}");
            return;
        }

        currentLines = dialogData.sections[sectionIndex].lines;
        if (currentLines.Count == 0)
        {
            Debug.LogWarning("[NPCInteraction] Diyalog bölümü boş.");
            return;
        }
        dialogText.gameObject.SetActive(true);
        currentLine = 0;
        isDialogActive = true;
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
            Debug.Log($"🔊 Oynatılıyor: {line.voiceClip.name}");
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
        audioSource.Stop();
        dialogText.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (GameObject obj in storedElements)
            if (obj != null) obj.SetActive(true);

        isZoomFOVActive = false;

        string key = $"{npcID.ToLower()}_{currentSectionIndex}";
        GameStateTracker.Instance.SetFlag(key, true);
        Debug.Log($"✅ Diyalog tamamlandı, flag ayarlandı: {key}");
    }
}
