using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tarladaki t√ºm SeedPoint socket‚Ä????lerini y√∂netir ve ayn???± zamanda
/// ISaveable implementasyonu ile kaydet/y√ºkle i≈ülevlerini sa???ülar.
/// </summary>
[DisallowMultipleComponent]
public class Field : MonoBehaviour, ISaveable
{
    [Tooltip("Tarladaki t√ºm seed socket GameObject'leri")]
    [SerializeField] private GameObject[] seedPoints = null;

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

    /// <summary> T√ºm socket‚Ä????leri toplu olarak sular. </summary>
    public void WaterAll()
    {
        foreach (var go in seedPoints)
        {
            if (go == null) continue;
            var sp = go.GetComponent<SeedPoint>();
            sp?.Water();
        }
    }

    /// <summary> T√ºm socket‚Ä????lere ayn???± tohum t√ºr√ºn√º eker. </summary>
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
    /// SeedPointData‚Ä????lar???± JSON‚Ä????a √ßevirip PlayerPrefs‚Ä????e kaydeder.
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
    /// PlayerPrefs‚Ä????ten okur, JSON‚Ä???????± seriden ge√ßirip her SeedPoint‚Ä????e uygular.
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

    /// <summary>
    /// Wrapper sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
    /// </summary>
    [System.Serializable]
    private class Wrapper
    {
        public List<SeedPointData> data;
    }

    #endregion
}
