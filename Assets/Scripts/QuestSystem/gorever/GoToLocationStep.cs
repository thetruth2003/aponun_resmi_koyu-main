using UnityEngine;

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
        // opsiyonel: elle tetiklemek istersen
        if (!IsComplete())
            GameStateTracker.Instance.SetFlag(GetKey(), true);
    }
}
