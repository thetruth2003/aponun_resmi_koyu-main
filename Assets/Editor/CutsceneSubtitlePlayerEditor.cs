using UnityEditor;
using UnityEngine;

/// <summary>
/// CutsceneSubtitlePlayer icin kullanim rehberi ve test tuslari.
/// </summary>
[CustomEditor(typeof(CutsceneSubtitlePlayer))]
public class CutsceneSubtitlePlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Kurulum: 1) Voice Subtitle Track asset'ini ver. 2) Subtitle TMP_Text ve panel'i bagla. 3) Ayni objede AudioSource zaten otomatik kullanilir. 4) CutsceneClip.onPlay icine bu componentin Play() metodunu baglayabilirsin. Track bitince clip bitsin istiyorsan finishAttachedCutsceneClipOnEnd acik olsun.",
            MessageType.None);

        DrawDefaultInspector();

        if (!Application.isPlaying) return;

        var player = (CutsceneSubtitlePlayer)target;

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Runtime Test", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Play Track"))
            {
                player.Play();
            }

            if (GUILayout.Button("Stop"))
            {
                player.StopPlayback();
            }
        }
    }
}
