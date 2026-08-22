using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tarladaki tÃƒÂ¼m SeedPoint socketÃ¢â‚¬????lerini yÃƒÂ¶netir ve ayn???Â± zamanda
/// ISaveable implementasyonu ile kaydet/yÃƒÂ¼kle iÃ…Å¸levlerini sa???Å¸lar.
/// </summary>
[DisallowMultipleComponent]
public class Field : MonoBehaviour, ISaveable
{
    [Tooltip("Tarladaki tÃƒÂ¼m seed socket GameObject'leri")]
    [SerializeField] private GameObject[] seedPoints = null;

    private string _prefsKey => $"Field_{gameObject.GetInstanceID()}";

    #region Unity Lifecycle

    private void OnEnable()
    {
        SaveLoadManager.TryRegister(this);
    }

    private void OnDisable()
    {
        SaveLoadManager.TryUnregister(this);
    }

    #endregion

    #region Field Operations

    /// <summary> TÃƒÂ¼m socketÃ¢â‚¬????leri toplu olarak sular. </summary>
    public void WaterAll()
    {
        foreach (var go in seedPoints)
        {
            if (go == null) continue;
            var sp = go.GetComponent<SeedPoint>();
            sp?.TryWater();
        }
    }

    /// <summary> TÃƒÂ¼m socketÃ¢â‚¬????lere ayn???Â± tohum tÃƒÂ¼rÃƒÂ¼nÃƒÂ¼ eker. </summary>
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
    /// SeedPointDataÃ¢â‚¬????lar???Â± JSONÃ¢â‚¬????a ÃƒÂ§evirip PlayerPrefsÃ¢â‚¬????e kaydeder.
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
    /// PlayerPrefsÃ¢â‚¬????ten okur, JSONÃ¢â‚¬???????Â± seriden geÃƒÂ§irip her SeedPointÃ¢â‚¬????e uygular.
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
