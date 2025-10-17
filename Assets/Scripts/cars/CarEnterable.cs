using UnityEngine;

/// <summary>
/// Araca binme/çıkma lojiklerini kapsülleyen bağımsız script.
/// Arabaya ekle; kamera ve sürüş scriptlerini otomatik bulur (istersen Inspector’dan atayabilirsin).
/// </summary>
public class CarEnterable : MonoBehaviour
{
    [Header("Refs (opsiyonel — boş bırakırsan otomatik bulunur)")]
    [Tooltip("Araç için kullanılacak kamera (boşsa children'da ilk Camera bulunur)")]
    public Camera carCamera;

    [Tooltip("Çıkış konumu (boşsa sadece araç yanında spawn kalırsın)")]
    public Transform exitPoint;

    [Tooltip("Birincil sürüş scripti (VehicleController / CarController vs.). Boşsa otomatik bulunur.")]
    public Behaviour primaryDriveScript;

    [Tooltip("Ek sürüş/yardımcı scriptler (ör. eski controller, input reader vs.)")]
    public Behaviour[] extraDriveScripts;

    [Header("Ayarlar")]
    [Tooltip("Enter’da tüm diğer araç kameralarını kapat")]
    public bool disableSiblingCameras = true;

    // runtime
    private GameObject _player;
    private Camera _playerCam;
    private AudioListener _playerListener;
    private AudioListener _carListener;
    private bool _isActive;

    // Otomatik bulmalar
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
        // Öncelik VehicleController, yoksa CarController
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
            Debug.LogWarning($"[CarEnterable] Sürüş scripti bulunamadı: {name}");
            return false;
        }

        if (!carCamera) AutoFindCamera();
        if (!carCamera)
        {
            Debug.LogWarning($"[CarEnterable] Kamera bulunamadı: {name}");
            return false;
        }

        // Player’ı gizle
        if (_player) _player.SetActive(false);

        // Player kamera kapat
        if (_playerCam)
        {
            _playerListener = _playerCam.GetComponent<AudioListener>();
            if (_playerListener) _playerListener.enabled = false;
            _playerCam.enabled = false;
            _playerCam.gameObject.SetActive(false);
        }

        // Diğer araç kameralarını kapat (opsiyonel)
        if (disableSiblingCameras)
            DisableSiblingCamerasExcept(carCamera);

        // Araç kamera aç + AudioListener tek olsun
        _carListener = carCamera.GetComponent<AudioListener>();
        if (!_carListener) _carListener = carCamera.gameObject.AddComponent<AudioListener>();
        carCamera.gameObject.SetActive(true);
        carCamera.enabled = true;
        _carListener.enabled = true;

        // Sürüş scriptlerini aç
        SetDriveScripts(true);

        _isActive = true;
        return true;
    }

    /// <summary> Araçtan çık: player geri gelsin, player kamera açılsın, araç script/kamera kapansın. </summary>
    public void Exit()
    {
        if (!_isActive) return;

        // Sürüş scriptlerini kapat
        SetDriveScripts(false);

        // Araç kamera kapat
        if (_carListener) _carListener.enabled = false;
        if (carCamera)
        {
            carCamera.enabled = false;
            carCamera.gameObject.SetActive(false);
        }

        // Player’ı çıkış noktasına koy
        if (_player)
        {
            if (exitPoint)
                _player.transform.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);

            _player.transform.SetParent(null);
            _player.SetActive(true);
        }

        // Player kamera aç
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
