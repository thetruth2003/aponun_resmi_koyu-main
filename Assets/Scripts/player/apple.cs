using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class apple : Collectable
{
    private Tree tree; // "apple" objesini buraya bağlayacağız

    private Rigidbody2D rb;

    private bool isDropped = false; // Elmanın zeminle temas ettiğinde düşürülüp düşürülmediğini kontrol etmek için

    void Start()
    {
        // Elma objesinin Rigidbody2D bileşenini al
        rb = GetComponent<Rigidbody2D>();
        
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing on the apple object!");
        }

        rb.gravityScale = 0;
    }

    // Zeminle temas anını kontrol et
    private void OnTriggerEnter2D(Collider2D collision) 
    {
        // Eğer temas eden obje "zemin" adlı obje ise
        if(collision.gameObject.CompareTag("zemin"))
        {
            // Yerçekimini devre dışı bırak
            rb.gravityScale = 0;
            rb.velocity = Vector2.zero;

            // Eğer henüz düşürülmemişse, Drop() metodunu çağır

        }

      Player player = collision.gameObject.GetComponent<Player>();

        if (player != null)
        {
            Item item = GetComponent<Item>();

            if (item != null)
            {
                player.inventoryManager.Add("backpack", item);
                Destroy(this.gameObject);
            }
        }
    }

    public void Drop()
    {
        rb.gravityScale = 1; // Yerçekimini etkinleştir
    }
}
