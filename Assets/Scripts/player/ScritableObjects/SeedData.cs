    using UnityEngine;

    [CreateAssetMenu(fileName = "Seed Data", menuName = "Seed Data", order = 51)]
    public class SeedData : ScriptableObject
    {
        public SeedType seedType;
        
        [Tooltip("Her growth stage için prefab'lar (0 = ekildiği an, 1..n büyüme aşamaları)")]
        public GameObject[] growthStages;

        [Tooltip("Sulama yapılmazsa kurumasına izin verilen maksimum gün sayısı")]
        public int maxDryDays = 2;

        [Tooltip("Market satış fiyatı")]
        public int sellPrice;

        // İleride eklenecek tohum özellikleri...
    }
