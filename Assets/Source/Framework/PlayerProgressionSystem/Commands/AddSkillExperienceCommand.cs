using PlayerProgression.Interfaces;

namespace PlayerProgression.Commands
{
    public class AddSkillExperienceCommand : IProgressionCommand
    {
        private readonly ISkillSystem skillSystem;
        private readonly string skillId;
        private readonly float experienceAmount;
        private float previousExperience;
        
        public AddSkillExperienceCommand(ISkillSystem system, string id, float amount)
        {
            skillSystem = system;
            skillId = id;
            experienceAmount = amount;
        }
        
        public void Execute()
        {
            previousExperience = skillSystem.GetSkillExperience(skillId);
            skillSystem.AddExperience(skillId, experienceAmount);
        }
        
        public void Undo()
        {
            skillSystem.SetExperience(skillId, previousExperience);
        }
    }
}