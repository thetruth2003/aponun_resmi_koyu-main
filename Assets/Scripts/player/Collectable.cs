using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Item))]
public class Collectable : MonoBehaviour
{
    // Nesneyi tetikleyici olarak ayarlayalım
    private void Awake()
    {
        Collider collider = GetComponent<Collider>();
    }

    // Raycast ile çalışacak Collect() metodu
    public void Collect()
    {
        Item item = GetComponent<Item>();

            if (item != null)
            {
                // Eşyayı envantere ekle ve nesneyi yok et
                InventoryManager.Instance.Add("backpack", item);
                Debug.Log($"{gameObject.name} toplandı!");
                // ✅ GameState'e harvest_ verisini yaz
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
            Debug.Log($"{amount} adet {gameObject.name} satın alındı!");
            string key = $"Harvested_{item.data.itemName.ToLower()}";
            GameStateTracker.Instance.IncrementCount(key, amount);
            Destroy(item.gameObject);
        }
    }
}
