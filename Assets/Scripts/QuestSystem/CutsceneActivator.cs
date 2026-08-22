using System.Collections.Generic;
using UnityEngine;
using System.Collections;

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
    public float triggerDelay = 0f;

    [Header("Ne yapacagiz?")]
    public List<GameObject> cutscenesToActivate = new List<GameObject>();
    public List<GameObject> cutscenesToDeactivate = new List<GameObject>();

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private bool triggered = false;
    private Coroutine waitForQuestSystemRoutine;

    private void OnEnable()
    {
        if (triggerType == TriggerType.OnQuestStepReached ||
            triggerType == TriggerType.OnQuestCompleted)
        {
            TrySubscribeQuestSystem();
        }

        if (triggerType == TriggerType.OnCutsceneFinished)
        {
            CutsceneClip.AnyClipFinished += HandleWatchedCutsceneFinished;
        }
    }

    private void OnDisable()
    {
        if (waitForQuestSystemRoutine != null)
        {
            StopCoroutine(waitForQuestSystemRoutine);
            waitForQuestSystemRoutine = null;
        }

        if (ActiveQuestSystem.Instance != null)
        {
            ActiveQuestSystem.Instance.OnActiveStepChanged -= OnQuestStepChanged;
        }

        CutsceneClip.AnyClipFinished -= HandleWatchedCutsceneFinished;
    }

    private void OnQuestStepChanged(QuestEditorAsset changedAsset, int newIndex)
    {
        if (triggered)
        {
            return;
        }

        if (changedAsset != quest)
        {
            return;
        }

        switch (triggerType)
        {
            case TriggerType.OnQuestStepReached:
                if (newIndex == stepIndex)
                {
                    Trigger();
                }
                break;

            case TriggerType.OnQuestCompleted:
                if (newIndex >= quest.quests.Count)
                {
                    Trigger();
                }
                break;
        }
    }

    /// <summary>
    /// Bunu cutscene bittiginde ilgili eventten cagirirsin.
    /// </summary>
    public void OnCutsceneFinished()
    {
        if (triggerType != TriggerType.OnCutsceneFinished)
        {
            return;
        }

        if (triggered)
        {
            return;
        }

        TryTriggerWithDelay();
    }

    private void Trigger()
    {
        triggered = true;

        foreach (GameObject go in cutscenesToActivate)
        {
            if (go != null)
            {
                go.SetActive(true);
            }
        }

        foreach (GameObject go in cutscenesToDeactivate)
        {
            if (go != null)
            {
                go.SetActive(false);
            }
        }

        enabled = false;
    }

    private void HandleWatchedCutsceneFinished(CutsceneClip finishedClip)
    {
        if (triggerType != TriggerType.OnCutsceneFinished)
            return;

        if (triggered)
            return;

        if (!waitForCutscene || finishedClip != waitForCutscene)
            return;

        TryTriggerWithDelay();
    }

    private void TryTriggerWithDelay()
    {
        if (triggerDelay <= 0f)
        {
            Trigger();
            return;
        }

        StartCoroutine(CoTriggerAfterDelay());
    }

    private IEnumerator CoTriggerAfterDelay()
    {
        yield return new WaitForSeconds(triggerDelay);
        Trigger();
    }

    private void EvaluateCurrentQuestState()
    {
        if (triggered || quest == null || ActiveQuestSystem.Instance == null)
            return;

        var tracked = ActiveQuestSystem.Instance.GetTracked(quest);
        if (tracked == null)
            return;

        switch (triggerType)
        {
            case TriggerType.OnQuestStepReached:
                if (tracked.currentIndex == stepIndex)
                {
                    Trigger();
                }
                break;

            case TriggerType.OnQuestCompleted:
                if (quest.quests != null && tracked.currentIndex >= quest.quests.Count)
                {
                    Trigger();
                }
                break;
        }
    }

    private void TrySubscribeQuestSystem()
    {
        ActiveQuestSystem activeQuestSystem = ActiveQuestSystem.Instance;
        if (activeQuestSystem != null)
        {
            activeQuestSystem.OnActiveStepChanged -= OnQuestStepChanged;
            activeQuestSystem.OnActiveStepChanged += OnQuestStepChanged;
            EvaluateCurrentQuestState();
            LogVerbose("Quest event aboneligi aktif.");
            return;
        }

        if (waitForQuestSystemRoutine == null && isActiveAndEnabled)
        {
            waitForQuestSystemRoutine = StartCoroutine(WaitForQuestSystemAndSubscribe());
        }
    }

    private IEnumerator WaitForQuestSystemAndSubscribe()
    {
        LogVerbose("ActiveQuestSystem bekleniyor.");

        while (isActiveAndEnabled && ActiveQuestSystem.Instance == null)
        {
            yield return null;
        }

        waitForQuestSystemRoutine = null;

        if (isActiveAndEnabled)
        {
            TrySubscribeQuestSystem();
        }
    }

    private void LogVerbose(string message)
    {
        if (verboseLogs)
        {
            Debug.Log($"[CutsceneActivator:{name}] {message}", this);
        }
    }
}
