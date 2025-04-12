using System;
using System.Collections.Generic;
using PlayerProgression.Data;
using PlayerProgression.Interfaces;

namespace PlayerProgression.Systems
{
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
}