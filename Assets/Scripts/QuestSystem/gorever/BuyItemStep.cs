using UnityEngine;

[System.Serializable]
public class BuyItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    private bool isCompleted = false;

    public string GetName() => $"Buy {requiredAmount}× {itemID}";

    public void OnStart() { }

    public void OnUpdate()
    {
        //IsComplete();
    }

    public bool IsComplete()
    {
        if (isCompleted) return true;

        int bought = GameStateTracker.Instance.GetCount($"Bought_{itemID}");
        if (bought >= requiredAmount)
        {
            isCompleted = true;
        }

        return isCompleted;
    }
}
