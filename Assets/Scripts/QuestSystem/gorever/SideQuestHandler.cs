using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SideQuestHandler sinifi, gorev sistemi icindeki ilgili davranis veya veriyi yonetir.
/// </summary>
public class SideQuestHandler : MonoBehaviour
{
    public QuestEditorAsset questAsset;

    void Update()
    {
        foreach (var quest in questAsset.quests)
        {
            if (!string.IsNullOrEmpty(quest.questTypeName)) continue;

            if (!string.IsNullOrEmpty(quest.optionalSideQuestID) && !quest.optionalSideQuestCompleted)
            {
                if (GameStateTracker.Instance.GetFlag(quest.optionalSideQuestID))
                {
                    quest.optionalSideQuestCompleted = true;
                    Debug.Log($"Yan gÃ¶rev tamamland???±: {quest.optionalSideQuestDescription}");

             if (quest.optionalTrustReward > 0 && !string.IsNullOrEmpty(quest.optionalSideQuestNPCID))
                {
                    string npcID = quest.optionalSideQuestNPCID.ToLower();
                    GameStateTracker.Instance.IncrementCount($"Trust_{npcID}", quest.optionalTrustReward);
                }
                }
            }
        }
    }
}

