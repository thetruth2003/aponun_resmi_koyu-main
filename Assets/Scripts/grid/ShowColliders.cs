using UnityEngine;

public class ShowColliders : MonoBehaviour
{
    public GameObject[] cells; // Bütün cell'leri içeren dizi

    void OnDrawGizmos()
    {
        foreach (GameObject cell in cells)
        {
            if (cell != null) // Eğer cell varsa
            {
                Collider collider = cell.GetComponent<Collider>(); // Collider'ı al
                if (collider != null) // Eğer collider varsa
                {
                    Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size); // Collider'ın sınırlarını çiz
                }
            }
        }
    }
}
