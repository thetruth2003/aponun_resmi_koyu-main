using System.Collections;
using UnityEngine;
using UnityEngine.AI; // Eğer karakter hareket edecekse NavMesh kullanmak için.

public class CharacterAnimationController : MonoBehaviour
{
    public Animator animator; // NPC'nin Animator bileşeni.
    public NavMeshAgent agent; // NPC'nin NavMeshAgent bileşeni.
    public Transform targetPoint; // NPC'nin yürüyeceği hedef nokta.

    void Start()
    {
        // NPC'yi hedef noktaya hareket ettir.
        if (agent != null && targetPoint != null)
        {
            agent.SetDestination(targetPoint.position);
            animator.SetBool("IsWalking", true); // Yürüme animasyonunu başlat.
            agent = GetComponent<NavMeshAgent>();
        }
    }

    void Update()
    {
        // NPC hedefe ulaştıysa animasyonu durdur ve hareketi bitir.
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                animator.SetBool("IsWalking", false); // Yürüme animasyonunu durdur.
            }
        }
        // Hedefe gitme
        if (targetPoint != null)
        {
            agent.SetDestination(targetPoint.position);
        }
    }
}
