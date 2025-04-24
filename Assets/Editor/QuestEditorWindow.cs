using UnityEditor;
using UnityEngine;

public class QuestEditorWindow : EditorWindow
{
    private QuestChain currentChain;
    private Vector2 scrollPos;
    private int newQuestTypeIndex;
    private readonly string[] questTypeOptions = { "Talk To NPC", "Go To Location" };

    [MenuItem("Window/Quest Editor")]
    public static void ShowWindow() => GetWindow<QuestEditorWindow>("Quest Editor");

    void OnGUI()
    {
        GUILayout.Label("Quest Chain Editor", EditorStyles.boldLabel);
        currentChain = (QuestChain)EditorGUILayout.ObjectField("Quest Chain", currentChain, typeof(QuestChain), false);
        if (currentChain == null) return;

        // Add / Delete
        GUILayout.BeginHorizontal();
        newQuestTypeIndex = EditorGUILayout.Popup(newQuestTypeIndex, questTypeOptions);
        if (GUILayout.Button("Add")) { AddStep(newQuestTypeIndex); }
        if (GUILayout.Button("Del Last")) { DelLastStep(); }
        GUILayout.EndHorizontal();

        // Listele & düzenle
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < currentChain.quests.Count; i++)
        {
            var qc = currentChain.quests[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Step {i + 1}: {qc.questName}", EditorStyles.boldLabel);

            var step = qc.GetStepInstance();
            EditorGUI.BeginChangeCheck();
            if (step is TalkToNPCStep talk)
            {
                talk.npcObject = (GameObject)EditorGUILayout.ObjectField("NPC Object", talk.npcObject, typeof(GameObject), true);
                qc.questName = talk.GetName();
            }
            else if (step is GoToLocationStep goTo)
            {
                goTo.targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", goTo.targetObject, typeof(GameObject), true);
                qc.questName = goTo.GetName();
            }
            if (EditorGUI.EndChangeCheck())
            {
                qc.SetStepInstance(step);
                EditorUtility.SetDirty(currentChain);
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    void AddStep(int idx)
    {
        IQuestStep step = idx == 0 ? (IQuestStep)new TalkToNPCStep() : new GoToLocationStep();
        var qc = new QuestContainer();
        qc.SetStepInstance(step);
        qc.questName = step.GetName();
        currentChain.quests.Add(qc);
        EditorUtility.SetDirty(currentChain);
    }

    void DelLastStep()
    {
        if (currentChain.quests.Count > 0)
        {
            currentChain.quests.RemoveAt(currentChain.quests.Count - 1);
            EditorUtility.SetDirty(currentChain);
        }
    }
}
