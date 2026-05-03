using UnityEngine;

/// <summary>
/// BuyItemStep, belirli bir esyanin satin alinma miktarini bekleyen gorev adimidir.
/// </summary>
[System.Serializable]
public class BuyItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    private bool isCompleted = false;

    public string GetName() => $"Buy {requiredAmount} {itemID}";

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        if (isCompleted) return true;
        if (GameStateTracker.Instance == null) return false;

        int bought = GameStateTracker.Instance.GetCount(GetProgressKey());
        if (bought >= requiredAmount)
        {
            isCompleted = true;
        }

        return isCompleted;
    }

    public string GetProgressKey()
    {
        return $"Bought_{NormalizeId(itemID)}";
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
