using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DialogLine sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
[System.Serializable]
public class DialogLine
{
    [TextArea(2, 5)]
    public string text;
    public AudioClip voiceClip;
    [Tooltip("Bu satir baslamadan once eklenecek gecikme.")]
    public float delayBeforeLine = 0f;
    [Tooltip("Delay sirasinda altyaziyi gizler.")]
    public bool hideSubtitleDuringDelay = true;
    [Tooltip("0'dan buyukse bu satirin ekranda kalma suresini dogrudan buna sabitler.")]
    public float durationOverride = 0f;
    [Tooltip("Satir bittikten sonra bir sonraki satira gecmeden once eklenecek ekstra bekleme.")]
    public float pauseAfterLine = 0f;
    [Tooltip("Pause After Line sirasinda altyaziyi gecici olarak gizler.")]
    public bool hideSubtitleDuringPause = false;
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
