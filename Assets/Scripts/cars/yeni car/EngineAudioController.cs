using UnityEngine;

/// <summary>
/// Aracin motor ve vites gecis seslerini devir bilgisine gore canlandirir.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EngineAudioController : MonoBehaviour
{
    public VehicleController vehicle;
    public AudioClip engineClip;
    public AudioClip shiftClip;

    private AudioSource engineSource;
    private AudioSource shiftSource;

    /// <summary>
    /// Motor ve vites degisim ses kaynaklarini olusturup temel 3D ses ayarlarini uygular.
    /// </summary>
    void Awake()
    {
        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.clip = engineClip;
        engineSource.loop = true;
        engineSource.playOnAwake = false;
        engineSource.spatialBlend = 1f;

        shiftSource = gameObject.AddComponent<AudioSource>();
        shiftSource.clip = shiftClip;
        shiftSource.loop = false;
        shiftSource.playOnAwake = false;
        shiftSource.spatialBlend = 1f;
    }

    void Start()
    {
        if (engineClip != null)
        {
            engineSource.Play();
        }
    }

    /// <summary>
    /// Arac devrine gore motor sesinin pitch ve ses seviyesini surekli ayarlar.
    /// </summary>
    void Update()
    {
        if (vehicle == null || vehicle.config == null || engineClip == null)
            return;

        float rpmNorm = Mathf.InverseLerp(vehicle.config.idleRPM, vehicle.config.maxRPM, vehicle.CurrentRPM);
        engineSource.pitch = Mathf.Lerp(0.9f, 2.0f, rpmNorm);
        engineSource.volume = Mathf.Lerp(0.4f, 1.0f, rpmNorm);
    }

    public void OnGearShift()
    {
        if (shiftClip != null)
        {
            shiftSource.Play();
        }

        if (engineSource.isPlaying)
        {
            engineSource.Pause();
            Invoke(nameof(ResumeEngine), vehicle.config.shiftDuration);
        }
    }

    private void ResumeEngine()
    {
        if (!engineSource.isPlaying)
        {
            engineSource.UnPause();
        }
    }
}
