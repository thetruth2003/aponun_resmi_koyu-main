using UnityEngine;

/// <summary>
/// NPCManager sinifi, ilgili sistemin akisini ve durum yonetimini ustlenir.
/// </summary>
public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// NPC ile konuþuldu mu kontrol et.
    /// Kendine göre logic ekle: örneðin bir flag sistemi veya trigger ile iþaretleme.
    /// ???imdilik false döndürür.
    /// </summary>
    public bool HasTalkedTo(GameObject npc)
    {
        return false;
    }
}
