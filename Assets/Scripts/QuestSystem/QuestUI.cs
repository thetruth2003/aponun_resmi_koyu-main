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

    private void Update()
    {
        UpdateQuestUI();
    }

    public void UpdateQuestUI()
    {
        List<TrackedQuest> trackedList = ActiveQuestSystem.Instance.GetAllTracked();
        if (trackedList == null || trackedList.Count == 0)
        {
            mainQuestText.text = "No Quest Tracked";
            questTypeText.text = "";
            requirementText.text = "";
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
                int sold = GameStateTracker.Instance.GetCount(sell.GetProgressKey());
                requirementText.text = $"{sold}/{sell.requiredAmount}";
            }
            else if (step is BuyItemStep buy)
            {
                questTypeText.text = $"Buy {buy.requiredAmount} {buy.itemID}";
                int bought = GameStateTracker.Instance.GetCount(buy.GetProgressKey());
                requirementText.text = $"{bought}/{buy.requiredAmount}";
            }
            else if (step is HarvestItemStep harvest)
            {
                questTypeText.text = $"Harvest {harvest.requiredAmount} {harvest.itemID}";
                int harvested = GameStateTracker.Instance.GetCount(harvest.GetProgressKey());
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

        mainQuestText.text = "All quests complete!";
        questTypeText.text = "";
        requirementText.text = "";
    }
}
