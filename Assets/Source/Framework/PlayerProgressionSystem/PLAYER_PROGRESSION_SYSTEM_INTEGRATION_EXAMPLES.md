# Player Progression System - Integration Examples

This document provides practical examples of how to integrate the Player Progression System with various game systems.

## Table of Contents

1. [Combat System Integration](#combat-system-integration)
2. [Quest System Integration](#quest-system-integration)
3. [Item and Inventory Integration](#item-and-inventory-integration)
4. [Dialog and NPC Interaction](#dialog-and-npc-interaction)
5. [Environment Interaction](#environment-interaction)
6. [Achievement System](#achievement-system)
7. [UI Integration](#ui-integration)

## Combat System Integration

### Example: Player Attack Handler

```csharp
using FuncTest.PlayerProgression;
using FuncTest.PlayerProgression.Data;
using UnityEngine;
using System.Collections.Generic;

public class PlayerCombatHandler : MonoBehaviour
{
    [SerializeField] private EventBusReference eventBus;
    
    public void ProcessAttack(string targetId, string weaponId, float damageDealt, bool isCritical)
    {
        // Determine which skills gain experience
        Dictionary<string, float> skillGains = new Dictionary<string, float>();
        
        // Add base weapon skill experience based on damage dealt
        skillGains.Add(weaponId, damageDealt * 0.5f);
        
        // Add experience to relevant attribute skills
        switch (weaponId)
        {
            case "sword":
            case "axe":
            case "mace":
                skillGains.Add("strength", damageDealt * 0.2f);
                break;
            case "dagger":
            case "bow":
                skillGains.Add("dexterity", damageDealt * 0.2f);
                break;
            case "staff":
            case "wand":
                skillGains.Add("intelligence", damageDealt * 0.2f);
                break;
        }
        
        // Add critical hit bonus if applicable
        if (isCritical)
        {
            skillGains.Add("critical_strikes", damageDealt * 0.8f);
        }
        
        // Create and publish the action event
        var attackEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.CompleteAction,
            parameters = new Dictionary<string, object>
            {
                { "actionId", "player_attack" },
                { "targetId", targetId },
                { "weaponId", weaponId },
                { "damageDealt", damageDealt },
                { "isCritical", isCritical },
                { "relevantSkills", skillGains }
            }
        };
        
        eventBus.Publish(attackEvent);
    }
    
    public void ProcessDefense(string attackerId, float damageTaken, float damageBlocked, string armorType)
    {
        // Determine which skills gain experience
        Dictionary<string, float> skillGains = new Dictionary<string, float>();
        
        // Add armor skill experience
        switch (armorType)
        {
            case "light_armor":
                skillGains.Add("light_armor", damageBlocked * 0.7f);
                skillGains.Add("agility", damageBlocked * 0.3f);
                break;
            case "medium_armor":
                skillGains.Add("medium_armor", damageBlocked * 0.7f);
                skillGains.Add("endurance", damageBlocked * 0.3f);
                break;
            case "heavy_armor":
                skillGains.Add("heavy_armor", damageBlocked * 0.7f);
                skillGains.Add("strength", damageBlocked * 0.3f);
                break;
        }
        
        // Add block skill if applicable
        if (damageBlocked > 0)
        {
            skillGains.Add("block", damageBlocked);
        }
        
        // Create and publish the action event
        var defenseEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.CompleteAction,
            parameters = new Dictionary<string, object>
            {
                { "actionId", "player_defense" },
                { "attackerId", attackerId },
                { "damageTaken", damageTaken },
                { "damageBlocked", damageBlocked },
                { "armorType", armorType },
                { "relevantSkills", skillGains }
            }
        };
        
        eventBus.Publish(defenseEvent);
    }
}
```

### Example: Enemy Defeat Handler

```csharp
using FuncTest.PlayerProgression;
using FuncTest.PlayerProgression.Data;
using UnityEngine;
using System.Collections.Generic;

public class EnemyDefeatHandler : MonoBehaviour
{
    [SerializeField] private EventBusReference eventBus;
    
    [System.Serializable]
    public class EnemyReward
    {
        public string enemyId;
        public int experienceValue;
        public Dictionary<string, float> reputationImpacts;
    }
    
    [SerializeField] private List<EnemyReward> enemyRewards;
    
    public void ProcessEnemyDefeat(string enemyId, string defeatingBlowWeapon)
    {
        // Find the reward data for this enemy
        EnemyReward reward = enemyRewards.Find(r => r.enemyId == enemyId);
        if (reward == null)
            return;
        
        // Add weapon skill experience
        var skillExpEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.SkillExperience,
            parameters = new Dictionary<string, object>
            {
                { "skillId", defeatingBlowWeapon },
                { "experienceAmount", reward.experienceValue * 0.5f }
            }
        };
        
        eventBus.Publish(skillExpEvent);
        
        // Update reputation if this enemy affects any factions
        if (reward.reputationImpacts != null && reward.reputationImpacts.Count > 0)
        {
            // For each affected faction
            foreach (var factionImpact in reward.reputationImpacts)
            {
                var reputationEvent = new ProgressionEvent
                {
                    type = ProgressionEvent.ProgressionEventType.ReputationImpact,
                    parameters = new Dictionary<string, object>
                    {
                        { "contextId", factionImpact.Key },
                        { "eventId", $"defeat_{enemyId}" },
                        { "description", $"Defeated {enemyId}" },
                        { "traitImpacts", new Dictionary<string, float>
                            {
                                { "combat_prowess", factionImpact.Value },
                                { "bravery", factionImpact.Value * 0.5f }
                            }
                        },
                        { "decayRate", 0.02f }
                    }
                };
                
                eventBus.Publish(reputationEvent);
            }
        }
    }
}
```

## Quest System Integration

### Example: Quest Completion Handler

```csharp
using FuncTest.PlayerProgression;
using FuncTest.PlayerProgression.Data;
using UnityEngine;
using System.Collections.Generic;

public class QuestCompletionHandler : MonoBehaviour
{
    [SerializeField] private EventBusReference eventBus;
    
    [System.Serializable]
    public class QuestReward
    {
        public string questId;
        public Dictionary<string, float> statIncreases;
        public Dictionary<string, float> skillExperience;
        public Dictionary<string, Dictionary<string, float>> reputationChanges;
    }
    
    [SerializeField] private List<QuestReward> questRewards;
    
    public void CompleteQuest(string questId)
    {
        // Find the reward data for this quest
        QuestReward reward = questRewards.Find(r => r.questId == questId);
        if (reward == null)
            return;
        
        // Apply stat increases
        if (reward.statIncreases != null)
        {
            foreach (var statIncrease in reward.statIncreases)
            {
                var statEvent = new ProgressionEvent
                {
                    type = ProgressionEvent.ProgressionEventType.StatChange,
                    parameters = new Dictionary<string, object>
                    {
                        { "statId", statIncrease.Key },
                        { "baseValueChange", statIncrease.Value }
                    }
                };
                
                eventBus.Publish(statEvent);
            }
        }
        
        // Apply skill experience
        if (reward.skillExperience != null)
        {
            foreach (var skillExp in reward.skillExperience)
            {
                var skillEvent = new ProgressionEvent
                {
                    type = ProgressionEvent.ProgressionEventType.SkillExperience,
                    parameters = new Dictionary<string, object>
                    {
                        { "skillId", skillExp.Key },
                        { "experienceAmount", skillExp.Value }
                    }
                };
                
                eventBus.Publish(skillEvent);
            }
        }
        
        // Apply reputation changes
        if (reward.reputationChanges != null)
        {
            foreach (var factionChange in reward.reputationChanges)
            {
                var reputationEvent = new ProgressionEvent
                {
                    type = ProgressionEvent.ProgressionEventType.ReputationImpact,
                    parameters = new Dictionary<string, object>
                    {
                        { "contextId", factionChange.Key },
                        { "eventId", $"quest_{questId}" },
                        { "description", $"Completed quest: {questId}" },
                        { "traitImpacts", factionChange.Value },
                        { "decayRate", 0.01f } // Quest reputation impacts decay very slowly
                    }
                };
                
                eventBus.Publish(reputationEvent);
            }
        }
    }
}
```

## Item and Inventory Integration

### Example: Item Usage Handler

```csharp
using FuncTest.PlayerProgression;
using FuncTest.PlayerProgression.Data;
using UnityEngine;
using System.Collections.Generic;

public class ItemUsageHandler : MonoBehaviour
{
    [SerializeField] private EventBusReference eventBus;
    
    public void UsePotion(string potionId, Dictionary<string, float> statBoosts, float duration)
    {
        // Apply temporary stat modifiers
        foreach (var statBoost in statBoosts)
        {
            var statModifier = new PlayerStats.StatModifier(
                $"potion_{potionId}", // Source identifier
                statBoost.Value,       // Value to add
                PlayerStats.ModifierType.Additive, // Type of modification
                duration               // Duration in seconds
            );
            
            var statEvent = new ProgressionEvent
            {
                type = ProgressionEvent.ProgressionEventType.StatChange,
                parameters = new Dictionary<string, object>
                {
                    { "statId", statBoost.Key },
                    { "modifier", statModifier }
                }
            };
            
            eventBus.Publish(statEvent);
        }
        
        // Also add experience to the alchemy skill for using a potion
        var skillEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.SkillExperience,
            parameters = new Dictionary<string, object>
            {
                { "skillId", "alchemy" },
                { "experienceAmount", 5.0f }
            }
        };
        
        eventBus.Publish(skillEvent);
    }
    
    public void EquipItem(string itemId, Dictionary<string, float> statModifiers)
    {
        // Apply permanent stat modifiers (until unequipped)
        foreach (var statMod in statModifiers)
        {
            var statModifier = new PlayerStats.StatModifier(
                $"equipment_{itemId}", // Source identifier
                statMod.Value,         // Value to add
                PlayerStats.ModifierType.Additive, // Type of modification
                -1                     // -1 means permanent until removed
            );
            
            var statEvent = new ProgressionEvent
            {
                type = ProgressionEvent.ProgressionEventType.StatChange,
                parameters = new Dictionary<string, object>
                {
                    { "statId", statMod.Key },
                    { "modifier", statModifier }
                }
            };
            
            eventBus.Publish(statEvent);
        }
    }
    
    public void UnequipItem(string itemId, List<string> affectedStats)
    {
        // Remove stat modifiers from the unequipped item
        foreach (var statId in affectedStats)
        {
            // Create a stat change event that instructs the system to remove modifiers from this source
            var statEvent = new ProgressionEvent
            {
                type = ProgressionEvent.ProgressionEventType.StatChange,
                parameters = new Dictionary<string, object>
                {
                    { "statId", statId },
                    { "removeModifiersFromSource", $"equipment_{itemId}" }
                }
            };
            
            eventBus.Publish(statEvent);
        }
    }
    
    public void CraftItem(string itemId, string craftingSkillId, float difficultyValue)
    {
        // Award crafting skill experience based on item difficulty
        var skillEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.SkillExperience,
            parameters = new Dictionary<string, object>
            {
                { "skillId", craftingSkillId },
                { "experienceAmount", difficultyValue * 10f }
            }
        };
        
        eventBus.Publish(skillEvent);
    }
}
```

## Dialog and NPC Interaction

### Example: Dialog Choice Handler

```csharp
using FuncTest.PlayerProgression;
using FuncTest.PlayerProgression.Data;
using UnityEngine;
using System.Collections.Generic;

public class DialogChoiceHandler : MonoBehaviour
{
    [SerializeField] private EventBusReference eventBus;
    
    public void ProcessDialogChoice(string npcId, string dialogOptionId, Dictionary<string, float> reputationImpacts)
    {
        // Create reputation impact event
        var reputationEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.ReputationImpact,
            parameters = new Dictionary<string, object>
            {
                { "contextId", $"npc_{npcId}" },
                { "eventId", $"dialog_{dialogOptionId}" },
                { "description", $"Dialog choice with {npcId}: {dialogOptionId}" },
                { "traitImpacts", reputationImpacts },
                { "decayRate", 0.03f }
            }
        };
        
        eventBus.Publish(reputationEvent);
        
        // Also award experience to the speech skill
        var skillEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.SkillExperience,
            parameters = new Dictionary<string, object>
            {
                { "skillId", "speech" },
                { "experienceAmount", 5.0f }
            }
        };
        
        eventBus.Publish(skillEvent);
    }
    
    public void PersuasionAttempt(string npcId, bool success, float difficulty)
    {
        // Award speech skill experience based on difficulty and success
        float expAmount = success ? difficulty * 15f : difficulty * 5f;
        
        var skillEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.SkillExperience,
            parameters = new Dictionary<string, object>
            {
                { "skillId", "speech" },
                { "experienceAmount", expAmount }
            }
        };
        
        eventBus.Publish(skillEvent);
        
        // Update reputation if successful
        if (success)
        {
            var reputationEvent = new ProgressionEvent
            {
                type = ProgressionEvent.ProgressionEventType.ReputationImpact,
                parameters = new Dictionary<string, object>
                {
                    { "contextId", $"npc_{npcId}" },
                    { "eventId", "successful_persuasion" },
                    { "description", $"Successfully persuaded {npcId}" },
                    { "traitImpacts", new Dictionary<string, float>
                        {
                            { "charisma", 5.0f },
                            { "respect", 3.0f }
                        }
                    },
                    { "decayRate", 0.02f }
                }
            };
            
            eventBus.Publish(reputationEvent);
        }
    }
}
```

## Environment Interaction

### Example: Resource Gathering

```csharp
using FuncTest.PlayerProgression;
using FuncTest.PlayerProgression.Data;
using UnityEngine;
using System.Collections.Generic;

public class ResourceGatheringHandler : MonoBehaviour
{
    [SerializeField] private EventBusReference eventBus;
    
    public void GatherResource(string resourceId, float amount, string toolUsed)
    {
        // Determine which skill to improve based on the resource type
        string skillId = "";
        float expMultiplier = 1.0f;
        
        switch (resourceId)
        {
            case "wood":
                skillId = "woodcutting";
                expMultiplier = 1.0f;
                break;
            case "ore":
                skillId = "mining";
                expMultiplier = 1.5f;
                break;
            case "herb":
                skillId = "herbalism";
                expMultiplier = 0.8f;
                break;
            case "fish":
                skillId = "fishing";
                expMultiplier = 1.2f;
                break;
            default:
                skillId = "gathering";
                expMultiplier = 1.0f;
                break;
        }
        
        // Award experience to the appropriate skill
        var skillEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.SkillExperience,
            parameters = new Dictionary<string, object>
            {
                { "skillId", skillId },
                { "experienceAmount", amount * expMultiplier }
            }
        };
        
        eventBus.Publish(skillEvent);
        
        // If a tool was used, also give some experience to the tool skill
        if (!string.IsNullOrEmpty(toolUsed))
        {
            var toolSkillEvent = new ProgressionEvent
            {
                type = ProgressionEvent.ProgressionEventType.SkillExperience,
                parameters = new Dictionary<string, object>
                {
                    { "skillId", $"{toolUsed}_mastery" },
                    { "experienceAmount", amount * 0.5f }
                }
            };
            
            eventBus.Publish(toolSkillEvent);
        }
    }
    
    public void ExploreRegion(string regionId, int newLocationsDiscovered)
    {
        // Award exploration skill experience
        var skillEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.SkillExperience,
            parameters = new Dictionary<string, object>
            {
                { "skillId", "exploration" },
                { "experienceAmount", newLocationsDiscovered * 25f }
            }
        };
        
        eventBus.Publish(skillEvent);
        
        // Update reputation with the region if significant exploration was done
        if (newLocationsDiscovered >= 3)
        {
            var reputationEvent = new ProgressionEvent
            {
                type = ProgressionEvent.ProgressionEventType.ReputationImpact,
                parameters = new Dictionary<string, object>
                {
                    { "contextId", $"region_{regionId}" },
                    { "eventId", "extensive_exploration" },
                    { "description", $"Extensively explored region {regionId}" },
                    { "traitImpacts", new Dictionary<string, float>
                        {
                            { "familiarity", newLocationsDiscovered * 2f },
                            { "adventurousness", newLocationsDiscovered * 1.5f }
                        }
                    },
                    { "decayRate", 0.005f } // Exploration reputation decays very slowly
                }
            };
            
            eventBus.Publish(reputationEvent);
        }
    }
}
```

## Achievement System

### Example: Achievement Tracker

```csharp
using FuncTest.PlayerProgression;
using FuncTest.PlayerProgression.Data;
using UnityEngine;
using System.Collections.Generic;

public class AchievementTracker : MonoBehaviour
{
    [SerializeField] private EventBusReference eventBus;
    [SerializeField] private PlayerProgressionManager progressionManager;
    
    [System.Serializable]
    public class SkillAchievement
    {
        public string achievementId;
        public string skillId;
        public float requiredLevel;
        public Dictionary<string, float> statBoosts;
    }
    
    [SerializeField] private List<SkillAchievement> skillAchievements;
    private HashSet<string> unlockedAchievements = new HashSet<string>();
    
    private void OnEnable()
    {
        // Subscribe to the event bus to check for achievements
        eventBus.Subscribe<ProgressionEvent>(CheckForAchievements);
    }
    
    private void OnDisable()
    {
        // Unsubscribe when disabled
        eventBus.Unsubscribe<ProgressionEvent>(CheckForAchievements);
    }
    
    private void CheckForAchievements(ProgressionEvent evt)
    {
        // Check skill achievements when skill experience is gained
        if (evt.type == ProgressionEvent.ProgressionEventType.SkillExperience && 
            evt.parameters.ContainsKey("skillId"))
        {
            string skillId = (string)evt.parameters["skillId"];
            CheckSkillAchievements(skillId);
        }
    }
    
    private void CheckSkillAchievements(string skillId)
    {
        float currentLevel = progressionManager.GetSkillLevel(skillId);
        
        foreach (var achievement in skillAchievements)
        {
            // Skip if this achievement is already unlocked
            if (unlockedAchievements.Contains(achievement.achievementId))
                continue;
            
            // Check if this achievement applies to the current skill
            if (achievement.skillId == skillId && currentLevel >= achievement.requiredLevel)
            {
                // Unlock the achievement
                UnlockAchievement(achievement);
            }
        }
    }
    
    private void UnlockAchievement(SkillAchievement achievement)
    {
        Debug.Log($"Achievement unlocked: {achievement.achievementId}");
        unlockedAchievements.Add(achievement.achievementId);
        
        // Apply stat boosts as rewards
        if (achievement.statBoosts != null)
        {
            foreach (var statBoost in achievement.statBoosts)
            {
                var statEvent = new ProgressionEvent
                {
                    type = ProgressionEvent.ProgressionEventType.StatChange,
                    parameters = new Dictionary<string, object>
                    {
                        { "statId", statBoost.Key },
                        { "baseValueChange", statBoost.Value }
                    }
                };
                
                eventBus.Publish(statEvent);
            }
        }
        
        // Publish achievement event
        var achievementEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.UnlockAchievement,
            parameters = new Dictionary<string, object>
            {
                { "achievementId", achievement.achievementId },
                { "skillId", achievement.skillId },
                { "requiredLevel", achievement.requiredLevel }
            }
        };
        
        eventBus.Publish(achievementEvent);
    }
}
```

## UI Integration

### Example: Character Stats UI

```csharp
using FuncTest.PlayerProgression;
using FuncTest.PlayerProgression.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterStatsUI : MonoBehaviour
{
    [SerializeField] private PlayerProgressionManager progressionManager;
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statPrefab;
    
    [SerializeField] private Transform skillsContainer;
    [SerializeField] private GameObject skillPrefab;
    
    [SerializeField] private TMP_Dropdown reputationContextDropdown;
    [SerializeField] private Transform reputationContainer;
    [SerializeField] private GameObject reputationPrefab;
    
    private Dictionary<string, GameObject> statUIElements = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> skillUIElements = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> reputationUIElements = new Dictionary<string, GameObject>();
    
    private void Start()
    {
        // Initialize the UI
        InitializeUI();
        
        // Set up periodic updates
        InvokeRepeating("UpdateUI", 0.1f, 0.5f);
    }
    
    private void InitializeUI()
    {
        // Initialize stats UI
        InitializeStatsUI();
        
        // Initialize skills UI
        InitializeSkillsUI();
        
        // Initialize reputation UI
        InitializeReputationUI();
    }
    
    private void InitializeStatsUI()
    {
        // Clear existing UI
        foreach (Transform child in statsContainer)
        {
            Destroy(child.gameObject);
        }
        statUIElements.Clear();
        
        // Add stats from the configuration
        var config = progressionManager.GetConfiguration();
        foreach (var statConfig in config.initialStats)
        {
            GameObject statObj = Instantiate(statPrefab, statsContainer);
            
            // Set up the UI elements
            TMP_Text statName = statObj.transform.Find("StatName").GetComponent<TMP_Text>();
            TMP_Text statValue = statObj.transform.Find("StatValue").GetComponent<TMP_Text>();
            Slider statSlider = statObj.transform.Find("StatSlider").GetComponent<Slider>();
            
            statName.text = statConfig.statName;
            
            // Update the values
            StatValue stat = progressionManager.GetStatValue(statConfig.statId);
            statValue.text = $"{stat.CurrentValue:F1} / {stat.MaxValue:F0}";
            statSlider.minValue = stat.MinValue;
            statSlider.maxValue = stat.MaxValue;
            statSlider.value = stat.CurrentValue;
            
            // Store reference for updates
            statUIElements[statConfig.statId] = statObj;
        }
    }
    
    private void InitializeSkillsUI()
    {
        // Clear existing UI
        foreach (Transform child in skillsContainer)
        {
            Destroy(child.gameObject);
        }
        skillUIElements.Clear();
        
        // Add skills from the configuration
        var config = progressionManager.GetConfiguration();
        foreach (var category in config.skillCategories)
        {
            // Create category header
            GameObject categoryHeader = new GameObject(category.categoryName);
            categoryHeader.transform.SetParent(skillsContainer, false);
            
            TMP_Text headerText = categoryHeader.AddComponent<TMP_Text>();
            headerText.text = category.categoryName;
            headerText.fontSize = 18;
            headerText.fontStyle = FontStyles.Bold;
            
            // Add skills
            foreach (var skill in category.skills)
            {
                GameObject skillObj = Instantiate(skillPrefab, skillsContainer);
                
                // Set up the UI elements
                TMP_Text skillName = skillObj.transform.Find("SkillName").GetComponent<TMP_Text>();
                TMP_Text skillLevel = skillObj.transform.Find("SkillLevel").GetComponent<TMP_Text>();
                Slider expSlider = skillObj.transform.Find("ExpSlider").GetComponent<Slider>();
                
                skillName.text = skill.skillName;
                
                // Update the values
                float level = progressionManager.GetSkillLevel(skill.skillId);
                // Experience info would require additional access methods
                
                skillLevel.text = $"Level: {level:F0}";
                // For the slider we'd need access to current experience and threshold
                
                // Store reference for updates
                skillUIElements[skill.skillId] = skillObj;
            }
        }
    }
    
    private void InitializeReputationUI()
    {
        // Clear existing UI
        foreach (Transform child in reputationContainer)
        {
            Destroy(child.gameObject);
        }
        reputationUIElements.Clear();
        
        // Set up the context dropdown
        reputationContextDropdown.ClearOptions();
        
        // Add contexts from the configuration
        var config = progressionManager.GetConfiguration();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        
        foreach (var context in config.reputationContexts)
        {
            options.Add(new TMP_Dropdown.OptionData(context.contextName));
        }
        
        reputationContextDropdown.AddOptions(options);
        reputationContextDropdown.onValueChanged.AddListener(UpdateReputationDisplay);
        
        // Initial update
        if (options.Count > 0)
        {
            UpdateReputationDisplay(0);
        }
    }
    
    private void UpdateReputationDisplay(int contextIndex)
    {
        // Clear current display
        foreach (Transform child in reputationContainer)
        {
            Destroy(child.gameObject);
        }
        reputationUIElements.Clear();
        
        var config = progressionManager.GetConfiguration();
        if (contextIndex < config.reputationContexts.Count)
        {
            var context = config.reputationContexts[contextIndex];
            
            // Add overall reputation
            GameObject overallObj = Instantiate(reputationPrefab, reputationContainer);
            TMP_Text overallName = overallObj.transform.Find("TraitName").GetComponent<TMP_Text>();
            TMP_Text overallValue = overallObj.transform.Find("TraitValue").GetComponent<TMP_Text>();
            Slider overallSlider = overallObj.transform.Find("TraitSlider").GetComponent<Slider>();
            
            overallName.text = "Overall";
            
            float overallScore = progressionManager.GetReputationScore(context.contextId);
            overallValue.text = $"{overallScore:F1}";
            overallSlider.value = (overallScore + 100) / 200; // Normalize from -100/+100 to 0/1
            
            // Add individual traits
            foreach (var trait in context.relevantTraits)
            {
                GameObject traitObj = Instantiate(reputationPrefab, reputationContainer);
                TMP_Text traitName = traitObj.transform.Find("TraitName").GetComponent<TMP_Text>();
                TMP_Text traitValue = traitObj.transform.Find("TraitValue").GetComponent<TMP_Text>();
                Slider traitSlider = traitObj.transform.Find("TraitSlider").GetComponent<Slider>();
                
                traitName.text = trait;
                
                float traitScore = progressionManager.GetReputationScore(context.contextId, trait);
                traitValue.text = $"{traitScore:F1}";
                traitSlider.value = (traitScore + 100) / 200; // Normalize from -100/+100 to 0/1
                
                // Store reference for updates
                reputationUIElements[trait] = traitObj;
            }
        }
    }
    
    private void UpdateUI()
    {
        // Update stats
        UpdateStatsUI();
        
        // Update skills
        UpdateSkillsUI();
        
        // Update reputation (current context)
        UpdateCurrentReputationUI();
    }
    
    private void UpdateStatsUI()
    {
        foreach (var statPair in statUIElements)
        {
            string statId = statPair.Key;
            GameObject statObj = statPair.Value;
            
            TMP_Text statValue = statObj.transform.Find("StatValue").GetComponent<TMP_Text>();
            Slider statSlider = statObj.transform.Find("StatSlider").GetComponent<Slider>();
            
            StatValue stat = progressionManager.GetStatValue(statId);
            statValue.text = $"{stat.CurrentValue:F1} / {stat.MaxValue:F0}";
            statSlider.value = stat.CurrentValue;
        }
    }
    
    private void UpdateSkillsUI()
    {
        foreach (var skillPair in skillUIElements)
        {
            string skillId = skillPair.Key;
            GameObject skillObj = skillPair.Value;
            
            TMP_Text skillLevel = skillObj.transform.Find("SkillLevel").GetComponent<TMP_Text>();
            
            float level = progressionManager.GetSkillLevel(skillId);
            skillLevel.text = $"Level: {level:F0}";
            // Updating experience progress would require additional access methods
        }
    }
    
    private void UpdateCurrentReputationUI()
    {
        int contextIndex = reputationContextDropdown.value;
        var config = progressionManager.GetConfiguration();
        
        if (contextIndex < config.reputationContexts.Count)
        {
            var context = config.reputationContexts[contextIndex];
            
            // Update overall reputation
            GameObject overallObj = reputationContainer.GetChild(0).gameObject;
            TMP_Text overallValue = overallObj.transform.Find("TraitValue").GetComponent<TMP_Text>();
            Slider overallSlider = overallObj.transform.Find("TraitSlider").GetComponent<Slider>();
            
            float overallScore = progressionManager.GetReputationScore(context.contextId);
            overallValue.text = $"{overallScore:F1}";
            overallSlider.value = (overallScore + 100) / 200; // Normalize from -100/+100 to 0/1
            
            // Update traits
            foreach (var traitPair in reputationUIElements)
            {
                string traitId = traitPair.Key;
                GameObject traitObj = traitPair.Value;
                
                TMP_Text traitValue = traitObj.transform.Find("TraitValue").GetComponent<TMP_Text>();
                Slider traitSlider = traitObj.transform.Find("TraitSlider").GetComponent<Slider>();
                
                float traitScore = progressionManager.GetReputationScore(context.contextId, traitId);
                traitValue.text = $"{traitScore:F1}";
                traitSlider.value = (traitScore + 100) / 200; // Normalize from -100/+100 to 0/1
            }
        }
    }
}
```

These examples provide a practical foundation for integrating the Player Progression System with various game systems. You can adapt and extend these patterns based on your specific game requirements.