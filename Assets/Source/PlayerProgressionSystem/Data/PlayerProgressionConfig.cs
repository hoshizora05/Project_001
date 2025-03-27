using System.Collections.Generic;
using UnityEngine;

namespace PlayerProgression.Data
{
    [CreateAssetMenu(fileName = "PlayerProgressionConfig", menuName = "Systems/Player Progression Config")]
    public class PlayerProgressionConfig : ScriptableObject
    {
        [Header("Stat System")]
        public List<StatConfig> initialStats = new List<StatConfig>();
        
        [Header("Skill System")]
        public List<SkillCategoryConfig> skillCategories = new List<SkillCategoryConfig>();
        
        [Header("Reputation System")]
        public List<ReputationContextConfig> reputationContexts = new List<ReputationContextConfig>();
        
        [System.Serializable]
        public class StatConfig
        {
            public string statId;
            public string statName;
            public float baseValue;
            public float minValue;
            public float maxValue;
            public float growthRate;
        }
        
        [System.Serializable]
        public class SkillCategoryConfig
        {
            public string categoryId;
            public string categoryName;
            public List<SkillConfig> skills = new List<SkillConfig>();
        }
        
        [System.Serializable]
        public class SkillConfig
        {
            public string skillId;
            public string skillName;
            public float initialLevelThreshold;
            public float levelThresholdMultiplier;
            public List<SkillRequirementConfig> requirements = new List<SkillRequirementConfig>();
        }
        
        [System.Serializable]
        public class SkillRequirementConfig
        {
            public SkillSystem.RequirementType type;
            public string targetId;
            public float requiredValue;
        }
        
        [System.Serializable]
        public class ReputationContextConfig
        {
            public string contextId;
            public string contextName;
            public List<string> relevantTraits = new List<string>();
        }
    }
}