using System.Collections.Generic;
using PlayerProgression.Data;

namespace PlayerProgression.Interfaces
{
    public interface ISkillSystem
    {
        void Initialize(string playerId, PlayerProgressionConfig config);
        void Update(float deltaTime);
        void ProcessEvent(ProgressionEvent evt);
        float GetSkillLevel(string skillId);
        float GetSkillExperience(string skillId);
        void AddExperience(string skillId, float amount);
        void SetExperience(string skillId, float value);
        bool CheckRequirements(string skillId);
        List<SkillSystem.SkillEffect> GetSkillEffects(string skillId);
        SkillSystemSaveData GenerateSaveData();
        void RestoreFromSaveData(SkillSystemSaveData saveData);
    }
}