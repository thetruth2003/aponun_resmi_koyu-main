using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    private bool isOpen = false; // Kapý açýk mý kapalý mý kontrolü

    public void Interact()
    {
        isOpen = !isOpen; // Aç/Kapat deðiþtir
        transform.rotation = isOpen ? Quaternion.Euler(0, 90, 0) : Quaternion.Euler(0, 0, 0);
        Debug.Log("Kapý " + (isOpen ? "Açýldý!" : "Kapandý!"));
    }
}
