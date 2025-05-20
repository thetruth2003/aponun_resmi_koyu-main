using UnityEngine;

[System.Serializable]
public class SellItemStep : IQuestStep, IHasNPC
{
    public string itemID;
    public int requiredAmount;
    public GameObject npcObject;

    private bool isCompleted = false;

    public string GetName() => $"Sell {requiredAmount}× {itemID}";

    public void OnStart() { }

    public void OnUpdate()
    {
        IsComplete(); // Sadece tetikleyici gibi
    }

    public bool IsComplete()
    {
        if (isCompleted) return true;

        int sold = GameStateTracker.Instance.GetCount($"Sold_{itemID}");
        if (sold >= requiredAmount)
        {
            isCompleted = true;

            // Eğer NPC atanmışsa ve daha önce bu step yapılmamışsa
            if (npcObject != null)
            {
                string npcName = npcObject.name.ToLower();
                string indexKey = $"DialogIndex_{npcName}";
                string flagKey = $"StepDone_{npcName}_{itemID}";

                if (!GameStateTracker.Instance.GetFlag(flagKey))
                {
                    int current = GameStateTracker.Instance.GetCount(indexKey);
                    GameStateTracker.Instance.SetCount(indexKey, current + 1);
                    GameStateTracker.Instance.SetFlag(flagKey, true);

                    Debug.Log($"✔️ DialogIndex +1 yapıldı: {indexKey} = {current + 1}");
                }
            }

            return true;
        }

        return false;
    }

    public GameObject GetAssignedNPC() => npcObject;
    public void SetAssignedNPC(GameObject npc) => npcObject = npc;
}
