using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CutsceneMover : MonoBehaviour
{
    [Header("General")]
    public bool playOnAwake = true;
    public bool loop = false;                 // istersen sahneyi döngüde oynat
    public Transform forwardReference;        // boşsa kendi forward'ı kullanılır
    public Animator animator;                 // opsiyonel; yoksa anim adımları atlanır
    public UnityEvent OnSequenceFinished;     // tüm adımlar bittiğinde

    [Header("Driver")]
    public DriverType driver = DriverType.Transform; // ilk sürümde Transform tavsiye
    public CharacterController character;            // CharacterController seçersen doldur
    public Rigidbody rb;                             // Rigidbody seçersen doldur

    [Header("Steps (sırayla oynar)")]
    public List<Step> steps = new();

    Coroutine seqCo;
    bool playing;

    void Awake()
    {
        if (playOnAwake) Play();
    }

    public void Play()
    {
        if (playing) Stop();
        seqCo = StartCoroutine(RunSequence());
    }

    public void Stop()
    {
        if (seqCo != null) StopCoroutine(seqCo);
        seqCo = null;
        playing = false;
    }

    IEnumerator RunSequence()
    {
        playing = true;

        do
        {
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
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
                    case StepType.InvokeEvent:
                        s.onInvoke?.Invoke();
                        break;
                }
            }

            if (!loop) break;

        } while (loop);

        playing = false;
        OnSequenceFinished?.Invoke();
    }

    // ---- Steps ----

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

        // referans (örn. kameraya göre ileri/sağ)
        var refTr = forwardReference ? forwardReference : transform;
        dir = refTr.TransformDirection(dir); // local 8-yönü dünya uzayına çevir

        if (s.faceDirection)
        {
            // hedef rotasyonu önceden hesapla
            var targetRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z).normalized, Vector3.up);
            // başlangıçta hizalı değilse yumuşakçe döndür
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
        else // ByDuration
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
            // world euler
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

    // ---- Move helpers ----
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

public enum DriverType { Transform, CharacterController, Rigidbody }

public enum StepType { Move, PlayAnimation, Wait, Rotate, InvokeEvent }

public enum MoveMode { ByDistance, ByDuration }

public enum Direction8
{
    Forward, Back, Left, Right,
    ForwardLeft, ForwardRight, BackLeft, BackRight,
    Custom
}

public enum Easing { Linear, EaseIn, EaseOut, EaseInOut }

public enum AnimSetType { Trigger, Bool, Float, Int }

public enum RotateMode { WorldEuler, LookAtTarget }

[Serializable]
public class Step
{
    public StepType type = StepType.Move;
    
    // ---- common ----
    [Tooltip("Adım başında opsiyonel olarak anim paramı ver")]
    public bool playAnimAtStart = false;
    public AnimSetType animSetType = AnimSetType.Trigger;
    public string animParam;
    public bool boolValue;
    public float floatValue;
    public int intValue;
    [Tooltip("Animasyon adımı veya anim bekleme süresi")]
    public float waitSeconds = 0f;
    
    // ---- move ----
    public MoveMode moveMode = MoveMode.ByDistance;
    public Direction8 direction = Direction8.Forward;
    public Vector3 customDirection = Vector3.forward;
    [Tooltip("ByDistance modunda kullanılır (metre)")]
    public float distance = 2f;
    [Tooltip("ByDuration modunda kullanılır (saniye)")]
    public float duration = 1f;
    [Tooltip("m/s")]
    public float speed = 1.5f;
    public Easing easing = Easing.Linear;
    public bool faceDirection = true;
    [Tooltip("yönü ne kadar hızlı dönecek (Slerp hızı), 0=anında")]
    public float turnSpeed = 8f;

    // ---- rotate ----
    public RotateMode rotateMode = RotateMode.WorldEuler;
    public Vector3 worldEuler = Vector3.zero;
    public Transform lookTarget;

    // ---- invoke ----
    public UnityEvent onInvoke;
}
