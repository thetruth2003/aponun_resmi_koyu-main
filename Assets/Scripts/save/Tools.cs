using UnityEngine;

public class Tools : MonoBehaviour
{
    public float duration;
    public string itemName;
    public int price;
    public int amount;
    public int sellPrice => price / 2;

    [Tooltip("Kalıcı benzersiz ID (prefabda boş bırak).")]
    public string persistentId;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(itemName))
            itemName = gameObject.name.Replace("(Clone)", "").Trim();

        if (string.IsNullOrEmpty(persistentId))
            persistentId = System.Guid.NewGuid().ToString("N");
    }
}
