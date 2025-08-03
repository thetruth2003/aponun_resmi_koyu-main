using UnityEngine;
using TMPro;

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

            mainQuestText.text = tracked.asset.quests[0].questName;


            // Alt görev tipi
            if (step is TalkToNPCStep talk)
            {
                questTypeText.text = $"Talk Whit {talk.npcID}";
                requirementText.text = !string.IsNullOrEmpty(talk.npcID)
                    ? ""
                    : "No NPC ID assigned";
            }
            else if (step is GoToLocationStep go)
            {
                questTypeText.text = $"{go.locationID} git";
                requirementText.text = !string.IsNullOrEmpty(go.locationID)
                    ? $""
                    : "No location ID assigned";
            }
            else if (step is SellItemStep sell)
            {
                questTypeText.text = $"{sell.requiredAmount} tane {sell.itemID} sat";
                int sold = GameStateTracker.Instance.GetCount($"Sold_{sell.itemID}");
                requirementText.text = $"{sold}/{sell.requiredAmount}";
            }
            else if (step is BuyItemStep buy)
            {
                questTypeText.text = $"{buy.requiredAmount} tane {buy.itemID} satın al";
                int bought = GameStateTracker.Instance.GetCount($"Bought_{buy.itemID}");
                requirementText.text = $"{bought}/{buy.requiredAmount}";
            }
            else if (step is HarvestItemStep harvest)
            {
                questTypeText.text = $"{harvest.requiredAmount} tane {harvest.itemID} hasat et";
                int harvested = GameStateTracker.Instance.GetCount($"Harvested_{harvest.itemID}");
                requirementText.text = $"{harvested}/{harvest.requiredAmount}";
            }
            else
            {
                questTypeText.text = "Unknown Step Type";
                requirementText.text = "";
            }

            return; // bulduğun ilk aktif görevden sonra UI güncellendi, çık
        }

        // Hiç görev bulunamadıysa:
        mainQuestText.text = "✔️ All quests complete!";
        questTypeText.text = "";
        requirementText.text = "";
    }
}
