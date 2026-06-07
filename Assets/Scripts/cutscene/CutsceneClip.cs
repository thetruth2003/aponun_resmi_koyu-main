using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// CutsceneClip sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public class CutsceneClip : MonoBehaviour
{
    public static event Action<CutsceneClip> AnyClipFinished;

    [Header("Kimlik")]
    public string id;

    [Header("Oynatma Event'leri")]
    public bool deactivateSelfOnFinish = true;
    [Min(0f)] public float deactivateDelayAfterFinish = 0f;
    public UnityEvent onPlay;
    public UnityEvent onSkip;

    [Header("Activation Fade (Optional)")]
    public bool revealScreenWhenActivated = false;
    public GameObject activationFadePanel;
    [Min(0f)] public float activationRevealDuration = 0.35f;

    [NonSerialized] public string triggerKey;
    [NonSerialized] public string groupKey;
    [NonSerialized] public CutscenePlayType playType = CutscenePlayType.Once;
    [NonSerialized] public int priority = 0;
    [NonSerialized] public int sequenceIndex = -1;

    private CutsceneManager manager;
    private Coroutine activationRevealCoroutine;

    public void EnsureStableId()
    {
        if (!string.IsNullOrWhiteSpace(id))
            return;

        string scenePath = gameObject.scene.path;
        if (string.IsNullOrWhiteSpace(scenePath))
            scenePath = gameObject.scene.name;

        string hierarchyPath = BuildHierarchyPath(transform);
        id = "auto_" + Hash128.Compute(scenePath + "::" + hierarchyPath).ToString();
    }

    private void Awake()
    {
        manager = GetComponentInParent<CutsceneManager>();
        if (!manager) manager = FindObjectOfType<CutsceneManager>();

        EnsureStableId();
    }

    private void OnEnable()
    {
        if (!revealScreenWhenActivated)
            return;

        if (activationRevealCoroutine != null)
            StopCoroutine(activationRevealCoroutine);

        activationRevealCoroutine = StartCoroutine(CoRevealScreenOnActivate());
    }

    private void OnDisable()
    {
        if (activationRevealCoroutine != null)
        {
            StopCoroutine(activationRevealCoroutine);
            activationRevealCoroutine = null;
        }
    }

    private void OnValidate()
    {
        EnsureStableId();
    }

    public void Play()
    {
        gameObject.SetActive(true);
        onPlay?.Invoke();
    }

    public void Skip()
    {
        onSkip?.Invoke();
        DeactivateIfNeeded();
    }

    public void Finish()
    {
        manager?.OnClipFinished(this);
        AnyClipFinished?.Invoke(this);
        DeactivateIfNeeded();
    }

    private void DeactivateIfNeeded()
    {
        if (!deactivateSelfOnFinish)
            return;

        if (deactivateDelayAfterFinish <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        StartCoroutine(CoDeactivateAfterDelay());
    }

    private IEnumerator CoDeactivateAfterDelay()
    {
        yield return new WaitForSecondsRealtime(deactivateDelayAfterFinish);

        if (this && gameObject)
            gameObject.SetActive(false);
    }

    private IEnumerator CoRevealScreenOnActivate()
    {
        if (!TryResolveActivationFade(out GameObject fadePanel, out CanvasGroup fadeCg, out Image fadeImg))
            yield break;

        fadePanel.SetActive(true);

        float startAlpha = 1f;
        if (fadeCg != null)
            startAlpha = fadeCg.alpha;
        else if (fadeImg != null)
            startAlpha = fadeImg.color.a;

        if (startAlpha <= 0.001f)
            startAlpha = 1f;

        SetFadeAlpha(fadeCg, fadeImg, startAlpha);

        if (activationRevealDuration <= 0f)
        {
            SetFadeAlpha(fadeCg, fadeImg, 0f);
            fadePanel.SetActive(false);
            yield break;
        }

        float t = 0f;
        while (t < activationRevealDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / activationRevealDuration);
            SetFadeAlpha(fadeCg, fadeImg, Mathf.Lerp(startAlpha, 0f, k));
            yield return null;
        }

        SetFadeAlpha(fadeCg, fadeImg, 0f);
        fadePanel.SetActive(false);
        activationRevealCoroutine = null;
    }

    private bool TryResolveActivationFade(out GameObject fadePanel, out CanvasGroup fadeCg, out Image fadeImg)
    {
        fadePanel = activationFadePanel;
        fadeCg = null;
        fadeImg = null;

        if (!fadePanel)
        {
            var cutsceneDialog = GetComponent<CutsceneDialog>();
            if (cutsceneDialog != null)
                fadePanel = cutsceneDialog.fadePanel;
        }

        if (!fadePanel)
            return false;

        fadeCg = fadePanel.GetComponent<CanvasGroup>();
        fadeImg = fadePanel.GetComponent<Image>();

        if (!fadeCg && !fadeImg)
            fadeCg = fadePanel.AddComponent<CanvasGroup>();

        return true;
    }

    private static void SetFadeAlpha(CanvasGroup fadeCg, Image fadeImg, float alpha)
    {
        if (fadeCg != null)
        {
            fadeCg.alpha = alpha;
            return;
        }

        if (fadeImg != null)
        {
            var color = fadeImg.color;
            color.a = alpha;
            fadeImg.color = color;
        }
    }

    private static string BuildHierarchyPath(Transform current)
    {
        if (!current)
            return string.Empty;

        string path = current.name + "#" + current.GetSiblingIndex();
        Transform cursor = current.parent;

        while (cursor != null)
        {
            path = cursor.name + "#" + cursor.GetSiblingIndex() + "/" + path;
            cursor = cursor.parent;
        }

        return path;
    }
}
