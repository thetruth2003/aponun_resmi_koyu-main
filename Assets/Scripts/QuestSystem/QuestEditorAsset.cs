using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Quest System/Editor Asset")]
public class QuestEditorAsset : ScriptableObject
{
    public List<QuestContainer> quests = new List<QuestContainer>();
}

