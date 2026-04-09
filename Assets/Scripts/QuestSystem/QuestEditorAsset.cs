using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// QuestEditorAsset sinifi, gorev sistemi icindeki ilgili davranis veya veriyi yonetir.
/// </summary>
[CreateAssetMenu(menuName = "Quest System/Editor Asset")]
public class QuestEditorAsset : ScriptableObject
{
    public List<QuestContainer> quests = new List<QuestContainer>();
}

