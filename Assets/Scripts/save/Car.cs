using UnityEngine;

/// <summary>
/// Car sinifi, kayit sistemiyle ilgili davranisi yonetir.
/// </summary>
public class Car : MonoBehaviour
{
    public float duration;
    public string car_name;
    public int price;
    public float Fuel;

    [Tooltip("Kalýcý benzersiz ID (prefabda boþ býrak).")]
    public string persistentId;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(car_name))
            car_name = gameObject.name.Replace("(Clone)", "").Trim();

        if (string.IsNullOrEmpty(persistentId))
            persistentId = System.Guid.NewGuid().ToString("N");
    }
}
