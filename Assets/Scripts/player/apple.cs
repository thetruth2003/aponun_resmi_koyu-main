using UnityEngine;

/// <summary>
/// Toplanabilir elma nesnesinin dusme ve oyuncu tarafindan alinma davranisini yonetir.
/// </summary>
public class apple : Collectable
{
    private Tree tree;
    private Rigidbody2D rb;
    private bool isDropped = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing on the apple object!");
            return;
        }

        rb.gravityScale = 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("zemin"))
        {
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero;
        }

        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            Item item = GetComponent<Item>();
            if (item != null)
            {
                player.inventoryManager.Add("backpack", item);
                Destroy(gameObject);
            }
        }
    }

    public void Drop()
    {
        rb.gravityScale = 1;
    }
}
