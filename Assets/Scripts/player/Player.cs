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
    /// Oyuncunun hedef pozisyona ulaşıp ulaşmadığını kontrol eder.
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
            if (handObject == null)
            {
                Debug.LogError("HandObject bulunamadı!");
            }
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
            Debug.LogError("HandObject null! Lütfen el nesnesini atayın.");
            return;
        }

        if (handObject.transform.childCount > 0)
        {
            Destroy(handObject.transform.GetChild(0).gameObject);
        }

        string selectedItemPrefab = toolbar != null ? toolbar.GetSelectedPrefab() : null;
        if (string.IsNullOrEmpty(selectedItemPrefab))
        {
            return;
        }

        GameObject newItem = Resources.Load<GameObject>($"Prefabs/{selectedItemPrefab}");
        if (newItem != null)
        {
            GameObject instantiatedItem = Instantiate(newItem, handObject.transform);
            instantiatedItem.transform.localPosition = Vector3.zero;
            instantiatedItem.transform.localRotation = Quaternion.identity;
            instantiatedItem.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.LogWarning($"Prefab not found for item: {selectedItemPrefab}");
        }
    }
}
