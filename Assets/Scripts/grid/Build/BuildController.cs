using UnityEngine;

public class BuildController : MonoBehaviour
{
    public GameObject foundation;          // Seçilen yapı prefab'ı
    public GameObject foundationPreview;  // Önizleme prefab'ı
    private Transform socket;             // Seçili "Socket" transformu
    public Camera playerCamera;
    private bool canBuild = true;

    void Update()
    {
        if (foundationPreview != null)
        {
            RaycastHit hit;
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            Renderer renderer = foundationPreview.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (canBuild)
                    renderer.sharedMaterial.SetColor("_Color", Color.green);
                else
                    renderer.sharedMaterial.SetColor("_Color", Color.red);
            }

            if (Physics.Raycast(ray, out hit, 10f))
            {
                foundationPreview.transform.position = hit.point + new Vector3(3, 0.1f, 3); // Zeminden 0.1 birim yukarı

                if (hit.transform.tag == "Platform")
                    canBuild = false;
                else
                    canBuild = true;

                if (hit.transform.CompareTag("socket"))
                {
                    socket = hit.transform;
                    if (canBuild)
                    {
                        foundationPreview.transform.position = socket.transform.position;
                        foundationPreview.SetActive(true);
                    }
                    else
                    {
                        foundationPreview.SetActive(true);
                    }

                    if (Input.GetMouseButtonDown(0) && canBuild)
                    {
                        GameObject spawnFoundation = Instantiate(foundation, socket.position, Quaternion.identity);
                        Destroy(socket.gameObject);
                    }
                }
                else
                {
                    if (foundationPreview != null)
                    {
                        foundationPreview.transform.position = hit.point;
                        foundationPreview.SetActive(true);
                    }

                    if (Input.GetMouseButtonDown(0) && canBuild)
                    {
                        GameObject spawnFoundation = Instantiate(foundation, hit.point, Quaternion.identity);
                    }
                }
            }
        }

        // Sağ tıklama ile prefab'ları "none" yap ve preview'i kapat
        if (Input.GetMouseButtonDown(1)) // 1 sağ tık için
        {
            ResetPrefabs();
        }
    }

    public void SetFoundation(string foundationName)
    {
        // Foundation prefab'ını ayarla
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
            if (foundationPreview != null)
            {
                Destroy(foundationPreview); // Eski preview prefab'ını yok et
            }

            foundationPreview = Instantiate(loadedPreview); // Yeni preview prefab'ını yarat
            foundationPreview.SetActive(false); // Başlangıçta görünmez yap
            Debug.Log("Preview prefab yüklendi ve sahnede yaratıldı: " + previewName);
        }
        else
        {
            Debug.LogError("Preview prefab bulunamadı: " + previewName);
        }
    }

    // Sağ tık ile prefab'ları sıfırlama ve preview'i kapatma
    private void ResetPrefabs()
    {
        foundation = null;
        if (foundationPreview != null)
        {
            foundationPreview.SetActive(false);  // Preview prefab'ını kapat
            Destroy(foundationPreview);  // Eğer aktifse, sahneden tamamen yok et
        }
        Debug.Log("Prefabs sıfırlandı.");
    }
}
