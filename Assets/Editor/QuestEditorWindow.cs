using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class QuestEditorWindow : EditorWindow
{
    [SerializeField] private QuestEditorAsset questAsset;

    [SerializeField] private string newChainTitle = "";
    private int selectedChainIndex = -1;

    private Vector2 chainListScroll;
    private Vector2 subQuestScroll;

    private int newSubQuestTypeIndex;
    private readonly string[] questTypeOptions = { "Talk To NPC", "Go To Location" };

    [MenuItem("Window/Quest Editor")]
    public static void ShowWindow() => GetWindow<QuestEditorWindow>("Quest Editor");

    private void OnGUI()
    {
        GUILayout.Label("Quest Editor", EditorStyles.boldLabel);

        // ScriptableObject olarak asset seç
        questAsset = (QuestEditorAsset)EditorGUILayout.ObjectField("Quest Data", questAsset, typeof(QuestEditorAsset), false);
        if (questAsset == null)
        {
            EditorGUILayout.HelpBox("Please assign a QuestEditorAsset to begin editing.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        // — Üst: Ana Görev Ekleme —
        EditorGUILayout.BeginHorizontal();
        newChainTitle = EditorGUILayout.TextField("New Main Quest", newChainTitle);
        if (GUILayout.Button("Add Main Quest", GUILayout.MaxWidth(130)) && !string.IsNullOrWhiteSpace(newChainTitle))
        {
            questAsset.chains.Add(new QuestChainData { title = newChainTitle });
            newChainTitle = "";
            EditorUtility.SetDirty(questAsset);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // — Ana Görev Listesi —
        GUILayout.Label("Main Quests:", EditorStyles.boldLabel);
        chainListScroll = EditorGUILayout.BeginScrollView(chainListScroll, GUILayout.Height(120));
        for (int i = 0; i < questAsset.chains.Count; i++)
        {
            if (GUILayout.Toggle(selectedChainIndex == i, questAsset.chains[i].title, "Button"))
                selectedChainIndex = i;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // — Alt Görevler —
        if (selectedChainIndex >= 0 && selectedChainIndex < questAsset.chains.Count)
        {
            var selectedChain = questAsset.chains[selectedChainIndex];
            GUILayout.Label($"Editing: {selectedChain.title}", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            newSubQuestTypeIndex = EditorGUILayout.Popup(newSubQuestTypeIndex, questTypeOptions);
            if (GUILayout.Button("Add Sub Quest", GUILayout.MaxWidth(120)))
            {
                IQuestStep step = newSubQuestTypeIndex == 0
                    ? new TalkToNPCStep()
                    : new GoToLocationStep();
                var qc = new QuestContainer();
                qc.SetStepInstance(step);
                qc.questName = step.GetName();
                selectedChain.quests.Add(qc);
                EditorUtility.SetDirty(questAsset);
            }
            EditorGUILayout.EndHorizontal();

            subQuestScroll = EditorGUILayout.BeginScrollView(subQuestScroll);
            for (int j = 0; j < selectedChain.quests.Count; j++)
            {
                var qc = selectedChain.quests[j];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Sub Quest {j + 1}: {qc.questName}", EditorStyles.boldLabel);

                var step = qc.GetStepInstance();
                EditorGUI.BeginChangeCheck();

                if (step is TalkToNPCStep talk)
                {
                    talk.npcObject = (GameObject)EditorGUILayout.ObjectField("NPC Object", talk.npcObject, typeof(GameObject), true);
                }
                else if (step is GoToLocationStep goTo)
                {
                    goTo.targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", goTo.targetObject, typeof(GameObject), true);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    qc.SetStepInstance(step);
                    qc.questName = step.GetName();
                    EditorUtility.SetDirty(questAsset);
                }

                if (GUILayout.Button("Remove Sub Quest", GUILayout.MaxWidth(150)))
                {
                    selectedChain.quests.RemoveAt(j);
                    EditorUtility.SetDirty(questAsset);
                    break;
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
