using UnityEngine;

[System.Serializable]
public class TalkToNPCStep : IQuestStep
{
    public string npcID;  // 🔄 Artık GameObject değil, string ID kullanıyoruz
    public int dialogSectionIndex = 0;

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

            // Diyalog indexini güvenli şekilde ilerlet
            int current = GameStateTracker.Instance.GetDialogIndex(npcID);
            int next = Mathf.Max(current, dialogSectionIndex + 1); // geri gitme
            GameStateTracker.Instance.SetDialogIndex(npcID, next);
        }
    }
}
