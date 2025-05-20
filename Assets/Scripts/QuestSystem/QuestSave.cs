using System.Collections.Generic;
using UnityEngine;

public class QuestSave : MonoBehaviour, ISaveable
{
    public GameStateTracker gameStateTracker;

    public string GetUniqueID() => "QuestSave";

    [System.Serializable]
    public class KeyListWrapper
    {
        public List<string> keys = new List<string>();
    }

    public void SaveData()
    {
        if (gameStateTracker == null)
        {
            Debug.LogWarning("GameStateTracker atanmamış!");
            return;
        }

        var allState = gameStateTracker.GetAll();
        List<string> keys = new List<string>();

        foreach (var kv in allState)
        {
            string key = kv.Key;
            object value = kv.Value;

            if (value is int i)
            {
                PlayerPrefs.SetInt("state_int_" + key, i);
                keys.Add("int:" + key);
            }
            else if (value is bool b)
            {
                PlayerPrefs.SetInt("state_bool_" + key, b ? 1 : 0);
                keys.Add("bool:" + key);
            }
            else if (value is string s)
            {
                PlayerPrefs.SetString("state_string_" + key, s);
                keys.Add("string:" + key);
            }
        }

        string json = JsonUtility.ToJson(new KeyListWrapper { keys = keys });
        PlayerPrefs.SetString("state_keys", json);
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        if (gameStateTracker == null)
        {
            Debug.LogWarning("GameStateTracker atanmamış!");
            return;
        }

        if (!PlayerPrefs.HasKey("state_keys")) return;

        gameStateTracker.state.Clear();

        string json = PlayerPrefs.GetString("state_keys");
        var wrapper = JsonUtility.FromJson<KeyListWrapper>(json);

        foreach (var typedKey in wrapper.keys)
        {
            if (!typedKey.Contains(":")) continue;

            string[] parts = typedKey.Split(':');
            if (parts.Length != 2) continue;

            string type = parts[0];
            string key = parts[1];

            switch (type)
            {
                case "int":
                    if (PlayerPrefs.HasKey("state_int_" + key))
                        gameStateTracker.SetCount(key, PlayerPrefs.GetInt("state_int_" + key));
                    break;

                case "bool":
                    if (PlayerPrefs.HasKey("state_bool_" + key))
                        gameStateTracker.SetFlag(key, PlayerPrefs.GetInt("state_bool_" + key) == 1);
                    break;

                case "string":
                    if (PlayerPrefs.HasKey("state_string_" + key))
                        gameStateTracker.SetString(key, PlayerPrefs.GetString("state_string_" + key));
                    break;
            }
        }
    }


    [ContextMenu("Print GameState To Console")]
    public void PrintState()
    {
        var dict = gameStateTracker.GetAll();
        foreach (var kv in dict) ;
            //Debug.Log($"{kv.Key} ({kv.Value.GetType().Name}) = {kv.Value}");
    }

    void Update()
    {
       PrintState();
    }

}
