using UnityEngine;

[System.Serializable]
public class SellItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    private bool isCompleted = false;

    public string GetName() => $"Sell {requiredAmount}× {itemID}";

    public void OnStart() { }

    public void OnUpdate()
    {
        //IsComplete(); // Trigger logic
    }

    public bool IsComplete()
    {
        if (isCompleted) return true;

        int sold = GameStateTracker.Instance.GetCount($"Sold_{itemID}");
        if (sold >= requiredAmount)
        {
            isCompleted = true;
        }

        return isCompleted;
    }
}
