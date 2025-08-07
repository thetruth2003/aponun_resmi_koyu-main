using UnityEngine;
using UnityEngine.AI;

public class CarAutoDrive : MonoBehaviour
{
    public Transform[] waypoints;
    private NavMeshAgent agent;
    private int currentIndex = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        agent.SetDestination(waypoints[currentIndex].position);
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            currentIndex++;
            if (currentIndex < waypoints.Length)
                agent.SetDestination(waypoints[currentIndex].position);
        }
    }
}
