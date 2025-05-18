using UnityEngine;
using System.Collections.Generic;

public class ActiveQuestSystem : MonoBehaviour
{
    public static ActiveQuestSystem Instance;

    [System.Serializable]
    public class TrackedQuest
    {
        public QuestEditorAsset asset;
        public int currentIndex = 0;  // O kişinin şu an hangi adımda olduğunu tutar
    }

    public List<TrackedQuest> allQuests = new List<TrackedQuest>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        foreach (var tracked in allQuests)
        {
            while (tracked.currentIndex < tracked.asset.quests.Count)
            {
                var qc = tracked.asset.quests[tracked.currentIndex];
                var step = qc.GetStepInstance();
                if (step == null || step.IsComplete())
                {
                    tracked.currentIndex++;
                }
                else
                {
                    break;
                }
            }
        }
    }

    public int GetCurrentIndex(QuestEditorAsset asset)
    {
        var tracked = allQuests.Find(q => q.asset == asset);
        return tracked != null ? tracked.currentIndex : -1;
    }

    public TrackedQuest GetTracked(QuestEditorAsset asset)
    {
        return allQuests.Find(q => q.asset == asset);
    }
}
