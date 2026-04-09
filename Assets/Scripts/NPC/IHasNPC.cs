using UnityEngine;

/// <summary>
/// IHasNPC arayuzu, bir nesnenin bagli NPC referansini okuyup yazabilmesini saglar.
/// </summary>
public interface IHasNPC
{
    GameObject GetAssignedNPC();
    void SetAssignedNPC(GameObject npc);
}

