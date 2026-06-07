using UnityEditor;
using UnityEngine;

/// <summary>
/// CutsceneSubtitleTrack icin zaman damgali altyazi duzenleyicisi.
/// </summary>
[CustomEditor(typeof(CutsceneSubtitleTrack))]
public class CutsceneSubtitleTrackEditor : Editor
{
    SerializedProperty voiceClipProp;
    SerializedProperty cuesProp;
    SerializedProperty endPaddingProp;

    void OnEnable()
    {
        voiceClipProp = serializedObject.FindProperty("voiceClip");
        cuesProp = serializedObject.FindProperty("cues");
        endPaddingProp = serializedObject.FindProperty("endPadding");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var track = (CutsceneSubtitleTrack)target;
        AudioClip clip = voiceClipProp.objectReferenceValue as AudioClip;
        float clipLength = clip ? clip.length : 0f;

        EditorGUILayout.HelpBox(
            "Bu asset tek bir ses kaydi icin zaman damgali altyazi tutar. Her cue girdigin saniyede ekrana gelir ve bir sonraki cue gelene kadar ekranda kalir. Altyaziyi erken temizlemek istersen bos metinli cue ekleyebilirsin.",
            MessageType.None);

        EditorGUILayout.PropertyField(voiceClipProp);
        EditorGUILayout.PropertyField(endPaddingProp);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Track Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Clip Length", clip ? FormatTime(clipLength) : "No clip");
            EditorGUILayout.LabelField("Playback Duration", FormatTime(track.GetDuration()));
            EditorGUILayout.LabelField("Cue Count", cuesProp.arraySize.ToString());
        }

        DrawTimelinePreview(track, clipLength);
        DrawToolbar(track, clipLength);
        DrawCueList(track, clipLength);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawToolbar(CutsceneSubtitleTrack track, float clipLength)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Subtitle"))
            {
                int index = cuesProp.arraySize;
                cuesProp.InsertArrayElementAtIndex(index);
                SerializedProperty cue = cuesProp.GetArrayElementAtIndex(index);
                cue.FindPropertyRelative("time").floatValue = GetSuggestedCueTime(index, clipLength);
                cue.FindPropertyRelative("text").stringValue = string.Empty;
            }

            if (GUILayout.Button("Add Clear Cue"))
            {
                int index = cuesProp.arraySize;
                cuesProp.InsertArrayElementAtIndex(index);
                SerializedProperty cue = cuesProp.GetArrayElementAtIndex(index);
                cue.FindPropertyRelative("time").floatValue = GetSuggestedCueTime(index, clipLength);
                cue.FindPropertyRelative("text").stringValue = string.Empty;
            }

            if (GUILayout.Button("Sort Times"))
            {
                serializedObject.ApplyModifiedProperties();
                track.SortCues();
                track.ClampCueTimesToClip();
                EditorUtility.SetDirty(track);
                serializedObject.Update();
            }
        }
    }

    float GetSuggestedCueTime(int newIndex, float clipLength)
    {
        if (newIndex <= 0) return 0f;

        SerializedProperty prevCue = cuesProp.GetArrayElementAtIndex(newIndex - 1);
        float prevTime = prevCue.FindPropertyRelative("time").floatValue;
        float nextTime = prevTime + 1f;

        if (clipLength > 0f)
        {
            nextTime = Mathf.Clamp(nextTime, 0f, clipLength);
        }

        return nextTime;
    }

    void DrawCueList(CutsceneSubtitleTrack track, float clipLength)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Subtitle Cues", EditorStyles.boldLabel);

        if (cuesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Henuz cue yok. Add Subtitle ile ilk altyazini ekleyebilirsin.", MessageType.Info);
            return;
        }

        for (int i = 0; i < cuesProp.arraySize; i++)
        {
            SerializedProperty cue = cuesProp.GetArrayElementAtIndex(i);
            SerializedProperty timeProp = cue.FindPropertyRelative("time");
            SerializedProperty textProp = cue.FindPropertyRelative("text");

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Cue {i + 1}  ({FormatTime(timeProp.floatValue)})", EditorStyles.boldLabel);

                    GUI.enabled = i > 0;
                    if (GUILayout.Button("Up", GUILayout.Width(42f)))
                    {
                        cuesProp.MoveArrayElement(i, i - 1);
                    }

                    GUI.enabled = i < cuesProp.arraySize - 1;
                    if (GUILayout.Button("Down", GUILayout.Width(52f)))
                    {
                        cuesProp.MoveArrayElement(i, i + 1);
                    }

                    GUI.enabled = true;
                    if (GUILayout.Button("Delete", GUILayout.Width(58f)))
                    {
                        cuesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                if (clipLength > 0f)
                {
                    timeProp.floatValue = EditorGUILayout.Slider("Start Time", timeProp.floatValue, 0f, clipLength);
                }
                else
                {
                    timeProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField("Start Time", timeProp.floatValue));
                }

                timeProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField("Precise Time", timeProp.floatValue));
                textProp.stringValue = EditorGUILayout.TextArea(textProp.stringValue, GUILayout.MinHeight(52f));

                if (string.IsNullOrWhiteSpace(textProp.stringValue))
                {
                    EditorGUILayout.HelpBox("Bos text kullanirsan bu saniyede altyazi temizlenir.", MessageType.None);
                }
            }
        }
    }

    void DrawTimelinePreview(CutsceneSubtitleTrack track, float clipLength)
    {
        if (clipLength <= 0f)
        {
            EditorGUILayout.HelpBox("Timeline preview icin bir AudioClip ver. Klip uzunlugu gelince cue'lar bar ustunde goreceksin.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Timeline Preview", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(10f, 54f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.14f, 0.14f, 0.14f, 1f));

        Rect barRect = new Rect(rect.x + 8f, rect.center.y - 6f, rect.width - 16f, 12f);
        EditorGUI.DrawRect(barRect, new Color(0.25f, 0.25f, 0.25f, 1f));

        Handles.BeginGUI();
        for (int i = 0; i < track.cues.Count; i++)
        {
            TimedSubtitleCue cue = track.cues[i];
            if (cue == null) continue;

            float normalized = Mathf.Clamp01(cue.time / clipLength);
            float x = Mathf.Lerp(barRect.xMin, barRect.xMax, normalized);

            EditorGUI.DrawRect(new Rect(x - 1f, barRect.yMin - 10f, 2f, barRect.height + 20f), new Color(0.33f, 0.9f, 1f, 1f));
            GUI.Label(new Rect(Mathf.Clamp(x - 20f, rect.xMin, rect.xMax - 40f), barRect.yMax + 4f, 40f, 16f), $"{i + 1}", EditorStyles.centeredGreyMiniLabel);
        }
        Handles.EndGUI();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("0.00", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(FormatTime(clipLength), EditorStyles.miniLabel, GUILayout.Width(60f));
        EditorGUILayout.EndHorizontal();
    }

    static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int mins = Mathf.FloorToInt(seconds / 60f);
        float secs = seconds - mins * 60f;
        return $"{mins:00}:{secs:00.00}";
    }
}
