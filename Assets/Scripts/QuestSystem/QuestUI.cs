using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    public QuestEditorAsset questAsset;
    public TMP_Text mainQuestText;
    public TMP_Text questTypeText;
    public TMP_Text requirementText;

    private int currentQuestIndex = 0;

    void Start()
    {
        UpdateQuestUI();
    }

    public void UpdateQuestUI()
    {
        if (questAsset == null || questAsset.quests.Count == 0)
        {
            mainQuestText.text = "No Quest Data";
            questTypeText.text = "";
            requirementText.text = "";
            return;
        }

        // Aktif görev adımını bul (ilk step ≠ null olan)
        int stepIndex = -1;
        for (int i = 0; i < questAsset.quests.Count; i++)
        {
            if (questAsset.quests[i].GetStepInstance() != null)
            {
                stepIndex = i;
                break;
            }
        }

        if (stepIndex == -1)
        {
            mainQuestText.text = "All quests complete!";
            questTypeText.text = "";
            requirementText.text = "";
            return;
        }

        // Ana başlık: kendisinden önceki en yakın step == null olan questName
        string mainTitle = "Untitled";
        for (int i = stepIndex - 1; i >= 0; i--)
        {
            if (questAsset.quests[i].GetStepInstance() == null)
            {
                mainTitle = questAsset.quests[i].questName;
                break;
            }
        }

        var currentContainer = questAsset.quests[stepIndex];
        var step = currentContainer.GetStepInstance();

        mainQuestText.text = mainTitle;

        if (step is TalkToNPCStep talk)
        {
            questTypeText.text = "Talk To NPC";
            requirementText.text = talk.npcObject != null
                ? $"Talk to {talk.npcObject.name}"
                : "No NPC assigned";
        }
        else if (step is GoToLocationStep go)
        {
            questTypeText.text = "Go To Location";
            requirementText.text = go.targetObject != null
                ? $"Go to {go.targetObject.name}"
                : "No location assigned";
        }
        else if (step is SellItemStep sell)
        {
            questTypeText.text = "Sell Item";
            int sold = GameStateTracker.Instance.GetCount($"Sold_{sell.itemID}");
            requirementText.text = $"{sold}/{sell.requiredAmount} × {sell.itemID}";
        }
        else if (step is BuyItemStep buy)
        {
            questTypeText.text = "Buy Item";
            int bought = GameStateTracker.Instance.GetCount($"Bought_{buy.itemID}");
            requirementText.text = $"{bought}/{buy.requiredAmount} × {buy.itemID}";
        }
        else if (step is HarvestItemStep harvest)
        {
            questTypeText.text = "Harvest Item";
            int harvested = GameStateTracker.Instance.GetCount($"Harvested_{harvest.itemID}");
            requirementText.text = $"{harvested}/{harvest.requiredAmount} × {harvest.itemID}";
        }
        else
        {
            questTypeText.text = "Unknown Type";
            requirementText.text = "";
        }
    }
}
