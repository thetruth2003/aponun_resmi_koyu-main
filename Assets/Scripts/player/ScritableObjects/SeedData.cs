using UnityEngine;

/// <summary>
/// Bir tohumun buyume asamalari ve temel tarim ayarlarini tutar.
/// </summary>
[CreateAssetMenu(fileName = "Seed Data", menuName = "Seed Data", order = 51)]
public class SeedData : ScriptableObject
{
    public SeedType seedType;

    [Tooltip("Her growth stage icin prefablar. 0 = ekildigi an, 1..n = buyume asamalari.")]
    public GameObject[] growthStages;

    [Tooltip("Sulama yapilmazsa kurumasina izin verilen maksimum gun sayisi.")]
    public int maxDryDays = 2;

    [Tooltip("Market satis fiyati.")]
    public int sellPrice;
}
