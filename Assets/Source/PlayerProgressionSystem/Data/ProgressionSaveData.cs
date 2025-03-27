using System.Collections.Generic;

namespace PlayerProgression.Data
{
    [System.Serializable]
    public class ProgressionSaveData
    {
        public StatSystemSaveData statData;
        public SkillSystemSaveData skillData;
        public ReputationSystemSaveData reputationData;
    }

    [System.Serializable]
    public class StatSystemSaveData
    {
        public string playerId;
        public List<StatData> stats = new List<StatData>();
        
        [System.Serializable]
        public class StatData
        {
            public string statId;
            public float baseValue;
            public float currentValue;
            public float minValue;
            public float maxValue;
            public float growthRate;
            public List<StatModifierData> modifiers = new List<StatModifierData>();
        }
        
        [System.Serializable]
        public class StatModifierData
        {
            public string source;
            public float value;
            public PlayerStats.ModifierType type;
            public float duration;
            public float remainingTime;
        }
    }

    [System.Serializable]
    public class SkillSystemSaveData
    {
        public string playerId;
        public List<SkillCategoryData> categories = new List<SkillCategoryData>();
        
        [System.Serializable]
        public class SkillCategoryData
        {
            public string categoryId;
            public string categoryName;
            public List<SkillData> skills = new List<SkillData>();
        }
        
        [System.Serializable]
        public class SkillData
        {
            public string skillId;
            public string skillName;
            public float level;
            public float experience;
            public float nextLevelThreshold;
        }
    }

    [System.Serializable]
    public class ReputationSystemSaveData
    {
        public string playerId;
        public List<ReputationData> reputations = new List<ReputationData>();
        
        [System.Serializable]
        public class ReputationData
        {
            public string contextId;
            public string contextName;
            public float overallScore;
            public List<TraitScoreData> traitScores = new List<TraitScoreData>();
            public List<ReputationEventData> recentEvents = new List<ReputationEventData>();
        }
        
        [System.Serializable]
        public class TraitScoreData
        {
            public string traitId;
            public float score;
        }
        
        [System.Serializable]
        public class ReputationEventData
        {
            public string eventId;
            public string description;
            public List<TraitImpactData> impacts = new List<TraitImpactData>();
            public float timestamp;
            public float decayRate;
        }
        
        [System.Serializable]
        public class TraitImpactData
        {
            public string traitId;
            public float impact;
        }
    }
}