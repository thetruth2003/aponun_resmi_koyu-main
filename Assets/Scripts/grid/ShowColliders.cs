using UnityEngine;

/// <summary>
/// ShowColliders sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
public class ShowColliders : MonoBehaviour
{
    public GameObject[] cells;

    void OnDrawGizmos()
    {
        foreach (GameObject cell in cells)
        {
            if (cell != null)
            {
                Collider collider = cell.GetComponent<Collider>();
                if (collider != null)
                {
                    Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
                }
            }
        }
    }
}
