using UnityEngine;

public class UniversalIdentifier : MonoBehaviour
{
    [SerializeField] private string id;

    public string ID => id;

    public void SetID(string newID) => id = newID;
}
