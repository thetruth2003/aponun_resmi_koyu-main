using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideQuestHandler : MonoBehaviour
{
    public QuestEditorAsset questAsset;

    void Update()
    {
        foreach (var quest in questAsset.quests)
        {
            // Ana görev değilse atla
            if (!string.IsNullOrEmpty(quest.questTypeName)) continue;

            // Yan görev tanımlıysa kontrol et
            if (!string.IsNullOrEmpty(quest.optionalSideQuestID) && !quest.optionalSideQuestCompleted)
            {
                if (GameStateTracker.Instance.GetFlag(quest.optionalSideQuestID))
                {
                    quest.optionalSideQuestCompleted = true;
                    Debug.Log($"Yan görev tamamlandı: {quest.optionalSideQuestDescription}");

                    // İsteğe bağlı: güven puanı artır
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

