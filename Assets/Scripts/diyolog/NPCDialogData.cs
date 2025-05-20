using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Dialog/NPC Dialog Data")]
public class NPCDialogData : ScriptableObject
{
    [System.Serializable]
    public class DialogSection
    {
        public string title;
        [TextArea(2, 10)]
        public List<string> lines;
    }

    public List<DialogSection> sections = new List<DialogSection>();
}
