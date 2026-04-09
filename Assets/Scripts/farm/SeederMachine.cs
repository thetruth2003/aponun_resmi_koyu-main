using UnityEngine;

/// <summary>
/// SeederMachine sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
public class SeederMachine : MonoBehaviour
{
    public SeedType seedType = SeedType.Wheat;

    private void OnTriggerEnter(Collider other)
    {
        Field field = other.GetComponent<Field>();
        if (field != null)
        {
            field.PlantAll(seedType);
        }
    }
}
