using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogLine
{
    public string text;
    public AudioClip voiceClip;  // 🔊 Ses dosyası
}

[System.Serializable]
public class DialogSection
{
    public List<DialogLine> lines;
    public string viewKey;
}

[CreateAssetMenu(menuName = "NPC/Dialog Data")]
public class NPCDialogData : ScriptableObject
{
    public List<DialogSection> sections;
}
