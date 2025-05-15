using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Quest System/Editor Asset")]
public class QuestEditorAsset : ScriptableObject
{
    public List<QuestChainData> chains = new List<QuestChainData>();
}

[System.Serializable]
public class QuestChainData
{
    public string title;
    public List<QuestContainer> quests = new List<QuestContainer>();
}
