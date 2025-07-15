using UnityEngine;

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
