using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TimedSubtitleCue, tek ses klibi icinde hangi saniyede hangi altyazinin cikacagini tutar.
/// </summary>
[Serializable]
public class TimedSubtitleCue
{
    [Min(0f)]
    public float time;

    [TextArea(2, 5)]
    public string text;
}

/// <summary>
/// CutsceneSubtitleTrack, tek bir ses kaydi ve ona bagli zaman damgali altyazilari tutar.
/// </summary>
[CreateAssetMenu(menuName = "Cutscene/Voice Subtitle Track", fileName = "NewCutsceneSubtitleTrack")]
public class CutsceneSubtitleTrack : ScriptableObject
{
    [Header("Voice")]
    public AudioClip voiceClip;

    [Tooltip("Her cue belirtilen saniyede ekrana gelir. Bir sonraki cue gelene kadar ekranda kalir. Altyaziyi erken kapatmak icin bos metinli cue ekleyebilirsin.")]
    public List<TimedSubtitleCue> cues = new();

    [Tooltip("Ses bittikten sonra cok kisa bir pay birakmak istersen kullan.")]
    [Min(0f)]
    public float endPadding = 0.15f;

    public float Duration => GetDuration();

    public float GetDuration()
    {
        float clipLength = voiceClip ? voiceClip.length : 0f;
        float cueLength = 0f;

        if (cues != null && cues.Count > 0)
        {
            cueLength = Mathf.Max(0f, cues[cues.Count - 1].time + endPadding);
        }

        return Mathf.Max(clipLength, cueLength);
    }

    public void SortCues()
    {
        if (cues == null) return;
        cues.Sort((a, b) => a.time.CompareTo(b.time));
    }

    public void ClampCueTimesToClip()
    {
        if (voiceClip == null || cues == null) return;

        float clipLength = Mathf.Max(0f, voiceClip.length);
        for (int i = 0; i < cues.Count; i++)
        {
            if (cues[i] == null) continue;
            cues[i].time = Mathf.Clamp(cues[i].time, 0f, clipLength);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        SortCues();
        ClampCueTimesToClip();
    }
#endif
}
