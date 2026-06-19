using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TrackedQuest = ActiveQuestSystem.TrackedQuest;

/// <summary>
/// QuestUI, takip edilen aktif gorevin mevcut adimini ekranda gosterir.
/// </summary>
public class QuestUI : MonoBehaviour
{
    public TMP_Text mainQuestText;
    public TMP_Text questTypeText;
    public TMP_Text requirementText;

    private bool warnedMissingQuestSystem;
    private bool warnedMissingTextReferences;
    private bool warnedMissingGameStateTracker;

    private void Update()
    {
        UpdateQuestUI();
    }

    public void UpdateQuestUI()
    {
        if (!HasTextReferences())
        {
            return;
        }

        ActiveQuestSystem activeQuestSystem = ActiveQuestSystem.Instance;
        if (activeQuestSystem == null)
        {
            if (!warnedMissingQuestSystem)
            {
                Debug.LogWarning("[QuestUI] ActiveQuestSystem.Instance bulunamadi. Quest UI beklemeye alindi.");
                warnedMissingQuestSystem = true;
            }

            SetTexts("No Quest System", "", "");
            return;
        }

        warnedMissingQuestSystem = false;

        List<TrackedQuest> trackedList = activeQuestSystem.GetAllTracked();
        if (trackedList == null || trackedList.Count == 0)
        {
            SetTexts("No Quest Tracked", "", "");
            return;
        }

        foreach (TrackedQuest tracked in trackedList)
        {
            int index = tracked.currentIndex;

            if (tracked.asset == null || tracked.asset.quests == null || index >= tracked.asset.quests.Count)
            {
                continue;
            }

            QuestContainer container = tracked.asset.quests[index];
            IQuestStep step = container.GetStepInstance();

            if (step == null || step.IsComplete())
            {
                continue;
            }

            mainQuestText.text = container.questName;

            if (step is TalkToNPCStep talk)
            {
                questTypeText.text = $"Talk With {talk.npcID}";
                requirementText.text = !string.IsNullOrEmpty(talk.npcID) ? "" : "No NPC ID assigned";
            }
            else if (step is GoToLocationStep go)
            {
                questTypeText.text = $"Go To {go.locationID}";
                requirementText.text = !string.IsNullOrEmpty(go.locationID) ? "" : "No location ID assigned";
            }
            else if (step is SellItemStep sell)
            {
                questTypeText.text = $"Sell {sell.requiredAmount} {sell.itemID}";
                int sold = GetTrackedCountOrZero(sell.GetProgressKey());
                requirementText.text = $"{sold}/{sell.requiredAmount}";
            }
            else if (step is BuyItemStep buy)
            {
                questTypeText.text = $"Buy {buy.requiredAmount} {buy.itemID}";
                int bought = GetTrackedCountOrZero(buy.GetProgressKey());
                requirementText.text = $"{bought}/{buy.requiredAmount}";
            }
            else if (step is HarvestItemStep harvest)
            {
                questTypeText.text = $"Harvest {harvest.requiredAmount} {harvest.itemID}";
                int harvested = GetTrackedCountOrZero(harvest.GetProgressKey());
                requirementText.text = $"{harvested}/{harvest.requiredAmount}";
            }
            else if (step is ChoiceStep choice)
            {
                questTypeText.text = $"Choice: {choice.choiceKey}";
                string currentValue = choice.GetCurrentValue();
                requirementText.text = string.IsNullOrWhiteSpace(currentValue)
                    ? "Waiting for player choice"
                    : $"Selected: {currentValue}";
            }
            else if (step is ModifyStatStep modify)
            {
                questTypeText.text = "Apply state result";
                requirementText.text = modify.GetName();
            }
            else if (step is SpendMoneyStep spend)
            {
                questTypeText.text = $"Pay for {spend.spendKey}";
                requirementText.text = $"{spend.GetCurrentProgress()}/{spend.requiredAmount} TL";
            }
            else if (step is WaitForDayStep waitForDay)
            {
                questTypeText.text = "Wait for day";
                requirementText.text = $"Day {waitForDay.GetCurrentDay()} / {waitForDay.GetTargetDay()}";
            }
            else
            {
                questTypeText.text = "Unknown Step Type";
                requirementText.text = "";
            }

            return;
        }

        SetTexts("All quests complete!", "", "");
    }

    private bool HasTextReferences()
    {
        if (mainQuestText != null && questTypeText != null && requirementText != null)
        {
            warnedMissingTextReferences = false;
            return true;
        }

        if (!warnedMissingTextReferences)
        {
            Debug.LogWarning("[QuestUI] TMP_Text alanlarindan biri eksik. Inspector atamalarini kontrol et.");
            warnedMissingTextReferences = true;
        }

        return false;
    }

    private void SetTexts(string main, string type, string requirement)
    {
        mainQuestText.text = main;
        questTypeText.text = type;
        requirementText.text = requirement;
    }

    private int GetTrackedCountOrZero(string key)
    {
        if (GameStateTracker.Instance != null)
        {
            warnedMissingGameStateTracker = false;
            return GameStateTracker.Instance.GetCount(key);
        }

        if (!warnedMissingGameStateTracker)
        {
            Debug.LogWarning("[QuestUI] GameStateTracker.Instance bulunamadi. Ilerleme 0 olarak gosterilecek.");
            warnedMissingGameStateTracker = true;
        }

        return 0;
    }
}
