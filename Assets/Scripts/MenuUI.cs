using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MenuUI, ana menu ekranini, secenekleri ve kayit ozeti akisini yonetir.
/// </summary>
public class MenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Load Summary (Optional)")]
    [SerializeField] private bool useLoadSummaryPanel = true;
    [SerializeField] private GameObject loadPanel;
    [SerializeField] private Button loadConfirmButton;
    [SerializeField] private Button loadCancelButton;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text questText;
    [SerializeField] private TMP_Text timeText;

    [Header("Main Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("Options Tabs")]
    [SerializeField] private GameObject voicePanel;
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject gamePanel;

    [SerializeField] private Button voiceTabButton;
    [SerializeField] private Button videoTabButton;
    [SerializeField] private Button controlsTabButton;
    [SerializeField] private Button gameTabButton;

    [Header("Options - Back")]
    [SerializeField] private Button backButton;

    [Header("Options - Audio")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";

    [Header("Options - Video")]
    [SerializeField] private Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Dropdown resolutionDropdown;

    private const string KEY_SAVE_EXISTS = "SaveExists";
    private const string KEY_LAST_SCENE  = "LastScene";
    private const string KEY_LAST_SCENE_PATH = "LastScenePath";
    private const int DEFAULT_NEW_GAME_BUILD_INDEX = 1;
    private const string KEY_VOL         = "opt_masterVol";
    private const string KEY_QUALITY     = "opt_quality";
    private const string KEY_FULLSCREEN  = "opt_full";
    private const string KEY_VSYNC       = "opt_vsync";
    private const string KEY_RESOLUTION  = "opt_res";

    private Resolution[] _resolutions;
    public GameObject[] killOnStart;
    private string _resolvedMasterVolumeParameter;
    private bool _masterVolumeParameterChecked;

    private string MetaPath => Path.Combine(Application.persistentDataPath, "meta_save.json");
    /// <summary>
    /// MetaSave sinifi, kayit ve yukleme akislarinda kullanilan veri veya yonetim davranisini saglar.
    /// </summary>
    [Serializable]
    private class MetaSave {
        public int day;
        public int money;
        public string lastQuest;
        public string savedAt;
    }

    private void Start()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (loadPanel) loadPanel.SetActive(false);

        bool hasSave = SaveCoordinator.HasAnySave() || PlayerPrefs.GetInt(KEY_SAVE_EXISTS, 0) == 1;
        if (continueButton) continueButton.interactable = hasSave;

        if (newGameButton) newGameButton.onClick.AddListener(OnClick_NewGame);
        if (continueButton) continueButton.onClick.AddListener(OnClick_Continue);
        if (optionsButton) optionsButton.onClick.AddListener(OnClick_Options);
        if (quitButton) quitButton.onClick.AddListener(OnClick_Quit);
        if (backButton) backButton.onClick.AddListener(OnClick_Back);

        if (loadConfirmButton) loadConfirmButton.onClick.AddListener(OnClick_LoadFromPanel);
        if (loadCancelButton)  loadCancelButton.onClick.AddListener(OnClick_CloseLoadPanel);

        InitQualityDropdown();
        InitResolutionDropdown();
        LoadAndApplyOptions();

        if (masterVolumeSlider) masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (vSyncToggle) vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void OnClick_NewGame()
    {
        if (SaveCoordinator.EnsureInstance() != null)
        {
            SaveCoordinator.Instance.StartDefaultNewGame();
            return;
        }

        PlayerPrefs.DeleteKey("DayCount");
        Time.timeScale = 1f;
        SceneManager.LoadScene(DEFAULT_NEW_GAME_BUILD_INDEX, LoadSceneMode.Single);
    }

    private void OnClick_Continue()
    {
        if (!SaveCoordinator.HasAnySave() && PlayerPrefs.GetInt(KEY_SAVE_EXISTS, 0) != 1) return;

        bool canUseSummaryPanel = useLoadSummaryPanel && loadPanel != null && loadConfirmButton != null;

        if (canUseSummaryPanel)
            OnClick_OpenLoadPanel();
        else
            OnClick_QuickLoad();
    }

    private void OnClick_Options()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
    }

    private void OnClick_Back()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
        if (mainPanel) mainPanel.SetActive(true);
        if (voicePanel)    voicePanel.SetActive(false);
        if (videoPanel)    videoPanel.SetActive(false);
        if (controlsPanel) controlsPanel.SetActive(false);
        if (gamePanel)     gamePanel.SetActive(false);
    }

    private void OnClick_Quit()
    {
        Application.Quit();
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }

    private void OnClick_OpenLoadPanel()
    {
        if (!loadPanel || !loadConfirmButton)
        {
            OnClick_QuickLoad();
            return;
        }

        if (loadPanel) loadPanel.SetActive(true);

        if (!File.Exists(MetaPath))
        {
            if (dayText)   dayText.text   = "Day: -";
            if (moneyText) moneyText.text = "Money: -";
            if (questText) questText.text = "Last Quest: -";
            if (timeText)  timeText.text  = "Saved: -";
            return;
        }

        var meta = JsonUtility.FromJson<MetaSave>(File.ReadAllText(MetaPath));
        if (dayText)   dayText.text   = $"Day: {meta.day}";
        if (moneyText) moneyText.text = $"Money: {meta.money}";
        if (questText) questText.text = $"Last Quest: {(string.IsNullOrWhiteSpace(meta.lastQuest) ? "-" : meta.lastQuest)}";
        if (timeText)  timeText.text  = $"Saved: {meta.savedAt}";
    }

    private void OnClick_LoadFromPanel()
    {
        if (loadPanel) loadPanel.SetActive(false);
        OnClick_QuickLoad();
    }

    public void OnClick_QuickLoad()
    {
        if (!SaveCoordinator.HasAnySave() && PlayerPrefs.GetInt(KEY_SAVE_EXISTS, 0) != 1) return;

        if (SaveCoordinator.EnsureInstance() != null)
        {
            SaveCoordinator.Instance.LoadLastSaveFromMenu();
            return;
        }

        string last = PlayerPrefs.GetString(KEY_LAST_SCENE_PATH, PlayerPrefs.GetString(KEY_LAST_SCENE, "aponun orjinal koyu"));
        Time.timeScale = 1f;
        string normalized = last.Replace("\\", "/");
        if (!normalized.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) && normalized.Contains("/"))
            normalized += ".unity";

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(normalized);
        if (buildIndex >= 0)
        {
            SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
            return;
        }

        SceneManager.LoadScene(Path.GetFileNameWithoutExtension(normalized), LoadSceneMode.Single);
    }

    private void OnClick_CloseLoadPanel()
    {
        if (loadPanel) loadPanel.SetActive(false);
    }

    private void ShowOnly(GameObject target)
    {
        if (!optionsPanel) return;

        if (voicePanel)    voicePanel.SetActive(false);
        if (videoPanel)    videoPanel.SetActive(false);
        if (controlsPanel) controlsPanel.SetActive(false);
        if (gamePanel)     gamePanel.SetActive(false);

        if (target) target.SetActive(true);
    }

    private void OnClick_VoiceTab()    => ShowOnly(voicePanel);
    private void OnClick_VideoTab()    => ShowOnly(videoPanel);
    private void OnClick_ControlsTab() => ShowOnly(controlsPanel);
    private void OnClick_GameTab()     => ShowOnly(gamePanel);

    private void InitQualityDropdown()
    {
        if (!qualityDropdown) return;
        qualityDropdown.ClearOptions();
        var names = new List<string>(QualitySettings.names);
        qualityDropdown.AddOptions(names);

        int savedIdx = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
        savedIdx = Mathf.Clamp(savedIdx, 0, names.Count - 1);
        qualityDropdown.value = savedIdx;
        qualityDropdown.RefreshShownValue();
    }

    private void InitResolutionDropdown()
    {
        if (!resolutionDropdown) return;

        _resolutions = Screen.resolutions;
        var options = new List<String>();
        int currentIndex = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            string label = $"{_resolutions[i].width} x {_resolutions[i].height} @ {_resolutions[i].refreshRateRatio.value:0}Hz";
            options.Add(label);

            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);

        int savedIndex = PlayerPrefs.GetInt(KEY_RESOLUTION, currentIndex);
        savedIndex = Mathf.Clamp(savedIndex, 0, _resolutions.Length - 1);
        resolutionDropdown.value = savedIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void LoadAndApplyOptions()
    {
        float vol  = PlayerPrefs.GetFloat(KEY_VOL, 0.75f);
        int   qual = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
        bool  full = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        bool  vsyn = PlayerPrefs.GetInt(KEY_VSYNC, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;

        if (masterVolumeSlider) masterVolumeSlider.value = vol;
        ApplyVolume(vol);

        qual = Mathf.Clamp(qual, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(qual, true);
        if (qualityDropdown) { qualityDropdown.value = qual; qualityDropdown.RefreshShownValue(); }

        Screen.fullScreen = full;
        if (fullscreenToggle) fullscreenToggle.isOn = full;

        QualitySettings.vSyncCount = vsyn ? 1 : 0;
        if (vSyncToggle) vSyncToggle.isOn = vsyn;

        if (resolutionDropdown && _resolutions != null && _resolutions.Length > 0)
        {
            int idx = Mathf.Clamp(PlayerPrefs.GetInt(KEY_RESOLUTION, resolutionDropdown.value), 0, _resolutions.Length - 1);
            SetResolutionByIndex(idx);
            resolutionDropdown.value = idx;
            resolutionDropdown.RefreshShownValue();
        }
    }

    private void OnVolumeChanged(float v)
    {
        PlayerPrefs.SetFloat(KEY_VOL, v);
        ApplyVolume(v);
    }

    private void OnQualityChanged(int idx)
    {
        PlayerPrefs.SetInt(KEY_QUALITY, idx);
        QualitySettings.SetQualityLevel(idx, true);
    }

    private void OnFullscreenChanged(bool full)
    {
        PlayerPrefs.SetInt(KEY_FULLSCREEN, full ? 1 : 0);
        Screen.fullScreen = full;
    }

    private void OnVSyncChanged(bool on)
    {
        PlayerPrefs.SetInt(KEY_VSYNC, on ? 1 : 0);
        QualitySettings.vSyncCount = on ? 1 : 0;
    }

    private void OnResolutionChanged(int idx)
    {
        PlayerPrefs.SetInt(KEY_RESOLUTION, idx);
        SetResolutionByIndex(idx);
    }

    private void ApplyVolume(float v)
    {
        if (!masterMixer) return;
        if (!TryGetMasterVolumeParameter(out string parameterName)) return;
        float dB = Mathf.Lerp(-80f, 0f, Mathf.Clamp01(v));
        masterMixer.SetFloat(parameterName, dB);
    }

    private bool TryGetMasterVolumeParameter(out string parameterName)
    {
        if (_masterVolumeParameterChecked)
        {
            parameterName = _resolvedMasterVolumeParameter;
            return !string.IsNullOrEmpty(parameterName);
        }

        _masterVolumeParameterChecked = true;

        string[] candidates =
        {
            masterVolumeParameter,
            "MasterVolume",
            "Master",
            "Volume"
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string candidate = candidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (masterMixer.GetFloat(candidate, out _))
            {
                _resolvedMasterVolumeParameter = candidate;
                break;
            }
        }

        parameterName = _resolvedMasterVolumeParameter;
        return !string.IsNullOrEmpty(parameterName);
    }

    private void SetResolutionByIndex(int idx)
    {
        if (_resolutions == null || _resolutions.Length == 0) return;
        idx = Mathf.Clamp(idx, 0, _resolutions.Length - 1);
        var r = _resolutions[idx];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
    }
}
