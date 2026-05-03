using UnityEngine;

/// <summary>
/// Uzerindeki Item verisini alip oyuncunun envanterine eklenebilen toplanabilir nesnedir.
/// </summary>
[RequireComponent(typeof(Item))]
public class Collectable : MonoBehaviour
{
    private void Awake()
    {
        _ = GetComponent<Collider>();
    }

    public void Collect()
    {
        Item item = GetComponent<Item>();
        if (item == null)
        {
            return;
        }

        InventoryManager.Instance.Add("backpack", item);
        Debug.Log($"{gameObject.name} toplandi!");

        if (GameStateTracker.Instance != null)
        {
            string key = $"Harvested_{NormalizeItemId(item.data.itemName)}";
            GameStateTracker.Instance.IncrementCount(key, 1);
        }

        Destroy(item.gameObject);
    }

    public void Buy(int amount)
    {
        Item item = GetComponent<Item>();
        if (item == null)
        {
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            InventoryManager.Instance.Add("backpack", item);
        }

        Debug.Log($"{amount} adet {gameObject.name} satin alindi!");

        if (GameStateTracker.Instance != null)
        {
            string key = $"Bought_{NormalizeItemId(item.data.itemName)}";
            GameStateTracker.Instance.IncrementCount(key, amount);
        }

        Destroy(item.gameObject);
    }

    private static string NormalizeItemId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
