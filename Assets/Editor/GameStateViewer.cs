using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class GameStateViewer : EditorWindow
{
    private Vector2 scroll;
    private string editKey = "";
    private bool isFlag;
    private int intValue;
    private bool boolValue;

    [MenuItem("Window/Game State Viewer")]
    public static void ShowWindow() => GetWindow<GameStateViewer>("Game State Viewer");

    void OnGUI()
    {
        GUILayout.Label("Live Game State", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to view/edit state.", MessageType.Info);
            return;
        }
        if (GameStateTracker.Instance == null)
        {
            EditorGUILayout.HelpBox("GameStateTracker.Instance is null.", MessageType.Warning);
            return;
        }

        // ── 1) Edit Section ──
        EditorGUILayout.Space();
        GUILayout.Label("🔧 Edit State", EditorStyles.miniBoldLabel);

        editKey = EditorGUILayout.TextField("Key", editKey);
        isFlag = EditorGUILayout.Toggle("Is Flag (bool)?", isFlag);
        if (isFlag)
            boolValue = EditorGUILayout.Toggle("Bool Value", boolValue);
        else
            intValue = EditorGUILayout.IntField("Int Value", intValue);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add", GUILayout.Width(60)))
        {
            GameStateTracker.Instance.IncrementCount(editKey, intValue);
            RepaintQuestEditor();
        }

        if (GUILayout.Button("Set", GUILayout.Width(60)))
        {
            if (isFlag)
                GameStateTracker.Instance.SetFlag(editKey, boolValue);
            else
                GameStateTracker.Instance.SetCount(editKey, intValue);

            RepaintQuestEditor();
        }

        if (GUILayout.Button("Reset", GUILayout.Width(60)))
        {
            GameStateTracker.Instance.ClearKey(editKey);
            RepaintQuestEditor();
        }

        EditorGUILayout.EndHorizontal();

        // ── 2) View Section ──
        EditorGUILayout.Space();
        GUILayout.Label("🔍 Current Entries", EditorStyles.miniBoldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var kv in GameStateTracker.Instance.GetAll())
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(kv.Key, GUILayout.Width(200));
            EditorGUILayout.LabelField(kv.Value?.ToString() ?? "null");
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        // ── 3) Clear All ──
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.red;

        if (GUILayout.Button("❌ CLEAR ALL", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Tüm Verileri Sil", "Tüm oyun verilerini (PlayerPrefs ve Dictionary) sıfırlamak istediğine emin misin?", "Evet", "Vazgeç"))
            {
                GameStateTracker.Instance.ClearAll();
                RepaintQuestEditor();
            }
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    void RepaintQuestEditor()
    {
        EditorApplication.delayCall += () =>
        {
            var qe = EditorWindow.GetWindow<QuestEditorWindow>();
            qe?.Repaint();
        };
    }
}
