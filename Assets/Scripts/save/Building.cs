using UnityEngine;
// public class Building : Item  // Item tabanlıysa bunu aç
public class Building : MonoBehaviour
{
    public string building_name;

    [Tooltip("Kalıcı benzersiz ID (prefabda boş bırak).")]
    public string persistentId;

    private void Awake()
    {
        if (string.IsNullOrEmpty(building_name))
            building_name = gameObject.name.Replace("(Clone)", "").Trim();

        if (string.IsNullOrEmpty(persistentId))
            persistentId = System.Guid.NewGuid().ToString("N");
    }
}
