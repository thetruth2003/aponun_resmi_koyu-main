using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// test sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
public class test : MonoBehaviour
{
        public Camera playerCamera;
            public float maxDistance = 100f;
public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {

        }
        if (Input.GetMouseButtonDown(1))
        {

        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    interactable.Interact();
                    Debug.Log("Etkileşim gerçekleşti: " + hit.collider.gameObject.name);
                }

                Tools item = hit.collider.GetComponent<Tools>();
                if (item == null)
                    item = hit.collider.GetComponentInParent<Tools>();

                if (item != null)
                {
                    Debug.Log("SATIN ALMA: Tools bulundu → " + item.itemName);
                }
                else
                {
                    Debug.LogWarning("SATIN ALMA: Tools component yok → BuyItem çal???�şmad???�.");
                }

            }
            if (Physics.Raycast(ray, out hit, 3f))
            {
                if (hit.collider.CompareTag("NPC"))
                {
                    NPCInteraction npc = hit.collider.GetComponent<NPCInteraction>();
                    if (npc != null)
                    {
                        npc.StartDialog();
                        Debug.Log("NPC ile etkileşim başlad???�: " + npc.gameObject.name);
                    }
                }
            }
        }
    }
}
