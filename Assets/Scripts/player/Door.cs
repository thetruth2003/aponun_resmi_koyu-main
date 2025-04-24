using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    private bool isOpen = false; // Kap� a��k m� kapal� m� kontrol�

    public void Interact()
    {
        isOpen = !isOpen; // A�/Kapat de�i�tir
        transform.rotation = isOpen ? Quaternion.Euler(0, 90, 0) : Quaternion.Euler(0, 0, 0);
        Debug.Log("Kap� " + (isOpen ? "A��ld�!" : "Kapand�!"));
    }
}
