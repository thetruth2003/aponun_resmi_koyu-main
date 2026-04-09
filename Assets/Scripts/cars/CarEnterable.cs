using UnityEngine;

/// <summary>
/// Araca binme/√ß???±kma lojiklerini kaps√ºlleyen ba???ü???±ms???±z script.
/// Arabaya ekle; kamera ve s√ºr√º≈ü scriptlerini otomatik bulur (istersen Inspector‚Ä????dan atayabilirsin).
/// </summary>
public class CarEnterable : MonoBehaviour
{
    [Header("Refs (opsiyonel ‚Äî bo≈ü b???±rak???±rsan otomatik bulunur)")]
    [Tooltip("Ara√ß i√ßin kullan???±lacak kamera (bo≈üsa children'da ilk Camera bulunur)")]
    public Camera carCamera;

    [Tooltip("√á???±k???±≈ü konumu (bo≈üsa sadece ara√ß yan???±nda spawn kal???±rs???±n)")]
    public Transform exitPoint;

    [Tooltip("Birincil s√ºr√º≈ü scripti (VehicleController / CarController vs.). Bo≈üsa otomatik bulunur.")]
    public Behaviour primaryDriveScript;

    [Tooltip("Ek s√ºr√º≈ü/yard???±mc???± scriptler (√∂r. eski controller, input reader vs.)")]
    public Behaviour[] extraDriveScripts;

    [Header("Ayarlar")]
    [Tooltip("Enter‚Ä????da t√ºm di???üer ara√ß kameralar???±n???± kapat")]
    public bool disableSiblingCameras = true;

    private GameObject _player;
    private Camera _playerCam;
    private AudioListener _playerListener;
    private AudioListener _carListener;
    private bool _isActive;

    private void Reset()
    {
        AutoFindCamera();
        AutoFindDriveScript();
    }

    private void Awake()
    {
        if (!carCamera) AutoFindCamera();
        if (!primaryDriveScript) AutoFindDriveScript();
    }

    private void AutoFindCamera()
    {
        carCamera = GetComponentInChildren<Camera>(true);
    }

    private void AutoFindDriveScript()
    {
        var vc = GetComponent<VehicleController>();
        if (vc) { primaryDriveScript = vc; return; }

        var cc = GetComponent<CarController>();
        if (cc) { primaryDriveScript = cc; return; }
    }

    /// <summary> Araca bin: player objesi ve player kamera ver. </summary>
    public bool Enter(GameObject player, Camera playerCamera)
    {
        if (_isActive) return true;

        _player = player;
        _playerCam = playerCamera;

        if (!primaryDriveScript && !TryAutoResolveDrive())
        {
            Debug.LogWarning($"[CarEnterable] S√ºr√º≈ü scripti bulunamad???±: {name}");
            return false;
        }

        if (!carCamera) AutoFindCamera();
        if (!carCamera)
        {
            Debug.LogWarning($"[CarEnterable] Kamera bulunamad???±: {name}");
            return false;
        }

        if (_player) _player.SetActive(false);

        if (_playerCam)
        {
            _playerListener = _playerCam.GetComponent<AudioListener>();
            if (_playerListener) _playerListener.enabled = false;
            _playerCam.enabled = false;
            _playerCam.gameObject.SetActive(false);
        }

        if (disableSiblingCameras)
            DisableSiblingCamerasExcept(carCamera);

        _carListener = carCamera.GetComponent<AudioListener>();
        if (!_carListener) _carListener = carCamera.gameObject.AddComponent<AudioListener>();
        carCamera.gameObject.SetActive(true);
        carCamera.enabled = true;
        _carListener.enabled = true;

        SetDriveScripts(true);

        _isActive = true;
        return true;
    }

    /// <summary> Ara√ßtan √ß???±k: player geri gelsin, player kamera a√ß???±ls???±n, ara√ß script/kamera kapans???±n. </summary>
    public void Exit()
    {
        if (!_isActive) return;

        SetDriveScripts(false);

        if (_carListener) _carListener.enabled = false;
        if (carCamera)
        {
            carCamera.enabled = false;
            carCamera.gameObject.SetActive(false);
        }

        if (_player)
        {
            if (exitPoint)
                _player.transform.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);

            _player.transform.SetParent(null);
            _player.SetActive(true);
        }

        if (_playerCam)
        {
            if (_playerListener == null)
                _playerListener = _playerCam.GetComponent<AudioListener>() ?? _playerCam.gameObject.AddComponent<AudioListener>();

            _playerCam.gameObject.SetActive(true);
            _playerCam.enabled = true;
            _playerListener.enabled = true;
        }

        _isActive = false;
        _player = null;
        _playerCam = null;
        _playerListener = null;
        _carListener = null;
    }

    private void SetDriveScripts(bool enable)
    {
        if (primaryDriveScript) primaryDriveScript.enabled = enable;
        if (extraDriveScripts != null)
        {
            for (int i = 0; i < extraDriveScripts.Length; i++)
            {
                var b = extraDriveScripts[i];
                if (b) b.enabled = enable;
            }
        }
    }

    private bool TryAutoResolveDrive()
    {
        AutoFindDriveScript();
        return primaryDriveScript;
    }

    private void DisableSiblingCamerasExcept(Camera keep)
    {
        var cams = GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cams.Length; i++)
        {
            var c = cams[i];
            if (!c || c == keep) continue;
            var l = c.GetComponent<AudioListener>();
            if (l) l.enabled = false;
            c.enabled = false;
            c.gameObject.SetActive(false);
        }
    }
}
