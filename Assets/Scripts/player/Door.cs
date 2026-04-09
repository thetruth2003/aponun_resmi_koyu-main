using UnityEngine;

/// <summary>
/// Etkilesim alinca kapinin acik veya kapali durumunu degistirir.
/// </summary>
public class Door : MonoBehaviour, IInteractable
{
    private bool isOpen = false;

    public void Interact()
    {
        isOpen = !isOpen;
        transform.rotation = isOpen ? Quaternion.Euler(0, 90, 0) : Quaternion.Euler(0, 0, 0);
        Debug.Log("Kapý " + (isOpen ? "Açýldý!" : "Kapandý!"));
    }
}
