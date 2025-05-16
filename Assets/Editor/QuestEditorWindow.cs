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
    private readonly string[] questTypeOptions = {
        "Talk To NPC", "Go To Location",
        "Sell Item", "Buy Item", "Harvest Item"
    };

    [MenuItem("Window/Quest Editor")]
    public static void ShowWindow() => GetWindow<QuestEditorWindow>("Quest Editor");

    private void OnGUI()
    {
        GUILayout.Label("Quest Editor", EditorStyles.boldLabel);

        // — QuestAsset seç
        questAsset = (QuestEditorAsset)EditorGUILayout.ObjectField(
            "Quest Data", questAsset, typeof(QuestEditorAsset), false
        );
        if (questAsset == null)
        {
            EditorGUILayout.HelpBox(
                "Please assign a QuestEditorAsset to begin editing.",
                MessageType.Info
            );
            return;
        }

        EditorGUILayout.Space();

        // — Ana Görev Ekle/Sil —
        EditorGUILayout.BeginHorizontal();
        newChainTitle = EditorGUILayout.TextField("New Main Quest", newChainTitle);
        if (GUILayout.Button("Add Main Quest", GUILayout.MaxWidth(130))
            && !string.IsNullOrWhiteSpace(newChainTitle))
        {
            questAsset.chains.Add(new QuestChainData { title = newChainTitle });
            newChainTitle = "";
            EditorUtility.SetDirty(questAsset);
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("🗑 Delete Selected", GUILayout.MaxWidth(130))
            && selectedChainIndex >= 0
            && selectedChainIndex < questAsset.chains.Count)
        {
            if (EditorUtility.DisplayDialog(
                    "Confirm Delete",
                    $"Delete '{questAsset.chains[selectedChainIndex].title}'?",
                    "Yes", "Cancel"))
            {
                questAsset.chains.RemoveAt(selectedChainIndex);
                selectedChainIndex = -1;
                EditorUtility.SetDirty(questAsset);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // — Ana Görev Listesi —
        GUILayout.Label("Main Quests:", EditorStyles.boldLabel);
        chainListScroll = EditorGUILayout.BeginScrollView(chainListScroll, GUILayout.Height(120));
        for (int i = 0; i < questAsset.chains.Count; i++)
            if (GUILayout.Toggle(selectedChainIndex == i, questAsset.chains[i].title, "Button"))
                selectedChainIndex = i;
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // — Alt Görevler —
        if (selectedChainIndex < 0 || selectedChainIndex >= questAsset.chains.Count) return;
        var chain = questAsset.chains[selectedChainIndex];
        GUILayout.Label($"Editing: {chain.title}", EditorStyles.boldLabel);

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
                chain.quests.Add(qc);
                EditorUtility.SetDirty(questAsset);
            }
        }
        EditorGUILayout.EndHorizontal();

        // — Sadece Play Mode’da durum etiketlerini hesapla ve göster
        int activeIndex = -1;
        if (Application.isPlaying)
        {
            for (int i = 0; i < chain.quests.Count; i++)
            {
                if (!chain.quests[i].GetStepInstance().IsComplete())
                {
                    activeIndex = i;
                    break;
                }
            }
        }

        // — Alt görev listesini çiz
        subQuestScroll = EditorGUILayout.BeginScrollView(subQuestScroll);
        for (int j = 0; j < chain.quests.Count; j++)
        {
            var qc = chain.quests[j];
            EditorGUILayout.BeginVertical("box");

            // Etiket: Play Mode’da dinamik, Edit Mode’da sade
            string label = $"Sub Quest {j + 1}: {qc.questName}";
            if (Application.isPlaying)
            {
                if (j < activeIndex)       label += "   ✔ Completed";
                else if (j == activeIndex) label += "   → Active";
                else                        label += "   (Inactive)";
            }
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            // Düzenleme: Play Mode’da sadece aktif adıma açık
            bool prevEnabled = GUI.enabled;
            if (Application.isPlaying)
                GUI.enabled = (j == activeIndex);

            // Düzenleme blokları
            var step2 = qc.GetStepInstance();
            EditorGUI.BeginChangeCheck();
            if (step2 is TalkToNPCStep talk)
                talk.npcObject = (GameObject)EditorGUILayout.ObjectField("NPC Object", talk.npcObject, typeof(GameObject), true);
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

            // Düzenlemeyi eski haline getir
            GUI.enabled = prevEnabled;

            // Silme butonu
            if (GUILayout.Button("Remove Sub Quest", GUILayout.MaxWidth(150)))
            {
                chain.quests.RemoveAt(j);
                EditorUtility.SetDirty(questAsset);
                break;
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        // — Play Mode’da Set/Reset sonrası GUI’yi güncellemek için
        if (Application.isPlaying)
            Repaint();
    }
}
