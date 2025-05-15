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

        var firstQuest = questChain.quests[0];
        var step = firstQuest.GetStepInstance();

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
        else
        {
            questTypeText.text = "Unknown Quest Type";
            targetNameText.text = "";
        }
    }
}
