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
        Vector3 startPosition = transform.position;
        bool useCustomWorldTarget = s.direction == Direction8.Custom && s.customMoveKind == CustomMoveKind.WorldTarget;
        bool wantsCustomTargetTransform = s.direction == Direction8.Custom && s.customMoveKind == CustomMoveKind.TargetTransform;
        bool useCustomTargetTransform = wantsCustomTargetTransform && s.customTargetTransform;

        if (wantsCustomTargetTransform && !s.customTargetTransform)
        {
            yield break;
        }

        Vector3 dir;
        Vector3 finalTargetPosition = startPosition;

        if (useCustomWorldTarget)
        {
            Vector3 targetDelta = s.customWorldTarget - startPosition;
            float targetDistance = targetDelta.magnitude;
            if (targetDistance < 0.0001f) yield break;

            dir = targetDelta / targetDistance;
            finalTargetPosition = s.customWorldTarget;
        }
        else if (useCustomTargetTransform)
        {
            finalTargetPosition = GetApproachTargetPosition(startPosition, s.customTargetTransform.position, s.customTargetStopDistance);
            Vector3 targetDelta = finalTargetPosition - startPosition;
            float targetDistance = targetDelta.magnitude;
            if (targetDistance < 0.0001f) yield break;

            dir = targetDelta / targetDistance;
        }
        else
        {
            dir = GetDirectionVector(s.direction, s.customDirection).normalized;
            if (dir.sqrMagnitude < 0.0001f) yield break;

            var refTr = forwardReference ? forwardReference : transform;
            dir = refTr.TransformDirection(dir);
        }

        Quaternion targetRot = transform.rotation;
        bool hasFacingTarget = false;

        if (s.faceDirection)
        {
            Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
            if (flatDir.sqrMagnitude > 0.0001f)
            {
                targetRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
                hasFacingTarget = true;
            }
        }

        if (hasFacingTarget && s.waitForFacingBeforeMove)
        {
            while (!UpdateFacingTowards(targetRot, s.turnSpeed))
            {
                yield return null;
            }
        }

        float totalDistance;
        float totalDuration;

        if (s.moveMode == MoveMode.ByDistance)
        {
            totalDistance = (useCustomWorldTarget || useCustomTargetTransform)
                ? Vector3.Distance(startPosition, finalTargetPosition)
                : Mathf.Max(0f, s.distance);
            float moveSpeed = Mathf.Max(0f, s.speed);
            if (totalDistance <= 0f || moveSpeed <= 0f) yield break;

            totalDuration = totalDistance / moveSpeed;
        }
        else
        {
            totalDuration = Mathf.Max(0.0001f, s.duration);
            totalDistance = (useCustomWorldTarget || useCustomTargetTransform)
                ? Vector3.Distance(startPosition, finalTargetPosition)
                : Mathf.Max(0f, s.speed) * totalDuration;
            if (totalDistance <= 0f) yield break;
        }

        float elapsed = 0f;
        float previousProgress = 0f;
        float appliedDistance = 0f;

        while (elapsed < totalDuration)
        {
            if (hasFacingTarget && !s.waitForFacingBeforeMove)
            {
                UpdateFacingTowards(targetRot, s.turnSpeed);
            }

            float nextElapsed = Mathf.Min(totalDuration, elapsed + Time.deltaTime);
            float nextProgress = Ease01(s.easing, Mathf.Clamp01(nextElapsed / totalDuration));
            float deltaProgress = Mathf.Max(0f, nextProgress - previousProgress);
            float stepDistance = totalDistance * deltaProgress;

            if (stepDistance > 0f)
            {
                MoveDriver(dir * stepDistance);
                appliedDistance += stepDistance;
            }

            elapsed = nextElapsed;
            previousProgress = nextProgress;
            yield return null;
        }

        float remainingDistance = totalDistance - appliedDistance;
        if (remainingDistance > 0.0001f)
        {
            MoveDriver(dir * remainingDistance);
        }

        if (useCustomWorldTarget || useCustomTargetTransform)
        {
            TeleportDriver(finalTargetPosition);
        }
    }

    static Vector3 GetApproachTargetPosition(Vector3 startPosition, Vector3 targetPosition, float stopDistance)
    {
        Vector3 delta = targetPosition - startPosition;
        float fullDistance = delta.magnitude;
        if (fullDistance < 0.0001f) return startPosition;

        float targetTravelDistance = Mathf.Max(0f, fullDistance - Mathf.Max(0f, stopDistance));
        if (targetTravelDistance <= 0.0001f) return startPosition;

        return startPosition + delta / fullDistance * targetTravelDistance;
    }

    bool UpdateFacingTowards(Quaternion targetRot, float turnSpeed)
    {
        if (turnSpeed <= 0f)
        {
            transform.rotation = targetRot;
            return true;
        }

        float t = Mathf.Clamp01(Time.deltaTime * turnSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
        return Quaternion.Angle(transform.rotation, targetRot) <= 0.2f;
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
/// CustomMoveKind sinifi, custom move alaninin vector mu yoksa world target mi oldugunu belirler.
/// </summary>
public enum CustomMoveKind { DirectionVector, WorldTarget, TargetTransform }

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
    public CustomMoveKind customMoveKind = CustomMoveKind.DirectionVector;
    public Vector3 customDirection = Vector3.forward;
    [Tooltip("Custom move world target modunda obje bu kesin world pozisyonunda biter.")]
    public Vector3 customWorldTarget = Vector3.zero;
    [Tooltip("Custom target transform modunda obje bu hedefe yaklasir.")]
    public Transform customTargetTransform;
    [Tooltip("Target Transform modunda hedef objeye ne kadar mesafe kala duracagi.")]
    public float customTargetStopDistance = 0f;
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
    [Tooltip("Aciksa obje once donusunu bitirir, sonra yurur. Kapaliysa yurururken ayni anda yone doner.")]
    public bool waitForFacingBeforeMove = false;

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

