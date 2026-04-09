using UnityEngine;

/// <summary>
/// Tools sinifi, kayit sistemiyle ilgili davranisi yonetir.
/// </summary>
public class Tools : MonoBehaviour
{
    public float duration;
    public string itemName;
    public int price;
    public int amount;
    public int sellPrice => price / 2;

    [Tooltip("Kalýcý benzersiz ID (prefabda boþ býrak).")]
    public string persistentId;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(itemName))
            itemName = gameObject.name.Replace("(Clone)", "").Trim();

        if (string.IsNullOrEmpty(persistentId))
            persistentId = System.Guid.NewGuid().ToString("N");
    }
}
