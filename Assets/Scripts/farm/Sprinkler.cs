using UnityEngine;

/// <summary>
/// Sprinkler sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
public class Sprinkler : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Field field = other.GetComponent<Field>();
        if (field != null)
        {
            field.WaterAll();
        }
    }
}
