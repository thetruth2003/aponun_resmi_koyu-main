using UnityEngine;

/// <summary>
/// IDlocationtrigger sinifi, oyuncu belirli bir konum tetigine girdiginde ilgili ID bilgisini state tarafina aktarir.
/// </summary>
public class IDlocationtrigger : MonoBehaviour
{
    /// <summary>
    /// Oyuncu bu tetige girdiginde ilgili konum ID'sini gorev ve state sistemi icin aktifler.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var identifier = GetComponent<UniversalIdentifier>();
            if (identifier != null)
            {
                string key = $"player_hit_{identifier.ID.ToLower()}";
                Debug.Log($"âœ… Player temas etti: {key}");

                GameStateTracker.Instance.SetFlag(key, true);
            }
            else
            {
                Debug.LogError("[IDlocationtrigger] UniversalIdentifier bulunamadi!");
            }
        }
    }
}
