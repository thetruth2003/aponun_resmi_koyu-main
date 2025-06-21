using System.Collections.Generic;
using UnityEngine;

public class UniversalManager : MonoBehaviour
{
    public static UniversalManager Instance;

    private Dictionary<string, GameObject> idLookup = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        RegisterAll();
    }

    void RegisterAll()
    {
        idLookup.Clear();

        foreach (var obj in FindObjectsOfType<UniversalIdentifier>())
        {
            string id = obj.ID.ToLower();
            if (!string.IsNullOrEmpty(id) && !idLookup.ContainsKey(id))
            {
                idLookup[id] = obj.gameObject;
            }
        }

        Debug.Log($"[UniversalManager] {idLookup.Count} ID kaydedildi.");
    }

    public GameObject GetObjectByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        id = id.ToLower();
        idLookup.TryGetValue(id, out GameObject obj);
        return obj;
    }

    public bool HasID(string id)
    {
        return idLookup.ContainsKey(id.ToLower());
    }

    public void Refresh() => RegisterAll(); // Sahne dinamikse çağrılabilir
}
