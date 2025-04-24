using System.Collections;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public Toolbar_UI toolbarUI; // Toolbar referansı
    public float speed;
    public Animator animator;

    private Vector3 direction;
    private Vector3Int characterPosition;
    private TileManager tileManager;

    private void Start()
    {

    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        direction = new Vector3(horizontal, vertical).normalized;
        AnimateMovement(direction);

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.z * -1));
            Vector3Int gridPosition = new Vector3Int(Mathf.RoundToInt(mouseWorldPosition.x), Mathf.RoundToInt(mouseWorldPosition.y), 0);

            characterPosition = new Vector3Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), 0);

            if (gridPosition.x >= characterPosition.x - 1 && gridPosition.x <= characterPosition.x + 1 &&
                gridPosition.y >= characterPosition.y - 1 && gridPosition.y <= characterPosition.y + 1)
            {
                if (tileManager.IsDiggable(gridPosition)) StartCoroutine(Dig(gridPosition));
                if (tileManager.IsSeed(gridPosition)) StartCoroutine(Seed(gridPosition));
            }
            else
            {
                Debug.Log("Tile is out of range");
            }
            Debug.Log($"Character Position: {characterPosition}, Mouse Position: {gridPosition}");

            HandleToolAnimation(); // Sol tıklama ile araç animasyonunu tetikle
        }
    }

    private void FixedUpdate()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void AnimateMovement(Vector3 direction)
    {
        if (animator != null)
        {
            if (direction.magnitude > 0)
            {
                animator.SetBool("isMoving", true);
                animator.SetFloat("horizontal", direction.x);
                animator.SetFloat("vertical", direction.y);
            }
            else
            {
                animator.SetBool("isMoving", false);
            }
        }
    }

    private void HandleToolAnimation()
    {
        if (toolbarUI == null) return;

        string itemName = toolbarUI.GetSelectedPrefab();
        
        if (string.IsNullOrEmpty(itemName)) return;

        animator.ResetTrigger("axe");
        animator.ResetTrigger("hammer");
        animator.ResetTrigger("sword");
        animator.ResetTrigger("rod");

        if (itemName == "axe" || itemName == "hammer" || itemName == "sword" || itemName == "rod")
        {
            animator.SetTrigger(itemName); // Sol tıklamayla ilgili animasyonu tetikle
        }
    }

    private IEnumerator Dig(Vector3Int gridPosition)
    {
        if (toolbarUI == null || animator == null) yield break;

        string itemName = toolbarUI.GetSelectedPrefab();

        if (string.IsNullOrEmpty(itemName) || itemName != "hoe") yield break;

        Debug.Log($"Swinging tool: {itemName}");
        animator.SetTrigger("hoe");

        yield return new WaitForSeconds(0.2f);
        tileManager.SetDiggable(gridPosition);
    }

    private IEnumerator Seed(Vector3Int gridPosition)
    {
        if (toolbarUI == null || animator == null) yield break;

        string itemName = toolbarUI.GetSelectedPrefab();

        if (string.IsNullOrEmpty(itemName) || itemName != "seed") yield break;

        Debug.Log($"Swinging tool: {itemName}");
        animator.SetTrigger("seed");

        yield return new WaitForSeconds(0.2f);
        tileManager.SetSeed(gridPosition);
    }
}
