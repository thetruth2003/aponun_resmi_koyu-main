using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toplanabilir item referanslarini isimlerine gore saklayip ulasmayi kolaylastirir.
/// </summary>
public class ItemManager : MonoBehaviour
{
    public Item[] Save;
    public Dictionary<string, Item> collectableItemsDict = new Dictionary<string, Item>();

    private void Awake()
    {
        foreach (Item collectable in Save)
        {
            AddItem(collectable);
        }
    }

    private void AddItem(Item item)
    {
        if (!collectableItemsDict.ContainsKey(item.data.itemName))
        {
            collectableItemsDict.Add(item.data.itemName, item);
        }
    }

    public Item GetItemByName(string key)
    {
        if (collectableItemsDict.ContainsKey(key))
        {
            return collectableItemsDict[key];
        }

        return null;
    }
}
