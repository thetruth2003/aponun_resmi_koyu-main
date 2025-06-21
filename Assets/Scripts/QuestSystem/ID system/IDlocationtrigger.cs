using UnityEngine;

public class IDlocationtrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var identifier = GetComponent<UniversalIdentifier>();
            if (identifier != null)
            {
                string key = $"player_hit_{identifier.ID.ToLower()}";
                Debug.Log($"✅ Player temas etti: {key}");
                
                // GameState'e kaydetmek istiyorsan:
                GameStateTracker.Instance.SetFlag(key, true);
            }
            else
            {
                Debug.LogError("[IDlocationtrigger] UniversalIdentifier bulunamadı!");
            }
        }
    }
}
