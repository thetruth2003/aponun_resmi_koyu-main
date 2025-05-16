using UnityEngine;

[System.Serializable]
public class SellItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    public string GetName() => $"Sell {requiredAmount}× {itemID}";

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        int sold = GameStateTracker.Instance.GetCount($"Sold_{itemID}");
        Debug.Log($"[SellItemStep] Checking Sold_{itemID}: {sold}/{requiredAmount}");
        return sold >= requiredAmount;
    }

}
