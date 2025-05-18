using UnityEngine;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Data")]
    public QuestEditorAsset questChain;

    [Header("UI")]
    public QuestUI questUI;         // QuestUI komponentine referans
    public TMP_Text headerText;     // (İsteğe bağlı üst başlık)

    private int currentIndex = 0;
    private IQuestStep currentStep;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (questChain == null)
        {
            Debug.LogError("QuestChain atanmadı!");
            enabled = false;
            return;
        }
        LoadCurrentStep();
    }

    void CheckingQuest()
    {
        if (currentStep == null) return;

        currentStep.OnUpdate();

        if (currentStep.IsComplete())
        {
            currentIndex++;
            if (currentIndex < questChain.quests.Count)
            {
                LoadCurrentStep();
            }
            else
            {
                if (headerText != null)
                    headerText.text = "<color=#00FF00>✔️ All quests done!</color>";
                currentStep = null;
            }
            if (questUI != null)
                questUI.UpdateQuestUI();
        }
    }

    void LoadCurrentStep()
    {
        var container = questChain.quests[currentIndex];
        currentStep = container.GetStepInstance();
        currentStep.OnStart();

        if (headerText != null)
            headerText.text = $"<b>Quest {currentIndex + 1}:</b> {currentStep.GetName()}";

        if (questUI != null)
            questUI.UpdateQuestUI();
    }

    public int GetCurrentIndex() => currentIndex;
}
