using UnityEngine;

/// <summary>
/// Envanterde kullanilan bir esyanin temel veri kaydini tutar.
/// </summary>
[CreateAssetMenu(fileName = "Item Data", menuName = "Item Data", order = 50)]
public class ItemData : ScriptableObject
{
    public string itemName = "itemName";
    public Sprite icon = null;
    public GameObject itemPrefab = null;
    public int maxAllowed;
    public GameObject itemUsedPrefab = null;
    public int sellPrice;
    public SeedData seedData;
}
