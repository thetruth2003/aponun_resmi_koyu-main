using UnityEngine;

[System.Serializable]
public class TalkToNPCStep : IQuestStep
{
    // Artık string değil, doğrudan GameObject referansı:
    public GameObject npcObject;
    private bool isCompleted = false;

    public string GetName() => npcObject != null ? $"Talk to {npcObject.name}" : "Talk to ...";
    public void OnStart() { } // Lazım değil, çünkü raycast’le tetiklenecek
    public void OnUpdate() { }

    public bool IsComplete() => isCompleted;
    public void MarkCompleted()
    {
        isCompleted = true;
        Debug.Log("TalkToNPCStep: Dialog completed.");
    }
}
