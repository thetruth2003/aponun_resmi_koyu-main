using System.Collections.Generic;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    // Kayıtlı tüm saveable nesneler
    private readonly Dictionary<string, ISaveable> _saveables = new Dictionary<string, ISaveable>();
        private void Update()
    {
        // L tuşuna basıldığında tüm kayıtlı nesneleri yükle
        if (Input.GetKeyDown(KeyCode.L))
        {
            SaveLoadManager.Instance.LoadAll();
            Debug.Log("🔄 Loaded all saveables");
        }

        // V tuşuna basıldığında tüm kayıtlı nesneleri kaydet
        if (Input.GetKeyDown(KeyCode.V))
        {
            SaveLoadManager.Instance.SaveAll();
            Debug.Log("💾 Saved all saveables");
        }
    }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void Register(ISaveable saveable)
    {
        if (!_saveables.ContainsKey(saveable.UniqueID))
            _saveables.Add(saveable.UniqueID, saveable);
    }

    public void Unregister(ISaveable saveable)
    {
        _saveables.Remove(saveable.UniqueID);
    }

    /// <summary> Tüm kayıtlı nesneleri kaydeder. </summary>
    public void SaveAll()
    {
        foreach (var saveable in _saveables.Values)
        {
            saveable.SaveData();
            Debug.Log($"[Save] {saveable.UniqueID}");
        }
    }

    /// <summary> Tüm kayıtlı nesneleri yükler. </summary>
    public void LoadAll()
    {
        foreach (var saveable in _saveables.Values)
        {
            saveable.LoadData();
            Debug.Log($"[Load] {saveable.UniqueID}");
        }
    }
}

public interface ISaveable
{
    string UniqueID { get; }
    void SaveData();
    void LoadData();
}

    public abstract class SaveableMonoBehaviour : MonoBehaviour, ISaveable
{
    // Her saveable için sahnede benzersiz bir ID üretip kaydeder
    [SerializeField] private string uniqueID;

    public string UniqueID
    {
        get
        {
            if (string.IsNullOrEmpty(uniqueID))
                uniqueID = System.Guid.NewGuid().ToString();
            return uniqueID;
        }
    }

    private void OnEnable()  => SaveLoadManager.Instance.Register(this);
    private void OnDisable() => SaveLoadManager.Instance.Unregister(this);

    public abstract void SaveData();
    public abstract void LoadData();
}