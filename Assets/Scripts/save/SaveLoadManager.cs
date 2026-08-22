using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SaveLoadManager, sahnedeki kaydedilebilir nesneleri kaydedip geri yukleyen merkezi yoneticidir.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;
    [Tooltip("Kapali tut. Runtime save/load giris noktasi SaveCoordinator olmali.")]
    [SerializeField] private bool allowLegacyDebugHotkeys = false;
    [SerializeField] private KeyCode legacySaveKey = KeyCode.V;
    [SerializeField] private KeyCode legacyLoadKey = KeyCode.L;

    private readonly Dictionary<string, ISaveable> saveables = new Dictionary<string, ISaveable>();

    private void Update()
    {
        if (Instance != this)
        {
            return;
        }

        if (!allowLegacyDebugHotkeys)
        {
            return;
        }

        if (Input.GetKeyDown(legacyLoadKey))
        {
            LoadAll();
        }

        if (Input.GetKeyDown(legacySaveKey))
        {
            SaveAll();
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

        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterExistingSaveables();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterExistingSaveables();
    }

    public static void TryRegister(ISaveable saveable)
    {
        Instance?.Register(saveable);
    }

    public static void TryUnregister(ISaveable saveable)
    {
        Instance?.Unregister(saveable);
    }

    public void Register(ISaveable saveable)
    {
        if (saveable == null || string.IsNullOrWhiteSpace(saveable.UniqueID))
        {
            return;
        }

        if (!saveables.ContainsKey(saveable.UniqueID))
        {
            saveables.Add(saveable.UniqueID, saveable);
        }
    }

    public void Unregister(ISaveable saveable)
    {
        if (saveable == null || string.IsNullOrWhiteSpace(saveable.UniqueID))
        {
            return;
        }

        saveables.Remove(saveable.UniqueID);
    }

    /// <summary>
    /// Kayitli tum nesnelerin verisini disariya yazar.
    /// </summary>
    public void SaveAll()
    {
        int savedCount = 0;
        List<string> staleKeys = null;

        foreach (KeyValuePair<string, ISaveable> pair in saveables)
        {
            ISaveable saveable = pair.Value;
            if (saveable is Object unityObject && unityObject == null)
            {
                staleKeys ??= new List<string>();
                staleKeys.Add(pair.Key);
                continue;
            }

            saveable.SaveData();
            savedCount++;
        }

        RemoveStaleKeys(staleKeys);
        LogVerbose($"[SaveLoadManager] {savedCount} saveable kaydedildi.");
    }

    /// <summary>
    /// Kayitli tum nesnelerin verisini geri yukler.
    /// </summary>
    public void LoadAll()
    {
        int loadedCount = 0;
        List<string> staleKeys = null;

        foreach (KeyValuePair<string, ISaveable> pair in saveables)
        {
            ISaveable saveable = pair.Value;
            if (saveable is Object unityObject && unityObject == null)
            {
                staleKeys ??= new List<string>();
                staleKeys.Add(pair.Key);
                continue;
            }

            saveable.LoadData();
            loadedCount++;
        }

        RemoveStaleKeys(staleKeys);
        LogVerbose($"[SaveLoadManager] {loadedCount} saveable yuklendi.");
    }

    private void RegisterExistingSaveables()
    {
        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            if (behaviour is ISaveable saveable)
            {
                Register(saveable);
            }
        }
    }

    private void RemoveStaleKeys(List<string> staleKeys)
    {
        if (staleKeys == null)
        {
            return;
        }

        for (int i = 0; i < staleKeys.Count; i++)
        {
            saveables.Remove(staleKeys[i]);
        }
    }

    private void LogVerbose(string message)
    {
        if (verboseLogs)
        {
            Debug.Log(message, this);
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
        SaveLoadManager.TryRegister(this);
    }

    private void OnDisable()
    {
        SaveLoadManager.TryUnregister(this);
    }

    public abstract void SaveData();
    public abstract void LoadData();
}
