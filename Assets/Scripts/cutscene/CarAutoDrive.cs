using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// CarAutoDrive sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public class CarAutoDrive : MonoBehaviour
{
    public Transform[] waypoints;
    public game_start gameStart;
    public GameObject player;
    public GameObject canvas;
    public GameObject canvas2;
    public GameObject npcler;

    public GameObject cutscene;
    public float arriveThreshold = 1f;

    NavMeshAgent agent;
    int i = 0;
    bool done = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;

        if (player)   player.SetActive(false);
                if (canvas)   canvas.SetActive(false);
                        if (canvas2)   canvas2.SetActive(false);
        if (cutscene) cutscene.SetActive(true);

        agent.SetDestination(waypoints[0].position);
    }

    void Update()
    {
        if (done || agent.pathPending) return;

        if (agent.remainingDistance <= arriveThreshold)
        {
            i++;
            if (i < waypoints.Length)
                agent.SetDestination(waypoints[i].position);
            else
                StartCoroutine(FinishSeq());
        }
    }

    IEnumerator FinishSeq()
    {
        done = true;
        agent.isStopped = true; agent.ResetPath();

        if (gameStart) yield return gameStart.StartCoroutine(gameStart.FadeIn());
        if (player) { player.SetActive(true); yield return null; }
        if (canvas) canvas.SetActive(true);
        if (canvas2) canvas2.SetActive(true);
        if (gameStart) yield return gameStart.StartCoroutine(gameStart.FadeOut());
        if (cutscene) cutscene.SetActive(false);
        if (npcler) npcler.SetActive(false);
    }
}
