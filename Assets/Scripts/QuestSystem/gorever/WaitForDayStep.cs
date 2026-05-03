using System;
using UnityEngine;

/// <summary>
/// WaitForDayStep, mutlak gun ya da gorev aktif olduktan sonra gecmesi gereken gun sayisini bekler.
/// </summary>
[Serializable]
public class WaitForDayStep : IQuestStep
{
    public enum WaitMode
    {
        RelativeToActivation,
        AbsoluteDay
    }

    public WaitMode waitMode = WaitMode.RelativeToActivation;
    public int daysToWait = 1;
    public int targetDay = 2;
    public string waitId = Guid.NewGuid().ToString("N");

    public string GetName()
    {
        return waitMode switch
        {
            WaitMode.AbsoluteDay => $"Wait until day {Mathf.Max(1, targetDay)}",
            _ => $"Wait {Mathf.Max(0, daysToWait)} day(s)"
        };
    }

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        if (GameStateTracker.Instance == null)
        {
            return false;
        }

        int currentDay = GetCurrentDay();

        if (waitMode == WaitMode.AbsoluteDay)
        {
            return currentDay >= Mathf.Max(1, targetDay);
        }

        if (daysToWait <= 0)
        {
            return true;
        }

        string startDayKey = GetStartDayKey();
        if (!GameStateTracker.Instance.HasKey(startDayKey))
        {
            GameStateTracker.Instance.SetCount(startDayKey, currentDay);
            return false;
        }

        int startDay = GameStateTracker.Instance.GetCount(startDayKey);
        return currentDay >= startDay + daysToWait;
    }

    public int GetTargetDay()
    {
        if (waitMode == WaitMode.AbsoluteDay)
        {
            return Mathf.Max(1, targetDay);
        }

        if (GameStateTracker.Instance == null || !GameStateTracker.Instance.HasKey(GetStartDayKey()))
        {
            return GetCurrentDay() + Mathf.Max(0, daysToWait);
        }

        return GameStateTracker.Instance.GetCount(GetStartDayKey()) + Mathf.Max(0, daysToWait);
    }

    public int GetCurrentDay()
    {
        if (GameTime.Instance != null)
        {
            return Mathf.Max(1, GameTime.Instance.dayCount);
        }

        return Mathf.Max(1, PlayerPrefs.GetInt("DayCount", 1));
    }

    private string GetStartDayKey()
    {
        EnsureWaitId();
        return $"WaitStartDay_{waitId}";
    }

    private void EnsureWaitId()
    {
        if (string.IsNullOrWhiteSpace(waitId))
        {
            waitId = Guid.NewGuid().ToString("N");
        }
    }
}
