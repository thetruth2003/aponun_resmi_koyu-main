using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tarladaki tüm SeedPoint socket’lerini yönetir ve aynı zamanda
/// ISaveable implementasyonu ile kaydet/yükle işlevlerini sağlar.
/// </summary>
[DisallowMultipleComponent]
public class Field : MonoBehaviour, ISaveable
{
    [Tooltip("Tarladaki tüm seed socket GameObject'leri")]
    [SerializeField] private GameObject[] seedPoints = null;

    // PlayerPrefs anahtarı için benzersiz ID
    private string _prefsKey => $"Field_{gameObject.GetInstanceID()}";

    #region Unity Lifecycle

    private void OnEnable()
    {
        SaveLoadManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        SaveLoadManager.Instance.Unregister(this);
    }

    #endregion

    #region Field Operations

    /// <summary> Tüm socket’leri toplu olarak sular. </summary>
    public void WaterAll()
    {
        foreach (var go in seedPoints)
        {
            if (go == null) continue;
            var sp = go.GetComponent<SeedPoint>();
            sp?.Water();
        }
    }

    /// <summary> Tüm socket’lere aynı tohum türünü eker. </summary>
    public void PlantAll(SeedType seedType)
    {
        foreach (var go in seedPoints)
        {
            if (go == null) continue;
            var sp = go.GetComponent<SeedPoint>();
            sp?.PlantSeed(seedType);
        }
    }

    #endregion

    #region ISaveable

    public string UniqueID => _prefsKey;

    /// <summary>
    /// SeedPointData’ları JSON’a çevirip PlayerPrefs’e kaydeder.
    /// </summary>
    public void SaveData()
    {
        var list = new List<SeedPointData>();
        foreach (var go in seedPoints)
        {
            if (go == null) continue;
            var sp = go.GetComponent<SeedPoint>();
            if (sp != null)
                list.Add(sp.GetState());
        }

        var wrapper = new Wrapper { data = list };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(_prefsKey, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// PlayerPrefs’ten okur, JSON’ı seriden geçirip her SeedPoint’e uygular.
    /// </summary>
    public void LoadData()
    {
        if (!PlayerPrefs.HasKey(_prefsKey))
            return;

        string json = PlayerPrefs.GetString(_prefsKey);
        var wrapper = JsonUtility.FromJson<Wrapper>(json);

        for (int i = 0; i < wrapper.data.Count && i < seedPoints.Length; i++)
        {
            var go = seedPoints[i];
            if (go == null) continue;
            var sp = go.GetComponent<SeedPoint>();
            sp?.SetState(wrapper.data[i]);
        }
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<SeedPointData> data;
    }

    #endregion
}
