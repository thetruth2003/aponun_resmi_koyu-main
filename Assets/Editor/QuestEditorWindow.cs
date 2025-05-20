using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class QuestEditorWindow : EditorWindow
{
    [SerializeField] private QuestEditorAsset questAsset;
    [SerializeField] private string newChainTitle = "";
    private int selectedChainIndex = -1;
    public IQuestStep ActiveStep;

    private Vector2 chainListScroll;
    private Vector2 subQuestScroll;

    private int newSubQuestTypeIndex;
    private readonly string[] questTypeOptions = {
        "Talk To NPC", "Go To Location",
        "Sell Item", "Buy Item", "Harvest Item"
    };

    [MenuItem("Window/Quest Editor")]
    public static void ShowWindow() => GetWindow<QuestEditorWindow>("Quest Editor");

    private void OnGUI()
    {
        GUILayout.Label("Quest Editor", EditorStyles.boldLabel);

        questAsset = (QuestEditorAsset)EditorGUILayout.ObjectField(
            "Quest Data", questAsset, typeof(QuestEditorAsset), false
        );

        if (questAsset == null)
        {
            EditorGUILayout.HelpBox("Please assign a QuestEditorAsset.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        // ─── Ana Görev (başlık) ekleme ───
        EditorGUILayout.BeginHorizontal();
        newChainTitle = EditorGUILayout.TextField("New Main Quest", newChainTitle);
        if (GUILayout.Button("Add Main Quest", GUILayout.MaxWidth(130)) &&
            !string.IsNullOrWhiteSpace(newChainTitle))
        {
            var mainQuest = new QuestContainer { questName = newChainTitle };
            questAsset.quests.Add(mainQuest);
            newChainTitle = "";
            EditorUtility.SetDirty(questAsset);
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("🗑 Delete Selected", GUILayout.MaxWidth(130)) &&
            selectedChainIndex >= 0)
        {
            int realIndex = GetMainQuestIndices()[selectedChainIndex];
            if (EditorUtility.DisplayDialog("Confirm Delete",
                $"Delete '{questAsset.quests[realIndex].questName}' and its sub-quests?", "Yes", "Cancel"))
            {
                DeleteMainQuestWithSubquests(realIndex);
                selectedChainIndex = -1;
                EditorUtility.SetDirty(questAsset);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // ─── Ana Görev Listesi ───
        GUILayout.Label("Main Quests:", EditorStyles.boldLabel);
        var mainQuestIndices = GetMainQuestIndices();

        chainListScroll = EditorGUILayout.BeginScrollView(chainListScroll, GUILayout.Height(120));
        for (int i = 0; i < mainQuestIndices.Count; i++)
        {
            string title = questAsset.quests[mainQuestIndices[i]].questName;
            if (GUILayout.Toggle(selectedChainIndex == i, title, "Button"))
                selectedChainIndex = i;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // ─── Alt Görevler ───
        if (selectedChainIndex < 0 || selectedChainIndex >= mainQuestIndices.Count)
            return;

        int baseIndex = mainQuestIndices[selectedChainIndex];
        GUILayout.Label($"Editing: {questAsset.quests[baseIndex].questName}", EditorStyles.boldLabel);

        // Alt görev ekleme
        EditorGUILayout.BeginHorizontal();
        newSubQuestTypeIndex = EditorGUILayout.Popup(newSubQuestTypeIndex, questTypeOptions);
        if (GUILayout.Button("Add Sub Quest", GUILayout.MaxWidth(120)))
        {
            IQuestStep step = newSubQuestTypeIndex switch
            {
                0 => new TalkToNPCStep(),
                1 => new GoToLocationStep(),
                2 => new SellItemStep(),
                3 => new BuyItemStep(),
                4 => new HarvestItemStep(),
                _ => null
            };

            if (step != null)
            {
                var qc = new QuestContainer();
                qc.SetStepInstance(step);
                qc.questName = step.GetName();

                int insertIndex = FindEndOfSubquestBlock(baseIndex);
                questAsset.quests.Insert(insertIndex, qc);
                EditorUtility.SetDirty(questAsset);
            }
        }
        EditorGUILayout.EndHorizontal();

        // Sadece Play Mode’da aktif adımı işaretle


        int activeIndex = -1;
        if (Application.isPlaying)
        {
            var subquests = GetSubquestsFor(baseIndex);
            for (int i = 0; i < subquests.Count; i++)
            {
                if (!subquests[i].GetStepInstance().IsComplete())
                {
                    activeIndex = i;
                    break;
                }
            }
        }

        // Alt görev çizimi
        subQuestScroll = EditorGUILayout.BeginScrollView(subQuestScroll);
        var subQuestList = GetSubquestsFor(baseIndex);
        for (int j = 0; j < subQuestList.Count; j++)
        {
            var qc = subQuestList[j];
            int globalIndex = baseIndex + 1 + j;

            EditorGUILayout.BeginVertical("box");

            string label = $"Sub Quest {j + 1}: {qc.questName}";
            if (Application.isPlaying)
            {
                if (j < activeIndex) label += "   ✔ Completed";
                else if (j == activeIndex) label += "   → Active";
                else label += "   (Inactive)";
            }
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            bool prevEnabled = GUI.enabled;
            if (Application.isPlaying)
                GUI.enabled = (j == activeIndex);

            var step2 = qc.GetStepInstance();
            EditorGUI.BeginChangeCheck();
            if (step2 is TalkToNPCStep talk)
            {
                talk.npcObject = (GameObject)EditorGUILayout.ObjectField("NPC Object", talk.npcObject, typeof(GameObject), true);
                talk.dialogSectionIndex = EditorGUILayout.IntField("Dialog Section Index", talk.dialogSectionIndex);
            }
            else if (step2 is GoToLocationStep goTo)
                goTo.targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", goTo.targetObject, typeof(GameObject), true);

            else if (step2 is SellItemStep sell)
            {
                sell.itemID = EditorGUILayout.TextField("Item ID", sell.itemID);
                sell.requiredAmount = EditorGUILayout.IntField("Required Amount", sell.requiredAmount);
            }
            else if (step2 is BuyItemStep buy)
            {
                buy.itemID = EditorGUILayout.TextField("Item ID", buy.itemID);
                buy.requiredAmount = EditorGUILayout.IntField("Required Amount", buy.requiredAmount);
            }
            else if (step2 is HarvestItemStep harvest)
            {
                harvest.itemID = EditorGUILayout.TextField("Item ID", harvest.itemID);
                harvest.requiredAmount = EditorGUILayout.IntField("Required Amount", harvest.requiredAmount);
            }

            if (EditorGUI.EndChangeCheck())
            {
                qc.SetStepInstance(step2);
                qc.questName = step2.GetName();
                EditorUtility.SetDirty(questAsset);
            }

            GUI.enabled = prevEnabled;

            if (GUILayout.Button("Remove Sub Quest", GUILayout.MaxWidth(150)))
            {
                questAsset.quests.RemoveAt(globalIndex);
                EditorUtility.SetDirty(questAsset);
                break;
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        if (Application.isPlaying)
            Repaint();
    }

    // Yardımcılar

    private List<int> GetMainQuestIndices()
    {
        var indices = new List<int>();
        for (int i = 0; i < questAsset.quests.Count; i++)
        {
            if (string.IsNullOrEmpty(questAsset.quests[i].questTypeName))
                indices.Add(i);
        }
        return indices;
    }

    private List<QuestContainer> GetSubquestsFor(int mainQuestIndex)
    {
        var subquests = new List<QuestContainer>();
        for (int i = mainQuestIndex + 1; i < questAsset.quests.Count; i++)
        {
            if (string.IsNullOrEmpty(questAsset.quests[i].questTypeName))
                break;
            subquests.Add(questAsset.quests[i]);
        }
        return subquests;
    }

    private int FindEndOfSubquestBlock(int mainQuestIndex)
    {
        int i = mainQuestIndex + 1;
        while (i < questAsset.quests.Count &&
               !string.IsNullOrEmpty(questAsset.quests[i].questTypeName))
        {
            i++;
        }
        return i;
    }

    private void DeleteMainQuestWithSubquests(int mainQuestIndex)
    {
        int end = FindEndOfSubquestBlock(mainQuestIndex);
        questAsset.quests.RemoveRange(mainQuestIndex, end - mainQuestIndex);
    }
}
