using UnityEngine;

[System.Serializable]
public class TalkToNPCStep : IQuestStep
{
    public GameObject npcObject;
    public int dialogSectionIndex = 0;

    public string GetName()
    {
        string name = npcObject != null ? npcObject.name : "...";
        return $"Talk to {name} (Section {dialogSectionIndex})";
    }

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        if (npcObject == null) return false;

        string key = $"Talked_{npcObject.name.ToLower()}_{dialogSectionIndex}";
        return GameStateTracker.Instance.GetFlag(key);
    }

    public void MarkCompleted()
    {
        if (npcObject == null) return;

        string key = $"Talked_{npcObject.name.ToLower()}_{dialogSectionIndex}";
        GameStateTracker.Instance.SetFlag(key, true);
        Debug.Log($"[TalkToNPCStep] İşaretlendi: {key}");
    }
}


