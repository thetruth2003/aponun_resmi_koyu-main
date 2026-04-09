using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SaveLoadManager, sahnedeki kaydedilebilir nesneleri kaydedip geri yukleyen merkezi yoneticidir.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    private readonly Dictionary<string, ISaveable> saveables = new Dictionary<string, ISaveable>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Instance.LoadAll();
            Debug.Log("Loaded all saveables");
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            Instance.SaveAll();
            Debug.Log("Saved all saveables");
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    public void Register(ISaveable saveable)
    {
        if (!saveables.ContainsKey(saveable.UniqueID))
        {
            saveables.Add(saveable.UniqueID, saveable);
        }
    }

    public void Unregister(ISaveable saveable)
    {
        saveables.Remove(saveable.UniqueID);
    }

    /// <summary>
    /// Kayitli tum nesnelerin verisini disariya yazar.
    /// </summary>
    public void SaveAll()
    {
        foreach (ISaveable saveable in saveables.Values)
        {
            saveable.SaveData();
            Debug.Log($"[Save] {saveable.UniqueID}");
        }
    }

    /// <summary>
    /// Kayitli tum nesnelerin verisini geri yukler.
    /// </summary>
    public void LoadAll()
    {
        foreach (ISaveable saveable in saveables.Values)
        {
            saveable.LoadData();
            Debug.Log($"[Load] {saveable.UniqueID}");
        }
    }
}

/// <summary>
/// SaveLoadManager ile calisan kaydedilebilir nesnelerin uygulamasi gereken temel arayuzdur.
/// </summary>
public interface ISaveable
{
    string UniqueID { get; }
    void SaveData();
    void LoadData();
}

/// <summary>
/// Save sistemine kaydolup benzersiz kimlik ureten temel MonoBehaviour sinifidir.
/// </summary>
public abstract class SaveableMonoBehaviour : MonoBehaviour, ISaveable
{
    [SerializeField] private string uniqueID;

    public string UniqueID
    {
        get
        {
            if (string.IsNullOrEmpty(uniqueID))
            {
                uniqueID = System.Guid.NewGuid().ToString();
            }

            return uniqueID;
        }
    }

    private void OnEnable()
    {
        SaveLoadManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        SaveLoadManager.Instance.Unregister(this);
    }

    public abstract void SaveData();
    public abstract void LoadData();
}
