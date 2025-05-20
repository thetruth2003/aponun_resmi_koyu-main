using UnityEngine;

[System.Serializable]
public class HarvestItemStep : IQuestStep, IHasNPC
{
    public string itemID;
    public int requiredAmount;
    public GameObject npcObject;

    public string GetName() => $"Harvest {requiredAmount}× {itemID}";

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
        int harvested = GameStateTracker.Instance.GetCount($"Harvested_{itemID}");
        bool complete = harvested >= requiredAmount;

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
