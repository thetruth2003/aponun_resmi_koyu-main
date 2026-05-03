using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestEditorAsset))]
public class QuestEditorAssetInspector : Editor
{
    private readonly Dictionary<int, bool> mainQuestFoldouts = new Dictionary<int, bool>();

    public override void OnInspectorGUI()
    {
        QuestEditorAsset questAsset = (QuestEditorAsset)target;

        EditorGUILayout.LabelField("Quest Groups", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        if (questAsset.quests == null || questAsset.quests.Count == 0)
        {
            EditorGUILayout.HelpBox("No quest data found.", MessageType.Info);
            return;
        }

        List<int> mainQuestIndices = GetMainQuestIndices(questAsset);

        if (mainQuestIndices.Count == 0)
        {
            EditorGUILayout.HelpBox("No main quest headers found. A main quest is a QuestContainer with an empty quest type.", MessageType.Warning);
            return;
        }

        for (int i = 0; i < mainQuestIndices.Count; i++)
        {
            int mainQuestIndex = mainQuestIndices[i];
            QuestContainer mainQuest = questAsset.quests[mainQuestIndex];

            if (!mainQuestFoldouts.ContainsKey(mainQuestIndex))
            {
                mainQuestFoldouts[mainQuestIndex] = true;
            }

            string title = string.IsNullOrWhiteSpace(mainQuest.questName)
                ? $"Main Quest {i + 1}"
                : mainQuest.questName;

            mainQuestFoldouts[mainQuestIndex] = EditorGUILayout.Foldout(
                mainQuestFoldouts[mainQuestIndex],
                title,
                true);

            if (!mainQuestFoldouts[mainQuestIndex])
            {
                continue;
            }

            EditorGUI.indentLevel++;
            DrawMainQuestFields(mainQuest);
            DrawSubQuestFields(questAsset, mainQuestIndex);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4f);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(questAsset);
        }
    }

    private void DrawMainQuestFields(QuestContainer mainQuest)
    {
        EditorGUILayout.BeginVertical("box");
        mainQuest.questName = EditorGUILayout.TextField("Main Quest Title", mainQuest.questName);
        mainQuest.optionalSideQuestID = EditorGUILayout.TextField("Optional Side Quest ID", mainQuest.optionalSideQuestID);
        mainQuest.optionalSideQuestNPCID = EditorGUILayout.TextField("Related NPC ID", mainQuest.optionalSideQuestNPCID);
        mainQuest.optionalTrustReward = EditorGUILayout.IntField("Trust Reward", mainQuest.optionalTrustReward);
        EditorGUILayout.LabelField("Optional Side Quest Description");
        mainQuest.optionalSideQuestDescription = EditorGUILayout.TextArea(mainQuest.optionalSideQuestDescription ?? string.Empty, GUILayout.MinHeight(45));
        mainQuest.optionalSideQuestCompleted = EditorGUILayout.Toggle("Side Quest Completed", mainQuest.optionalSideQuestCompleted);
        EditorGUILayout.EndVertical();
    }

    private void DrawSubQuestFields(QuestEditorAsset questAsset, int mainQuestIndex)
    {
        List<int> subQuestIndices = GetSubQuestIndices(questAsset, mainQuestIndex);
        if (subQuestIndices.Count == 0)
        {
            EditorGUILayout.HelpBox("No sub quests under this main quest.", MessageType.None);
            return;
        }

        for (int i = 0; i < subQuestIndices.Count; i++)
        {
            QuestContainer subQuest = questAsset.quests[subQuestIndices[i]];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Step {i + 1}", EditorStyles.boldLabel);
            EditorGUILayout.TextField("Step Name", subQuest.questName);
            EditorGUILayout.TextField("Quest Type", GetShortTypeName(subQuest.questTypeName));

            IQuestStep step = subQuest.GetStepInstance();
            if (step is TalkToNPCStep talk)
            {
                EditorGUILayout.TextField("NPC ID", talk.npcID);
                EditorGUILayout.IntField("Dialog Section Index", talk.dialogSectionIndex);
            }
            else if (step is GoToLocationStep goTo)
            {
                EditorGUILayout.TextField("Location ID", goTo.locationID);
            }
            else if (step is SellItemStep sell)
            {
                EditorGUILayout.TextField("Item ID", sell.itemID);
                EditorGUILayout.IntField("Required Amount", sell.requiredAmount);
            }
            else if (step is BuyItemStep buy)
            {
                EditorGUILayout.TextField("Item ID", buy.itemID);
                EditorGUILayout.IntField("Required Amount", buy.requiredAmount);
            }
            else if (step is HarvestItemStep harvest)
            {
                EditorGUILayout.TextField("Item ID", harvest.itemID);
                EditorGUILayout.IntField("Required Amount", harvest.requiredAmount);
            }
            else if (step is ChoiceStep choice)
            {
                EditorGUILayout.TextField("Choice Key", choice.choiceKey);
                EditorGUILayout.TextField("Expected Value", choice.expectedValue);
                EditorGUILayout.Toggle("Complete On Any Value", choice.completeOnAnyValue);
            }
            else if (step is ModifyStatStep modify)
            {
                EditorGUILayout.TextField("Mode", modify.mode.ToString());
                EditorGUILayout.TextField("State Key", modify.statKey);

                switch (modify.mode)
                {
                    case ModifyStatStep.ModifyMode.AddCount:
                    case ModifyStatStep.ModifyMode.SetCount:
                        EditorGUILayout.IntField("Int Value", modify.intValue);
                        break;

                    case ModifyStatStep.ModifyMode.SetFlag:
                        EditorGUILayout.Toggle("Bool Value", modify.boolValue);
                        break;

                    case ModifyStatStep.ModifyMode.SetString:
                        EditorGUILayout.TextField("String Value", modify.stringValue);
                        break;
                }
            }
            else if (step is SpendMoneyStep spend)
            {
                EditorGUILayout.TextField("Spend Key", spend.spendKey);
                EditorGUILayout.IntField("Required Amount", spend.requiredAmount);
            }
            else if (step is WaitForDayStep waitForDay)
            {
                EditorGUILayout.TextField("Wait Mode", waitForDay.waitMode.ToString());

                if (waitForDay.waitMode == WaitForDayStep.WaitMode.AbsoluteDay)
                {
                    EditorGUILayout.IntField("Target Day", waitForDay.targetDay);
                }
                else
                {
                    EditorGUILayout.IntField("Days To Wait", waitForDay.daysToWait);
                }
            }
            else if (step == null)
            {
                EditorGUILayout.HelpBox("This step could not be read from stored type/json data.", MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }
    }

    private string GetShortTypeName(string questTypeName)
    {
        if (string.IsNullOrWhiteSpace(questTypeName))
        {
            return "Main Quest Header";
        }

        int commaIndex = questTypeName.IndexOf(',');
        return commaIndex > 0 ? questTypeName.Substring(0, commaIndex) : questTypeName;
    }

    private List<int> GetMainQuestIndices(QuestEditorAsset questAsset)
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

    private List<int> GetSubQuestIndices(QuestEditorAsset questAsset, int mainQuestIndex)
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
}
