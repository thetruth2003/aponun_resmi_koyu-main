using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// CutsceneMover sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public class CutsceneMover : MonoBehaviour
{
    [Header("General")]
    public bool playOnAwake = true;
    public bool loop = false;
    public Transform forwardReference;
    public Animator animator;
    public CanvasGroup fadeCanvasGroup;
    public UnityEvent OnSequenceFinished;

    [Header("Driver")]
    public DriverType driver = DriverType.Transform;
    public CharacterController character;
    public Rigidbody rb;

    [Header("Steps (sirayla oynar)")]
    public List<Step> steps = new();

    Coroutine seqCo;
    bool playing;
    int currentStepIndex = -1;

    public bool IsPlaying => playing;
    public int CurrentStepIndex => currentStepIndex;

    void Awake()
    {
        if (playOnAwake) Play();
    }

    public void Play()
    {
        StartSequence(0, steps.Count, loop, true);
    }

    public void PlayFromStep(int startIndex)
    {
        if (steps == null || steps.Count == 0) return;
        startIndex = Mathf.Clamp(startIndex, 0, steps.Count - 1);
        StartSequence(startIndex, steps.Count, false, false);
    }

    public void PlaySingleStep(int stepIndex)
    {
        if (steps == null || steps.Count == 0) return;
        stepIndex = Mathf.Clamp(stepIndex, 0, steps.Count - 1);
        StartSequence(stepIndex, stepIndex + 1, false, false);
    }

    public void Stop()
    {
        if (seqCo != null) StopCoroutine(seqCo);
        seqCo = null;
        playing = false;
        currentStepIndex = -1;
    }

    void StartSequence(int startIndex, int endExclusive, bool shouldLoop, bool invokeFinishedEvent)
    {
        if (playing) Stop();
        seqCo = StartCoroutine(RunSequence(startIndex, endExclusive, shouldLoop, invokeFinishedEvent));
    }

    IEnumerator RunSequence(int startIndex, int endExclusive, bool shouldLoop, bool invokeFinishedEvent)
    {
        playing = true;
        currentStepIndex = -1;

        do
        {
            int safeStart = Mathf.Clamp(startIndex, 0, steps.Count);
            int safeEnd = Mathf.Clamp(endExclusive, 0, steps.Count);

            for (int i = safeStart; i < safeEnd; i++)
            {
                var s = steps[i];
                if (s == null || s.skip) continue;

                currentStepIndex = i;
                yield return RunConfiguredStep(s);
            }

            if (!shouldLoop) break;

            startIndex = 0;
            endExclusive = steps.Count;

        } while (shouldLoop);

        seqCo = null;
        playing = false;
        currentStepIndex = -1;
        if (invokeFinishedEvent)
        {
            OnSequenceFinished?.Invoke();
        }
    }

    IEnumerator RunConfiguredStep(Step s)
    {
        if (!s.waitForCompletion)
        {
            StartCoroutine(RunDelayedStepBody(s));
            yield break;
        }

        yield return RunDelayedStepBody(s);
    }

    IEnumerator RunDelayedStepBody(Step s)
    {
        if (s.startDelay > 0f)
        {
            yield return new WaitForSeconds(s.startDelay);
        }

        yield return RunStepBody(s);
    }

    IEnumerator RunStepBody(Step s)
    {
        switch (s.type)
        {
            case StepType.PlayAnimation:
                yield return RunAnimStep(s);
                break;
            case StepType.Wait:
                yield return new WaitForSeconds(Mathf.Max(0f, s.waitSeconds));
                break;
            case StepType.Move:
                yield return RunMoveStep(s);
                break;
            case StepType.Rotate:
                yield return RunRotateStep(s);
                break;
            case StepType.Fade:
                yield return RunFadeStep(s);
                break;
            case StepType.Teleport:
                yield return RunTeleportStep(s);
                break;
            case StepType.Attach:
                yield return RunAttachStep(s);
                break;
            case StepType.InvokeEvent:
                s.onInvoke?.Invoke();
                break;
        }
    }

    IEnumerator RunAnimStep(Step s)
    {
        if (!animator) { if (s.waitSeconds > 0) yield return new WaitForSeconds(s.waitSeconds); yield break; }

        switch (s.animSetType)
        {
            case AnimSetType.Trigger:
                if (!string.IsNullOrEmpty(s.animParam)) animator.SetTrigger(s.animParam);
                break;
            case AnimSetType.Bool:
                if (!string.IsNullOrEmpty(s.animParam)) animator.SetBool(s.animParam, s.boolValue);
                break;
            case AnimSetType.Float:
                if (!string.IsNullOrEmpty(s.animParam)) animator.SetFloat(s.animParam, s.floatValue);
                break;
            case AnimSetType.Int:
                if (!string.IsNullOrEmpty(s.animParam)) animator.SetInteger(s.animParam, s.intValue);
                break;
        }

        if (s.waitSeconds > 0) yield return new WaitForSeconds(s.waitSeconds);
    }

    IEnumerator RunMoveStep(Step s)
    {
        Vector3 dir = GetDirectionVector(s.direction, s.customDirection).normalized;
        if (dir.sqrMagnitude < 0.0001f) yield break;

        var refTr = forwardReference ? forwardReference : transform;
        dir = refTr.TransformDirection(dir);

        if (s.faceDirection)
        {
            var targetRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z).normalized, Vector3.up);
            float rotT = 0f;
            while (rotT < 1f && s.turnSpeed > 0f)
            {
                rotT += Time.deltaTime * s.turnSpeed;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(rotT));
                yield return null;
            }
            transform.rotation = targetRot;
        }

        if (s.moveMode == MoveMode.ByDistance)
        {
            float distance = Mathf.Max(0f, s.distance);
            float moved = 0f;
            Vector3 start = transform.position;

            while (moved < distance)
            {
                float step = Mathf.Max(0f, s.speed) * Time.deltaTime;
                float t = Mathf.Clamp01(moved / Mathf.Max(0.0001f, distance));
                step *= EaseFactor(s.easing, t);

                MoveDriver(dir * step);
                moved = Vector3.Distance(start, transform.position);
                yield return null;
            }
        }
        else
        {
            float dur = Mathf.Max(0.0001f, s.duration);
            float t = 0f;
            while (t < dur)
            {
                float ratio = Mathf.Clamp01(t / dur);
                float step = Mathf.Max(0f, s.speed) * Time.deltaTime * EaseFactor(s.easing, ratio);
                MoveDriver(dir * step);
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    IEnumerator RunRotateStep(Step s)
    {
        Quaternion target;
        if (s.rotateMode == RotateMode.LookAtTarget && s.lookTarget)
        {
            Vector3 lookDir = (s.lookTarget.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude < 0.0001f) yield break;
            target = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }
        else
        {
            target = Quaternion.Euler(s.worldEuler);
        }

        float dur = Mathf.Max(0.0001f, s.duration);
        Quaternion start = transform.rotation;
        float t = 0f;

        while (t < dur)
        {
            float k = Mathf.Clamp01(t / dur);
            k = Ease01(s.easing, k);
            transform.rotation = Quaternion.Slerp(start, target, k);
            t += Time.deltaTime;
            yield return null;
        }

        transform.rotation = target;
    }

    IEnumerator RunFadeStep(Step s)
    {
        if (!fadeCanvasGroup) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float targetAlpha = s.fadeMode == FadeMode.FadeOutToBlack ? 1f : 0f;
        float dur = Mathf.Max(0.0001f, s.duration);
        float t = 0f;

        while (t < dur)
        {
            float k = Mathf.Clamp01(t / dur);
            k = Ease01(s.easing, k);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, k);
            t += Time.deltaTime;
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.01f;
        fadeCanvasGroup.interactable = false;
    }

    IEnumerator RunTeleportStep(Step s)
    {
        TeleportDriver(s.teleportWorldPosition);
        yield break;
    }

    IEnumerator RunAttachStep(Step s)
    {
        if (!s.attachObject) yield break;

        switch (s.attachMode)
        {
            case AttachMode.DetachToWorld:
                s.attachObject.SetParent(null, s.keepWorldTransform);
                break;

            case AttachMode.AttachToTarget:
                if (!s.attachTarget) yield break;

                s.attachObject.SetParent(s.attachTarget, s.keepWorldTransform);

                if (!s.keepWorldTransform)
                {
                    s.attachObject.localPosition = s.localPositionOffset;
                    s.attachObject.localRotation = Quaternion.Euler(s.localEulerOffset);
                }
                break;
        }

        yield break;
    }

    void TeleportDriver(Vector3 worldPosition)
    {
        switch (driver)
        {
            case DriverType.Transform:
                transform.position = worldPosition;
                break;

            case DriverType.CharacterController:
                if (character)
                {
                    bool wasEnabled = character.enabled;
                    if (wasEnabled) character.enabled = false;
                    transform.position = worldPosition;
                    if (wasEnabled) character.enabled = true;
                }
                else
                {
                    transform.position = worldPosition;
                }
                break;

            case DriverType.Rigidbody:
                if (rb)
                {
                    rb.position = worldPosition;
                    rb.transform.position = worldPosition;
                }
                else
                {
                    transform.position = worldPosition;
                }
                break;
        }
    }

    void MoveDriver(Vector3 delta)
    {
        switch (driver)
        {
            case DriverType.Transform:
                transform.position += delta;
                break;
            case DriverType.CharacterController:
                if (character) character.Move(delta);
                else transform.position += delta;
                break;
            case DriverType.Rigidbody:
                if (rb) rb.MovePosition(rb.position + delta);
                else transform.position += delta;
                break;
        }
    }

    static Vector3 GetDirectionVector(Direction8 dir, Vector3 custom)
    {
        switch (dir)
        {
            case Direction8.Forward:      return new Vector3(0, 0, 1);
            case Direction8.Back:         return new Vector3(0, 0, -1);
            case Direction8.Left:         return new Vector3(-1, 0, 0);
            case Direction8.Right:        return new Vector3(1, 0, 0);
            case Direction8.ForwardLeft:  return new Vector3(-1, 0, 1);
            case Direction8.ForwardRight: return new Vector3(1, 0, 1);
            case Direction8.BackLeft:     return new Vector3(-1, 0, -1);
            case Direction8.BackRight:    return new Vector3(1, 0, -1);
            case Direction8.Custom:       return custom;
        }
        return Vector3.zero;
    }

    static float EaseFactor(Easing e, float t) => Mathf.Max(0.0001f, Ease01(e, Mathf.Clamp01(t)));
    static float Ease01(Easing e, float t)
    {
        switch (e)
        {
            case Easing.EaseIn:     return t * t;
            case Easing.EaseOut:    return 1f - (1f - t) * (1f - t);
            case Easing.EaseInOut:  return t < 0.5f ? 2f*t*t : 1f - Mathf.Pow(-2f*t + 2f, 2f)/2f;
            default:                return t;
        }
    }
}

/// <summary>
/// DriverType sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public enum DriverType { Transform, CharacterController, Rigidbody }

/// <summary>
/// StepType sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public enum StepType { Move, PlayAnimation, Wait, Rotate, Fade, Teleport, Attach, InvokeEvent }

/// <summary>
/// MoveMode sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public enum MoveMode { ByDistance, ByDuration }

/// <summary>
/// Direction8 sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public enum Direction8
{
    Forward, Back, Left, Right,
    ForwardLeft, ForwardRight, BackLeft, BackRight,
    Custom
}

/// <summary>
/// Easing sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public enum Easing { Linear, EaseIn, EaseOut, EaseInOut }

/// <summary>
/// AnimSetType sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public enum AnimSetType { Trigger, Bool, Float, Int }

/// <summary>
/// RotateMode sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public enum RotateMode { WorldEuler, LookAtTarget }

/// <summary>
/// FadeMode sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public enum FadeMode { FadeOutToBlack, FadeInFromBlack }

/// <summary>
/// AttachMode sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public enum AttachMode { AttachToTarget, DetachToWorld }

/// <summary>
/// Step sinifi, gorev sistemindeki ilgili adimi temsil eder.
/// </summary>
[Serializable]
public class Step
{
    public StepType type = StepType.Move;
    public bool skip = false;
    [Tooltip("Kapaliysa bu step arka planda baslar ve siradaki step hemen calisir. Fade'i yurume sirasinda baslatmak icin kullanisli.")]
    public bool waitForCompletion = true;
    [Tooltip("Bu step'in ne kadar gecikmeyle baslayacagi. Async fade'i yuruyusun ortasina sokmak icin faydali.")]
    public float startDelay = 0f;
    public string title;
    [TextArea(2, 5)] public string note;

    [Tooltip("Bu alan su an runtime tarafinda kullanilmiyor. Editor notu olarak kalabilir.")]
    public bool playAnimAtStart = false;
    public AnimSetType animSetType = AnimSetType.Trigger;
    public string animParam;
    public bool boolValue;
    public float floatValue;
    public int intValue;
    [Tooltip("Wait step icin bekleme suresi. Animation step icin anim verdikten sonra beklenecek sure.")]
    public float waitSeconds = 0f;

    public MoveMode moveMode = MoveMode.ByDistance;
    public Direction8 direction = Direction8.Forward;
    public Vector3 customDirection = Vector3.forward;
    [Tooltip("ByDistance modunda kullanilir. Birim metredir.")]
    public float distance = 2f;
    [Tooltip("ByDuration veya Rotate stepinde kullanilir. Birim saniyedir.")]
    public float duration = 1f;
    [Tooltip("m/s")]
    public float speed = 1.5f;
    public Easing easing = Easing.Linear;
    public bool faceDirection = true;
    [Tooltip("Move stepinde yuzunu hedef yone ne kadar hizli cevirecegi. 0 ise aninda doner.")]
    public float turnSpeed = 8f;

    public RotateMode rotateMode = RotateMode.WorldEuler;
    public Vector3 worldEuler = Vector3.zero;
    public Transform lookTarget;

    public FadeMode fadeMode = FadeMode.FadeOutToBlack;

    [Tooltip("Teleport step geldigi anda objeyi direkt bu world pozisyonuna tasir.")]
    public Vector3 teleportWorldPosition = Vector3.zero;

    public AttachMode attachMode = AttachMode.AttachToTarget;
    public Transform attachObject;
    public Transform attachTarget;
    [Tooltip("True ise parent degisirken dunyadaki poz korunur. False ise target local pose'una snap olur.")]
    public bool keepWorldTransform = false;
    [Tooltip("Keep World Transform kapaliysa attach sonrasi local position olarak uygulanir.")]
    public Vector3 localPositionOffset = Vector3.zero;
    [Tooltip("Keep World Transform kapaliysa attach sonrasi local rotation olarak uygulanir.")]
    public Vector3 localEulerOffset = Vector3.zero;

    public UnityEvent onInvoke;
}

