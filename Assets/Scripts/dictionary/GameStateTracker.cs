using System.Collections.Generic;
using UnityEngine;

public class GameStateTracker : MonoBehaviour
{
    public static GameStateTracker Instance { get; private set; }

    // Tek bir sözlükte hem sayıcıları (int) hem bayrakları (bool) tutuyoruz
    private Dictionary<string, object> state = new Dictionary<string, object>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // ─── Sayaç işlemleri ───

    // Belirli bir anahtarın sayacını 1 artırır (veya amount kadar)
    public void IncrementCount(string key, int amount = 1)
    {
        int current = GetCount(key);
        state[key] = current + amount;
    }

    // Sayaç değeri döner (hiç yoksa 0)
    public int GetCount(string key)
    {
        if (state.TryGetValue(key, out var val) && val is int i)
            return i;
        return 0;
    }

    // ─── Bayrak işlemleri ───

    // Boolean flag atar
    public void SetFlag(string key, bool value)
    {
        state[key] = value;
    }

    // Bayrağı döner (hiç yoksa false)
    public bool GetFlag(string key)
    {
        if (state.TryGetValue(key, out var val) && val is bool b)
            return b;
        return false;
    }

    // ─── Debug / Live-view için ───

    // Bütün anahtar-değer çiftlerini döner
    public Dictionary<string, object> GetAll()
    {
        return new Dictionary<string, object>(state);
    }
    // Adds inside GameStateTracker class

    /// <summary>
    /// Doğrudan bir sayaç/flag’i bu değere set eder.
    /// </summary>
    public void SetCount(string key, int value)
    {
        state[key] = value;
    }

    /// <summary>
    /// Belirli bir anahtarı tamamen kaldırır.
    /// </summary>
    public void ClearKey(string key)
    {
        state.Remove(key);
    }

}
