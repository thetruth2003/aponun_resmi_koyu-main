using UnityEngine;

/// <summary>
/// GoToLocationStep sinifi, gorev sistemindeki ilgili adimi temsil eder.
/// </summary>
[System.Serializable]
public class GoToLocationStep : IQuestStep
{
    public string locationID;

    public string GetKey() => $"player_hit_{locationID.ToLower()}";

    public string GetName() => $"Go to {locationID}";

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        if (string.IsNullOrEmpty(locationID)) return false;
        return GameStateTracker.Instance.GetFlag(GetKey());
    }

    public void MarkCompleted()
    {
        if (!IsComplete())
            GameStateTracker.Instance.SetFlag(GetKey(), true);
    }
}
