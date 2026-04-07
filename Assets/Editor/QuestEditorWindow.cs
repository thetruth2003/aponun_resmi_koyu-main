using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class QuestEditorWindow : EditorWindow
{
    [SerializeField] private QuestEditorAsset questAsset;
    [SerializeField] private string newMainQuestTitle = "";

    private int selectedMainQuestListIndex = -1;
    private int newSubQuestTypeIndex;
    private Vector2 mainQuestScroll;
    private Vector2 subQuestScroll;

    private readonly string[] questTypeOptions =
    {
        "Talk To NPC",
        "Go To Location",
        "Sell Item",
        "Buy Item",
        "Harvest Item"
    };

    [MenuItem("Window/Quest Editor")]
    public static void ShowWindow()
    {
        GetWindow<QuestEditorWindow>("Quest Editor");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Quest Editor", EditorStyles.boldLabel);

        questAsset = (QuestEditorAsset)EditorGUILayout.ObjectField(
            "Quest Data",
            questAsset,
            typeof(QuestEditorAsset),
            false);

        if (questAsset == null)
        {
            EditorGUILayout.HelpBox("Please assign a QuestEditorAsset.", MessageType.Info);
            return;
        }

        EnsureSelectionIsValid();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        DrawMainQuestPanel();
        DrawSelectedQuestPanel();
        EditorGUILayout.EndHorizontal();

        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void DrawMainQuestPanel()
    {
        List<int> mainQuestIndices = GetMainQuestIndices();

        EditorGUILayout.BeginVertical(GUILayout.Width(320));
        EditorGUILayout.LabelField("Main Quests", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        newMainQuestTitle = EditorGUILayout.TextField("New Main Quest", newMainQuestTitle);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newMainQuestTitle)))
        {
            if (GUILayout.Button("Add Main Quest", GUILayout.Width(120)))
            {
                AddMainQuest(newMainQuestTitle.Trim());
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6f);

        mainQuestScroll = EditorGUILayout.BeginScrollView(mainQuestScroll, GUILayout.Height(180));
        if (mainQuestIndices.Count == 0)
        {
            EditorGUILayout.HelpBox("No main quests yet.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < mainQuestIndices.Count; i++)
            {
                int realIndex = mainQuestIndices[i];
                string title = GetMainQuestTitle(realIndex);

                if (GUILayout.Toggle(selectedMainQuestListIndex == i, title, "Button"))
                {
                    selectedMainQuestListIndex = i;
                }
            }
        }
        EditorGUILayout.EndScrollView();

        using (new EditorGUI.DisabledScope(selectedMainQuestListIndex < 0 || selectedMainQuestListIndex >= mainQuestIndices.Count))
        {
            if (GUILayout.Button("Delete Selected Main Quest"))
            {
                int realIndex = mainQuestIndices[selectedMainQuestListIndex];
                string title = GetMainQuestTitle(realIndex);

                bool confirm = EditorUtility.DisplayDialog(
                    "Confirm Delete",
                    $"Delete '{title}' and all of its sub quests?",
                    "Delete",
                    "Cancel");

                if (confirm)
                {
                    DeleteMainQuestWithSubquests(realIndex);
                    selectedMainQuestListIndex = -1;
                    MarkAssetDirty();
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedQuestPanel()
    {
        List<int> mainQuestIndices = GetMainQuestIndices();

        EditorGUILayout.BeginVertical("box");

        if (selectedMainQuestListIndex < 0 || selectedMainQuestListIndex >= mainQuestIndices.Count)
        {
            EditorGUILayout.HelpBox("Select a main quest to edit.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        int mainQuestIndex = mainQuestIndices[selectedMainQuestListIndex];
        QuestContainer mainQuest = questAsset.quests[mainQuestIndex];

        EditorGUILayout.LabelField($"Editing: {GetMainQuestTitle(mainQuestIndex)}", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        EditorGUI.BeginChangeCheck();
        string newTitle = EditorGUILayout.TextField("Main Quest Title", mainQuest.questName);
        string sideQuestId = EditorGUILayout.TextField("Optional Side Quest ID", mainQuest.optionalSideQuestID);
        string sideQuestNpcId = EditorGUILayout.TextField("Related NPC ID", mainQuest.optionalSideQuestNPCID);
        int trustReward = EditorGUILayout.IntField("Trust Reward", mainQuest.optionalTrustReward);

        EditorGUILayout.LabelField("Optional Side Quest Description");
        string sideQuestDescription = EditorGUILayout.TextArea(mainQuest.optionalSideQuestDescription ?? string.Empty, GUILayout.MinHeight(50));

        if (EditorGUI.EndChangeCheck())
        {
            mainQuest.questName = newTitle;
            mainQuest.optionalSideQuestID = sideQuestId;
            mainQuest.optionalSideQuestNPCID = sideQuestNpcId;
            mainQuest.optionalTrustReward = trustReward;
            mainQuest.optionalSideQuestDescription = sideQuestDescription;
            MarkAssetDirty();
        }

        if (Application.isPlaying)
        {
            string status = mainQuest.optionalSideQuestCompleted ? "Completed" : "Not Completed";
            EditorGUILayout.LabelField("Side Quest Status", status);
        }

        EditorGUILayout.Space(10f);
        DrawAddSubQuestRow(mainQuestIndex);
        DrawSubQuestList(mainQuestIndex);

        EditorGUILayout.EndVertical();
    }

    private void DrawAddSubQuestRow(int mainQuestIndex)
    {
        EditorGUILayout.LabelField("Sub Quests", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        newSubQuestTypeIndex = EditorGUILayout.Popup(newSubQuestTypeIndex, questTypeOptions);

        if (GUILayout.Button("Add Sub Quest", GUILayout.Width(120)))
        {
            IQuestStep step = CreateStepFromSelection();
            if (step != null)
            {
                QuestContainer newSubQuest = new QuestContainer();
                newSubQuest.SetStepInstance(step);
                newSubQuest.questName = SafeStepName(step);

                int insertIndex = FindEndOfSubquestBlock(mainQuestIndex);
                questAsset.quests.Insert(insertIndex, newSubQuest);
                MarkAssetDirty();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6f);
    }

    private void DrawSubQuestList(int mainQuestIndex)
    {
        List<int> subQuestIndices = GetSubQuestIndicesFor(mainQuestIndex);
        int activeIndex = GetActiveSubQuestRelativeIndex(subQuestIndices);

        subQuestScroll = EditorGUILayout.BeginScrollView(subQuestScroll);

        if (subQuestIndices.Count == 0)
        {
            EditorGUILayout.HelpBox("This main quest does not have any sub quests yet.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        for (int i = 0; i < subQuestIndices.Count; i++)
        {
            int globalIndex = subQuestIndices[i];
            QuestContainer container = questAsset.quests[globalIndex];
            IQuestStep step = container.GetStepInstance();

            EditorGUILayout.BeginVertical("box");

            string header = $"Step {i + 1}: {container.questName}";
            if (Application.isPlaying)
            {
                if (activeIndex == i)
                {
                    header += "   -> Active";
                }
                else if (activeIndex >= 0 && i < activeIndex)
                {
                    header += "   Completed";
                }
                else
                {
                    header += "   Inactive";
                }
            }

            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

            if (step == null)
            {
                EditorGUILayout.HelpBox(
                    $"This sub quest could not be loaded. Stored type: {container.questTypeName}",
                    MessageType.Error);
            }
            else
            {
                DrawStepFields(container, step);
            }

            if (GUILayout.Button("Remove Sub Quest", GUILayout.Width(150)))
            {
                questAsset.quests.RemoveAt(globalIndex);
                MarkAssetDirty();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawStepFields(QuestContainer container, IQuestStep step)
    {
        EditorGUI.BeginChangeCheck();

        if (step is TalkToNPCStep talk)
        {
            talk.npcID = EditorGUILayout.TextField("NPC ID", talk.npcID);
            talk.dialogSectionIndex = EditorGUILayout.IntField("Dialog Section Index", talk.dialogSectionIndex);
        }
        else if (step is GoToLocationStep goTo)
        {
            goTo.locationID = EditorGUILayout.TextField("Location ID", goTo.locationID);
        }
        else if (step is SellItemStep sell)
        {
            sell.itemID = EditorGUILayout.TextField("Item ID", sell.itemID);
            sell.requiredAmount = EditorGUILayout.IntField("Required Amount", sell.requiredAmount);
        }
        else if (step is BuyItemStep buy)
        {
            buy.itemID = EditorGUILayout.TextField("Item ID", buy.itemID);
            buy.requiredAmount = EditorGUILayout.IntField("Required Amount", buy.requiredAmount);
        }
        else if (step is HarvestItemStep harvest)
        {
            harvest.itemID = EditorGUILayout.TextField("Item ID", harvest.itemID);
            harvest.requiredAmount = EditorGUILayout.IntField("Required Amount", harvest.requiredAmount);
        }
        else
        {
            EditorGUILayout.HelpBox($"Unsupported step type: {step.GetType().Name}", MessageType.Warning);
        }

        if (EditorGUI.EndChangeCheck())
        {
            container.SetStepInstance(step);
            container.questName = SafeStepName(step);
            MarkAssetDirty();
        }
    }

    private IQuestStep CreateStepFromSelection()
    {
        return newSubQuestTypeIndex switch
        {
            0 => new TalkToNPCStep(),
            1 => new GoToLocationStep(),
            2 => new SellItemStep(),
            3 => new BuyItemStep(),
            4 => new HarvestItemStep(),
            _ => null
        };
    }

    private int GetActiveSubQuestRelativeIndex(List<int> subQuestIndices)
    {
        if (!Application.isPlaying)
        {
            return -1;
        }

        for (int i = 0; i < subQuestIndices.Count; i++)
        {
            IQuestStep step = questAsset.quests[subQuestIndices[i]].GetStepInstance();
            if (step == null || !step.IsComplete())
            {
                return i;
            }
        }

        return -1;
    }

    private void AddMainQuest(string title)
    {
        questAsset.quests.Add(new QuestContainer
        {
            questName = title,
            questTypeName = string.Empty,
            jsonData = string.Empty
        });

        selectedMainQuestListIndex = GetMainQuestIndices().Count - 1;
        newMainQuestTitle = string.Empty;
        MarkAssetDirty();
    }

    private void EnsureSelectionIsValid()
    {
        int mainQuestCount = GetMainQuestIndices().Count;
        if (mainQuestCount == 0)
        {
            selectedMainQuestListIndex = -1;
            return;
        }

        selectedMainQuestListIndex = Mathf.Clamp(selectedMainQuestListIndex, 0, mainQuestCount - 1);
    }

    private string GetMainQuestTitle(int questIndex)
    {
        string title = questAsset.quests[questIndex].questName;
        return string.IsNullOrWhiteSpace(title) ? "(Untitled Main Quest)" : title;
    }

    private List<int> GetMainQuestIndices()
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < questAsset.quests.Count; i++)
        {
            if (string.IsNullOrEmpty(questAsset.quests[i].questTypeName))
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    private List<int> GetSubQuestIndicesFor(int mainQuestIndex)
    {
        List<int> indices = new List<int>();
        for (int i = mainQuestIndex + 1; i < questAsset.quests.Count; i++)
        {
            if (string.IsNullOrEmpty(questAsset.quests[i].questTypeName))
            {
                break;
            }

            indices.Add(i);
        }

        return indices;
    }

    private int FindEndOfSubquestBlock(int mainQuestIndex)
    {
        int index = mainQuestIndex + 1;
        while (index < questAsset.quests.Count && !string.IsNullOrEmpty(questAsset.quests[index].questTypeName))
        {
            index++;
        }

        return index;
    }

    private void DeleteMainQuestWithSubquests(int mainQuestIndex)
    {
        int endIndex = FindEndOfSubquestBlock(mainQuestIndex);
        questAsset.quests.RemoveRange(mainQuestIndex, endIndex - mainQuestIndex);
    }

    private string SafeStepName(IQuestStep step)
    {
        try
        {
            return step.GetName();
        }
        catch (Exception)
        {
            return step.GetType().Name;
        }
    }

    private void MarkAssetDirty()
    {
        EditorUtility.SetDirty(questAsset);
    }
}
