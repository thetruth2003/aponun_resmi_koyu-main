using UnityEngine;
using System;

public class GameTime : MonoBehaviour
{
    public static GameTime Instance;
    public int dayCount = 1;

    public delegate void NewDayAction();
    public event NewDayAction OnNewDay;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TriggerNewDay()
    {
        dayCount++;
        PlayerPrefs.SetInt("DayCount", dayCount);
        OnNewDay?.Invoke();
    }

    private void Start()
    {
        dayCount = PlayerPrefs.GetInt("DayCount", 1);
    }
}

