using UnityEngine;

[System.Serializable]
public class SellItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    private bool isCompleted = false;

    public string GetName()
    {
        string name = string.IsNullOrEmpty(itemID) ? "???" : itemID;
        return $"Sell {requiredAmount}× {name}";
    }

    public void OnStart() { }

    public void OnUpdate()
    {
        // İsteğe bağlı tetikleyici
    }

    public bool IsComplete()
    {
        if (isCompleted) return true;
        if (string.IsNullOrEmpty(itemID)) return false;

        int sold = GameStateTracker.Instance.GetCount($"Sold_{itemID}");
        if (sold >= requiredAmount)
        {
            isCompleted = true;
        }

        return isCompleted;
    }
}
