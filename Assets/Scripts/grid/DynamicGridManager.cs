using UnityEngine;

/// <summary>
/// Oyuncunun etrafinda tarim hucrelerini olusturup gorunurlugunu yonetir.
/// </summary>
public class DynamicGridManager : MonoBehaviour
{
    [SerializeField] private GameObject gridCellPrefab;
    [SerializeField] private int gridWidth = 50;
    [SerializeField] private int gridHeight = 50;
    [SerializeField] private float cellSize = 2.5f;
    [SerializeField] private int renderDistance = 6;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[,] gridCells;
    [SerializeField] private float updateInterval = 0.1f;
    private float timeSinceLastUpdate = 0f;
    public Crosshair crosshair;
    public GameObject selectedCell;
    public Toolbar_UI toolbar;

    void Start()
    {
        if (!gridCellPrefab)
        {
            Debug.LogError("Grid hucre prefab'i atanmadi!");
            return;
        }

        if (!player)
        {
            player = GameObject.FindWithTag("Player")?.transform;
            if (!player)
            {
                Debug.LogError("Oyuncu objesi bulunamadi!");
                return;
            }
        }

        CreateGrid();
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            UpdateGridVisibility();
            timeSinceLastUpdate = 0f;
        }

        if (Input.GetMouseButtonDown(1) && crosshair != null)
        {
            crosshair.ActivateCellAtMousePosition();
        }
    }

    private void CreateGrid()
    {
        gridCells = new GameObject[gridWidth, gridHeight];
        Vector3 gridOrigin = player.position - new Vector3((gridWidth / 2) * cellSize, 0f, (gridHeight / 2) * cellSize);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 cellPosition = new Vector3(gridOrigin.x + x * cellSize, 2f, gridOrigin.z + z * cellSize);
                RaycastHit hit = default;
                if (Physics.Raycast(cellPosition + Vector3.up * 10f, Vector3.down, out hit, 20f))
                {
                    cellPosition.y = hit.point.y;
                }

                GameObject newCell = Instantiate(gridCellPrefab, cellPosition, Quaternion.identity, transform);
                AlignToSurface(newCell, hit);
                gridCells[x, z] = newCell;
            }
        }
    }

    private void AlignToSurface(GameObject cell, RaycastHit hit)
    {
        if (hit.collider != null)
        {
            cell.transform.position = hit.point;
            cell.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
    }

    private void UpdateGridVisibility()
    {
        if (!player)
        {
            return;
        }

        float maxDistance = renderDistance * cellSize;
        float maxDistanceSqr = maxDistance * maxDistance;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                GameObject cell = gridCells[x, z];
                if (cell == null)
                {
                    continue;
                }

                float distanceSqr = (player.position - cell.transform.position).sqrMagnitude;
                bool shouldBeActive = distanceSqr <= maxDistanceSqr;

                if (cell.activeSelf != shouldBeActive)
                {
                    cell.SetActive(shouldBeActive);
                }
            }
        }
    }
}
