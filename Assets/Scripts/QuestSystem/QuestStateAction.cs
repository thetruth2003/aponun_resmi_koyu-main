using UnityEngine;

/// <summary>
/// QuestStateAction, buton, trigger, cutscene event ya da inspector uzerinden quest state degistirmek icin kullanilir.
/// </summary>
public class QuestStateAction : MonoBehaviour
{
    public enum ActionType
    {
        SetFlag,
        SetChoice,
        AddCount,
        SetCount,
        SetString,
        ClearKey,
        SpendMoneyForKey,
        AddMoney,
        RecordBoughtItem,
        RecordSoldItem,
        RecordHarvestedItem
    }

    [Header("Action")]
    public ActionType actionType;

    [Header("Shared Data")]
    public string key;
    public int amount = 1;
    public bool boolValue = true;
    public string stringValue = string.Empty;

    [Header("Debug")]
    public bool logResult = true;

    public void InvokeAction()
    {
        bool success = Execute();

        if (logResult)
        {
            Debug.Log($"[QuestStateAction] {name} -> {actionType} ({success})");
        }
    }

    public bool Execute()
    {
        if (actionType == ActionType.AddMoney)
        {
            QuestEconomyBridge.AddMoney(amount);
            return true;
        }

        if (actionType == ActionType.SpendMoneyForKey)
        {
            if (!QuestEconomyBridge.TrySpendMoney(amount))
            {
                Debug.LogWarning($"[QuestStateAction] Not enough money for '{key}'.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(key) && GameStateTracker.Instance != null)
            {
                GameStateTracker.Instance.IncrementCount(GetSpentKey(key), amount);
            }

            return true;
        }

        if (GameStateTracker.Instance == null)
        {
            Debug.LogWarning("[QuestStateAction] GameStateTracker.Instance is missing.");
            return false;
        }

        switch (actionType)
        {
            case ActionType.SetFlag:
                GameStateTracker.Instance.SetFlag(SanitizeKey(key), boolValue);
                return true;

            case ActionType.SetChoice:
                if (string.IsNullOrWhiteSpace(key))
                {
                    return false;
                }

                GameStateTracker.Instance.SetString(GetChoiceKey(key), stringValue ?? string.Empty);
                return true;

            case ActionType.AddCount:
                GameStateTracker.Instance.IncrementCount(SanitizeKey(key), amount);
                return true;

            case ActionType.SetCount:
                GameStateTracker.Instance.SetCount(SanitizeKey(key), amount);
                return true;

            case ActionType.SetString:
                GameStateTracker.Instance.SetString(SanitizeKey(key), stringValue ?? string.Empty);
                return true;

            case ActionType.ClearKey:
                GameStateTracker.Instance.ClearKey(SanitizeKey(key));
                return true;

            case ActionType.RecordBoughtItem:
                GameStateTracker.Instance.IncrementCount(GetItemKey("Bought", key), amount);
                return true;

            case ActionType.RecordSoldItem:
                GameStateTracker.Instance.IncrementCount(GetItemKey("Sold", key), amount);
                return true;

            case ActionType.RecordHarvestedItem:
                GameStateTracker.Instance.IncrementCount(GetItemKey("Harvested", key), amount);
                return true;
        }

        return false;
    }

    private static string GetChoiceKey(string value)
    {
        return $"Choice_{NormalizeId(value)}";
    }

    private static string GetSpentKey(string value)
    {
        return $"Spent_{NormalizeId(value)}";
    }

    private static string GetItemKey(string prefix, string value)
    {
        return $"{prefix}_{NormalizeId(value)}";
    }

    private static string SanitizeKey(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}

internal static class QuestEconomyBridge
{
    public static bool TrySpendMoney(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (TryGetMuhasebeci(out Muhasebeci muhasebeci))
        {
            if (muhasebeci.GetMoney() < amount)
            {
                return false;
            }

            muhasebeci.SetMoney(muhasebeci.GetMoney() - amount);
            return true;
        }

        if (Money.Instance != null)
        {
            return Money.Instance.SpendMoney(amount);
        }

        return false;
    }

    public static void AddMoney(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        if (TryGetMuhasebeci(out Muhasebeci muhasebeci))
        {
            muhasebeci.AddMoney(amount);
            return;
        }

        if (Money.Instance == null)
        {
            return;
        }

        if (amount > 0)
        {
            Money.Instance.AddMoney(amount);
        }
        else
        {
            Money.Instance.SpendMoney(-amount);
        }
    }

    private static bool TryGetMuhasebeci(out Muhasebeci muhasebeci)
    {
        muhasebeci = Object.FindFirstObjectByType<Muhasebeci>();
        return muhasebeci != null;
    }
}
