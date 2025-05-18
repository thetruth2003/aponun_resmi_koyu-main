using UnityEngine;

[System.Serializable]
public class GoToLocationStep : IQuestStep
{
    // Artık string değil, doğrudan GameObject referansı:
    public GameObject targetObject;

    public string GetName() => targetObject != null ? $"Go to {targetObject.name}" : "Go to ...";
    public void OnStart() { }
    public void OnUpdate() { }
    public bool IsComplete()
    {
        // Örnek: oyuncu collider’ı targetObject’in collider’ıyla tetiklerse
        return targetObject != null && Player.Instance.IsAt(targetObject.transform.position);
    }
}
