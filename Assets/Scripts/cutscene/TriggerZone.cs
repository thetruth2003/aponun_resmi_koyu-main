using UnityEngine;

/// <summary>
/// TriggerZone sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TriggerZone : MonoBehaviour
{
    public string triggerKey = "Shop.Enter";
    public bool onlyPlayer = true;
    [SerializeField] private bool runSkipIfAlreadyPlayed = true;

    private CutsceneManager manager;

    private void Awake()
    {
        manager = FindObjectOfType<CutsceneManager>();
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (onlyPlayer && !other.CompareTag("Player")) return;
        if (!manager) return;

        if (runSkipIfAlreadyPlayed)
            manager.TryStartOrSkip(triggerKey);
        else
            manager.TryStart(triggerKey);
    }
}
