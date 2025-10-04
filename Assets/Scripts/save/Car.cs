using UnityEngine;

public class Car : MonoBehaviour
{
    public float duration;
    public string car_name;
    public int price;
    public float Fuel;

    [Tooltip("Kalıcı benzersiz ID (prefabda boş bırak).")]
    public string persistentId;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(car_name))
            car_name = gameObject.name.Replace("(Clone)", "").Trim();

        if (string.IsNullOrEmpty(persistentId))
            persistentId = System.Guid.NewGuid().ToString("N");
    }
}
