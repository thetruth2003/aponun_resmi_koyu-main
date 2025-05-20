using UnityEngine;

[System.Serializable]
public class HarvestItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    private bool isCompleted = false;

    public string GetName() => $"Harvest {requiredAmount}× {itemID}";

    public void OnStart() { }

    public void OnUpdate()
    {
        //IsComplete();
    }

    public bool IsComplete()
    {
        if (isCompleted) return true;

        int harvested = GameStateTracker.Instance.GetCount($"harvest_{itemID}");
        if (harvested >= requiredAmount)
        {
            isCompleted = true;
        }

        return isCompleted;
    }
}
