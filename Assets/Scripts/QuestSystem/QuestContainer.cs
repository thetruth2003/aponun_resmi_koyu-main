using UnityEngine;

[System.Serializable]
public class QuestContainer
{
    public string questName;
    public string questTypeName;   // �rn: TalkToNPCStep
    public string jsonData;        // G�rev parametreleri

    // JSON'dan IQuestStep �ret
    public IQuestStep GetStepInstance()
    {
        var type = System.Type.GetType(questTypeName);
        return (IQuestStep)JsonUtility.FromJson(jsonData, type);
    }

    // IQuestStep'i JSON'a �evir
    public void SetStepInstance(IQuestStep step)
    {
        questTypeName = step.GetType().AssemblyQualifiedName;
        jsonData = JsonUtility.ToJson(step);
    }
}
