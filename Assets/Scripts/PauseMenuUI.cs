using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Video;

/// <summary>
/// PauseMenuUI, oyunu durdurma, secenekleri acma ve dunyayi gecici olarak dondurma akisini yonetir.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Panels")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitToMenuButton;
    [SerializeField] private Button quitToDesktopButton;
    [SerializeField] private bool optionsTemporarilyDisabled = true;
    [SerializeField] private bool showQuitToDesktopButton = false;

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

    [Header("Options - Audio")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";

    [Header("Options - Video")]
    [SerializeField] private Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Dropdown resolutionDropdown;

    [Header("Player Controller")]
    public SC_FPSController playerController;
    [Header("Disable these while paused (player controls)")]
    [SerializeField] private MonoBehaviour[] disableWhilePaused;

    [Header("Hide these while paused (optional)")]
    [SerializeField] private GameObject[] hideWhilePaused;

    [Header("Block raycasts while paused (optional)")]
    [SerializeField] private CanvasGroup[] blockWhilePaused;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnResume = true;

    [Header("Hard Freeze (her seyi dondur)")]
    [SerializeField] private bool hardFreezeEverything = true;
    public static bool IsInputLocked = false;
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private readonly Dictionary<Animator, float> _animatorSpeeds = new Dictionary<Animator, float>();
    private readonly List<ParticleSystem> _pausedParticles = new List<ParticleSystem>();
    private readonly List<VideoPlayer> _pausedVideos = new List<VideoPlayer>();
    private readonly List<PlayableDirector> _pausedDirectors = new List<PlayableDirector>();
    private readonly List<NavMeshAgent> _stoppedAgents = new List<NavMeshAgent>();
    private readonly List<AudioSource> _pausedSourcesIgnoringListener = new List<AudioSource>();

    private const string KEY_VOL        = "opt_masterVol";
    private const string KEY_QUALITY    = "opt_quality";
    private const string KEY_FULLSCREEN = "opt_full";
    private const string KEY_VSYNC      = "opt_vsync";
    private const string KEY_RESOLUTION = "opt_res";

    public static bool IsPaused { get; private set; }
    private float _prevTimeScale = 1f;
    private bool _prevCursorVisible;
    private CursorLockMode _prevCursorLock;
    private Resolution[] _resolutions;
    private string _resolvedMasterVolumeParameter;
    private bool _masterVolumeParameterChecked;

    private void Awake()
    {
        if (rootPanel) rootPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (quitToDesktopButton) quitToDesktopButton.gameObject.SetActive(showQuitToDesktopButton);
        IsPaused = false;
    }

    private void Start()
    {
        if (resumeButton)        resumeButton.onClick.AddListener(Resume);
        if (optionsButton)
        {
            optionsButton.interactable = !optionsTemporarilyDisabled;
            if (!optionsTemporarilyDisabled)
                optionsButton.onClick.AddListener(ShowOptions);
        }
        if (quitToMenuButton)    quitToMenuButton.onClick.AddListener(QuitToMenu);
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
        if (Input.GetKeyDown(pauseKey))
        {
            if (optionsPanel && optionsPanel.activeSelf)
                HideOptions();
            else
                TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsInputLocked = true;
        IsPaused = true;
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
            FreezeWorld();

        IsPaused = true;
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsInputLocked = false;
        IsPaused = false;
        if (hardFreezeEverything)
            UnfreezeWorld();

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

    public void OnResumePressed()
    {
        Resume();
    }

    public void OnBackToMenuPressed()
    {
        QuitToMenu();
    }

    public void OnOptionsPressed()
    {
        ShowOptions();
    }

    public void OpenPauseMenu()
    {
        Pause();
    }

    public void ClosePauseMenu()
    {
        Resume();
    }

    public void TogglePauseFromButton()
    {
        TogglePause();
    }

    public void QuitToMenu()
    {
        SaveCoordinator.EnsureInstance()?.SaveGame("quit to menu");

        if (hardFreezeEverything) UnfreezeWorld();

        Time.timeScale = 1f;
        AudioListener.pause = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SetPausedOnComponents(false);
        SetHiddenWhilePaused(false);
        SetBlockedWhilePaused(false);
        IsPaused = false;

        if (string.IsNullOrEmpty(mainMenuSceneName))
            mainMenuSceneName = "Main Menu";

        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

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
        _animatorSpeeds.Clear();
        foreach (var a in FindObjectsOfType<Animator>(true))
        {
            if (!a) continue;
            _animatorSpeeds[a] = a.speed;
            a.speed = 0f;
        }

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

    public void ShowOptions()
    {
        if (rootPanel) rootPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
        ShowOnly(voicePanel);
    }

    public void HideOptions()
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
    private void SetPausedOnComponents(bool paused)
    {
        if (playerController != null)
        {
            playerController.enabled = !paused;
        }
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
#if UNITY_2022_2_OR_NEWER
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
#else
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
#endif
    }

    public void QuitToDesktop()
    {
        SaveCoordinator.EnsureInstance()?.SaveGame("quit to desktop");
        ResumeHard();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
