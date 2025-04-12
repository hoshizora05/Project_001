using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProgressionAndEventSystem
{
    #region Enums
    
    public enum EventType
    {
        Normal,
        Special,
        Seasonal,
        Anniversary,
        Crisis
    }
    
    public enum ConditionType
    {
        Time,
        Relationship,
        State,
        Location,
        Compound
    }
    
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Contains,
        NotContains
    }
    
    public enum LogicalOperator
    {
        AND,
        OR
    }
    
    public enum EventUICallbackType
    {
        EventStart,
        ChoicesShown,
        ProgressUpdated,
        ResultShown
    }
    
    #endregion
    
    #region Interfaces
    
    public interface ICharacter
    {
        string Id { get; }
        Dictionary<string, float> GetStats();
        Dictionary<string, object> GetState();
        Dictionary<string, float> GetRelationships();
        string GetCurrentLocation();
        bool HasFlag(string flagName);
        void SetFlag(string flagName, bool value);
        Dictionary<string, DateTime> GetEventHistory();
        void RecordEventOccurrence(string eventId);
    }
    
    public interface IEventManager
    {
        void RegisterEvent(GameEvent gameEvent);
        void UnregisterEvent(string eventId);
        List<GameEvent> GetAvailableEvents(ICharacter player);
        void TriggerEvent(string eventId, ICharacter player);
        bool CheckEventConditions(GameEvent gameEvent, ICharacter player);
        void UpdateEventStates();
    }
    
    public interface IEventTypeSystem
    {
        void RegisterEventType(EventType eventType);
        EventType GetEventType(string typeId);
        List<string> GetEventsByType(EventType eventType);
        Dictionary<EventType, int> GetEventTypeStatistics(ICharacter player);
    }
    
    public interface IEventEffectSystem
    {
        void ApplyEventEffects(GameEvent gameEvent, EventResult result, ICharacter player);
        void ReverseEventEffects(GameEvent gameEvent, ICharacter player);
        Dictionary<string, float> CalculateEffects(GameEvent gameEvent, List<EventChoice> choices);
        List<UnlockedContent> GetUnlockedContentFromEvent(GameEvent gameEvent, EventResult result);
    }
    
    public interface IEventUIManager
    {
        void DisplayEventStart(GameEvent gameEvent);
        void ShowEventChoices(List<EventChoice> choices);
        void DisplayEventProgress(float progress, string stageDescription);
        void DisplayEventResult(EventResult result);
        void RegisterEventUICallback(EventUICallbackType type, Action<object> callback);
    }
    
    #endregion
    
    #region Data Structures
    
    [Serializable]
    public class GameEvent
    {
        public string Id;
        public string Title;
        public string Description;
        public EventType Type;
        public int Priority;
        public List<EventCondition> Conditions;
        public List<EventStage> Stages;
        public Dictionary<string, object> EventData;
        public List<string> DependentEvents;
        public List<string> BlockedEvents;
        public bool IsRepeatable;
        public TimeSpan? CooldownPeriod;
        public DateTime? ExpirationDate;
    }
    
    [Serializable]
    public class EventCondition
    {
        public ConditionType Type;
        public string TargetId;
        public ComparisonOperator Operator;
        public object ExpectedValue;
        public List<EventCondition> SubConditions;
        public LogicalOperator SubConditionOperator;
    }
    
    [Serializable]
    public class EventStage
    {
        public string StageId;
        public string Description;
        public List<DialogueLine> Dialogue;
        public List<EventChoice> Choices;
        public Dictionary<string, StageEffect> Effects;
        public TimeSpan? TimeLimit;
        public List<EventCondition> ProgressConditions;
        public string NextStageId;
        public Dictionary<string, string> ConditionalNextStages;
    }
    
    [Serializable]
    public class DialogueLine
    {
        public string CharacterId;
        public string Text;
        public string EmotionState;
        public float Duration;
        public List<EventCondition> VisibilityConditions;
    }
    
    [Serializable]
    public class StageEffect
    {
        public string TargetId;
        public string EffectType;
        public float Value;
        public List<EventCondition> EffectConditions;
    }
    
    [Serializable]
    public class EventChoice
    {
        public string Id;
        public string Text;
        public string Description;
        public List<EventCondition> AvailabilityConditions;
        public Dictionary<string, float> Effects;
        public float SuccessRate;
        public Dictionary<string, object> ResultData;
        public List<EventCondition> UnlockConditions;
    }
    
    [Serializable]
    public class EventResult
    {
        public bool Success;
        public string CompletedStageId;
        public List<string> ChoicesMade;
        public Dictionary<string, float> AppliedEffects;
        public List<UnlockedContent> NewlyUnlockedContent;
        public Dictionary<string, object> ResultData;
        public DateTime CompletionTime;
    }
    
    [Serializable]
    public class UnlockedContent
    {
        public string ContentId;
        public string ContentType;
        public string Description;
        public Dictionary<string, object> ContentData;
    }
    
    #endregion
    
    #region Implementation
    
    public class EventManager : MonoBehaviour, IEventManager
    {
        private Dictionary<string, GameEvent> _registeredEvents = new Dictionary<string, GameEvent>();
        private Dictionary<string, DateTime> _lastTriggeredEvents = new Dictionary<string, DateTime>();
        private List<string> _activeEventIds = new List<string>();
        private IEventEffectSystem _effectSystem;
        private IEventTypeSystem _typeSystem;
        
        public event Action<GameEvent> OnEventRegistered;
        public event Action<GameEvent, ICharacter> OnEventTriggered;
        public event Action<GameEvent, EventResult, ICharacter> OnEventCompleted;
        public event Action<UnlockedContent> OnNewContentUnlocked;
        
        public void Awake()
        {
            _effectSystem = GetComponent<IEventEffectSystem>();
            _typeSystem = GetComponent<IEventTypeSystem>();
            
            if (_effectSystem == null)
                Debug.LogError("EventEffectSystem not found!");
                
            if (_typeSystem == null)
                Debug.LogError("EventTypeSystem not found!");
        }
        
        public void RegisterEvent(GameEvent gameEvent)
        {
            if (string.IsNullOrEmpty(gameEvent.Id))
            {
                Debug.LogError("Cannot register event with null or empty ID");
                return;
            }
            
            _registeredEvents[gameEvent.Id] = gameEvent;
            _typeSystem.RegisterEventType(gameEvent.Type);
            OnEventRegistered?.Invoke(gameEvent);
            
            Debug.Log($"Event registered: {gameEvent.Id} - {gameEvent.Title}");
        }
        
        public void UnregisterEvent(string eventId)
        {
            if (_registeredEvents.ContainsKey(eventId))
            {
                _registeredEvents.Remove(eventId);
                Debug.Log($"Event unregistered: {eventId}");
            }
            else
            {
                Debug.LogWarning($"Attempted to unregister non-existent event: {eventId}");
            }
        }
        
        public List<GameEvent> GetAvailableEvents(ICharacter player)
        {
            List<GameEvent> availableEvents = new List<GameEvent>();
            
            foreach (var eventItem in _registeredEvents.Values)
            {
                if (CheckEventAvailability(eventItem, player))
                {
                    availableEvents.Add(eventItem);
                }
            }
            
            // Sort events by priority (higher priority first)
            availableEvents.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return availableEvents;
        }
        
        public void TriggerEvent(string eventId, ICharacter player)
        {
            if (!_registeredEvents.TryGetValue(eventId, out GameEvent gameEvent))
            {
                Debug.LogError($"Failed to trigger event: {eventId} - Event not found");
                return;
            }
            
            if (!CheckEventAvailability(gameEvent, player))
            {
                Debug.LogWarning($"Cannot trigger event: {eventId} - Conditions not met");
                return;
            }
            
            // Check for blocked events
            if (_activeEventIds.Any(id => gameEvent.BlockedEvents.Contains(id)))
            {
                Debug.LogWarning($"Cannot trigger event: {eventId} - Blocked by active events");
                return;
            }
            
            _activeEventIds.Add(eventId);
            _lastTriggeredEvents[eventId] = DateTime.Now;
            player.RecordEventOccurrence(eventId);
            
            OnEventTriggered?.Invoke(gameEvent, player);
            Debug.Log($"Event triggered: {gameEvent.Id} - {gameEvent.Title}");
            
            // In a real implementation, this would start the event UI flow
            // For now, we'll just simulate success
            EventResult result = new EventResult
            {
                Success = true,
                CompletedStageId = gameEvent.Stages.Last().StageId,
                ChoicesMade = new List<string>(),
                AppliedEffects = new Dictionary<string, float>(),
                ResultData = new Dictionary<string, object>(),
                CompletionTime = DateTime.Now
            };
            
            CompleteEvent(gameEvent, result, player);
        }
        
        private void CompleteEvent(GameEvent gameEvent, EventResult result, ICharacter player)
        {
            _activeEventIds.Remove(gameEvent.Id);
            
            // Apply effects
            _effectSystem.ApplyEventEffects(gameEvent, result, player);
            
            // Check for newly unlocked content
            var unlockedContent = _effectSystem.GetUnlockedContentFromEvent(gameEvent, result);
            result.NewlyUnlockedContent = unlockedContent;
            
            foreach (var content in unlockedContent)
            {
                OnNewContentUnlocked?.Invoke(content);
            }
            
            // Trigger completion event
            OnEventCompleted?.Invoke(gameEvent, result, player);
            
            Debug.Log($"Event completed: {gameEvent.Id} - Success: {result.Success}");
        }
        
        // Method to handle player making a choice for an event
        // This would be used by the UI system when a player selects a choice
        public void MakeChoice(GameEvent gameEvent, EventChoice choice, ICharacter player)
        {
            // In a real implementation, this would:
            // 1. Record the choice
            // 2. Determine success/failure based on success rate
            // 3. Process to the next stage or complete the event
            // 4. Apply immediate effects from the choice
            
            Debug.Log($"Player made choice: {choice.Text} for event: {gameEvent.Title}");
            
            // In the real implementation, this is where you would use the OnEventChoiceMade event
            // OnEventChoiceMade?.Invoke(gameEvent, choice, player);
        }
        
        // Method to cancel an active event
        public void CancelEvent(string eventId, ICharacter player)
        {
            if (!_registeredEvents.TryGetValue(eventId, out GameEvent gameEvent))
            {
                Debug.LogWarning($"Attempted to cancel non-existent event: {eventId}");
                return;
            }
            
            if (!_activeEventIds.Contains(eventId))
            {
                Debug.LogWarning($"Attempted to cancel inactive event: {eventId}");
                return;
            }
            
            _activeEventIds.Remove(eventId);
            
            // In the real implementation, this is where you would use the OnEventCancelled event
            // OnEventCancelled?.Invoke(gameEvent, player);
            
            Debug.Log($"Event cancelled: {gameEvent.Title}");
        }
        
        public bool CheckEventConditions(GameEvent gameEvent, ICharacter player)
        {
            if (gameEvent.Conditions == null || gameEvent.Conditions.Count == 0)
                return true;
                
            return EvaluateConditions(gameEvent.Conditions, player);
        }
        
        private bool CheckEventAvailability(GameEvent gameEvent, ICharacter player)
        {
            // Check expiration
            if (gameEvent.ExpirationDate.HasValue && DateTime.Now > gameEvent.ExpirationDate.Value)
                return false;
                
            // Check cooldown
            if (_lastTriggeredEvents.TryGetValue(gameEvent.Id, out DateTime lastTriggered) &&
                gameEvent.CooldownPeriod.HasValue)
            {
                if (DateTime.Now - lastTriggered < gameEvent.CooldownPeriod.Value)
                    return false;
            }
            
            // Check repeatability
            if (!gameEvent.IsRepeatable && 
                player.GetEventHistory().ContainsKey(gameEvent.Id))
                return false;
                
            // Check dependencies
            foreach (var dependency in gameEvent.DependentEvents)
            {
                if (!player.GetEventHistory().ContainsKey(dependency))
                    return false;
            }
            
            // Check conditions
            return CheckEventConditions(gameEvent, player);
        }
        
        public void UpdateEventStates()
        {
            // This would be called periodically to check for events whose conditions have been met
            // For performance, we should only check a subset of events each frame
            
            // Implementation depends on the game's needs, but a basic version would be:
            // 1. Check for events that should be triggered automatically
            // 2. Update state of active events (e.g., time limits)
            // 3. Check for conflicting events and resolve priorities
            
            // Basic implementation to check for events with met conditions
            // In a real game, this would be optimized to only check a subset of events each frame
            /* 
            foreach (var eventItem in _registeredEvents.Values)
            {
                // Skip events that are already active
                if (_activeEventIds.Contains(eventItem.Id))
                    continue;
                    
                // Check if the event conditions are met
                if (CheckEventConditions(eventItem, player))
                {
                    // Notify that conditions are met (for events that require manual triggering)
                    // This is where we would use the OnEventConditionsMet event
                }
            }
            */
        }
        
        private bool EvaluateConditions(List<EventCondition> conditions, ICharacter player)
        {
            foreach (var condition in conditions)
            {
                bool result = EvaluateCondition(condition, player);
                if (!result)
                    return false; // If any condition fails, return false (implicit AND)
            }
            
            return true; // All conditions passed
        }
        
        private bool EvaluateCondition(EventCondition condition, ICharacter player)
        {
            switch (condition.Type)
            {
                case ConditionType.Compound:
                    return EvaluateCompoundCondition(condition, player);
                    
                case ConditionType.Time:
                    return EvaluateTimeCondition(condition);
                    
                case ConditionType.Relationship:
                    return EvaluateRelationshipCondition(condition, player);
                    
                case ConditionType.State:
                    return EvaluateStateCondition(condition, player);
                    
                case ConditionType.Location:
                    return EvaluateLocationCondition(condition, player);
                    
                default:
                    Debug.LogWarning($"Unknown condition type: {condition.Type}");
                    return false;
            }
        }
        
        private bool EvaluateCompoundCondition(EventCondition condition, ICharacter player)
        {
            if (condition.SubConditions == null || condition.SubConditions.Count == 0)
                return true;
                
            if (condition.SubConditionOperator == LogicalOperator.AND)
            {
                // All conditions must be true
                foreach (var subCondition in condition.SubConditions)
                {
                    if (!EvaluateCondition(subCondition, player))
                        return false;
                }
                return true;
            }
            else // OR
            {
                // At least one condition must be true
                foreach (var subCondition in condition.SubConditions)
                {
                    if (EvaluateCondition(subCondition, player))
                        return true;
                }
                return false;
            }
        }
        
        private bool EvaluateTimeCondition(EventCondition condition)
        {
            // In a real implementation, this would check game time, not real time
            // For now, we'll use real time as a placeholder
            
            DateTime now = DateTime.Now;
            DateTime expectedTime = (DateTime)condition.ExpectedValue;
            
            switch (condition.Operator)
            {
                case ComparisonOperator.Equal:
                    return now.Date == expectedTime.Date;
                case ComparisonOperator.NotEqual:
                    return now.Date != expectedTime.Date;
                case ComparisonOperator.GreaterThan:
                    return now > expectedTime;
                case ComparisonOperator.LessThan:
                    return now < expectedTime;
                case ComparisonOperator.GreaterThanOrEqual:
                    return now >= expectedTime;
                case ComparisonOperator.LessThanOrEqual:
                    return now <= expectedTime;
                default:
                    return false;
            }
        }
        
        private bool EvaluateRelationshipCondition(EventCondition condition, ICharacter player)
        {
            var relationships = player.GetRelationships();
            
            if (!relationships.TryGetValue(condition.TargetId, out float value))
                return false;
                
            float expectedValue = Convert.ToSingle(condition.ExpectedValue);
            
            switch (condition.Operator)
            {
                case ComparisonOperator.Equal:
                    return Math.Abs(value - expectedValue) < 0.001f;
                case ComparisonOperator.NotEqual:
                    return Math.Abs(value - expectedValue) >= 0.001f;
                case ComparisonOperator.GreaterThan:
                    return value > expectedValue;
                case ComparisonOperator.LessThan:
                    return value < expectedValue;
                case ComparisonOperator.GreaterThanOrEqual:
                    return value >= expectedValue;
                case ComparisonOperator.LessThanOrEqual:
                    return value <= expectedValue;
                default:
                    return false;
            }
        }
        
        private bool EvaluateStateCondition(EventCondition condition, ICharacter player)
        {
            if (condition.TargetId.StartsWith("flag:"))
            {
                string flagName = condition.TargetId.Substring(5);
                bool hasFlag = player.HasFlag(flagName);
                bool expectedValue = (bool)condition.ExpectedValue;
                
                switch (condition.Operator)
                {
                    case ComparisonOperator.Equal:
                        return hasFlag == expectedValue;
                    case ComparisonOperator.NotEqual:
                        return hasFlag != expectedValue;
                    default:
                        return false;
                }
            }
            else
            {
                var states = player.GetState();
                
                if (!states.TryGetValue(condition.TargetId, out object stateValue))
                    return false;
                    
                // Handle comparison based on type
                if (stateValue is float floatValue)
                {
                    float expectedValue = Convert.ToSingle(condition.ExpectedValue);
                    
                    switch (condition.Operator)
                    {
                        case ComparisonOperator.Equal:
                            return Math.Abs(floatValue - expectedValue) < 0.001f;
                        case ComparisonOperator.NotEqual:
                            return Math.Abs(floatValue - expectedValue) >= 0.001f;
                        case ComparisonOperator.GreaterThan:
                            return floatValue > expectedValue;
                        case ComparisonOperator.LessThan:
                            return floatValue < expectedValue;
                        case ComparisonOperator.GreaterThanOrEqual:
                            return floatValue >= expectedValue;
                        case ComparisonOperator.LessThanOrEqual:
                            return floatValue <= expectedValue;
                        default:
                            return false;
                    }
                }
                else if (stateValue is string stringValue)
                {
                    string expectedValue = condition.ExpectedValue.ToString();
                    
                    switch (condition.Operator)
                    {
                        case ComparisonOperator.Equal:
                            return stringValue == expectedValue;
                        case ComparisonOperator.NotEqual:
                            return stringValue != expectedValue;
                        case ComparisonOperator.Contains:
                            return stringValue.Contains(expectedValue);
                        case ComparisonOperator.NotContains:
                            return !stringValue.Contains(expectedValue);
                        default:
                            return false;
                    }
                }
                
                // Default case - direct comparison
                return stateValue.Equals(condition.ExpectedValue);
            }
        }
        
        private bool EvaluateLocationCondition(EventCondition condition, ICharacter player)
        {
            string currentLocation = player.GetCurrentLocation();
            string expectedLocation = condition.ExpectedValue.ToString();
            
            switch (condition.Operator)
            {
                case ComparisonOperator.Equal:
                    return currentLocation == expectedLocation;
                case ComparisonOperator.NotEqual:
                    return currentLocation != expectedLocation;
                case ComparisonOperator.Contains:
                    return currentLocation.Contains(expectedLocation);
                case ComparisonOperator.NotContains:
                    return !currentLocation.Contains(expectedLocation);
                default:
                    return false;
            }
        }
    }
    
    public class EventTypeSystem : MonoBehaviour, IEventTypeSystem
    {
        private Dictionary<string, EventType> _registeredTypes = new Dictionary<string, EventType>();
        private Dictionary<EventType, List<string>> _eventsByType = new Dictionary<EventType, List<string>>();
        
        public void RegisterEventType(EventType eventType)
        {
            if (!_eventsByType.ContainsKey(eventType))
            {
                _eventsByType[eventType] = new List<string>();
            }
        }
        
        public void RegisterEvent(string eventId, EventType eventType)
        {
            // Register the event type
            RegisterEventType(eventType);
            
            // Add to event types
            if (!_eventsByType[eventType].Contains(eventId))
            {
                _eventsByType[eventType].Add(eventId);
            }
            
            // Register type by ID
            _registeredTypes[eventId] = eventType;
        }
        
        public EventType GetEventType(string eventId)
        {
            if (_registeredTypes.TryGetValue(eventId, out EventType type))
                return type;
                
            Debug.LogWarning($"Event type not found for event ID: {eventId}");
            return EventType.Normal; // Default
        }
        
        public List<string> GetEventsByType(EventType eventType)
        {
            if (_eventsByType.TryGetValue(eventType, out List<string> events))
                return events;
                
            return new List<string>();
        }
        
        public Dictionary<EventType, int> GetEventTypeStatistics(ICharacter player)
        {
            Dictionary<EventType, int> stats = new Dictionary<EventType, int>();
            var history = player.GetEventHistory();
            
            foreach (var eventType in Enum.GetValues(typeof(EventType)).Cast<EventType>())
            {
                stats[eventType] = 0;
            }
            
            foreach (var eventId in history.Keys)
            {
                if (_registeredTypes.TryGetValue(eventId, out EventType type))
                {
                    stats[type]++;
                }
            }
            
            return stats;
        }
    }
    
    public class EventEffectSystem : MonoBehaviour, IEventEffectSystem
    {
        // Dependencies
        private EventManager _eventManager;
        
        void Awake()
        {
            _eventManager = GetComponent<EventManager>();
            
            if (_eventManager == null)
                Debug.LogError("EventManager not found!");
        }
        
        public void ApplyEventEffects(GameEvent gameEvent, EventResult result, ICharacter player)
        {
            if (result == null || !result.Success)
                return;
                
            Dictionary<string, float> appliedEffects = new Dictionary<string, float>();
            
            // Apply stage effects
            EventStage stage = gameEvent.Stages.FirstOrDefault(s => s.StageId == result.CompletedStageId);
            if (stage != null && stage.Effects != null)
            {
                foreach (var effect in stage.Effects)
                {
                    bool shouldApply = true;
                    
                    // Check conditions if any
                    if (effect.Value.EffectConditions != null && effect.Value.EffectConditions.Count > 0)
                    {
                        shouldApply = _eventManager.CheckEventConditions(
                            new GameEvent { Conditions = effect.Value.EffectConditions }, 
                            player
                        );
                    }
                    
                    if (shouldApply)
                    {
                        ApplyEffect(player, effect.Value.TargetId, effect.Value.EffectType, effect.Value.Value);
                        appliedEffects[effect.Value.TargetId] = effect.Value.Value;
                    }
                }
            }
            
            // Apply choice effects
            if (result.ChoicesMade != null)
            {
                foreach (var choiceId in result.ChoicesMade)
                {
                    // Find the choice in the completed stage
                    if (stage != null)
                    {
                        EventChoice choice = stage.Choices.FirstOrDefault(c => c.Id == choiceId);
                        if (choice != null && choice.Effects != null)
                        {
                            foreach (var effect in choice.Effects)
                            {
                                ApplyEffect(player, effect.Key, "choice", effect.Value);
                                appliedEffects[effect.Key] = effect.Value;
                            }
                        }
                    }
                }
            }
            
            result.AppliedEffects = appliedEffects;
        }
        
        public void ReverseEventEffects(GameEvent gameEvent, ICharacter player)
        {
            // This would reverse effects from an event, used for scenarios like:
            // - Event cancellation
            // - Game state rollback
            // - "Undo" functionality
            
            // Implementation depends on how effects are stored and tracked
            Debug.LogWarning("ReverseEventEffects not fully implemented");
        }
        
        public Dictionary<string, float> CalculateEffects(GameEvent gameEvent, List<EventChoice> choices)
        {
            Dictionary<string, float> totalEffects = new Dictionary<string, float>();
            
            // Calculate stage effects (assuming current/last stage)
            EventStage stage = gameEvent.Stages.LastOrDefault();
            if (stage != null && stage.Effects != null)
            {
                foreach (var effect in stage.Effects)
                {
                    string key = $"{effect.Value.EffectType}:{effect.Value.TargetId}";
                    if (!totalEffects.ContainsKey(key))
                        totalEffects[key] = 0;
                    
                    totalEffects[key] += effect.Value.Value;
                }
            }
            
            // Calculate choice effects
            if (choices != null)
            {
                foreach (var choice in choices)
                {
                    if (choice.Effects != null)
                    {
                        foreach (var effect in choice.Effects)
                        {
                            string key = $"choice:{effect.Key}";
                            if (!totalEffects.ContainsKey(key))
                                totalEffects[key] = 0;
                            
                            totalEffects[key] += effect.Value;
                        }
                    }
                }
            }
            
            return totalEffects;
        }
        
        public List<UnlockedContent> GetUnlockedContentFromEvent(GameEvent gameEvent, EventResult result)
        {
            List<UnlockedContent> unlockedContent = new List<UnlockedContent>();
            
            // This would check for content that should be unlocked based on the event result
            // For example, new events, locations, activities, or character information
            
            // Implementation depends on game-specific unlocking rules
            // For now, we'll assume it's defined in the result data
            
            if (result.ResultData != null && 
                result.ResultData.TryGetValue("unlockedContent", out object contentData) &&
                contentData is List<UnlockedContent> content)
            {
                unlockedContent.AddRange(content);
            }
            
            return unlockedContent;
        }
        
        private void ApplyEffect(ICharacter player, string targetId, string effectType, float value)
        {
            // Implement effect application based on the target and effect type
            // Examples:
            
            if (targetId.StartsWith("relationship:"))
            {
                string characterId = targetId.Substring(13);
                // Apply to relationship system
                Debug.Log($"Applying relationship effect: {characterId} = {value}");
                // In a real implementation, would call into character relationship system
            }
            else if (targetId.StartsWith("flag:"))
            {
                string flagName = targetId.Substring(5);
                player.SetFlag(flagName, value > 0);
                Debug.Log($"Setting flag: {flagName} = {value > 0}");
            }
            else if (targetId.StartsWith("stat:"))
            {
                string statName = targetId.Substring(5);
                // Apply to stat system
                Debug.Log($"Applying stat effect: {statName} = {value}");
                // In a real implementation, would call into player stat system
            }
            else
            {
                Debug.LogWarning($"Unknown effect target: {targetId}");
            }
        }
    }
    
    public class EventUIManager : MonoBehaviour, IEventUIManager
    {
        private Dictionary<EventUICallbackType, List<Action<object>>> _callbacks = new Dictionary<EventUICallbackType, List<Action<object>>>();
        
        public void DisplayEventStart(GameEvent gameEvent)
        {
            Debug.Log($"Displaying event start: {gameEvent.Title}");
            InvokeCallbacks(EventUICallbackType.EventStart, gameEvent);
            
            // In a real implementation, this would activate UI components
            // and set up the event display
        }
        
        public void ShowEventChoices(List<EventChoice> choices)
        {
            Debug.Log($"Showing {choices.Count} event choices");
            InvokeCallbacks(EventUICallbackType.ChoicesShown, choices);
            
            // In a real implementation, this would display choice UI elements
        }
        
        public void DisplayEventProgress(float progress, string stageDescription)
        {
            Debug.Log($"Event progress: {progress:P} - {stageDescription}");
            
            var progressData = new Dictionary<string, object>
            {
                { "progress", progress },
                { "description", stageDescription }
            };
            
            InvokeCallbacks(EventUICallbackType.ProgressUpdated, progressData);
            
            // In a real implementation, this would update progress bars and descriptions
        }
        
        public void DisplayEventResult(EventResult result)
        {
            Debug.Log($"Displaying event result - Success: {result.Success}");
            InvokeCallbacks(EventUICallbackType.ResultShown, result);
            
            // In a real implementation, this would show result UI with effects,
            // unlocked content, etc.
        }
        
        public void RegisterEventUICallback(EventUICallbackType type, Action<object> callback)
        {
            if (!_callbacks.ContainsKey(type))
            {
                _callbacks[type] = new List<Action<object>>();
            }
            
            _callbacks[type].Add(callback);
        }
        
        private void InvokeCallbacks(EventUICallbackType type, object data)
        {
            if (_callbacks.TryGetValue(type, out List<Action<object>> callbacks))
            {
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in event UI callback: {ex.Message}");
                    }
                }
            }
        }
    }
    
    // Main system controller that ties everything together
    public class ProgressionAndEventSystem : MonoBehaviour
    {
        private IEventManager _eventManager;
        private IEventTypeSystem _eventTypeSystem;
        private IEventEffectSystem _eventEffectSystem;
        private IEventUIManager _eventUIManager;
        
        private void Awake()
        {
            // Initialize components
            _eventManager = GetComponent<IEventManager>();
            _eventTypeSystem = GetComponent<IEventTypeSystem>();
            _eventEffectSystem = GetComponent<IEventEffectSystem>();
            _eventUIManager = GetComponent<IEventUIManager>();
            
            if (_eventManager == null)
                gameObject.AddComponent<EventManager>();
                
            if (_eventTypeSystem == null)
                gameObject.AddComponent<EventTypeSystem>();
                
            if (_eventEffectSystem == null)
                gameObject.AddComponent<EventEffectSystem>();
                
            if (_eventUIManager == null)
                gameObject.AddComponent<EventUIManager>();
                
            Debug.Log("Progression and Event System initialized");
        }
        
        private void Start()
        {
            // Connect to other game systems
            // Load event data
            // Initialize event listeners
        }
        
        private void Update()
        {
            // Regular updates for time-based triggers and active events
            if (_eventManager != null)
            {
                _eventManager.UpdateEventStates();
            }
        }
        
        // Public API methods
        
        public void RegisterEvent(GameEvent gameEvent)
        {
            _eventManager.RegisterEvent(gameEvent);
        }
        
        public List<GameEvent> GetAvailableEvents(ICharacter player)
        {
            return _eventManager.GetAvailableEvents(player);
        }
        
        public void TriggerEvent(string eventId, ICharacter player)
        {
            _eventManager.TriggerEvent(eventId, player);
        }
        
        public Dictionary<EventType, int> GetEventStatistics(ICharacter player)
        {
            return _eventTypeSystem.GetEventTypeStatistics(player);
        }
        
        // Utility methods for editor and debugging
        
        public void LogEventStructure(GameEvent gameEvent)
        {
            Debug.Log($"Event: {gameEvent.Id} - {gameEvent.Title}");
            Debug.Log($"Type: {gameEvent.Type}, Priority: {gameEvent.Priority}");
            Debug.Log($"Stages: {gameEvent.Stages.Count}, Repeatable: {gameEvent.IsRepeatable}");
            
            // Log dependencies
            if (gameEvent.DependentEvents != null && gameEvent.DependentEvents.Count > 0)
            {
                Debug.Log($"Dependencies: {string.Join(", ", gameEvent.DependentEvents)}");
            }
            
            // Log blocked events
            if (gameEvent.BlockedEvents != null && gameEvent.BlockedEvents.Count > 0)
            {
                Debug.Log($"Blocks: {string.Join(", ", gameEvent.BlockedEvents)}");
            }
        }
    }
    
    #endregion
}