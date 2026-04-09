using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CutsceneActivator, gorev veya cutscene olaylarina gore sahnedeki cutscene objelerini acip kapatir.
/// </summary>
public class CutsceneActivator : MonoBehaviour
{
    /// <summary>
    /// Tetikleme zamanini belirleyen modlari tutar.
    /// </summary>
    public enum TriggerType
    {
        OnQuestStepReached,
        OnQuestCompleted,
        OnCutsceneFinished
    }

    [Header("Ne zaman tetiklensin?")]
    public TriggerType triggerType;

    [Header("Gorev Kosulu")]
    public QuestEditorAsset quest;
    public int stepIndex = 0;

    [Header("Cutscene Kosulu")]
    public CutsceneClip waitForCutscene;

    [Header("Ne yapacagiz?")]
    public List<GameObject> cutscenesToActivate = new List<GameObject>();
    public List<GameObject> cutscenesToDeactivate = new List<GameObject>();

    private bool triggered = false;

    private void OnEnable()
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
                Debug.LogWarning($"[CutsceneActivator:{name}] ActiveQuestSystem.Instance yok, event'e abone olamiyorum!");
            }
        }
    }

    private void OnDisable()
    {
        if (ActiveQuestSystem.Instance != null)
        {
            ActiveQuestSystem.Instance.OnActiveStepChanged -= OnQuestStepChanged;
        }
    }

    private void OnQuestStepChanged(QuestEditorAsset changedAsset, int newIndex)
    {
        Debug.Log(
            $"[CutsceneActivator:{name}] EVENT GELDI | changedAsset = {(changedAsset ? changedAsset.name : "NULL")}, " +
            $"newIndex = {newIndex}, myQuest = {(quest ? quest.name : "NULL")}, myStep = {stepIndex}, triggerType = {triggerType}"
        );

        if (triggered)
        {
            Debug.Log($"[CutsceneActivator:{name}] Zaten tetiklenmis, donuyorum.");
            return;
        }

        if (changedAsset != quest)
        {
            Debug.Log($"[CutsceneActivator:{name}] Asset uyusmuyor, beni ilgilendirmiyor.");
            return;
        }

        switch (triggerType)
        {
            case TriggerType.OnQuestStepReached:
                if (newIndex == stepIndex)
                {
                    Debug.Log($"[CutsceneActivator:{name}] STEP ESLESTI (newIndex={newIndex}), Trigger cagrilmali.");
                }
                break;

            case TriggerType.OnQuestCompleted:
                if (newIndex >= quest.quests.Count)
                {
                    Debug.Log($"[CutsceneActivator:{name}] GOREV TAMAMLANDI, Trigger cagrilmali.");
                }
                break;
        }
    }

    /// <summary>
    /// Bunu cutscene bittiginde ilgili eventten cagirirsin.
    /// </summary>
    public void OnCutsceneFinished()
    {
        Debug.Log($"[CutsceneActivator:{name}] OnCutsceneFinished cagrildi.");

        if (triggerType != TriggerType.OnCutsceneFinished)
        {
            Debug.Log($"[CutsceneActivator:{name}] TriggerType OnCutsceneFinished degil, donuyorum.");
            return;
        }

        if (triggered)
        {
            Debug.Log($"[CutsceneActivator:{name}] Zaten tetiklenmis, donuyorum.");
            return;
        }

        Trigger();
    }

    private void Trigger()
    {
        Debug.Log($"[CutsceneActivator:{name}] TRIGGER CALISTI.");

        triggered = true;

        foreach (GameObject go in cutscenesToActivate)
        {
            if (go != null)
            {
                Debug.Log($"[CutsceneActivator:{name}] Activate: {go.name}");
                go.SetActive(true);
            }
        }

        foreach (GameObject go in cutscenesToDeactivate)
        {
            if (go != null)
            {
                Debug.Log($"[CutsceneActivator:{name}] Deactivate: {go.name}");
                go.SetActive(false);
            }
        }

        enabled = false;
    }
}
