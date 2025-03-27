using PlayerProgression.Data;

namespace PlayerProgression.Interfaces
{
    public interface IReputationSystem
    {
        void Initialize(string playerId, PlayerProgressionConfig config);
        void Update(float deltaTime);
        void ProcessEvent(ProgressionEvent evt);
        float GetReputationScore(string contextId, string traitId = "");
        void AddReputationEvent(string contextId, SocialStandingSystem.ReputationEvent evt);
        ReputationSystemSaveData GenerateSaveData();
        void RestoreFromSaveData(ReputationSystemSaveData saveData);
    }
}