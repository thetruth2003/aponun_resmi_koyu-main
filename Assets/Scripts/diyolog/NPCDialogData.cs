using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DialogLine sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
[System.Serializable]
public class DialogLine
{
    public string text;
    public AudioClip voiceClip;
}

/// <summary>
/// DialogSection sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
[System.Serializable]
public class DialogSection
{
    public List<DialogLine> lines;
    public string viewKey;
}

/// <summary>
/// NPCDialogData sinifi, ilgili veriyi tanimlamak ve tasimak icin kullanilir.
/// </summary>
[CreateAssetMenu(menuName = "NPC/Dialog Data")]
public class NPCDialogData : ScriptableObject
{
    public List<DialogSection> sections;
}
