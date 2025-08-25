using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering; // sadece AmbientMode için (Built-in'de de var)

[DefaultExecutionOrder(-1000)]
public class GameSceneFixer : MonoBehaviour
{
    [Header("Target")]
    public Camera targetCamera;            // boş bırakılırsa otomatik bulur
    public Material defaultSkybox;         // isteğe bağlı

    [Header("What to disable from DDOL (safe)")]
    public bool disableDDOLCameras        = true;
    public bool disableDDOLVolumes        = true;  // URP Volume + PPSv2
    public bool disableDDOLAudioListeners = true;

    [Header("Debug")]
    public bool logOnly = false;           // önce true ile dene: log atar, kapatmaz

    void Awake()
    {
        // 0) temel güvenlik
        Time.timeScale = 1f;
        var active = SceneManager.GetActiveScene();
        if (active.IsValid()) SceneManager.SetActiveScene(active);

        // 1) kamera seç
        if (!targetCamera) targetCamera = Camera.main;
        if (!targetCamera)
            targetCamera = FindObjectsOfType<Camera>(true)
                          .FirstOrDefault(c => c && c.gameObject.scene == active)
                          ?? FindObjectsOfType<Camera>(true).FirstOrDefault();

        // 2) DDOL'daki kalıntıları devre dışı bırak
        if (disableDDOLCameras)        DisableDDOLType("UnityEngine.Camera");
        if (disableDDOLAudioListeners) DisableDDOLType("UnityEngine.AudioListener");
        if (disableDDOLVolumes)
        {
            DisableDDOLType("UnityEngine.Rendering.Volume"); // URP/HDRP
            DisableDDOLType("UnityEngine.Rendering.PostProcessing.PostProcessVolume"); // PPSv2
            DisableDDOLType("UnityEngine.Rendering.PostProcessing.PostProcessLayer");  // PPSv2
        }

        // 3) sadece targetCamera açık kalsın + AudioListener tek olsun
        foreach (var cam in Resources.FindObjectsOfTypeAll<Camera>())
        {
            if (!cam) continue;
            bool keep = (cam == targetCamera);
            Log($"Camera {(keep ? "KEEP" : "DISABLE")}", cam.gameObject);
            if (!logOnly) cam.enabled = keep;
        }
        var listeners = Resources.FindObjectsOfTypeAll<AudioListener>();
        foreach (var al in listeners)
        {
            bool keep = (targetCamera && al && al.gameObject == targetCamera.gameObject);
            Log($"AudioListener {(keep ? "KEEP" : "DISABLE")}", al ? al.gameObject : null);
            if (!logOnly && al) al.enabled = keep;
        }
        if (!logOnly && targetCamera && !targetCamera.GetComponent<AudioListener>())
            targetCamera.gameObject.AddComponent<AudioListener>();

        // 4) kamera/ışık reset
        if (targetCamera)
        {
            targetCamera.clearFlags = CameraClearFlags.Skybox;
            targetCamera.backgroundColor = Color.black;
        }
        if (defaultSkybox) RenderSettings.skybox = defaultSkybox;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.reflectionIntensity = 1f;
        DynamicGI.UpdateEnvironment();

        // 5) URP Overlay ise Base'e çevir + camera stack'i temizle (reflection)
        MakeURPCameraBase(targetCamera);
    }

    void DisableDDOLType(string fullTypeName)
    {
        var comps = Resources.FindObjectsOfTypeAll<Component>();
        foreach (var c in comps)
        {
            if (!c) continue;
            if (c.gameObject.scene.name != "DontDestroyOnLoad") continue;
            if (c.GetType().FullName != fullTypeName) continue;

            if (c is Behaviour b)
            {
                Log($"DDOL {fullTypeName} DISABLE", c.gameObject);
                if (!logOnly) b.enabled = false;
            }
            else
            {
                Log($"DDOL {fullTypeName} FOUND(not Behaviour)", c.gameObject);
            }
        }
    }

    void MakeURPCameraBase(Camera cam)
    {
        if (!cam) return;
        var urpType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType == null) return;

        var data = cam.GetComponent(urpType);
        if (data == null) return;

        var rtProp = urpType.GetProperty("renderType");
        if (rtProp != null)
        {
            // enum RenderType: 0=Base, 1=Overlay
            var enumType = rtProp.PropertyType;
            var baseValue = System.Enum.GetValues(enumType).GetValue(0);
            rtProp.SetValue(data, baseValue, null);
        }
        var stackProp = urpType.GetProperty("cameraStack");
        if (stackProp != null)
        {
            var stack = stackProp.GetValue(data) as System.Collections.IList;
            stack?.Clear();
        }
        Log("URP set Base + clear stack", cam.gameObject);
    }

    void Log(string msg, GameObject go)
    {
        var path = go ? GetPath(go) : "(null)";
        Debug.Log($"[GameSceneFixer] {msg} → {path}");
    }
    string GetPath(GameObject go)
    {
        if (!go) return "(null)";
        var t = go.transform;
        string p = t.name;
        while (t.parent) { t = t.parent; p = t.name + "/" + p; }
        return $"{p} (scene:{go.scene.name})";
    }
}
