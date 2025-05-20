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

    public TalkToNPCStep linkedStep;
    public QuestEditorAsset linkedAsset;

    private List<string> currentLines = new List<string>();
    private int currentLine = 0;
    private bool isDialogActive = false;

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
    }

    public void StartDialog()
{
    if (linkedStep == null || linkedAsset == null || linkedStep.npcObject == null)
    {
        Debug.LogWarning("[NPCInteraction] Eksik bağlantı: linkedStep, asset veya NPC nesnesi null.");
        return;
    }

    // Aktif görevi al
    var tracked = ActiveQuestSystem.Instance?.GetTracked(linkedAsset);
    if (tracked == null)
    {
        Debug.LogWarning("[NPCInteraction] linkedAsset takip edilmiyor.");
        return;
    }

    // Şu anki aktif adım ile bu NPC'nin adımı eşleşiyor mu? (== değil, içerik bazlı kıyas)
    bool isMyStepActive = false;
    if (tracked.currentIndex < linkedAsset.quests.Count)
    {
        var activeStep = linkedAsset.quests[tracked.currentIndex].GetStepInstance();

        if (activeStep is TalkToNPCStep currentTalkStep &&
            currentTalkStep.npcObject == linkedStep.npcObject &&
            currentTalkStep.dialogSectionIndex == linkedStep.dialogSectionIndex)
        {
            isMyStepActive = true;
        }
    }

    if (!isMyStepActive)
    {
        Debug.Log($"[NPCInteraction] Bu step şu anda aktif değil: {linkedStep.npcObject.name} / section {linkedStep.dialogSectionIndex}");
        return;
    }

    int sectionIndex = linkedStep.dialogSectionIndex;

    if (dialogData == null || sectionIndex >= dialogData.sections.Count)
    {
        Debug.LogWarning("[NPCInteraction] Geçersiz dialog bölümü!");
        return;
    }

    currentLines = dialogData.sections[sectionIndex].lines;
    if (currentLines.Count == 0)
    {
        Debug.LogWarning("[NPCInteraction] Diyalog bölümü boş.");
        return;
    }

    // Diyalog başlat
    currentLine = 0;
    isDialogActive = true;
    diyolog.SetActive(true);
    dialogText.text = currentLines[currentLine];

    fpsController.enabled = false;
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;

    foreach (GameObject obj in storedElements)
    {
        if (obj != null) obj.SetActive(false);
    }

    isZoomFOVActive = true;

    // ✅ GameState'e işaretleme (Talked_ahmet_0 gibi)
    string npcName = linkedStep.npcObject.name.ToLower();
    string stateKey = $"Talked_{npcName}_{sectionIndex}";
    GameStateTracker.Instance.SetFlag(stateKey, true);
    Debug.Log($"✅ GameState yazıldı: {stateKey} = true");

    // ✅ Görevi tamamla
    linkedStep.MarkCompleted();

    // ✅ Bir sonraki adıma geç
    tracked.currentIndex++;
    Debug.Log($"[ActiveQuestSystem] Görev tamamlandı, sıradakine geçildi. Yeni index: {tracked.currentIndex}");
}


    void Update()
    {
        if (isDialogActive && Input.GetKeyDown(KeyCode.Space))
        {
            currentLine++;
            if (currentLine < currentLines.Count)
            {
                dialogText.text = currentLines[currentLine];
            }
            else
            {
                EndDialog();
            }
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (GameObject obj in storedElements)
        {
            if (obj != null) obj.SetActive(true);
        }

        isZoomFOVActive = false;
    }
}
