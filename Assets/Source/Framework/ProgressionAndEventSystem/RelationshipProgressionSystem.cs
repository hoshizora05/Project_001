using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProgressionAndEventSystem
{
    public enum RelationshipType
    {
        Friendship,
        Romance,
        Mentor,
        Rival,
        Family,
        Professional
    }

    public enum RequirementType
    {
        StatValue,
        EventCompletion,
        ItemPossession,
        SkillLevel,
        TimeSpent,
        LocationVisits,
        ActivityCompletion,
        KeyDecision
    }

    public enum TriggerType
    {
        Statistical,
        EventCompletion,
        TimePassed,
        Compound,
        WorldState,
        PlayerAction,
        ScheduledTime
    }

    public enum EffectType
    {
        UnlockEvent,
        AdvanceStage,
        UnlockContent,
        GrantItem,
        ModifyStat,
        ChangeWorldState,
        TriggerAnimation,
        PlaySound
    }

    public class RelationshipEffect
    {
        public string CharacterId;
        public string ParameterId;
        public float Value;
        public string Description;
    }

    [Serializable]
    public class RelationshipStage
    {
        public string Id;
        public string Name;
        public string Description;
        public int Level;
        public RelationshipType Type;
        public List<RelationshipRequirement> AdvancementRequirements;
        public Dictionary<string, float> StageEffects;
        public List<string> UnlockedContent;
        public List<string> UnlockedEvents;
        public List<string> UnlockedDialogueTopics;
    }

    [Serializable]
    public class RelationshipRequirement
    {
        public RequirementType Type;
        public string ParameterId;
        public float RequiredValue;
        public bool IsMandatory;
        public string Description;
        public bool IsHidden;
    }

    [Serializable]
    public class ProgressionTrigger
    {
        public string Id;
        public string Description;
        public TriggerType Type;
        public List<EventCondition> Conditions;
        public List<TriggerEffect> Effects;
        public bool IsOneTime;
        public int Priority;
        public List<string> Dependencies;
        
        public bool CheckConditions(ICharacter player, ICharacter npc = null)
        {
            if (Conditions == null || Conditions.Count == 0)
                return true;
                
            foreach (var condition in Conditions)
            {
                if (!EvaluateCondition(condition, player, npc))
                    return false;
            }
            
            return true;
        }
        
        private bool EvaluateCondition(EventCondition condition, ICharacter player, ICharacter npc)
        {
            // We adapt to the existing EventCondition implementation
            // This is a simplified version to bridge the gap
            
            // Check if it's a stat condition
            if (condition.TargetId != null)
            {
                var stats = player.GetStats();
                if (stats.TryGetValue(condition.TargetId, out float value))
                {
                    float expected = Convert.ToSingle(condition.ExpectedValue);
                    
                    switch (condition.Operator)
                    {
                        case ComparisonOperator.Equal:
                            return Math.Abs(value - expected) < 0.001f;
                        case ComparisonOperator.NotEqual:
                            return Math.Abs(value - expected) >= 0.001f;
                        case ComparisonOperator.GreaterThan:
                            return value > expected;
                        case ComparisonOperator.LessThan:
                            return value < expected;
                        case ComparisonOperator.GreaterThanOrEqual:
                            return value >= expected;
                        case ComparisonOperator.LessThanOrEqual:
                            return value <= expected;
                    }
                }
            }
            
            // If using the existing EventManager from ProgressionAndEventSystem
            if (GameObject.FindFirstObjectByType<EventManager>() is EventManager eventManager)
            {
                // Create a dummy GameEvent with just this condition
                var dummyEvent = new GameEvent 
                { 
                    Conditions = new List<EventCondition> { condition } 
                };
                
                return eventManager.CheckEventConditions(dummyEvent, player);
            }
            
            // Fallback simple implementation
            return true;
        }
    }

    [Serializable]
    public class TriggerEffect
    {
        public EffectType Type;
        public string TargetId;
        public object Value;
        public string Description;
    }

    [Serializable]
    public class KeyDecision
    {
        public string Id;
        public string Title;
        public string Description;
        public List<DecisionChoice> Choices;
        public List<EventCondition> ActivationConditions;
        public TimeSpan? DecisionTimeLimit;
        public bool IsForcedChoice;
        public DateTime ExpirationDate;
    }

    [Serializable]
    public class DecisionChoice
    {
        public string Id;
        public string Text;
        public string Description;
        public Dictionary<string, object> Effects;
        public List<string> UnlockedContent;
        public List<string> BlockedContent;
        public List<RelationshipEffect> RelationshipChanges;
        public List<EventCondition> AvailabilityConditions;
    }

    public class RelationshipData
    {
        public string PlayerId;
        public string NpcId;
        public string CurrentStageId;
        public float ProgressToNextStage;
        public Dictionary<string, float> Parameters = new Dictionary<string, float>();
        public List<string> CompletedEvents = new List<string>();
        public Dictionary<string, DateTime> StageAdvancementDates = new Dictionary<string, DateTime>();
        public Dictionary<string, string> DecisionHistory = new Dictionary<string, string>();
        public List<string> TriggeredProgressionEvents = new List<string>();
    }
    
    // Helper extension methods to work with the existing ICharacter interface
    public static class CharacterExtensions
    {
        public static Dictionary<string, float> GetCharacterStats(this ICharacter character)
        {
            return character.GetStats();
        }
        
        public static bool HasCompletedEvent(this ICharacter character, string eventId)
        {
            return character.GetEventHistory().ContainsKey(eventId);
        }
    }

    public interface IRelationshipStageManager
    {
        RelationshipStage GetCurrentStage(ICharacter player, ICharacter npc);
        bool AdvanceToNextStage(ICharacter player, ICharacter npc);
        float GetProgressToNextStage(ICharacter player, ICharacter npc);
        List<RelationshipRequirement> GetNextStageRequirements(ICharacter player, ICharacter npc);
    }

    public interface IProgressionTriggerSystem
    {
        void RegisterTrigger(ProgressionTrigger trigger);
        List<ProgressionTrigger> GetActiveTriggers(ICharacter player, ICharacter npc);
        bool CheckTriggerConditions(string triggerId, ICharacter player, ICharacter npc);
        void ActivateTrigger(string triggerId, ICharacter player, ICharacter npc);
    }

    public interface IKeyDecisionSystem
    {
        void RegisterKeyDecision(KeyDecision decision);
        List<KeyDecision> GetPendingKeyDecisions(ICharacter player);
        void MakeDecision(string decisionId, string choiceId, ICharacter player);
        Dictionary<string, string> GetPreviousDecisions(ICharacter player);
    }

    public class RelationshipProgressionSystem : MonoBehaviour, IRelationshipStageManager, IProgressionTriggerSystem, IKeyDecisionSystem
    {
        private Dictionary<string, RelationshipStage> _stagesById = new Dictionary<string, RelationshipStage>();
        private Dictionary<RelationshipType, List<RelationshipStage>> _stagesByType = new Dictionary<RelationshipType, List<RelationshipStage>>();
        private Dictionary<string, RelationshipData> _relationshipData = new Dictionary<string, RelationshipData>();
        private Dictionary<string, ProgressionTrigger> _triggersById = new Dictionary<string, ProgressionTrigger>();
        private Dictionary<string, KeyDecision> _decisionsById = new Dictionary<string, KeyDecision>();
        private List<KeyDecision> _activeDecisions = new List<KeyDecision>();

        public delegate void RelationshipStageChangedHandler(ICharacter player, ICharacter npc, RelationshipStage oldStage, RelationshipStage newStage);
        public delegate void ProgressionTriggerActivatedHandler(string triggerId, ICharacter player, ICharacter npc);
        public delegate void KeyDecisionRequiredHandler(KeyDecision decision, ICharacter player);
        public delegate void DecisionMadeHandler(string decisionId, string choiceId, ICharacter player);
        public delegate void RelationshipTypeChangedHandler(ICharacter player, ICharacter npc, RelationshipType oldType, RelationshipType newType);
        public delegate void NewStageRequirementsMetHandler(ICharacter player, ICharacter npc, RelationshipStage nextStage);

        public event RelationshipStageChangedHandler OnRelationshipStageChanged;
        public event ProgressionTriggerActivatedHandler OnProgressionTriggerActivated;
        public event KeyDecisionRequiredHandler OnKeyDecisionRequired;
        public event DecisionMadeHandler OnDecisionMade;
        public event RelationshipTypeChangedHandler OnRelationshipTypeChanged;
        public event NewStageRequirementsMetHandler OnNewStageRequirementsMet;

        [SerializeField] private TextAsset _stagesConfigFile;
        [SerializeField] private TextAsset _triggersConfigFile;
        [SerializeField] private TextAsset _decisionsConfigFile;

        private void Awake()
        {
            LoadStages();
            LoadTriggers();
            LoadDecisions();
        }

        private void Start()
        {
            StartCoroutine(CheckPendingDecisions());
            StartCoroutine(CheckTriggerConditions());
        }

        private System.Collections.IEnumerator CheckPendingDecisions()
        {
            while (true)
            {
                // Check pending decisions
                yield return new WaitForSeconds(30);
            }
        }

        private System.Collections.IEnumerator CheckTriggerConditions()
        {
            while (true)
            {
                // Check trigger conditions
                yield return new WaitForSeconds(10);
            }
        }

        private void LoadStages()
        {
            // In a real implementation, this would parse JSON or ScriptableObjects
            if (_stagesConfigFile != null)
            {
                // Parse stages from configuration
                // For example: _stages = JsonUtility.FromJson<List<RelationshipStage>>(_stagesConfigFile.text);
                
                // Organize stages by type for easier lookup
                foreach (var stage in _stagesById.Values)
                {
                    if (!_stagesByType.ContainsKey(stage.Type))
                    {
                        _stagesByType[stage.Type] = new List<RelationshipStage>();
                    }
                    _stagesByType[stage.Type].Add(stage);
                }

                // Sort stages by level within each type
                foreach (var type in _stagesByType.Keys)
                {
                    _stagesByType[type] = _stagesByType[type].OrderBy(s => s.Level).ToList();
                }
            }
        }

        private void LoadTriggers()
        {
            // In a real implementation, this would parse JSON or ScriptableObjects
            if (_triggersConfigFile != null)
            {
                // Parse triggers from configuration
                // For example: _triggersById = JsonUtility.FromJson<List<ProgressionTrigger>>(_triggersConfigFile.text)
                //                 .ToDictionary(t => t.Id);
            }
        }

        private void LoadDecisions()
        {
            // In a real implementation, this would parse JSON or ScriptableObjects
            if (_decisionsConfigFile != null)
            {
                // Parse decisions from configuration
                // For example: _decisionsById = JsonUtility.FromJson<List<KeyDecision>>(_decisionsConfigFile.text)
                //                 .ToDictionary(d => d.Id);
            }
        }

        private string GetRelationshipKey(ICharacter player, ICharacter npc)
        {
            return $"{player.Id}_{npc.Id}";
        }

        private RelationshipData GetOrCreateRelationshipData(ICharacter player, ICharacter npc)
        {
            string key = GetRelationshipKey(player, npc);
            if (!_relationshipData.ContainsKey(key))
            {
                _relationshipData[key] = new RelationshipData
                {
                    PlayerId = player.Id,
                    NpcId = npc.Id,
                    CurrentStageId = _stagesByType[RelationshipType.Friendship][0].Id,
                    ProgressToNextStage = 0
                };
            }
            return _relationshipData[key];
        }

        // IRelationshipStageManager Implementation
        public RelationshipStage GetCurrentStage(ICharacter player, ICharacter npc)
        {
            var data = GetOrCreateRelationshipData(player, npc);
            return _stagesById.TryGetValue(data.CurrentStageId, out var stage) ? stage : null;
        }

        public bool AdvanceToNextStage(ICharacter player, ICharacter npc)
        {
            var data = GetOrCreateRelationshipData(player, npc);
            var currentStage = GetCurrentStage(player, npc);
            
            if (currentStage == null) return false;

            var stagesOfType = _stagesByType[currentStage.Type];
            int currentIndex = stagesOfType.FindIndex(s => s.Id == currentStage.Id);
            
            if (currentIndex < 0 || currentIndex >= stagesOfType.Count - 1) return false;
            
            var nextStage = stagesOfType[currentIndex + 1];
            var oldStage = currentStage;
            
            // Update relationship data
            data.CurrentStageId = nextStage.Id;
            data.ProgressToNextStage = 0;
            data.StageAdvancementDates[nextStage.Id] = DateTime.Now;
            
            // Apply stage effects
            // In a real implementation, this would apply the effects defined in StageEffects
            
            // Notify listeners
            OnRelationshipStageChanged?.Invoke(player, npc, oldStage, nextStage);
            
            return true;
        }

        public float GetProgressToNextStage(ICharacter player, ICharacter npc)
        {
            var data = GetOrCreateRelationshipData(player, npc);
            return data.ProgressToNextStage;
        }

        public List<RelationshipRequirement> GetNextStageRequirements(ICharacter player, ICharacter npc)
        {
            var currentStage = GetCurrentStage(player, npc);
            if (currentStage == null) return new List<RelationshipRequirement>();

            var stagesOfType = _stagesByType[currentStage.Type];
            int currentIndex = stagesOfType.FindIndex(s => s.Id == currentStage.Id);
            
            if (currentIndex < 0 || currentIndex >= stagesOfType.Count - 1) 
                return new List<RelationshipRequirement>();
            
            var nextStage = stagesOfType[currentIndex + 1];
            return nextStage.AdvancementRequirements;
        }

        // Calculate and update progress based on relationship parameters and requirements
        public void UpdateRelationshipProgress(ICharacter player, ICharacter npc)
        {
            var data = GetOrCreateRelationshipData(player, npc);
            var currentStage = GetCurrentStage(player, npc);
            var requirements = GetNextStageRequirements(player, npc);
            
            if (requirements.Count == 0) return;
            
            // Calculate progress based on requirements met
            int totalReqs = requirements.Count;
            int metReqs = 0;
            bool allMandatoryMet = true;
            
            foreach (var req in requirements)
            {
                bool isMet = CheckRequirementMet(req, player, npc, data);
                if (isMet) metReqs++;
                else if (req.IsMandatory) allMandatoryMet = false;
            }
            
            // Update progress
            data.ProgressToNextStage = totalReqs > 0 ? (float)metReqs / totalReqs : 0;
            
            // Check if all requirements are met for advancement
            if (metReqs == totalReqs || (allMandatoryMet && metReqs > 0))
            {
                var nextStage = GetNextStage(currentStage);
                if (nextStage != null)
                {
                    OnNewStageRequirementsMet?.Invoke(player, npc, nextStage);
                }
            }
        }

        private RelationshipStage GetNextStage(RelationshipStage currentStage)
        {
            if (currentStage == null) return null;
            
            var stagesOfType = _stagesByType[currentStage.Type];
            int currentIndex = stagesOfType.FindIndex(s => s.Id == currentStage.Id);
            
            if (currentIndex < 0 || currentIndex >= stagesOfType.Count - 1) 
                return null;
            
            return stagesOfType[currentIndex + 1];
        }

        private bool CheckRequirementMet(RelationshipRequirement req, ICharacter player, ICharacter npc, RelationshipData data)
        {
            switch (req.Type)
            {
                case RequirementType.StatValue:
                    var stats = player.GetStats();
                    if (stats.TryGetValue(req.ParameterId, out float statValue))
                        return statValue >= req.RequiredValue;
                        
                    if (data.Parameters.TryGetValue(req.ParameterId, out float value))
                        return value >= req.RequiredValue;
                    return false;
                    
                case RequirementType.EventCompletion:
                    bool eventInCharacterHistory = player.HasCompletedEvent(req.ParameterId);
                    bool eventInRelationshipHistory = data.CompletedEvents.Contains(req.ParameterId);
                    return eventInCharacterHistory || eventInRelationshipHistory;
                    
                case RequirementType.TimeSpent:
                    if (data.StageAdvancementDates.TryGetValue(data.CurrentStageId, out DateTime stageDate))
                        return (DateTime.Now - stageDate).TotalDays >= req.RequiredValue;
                    return false;
                    
                case RequirementType.KeyDecision:
                    return data.DecisionHistory.ContainsKey(req.ParameterId);
                
                case RequirementType.ItemPossession:
                    // Implement using character's inventory system
                    return player.HasFlag("item_" + req.ParameterId);
                    
                case RequirementType.SkillLevel:
                    // Would check player's skill system
                    var playerState = player.GetState();
                    if (playerState.TryGetValue("skill_" + req.ParameterId, out object skillObj) && 
                        skillObj is float skillValue)
                        return skillValue >= req.RequiredValue;
                    return false;
                    
                case RequirementType.LocationVisits:
                    // Would check if player has visited location enough times
                    return player.HasFlag("visited_" + req.ParameterId);
                    
                // Additional requirement types would be implemented here
                
                default:
                    return false;
            }
        }

        // IProgressionTriggerSystem Implementation
        public void RegisterTrigger(ProgressionTrigger trigger)
        {
            if (!_triggersById.ContainsKey(trigger.Id))
            {
                _triggersById[trigger.Id] = trigger;
            }
        }

        public List<ProgressionTrigger> GetActiveTriggers(ICharacter player, ICharacter npc)
        {
            var data = GetOrCreateRelationshipData(player, npc);
            
            return _triggersById.Values
                .Where(t => !t.IsOneTime || !data.TriggeredProgressionEvents.Contains(t.Id))
                .Where(t => t.Dependencies.All(d => data.TriggeredProgressionEvents.Contains(d)))
                .Where(t => t.CheckConditions(player, npc))
                .OrderByDescending(t => t.Priority)
                .ToList();
        }

        public bool CheckTriggerConditions(string triggerId, ICharacter player, ICharacter npc)
        {
            if (!_triggersById.TryGetValue(triggerId, out var trigger))
                return false;
                
            return trigger.CheckConditions(player, npc);
        }

        public void ActivateTrigger(string triggerId, ICharacter player, ICharacter npc)
        {
            if (!_triggersById.TryGetValue(triggerId, out var trigger))
                return;
                
            if (!CheckTriggerConditions(triggerId, player, npc))
                return;
                
            var data = GetOrCreateRelationshipData(player, npc);
            
            // Process trigger effects
            foreach (var effect in trigger.Effects)
            {
                ApplyTriggerEffect(effect, player, npc);
            }
            
            if (trigger.IsOneTime)
            {
                data.TriggeredProgressionEvents.Add(triggerId);
            }
            
            // Notify listeners
            OnProgressionTriggerActivated?.Invoke(triggerId, player, npc);
        }

        private void ApplyTriggerEffect(TriggerEffect effect, ICharacter player, ICharacter npc)
        {
            switch (effect.Type)
            {
                case EffectType.AdvanceStage:
                    AdvanceToNextStage(player, npc);
                    break;
                    
                case EffectType.UnlockEvent:
                    // Implementation would depend on event system
                    break;
                    
                case EffectType.ModifyStat:
                    // Modify relationship parameter
                    var data = GetOrCreateRelationshipData(player, npc);
                    if (effect.Value is float statValue)
                    {
                        if (!data.Parameters.ContainsKey(effect.TargetId))
                            data.Parameters[effect.TargetId] = 0;
                            
                        data.Parameters[effect.TargetId] += statValue;
                    }
                    break;
                    
                // Additional effect types would be implemented here
            }
        }

        // IKeyDecisionSystem Implementation
        public void RegisterKeyDecision(KeyDecision decision)
        {
            if (!_decisionsById.ContainsKey(decision.Id))
            {
                _decisionsById[decision.Id] = decision;
            }
        }

        public List<KeyDecision> GetPendingKeyDecisions(ICharacter player)
        {
            DateTime now = DateTime.Now;
            var eventManager = GameObject.FindFirstObjectByType<EventManager>();
            
            return _decisionsById.Values
                .Where(d => !GetPreviousDecisions(player).ContainsKey(d.Id))
                .Where(d => d.ExpirationDate > now || d.ExpirationDate == default)
                .Where(d => CheckDecisionConditions(d, player, eventManager))
                .ToList();
        }
        
        private bool CheckDecisionConditions(KeyDecision decision, ICharacter player, EventManager eventManager)
        {
            if (decision.ActivationConditions == null || decision.ActivationConditions.Count == 0)
                return true;
                
            if (eventManager != null)
            {
                var dummyEvent = new GameEvent { Conditions = decision.ActivationConditions };
                return eventManager.CheckEventConditions(dummyEvent, player);
            }
            
            // Fallback simple implementation if EventManager is not available
            return true;
        }

        public void MakeDecision(string decisionId, string choiceId, ICharacter player)
        {
            if (!_decisionsById.TryGetValue(decisionId, out var decision))
                return;
                
            var choice = decision.Choices.FirstOrDefault(c => c.Id == choiceId);
            if (choice == null) return;
            
            // Apply decision effects
            ApplyDecisionEffects(choice, player);
            
            // Store decision
            foreach (var data in _relationshipData.Values.Where(d => d.PlayerId == player.Id))
            {
                data.DecisionHistory[decisionId] = choiceId;
            }
            
            // Notify listeners
            OnDecisionMade?.Invoke(decisionId, choiceId, player);
        }

        public Dictionary<string, string> GetPreviousDecisions(ICharacter player)
        {
            // For simplicity, just return the first relationship data found for this player
            var data = _relationshipData.Values.FirstOrDefault(d => d.PlayerId == player.Id);
            return data?.DecisionHistory ?? new Dictionary<string, string>();
        }

        private void ApplyDecisionEffects(DecisionChoice choice, ICharacter player)
        {
            // Check if the choice is available based on conditions
            if (choice.AvailabilityConditions != null && choice.AvailabilityConditions.Count > 0)
            {
                var eventManager = GameObject.FindFirstObjectByType<EventManager>();
                if (eventManager != null)
                {
                    var dummyEvent = new GameEvent { Conditions = choice.AvailabilityConditions };
                    if (!eventManager.CheckEventConditions(dummyEvent, player))
                    {
                        Debug.LogWarning($"Choice {choice.Id} is not available due to unfulfilled conditions");
                        return;
                    }
                }
            }
            
            // Handle relationship changes
            foreach (var effect in choice.RelationshipChanges)
            {
                // Find the NPC
                // In a real implementation, this would look up the character by ID using GameObject.Find or a registry
                ICharacter npc = null; // Get NPC by ID logic
                
                if (npc != null)
                {
                    var data = GetOrCreateRelationshipData(player, npc);
                    string paramId = effect.ParameterId ?? "relationship";
                    
                    if (!data.Parameters.ContainsKey(paramId))
                        data.Parameters[paramId] = 0;
                        
                    data.Parameters[paramId] += effect.Value;
                    
                    // Update relationship progress after parameter change
                    UpdateRelationshipProgress(player, npc);
                }
                else
                {
                    // If we can't find the character, apply directly to player relationships
                    var relationships = player.GetRelationships();
                    // In a real implementation this would modify the relationship directly
                    // For now we'll just log it
                    string characterId = effect.CharacterId ?? "unknown";
                    string paramId = effect.ParameterId ?? "relationship";
                    Debug.Log($"Would apply relationship change: {characterId} / {paramId} = {effect.Value}");
                }
            }
            
            // Handle content unlocking
            if (choice.UnlockedContent != null && choice.UnlockedContent.Count > 0)
            {
                foreach (var contentId in choice.UnlockedContent)
                {
                    player.SetFlag("content_" + contentId, true);
                    Debug.Log($"Unlocked content: {contentId}");
                }
            }
            
            // Handle content blocking
            if (choice.BlockedContent != null && choice.BlockedContent.Count > 0)
            {
                foreach (var contentId in choice.BlockedContent)
                {
                    player.SetFlag("content_" + contentId, false);
                    Debug.Log($"Blocked content: {contentId}");
                }
            }
            
            // Handle other effects (content unlocking, etc.)
            if (choice.Effects != null)
            {
                foreach (var effect in choice.Effects)
                {
                    if (effect.Value is float floatValue)
                    {
                        if (effect.Key.StartsWith("stat."))
                        {
                            string statName = effect.Key.Substring(5);
                            // Apply to player stats - in a real implementation this would use a stat system
                            Debug.Log($"Would modify stat: {statName} by {floatValue}");
                        }
                        else if (effect.Key.StartsWith("flag."))
                        {
                            string flagName = effect.Key.Substring(5);
                            player.SetFlag(flagName, floatValue > 0);
                            Debug.Log($"Set flag: {flagName} = {floatValue > 0}");
                        }
                    }
                }
            }
        }
        
        // Change relationship type (e.g., from friendship to romance)
        public void ChangeRelationshipType(ICharacter player, ICharacter npc, RelationshipType newType)
        {
            var data = GetOrCreateRelationshipData(player, npc);
            var currentStage = GetCurrentStage(player, npc);
            
            if (currentStage == null || currentStage.Type == newType) return;
            
            RelationshipType oldType = currentStage.Type;
            
            // Find appropriate stage in new relationship type
            // Typically start at the beginning, but could have logic to map to equivalent stages
            var newStage = _stagesByType[newType].FirstOrDefault();
            if (newStage == null) return;
            
            // Update relationship data
            data.CurrentStageId = newStage.Id;
            data.ProgressToNextStage = 0;
            data.StageAdvancementDates[newStage.Id] = DateTime.Now;
            
            // Notify listeners
            OnRelationshipTypeChanged?.Invoke(player, npc, oldType, newType);
            OnRelationshipStageChanged?.Invoke(player, npc, currentStage, newStage);
        }
        
        // Modify a relationship parameter
        public void ModifyRelationshipParameter(ICharacter player, ICharacter npc, string parameterId, float delta)
        {
            var data = GetOrCreateRelationshipData(player, npc);
            
            // Initialize if not exists
            if (!data.Parameters.ContainsKey(parameterId))
                data.Parameters[parameterId] = 0;
                
            data.Parameters[parameterId] += delta;
            
            // Update progress after parameter change
            UpdateRelationshipProgress(player, npc);
            
            // Check for triggers that might be activated by this change
            CheckAndActivateTriggersForParameter(player, npc, parameterId);
        }
        
        private void CheckAndActivateTriggersForParameter(ICharacter player, ICharacter npc, string parameterId)
        {
            foreach (var trigger in _triggersById.Values)
            {
                // Since we can't directly check for the parameter in the condition, 
                // we'll simply check all triggers of Statistical type
                if (trigger.Type == TriggerType.Statistical && CheckTriggerConditions(trigger.Id, player, npc))
                {
                    ActivateTrigger(trigger.Id, player, npc);
                }
            }
        }
        
        // Record completion of an event that might affect relationships
        public void RecordEventCompletion(ICharacter player, ICharacter npc, string eventId)
        {
            var data = GetOrCreateRelationshipData(player, npc);
            
            if (!data.CompletedEvents.Contains(eventId))
            {
                data.CompletedEvents.Add(eventId);
                
                // Update progress after event completion
                UpdateRelationshipProgress(player, npc);
                
                // Check for triggers that might be activated by this event
                CheckAndActivateTriggersForEvent(player, npc, eventId);
            }
        }
        
        private void CheckAndActivateTriggersForEvent(ICharacter player, ICharacter npc, string eventId)
        {
            foreach (var trigger in _triggersById.Values)
            {
                // Check if this trigger has conditions related to the completed event
                // Since we can't directly check for the event ID in the condition, we'll just check all triggers
                if (trigger.Type == TriggerType.EventCompletion && CheckTriggerConditions(trigger.Id, player, npc))
                {
                    ActivateTrigger(trigger.Id, player, npc);
                }
            }
        }
        
        // Schedule a key decision to be presented to the player
        public void ScheduleKeyDecision(string decisionId, ICharacter player, DateTime? expirationDate = null)
        {
            if (!_decisionsById.TryGetValue(decisionId, out var decision))
                return;
                
            // Clone the decision to avoid modifying the template
            KeyDecision activeDecision = new KeyDecision
            {
                Id = decision.Id,
                Title = decision.Title,
                Description = decision.Description,
                Choices = decision.Choices,
                ActivationConditions = decision.ActivationConditions,
                DecisionTimeLimit = decision.DecisionTimeLimit,
                IsForcedChoice = decision.IsForcedChoice,
                ExpirationDate = expirationDate ?? decision.ExpirationDate
            };
            
            _activeDecisions.Add(activeDecision);
            
            // Notify listeners
            OnKeyDecisionRequired?.Invoke(activeDecision, player);
        }
        
        // Load saved relationship data
        public void LoadRelationshipData(Dictionary<string, RelationshipData> savedData)
        {
            _relationshipData = new Dictionary<string, RelationshipData>(savedData);
        }
        
        // Get serializable relationship data for saving
        public Dictionary<string, RelationshipData> GetSerializableRelationshipData()
        {
            return new Dictionary<string, RelationshipData>(_relationshipData);
        }
    }
}