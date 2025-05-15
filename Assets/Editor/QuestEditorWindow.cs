using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class QuestEditorWindow : EditorWindow
{
    [SerializeField] private List<QuestChainData> chains = new List<QuestChainData>();
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

        EditorGUILayout.Space();

        // — Üst: Ana Görev Ekleme —
        EditorGUILayout.BeginHorizontal();
        newChainTitle = EditorGUILayout.TextField("New Main Quest", newChainTitle);
        if (GUILayout.Button("Add Main Quest", GUILayout.MaxWidth(130)) 
            && !string.IsNullOrWhiteSpace(newChainTitle))
        {
            chains.Add(new QuestChainData { title = newChainTitle });
            newChainTitle = "";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // — Ana Görev Listesi —
        GUILayout.Label("Main Quests:", EditorStyles.boldLabel);
        chainListScroll = EditorGUILayout.BeginScrollView(chainListScroll, GUILayout.Height(120));
        for (int i = 0; i < chains.Count; i++)
        {
            // Toggle butonla seçim
            if (GUILayout.Toggle(selectedChainIndex == i, chains[i].title, "Button"))
                selectedChainIndex = i;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // — Alt: Seçilmiş Ana Göreve Ait Alt Görevler —
        if (selectedChainIndex >= 0 && selectedChainIndex < chains.Count)
        {
            var selectedChain = chains[selectedChainIndex];
            GUILayout.Label($"Editing: {selectedChain.title}", EditorStyles.boldLabel);

            // Alt görev tipi seçimi & ekleme
            EditorGUILayout.BeginHorizontal();
            newSubQuestTypeIndex = EditorGUILayout.Popup(newSubQuestTypeIndex, questTypeOptions);
            if (GUILayout.Button("Add Sub Quest", GUILayout.MaxWidth(120)))
            {
                IQuestStep step = newSubQuestTypeIndex == 0
                    ? (IQuestStep)new TalkToNPCStep()
                    : new GoToLocationStep();
                var qc = new QuestContainer();
                qc.SetStepInstance(step);
                qc.questName = step.GetName();
                selectedChain.quests.Add(qc);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Alt görevlerin listesi ve düzenleme
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
                    qc.questName = step.GetName();
                }

                // Alt görev silme
                if (GUILayout.Button("Remove Sub Quest", GUILayout.MaxWidth(150)))
                {
                    selectedChain.quests.RemoveAt(j);
                    break; // listeyi güncelle
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }
    }

    // Küçük yardımcı sınıf: bir ana görevin başlığı + alt görev listesi
    [System.Serializable]
    private class QuestChainData
    {
        public string title;
        public List<QuestContainer> quests = new List<QuestContainer>();
    }
}