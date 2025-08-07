using UnityEngine;
using System.Collections;
using System.Collections.Generic;   
public class UniversalIdentifier : MonoBehaviour
{
    public GameObject inventoryUI;
    public GameObject market;
    public SC_FPSController SC_FPSController;
    [SerializeField] private string id;
    

    public string ID => id;

    public void SetID(string newID) => id = newID;
    public void openmarket()
    {
        market.SetActive(true);
        inventoryUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SC_FPSController.freeze();
    }

    public void closemarket()
    {
        market.SetActive(false);
        inventoryUI.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        SC_FPSController.unfreeze();
    }
}
