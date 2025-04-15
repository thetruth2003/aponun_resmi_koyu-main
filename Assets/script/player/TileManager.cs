using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public Tilemap interactableMap;
    public Tile hiddenInteractableTile;
    public Tile plowedTile;
    public GameObject seedPrefab;

    void Start()
    {

    }
    public bool IsDiggable(Vector3Int position)
    {
        TileBase tile = interactableMap.GetTile(position);
        
        if(tile != null)
        {
            if(tile.name == "Interactable")
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
        
        if(tile != null)
        {
            if(tile.name == "excavated")
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
        // Tile'ın boyutunu almak için TileMap'ten pozisyona göre bir Tile alalım
        TileBase tile = interactableMap.GetTile(position);
        
        // GameObject'i instantiate et ve tile'ın merkezine yerleştir
        GameObject seedObject = Instantiate(seedPrefab, new Vector3(position.x + 0.5f, position.y + 0.5f, 0), Quaternion.identity);
        
        // Ekstra bir işlem yapmak isterseniz, örneğin seedObject'in boyutunu ayarlayabilirsiniz
        // Ancak tile boyutu ile seedPrefab boyutu aynıysa bu adım gerekli olmayabilir.
        seedObject.transform.localScale = Vector3.one; // İsterseniz boyutu ayarlayabilirsiniz
        
        // Ekstra bir işlem yapmak isterseniz, örneğin seedObject'in parent'ını ayarlayabilirsiniz
        // seedObject.transform.parent = yourParentTransform; 
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
