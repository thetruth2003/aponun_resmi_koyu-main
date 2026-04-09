using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Islenebilir tile kontrolunu yapar ve tohum ekme gibi tarla degisikliklerini uygular.
/// </summary>
public class TileManager : MonoBehaviour
{
    public Tilemap interactableMap;
    public Tile hiddenInteractableTile;
    public Tile plowedTile;
    public GameObject seedPrefab;

    private void Start()
    {
    }

    public bool IsDiggable(Vector3Int position)
    {
        TileBase tile = interactableMap.GetTile(position);
        if (tile != null)
        {
            if (tile.name == "Interactable")
            {
                Debug.Log("Tile is interactable");
                return true;
            }
        }

        return false;
    }

    public void SetDiggable(Vector3Int position)
    {
        interactableMap.SetTile(position, plowedTile);
    }

    public bool IsSeed(Vector3Int position)
    {
        TileBase tile = interactableMap.GetTile(position);
        if (tile != null)
        {
            if (tile.name == "excavated")
            {
                Debug.Log("Tile is excevated");
                return true;
            }
        }

        return false;
    }

    public void SetSeed(Vector3Int position)
    {
        if (seedPrefab != null)
        {
            TileBase tile = interactableMap.GetTile(position);
            GameObject seedObject = Instantiate(seedPrefab, new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity);
            seedObject.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.LogError("SeedPrefab is not assigned!");
        }
    }

    public string GetTileName(Vector3Int position)
    {
        if (interactableMap != null)
        {
            TileBase tile = interactableMap.GetTile(position);
            if (tile != null)
            {
                return tile.name;
            }
        }

        return "";
    }
}
