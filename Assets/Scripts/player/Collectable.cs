using UnityEngine;

/// <summary>
/// Uzerindeki Item verisini alip oyuncunun envanterine eklenebilen toplanabilir nesnedir.
/// </summary>
[RequireComponent(typeof(Item))]
public class Collectable : MonoBehaviour
{
    private void Awake()
    {
        Collider collider = GetComponent<Collider>();
    }

    public void Collect()
    {
        Item item = GetComponent<Item>();
        if (item != null)
        {
            InventoryManager.Instance.Add("backpack", item);
            Debug.Log($"{gameObject.name} toplandý!");

            string key = $"Harvested_{item.data.itemName.ToLower()}";
            GameStateTracker.Instance.IncrementCount(key, 1);
            Destroy(item.gameObject);
        }
    }

    public void Buy(int amount)
    {
        Item item = GetComponent<Item>();
        if (item != null)
        {
            for (int i = 0; i < amount; i++)
            {
                InventoryManager.Instance.Add("backpack", item);
            }

            Debug.Log($"{amount} adet {gameObject.name} satýn alýndý!");
            string key = $"Harvested_{item.data.itemName.ToLower()}";
            GameStateTracker.Instance.IncrementCount(key, amount);
            Destroy(item.gameObject);
        }
    }
}
