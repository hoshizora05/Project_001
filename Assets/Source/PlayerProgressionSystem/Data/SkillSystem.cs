using System.Collections.Generic;

namespace PlayerProgression.Data
{
    [System.Serializable]
    public class SkillSystem
    {
        public string playerId;
        public Dictionary<string, SkillCategory> categories = new Dictionary<string, SkillCategory>();
        
        [System.Serializable]
        public class SkillCategory
        {
            public string categoryId;
            public string categoryName;
            public Dictionary<string, Skill> skills = new Dictionary<string, Skill>();

            public SkillCategory(string id, string name)
            {
                categoryId = id;
                categoryName = name;
            }
        }
        
        [System.Serializable]
        public class Skill
        {
            public string skillId;
            public string skillName;
            public float level;
            public float experience;
            public float nextLevelThreshold;
            public List<SkillEffect> effects = new List<SkillEffect>();
            public List<SkillRequirement> requirements = new List<SkillRequirement>();

            public Skill(string id, string name, float threshold)
            {
                skillId = id;
                skillName = name;
                level = 0;
                experience = 0;
                nextLevelThreshold = threshold;
            }
        }
        
        [System.Serializable]
        public class SkillEffect
        {
            public string targetStat;
            public float effectValue;
            public EffectType type;

            public SkillEffect(string target, float value, EffectType effectType)
            {
                targetStat = target;
                effectValue = value;
                type = effectType;
            }
        }
        
        public enum EffectType
        {
            StatBoost,
            UnlockAction,
            SpecialAbility
        }
        
        [System.Serializable]
        public class SkillRequirement
        {
            public RequirementType type;
            public string targetId;
            public float requiredValue;

            public SkillRequirement(RequirementType reqType, string target, float value)
            {
                type = reqType;
                targetId = target;
                requiredValue = value;
            }
        }
        
        public enum RequirementType
        {
            StatMinimum,
            SkillLevel,
            CompletedAction
        }
    }
}