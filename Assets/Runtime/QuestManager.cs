using System.Collections;
using UnityEngine;
using TMPro;  // ← TMP kütüphanesini ekledik

public class QuestManager : MonoBehaviour
{
    [Header("Quest Data")]
    public QuestChain questChain;         // Atayacağın QuestChain asset

    [Header("UI (TextMeshPro)")]
    public TMP_Text uiText;               // TextMeshPro – UGUI bileşeni

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
        // Şu anki adımı al, başlat ve UI'ı güncelle
        var container = questChain.quests[currentIndex];
        currentStep = container.GetStepInstance();
        currentStep.OnStart();

        uiText.text = $"<b>Görev {currentIndex + 1}:</b> {currentStep.GetName()}";
    }
}
