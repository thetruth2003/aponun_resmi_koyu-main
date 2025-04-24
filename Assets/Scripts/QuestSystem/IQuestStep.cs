public interface IQuestStep
{
    string GetName();       // "Eve git", "NPC ile konuş" gibi
    void OnStart();         // Görev başladığında çağrılır
    void OnUpdate();        // Oyun sırasında güncellenebilir
    bool IsComplete();      // Görev tamamlandı mı?
}
