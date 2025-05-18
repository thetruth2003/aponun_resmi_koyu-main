using UnityEngine;

[System.Serializable]
public class HarvestItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    public string GetName() => $"Harvest {requiredAmount}× {itemID}";

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        int harvested = GameStateTracker.Instance.GetCount($"Harvested_{itemID}");
        return harvested >= requiredAmount;
    }
}
