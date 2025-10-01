using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Video;

public class PauseMenuUI : MonoBehaviour
{
    // ========= PANELS =========
    [Header("Panels")]
    [SerializeField] private GameObject rootPanel;     // Pause ana panel (Resume/Options/Quit)
    [SerializeField] private GameObject optionsPanel;  // Options ana paneli

    // ========= MAIN BUTTONS =========
    [Header("Main Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitToMenuButton;     // Main Menu sahnesine dön
    [SerializeField] private Button quitToDesktopButton;  // Masaüstüne çık

    // ========= OPTIONS: TABS =========
    [Header("Options Tabs")]
    [SerializeField] private GameObject voicePanel;
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject gamePanel;

    [SerializeField] private Button voiceTabButton;
    [SerializeField] private Button videoTabButton;
    [SerializeField] private Button controlsTabButton;
    [SerializeField] private Button gameTabButton;
    [SerializeField] private Button backButton;

    // ========= OPTIONS: WIDGETS =========
    [Header("Options - Audio")]
    [SerializeField] private AudioMixer masterMixer;      // Exposed param: "MasterVolume"
    [SerializeField] private Slider masterVolumeSlider;   // 0..1

    [Header("Options - Video")]
    [SerializeField] private Dropdown qualityDropdown;    // QualitySettings.names
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Dropdown resolutionDropdown; // opsiyonel

    // ========= INPUT / UI KİLİDİ =========
    [Header("Disable these while paused (player controls)")]
    [SerializeField] private MonoBehaviour[] disableWhilePaused; // örn: SC_FPSController, StateManger, vb.

    [Header("Hide these while paused (optional)")]
    [SerializeField] private GameObject[] hideWhilePaused; // HUD, Crosshair, Inventory gibi kök objeler

    [Header("Block raycasts while paused (optional)")]
    [SerializeField] private CanvasGroup[] blockWhilePaused; // Görünsün ama tıklanmasın istediklerin

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnResume = true;

    // ========= HARD FREEZE =========
    [Header("Hard Freeze (her şeyi dondur)")]
    [SerializeField] private bool hardFreezeEverything = true;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // Donmuş bileşen cache’leri
    private readonly Dictionary<Animator, float> _animatorSpeeds = new Dictionary<Animator, float>();
    private readonly List<ParticleSystem> _pausedParticles = new List<ParticleSystem>();
    private readonly List<VideoPlayer> _pausedVideos = new List<VideoPlayer>();
    private readonly List<PlayableDirector> _pausedDirectors = new List<PlayableDirector>();
    private readonly List<NavMeshAgent> _stoppedAgents = new List<NavMeshAgent>();
    private readonly List<AudioSource> _pausedSourcesIgnoringListener = new List<AudioSource>();

    // ========= PREF KEYS (MenuUI ile aynı) =========
    private const string KEY_VOL        = "opt_masterVol";
    private const string KEY_QUALITY    = "opt_quality";
    private const string KEY_FULLSCREEN = "opt_full";
    private const string KEY_VSYNC      = "opt_vsync";
    private const string KEY_RESOLUTION = "opt_res";

    // ========= STATE =========
    public static bool IsPaused { get; private set; }
    private float _prevTimeScale = 1f;
    private bool _prevCursorVisible;
    private CursorLockMode _prevCursorLock;
    private Resolution[] _resolutions;

    private void Awake()
    {
        if (rootPanel) rootPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);
        IsPaused = false;
    }

    private void Start()
    {
        if (resumeButton)        resumeButton.onClick.AddListener(Resume);
        if (optionsButton)       optionsButton.onClick.AddListener(ShowOptions);
        if (quitToMenuButton)    quitToMenuButton.onClick.AddListener(QuitToMenu); // önemli: cursor açık kalır
        if (quitToDesktopButton) quitToDesktopButton.onClick.AddListener(QuitToDesktop);
        if (backButton)          backButton.onClick.AddListener(HideOptions);

        if (voiceTabButton)    voiceTabButton.onClick.AddListener(() => ShowOnly(voicePanel));
        if (videoTabButton)    videoTabButton.onClick.AddListener(() => ShowOnly(videoPanel));
        if (controlsTabButton) controlsTabButton.onClick.AddListener(() => ShowOnly(controlsPanel));
        if (gameTabButton)     gameTabButton.onClick.AddListener(() => ShowOnly(gamePanel));

        InitQualityDropdown();
        InitResolutionDropdown();
        LoadAndApplyOptions();

        if (masterVolumeSlider) masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (qualityDropdown)    qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (fullscreenToggle)   fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (vSyncToggle)        vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionsPanel && optionsPanel.activeSelf)
                HideOptions();
            else
                TogglePause();
        }
    }

    // ========= PAUSE / RESUME =========
    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;

        if (rootPanel) rootPanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);

        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        _prevCursorVisible = Cursor.visible;
        _prevCursorLock = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SetPausedOnComponents(true);
        SetHiddenWhilePaused(true);
        SetBlockedWhilePaused(true);

        if (hardFreezeEverything)
            FreezeWorld(); // <<< HER ŞEYİ DONDUR

        IsPaused = true;
    }

    public void Resume()
    {
        if (!IsPaused) return;

        if (hardFreezeEverything)
            UnfreezeWorld(); // <<< HER ŞEYİ ESKİ HALİNE GETİR

        if (optionsPanel) optionsPanel.SetActive(false);
        if (rootPanel) rootPanel.SetActive(false);

        Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
        AudioListener.pause = false;

        if (lockCursorOnResume)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = _prevCursorVisible;
            Cursor.lockState = _prevCursorLock;
        }

        SetPausedOnComponents(false);
        SetHiddenWhilePaused(false);
        SetBlockedWhilePaused(false);

        IsPaused = false;
    }

    // Menüyü yüklerken cursor'ı açık/serbest bırak
    private void QuitToMenu()
    {
        if (hardFreezeEverything) UnfreezeWorld();

        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Cursor'u ZORLA AÇ
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SetPausedOnComponents(false);
        SetHiddenWhilePaused(false);
        SetBlockedWhilePaused(false);
        IsPaused = false;

        if (string.IsNullOrEmpty(mainMenuSceneName))
            mainMenuSceneName = "MainMenu";

        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    // Masaüstüne çıkarken toparlayan versiyon (cursor'u da açıyoruz)
    private void ResumeHard()
    {
        if (hardFreezeEverything) UnfreezeWorld();

        if (optionsPanel) optionsPanel.SetActive(false);
        if (rootPanel) rootPanel.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SetPausedOnComponents(false);
        SetHiddenWhilePaused(false);
        SetBlockedWhilePaused(false);

        IsPaused = false;
    }

    private void SetPausedOnComponents(bool paused)
    {
        if (disableWhilePaused == null) return;
        foreach (var mb in disableWhilePaused)
        {
            if (!mb) continue;
            mb.enabled = !paused;
            // if (mb is SC_FPSController fps) fps.canMove = !paused;
        }
    }

    private void SetHiddenWhilePaused(bool hidden)
    {
        if (hideWhilePaused == null) return;
        foreach (var go in hideWhilePaused)
            if (go) go.SetActive(!hidden);
    }

    private void SetBlockedWhilePaused(bool blocked)
    {
        if (blockWhilePaused == null) return;
        foreach (var cg in blockWhilePaused)
        {
            if (!cg) continue;
            cg.interactable   = !blocked;
            cg.blocksRaycasts = !blocked;
        }
    }

    // ========= HARD FREEZE IMPLEMENTASYONU =========

    // Yalnızca sahnede aktif, enabled ve NavMesh üzerinde olan ajanlara dokun
    private bool AgentUsable(NavMeshAgent ag)
    {
        if (ag == null) return false;
        if (!ag.isActiveAndEnabled || !ag.gameObject.activeInHierarchy) return false;
        if (!ag.enabled) return false;
        bool onMesh;
        try { onMesh = ag.isOnNavMesh; }
        catch { onMesh = false; }
        return onMesh;
    }

    private void FreezeWorld()
    {
        // Animator (UnscaledTime bile dursun)
        _animatorSpeeds.Clear();
        foreach (var a in FindObjectsOfType<Animator>(true))
        {
            if (!a) continue;
            _animatorSpeeds[a] = a.speed;
            a.speed = 0f;
        }

        // ParticleSystem
        _pausedParticles.Clear();
        foreach (var ps in FindObjectsOfType<ParticleSystem>(true))
        {
            if (!ps) continue;
            if (ps.isPlaying)
            {
                _pausedParticles.Add(ps);
                ps.Pause(true);
            }
        }

        // VideoPlayer
        _pausedVideos.Clear();
        foreach (var vp in FindObjectsOfType<VideoPlayer>(true))
        {
            if (!vp) continue;
            if (vp.isPlaying)
            {
                _pausedVideos.Add(vp);
                vp.Pause();
            }
        }

        // Timeline (PlayableDirector)
        _pausedDirectors.Clear();
        foreach (var dir in FindObjectsOfType<PlayableDirector>(true))
        {
            if (!dir) continue;
            if (dir.state == PlayState.Playing)
            {
                _pausedDirectors.Add(dir);
                dir.Pause();
            }
        }

        // NavMeshAgent (sadece usable olanları durdur)
        _stoppedAgents.Clear();
        foreach (var ag in FindObjectsOfType<NavMeshAgent>(true))
        {
            if (!AgentUsable(ag)) continue;
            if (!ag.isStopped)
            {
                ag.isStopped = true;
                _stoppedAgents.Add(ag);
            }
        }

        // AudioSource: Listener pause'u dinlemeyenler
        _pausedSourcesIgnoringListener.Clear();
        foreach (var src in FindObjectsOfType<AudioSource>(true))
        {
            if (!src) continue;
            if (src.ignoreListenerPause && src.isPlaying)
            {
                _pausedSourcesIgnoringListener.Add(src);
                src.Pause();
            }
        }
    }

    private void UnfreezeWorld()
    {
        foreach (var kv in _animatorSpeeds)
            if (kv.Key) kv.Key.speed = kv.Value;
        _animatorSpeeds.Clear();

        foreach (var ps in _pausedParticles)
            if (ps) ps.Play(true);
        _pausedParticles.Clear();

        foreach (var vp in _pausedVideos)
            if (vp) vp.Play();
        _pausedVideos.Clear();

        foreach (var dir in _pausedDirectors)
            if (dir) dir.Play();
        _pausedDirectors.Clear();

        foreach (var ag in _stoppedAgents)
        {
            if (!AgentUsable(ag)) continue;
            ag.isStopped = false;
        }
        _stoppedAgents.Clear();

        foreach (var src in _pausedSourcesIgnoringListener)
            if (src) src.UnPause();
        _pausedSourcesIgnoringListener.Clear();
    }

    // ========= OPTIONS UI =========
    private void ShowOptions()
    {
        if (rootPanel) rootPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
        ShowOnly(voicePanel);
    }

    private void HideOptions()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
        if (rootPanel) rootPanel.SetActive(true);
    }

    private void ShowOnly(GameObject target)
    {
        if (voicePanel)    voicePanel.SetActive(false);
        if (videoPanel)    videoPanel.SetActive(false);
        if (controlsPanel) controlsPanel.SetActive(false);
        if (gamePanel)     gamePanel.SetActive(false);
        if (target) target.SetActive(true);
    }

    // ========= OPTIONS: INIT/APPLY =========
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
        var options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
#if UNITY_2022_2_OR_NEWER
            string hz = $"{_resolutions[i].refreshRateRatio.value:0}";
#else
            string hz = $"{_resolutions[i].refreshRate}";
#endif
            options.Add($"{_resolutions[i].width} x {_resolutions[i].height} @ {hz}Hz");

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

    // ========= OPTIONS: LISTENERS =========
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

    // ========= HELPERS =========
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
#if UNITY_2022_2_OR_NEWER
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
#else
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
#endif
    }

    private void QuitToDesktop()
    {
        ResumeHard(); // timeScale/Audio/cursor/inputs toparla
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
