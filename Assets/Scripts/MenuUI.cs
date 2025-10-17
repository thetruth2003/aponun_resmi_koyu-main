using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering; // Volume için
using TMPro;

public class MenuUI : MonoBehaviour
{
    // ===================== PANELS =====================
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;

    // ===================== LOAD SUMMARY (Optional) =====================
    [Header("Load Summary (Optional)")]
    [SerializeField] private bool useLoadSummaryPanel = true;   // true → Continue panel açar; false → direkt yükler
    [SerializeField] private GameObject loadPanel;              // özet paneli
    [SerializeField] private Button loadConfirmButton;          // panel içi "Load/Continue"
    [SerializeField] private Button loadCancelButton;           // panel içi "Close/Back"
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text questText;
    [SerializeField] private TMP_Text timeText;

    // ===================== MAIN BUTTONS =====================
    [Header("Main Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    // ===================== OPTIONS TABS & BUTTONS =====================
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

    // ===================== OPTIONS WIDGETS =====================
    [Header("Options - Audio")]
    [SerializeField] private AudioMixer masterMixer;      // Exposed param: "MasterVolume"
    [SerializeField] private Slider masterVolumeSlider;   // 0..1

    [Header("Options - Video")]
    [SerializeField] private Dropdown qualityDropdown;    // QualitySettings.names
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Dropdown resolutionDropdown; // optional

    // ===================== PLAYER PREFS KEYS =====================
    private const string KEY_SAVE_EXISTS = "SaveExists";
    private const string KEY_LAST_SCENE  = "LastScene";
    private const string KEY_VOL         = "opt_masterVol";
    private const string KEY_QUALITY     = "opt_quality";
    private const string KEY_FULLSCREEN  = "opt_full";
    private const string KEY_VSYNC       = "opt_vsync";
    private const string KEY_RESOLUTION  = "opt_res";

    private Resolution[] _resolutions;
    public GameObject[] killOnStart; // Menu Camera, Global Volume, vs.

    // ===================== META (summary) =====================
    private string MetaPath => Path.Combine(Application.persistentDataPath, "meta_save.json");
    [Serializable]
    private class MetaSave {
        public int day;
        public int money;
        public string lastQuest;
        public string savedAt;
    }

    private void Start()
    {
        // Paneller
        if (mainPanel) mainPanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (loadPanel) loadPanel.SetActive(false);

        // Continue aktifliği
        bool hasSave = PlayerPrefs.GetInt(KEY_SAVE_EXISTS, 0) == 1;
        if (continueButton) continueButton.interactable = hasSave;

        // Main buttons
        if (newGameButton) newGameButton.onClick.AddListener(OnClick_NewGame);
        if (continueButton) continueButton.onClick.AddListener(OnClick_Continue);
        if (optionsButton) optionsButton.onClick.AddListener(OnClick_Options);
        if (quitButton) quitButton.onClick.AddListener(OnClick_Quit);
        if (backButton) backButton.onClick.AddListener(OnClick_Back);

        // Load panel buttons
        if (loadConfirmButton) loadConfirmButton.onClick.AddListener(OnClick_LoadFromPanel);
        if (loadCancelButton)  loadCancelButton.onClick.AddListener(OnClick_CloseLoadPanel);

        // Options init + listeners
        InitQualityDropdown();
        InitResolutionDropdown();
        LoadAndApplyOptions();

        if (masterVolumeSlider) masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (vSyncToggle) vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    // ===================== MAIN =====================
    private void OnClick_NewGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("aponun orjinal koyu", LoadSceneMode.Single);
    }

    // Continue → panel veya direkt
    private void OnClick_Continue()
    {
        if (PlayerPrefs.GetInt(KEY_SAVE_EXISTS, 0) != 1) return;

        if (useLoadSummaryPanel)
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

    // ===================== LOAD SUMMARY & QUICK LOAD =====================
    private void OnClick_OpenLoadPanel()
    {
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

    // Direkt yükleme (panel yok)
    public void OnClick_QuickLoad()
    {
        if (PlayerPrefs.GetInt(KEY_SAVE_EXISTS, 0) != 1) return;
        string last = PlayerPrefs.GetString(KEY_LAST_SCENE, "aponun orjinal koyu");
        Time.timeScale = 1f;
        SceneManager.LoadScene(last, LoadSceneMode.Single);
    }

    private void OnClick_CloseLoadPanel()
    {
        if (loadPanel) loadPanel.SetActive(false);
    }

    // ===================== OPTIONS: TAB LOGIC =====================
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

    // ===================== OPTIONS: INIT & APPLY =====================
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

    // ===================== OPTIONS: LISTENERS =====================
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

    // ===================== HELPERS =====================
    private void ApplyVolume(float v)
    {
        if (!masterMixer) return;
        float dB = Mathf.Lerp(-80f, 0f, Mathf.Clamp01(v)); // 0..1 → -80..0 dB
        masterMixer.SetFloat("MasterVolume", dB);
    }

    private void SetResolutionByIndex(int idx)
    {
        if (_resolutions == null || _resolutions.Length == 0) return;
        idx = Mathf.Clamp(idx, 0, _resolutions.Length - 1);
        var r = _resolutions[idx];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
    }
}
