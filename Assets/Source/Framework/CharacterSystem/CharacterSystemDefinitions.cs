using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    #region Core Interfaces

    /// <summary>
    /// Base interface for all character subsystems
    /// </summary>
    public interface ICharacterSubsystem
    {
        void Initialize();
        void Update(float deltaTime);
        void Reset();
    }

    /// <summary>
    /// Interface for the Psychology System
    /// </summary>
    public interface IPsychologySystem : ICharacterSubsystem
    {
        //// Internal state update (basic update not dependent on external events)
        //void Update(float deltaTime);
        
        // Response to external events (receives only event objects)
        EmotionalResponse ProcessEvent(PsychologyEvent psychologyEvent);
        
        // Query of current state (returns read-only data)
        ReadOnlyStateData QueryState(string characterId, StateQuery query);
        
        // Calculate how an action affects desires
        Dictionary<string, float> CalculateDesireEffect(string characterId, string actionId, Dictionary<string, object> contextParameters);
        
        // Generate emotional response to a situation
        EmotionalResponse GenerateEmotionalResponse(string characterId, string situationId, float intensityLevel);
        
        // Evaluate and resolve internal conflicts at a decision point
        ConflictResolution EvaluateInternalConflict(string characterId, string decisionPoint);
    }

    /// <summary>
    /// Read-only state data for returning from the query method
    /// </summary>
    [Serializable]
    public class ReadOnlyStateData
    {
        public string dataType;
        public Dictionary<string, object> values = new Dictionary<string, object>();
    }

    /// <summary>
    /// Query parameters for requesting specific state information
    /// </summary>
    [Serializable]
    public class StateQuery
    {
        public string queryType;  // "desire", "emotion", "mood", "conflict", etc.
        public string specificId; // Specific desire/emotion ID if needed
        public int detailLevel;   // Level of detail requested (0: basic, 1: medium, 2: detailed)
    }

    /// <summary>
    /// Base event class for psychology system events
    /// </summary>
    [Serializable]
    public class PsychologyEvent
    {
        public string characterId;
        public string eventType;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
        public long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    #endregion

    #region Event Bus System

    /// <summary>
    /// Central event bus for character system events
    /// </summary>
    public class CharacterEventBus : MonoBehaviour
    {
        private static CharacterEventBus _instance;
        public static CharacterEventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CharacterEventBus>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CharacterEventBus");
                        _instance = go.AddComponent<CharacterEventBus>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Dictionary of event handlers by event type
        private Dictionary<string, List<Action<PsychologyEvent>>> eventHandlers = new Dictionary<string, List<Action<PsychologyEvent>>>();
        
        // Subscribe to an event type
        public void Subscribe(string eventType, Action<PsychologyEvent> handler)
        {
            if (!eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType] = new List<Action<PsychologyEvent>>();
            }
            
            if (!eventHandlers[eventType].Contains(handler))
            {
                eventHandlers[eventType].Add(handler);
            }
        }
        
        // Unsubscribe from an event type
        public void Unsubscribe(string eventType, Action<PsychologyEvent> handler)
        {
            if (eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType].Remove(handler);
                
                if (eventHandlers[eventType].Count == 0)
                {
                    eventHandlers.Remove(eventType);
                }
            }
        }
        
        // Publish an event
        public void Publish(PsychologyEvent evt)
        {
            // Log event for debugging
            Debug.Log($"[EventBus] Publishing event: {evt.eventType} for character {evt.characterId}");
            
            // Notify handlers for this specific event type
            if (eventHandlers.ContainsKey(evt.eventType))
            {
                foreach (var handler in eventHandlers[evt.eventType])
                {
                    handler.Invoke(evt);
                }
            }
            
            // Notify handlers for "all" events
            if (eventHandlers.ContainsKey("all"))
            {
                foreach (var handler in eventHandlers["all"])
                {
                    handler.Invoke(evt);
                }
            }
        }
    }

    #endregion

    #region Event Types

    /// <summary>
    /// Event for desire changes
    /// </summary>
    [Serializable]
    public class DesireChangeEvent : PsychologyEvent
    {
        public string desireType;
        public float oldValue;
        public float newValue;
        public bool isDominant;
        
        public DesireChangeEvent()
        {
            eventType = "desire_change";
        }
    }

    /// <summary>
    /// Event for mood changes
    /// </summary>
    [Serializable]
    public class MoodChangeEvent : PsychologyEvent
    {
        public string oldMood;
        public string newMood;
        public Dictionary<string, float> emotionValues = new Dictionary<string, float>();
        
        public MoodChangeEvent()
        {
            eventType = "mood_change";
        }
    }

    /// <summary>
    /// Event for emotional responses
    /// </summary>
    [Serializable]
    public class EmotionalResponseEvent : PsychologyEvent
    {
        public EmotionalResponse response;
        
        public EmotionalResponseEvent()
        {
            eventType = "emotional_response";
        }
    }

    /// <summary>
    /// Event for conflict resolutions
    /// </summary>
    [Serializable]
    public class ConflictResolutionEvent : PsychologyEvent
    {
        public ConflictResolution resolution;
        
        public ConflictResolutionEvent()
        {
            eventType = "conflict_resolution";
        }
    }

    /// <summary>
    /// Event for action effects
    /// </summary>
    [Serializable]
    public class ActionEffectEvent : PsychologyEvent
    {
        public string actionId;
        public Dictionary<string, float> desireEffects = new Dictionary<string, float>();
        
        public ActionEffectEvent()
        {
            eventType = "action_effect";
        }
    }

    #endregion

    #region Scriptable Object Definitions
    
    /// <summary>
    /// Base class for all character system data definitions
    /// </summary>
    public abstract class CharacterSystemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string description;
    }
    
    /// <summary>
    /// Definition of a desire type
    /// </summary>
    [CreateAssetMenu(fileName = "New Desire Type", menuName = "Character System/Desire Type")]
    public class DesireTypeDefinition : CharacterSystemDefinition
    {
        [Range(0, 100)]
        public float defaultBaseLevel = 50f;
        
        [Range(0, 100)]
        public float defaultThresholdLow = 30f;
        
        [Range(0, 100)]
        public float defaultThresholdHigh = 70f;
        
        [Range(0, 5)]
        public float defaultDecayRate = 1f;
        
        // Defines how this desire type affects emotions when satisfied or unsatisfied
        [Serializable]
        public class EmotionModifier
        {
            public string emotionType;
            
            [Range(-100, 100)]
            public float satisfiedEffect;
            
            [Range(-100, 100)]
            public float unsatisfiedEffect;
        }
        
        public List<EmotionModifier> emotionModifiers = new List<EmotionModifier>();
        
        // Icon to represent this desire type in UI
        public Sprite icon;
    }
    
    /// <summary>
    /// Definition of an emotion type
    /// </summary>
    [CreateAssetMenu(fileName = "New Emotion Type", menuName = "Character System/Emotion Type")]
    public class EmotionTypeDefinition : CharacterSystemDefinition
    {
        // Is this a positive or negative emotion?
        public enum EmotionValence
        {
            Positive,
            Negative,
            Neutral
        }
        
        public EmotionValence valence = EmotionValence.Neutral;
        
        [Range(0, 5)]
        public float defaultDecayRate = 1f;
        
        [Range(0, 1)]
        public float defaultVolatility = 0.5f;
        
        // Visual expression to use when this emotion is dominant
        public Sprite expressionIcon;
        
        // Animation trigger to activate when this emotion is dominant
        public string animationTrigger;
        
        // Relationships to other emotions
        [Serializable]
        public class EmotionRelationship
        {
            public string relatedEmotionType;
            
            [Tooltip("How much this emotion affects the related emotion")]
            [Range(-1, 1)]
            public float influence;
        }
        
        public List<EmotionRelationship> relationships = new List<EmotionRelationship>();
    }
    
    /// <summary>
    /// Definition of a personality type
    /// </summary>
    [CreateAssetMenu(fileName = "New Personality Type", menuName = "Character System/Personality Type")]
    public class PersonalityTypeDefinition : CharacterSystemDefinition
    {
        // Modifiers for desire base levels
        [Serializable]
        public class DesireModifier
        {
            public string desireType;
            
            [Range(-50, 50)]
            public float baseModifier;
            
            [Range(0.5f, 2f)]
            public float decayRateMultiplier = 1f;
        }
        
        public List<DesireModifier> desireModifiers = new List<DesireModifier>();
        
        // Modifiers for emotional volatility
        [Serializable]
        public class EmotionModifier
        {
            public string emotionType;
            
            [Range(0.5f, 2f)]
            public float volatilityMultiplier = 1f;
            
            [Range(0.5f, 2f)]
            public float decayRateMultiplier = 1f;
        }
        
        public List<EmotionModifier> emotionModifiers = new List<EmotionModifier>();
        
        // Starting values for personal values
        [Serializable]
        public class ValuePreference
        {
            public string valueType;
            
            [Range(0, 100)]
            public float initialImportance = 50f;
        }
        
        public List<ValuePreference> valuePreferences = new List<ValuePreference>();
    }
    
    /// <summary>
    /// Definition of a character template
    /// </summary>
    [CreateAssetMenu(fileName = "New Character Template", menuName = "Character System/Character Template")]
    public class CharacterTemplateDefinition : CharacterSystemDefinition
    {
        // Base information
        [Range(18, 100)]
        public int defaultAge = 30;
        
        public string defaultGender = "Unspecified";
        public string defaultOccupation = "Unspecified";
        public string personalityType;
        public string defaultBackstory = "";
        public string defaultRelationshipStatus = "Single";
        
        // Visual defaults
        public Sprite defaultPortrait;
        public GameObject defaultModel;
        
        // Starting desires
        [Serializable]
        public class DesirePreset
        {
            public string desireType;
            
            [Range(0, 100)]
            public float initialValue = 50f;
            
            [Range(0, 5)]
            public float decayRateOverride = 0f;
        }
        
        public List<DesirePreset> desirePresets = new List<DesirePreset>();
        
        // Starting emotional states
        [Serializable]
        public class EmotionPreset
        {
            public string emotionType;
            
            [Range(-100, 100)]
            public float initialValue = 0f;
        }
        
        public List<EmotionPreset> emotionPresets = new List<EmotionPreset>();
        
        // Initial internal conflicts
        [Serializable]
        public class ConflictPreset
        {
            public string conflictType;
            
            [Range(0, 100)]
            public float firstValueInitial = 50f;
            
            [Range(0, 100)]
            public float secondValueInitial = 50f;
        }
        
        public List<ConflictPreset> conflictPresets = new List<ConflictPreset>();
    }
    #endregion
}