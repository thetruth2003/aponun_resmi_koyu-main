using UnityEngine;

/// <summary>
/// SellItemStep, belirli bir esyanin satilma miktarini bekleyen gorev adimidir.
/// </summary>
[System.Serializable]
public class SellItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    private bool isCompleted = false;

    public string GetName()
    {
        string name = string.IsNullOrEmpty(itemID) ? "???" : itemID;
        return $"Sell {requiredAmount} {name}";
    }

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        if (isCompleted) return true;
        if (string.IsNullOrEmpty(itemID)) return false;
        if (GameStateTracker.Instance == null) return false;

        int sold = GameStateTracker.Instance.GetCount(GetProgressKey());
        if (sold >= requiredAmount)
        {
            isCompleted = true;
        }

        return isCompleted;
    }

    public string GetProgressKey()
    {
        return $"Sold_{NormalizeId(itemID)}";
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
