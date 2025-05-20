using UnityEngine;
using System.Collections.Generic;

public class ActiveQuestSystem : MonoBehaviour
{
    public static ActiveQuestSystem Instance;

    [System.Serializable]
    public class TrackedQuest
    {
        public QuestEditorAsset asset;
        public int currentIndex = 0;

        // ✅ Şu anki aktif adımı verir (null değilse)
        public QuestContainer GetActiveStep()
        {
            if (asset == null || asset.quests == null) return null;
            if (currentIndex < 0 || currentIndex >= asset.quests.Count) return null;
            return asset.quests[currentIndex];
        }
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
            // Her frame'de aktif görev adımını kontrol et
            while (tracked.currentIndex < tracked.asset.quests.Count)
            {
                var container = tracked.asset.quests[tracked.currentIndex];
                var step = container.GetStepInstance();
                if (step == null || step.IsComplete())
                {
                    tracked.currentIndex++; // geç tamamlandıysa
                }
                else
                {
                    break; // aktif görev devam ediyor
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

    // 🔁 Tüm takip edilen görevleri verir
    public List<TrackedQuest> GetAllTracked()
    {
        return allQuests;
    }
}
