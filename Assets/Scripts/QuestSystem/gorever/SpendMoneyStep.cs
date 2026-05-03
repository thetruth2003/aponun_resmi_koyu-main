using System;

/// <summary>
/// SpendMoneyStep, belirli bir odeme anahtari uzerinden biriken harcama miktarini bekler.
/// </summary>
[Serializable]
public class SpendMoneyStep : IQuestStep
{
    public string spendKey;
    public int requiredAmount = 1;

    public string GetName()
    {
        if (string.IsNullOrWhiteSpace(spendKey))
        {
            return $"Spend {requiredAmount} TL";
        }

        return $"Spend {requiredAmount} TL for {spendKey}";
    }

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        if (GameStateTracker.Instance == null)
        {
            return false;
        }

        if (requiredAmount <= 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(spendKey))
        {
            return false;
        }

        return GetCurrentProgress() >= requiredAmount;
    }

    public string GetProgressKey()
    {
        return $"Spent_{NormalizeId(spendKey)}";
    }

    public int GetCurrentProgress()
    {
        if (GameStateTracker.Instance == null || string.IsNullOrWhiteSpace(spendKey))
        {
            return 0;
        }

        return GameStateTracker.Instance.GetCount(GetProgressKey());
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
