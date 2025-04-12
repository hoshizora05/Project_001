using System.Collections.Generic;
using UnityEngine;
using PlayerProgression.Core;
using PlayerProgression.Data;
using PlayerProgression.Interfaces;
using PlayerProgression.Systems;

namespace PlayerProgression
{
    public class PlayerProgressionManager : MonoBehaviour, IPlayerProgressionSystem
    {
        #region Singleton
        private static PlayerProgressionManager _instance;

        public static PlayerProgressionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayerProgressionManager>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PlayerProgression");
                        _instance = go.AddComponent<PlayerProgressionManager>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }
        #endregion

        [SerializeField] private PlayerProgressionConfig config;
        
        private IStatSystem statSystem;
        private ISkillSystem skillSystem;
        private IReputationSystem reputationSystem;
        
        [SerializeField] private EventBusReference eventBus;
        private string currentPlayerId;
        
        private void Awake()
        {
            statSystem = new StatSystem();
            skillSystem = new SkillProgressionSystem();
            reputationSystem = new ReputationManager();
        }
        
        private void OnEnable()
        {
            if (eventBus != null)
            {
                eventBus.Subscribe<ProgressionEvent>(ProcessEvent);
            }
        }
        
        private void OnDisable()
        {
            if (eventBus != null)
            {
                eventBus.Unsubscribe<ProgressionEvent>(ProcessEvent);
            }
        }
        
        public void Initialize(string playerId, PlayerProgressionConfig configOverride = null)
        {
            currentPlayerId = playerId;
            var configToUse = configOverride ?? config;
            
            statSystem.Initialize(playerId, configToUse);
            skillSystem.Initialize(playerId, configToUse);
            reputationSystem.Initialize(playerId, configToUse);
        }
        
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            UpdateProgression(deltaTime);
        }
        
        public void UpdateProgression(float deltaTime)
        {
            statSystem.Update(deltaTime);
            skillSystem.Update(deltaTime);
            reputationSystem.Update(deltaTime);
            
            UpdateCrossSystemEffects();
        }
        
        public void ProcessEvent(ProgressionEvent progressEvent)
        {
            statSystem.ProcessEvent(progressEvent);
            skillSystem.ProcessEvent(progressEvent);
            reputationSystem.ProcessEvent(progressEvent);
            
            UpdateCrossSystemEffects();
        }
        
        private void UpdateCrossSystemEffects()
        {
            // Apply skill effects to stats
            foreach (var categoryId in GetAllSkillCategories())
            {
                foreach (var skillId in GetSkillsInCategory(categoryId))
                {
                    float skillLevel = skillSystem.GetSkillLevel(skillId);
                    if (skillLevel > 0)
                    {
                        var effects = skillSystem.GetSkillEffects(skillId);
                        foreach (var effect in effects)
                        {
                            if (effect.type == SkillSystem.EffectType.StatBoost)
                            {
                                // Remove old modifier first to prevent stacking
                                statSystem.RemoveModifiersFromSource(effect.targetStat, $"skill_{skillId}");
                                
                                // Apply new modifier based on current skill level
                                var modifier = new PlayerStats.StatModifier(
                                    $"skill_{skillId}",
                                    effect.effectValue * skillLevel,
                                    PlayerStats.ModifierType.Additive,
                                    -1  // Permanent until skill level changes
                                );
                                
                                statSystem.ApplyModifier(effect.targetStat, modifier);
                            }
                        }
                    }
                }
            }
        }
        
        private List<string> GetAllSkillCategories()
        {
            List<string> result = new List<string>();
            foreach (var category in config.skillCategories)
            {
                result.Add(category.categoryId);
            }
            return result;
        }
        
        private List<string> GetSkillsInCategory(string categoryId)
        {
            List<string> result = new List<string>();
            foreach (var category in config.skillCategories)
            {
                if (category.categoryId == categoryId)
                {
                    foreach (var skill in category.skills)
                    {
                        result.Add(skill.skillId);
                    }
                    break;
                }
            }
            return result;
        }
        
        public StatValue GetStatValue(string statId)
        {
            return statSystem.GetStatValue(statId);
        }
        
        public float GetSkillLevel(string skillId)
        {
            return skillSystem.GetSkillLevel(skillId);
        }
        
        public float GetReputationScore(string contextId, string traitId = "")
        {
            return reputationSystem.GetReputationScore(contextId, traitId);
        }
        
        public ProgressionSaveData GenerateSaveData()
        {
            return new ProgressionSaveData
            {
                statData = statSystem.GenerateSaveData(),
                skillData = skillSystem.GenerateSaveData(),
                reputationData = reputationSystem.GenerateSaveData()
            };
        }
        
        public void RestoreFromSaveData(ProgressionSaveData saveData)
        {
            statSystem.RestoreFromSaveData(saveData.statData);
            skillSystem.RestoreFromSaveData(saveData.skillData);
            reputationSystem.RestoreFromSaveData(saveData.reputationData);
            
            // Make sure cross-system effects are applied
            UpdateCrossSystemEffects();
        }
        
        public void InjectDependencies(
            IStatSystem statSys, 
            ISkillSystem skillSys,
            IReputationSystem repSys)
        {
            statSystem = statSys;
            skillSystem = skillSys;
            reputationSystem = repSys;
        }
        
        public void InjectForTesting(
            IStatSystem mockStatSys,
            ISkillSystem mockSkillSys,
            IReputationSystem mockRepSys)
        {
            InjectDependencies(mockStatSys, mockSkillSys, mockRepSys);
        }

        public string GetPlayerId()
        {
            return currentPlayerId;
        }
    }
}