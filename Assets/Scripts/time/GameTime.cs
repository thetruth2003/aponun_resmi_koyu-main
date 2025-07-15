using UnityEngine;
using System;

public class GameTime : MonoBehaviour
{
    public static GameTime Instance;

    public int currentDay = 1;
    public float secondsPerDay = 60f; // 1 gün = 60 saniye
    private float timer = 0f;

    public event Action OnNewDay;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= secondsPerDay)
        {
            timer = 0f;
            currentDay++;
            Debug.Log("Yeni gün: " + currentDay);
            OnNewDay?.Invoke(); // Abonelere haber ver (örneğin tarlalar)
        }
    }
}
