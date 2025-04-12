using System.Collections.Generic;

namespace PlayerProgression.Data
{
    [System.Serializable]
    public class ProgressionEvent
    {
        public ProgressionEventType type;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
        
        public enum ProgressionEventType
        {
            StatChange,
            SkillExperience,
            ReputationImpact,
            UnlockAchievement,
            CompleteAction
        }
    }
}