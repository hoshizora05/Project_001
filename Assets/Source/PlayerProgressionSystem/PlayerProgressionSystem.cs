using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PlayerProgression
{
    // Core data structures
    [System.Serializable]
    public class PlayerStats
    {
        public string playerId;
        public Dictionary<string, Stat> stats = new Dictionary<string, Stat>();

        [System.Serializable]
        public class Stat
        {
            public float baseValue;
            public float currentValue;
            public float minValue;
            public float maxValue;
            public float growthRate;
            public List<StatModifier> modifiers = new List<StatModifier>();

            public Stat(float baseVal, float min, float max, float growth)
            {
                baseValue = baseVal;
                currentValue = baseVal;
                minValue = min;
                maxValue = max;
                growthRate = growth;
            }
        }

        [System.Serializable]
        public class StatModifier
        {
            public string source;
            public float value;
            public ModifierType type;
            public float duration;
            public float remainingTime;

            public StatModifier(string src, float val, ModifierType modType, float dur)
            {
                source = src;
                value = val;
                type = modType;
                duration = dur;
                remainingTime = dur;
            }
        }

        public enum ModifierType
        {
            Additive,
            Multiplicative,
            Override
        }
    }

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

    [System.Serializable]
    public class SocialStandingSystem
    {
        public string playerId;
        public Dictionary<string, Reputation> reputations = new Dictionary<string, Reputation>();

        [System.Serializable]
        public class Reputation
        {
            public string contextId;
            public string contextName;
            public float overallScore;
            public Dictionary<string, float> traitScores = new Dictionary<string, float>();
            public List<ReputationEvent> recentEvents = new List<ReputationEvent>();

            public Reputation(string id, string name)
            {
                contextId = id;
                contextName = name;
                overallScore = 0;
            }
        }

        [System.Serializable]
        public class ReputationEvent
        {
            public string eventId;
            public string description;
            public Dictionary<string, float> impacts = new Dictionary<string, float>();
            public float timestamp;
            public float decayRate;

            public ReputationEvent(string id, string desc, float time, float decay)
            {
                eventId = id;
                description = desc;
                timestamp = time;
                decayRate = decay;
            }
        }

        [System.Serializable]
        public class SocialLabel
        {
            public string labelId;
            public string labelName;
            public Dictionary<string, float> thresholds = new Dictionary<string, float>();
            public List<string> effects = new List<string>();
            public bool isActive;

            public SocialLabel(string id, string name)
            {
                labelId = id;
                labelName = name;
                isActive = false;
            }
        }
    }

    // System interface
    public interface IPlayerProgressionSystem
    {
        void Initialize(string playerId, PlayerProgressionConfig config);
        void UpdateProgression(float deltaTime);
        void ProcessEvent(ProgressionEvent progressEvent);

        StatValue GetStatValue(string statId);
        float GetSkillLevel(string skillId);
        float GetReputationScore(string contextId, string traitId = "");

        ProgressionSaveData GenerateSaveData();
        void RestoreFromSaveData(ProgressionSaveData saveData);
    }

    // Value object for stat queries
    public struct StatValue
    {
        public float BaseValue;
        public float CurrentValue;
        public float MinValue;
        public float MaxValue;

        public StatValue(float baseVal, float current, float min, float max)
        {
            BaseValue = baseVal;
            CurrentValue = current;
            MinValue = min;
            MaxValue = max;
        }
    }

    // Event definition for loose coupling
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

    // Configuration scriptable object
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

    // Event Bus
    [CreateAssetMenu(fileName = "EventBus", menuName = "Systems/Event Bus")]
    public class EventBusReference : ScriptableObject
    {
        private Dictionary<Type, List<object>> subscribers = new Dictionary<Type, List<object>>();

        public void Subscribe<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (!subscribers.ContainsKey(type))
            {
                subscribers[type] = new List<object>();
            }
            subscribers[type].Add(callback);
        }

        public void Unsubscribe<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (subscribers.ContainsKey(type))
            {
                subscribers[type].Remove(callback);
            }
        }

        public void Publish<T>(T eventData)
        {
            var type = typeof(T);
            if (subscribers.ContainsKey(type))
            {
                foreach (var subscriber in subscribers[type].ToList())
                {
                    ((Action<T>)subscriber).Invoke(eventData);
                }
            }
        }
    }

    // Save data containers
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

    // System interfaces for dependency injection
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

    // Implementation classes
    public class StatSystem : IStatSystem
    {
        private PlayerStats playerStats = new PlayerStats();

        public void Initialize(string playerId, PlayerProgressionConfig config)
        {
            playerStats.playerId = playerId;

            foreach (var statConfig in config.initialStats)
            {
                playerStats.stats[statConfig.statId] = new PlayerStats.Stat(
                    statConfig.baseValue,
                    statConfig.minValue,
                    statConfig.maxValue,
                    statConfig.growthRate
                );
            }
        }

        public void Update(float deltaTime)
        {
            foreach (var statPair in playerStats.stats)
            {
                var stat = statPair.Value;

                // Update modifiers and remove expired ones
                for (int i = stat.modifiers.Count - 1; i >= 0; i--)
                {
                    var mod = stat.modifiers[i];
                    if (mod.duration > 0)
                    {
                        mod.remainingTime -= deltaTime;
                        if (mod.remainingTime <= 0)
                        {
                            stat.modifiers.RemoveAt(i);
                        }
                    }
                }

                // Recalculate current value
                RecalculateStatValue(statPair.Key);
            }
        }

        public void ProcessEvent(ProgressionEvent evt)
        {
            if (evt.type == ProgressionEvent.ProgressionEventType.StatChange)
            {
                string statId = (string)evt.parameters["statId"];

                if (evt.parameters.TryGetValue("baseValueChange", out object baseValueChangeObj))
                {
                    float baseValueChange = Convert.ToSingle(baseValueChangeObj);
                    if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
                    {
                        stat.baseValue = Mathf.Clamp(stat.baseValue + baseValueChange, stat.minValue, stat.maxValue);
                        RecalculateStatValue(statId);
                    }
                }

                if (evt.parameters.TryGetValue("modifier", out object modifierObj))
                {
                    PlayerStats.StatModifier modifier = (PlayerStats.StatModifier)modifierObj;
                    ApplyModifier(statId, modifier);
                }
            }
        }

        public StatValue GetStatValue(string statId)
        {
            if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
            {
                return new StatValue(stat.baseValue, stat.currentValue, stat.minValue, stat.maxValue);
            }

            return new StatValue(0, 0, 0, 0);
        }

        public float GetStatBaseValue(string statId)
        {
            if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
            {
                return stat.baseValue;
            }

            return 0;
        }

        public void ApplyModifier(string statId, PlayerStats.StatModifier modifier)
        {
            if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
            {
                stat.modifiers.Add(modifier);
                RecalculateStatValue(statId);
            }
        }

        public void RemoveModifiersFromSource(string statId, string source)
        {
            if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
            {
                stat.modifiers.RemoveAll(mod => mod.source == source);
                RecalculateStatValue(statId);
            }
        }

        private void RecalculateStatValue(string statId)
        {
            if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
            {
                float baseValue = stat.baseValue;
                float additiveTotal = 0;
                float multiplicativeTotal = 1.0f;
                float overrideValue = float.MinValue;

                foreach (var mod in stat.modifiers)
                {
                    switch (mod.type)
                    {
                        case PlayerStats.ModifierType.Additive:
                            additiveTotal += mod.value;
                            break;
                        case PlayerStats.ModifierType.Multiplicative:
                            multiplicativeTotal *= (1.0f + mod.value);
                            break;
                        case PlayerStats.ModifierType.Override:
                            overrideValue = Mathf.Max(overrideValue, mod.value);
                            break;
                    }
                }

                if (overrideValue > float.MinValue)
                {
                    stat.currentValue = Mathf.Clamp(overrideValue, stat.minValue, stat.maxValue);
                }
                else
                {
                    stat.currentValue = Mathf.Clamp((baseValue + additiveTotal) * multiplicativeTotal, stat.minValue, stat.maxValue);
                }
            }
        }

        public StatSystemSaveData GenerateSaveData()
        {
            var saveData = new StatSystemSaveData
            {
                playerId = playerStats.playerId
            };

            foreach (var statPair in playerStats.stats)
            {
                var statData = new StatSystemSaveData.StatData
                {
                    statId = statPair.Key,
                    baseValue = statPair.Value.baseValue,
                    currentValue = statPair.Value.currentValue,
                    minValue = statPair.Value.minValue,
                    maxValue = statPair.Value.maxValue,
                    growthRate = statPair.Value.growthRate
                };

                foreach (var mod in statPair.Value.modifiers)
                {
                    statData.modifiers.Add(new StatSystemSaveData.StatModifierData
                    {
                        source = mod.source,
                        value = mod.value,
                        type = mod.type,
                        duration = mod.duration,
                        remainingTime = mod.remainingTime
                    });
                }

                saveData.stats.Add(statData);
            }

            return saveData;
        }

        public void RestoreFromSaveData(StatSystemSaveData saveData)
        {
            playerStats.playerId = saveData.playerId;
            playerStats.stats.Clear();

            foreach (var statData in saveData.stats)
            {
                var stat = new PlayerStats.Stat(
                    statData.baseValue,
                    statData.minValue,
                    statData.maxValue,
                    statData.growthRate
                );

                stat.currentValue = statData.currentValue;

                foreach (var modData in statData.modifiers)
                {
                    stat.modifiers.Add(new PlayerStats.StatModifier(
                        modData.source,
                        modData.value,
                        modData.type,
                        modData.duration
                    )
                    {
                        remainingTime = modData.remainingTime
                    });
                }

                playerStats.stats[statData.statId] = stat;
            }
        }
    }

    public class SkillProgressionSystem : ISkillSystem
    {
        private SkillSystem skillSystem = new SkillSystem();
        private Dictionary<string, string> skillIdToCategoryId = new Dictionary<string, string>();

        public void Initialize(string playerId, PlayerProgressionConfig config)
        {
            skillSystem.playerId = playerId;

            foreach (var categoryConfig in config.skillCategories)
            {
                var category = new SkillSystem.SkillCategory(categoryConfig.categoryId, categoryConfig.categoryName);

                foreach (var skillConfig in categoryConfig.skills)
                {
                    var skill = new SkillSystem.Skill(
                        skillConfig.skillId,
                        skillConfig.skillName,
                        skillConfig.initialLevelThreshold
                    );

                    foreach (var reqConfig in skillConfig.requirements)
                    {
                        skill.requirements.Add(new SkillSystem.SkillRequirement(
                            reqConfig.type,
                            reqConfig.targetId,
                            reqConfig.requiredValue
                        ));
                    }

                    category.skills[skillConfig.skillId] = skill;
                    skillIdToCategoryId[skillConfig.skillId] = categoryConfig.categoryId;
                }

                skillSystem.categories[categoryConfig.categoryId] = category;
            }
        }

        public void Update(float deltaTime)
        {
            // Time-based skill degradation or passive growth could be implemented here
        }

        public void ProcessEvent(ProgressionEvent evt)
        {
            if (evt.type == ProgressionEvent.ProgressionEventType.SkillExperience)
            {
                string skillId = (string)evt.parameters["skillId"];
                float experienceAmount = Convert.ToSingle(evt.parameters["experienceAmount"]);

                AddExperience(skillId, experienceAmount);
            }
            else if (evt.type == ProgressionEvent.ProgressionEventType.CompleteAction)
            {
                // Handle actions that might affect skills
                string actionId = (string)evt.parameters["actionId"];

                // Example: Apply experience to relevant skills based on the action
                if (evt.parameters.TryGetValue("relevantSkills", out object relevantSkillsObj))
                {
                    var relevantSkills = (Dictionary<string, float>)relevantSkillsObj;
                    foreach (var skillPair in relevantSkills)
                    {
                        AddExperience(skillPair.Key, skillPair.Value);
                    }
                }
            }
        }

        public float GetSkillLevel(string skillId)
        {
            if (skillIdToCategoryId.TryGetValue(skillId, out string categoryId))
            {
                if (skillSystem.categories.TryGetValue(categoryId, out SkillSystem.SkillCategory category))
                {
                    if (category.skills.TryGetValue(skillId, out SkillSystem.Skill skill))
                    {
                        return skill.level;
                    }
                }
            }

            return 0;
        }

        public float GetSkillExperience(string skillId)
        {
            if (skillIdToCategoryId.TryGetValue(skillId, out string categoryId))
            {
                if (skillSystem.categories.TryGetValue(categoryId, out SkillSystem.SkillCategory category))
                {
                    if (category.skills.TryGetValue(skillId, out SkillSystem.Skill skill))
                    {
                        return skill.experience;
                    }
                }
            }

            return 0;
        }

        public void AddExperience(string skillId, float amount)
        {
            if (skillIdToCategoryId.TryGetValue(skillId, out string categoryId))
            {
                if (skillSystem.categories.TryGetValue(categoryId, out SkillSystem.SkillCategory category))
                {
                    if (category.skills.TryGetValue(skillId, out SkillSystem.Skill skill))
                    {
                        skill.experience += amount;

                        // Check for level up
                        while (skill.experience >= skill.nextLevelThreshold)
                        {
                            skill.experience -= skill.nextLevelThreshold;
                            skill.level += 1;

                            // Increase next level threshold (could be customized based on skill config)
                            skill.nextLevelThreshold *= 1.5f;
                        }
                    }
                }
            }
        }

        public void SetExperience(string skillId, float value)
        {
            if (skillIdToCategoryId.TryGetValue(skillId, out string categoryId))
            {
                if (skillSystem.categories.TryGetValue(categoryId, out SkillSystem.SkillCategory category))
                {
                    if (category.skills.TryGetValue(skillId, out SkillSystem.Skill skill))
                    {
                        skill.experience = value;
                    }
                }
            }
        }

        public bool CheckRequirements(string skillId)
        {
            if (skillIdToCategoryId.TryGetValue(skillId, out string categoryId))
            {
                if (skillSystem.categories.TryGetValue(categoryId, out SkillSystem.SkillCategory category))
                {
                    if (category.skills.TryGetValue(skillId, out SkillSystem.Skill skill))
                    {
                        foreach (var req in skill.requirements)
                        {
                            switch (req.type)
                            {
                                case SkillSystem.RequirementType.StatMinimum:
                                    // This would need a reference to the stat system
                                    break;
                                case SkillSystem.RequirementType.SkillLevel:
                                    if (GetSkillLevel(req.targetId) < req.requiredValue)
                                    {
                                        return false;
                                    }
                                    break;
                                case SkillSystem.RequirementType.CompletedAction:
                                    // This would need a reference to an action tracking system
                                    break;
                            }
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public List<SkillSystem.SkillEffect> GetSkillEffects(string skillId)
        {
            if (skillIdToCategoryId.TryGetValue(skillId, out string categoryId))
            {
                if (skillSystem.categories.TryGetValue(categoryId, out SkillSystem.SkillCategory category))
                {
                    if (category.skills.TryGetValue(skillId, out SkillSystem.Skill skill))
                    {
                        return skill.effects;
                    }
                }
            }

            return new List<SkillSystem.SkillEffect>();
        }

        public SkillSystemSaveData GenerateSaveData()
        {
            var saveData = new SkillSystemSaveData
            {
                playerId = skillSystem.playerId
            };

            foreach (var categoryPair in skillSystem.categories)
            {
                var categoryData = new SkillSystemSaveData.SkillCategoryData
                {
                    categoryId = categoryPair.Value.categoryId,
                    categoryName = categoryPair.Value.categoryName
                };

                foreach (var skillPair in categoryPair.Value.skills)
                {
                    var skillData = new SkillSystemSaveData.SkillData
                    {
                        skillId = skillPair.Value.skillId,
                        skillName = skillPair.Value.skillName,
                        level = skillPair.Value.level,
                        experience = skillPair.Value.experience,
                        nextLevelThreshold = skillPair.Value.nextLevelThreshold
                    };

                    categoryData.skills.Add(skillData);
                }

                saveData.categories.Add(categoryData);
            }

            return saveData;
        }

        public void RestoreFromSaveData(SkillSystemSaveData saveData)
        {
            skillSystem.playerId = saveData.playerId;
            skillSystem.categories.Clear();
            skillIdToCategoryId.Clear();

            foreach (var categoryData in saveData.categories)
            {
                var category = new SkillSystem.SkillCategory(
                    categoryData.categoryId,
                    categoryData.categoryName
                );

                foreach (var skillData in categoryData.skills)
                {
                    var skill = new SkillSystem.Skill(
                        skillData.skillId,
                        skillData.skillName,
                        skillData.nextLevelThreshold
                    );

                    skill.level = skillData.level;
                    skill.experience = skillData.experience;

                    category.skills[skillData.skillId] = skill;
                    skillIdToCategoryId[skillData.skillId] = categoryData.categoryId;
                }

                skillSystem.categories[categoryData.categoryId] = category;
            }
        }
    }

    public class ReputationManager : IReputationSystem
    {
        private SocialStandingSystem socialSystem = new SocialStandingSystem();
        private Dictionary<string, Dictionary<string, SocialStandingSystem.SocialLabel>> contextLabels =
            new Dictionary<string, Dictionary<string, SocialStandingSystem.SocialLabel>>();

        public void Initialize(string playerId, PlayerProgressionConfig config)
        {
            socialSystem.playerId = playerId;

            foreach (var contextConfig in config.reputationContexts)
            {
                var reputation = new SocialStandingSystem.Reputation(
                    contextConfig.contextId,
                    contextConfig.contextName
                );

                foreach (var trait in contextConfig.relevantTraits)
                {
                    reputation.traitScores[trait] = 0;
                }

                socialSystem.reputations[contextConfig.contextId] = reputation;
            }
        }

        public void Update(float deltaTime)
        {
            // Update reputation event decay
            foreach (var reputationPair in socialSystem.reputations)
            {
                var reputation = reputationPair.Value;

                // Process reputation event decay
                for (int i = reputation.recentEvents.Count - 1; i >= 0; i--)
                {
                    var evt = reputation.recentEvents[i];
                    bool eventExpired = true;

                    foreach (var impactPair in evt.impacts)
                    {
                        float decayAmount = evt.decayRate * deltaTime;

                        if (Mathf.Abs(impactPair.Value) > decayAmount)
                        {
                            // Event still has impact
                            float newImpact = impactPair.Value > 0 ?
                                impactPair.Value - decayAmount :
                                impactPair.Value + decayAmount;

                            evt.impacts[impactPair.Key] = newImpact;
                            eventExpired = false;
                        }
                        else
                        {
                            // Impact has decayed to zero
                            evt.impacts[impactPair.Key] = 0;
                        }
                    }

                    if (eventExpired)
                    {
                        reputation.recentEvents.RemoveAt(i);
                    }
                }

                // Recalculate reputation scores
                RecalculateReputationScores(reputationPair.Key);
            }

            // Update social labels
            foreach (var contextPair in contextLabels)
            {
                string contextId = contextPair.Key;

                if (socialSystem.reputations.TryGetValue(contextId, out SocialStandingSystem.Reputation reputation))
                {
                    foreach (var labelPair in contextPair.Value)
                    {
                        var label = labelPair.Value;
                        bool shouldBeActive = true;

                        foreach (var thresholdPair in label.thresholds)
                        {
                            string traitId = thresholdPair.Key;
                            float requiredValue = thresholdPair.Value;

                            if (reputation.traitScores.TryGetValue(traitId, out float traitScore))
                            {
                                if (traitScore < requiredValue)
                                {
                                    shouldBeActive = false;
                                    break;
                                }
                            }
                            else
                            {
                                shouldBeActive = false;
                                break;
                            }
                        }

                        label.isActive = shouldBeActive;
                    }
                }
            }
        }

        public void ProcessEvent(ProgressionEvent evt)
        {
            if (evt.type == ProgressionEvent.ProgressionEventType.ReputationImpact)
            {
                string contextId = (string)evt.parameters["contextId"];

                if (evt.parameters.TryGetValue("reputationEvent", out object reputationEventObj))
                {
                    var reputationEvent = (SocialStandingSystem.ReputationEvent)reputationEventObj;
                    AddReputationEvent(contextId, reputationEvent);
                }
                else if (evt.parameters.TryGetValue("traitImpacts", out object traitImpactsObj))
                {
                    var traitImpacts = (Dictionary<string, float>)traitImpactsObj;
                    string eventId = evt.parameters.TryGetValue("eventId", out object eventIdObj) ?
                        (string)eventIdObj : "generic_event_" + DateTime.Now.Ticks;

                    string description = evt.parameters.TryGetValue("description", out object descriptionObj) ?
                        (string)descriptionObj : "Generic reputation event";

                    float decayRate = evt.parameters.TryGetValue("decayRate", out object decayRateObj) ?
                        Convert.ToSingle(decayRateObj) : 0.1f;

                    var reputationEvent = new SocialStandingSystem.ReputationEvent(
                        eventId,
                        description,
                        Time.time,
                        decayRate
                    );

                    foreach (var impact in traitImpacts)
                    {
                        reputationEvent.impacts[impact.Key] = impact.Value;
                    }

                    AddReputationEvent(contextId, reputationEvent);
                }
            }
        }

        public float GetReputationScore(string contextId, string traitId = "")
        {
            if (socialSystem.reputations.TryGetValue(contextId, out SocialStandingSystem.Reputation reputation))
            {
                if (string.IsNullOrEmpty(traitId))
                {
                    return reputation.overallScore;
                }
                else if (reputation.traitScores.TryGetValue(traitId, out float traitScore))
                {
                    return traitScore;
                }
            }

            return 0;
        }

        public void AddReputationEvent(string contextId, SocialStandingSystem.ReputationEvent evt)
        {
            if (socialSystem.reputations.TryGetValue(contextId, out SocialStandingSystem.Reputation reputation))
            {
                reputation.recentEvents.Add(evt);

                foreach (var impactPair in evt.impacts)
                {
                    string traitId = impactPair.Key;
                    float impactValue = impactPair.Value;

                    if (!reputation.traitScores.ContainsKey(traitId))
                    {
                        reputation.traitScores[traitId] = 0;
                    }

                    reputation.traitScores[traitId] += impactValue;
                }

                RecalculateReputationScores(contextId);
            }
        }

        private void RecalculateReputationScores(string contextId)
        {
            if (socialSystem.reputations.TryGetValue(contextId, out SocialStandingSystem.Reputation reputation))
            {
                float totalTraitScore = 0;
                int traitCount = 0;

                foreach (var traitPair in reputation.traitScores)
                {
                    totalTraitScore += traitPair.Value;
                    traitCount++;
                }

                reputation.overallScore = traitCount > 0 ? totalTraitScore / traitCount : 0;
            }
        }

        public ReputationSystemSaveData GenerateSaveData()
        {
            var saveData = new ReputationSystemSaveData
            {
                playerId = socialSystem.playerId
            };

            foreach (var reputationPair in socialSystem.reputations)
            {
                var reputationData = new ReputationSystemSaveData.ReputationData
                {
                    contextId = reputationPair.Value.contextId,
                    contextName = reputationPair.Value.contextName,
                    overallScore = reputationPair.Value.overallScore
                };

                foreach (var traitPair in reputationPair.Value.traitScores)
                {
                    reputationData.traitScores.Add(new ReputationSystemSaveData.TraitScoreData
                    {
                        traitId = traitPair.Key,
                        score = traitPair.Value
                    });
                }

                foreach (var evt in reputationPair.Value.recentEvents)
                {
                    var eventData = new ReputationSystemSaveData.ReputationEventData
                    {
                        eventId = evt.eventId,
                        description = evt.description,
                        timestamp = evt.timestamp,
                        decayRate = evt.decayRate
                    };

                    foreach (var impactPair in evt.impacts)
                    {
                        eventData.impacts.Add(new ReputationSystemSaveData.TraitImpactData
                        {
                            traitId = impactPair.Key,
                            impact = impactPair.Value
                        });
                    }

                    reputationData.recentEvents.Add(eventData);
                }

                saveData.reputations.Add(reputationData);
            }

            return saveData;
        }

        public void RestoreFromSaveData(ReputationSystemSaveData saveData)
        {
            socialSystem.playerId = saveData.playerId;
            socialSystem.reputations.Clear();

            foreach (var reputationData in saveData.reputations)
            {
                var reputation = new SocialStandingSystem.Reputation(
                    reputationData.contextId,
                    reputationData.contextName
                );

                reputation.overallScore = reputationData.overallScore;

                foreach (var traitData in reputationData.traitScores)
                {
                    reputation.traitScores[traitData.traitId] = traitData.score;
                }

                foreach (var eventData in reputationData.recentEvents)
                {
                    var evt = new SocialStandingSystem.ReputationEvent(
                        eventData.eventId,
                        eventData.description,
                        eventData.timestamp,
                        eventData.decayRate
                    );

                    foreach (var impactData in eventData.impacts)
                    {
                        evt.impacts[impactData.traitId] = impactData.impact;
                    }

                    reputation.recentEvents.Add(evt);
                }

                socialSystem.reputations[reputationData.contextId] = reputation;
            }
        }
    }

    // Command Pattern implementations
    public interface IProgressionCommand
    {
        void Execute();
        void Undo();
    }

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

    public class ModifyStatCommand : IProgressionCommand
    {
        private readonly IStatSystem statSystem;
        private readonly string statId;
        private readonly PlayerStats.StatModifier modifier;
        private readonly bool isTemporary;

        public ModifyStatCommand(IStatSystem system, string id, PlayerStats.StatModifier mod, bool temporary = true)
        {
            statSystem = system;
            statId = id;
            modifier = mod;
            isTemporary = temporary;
        }

        public void Execute()
        {
            statSystem.ApplyModifier(statId, modifier);
        }

        public void Undo()
        {
            if (isTemporary)
            {
                statSystem.RemoveModifiersFromSource(statId, modifier.source);
            }
        }
    }

    public class AddReputationEventCommand : IProgressionCommand
    {
        private readonly IReputationSystem reputationSystem;
        private readonly string contextId;
        private readonly SocialStandingSystem.ReputationEvent reputationEvent;

        public AddReputationEventCommand(IReputationSystem system, string context, SocialStandingSystem.ReputationEvent evt)
        {
            reputationSystem = system;
            contextId = context;
            reputationEvent = evt;
        }

        public void Execute()
        {
            reputationSystem.AddReputationEvent(contextId, reputationEvent);
        }

        public void Undo()
        {
            // Reputation events are harder to undo perfectly
            // A proper implementation would need to store the previous reputation state
        }
    }
}