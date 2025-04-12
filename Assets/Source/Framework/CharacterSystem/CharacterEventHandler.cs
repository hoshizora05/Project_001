using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace CharacterSystem
{
    /// <summary>
    /// Unity event definitions for UI updates
    /// </summary>
    [Serializable]
    public class CharacterUIEvent : UnityEvent<string> { }
    
    [Serializable]
    public class EmotionalResponseUIEvent : UnityEvent<EmotionalResponse> { }
    
    [Serializable]
    public class DesireChangeUIEvent : UnityEvent<string, float> { }
    
    [Serializable]
    public class MoodChangeUIEvent : UnityEvent<string> { }
    
    [Serializable]
    public class ConflictResolutionUIEvent : UnityEvent<ConflictResolution> { }
    
    /// <summary>
    /// Bridge between the character event bus and Unity's event system
    /// Listens for central events and forwards them to UI components
    /// </summary>
    public class CharacterEventHandler : MonoBehaviour
    {
        [Header("Unity Events")]
        public CharacterUIEvent onDominantDesireChanged;
        public EmotionalResponseUIEvent onEmotionalResponse;
        public DesireChangeUIEvent onDesireChanged;
        public MoodChangeUIEvent onMoodChanged;
        public ConflictResolutionUIEvent onConflictResolved;
        
        [Header("Debug")]
        [SerializeField] private bool logEvents = false;
        
        private string characterId;
        
        private void Start()
        {
            // Get character ID from attached component
            characterId = GetComponent<CharacterIdentifier>()?.characterId;
            
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogError("CharacterEventHandler: No character ID found!");
                return;
            }
            
            // Subscribe to all relevant events from the central event bus
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe when destroyed to prevent memory leaks
            UnsubscribeFromEvents();
        }
        
        /// <summary>
        /// Subscribe to all character events from the event bus
        /// </summary>
        private void SubscribeToEvents()
        {
            var eventBus = CharacterEventBus.Instance;
            
            // Subscribe to desire change events
            eventBus.Subscribe("desire_change", HandleDesireChangeEvent);
            
            // Subscribe to mood change events
            eventBus.Subscribe("mood_change", HandleMoodChangeEvent);
            
            // Subscribe to emotional response events
            eventBus.Subscribe("emotional_response", HandleEmotionalResponseEvent);
            
            // Subscribe to conflict resolution events
            eventBus.Subscribe("conflict_resolution", HandleConflictResolutionEvent);
        }
        
        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            var eventBus = CharacterEventBus.Instance;
            
            eventBus.Unsubscribe("desire_change", HandleDesireChangeEvent);
            eventBus.Unsubscribe("mood_change", HandleMoodChangeEvent);
            eventBus.Unsubscribe("emotional_response", HandleEmotionalResponseEvent);
            eventBus.Unsubscribe("conflict_resolution", HandleConflictResolutionEvent);
        }
        
        /// <summary>
        /// Handle desire change events
        /// </summary>
        private void HandleDesireChangeEvent(PsychologyEvent evt)
        {
            // Only process events for this character
            if (evt.characterId != characterId)
                return;
                
            if (evt is CharacterSystem.DesireChangeEvent desireEvent)
            {
                if (logEvents)
                {
                    Debug.Log($"Character {characterId}: Desire {desireEvent.desireType} changed from {desireEvent.oldValue} to {desireEvent.newValue}");
                }
                
                // Invoke desire change event
                onDesireChanged?.Invoke(desireEvent.desireType, desireEvent.newValue);
                
                // If this is the dominant desire, also invoke that event
                if (desireEvent.isDominant)
                {
                    onDominantDesireChanged?.Invoke(desireEvent.desireType);
                }
            }
        }
        
        /// <summary>
        /// Handle mood change events
        /// </summary>
        private void HandleMoodChangeEvent(PsychologyEvent evt)
        {
            // Only process events for this character
            if (evt.characterId != characterId)
                return;
                
            if (evt is CharacterSystem.MoodChangeEvent moodEvent)
            {
                if (logEvents)
                {
                    Debug.Log($"Character {characterId}: Mood changed from {moodEvent.oldMood} to {moodEvent.newMood}");
                }
                
                // Invoke mood change event
                onMoodChanged?.Invoke(moodEvent.newMood);
            }
        }
        
        /// <summary>
        /// Handle emotional response events
        /// </summary>
        private void HandleEmotionalResponseEvent(PsychologyEvent evt)
        {
            // Only process events for this character
            if (evt.characterId != characterId)
                return;
                
            if (evt is CharacterSystem.EmotionalResponseEvent responseEvent)
            {
                if (logEvents)
                {
                    Debug.Log($"Character {characterId}: Emotional response to {responseEvent.response.situationId} - {responseEvent.response.responseType}");
                }
                
                // Invoke emotional response event
                onEmotionalResponse?.Invoke(responseEvent.response);
            }
        }
        
        /// <summary>
        /// Handle conflict resolution events
        /// </summary>
        private void HandleConflictResolutionEvent(PsychologyEvent evt)
        {
            // Only process events for this character
            if (evt.characterId != characterId)
                return;
                
            if (evt is CharacterSystem.ConflictResolutionEvent resolutionEvent)
            {
                if (logEvents)
                {
                    Debug.Log($"Character {characterId}: Conflict resolution for {resolutionEvent.resolution.decisionPoint}");
                }
                
                // Invoke conflict resolution event
                onConflictResolved?.Invoke(resolutionEvent.resolution);
            }
        }
    }
    
    /// <summary>
    /// Extended interaction API that uses events for cleaner integration
    /// </summary>
    public class EventAwareInteractionAPI : MonoBehaviour
    {
        [SerializeField] private CharacterInteractionAPI baseAPI;
        
        private void Awake()
        {
            if (baseAPI == null)
            {
                baseAPI = CharacterInteractionAPI.Instance;
            }
        }
        
        /// <summary>
        /// Process an interaction with a character using the event system
        /// </summary>
        public void TriggerAction(string characterId, string actionId, Dictionary<string, object> parameters = null)
        {
            baseAPI.TriggerAction(characterId, actionId, parameters);
        }
        
        /// <summary>
        /// Trigger a situation for a character using the event system
        /// </summary>
        public void TriggerSituation(string characterId, string situationId, float intensity = 0.5f, Dictionary<string, object> parameters = null)
        {
            baseAPI.TriggerSituation(characterId, situationId, intensity, parameters);
        }
        
        /// <summary>
        /// Trigger a decision for a character using the event system
        /// </summary>
        public void TriggerDecision(string characterId, string decisionPoint, Dictionary<string, object> parameters = null)
        {
            baseAPI.TriggerDecision(characterId, decisionPoint, parameters);
        }
    }
}