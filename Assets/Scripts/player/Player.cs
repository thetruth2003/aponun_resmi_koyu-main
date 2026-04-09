using UnityEngine;

/// <summary>
/// Oyuncunun elde tuttugu nesneyi ve yere item birakma davranisini yonetir.
/// </summary>
public class Player : MonoBehaviour
{
    public InventoryManager inventoryManager;
    private TileManager tileManager;
    public GameManager gameManager;
    public Toolbar_UI toolbar;
    public GameObject handObject;

    public static Player Instance;
    private bool isUpdating = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Oyuncunun hedef pozisyona ulaþýp ulaþmadýðýný kontrol eder.
    /// </summary>
    public bool IsAt(Vector3 targetPosition)
    {
        return Vector3.Distance(transform.position, targetPosition) < 1f;
    }

    private void Start()
    {
        if (handObject == null)
        {
            handObject = GameObject.Find("HandObject");
            if (handObject != null)
            {
                Debug.Log("HandObject bulundu: " + handObject.name);
            }
            else
            {
                Debug.LogError("HandObject bulunamadý!");
            }
        }
        else
        {
            Debug.Log("HandObject zaten atanmýþ: " + handObject.name);
        }
    }

    public void DropItem(Item item)
    {
        Vector3 spawnLocation = transform.position;
        Vector3 spawnOffset = Random.insideUnitSphere * 1.25f;
        Item droppedItem = Instantiate(item, spawnLocation + spawnOffset, Quaternion.identity);
    }

    public void DropItem(Item item, int numToDrop)
    {
        for (int i = 0; i < numToDrop; i++)
        {
            DropItem(item);
        }
    }

    public void UpdateHandObject()
    {
        if (handObject == null)
        {
            Debug.LogError("HandObject null! Lütfen el nesnesini atayýn.");
            return;
        }

        if (handObject.transform.childCount > 0)
        {
            Destroy(handObject.transform.GetChild(0).gameObject);
        }

        string selectedItemPrefab = toolbar.GetSelectedPrefab();
        if (!string.IsNullOrEmpty(selectedItemPrefab))
        {
            GameObject newItem = Resources.Load<GameObject>($"Prefabs/{selectedItemPrefab}");
            if (newItem != null)
            {
                GameObject instantiatedItem = Instantiate(newItem, handObject.transform);
                instantiatedItem.transform.localPosition = Vector3.zero;
                instantiatedItem.transform.localRotation = Quaternion.identity;
                instantiatedItem.transform.localScale = Vector3.one;
                Debug.Log($"Prefab found and added: {selectedItemPrefab}");
            }
            else
            {
                Debug.LogWarning($"Prefab not found for item: {selectedItemPrefab}");
            }
        }
        else
        {
            Debug.Log("Seçili bir item yok.");
        }
    }
}
