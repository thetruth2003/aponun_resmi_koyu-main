using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// CutsceneClip sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public class CutsceneClip : MonoBehaviour
{
    [Header("Kimlik")]
    public string id;

    [Header("Oynatma Event'leri")]
    public bool deactivateSelfOnFinish = true;
    public UnityEvent onPlay;
    public UnityEvent onSkip;

    [NonSerialized] public string triggerKey;
    [NonSerialized] public string groupKey;
    [NonSerialized] public CutscenePlayType playType = CutscenePlayType.Once;
    [NonSerialized] public int priority = 0;
    [NonSerialized] public int sequenceIndex = -1;

    private CutsceneManager manager;

    private void Awake()
    {
        manager = GetComponentInParent<CutsceneManager>();
        if (!manager) manager = FindObjectOfType<CutsceneManager>();

        if (string.IsNullOrWhiteSpace(id))
            id = Guid.NewGuid().ToString("N");
    }

    public void Play()
    {
        gameObject.SetActive(true);
        onPlay?.Invoke();
    }

    public void Skip()
    {
        onSkip?.Invoke();
        if (deactivateSelfOnFinish) gameObject.SetActive(false);
    }

    public void Finish()
    {
        manager?.OnClipFinished(this);
        if (deactivateSelfOnFinish) gameObject.SetActive(false);
    }
}
