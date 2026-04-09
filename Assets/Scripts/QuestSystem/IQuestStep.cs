/// <summary>
/// IQuestStep arayuzu, gorev adimlarinin uygulamasi gereken temel davranis sozlesmesini tanimlar.
/// </summary>
public interface IQuestStep
{
    string GetName();
    void OnStart();
    void OnUpdate();
    bool IsComplete();
}
