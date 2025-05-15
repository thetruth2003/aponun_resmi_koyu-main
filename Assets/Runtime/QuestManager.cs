using UnityEngine;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public QuestChain questChain;
    public TMP_Text uiText;

    private int currentIndex = 0;
    private IQuestStep currentStep;

    void Start()
    {
        if (questChain == null || uiText == null)
        {
            Debug.LogError("QuestChain veya uiText atanmamış!");
            enabled = false;
            return;
        }

        LoadCurrentStep();
    }

    void Update()
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
                uiText.text = "<color=#00FF00>✔️ Tüm görevler tamamlandı!</color>";
                currentStep = null;
            }
        }
    }

    void LoadCurrentStep()
    {
        var container = questChain.quests[currentIndex];
        currentStep = container.GetStepInstance();
        currentStep.OnStart();

        uiText.text = $"<b>Görev {currentIndex + 1}:</b> {currentStep.GetName()}";
    }
}
