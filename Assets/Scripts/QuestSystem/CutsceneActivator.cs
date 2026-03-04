using System.Collections.Generic;
using UnityEngine;

public class CutsceneActivator : MonoBehaviour
{
    public enum TriggerType
    {
        OnQuestStepReached,   // Belirli görev adımına gelince
        OnQuestCompleted,     // Görev tamamen bitince
        OnCutsceneFinished    // Başka bir cutscene bitince
    }

    [Header("Ne zaman tetiklensin?")]
    public TriggerType triggerType;

    [Header("Görev Koşulu")]
    public QuestEditorAsset quest;      // Takip ettiğin asset
    public int stepIndex = 0;           // 0-based, hangi adıma gelince çalışsın?

    [Header("Cutscene Koşulu")]
    public CutsceneClip waitForCutscene; // Bitmesini dinleyeceğin cutscene (OnCutsceneFinished ile çağıracaksın)

    [Header("Ne yapacağız?")]
    public List<GameObject> cutscenesToActivate = new List<GameObject>();
    public List<GameObject> cutscenesToDeactivate = new List<GameObject>();

    bool triggered = false;

    void OnEnable()
    {
        Debug.Log($"[CutsceneActivator:{name}] OnEnable, triggerType = {triggerType}");

        if (triggerType == TriggerType.OnQuestStepReached ||
            triggerType == TriggerType.OnQuestCompleted)
        {
            if (ActiveQuestSystem.Instance != null)
            {
                Debug.Log($"[CutsceneActivator:{name}] ActiveQuestSystem bulundu, event'e abone oluyorum.");
                ActiveQuestSystem.Instance.OnActiveStepChanged += OnQuestStepChanged;
            }
            else
            {
                Debug.LogWarning($"[CutsceneActivator:{name}] ActiveQuestSystem.Instance YOK, event'e abone olamıyorum!");
            }
        }
        // OnCutsceneFinished’da event’e script üzerinden bağlanacağız (UnityEvent)
    }

    void OnDisable()
    {
        if (ActiveQuestSystem.Instance != null)
            ActiveQuestSystem.Instance.OnActiveStepChanged -= OnQuestStepChanged;
    }

    void OnQuestStepChanged(QuestEditorAsset changedAsset, int newIndex)
    {
        // EN TEPE LOG
        Debug.Log(
            $"[CutsceneActivator:{name}] EVENT GELDI | changedAsset = {(changedAsset ? changedAsset.name : "NULL")}, " +
            $"newIndex = {newIndex}, myQuest = {(quest ? quest.name : "NULL")}, myStep = {stepIndex}, triggerType = {triggerType}"
        );

        if (triggered)
        {
            Debug.Log($"[CutsceneActivator:{name}] Zaten tetiklenmiş, dönüyorum.");
            return;
        }

        if (changedAsset != quest)
        {
            Debug.Log($"[CutsceneActivator:{name}] Asset uyuşmuyor, beni ilgilendirmiyor.");
            return;
        }

        switch (triggerType)
        {
            case TriggerType.OnQuestStepReached:
                // newIndex, aktif adım index’i. O adıma gelince çalıştır.
                if (newIndex == stepIndex)
                {
                    Debug.Log($"[CutsceneActivator:{name}] STEP EŞLEŞTİ (newIndex={newIndex}), Trigger çağrılmalı.");
                    //Trigger();
                }
                break;

            case TriggerType.OnQuestCompleted:
                // Görev son adımı geçince (index out of range) çalıştır.
                if (newIndex >= quest.quests.Count)
                {
                    Debug.Log($"[CutsceneActivator:{name}] GÖREV TAMAMLANDI, Trigger çağrılmalı.");
                    //Trigger();
                }
                break;
        }
    }

    /// <summary>
    /// Bunu cutscene'in bitiş event'inden çağıracaksın.
    /// </summary>
    public void OnCutsceneFinished()
    {
        Debug.Log($"[CutsceneActivator:{name}] OnCutsceneFinished çağrıldı.");

        if (triggerType != TriggerType.OnCutsceneFinished)
        {
            Debug.Log($"[CutsceneActivator:{name}] TriggerType OnCutsceneFinished değil, dönüyorum.");
            return;
        }

        if (triggered)
        {
            Debug.Log($"[CutsceneActivator:{name}] Zaten tetiklenmiş, dönüyorum.");
            return;
        }

        Trigger();
    }

    void Trigger()
    {
        Debug.Log($"[CutsceneActivator:{name}] TRIGGER ÇALIŞTI.");

        triggered = true;

        foreach (var go in cutscenesToActivate)
        {
            if (go != null)
            {
                Debug.Log($"[CutsceneActivator:{name}] Activate: {go.name}");
                go.SetActive(true);
            }
        }

        foreach (var go in cutscenesToDeactivate)
        {
            if (go != null)
            {
                Debug.Log($"[CutsceneActivator:{name}] Deactivate: {go.name}");
                go.SetActive(false);
            }
        }

        // Tek seferlik çalışsın istersen:
        enabled = false;
    }
}
