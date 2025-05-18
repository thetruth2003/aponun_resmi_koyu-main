using UnityEngine;

[System.Serializable]
public class BuyItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    public string GetName() => $"Buy {requiredAmount}× {itemID}";

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        // Burada da GetCount kullan
        int bought = GameStateTracker.Instance.GetCount($"Bought_{itemID}");
        return bought >= requiredAmount;
    }
}
