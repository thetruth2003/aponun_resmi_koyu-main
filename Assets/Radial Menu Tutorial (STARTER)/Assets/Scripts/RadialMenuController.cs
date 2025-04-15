using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialMenuController : MonoBehaviour
{
    public GameObject theMenu;

    public GameObject objRed, objBlue, objGreen, objYellow;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            theMenu.SetActive(true);
        }

        

        if(Input.GetKeyDown(KeyCode.Q))
        {
            SwitchRed();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            SwitchBlue();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchGreen();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SwitchYellow();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            SwitchAll();
        }
    }

    public void SwitchRed()
    {
        Debug.Log("Red");
        objRed.SetActive(!objRed.activeInHierarchy);
    }

    public void SwitchBlue()
    {
        objBlue.SetActive(!objBlue.activeInHierarchy);
    }

    public void SwitchGreen()
    {
        objGreen.SetActive(!objGreen.activeInHierarchy);
    }

    public void SwitchYellow()
    {
        objYellow.SetActive(!objYellow.activeInHierarchy);
    }

    public void SwitchAll()
    {
        SwitchRed();
        SwitchBlue();
        SwitchGreen();
        SwitchYellow();
    }
}
