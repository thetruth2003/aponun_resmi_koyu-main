using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Data", menuName = "Item Data", order = 50)]
public class ItemData : ScriptableObject
{
    public string itemName = "itemName";
    public Sprite icon = null;
    public GameObject itemPrefab = null; // Prefab burada tutuluyor
    public int maxAllowed;
    public GameObject itemUsedPrefab = null; // Prefab burada tutuluyor
}
