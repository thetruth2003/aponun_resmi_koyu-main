using UnityEngine;

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
