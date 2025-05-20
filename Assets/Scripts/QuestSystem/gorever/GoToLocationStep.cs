using UnityEngine;

[System.Serializable]
public class GoToLocationStep : IQuestStep
{
    public GameObject targetObject;

    private bool isCompleted = false;

    public string GetName() => targetObject != null
        ? $"Go to {targetObject.name}"
        : "Go to location";

    public void OnStart() { }

    public void OnUpdate()
    {
        if (isCompleted || targetObject == null) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        float distance = Vector3.Distance(player.transform.position, targetObject.transform.position);

        if (distance <= 2f)
        {
            isCompleted = true;
        }
    }

    public bool IsComplete() => isCompleted;
}
