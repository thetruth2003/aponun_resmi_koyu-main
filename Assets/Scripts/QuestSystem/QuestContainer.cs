using UnityEngine;

[System.Serializable]
public class QuestContainer
{
    public string questName;
    public string questTypeName;   // Örn: TalkToNPCStep
    public string jsonData;        // Görev parametreleri JSON olarak

    public IQuestStep GetStepInstance()
    {
        var type = System.Type.GetType(questTypeName);
        if (type == null)
        {
            //Debug.LogError("Geçersiz quest type: " + questTypeName);
            return null;
        }
        return (IQuestStep)JsonUtility.FromJson(jsonData, type);
    }

    public void SetStepInstance(IQuestStep step)
    {
        if (step == null)
        {
            //Debug.LogWarning("SetStepInstance'e null verildi.");
            return;
        }

        questTypeName = step.GetType().AssemblyQualifiedName;
        jsonData = JsonUtility.ToJson(step);
    }
}
