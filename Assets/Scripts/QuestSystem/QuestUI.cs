using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class QuestUI : MonoBehaviour
{
    public TMP_Text mainQuestText;
    public TMP_Text questTypeText;
    public TMP_Text requirementText;

    public void Update()
    {
        UpdateQuestUI();
    }

    public void UpdateQuestUI()
    {
        var trackedList = ActiveQuestSystem.Instance.GetAllTracked();
        if (trackedList == null || trackedList.Count == 0)
        {
            mainQuestText.text = "No Quest Tracked";
            questTypeText.text = "";
            requirementText.text = "";
            return;
        }

        foreach (var tracked in trackedList)
        {
            var index = tracked.currentIndex;

            if (tracked.asset == null || tracked.asset.quests == null || index >= tracked.asset.quests.Count)
                continue;

            var container = tracked.asset.quests[index];
            var step = container.GetStepInstance();

            if (step == null || step.IsComplete())
                continue;

            // ✅ aktif adımın container adını göster
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
                int sold = GameStateTracker.Instance.GetCount($"Sold_{sell.itemID}");
                requirementText.text = $"{sold}/{sell.requiredAmount}";
            }
            else if (step is BuyItemStep buy)
            {
                questTypeText.text = $"Buy {buy.requiredAmount} {buy.itemID}";
                int bought = GameStateTracker.Instance.GetCount($"Bought_{buy.itemID}");
                requirementText.text = $"{bought}/{buy.requiredAmount}";
            }
            else if (step is HarvestItemStep harvest)
            {
                questTypeText.text = $"Harvest {harvest.requiredAmount} {harvest.itemID}";
                int harvested = GameStateTracker.Instance.GetCount($"Harvested_{harvest.itemID}");
                requirementText.text = $"{harvested}/{harvest.requiredAmount}";
            }
            else
            {
                questTypeText.text = "Unknown Step Type";
                requirementText.text = "";
            }

            return; // ilk aktif görevi yaz ve çık
        }

        mainQuestText.text = "✔️ All quests complete!";
        questTypeText.text = "";
        requirementText.text = "";
    }
}
