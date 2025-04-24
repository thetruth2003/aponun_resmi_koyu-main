using UnityEngine;

[System.Serializable]
public class QuestContainer
{
    public string questName;
    public string questTypeName;   // Örn: TalkToNPCStep
    public string jsonData;        // Görev parametreleri

    // JSON'dan IQuestStep üret
    public IQuestStep GetStepInstance()
    {
        var type = System.Type.GetType(questTypeName);
        return (IQuestStep)JsonUtility.FromJson(jsonData, type);
    }

    // IQuestStep'i JSON'a çevir
    public void SetStepInstance(IQuestStep step)
    {
        questTypeName = step.GetType().AssemblyQualifiedName;
        jsonData = JsonUtility.ToJson(step);
    }
}
