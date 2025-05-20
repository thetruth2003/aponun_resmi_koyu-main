using UnityEngine;

[System.Serializable]
public class GoToLocationStep : IQuestStep, IHasNPC
{
    public GameObject targetObject;
    public GameObject npcObject;

    public string GetName() => targetObject != null ? $"Go to {targetObject.name}" : "Go to ...";

    public void OnStart() { }

public void OnUpdate()
{
    if (!IsComplete()) return;

    // NPC varsa, dialog index'i artır
    if (npcObject != null)
    {
        GameStateTracker.Instance.IncrementDialogIndexForNPC(npcObject.name);
    }
}


    public bool IsComplete()
    {
        if (targetObject == null) return false;

        float distance = Vector3.Distance(GameObject.FindGameObjectWithTag("Player").transform.position, targetObject.transform.position);
        bool complete = distance < 3f;

        if (complete && npcObject != null)
        {
            string npcKey = $"DialogIndex_{npcObject.name.ToLower()}";
            int current = GameStateTracker.Instance.GetCount(npcKey);
            GameStateTracker.Instance.SetCount(npcKey, current + 1);
        }

        return complete;
    }

    public GameObject GetAssignedNPC() => npcObject;
    public void SetAssignedNPC(GameObject npc) => npcObject = npc;
}
