using UnityEngine;

/// <summary>
/// Buy sinifi, gorev sistemi icindeki ilgili davranis veya veriyi yonetir.
/// </summary>
[System.Serializable]
public class BuyItemStep : IQuestStep
{
    public string itemID;
    public int requiredAmount;

    private bool isCompleted = false;

    public string GetName() => $"Buy {requiredAmount}Ãƒâ€” {itemID}";

    public void OnStart() { }

    public void OnUpdate()
    {
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
