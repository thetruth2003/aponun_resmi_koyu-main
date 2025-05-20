using UnityEngine;

[System.Serializable]
public class TalkToNPCStep : IQuestStep
{
    public GameObject npcObject;
    public int dialogSectionIndex = 0;

    public string GetKey()
    {
        return $"Talked_{npcObject.name.ToLower()}_{dialogSectionIndex}";
    }

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

        string npc = npcObject.name.ToLower();
        int section = dialogSectionIndex;

        return GameStateTracker.Instance.GetFlag($"{npc}_{section}");
    }


    public void MarkCompleted()
    {
        if (npcObject == null) return;

        if (!IsComplete())
        {
            GameStateTracker.Instance.SetFlag(GetKey(), true);

            // Diyalog indexini güvenli şekilde ilerlet
            string npcKey = npcObject.name.ToLower();
            int current = GameStateTracker.Instance.GetDialogIndex(npcKey);
            int next = Mathf.Max(current, dialogSectionIndex + 1); // geri gitme
            GameStateTracker.Instance.SetDialogIndex(npcKey, next);
        }
    }
}
