using UnityEngine;

public class BuildController : MonoBehaviour
{
    public GameObject foundation;          // Seçilen yapı prefab'ı
    public GameObject foundationPreview;   // Önizleme prefab'ı
    private Transform socket;              // Seçili "Socket"
    public Camera playerCamera;
    private bool canBuild = true;

    [Header("Rotation (Mouse Wheel)")]
    [SerializeField] private float rotationPerNotch = 15f; // teker bir 'tik' için derece
    [SerializeField] private bool invertScroll = false;    // yön ters gelirse işaretle
    private float currentYaw = 0f;
    private Quaternion basePreviewRotation = Quaternion.identity;

    void Update()
    {
        if (PauseMenuUI.IsInputLocked) return;

        if (foundationPreview != null)
        {
            // --- ORTA TEKER: ROTASYON ---
            float scroll = Input.mouseScrollDelta.y; // genelde +1 / -1
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                if (invertScroll) scroll = -scroll;
                currentYaw += scroll * rotationPerNotch;
                // 0..360 aralığında tut
                if (currentYaw >= 360f) currentYaw -= 360f;
                else if (currentYaw < 0f) currentYaw += 360f;

                foundationPreview.transform.rotation =
                    basePreviewRotation * Quaternion.Euler(0f, currentYaw, 0f);
            }

            RaycastHit hit;
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

            var renderer = foundationPreview.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.SetColor("_Color", canBuild ? Color.green : Color.red);
            }

            if (Physics.Raycast(ray, out hit, 10f))
            {
                // basit engel kuralı
                canBuild = !hit.transform.CompareTag("Platform");

                if (hit.transform.CompareTag("socket"))
                {
                    socket = hit.transform;
                    foundationPreview.transform.position = socket.position;
                    foundationPreview.SetActive(true);

                    if (Input.GetMouseButtonDown(0) && canBuild)
                    {
                        // preview rotasyonunu kullan
                        Quaternion rot = foundationPreview.transform.rotation;
                        Instantiate(foundation, socket.position, rot);
                        Destroy(socket.gameObject);
                    }
                }
                else
                {
                    foundationPreview.transform.position = hit.point;
                    foundationPreview.SetActive(true);

                    if (Input.GetMouseButtonDown(0) && canBuild)
                    {
                        Quaternion rot = foundationPreview.transform.rotation;
                        Instantiate(foundation, hit.point, rot);
                    }
                }
            }
        }

        // Sağ tık: sıfırla
        if (Input.GetMouseButtonDown(1))
        {
            ResetPrefabs();
        }
    }

    public void SetFoundation(string foundationName)
    {
        GameObject loadedFoundation = Resources.Load<GameObject>($"build/{foundationName}");
        if (loadedFoundation != null)
        {
            foundation = loadedFoundation;
            Debug.Log("Foundation prefab yüklendi: " + foundationName);
        }
        else
        {
            Debug.LogError("Foundation prefab bulunamadı: " + foundationName);
        }
    }

    public void SetFoundationPreviewName(string previewName)
    {
        GameObject loadedPreview = Resources.Load<GameObject>($"build/{previewName}");
        if (loadedPreview != null)
        {
            if (foundationPreview != null) Destroy(foundationPreview);

            foundationPreview = Instantiate(loadedPreview);
            foundationPreview.SetActive(false);

            basePreviewRotation = foundationPreview.transform.rotation; // referans
            currentYaw = 0f; // teker açısını sıfırla

            Debug.Log("Preview prefab yüklendi ve sahnede yaratıldı: " + previewName);
        }
        else
        {
            Debug.LogError("Preview prefab bulunamadı: " + previewName);
        }
    }

    private void ResetPrefabs()
    {
        foundation = null;
        if (foundationPreview != null)
        {
            foundationPreview.SetActive(false);
            Destroy(foundationPreview);
        }
        currentYaw = 0f;
        basePreviewRotation = Quaternion.identity;

        Debug.Log("Prefabs sıfırlandı.");
    }
}
