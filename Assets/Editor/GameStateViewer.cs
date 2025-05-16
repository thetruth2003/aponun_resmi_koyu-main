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

        // Play Mode kontrolü
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

        // ── 1) Düzenleme (Edit State) ──
        EditorGUILayout.Space();
        GUILayout.Label("🔧 Edit State", EditorStyles.miniBoldLabel);

        editKey = EditorGUILayout.TextField("Key", editKey);
        isFlag  = EditorGUILayout.Toggle("Is Flag (bool)?", isFlag);
        if (isFlag)
            boolValue = EditorGUILayout.Toggle("Bool Value", boolValue);
        else
            intValue = EditorGUILayout.IntField("Int Value", intValue);

        EditorGUILayout.BeginHorizontal();
        // ► Add butonu: sayacı mevcut değeriyle toplar
        if (GUILayout.Button("Add", GUILayout.Width(60)))
        {
            GameStateTracker.Instance.IncrementCount(editKey, intValue);
            EditorApplication.delayCall += () =>
            {
                var qe = EditorWindow.GetWindow<QuestEditorWindow>();
                qe?.Repaint();
            };
        }

        // ► Set butonu: sayacı doğrudan o değere ayarlar
        if (GUILayout.Button("Set", GUILayout.Width(60)))
        {
            if (isFlag)
                GameStateTracker.Instance.SetFlag(editKey, boolValue);
            else
                GameStateTracker.Instance.SetCount(editKey, intValue);

            EditorApplication.delayCall += () =>
            {
                var qe = EditorWindow.GetWindow<QuestEditorWindow>();
                qe?.Repaint();
            };
        }

        // ► Reset butonu: sayacı tamamen siler
        if (GUILayout.Button("Reset", GUILayout.Width(60)))
        {
            GameStateTracker.Instance.ClearKey(editKey);
            EditorApplication.delayCall += () =>
            {
                var qe = EditorWindow.GetWindow<QuestEditorWindow>();
                qe?.Repaint();
            };
        }
        EditorGUILayout.EndHorizontal();

        // ── 2) Live-view (Mevcut State) ──
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
    }
}
