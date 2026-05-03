using UnityEngine;

/// <summary>
/// HarvestItemStep, belirli bir urunun hasat edilme miktarini bekleyen gorev adimidir.
/// </summary>
[System.Serializable]
public class HarvestItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    private bool isCompleted = false;

    public string GetName()
    {
        string name = string.IsNullOrEmpty(itemID) ? "???" : itemID;
        return $"Harvest {requiredAmount} {name}";
    }

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        if (isCompleted) return true;
        if (string.IsNullOrEmpty(itemID)) return false;
        if (GameStateTracker.Instance == null) return false;

        int harvested = GameStateTracker.Instance.GetCount(GetProgressKey());
        if (harvested >= requiredAmount)
        {
            isCompleted = true;
        }

        return isCompleted;
    }

    public string GetProgressKey()
    {
        return $"Harvested_{NormalizeId(itemID)}";
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
