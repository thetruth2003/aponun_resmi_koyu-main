using UnityEngine;
using System.Collections.Generic;

public class GameStateTracker : MonoBehaviour
{
    public static GameStateTracker Instance;

    public Dictionary<string, object> state = new Dictionary<string, object>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ------------------ INT ------------------

    public int GetCount(string key)
    {
        if (state.ContainsKey(key)) return (int)state[key];

        int value = PlayerPrefs.GetInt(key, 0);
        state[key] = value;
        return value;
    }

    public void SetCount(string key, int value)
    {
        state[key] = value;
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public void IncrementCount(string key)
    {
        int current = GetCount(key);
        SetCount(key, current + 1);
    }

    public void IncrementCount(string key, int amount)
    {
        int current = GetCount(key);
        SetCount(key, current + amount);
    }

    // ------------------ BOOL ------------------

    public bool GetFlag(string key)
    {
        if (state.ContainsKey(key)) return (bool)state[key];

        bool value = PlayerPrefs.GetInt(key, 0) == 1;
        state[key] = value;
        return value;
    }

    public void SetFlag(string key, bool value)
    {
        state[key] = value;
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ------------------ STRING ------------------

    public string GetString(string key)
    {
        if (state.ContainsKey(key)) return (string)state[key];

        string value = PlayerPrefs.GetString(key, "");
        state[key] = value;
        return value;
    }

    public void SetString(string key, string value)
    {
        state[key] = value;
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    // ------------------ DIALOG INDEX ------------------

    public int GetDialogIndex(string npcName)
    {
        return GetCount($"DialogIndex_{npcName.ToLower()}");
    }

    public void SetDialogIndex(string npcName, int value)
    {
        SetCount($"DialogIndex_{npcName.ToLower()}", value);
    }

    public bool HasKey(string key)
    {
        return state.ContainsKey(key) || PlayerPrefs.HasKey(key);
    }

    // ------------------ DEBUG ------------------

    public void PrintState()
    {
        foreach (var kv in state)
            Debug.Log($"{kv.Key} ({kv.Value?.GetType().Name}): {kv.Value}");
    }

    public Dictionary<string, object> GetAll()
    {
        return new Dictionary<string, object>(state);
    }

    public void ClearKey(string key)
    {
        if (state.ContainsKey(key))
            state.Remove(key);

        PlayerPrefs.DeleteKey(key);
    }
    public void ClearAll()
    {
        var keys = new List<string>(state.Keys);
        foreach (var key in keys)
        {
            PlayerPrefs.DeleteKey(key);
        }

        state.Clear();
        PlayerPrefs.Save();

        // ✅ ActiveQuestSystem'daki currentIndex değerlerini de sıfırla
        if (ActiveQuestSystem.Instance != null)
        {
            foreach (var q in ActiveQuestSystem.Instance.allQuests)
            {
                q.currentIndex = 0;
            }
        }

        Debug.Log("[GameStateTracker] Tüm state ve görev ilerlemesi sıfırlandı.");
    }
}
