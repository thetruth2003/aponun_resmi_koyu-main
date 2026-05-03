using System;

/// <summary>
/// ModifyStatStep, aktif oldugu anda bir kez state uzerinde degisiklik yapip tamamlanan gorev adimidir.
/// </summary>
[Serializable]
public class ModifyStatStep : IQuestStep
{
    public enum ModifyMode
    {
        AddCount,
        SetCount,
        SetFlag,
        SetString,
        ClearKey
    }

    public ModifyMode mode = ModifyMode.AddCount;
    public string statKey;
    public int intValue = 1;
    public bool boolValue = true;
    public string stringValue = string.Empty;
    public string operationId = Guid.NewGuid().ToString("N");

    public string GetName()
    {
        return mode switch
        {
            ModifyMode.AddCount => $"Add {intValue} to {statKey}",
            ModifyMode.SetCount => $"Set {statKey} to {intValue}",
            ModifyMode.SetFlag => $"Set {statKey} to {boolValue}",
            ModifyMode.SetString => $"Set {statKey} to {stringValue}",
            ModifyMode.ClearKey => $"Clear {statKey}",
            _ => "Modify state"
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

        EnsureOperationId();

        string appliedKey = GetAppliedKey();
        if (GameStateTracker.Instance.GetFlag(appliedKey))
        {
            return true;
        }

        if (mode != ModifyMode.ClearKey && string.IsNullOrWhiteSpace(statKey))
        {
            return false;
        }

        switch (mode)
        {
            case ModifyMode.AddCount:
                GameStateTracker.Instance.IncrementCount(statKey, intValue);
                break;

            case ModifyMode.SetCount:
                GameStateTracker.Instance.SetCount(statKey, intValue);
                break;

            case ModifyMode.SetFlag:
                GameStateTracker.Instance.SetFlag(statKey, boolValue);
                break;

            case ModifyMode.SetString:
                GameStateTracker.Instance.SetString(statKey, stringValue ?? string.Empty);
                break;

            case ModifyMode.ClearKey:
                if (!string.IsNullOrWhiteSpace(statKey))
                {
                    GameStateTracker.Instance.ClearKey(statKey);
                }
                break;
        }

        GameStateTracker.Instance.SetFlag(appliedKey, true);
        return true;
    }

    public string GetAppliedKey()
    {
        EnsureOperationId();
        return $"StepApplied_{operationId}";
    }

    private void EnsureOperationId()
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            operationId = Guid.NewGuid().ToString("N");
        }
    }
}
