using PlayerProgression.Data;

namespace PlayerProgression.Interfaces
{
    public interface IPlayerProgressionSystem
    {
        void Initialize(string playerId, PlayerProgressionConfig config);
        void Update(float deltaTime);
        void ProcessEvent(ProgressionEvent progressEvent);
        
        StatValue GetStatValue(string statId);
        float GetSkillLevel(string skillId);
        float GetReputationScore(string contextId, string traitId = "");
        
        ProgressionSaveData GenerateSaveData();
        void RestoreFromSaveData(ProgressionSaveData saveData);
    }
}