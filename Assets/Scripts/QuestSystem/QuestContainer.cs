using UnityEngine;

[System.Serializable]
public class QuestContainer
{
    public string questName;
    public string questTypeName;   // Alt görev ise tipi dolu olur
    public string jsonData;        // Alt görev için JSON veri

    // ─── Alt Görev Get/Set ───
    public IQuestStep GetStepInstance()
    {
        if (string.IsNullOrEmpty(questTypeName)) return null;

        var type = System.Type.GetType(questTypeName);
        if (type == null)
        {
            Debug.LogError("Geçersiz quest type: " + questTypeName);
            return null;
        }
        return (IQuestStep)JsonUtility.FromJson(jsonData, type);
    }

    public void SetStepInstance(IQuestStep step)
    {
        if (step == null) return;

        questTypeName = step.GetType().AssemblyQualifiedName;
        jsonData = JsonUtility.ToJson(step);
    }

    // ─── YAN GÖREV ALANLARI ───
    public string optionalSideQuestID;                     // "alperen_kasa_tasi"
    [TextArea] public string optionalSideQuestDescription; // Açıklama metni
    public GameObject optionalSideQuestNPC;                // NPC objesi
    public int optionalTrustReward;                        // Güven puanı
    public bool optionalSideQuestCompleted;                // Tamamlandı mı
}
