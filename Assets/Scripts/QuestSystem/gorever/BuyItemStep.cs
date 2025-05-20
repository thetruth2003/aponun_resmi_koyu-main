using UnityEngine;

[System.Serializable]
public class BuyItemStep : IQuestStep, IHasNPC
{
    public string itemID;
    public int requiredAmount;
    public GameObject npcObject;

    public string GetName() => $"Buy {requiredAmount}× {itemID}";

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
        int bought = GameStateTracker.Instance.GetCount($"Bought_{itemID}");
        bool complete = bought >= requiredAmount;

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
