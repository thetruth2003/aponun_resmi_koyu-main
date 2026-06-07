using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// CutsceneSubtitlePlayer, tek ses klibini oynatir ve belirtilen zamanlarda altyazilari ekrana basar.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class CutsceneSubtitlePlayer : MonoBehaviour
{
    [Header("Track & UI")]
    public CutsceneSubtitleTrack track;
    public TextMeshProUGUI subtitleText;
    public GameObject subtitlePanel;

    [Header("Playback")]
    public bool playOnAwake = false;
    public bool hidePanelWhenIdle = true;
    public bool finishAttachedCutsceneClipOnEnd = false;
    [Min(0f)] public float startDelay = 0f;
    public UnityEvent onFinished;

    Coroutine playCo;
    AudioSource audioSource;
    bool isPlaying;

    public bool IsPlaying => isPlaying;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        TryAutoAssignUI();
        ApplyIdleVisualState();

        if (playOnAwake)
        {
            Play();
        }
    }

    void OnDisable()
    {
        StopPlayback(invokeCallbacks: false);
    }

    void TryAutoAssignUI()
    {
        if (subtitleText == null)
        {
            var tagObj = GameObject.FindGameObjectWithTag("DialogText");
            if (tagObj) subtitleText = tagObj.GetComponent<TextMeshProUGUI>();
        }

        if (subtitlePanel == null && subtitleText != null)
        {
            subtitlePanel = subtitleText.transform.parent ? subtitleText.transform.parent.gameObject : null;
        }
    }

    public void Play()
    {
        Play(track);
    }

    public void Play(CutsceneSubtitleTrack overrideTrack)
    {
        if (overrideTrack == null)
        {
            Debug.LogWarning("[CutsceneSubtitlePlayer] Track atanmadigi icin oynatma baslatilamadi.", this);
            return;
        }

        if (playCo != null) StopCoroutine(playCo);
        playCo = StartCoroutine(CoPlay(overrideTrack));
    }

    public void StopPlayback()
    {
        StopPlayback(invokeCallbacks: false);
    }

    void StopPlayback(bool invokeCallbacks)
    {
        if (playCo != null)
        {
            StopCoroutine(playCo);
            playCo = null;
        }

        if (audioSource && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isPlaying = false;
        ApplyIdleVisualState();

        if (invokeCallbacks)
        {
            onFinished?.Invoke();

            if (finishAttachedCutsceneClipOnEnd)
            {
                GetComponent<CutsceneClip>()?.Finish();
            }
        }
    }

    IEnumerator CoPlay(CutsceneSubtitleTrack activeTrack)
    {
        isPlaying = true;

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        activeTrack.SortCues();

        if (subtitlePanel) subtitlePanel.SetActive(true);
        if (subtitleText)
        {
            subtitleText.gameObject.SetActive(true);
            subtitleText.text = string.Empty;
        }

        float elapsed = 0f;
        int nextCueIndex = 0;
        float duration = Mathf.Max(0f, activeTrack.GetDuration());

        if (audioSource)
        {
            audioSource.Stop();
            audioSource.clip = activeTrack.voiceClip;
            if (activeTrack.voiceClip) audioSource.Play();
        }

        while (true)
        {
            if (audioSource && activeTrack.voiceClip)
            {
                elapsed = audioSource.isPlaying ? audioSource.time : Mathf.Max(elapsed, activeTrack.voiceClip.length);
            }
            else
            {
                elapsed += Time.deltaTime;
            }

            while (nextCueIndex < activeTrack.cues.Count && elapsed >= activeTrack.cues[nextCueIndex].time)
            {
                ApplyCue(activeTrack.cues[nextCueIndex]);
                nextCueIndex++;
            }

            bool audioFinished = activeTrack.voiceClip == null || audioSource == null || !audioSource.isPlaying;
            if (elapsed >= duration && audioFinished)
            {
                break;
            }

            yield return null;
        }

        playCo = null;
        StopPlayback(invokeCallbacks: true);
    }

    void ApplyCue(TimedSubtitleCue cue)
    {
        if (subtitleText == null) return;
        subtitleText.text = cue != null ? cue.text ?? string.Empty : string.Empty;
    }

    void ApplyIdleVisualState()
    {
        if (subtitleText) subtitleText.text = string.Empty;

        if (!hidePanelWhenIdle) return;

        if (subtitlePanel) subtitlePanel.SetActive(false);
        if (subtitleText) subtitleText.gameObject.SetActive(false);
    }
}
