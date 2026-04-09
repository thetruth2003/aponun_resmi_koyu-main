using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ActiveQuestSystem sinifi, gorev sistemi icindeki ilgili davranis veya veriyi yonetir.
/// </summary>
public class ActiveQuestSystem : MonoBehaviour
{
    public static ActiveQuestSystem Instance;

    public event System.Action<QuestEditorAsset, int> OnActiveStepChanged;

    /// <summary>
    /// TrackedQuest sinifi, gorev sistemi icindeki ilgili davranis veya veriyi yonetir.
    /// </summary>
    [System.Serializable]
    public class TrackedQuest
    {
        public QuestEditorAsset asset;
        public int currentIndex = 0;

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
            while (tracked.asset != null &&
                   tracked.asset.quests != null &&
                   tracked.currentIndex < tracked.asset.quests.Count)
            {
                var container = tracked.asset.quests[tracked.currentIndex];
                var step = container.GetStepInstance();

                if (step == null || step.IsComplete())
                {
                    tracked.currentIndex++;
                    OnActiveStepChanged?.Invoke(tracked.asset, tracked.currentIndex);
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

    public List<TrackedQuest> GetAllTracked()
    {

        return allQuests;
    }
}
