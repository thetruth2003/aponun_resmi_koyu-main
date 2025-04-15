using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
public class Item : MonoBehaviour, ISaveable
{
    public ItemData data;

    [HideInInspector]
    public Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void EnablePhysics()
    {
        rb.isKinematic = false; // Fiziksel kuvvetleri etkinleştir
    }

    public string GetUniqueID() => transform.GetInstanceID().ToString();

    public void SaveData()
    {
        if (gameObject.GetComponent<Tools>())
        {
            float duration = gameObject.GetComponent<Tools>().duration;
            PlayerPrefs.SetFloat(GetUniqueID() + "_duration", duration);
        }
        if (gameObject.GetComponent<Car>())
        {
            float Fuel = gameObject.GetComponent<Car>().Fuel;
            PlayerPrefs.SetFloat(GetUniqueID() + "_Fuel", Fuel);
            float duration = gameObject.GetComponent<Car>().duration;
            PlayerPrefs.SetFloat(GetUniqueID() + "_duration", duration);
        }
        if (gameObject.GetComponent<Building>())
        {
            string building_name = gameObject.GetComponent<Building>().building_name;
            PlayerPrefs.SetString(GetUniqueID() + "_building_name", building_name);
        }
        PlayerPrefs.SetFloat(GetUniqueID() + "_posX", transform.localPosition.x);
        PlayerPrefs.SetFloat(GetUniqueID() + "_posY", transform.localPosition.y);
        PlayerPrefs.SetFloat(GetUniqueID() + "_posZ", transform.localPosition.z);
    }

    public void LoadData()
    {
        if (gameObject.GetComponent<Tools>())
        {
            gameObject.GetComponent<Tools>().duration = PlayerPrefs.GetFloat(GetUniqueID() + "_duration");
            
        }
        if (gameObject.GetComponent<Car>())
        {
            gameObject.GetComponent<Car>().Fuel = PlayerPrefs.GetFloat(GetUniqueID() + "_Fuel");
            gameObject.GetComponent<Car>().duration = PlayerPrefs.GetFloat(GetUniqueID() + "_duration");
        }
        if (gameObject.GetComponent<Building>())
        {
            gameObject.GetComponent<Building>().building_name = PlayerPrefs.GetString(GetUniqueID() + "_building_name");

        }
        Vector3 currentPosition = transform.position;
        currentPosition.x = PlayerPrefs.GetFloat(GetUniqueID() + "_posX", 0);
        currentPosition.y = PlayerPrefs.GetFloat(GetUniqueID() + "_posY", 0);
        currentPosition.z = PlayerPrefs.GetFloat(GetUniqueID() + "_posZ", 0);
        transform.localPosition = currentPosition;
    }
}
