using UnityEngine;

[System.Serializable]
public class HarvestItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    private bool isCompleted = false;

    public string GetName()
    {
        string name = string.IsNullOrEmpty(itemID) ? "???" : itemID;
        return $"Harvest {requiredAmount}× {name}";
    }

    public void OnStart() { }

    public void OnUpdate()
    {
        // Opsiyonel tetikleyici
    }

    public bool IsComplete()
    {
        if (isCompleted) return true;
        if (string.IsNullOrEmpty(itemID)) return false;

        int harvested = GameStateTracker.Instance.GetCount($"Harvested_{itemID}");
        if (harvested >= requiredAmount)
        {
            isCompleted = true;
        }

        return isCompleted;
    }
}
