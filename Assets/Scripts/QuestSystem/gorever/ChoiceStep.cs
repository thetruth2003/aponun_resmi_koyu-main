using System;

/// <summary>
/// ChoiceStep, belirli bir secim anahtarina yazilan degeri bekleyen gorev adimidir.
/// </summary>
[Serializable]
public class ChoiceStep : IQuestStep
{
    public string choiceKey;
    public string expectedValue;
    public bool completeOnAnyValue = true;

    public string GetName()
    {
        if (string.IsNullOrWhiteSpace(choiceKey))
        {
            return "Wait for choice";
        }

        if (completeOnAnyValue || string.IsNullOrWhiteSpace(expectedValue))
        {
            return $"Make choice: {choiceKey}";
        }

        return $"Choose {expectedValue} for {choiceKey}";
    }

    public void OnStart() { }

    public void OnUpdate() { }

    public bool IsComplete()
    {
        if (GameStateTracker.Instance == null || string.IsNullOrWhiteSpace(choiceKey))
        {
            return false;
        }

        string currentValue = GameStateTracker.Instance.GetString(GetChoiceStateKey());
        if (string.IsNullOrWhiteSpace(currentValue))
        {
            return false;
        }

        if (completeOnAnyValue || string.IsNullOrWhiteSpace(expectedValue))
        {
            return true;
        }

        return string.Equals(
            NormalizeId(currentValue),
            NormalizeId(expectedValue),
            StringComparison.Ordinal);
    }

    public string GetChoiceStateKey()
    {
        return $"Choice_{NormalizeId(choiceKey)}";
    }

    public string GetCurrentValue()
    {
        if (GameStateTracker.Instance == null || string.IsNullOrWhiteSpace(choiceKey))
        {
            return string.Empty;
        }

        return GameStateTracker.Instance.GetString(GetChoiceStateKey());
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
