using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Inegin rastgele dolasmasini, olmesini ve sagilmasini yonetir.
/// </summary>
public class CowMovement3D : MonoBehaviour
{
    private float timer;
    public float timerDuration = 10f;
    public GameObject prefab;
    public GameObject prefab2;
    private Vector3 targetPosition;
    public Animator animator;
    public float moveSpeed = 2f;
    public float waitTime = 2f;

    private bool isWalking = false;
    private Collider myCollider;

    private void Start()
    {
        myCollider = GetComponent<Collider>();
        SetRandomTarget();
        timer = timerDuration;
    }

    private void Update()
    {
        MoveToTarget();

        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        else
        {
            myCollider.enabled = true;
        }
    }

    private void MoveToTarget()
    {
        if (isWalking)
        {
            Vector3 currentPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            Vector3 direction = (targetPosition - currentPosition).normalized;
            if (direction.magnitude > 0.1f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * moveSpeed);
            }

            AnimateMovement(direction);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isWalking = false;
                AnimateMovement(Vector3.zero);
                StartCoroutine(WaitAndMove());
            }
        }
        else
        {
            AnimateMovement(Vector3.zero);
        }
    }

    private void SetRandomTarget()
    {
        float randomX = Random.Range(-5f, 5f);
        float randomZ = Random.Range(-5f, 5f);
        targetPosition = new Vector3(randomX, 0f, randomZ);
        isWalking = true;
    }

    private IEnumerator WaitAndMove()
    {
        yield return new WaitForSeconds(waitTime);
        SetRandomTarget();
    }

    private void AnimateMovement(Vector3 direction)
    {
        if (animator != null)
        {
            if (direction.magnitude > 0.1f)
            {
                animator.SetBool("isMoving", true);
                animator.SetFloat("horizontal", direction.x);
                animator.SetFloat("vertical", direction.z);
            }
            else
            {
                animator.SetBool("isMoving", false);
            }
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("axe"))
        {
            Death();
        }

        if (collision.CompareTag("sagmak"))
        {
            Sagmak();
            timer = timerDuration;
            myCollider.enabled = false;
        }
    }

    private void Death()
    {
        Debug.Log("Cow died");
        Destroy(gameObject);

        Vector3 spawnPoint = transform.position;
        Instantiate(prefab, spawnPoint, Quaternion.identity);
    }

    private void Sagmak()
    {
        Vector3 spawnPoint = transform.position;
        Instantiate(prefab2, spawnPoint, Quaternion.identity);
    }
}
