using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quest System/Quest Chain")]
public class QuestChain : ScriptableObject
{
    public List<QuestContainer> quests = new List<QuestContainer>();
}
