using UnityEngine;
/// <summary>
/// Building sinifi, kayit sistemiyle ilgili davranisi yonetir.
/// </summary>
public class Building : MonoBehaviour
{
    public string building_name;

    [Tooltip("Kalýcý benzersiz ID (prefabda boþ býrak).")]
    public string persistentId;

    private void Awake()
    {
        if (string.IsNullOrEmpty(building_name))
            building_name = gameObject.name.Replace("(Clone)", "").Trim();

        if (string.IsNullOrEmpty(persistentId))
            persistentId = System.Guid.NewGuid().ToString("N");
    }
}
