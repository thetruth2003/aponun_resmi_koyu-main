using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class svmanager : MonoBehaviour
{
    public Inventory_UI uı;
    public Item item;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            saveobjevt();
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            Load();
        }
    }

    void saveobjevt()
    {
        ISaveable[] allSaveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();

        foreach (var Save in allSaveables)
        {
            Save.SaveData();
        }
    }

    void Load()
    {
        ISaveable[] allSaveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();

        foreach (var Save in allSaveables)
        {
            Save.LoadData();
            Debug.Log(Save);
        }
    }
}


public interface ISaveable
{
    string GetUniqueID();
    void SaveData();
    void LoadData();
}
