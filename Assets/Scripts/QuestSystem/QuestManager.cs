using TMPro;
using UnityEngine;

/// <summary>
/// QuestManager, tek bir gorev zincirinin aktif adimini ilerletip UI ile senkron tutar.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Data")]
    public QuestEditorAsset questChain;

    [Header("UI")]
    public QuestUI questUI;
    public TMP_Text headerText;

    private int currentIndex = 0;
    private IQuestStep currentStep;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (questChain == null)
        {
            Debug.LogError("QuestChain atanmadi!");
            enabled = false;
            return;
        }

        LoadCurrentStep();
    }

    private void Update()
    {
        CheckingQuest();
    }

    private void CheckingQuest()
    {
        if (currentStep == null)
        {
            return;
        }

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
                {
                    headerText.text = "<color=#00FF00>All quests done!</color>";
                }

                currentStep = null;
            }

            if (questUI != null)
            {
                questUI.UpdateQuestUI();
            }
        }
    }

    private void LoadCurrentStep()
    {
        QuestContainer container = questChain.quests[currentIndex];
        currentStep = container.GetStepInstance();
        currentStep.OnStart();

        if (headerText != null)
        {
            headerText.text = $"<b>Quest {currentIndex + 1}:</b> {currentStep.GetName()}";
        }

        if (questUI != null)
        {
            questUI.UpdateQuestUI();
        }
    }

    public int GetCurrentIndex() => currentIndex;
}
