using PlayerProgression.Data;

namespace PlayerProgression.Interfaces
{
    public interface IStatSystem
    {
        void Initialize(string playerId, PlayerProgressionConfig config);
        void Update(float deltaTime);
        void ProcessEvent(ProgressionEvent evt);
        StatValue GetStatValue(string statId);
        float GetStatBaseValue(string statId);
        void ApplyModifier(string statId, PlayerStats.StatModifier modifier);
        void RemoveModifiersFromSource(string statId, string source);
        StatSystemSaveData GenerateSaveData();
        void RestoreFromSaveData(StatSystemSaveData saveData);
    }
}