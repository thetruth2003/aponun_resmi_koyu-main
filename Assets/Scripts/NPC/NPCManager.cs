using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// NPC ile konuşuldu mu kontrol et.
    /// Kendine göre logic ekle: örneğin bir flag sistemi veya trigger ile işaretleme.
    /// Şimdilik false döndürür.
    /// </summary>
    public bool HasTalkedTo(GameObject npc)
    {
        // TODO: Buraya kendi konuşma tespit mantığını yaz
        return false;
    }
}
