using UnityEngine;

[System.Serializable]
public class TalkToNPCStep : IQuestStep
{
    // Artık string değil, doğrudan GameObject referansı:
    public GameObject npcObject;

    public string GetName() => npcObject != null ? $"Talk to {npcObject.name}" : "Talk to ...";
    public void OnStart() { }
    public void OnUpdate() { }
    public bool IsComplete()
    {
        // Örnek kontrol: NPCManager ile ilişkilendir
        return npcObject != null && NPCManager.Instance.HasTalkedTo(npcObject);
    }
}
