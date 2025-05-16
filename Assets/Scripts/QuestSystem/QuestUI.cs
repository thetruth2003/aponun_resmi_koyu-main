using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    public QuestChain questChain;
    public TMP_Text questTypeText;
    public TMP_Text targetNameText;

    void Start()
    {
        UpdateQuestUI();
    }

    public void UpdateQuestUI()
    {
        if (questChain == null || questChain.quests.Count == 0)
        {
            questTypeText.text = "No Quest";
            targetNameText.text = "";
            return;
        }

        // İlk görev adımını al
        var firstQuest = questChain.quests[0];
        var step = firstQuest.GetStepInstance();

        // Hangi step tipiyse ona göre başlık ve sayaç göster
        if (step is TalkToNPCStep talk)
        {
            questTypeText.text = "Talk To NPC";
            targetNameText.text = talk.npcObject != null ? talk.npcObject.name : "No NPC Assigned";
        }
        else if (step is GoToLocationStep goTo)
        {
            questTypeText.text = "Go To Location";
            targetNameText.text = goTo.targetObject != null ? goTo.targetObject.name : "No Target Assigned";
        }
        else if (step is SellItemStep sell)
        {
            questTypeText.text = $"Sell {sell.requiredAmount}× {sell.itemID}";
            int sold = GameStateTracker.Instance.GetCount($"Sold_{sell.itemID}");
            targetNameText.text = $"{sold}/{sell.requiredAmount} sold";
        }
        else if (step is BuyItemStep buy)
        {
            questTypeText.text = $"Buy {buy.requiredAmount}× {buy.itemID}";
            int bought = GameStateTracker.Instance.GetCount($"Bought_{buy.itemID}");
            targetNameText.text = $"{bought}/{buy.requiredAmount} bought";
        }
        else if (step is HarvestItemStep harvest)
        {
            questTypeText.text = $"Harvest {harvest.requiredAmount}× {harvest.itemID}";
            int harvested = GameStateTracker.Instance.GetCount($"Harvested_{harvest.itemID}");
            targetNameText.text = $"{harvested}/{harvest.requiredAmount} harvested";
        }
        else
        {
            questTypeText.text = "Unknown Quest Type";
            targetNameText.text = "";
        }
    }
}
