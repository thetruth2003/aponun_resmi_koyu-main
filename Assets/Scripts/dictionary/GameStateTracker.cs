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

        //LoadFromPlayerPrefs(); // ⬅ Başlangıçta verileri yükle
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



    // ------------------ DEBUG (Inspector Gibi) ------------------

    public void PrintState()
    {
        foreach (var kv in state)
            Debug.Log($"{kv.Key} ({kv.Value?.GetType().Name}): {kv.Value}");
    }

    // ------------------ LOAD FROM PLAYERPREFS ------------------

    private void LoadFromPlayerPrefs()
    {
        // Elle tek tek yüklemek gerekir çünkü PlayerPrefs anahtarlarını listeleyemez.
        // Buraya özel anahtarlar yazılabilir, örnek:
        string[] knownKeys = {
            "DialogIndex_ahmet", "Sold_elma", "Bought_karpuz", "Talked_ahmet_0"
            // gerektiği kadar ekle
        };

        foreach (string key in knownKeys)
        {
            if (PlayerPrefs.HasKey(key))
            {
                int intVal = PlayerPrefs.GetInt(key);
                state[key] = intVal;
            }
        }
    }
        // GameState'i dışarı açmak için (QuestSave kullanır)
    public Dictionary<string, object> GetAll()
    {
        return new Dictionary<string, object>(state);
    }
    public void IncrementCount(string key, int amount)
    {
        if (!state.ContainsKey(key)) state[key] = 0;

        if (state[key] is int current)
        {
            state[key] = current + amount;
        }
        else
        {
            Debug.LogWarning($"[GameStateTracker] '{key}' is not an int. Cannot increment.");
        }
    }

    public void ClearKey(string key)
    {
        if (state.ContainsKey(key))
            state.Remove(key);

        PlayerPrefs.DeleteKey("state_int_" + key);
        PlayerPrefs.DeleteKey("state_bool_" + key);
        PlayerPrefs.DeleteKey("state_string_" + key);
    }
}
