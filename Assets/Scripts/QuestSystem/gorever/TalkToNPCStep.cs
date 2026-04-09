using UnityEngine;

/// <summary>
/// TalkToNPCStep sinifi, gorev sistemindeki ilgili adimi temsil eder.
/// </summary>
[System.Serializable]
public class TalkToNPCStep : IQuestStep
{
    public string npcID;
    public int dialogSectionIndex = 1;

    public string GetKey()
    {
        return $"{npcID.ToLower()}_{dialogSectionIndex}";
    }

    public string GetName()
    {
        string name = string.IsNullOrEmpty(npcID) ? "???" : npcID;
        return $"Talk to {name} (Section {dialogSectionIndex})";
    }

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        if (string.IsNullOrEmpty(npcID)) return false;
        return GameStateTracker.Instance.GetFlag(GetKey());
    }

    public void MarkCompleted()
    {
        if (string.IsNullOrEmpty(npcID)) return;

        if (!IsComplete())
        {
            GameStateTracker.Instance.SetFlag(GetKey(), true);

            int current = GameStateTracker.Instance.GetDialogIndex(npcID);
            int next = Mathf.Max(current, dialogSectionIndex + 1);
            GameStateTracker.Instance.SetDialogIndex(npcID, next);
        }
    }
}
