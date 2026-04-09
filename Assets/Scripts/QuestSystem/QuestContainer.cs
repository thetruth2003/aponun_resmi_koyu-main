using UnityEngine;

/// <summary>
/// QuestContainer, ana veya alt gorev verisini tek satirlik veri kutusu olarak saklar.
/// </summary>
[System.Serializable]
public class QuestContainer
{
    public string questName;
    public string questTypeName;
    public string jsonData;

    public string optionalSideQuestID;
    [TextArea] public string optionalSideQuestDescription;
    public string optionalSideQuestNPCID;
    public int optionalTrustReward;
    public bool optionalSideQuestCompleted;

    public IQuestStep GetStepInstance()
    {
        if (string.IsNullOrEmpty(questTypeName))
        {
            return null;
        }

        System.Type type = System.Type.GetType(questTypeName);
        if (type == null)
        {
            Debug.LogError("Gecersiz quest type: " + questTypeName);
            return null;
        }

        return (IQuestStep)JsonUtility.FromJson(jsonData, type);
    }

    public void SetStepInstance(IQuestStep step)
    {
        if (step == null)
        {
            return;
        }

        questTypeName = step.GetType().AssemblyQualifiedName;
        jsonData = JsonUtility.ToJson(step);
    }
}
