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
                Destroy(item.gameObject);
            }
        
    }
}
