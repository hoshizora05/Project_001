using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Core implementation of the character psychology system
    /// Handles desires, emotions, and internal conflicts
    /// </summary>
    public class CharacterPsychologySystem : MonoBehaviour, IPsychologySystem
    {
        [SerializeField] private float timeUnitInSeconds = 5f; // How often to update time-based effects
        private float timeSinceLastUpdate = 0f;
        private bool isInitialized = false;
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// Initialize the psychology system
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;
                
            Debug.Log("Initializing CharacterPsychologySystem");
            
            // Subscribe to relevant events
            SubscribeToEvents();
            
            isInitialized = true;
        }
        
        /// <summary>
        /// Reset the psychology system
        /// </summary>
        public void Reset()
        {
            Debug.Log("Resetting CharacterPsychologySystem");
            
            // Unsubscribe from events to avoid memory leaks
            UnsubscribeFromEvents();
            
            // Re-initialize
            isInitialized = false;
            Initialize();
        }
        
        /// <summary>
        /// Subscribe to the events this system cares about
        /// </summary>
        private void SubscribeToEvents()
        {
            var eventBus = CharacterEventBus.Instance;
            
            // Subscribe to action events
            eventBus.Subscribe("action_performed", HandleActionEvent);
            
            // Subscribe to situation events
            eventBus.Subscribe("situation_encountered", HandleSituationEvent);
            
            // Subscribe to decision events
            eventBus.Subscribe("decision_required", HandleDecisionEvent);
        }
        
        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            var eventBus = CharacterEventBus.Instance;
            
            eventBus.Unsubscribe("action_performed", HandleActionEvent);
            eventBus.Unsubscribe("situation_encountered", HandleSituationEvent);
            eventBus.Unsubscribe("decision_required", HandleDecisionEvent);
        }
        
        /// <summary>
        /// Handle events related to actions
        /// </summary>
        private void HandleActionEvent(PsychologyEvent evt)
        {
            if (evt.parameters.TryGetValue("actionId", out object actionIdObj) && actionIdObj is string actionId)
            {
                Dictionary<string, object> contextParams = new Dictionary<string, object>();
                
                // Extract context parameters if available
                if (evt.parameters.TryGetValue("contextParameters", out object contextObj) && 
                    contextObj is Dictionary<string, object> contextDict)
                {
                    contextParams = contextDict;
                }
                
                // Calculate and apply desire effects
                Dictionary<string, float> effects = CalculateDesireEffect(evt.characterId, actionId, contextParams);
                
                // Create and publish an effect event
                var effectEvent = new ActionEffectEvent
                {
                    characterId = evt.characterId,
                    actionId = actionId,
                    desireEffects = effects
                };
                
                CharacterEventBus.Instance.Publish(effectEvent);
                
                // Update mental state
                UpdateMentalState(evt.characterId, contextParams);
            }
        }
        
        /// <summary>
        /// Handle events related to situations
        /// </summary>
        private void HandleSituationEvent(PsychologyEvent evt)
        {
            if (evt.parameters.TryGetValue("situationId", out object situationIdObj) && situationIdObj is string situationId)
            {
                float intensity = 0.5f; // Default intensity
                
                // Extract intensity if available
                if (evt.parameters.TryGetValue("intensity", out object intensityObj) && intensityObj is float intensityValue)
                {
                    intensity = intensityValue;
                }
                
                // Generate emotional response
                EmotionalResponse response = GenerateEmotionalResponse(evt.characterId, situationId, intensity);
                
                // Create and publish response event
                var responseEvent = new EmotionalResponseEvent
                {
                    characterId = evt.characterId,
                    response = response
                };
                
                CharacterEventBus.Instance.Publish(responseEvent);
            }
        }
        
        /// <summary>
        /// Handle events related to decisions
        /// </summary>
        private void HandleDecisionEvent(PsychologyEvent evt)
        {
            if (evt.parameters.TryGetValue("decisionPoint", out object decisionObj) && decisionObj is string decisionPoint)
            {
                // Evaluate internal conflict
                ConflictResolution resolution = EvaluateInternalConflict(evt.characterId, decisionPoint);
                
                // Create and publish resolution event
                var resolutionEvent = new ConflictResolutionEvent
                {
                    characterId = evt.characterId,
                    resolution = resolution
                };
                
                CharacterEventBus.Instance.Publish(resolutionEvent);
            }
        }
        
        /// <summary>
        /// Process generic psychology events
        /// </summary>
        public EmotionalResponse ProcessEvent(PsychologyEvent evt)
        {
            EmotionalResponse response = null;
            
            switch (evt.eventType)
            {
                case "action_performed":
                    HandleActionEvent(evt);
                    break;
                    
                case "situation_encountered":
                    HandleSituationEvent(evt);
                    
                    // Return the emotional response for this event
                    if (evt.parameters.TryGetValue("situationId", out object situationIdObj) && situationIdObj is string situationId)
                    {
                        float intensity = 0.5f; // Default intensity
                        
                        if (evt.parameters.TryGetValue("intensity", out object intensityObj) && intensityObj is float intensityValue)
                        {
                            intensity = intensityValue;
                        }
                        
                        response = GenerateEmotionalResponse(evt.characterId, situationId, intensity);
                    }
                    break;
                    
                case "decision_required":
                    HandleDecisionEvent(evt);
                    break;
            }
            
            return response;
        }
        
        /// <summary>
        /// Unity Update method - calls the interface Update method with deltaTime
        /// </summary>
        private void Update()
        {
            if (!isInitialized)
                return;
                
            // Update time-based effects
            Update(Time.deltaTime);
        }
        
        /// <summary>
        /// Update the psychology system
        /// </summary>
        public void Update(float deltaTime)
        {
            timeSinceLastUpdate += deltaTime;
            
            if (timeSinceLastUpdate >= timeUnitInSeconds)
            {
                UpdateAllCharacters();
                timeSinceLastUpdate = 0f;
            }
        }
        
        /// <summary>
        /// Query a character's current state
        /// </summary>
        public ReadOnlyStateData QueryState(string characterId, StateQuery query)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return null;
                
            var result = new ReadOnlyStateData
            {
                dataType = query.queryType
            };
            
            switch (query.queryType)
            {
                case "desire":
                    if (!string.IsNullOrEmpty(query.specificId))
                    {
                        // Specific desire information
                        var desire = character.desires.desireTypes.Find(d => d.type == query.specificId);
                        
                        if (desire != null)
                        {
                            result.values["type"] = desire.type;
                            result.values["currentValue"] = desire.currentValue;
                            result.values["isDominant"] = (character.desires.dominantDesire == desire.type);
                            result.values["status"] = GetDesireStatus(desire);
                        }
                    }
                    else
                    {
                        // All desires
                        var desireDict = new Dictionary<string, object>();
                        
                        foreach (var desire in character.desires.desireTypes)
                        {
                            var desireData = new Dictionary<string, object>
                            {
                                ["currentValue"] = desire.currentValue,
                                ["isDominant"] = (character.desires.dominantDesire == desire.type),
                                ["status"] = GetDesireStatus(desire)
                            };
                            
                            desireDict[desire.type] = desireData;
                        }
                        
                        result.values["desires"] = desireDict;
                        result.values["dominantDesire"] = character.desires.dominantDesire;
                    }
                    break;
                    
                case "emotion":
                    if (!string.IsNullOrEmpty(query.specificId))
                    {
                        // Specific emotion information
                        var emotion = character.mentalState.emotionalStates.Find(e => e.type == query.specificId);
                        
                        if (emotion != null)
                        {
                            result.values["type"] = emotion.type;
                            result.values["currentValue"] = emotion.currentValue;
                            result.values["normalizedValue"] = emotion.currentValue / 100f;
                        }
                    }
                    else
                    {
                        // All emotions
                        var emotionDict = new Dictionary<string, object>();
                        
                        foreach (var emotion in character.mentalState.emotionalStates)
                        {
                            emotionDict[emotion.type] = emotion.currentValue;
                        }
                        
                        result.values["emotions"] = emotionDict;
                    }
                    break;
                    
                case "mood":
                    result.values["currentMood"] = character.mentalState.currentMood;
                    
                    if (query.detailLevel >= 1)
                    {
                        // Find dominant emotion
                        float strongestValue = 0f;
                        string dominantEmotion = "";
                        
                        foreach (var emotion in character.mentalState.emotionalStates)
                        {
                            if (Mathf.Abs(emotion.currentValue) > Mathf.Abs(strongestValue))
                            {
                                strongestValue = emotion.currentValue;
                                dominantEmotion = emotion.type;
                            }
                        }
                        
                        result.values["dominantEmotion"] = dominantEmotion;
                        result.values["dominantEmotionStrength"] = Mathf.Abs(strongestValue) / 100f;
                    }
                    
                    if (query.detailLevel >= 2)
                    {
                        // Full breakdown of all emotions
                        var allEmotions = new Dictionary<string, float>();
                        
                        foreach (var emotion in character.mentalState.emotionalStates)
                        {
                            allEmotions[emotion.type] = emotion.currentValue / 100f;
                        }
                        
                        result.values["allEmotions"] = allEmotions;
                        result.values["stressLevel"] = character.mentalState.stressLevel / 100f;
                        
                        // Active mood modifiers
                        var activeMoodModifiers = new List<string>();
                        foreach (var modifier in character.mentalState.moodModifiers)
                        {
                            activeMoodModifiers.Add(modifier.source);
                        }
                        
                        result.values["activeMoodModifiers"] = activeMoodModifiers;
                    }
                    break;
                    
                case "conflict":
                    if (!string.IsNullOrEmpty(query.specificId))
                    {
                        // Specific conflict information
                        var conflict = character.complexEmotions.internalConflicts.Find(c => c.type == query.specificId);
                        
                        if (conflict != null)
                        {
                            result.values["type"] = conflict.type;
                            result.values["dominantSide"] = conflict.dominantSide;
                            result.values["firstValue"] = conflict.values.firstValue;
                            result.values["secondValue"] = conflict.values.secondValue;
                        }
                    }
                    else
                    {
                        // All conflicts
                        var conflictDict = new Dictionary<string, object>();
                        
                        foreach (var conflict in character.complexEmotions.internalConflicts)
                        {
                            var conflictData = new Dictionary<string, object>
                            {
                                ["dominantSide"] = conflict.dominantSide,
                                ["firstValue"] = conflict.values.firstValue,
                                ["secondValue"] = conflict.values.secondValue
                            };
                            
                            conflictDict[conflict.type] = conflictData;
                        }
                        
                        result.values["conflicts"] = conflictDict;
                    }
                    break;
            }
            
            return result;
        }
        
        /// <summary>
        /// Determine the status of a desire based on thresholds
        /// </summary>
        private string GetDesireStatus(DesireParameters.Desire desire)
        {
            if (desire.currentValue < desire.threshold.low)
            {
                return "unsatisfied";
            }
            else if (desire.currentValue > desire.threshold.high)
            {
                return "satisfied";
            }
            else
            {
                return "neutral";
            }
        }
        
        #region Character Update Methods
        
        /// <summary>
        /// Update all characters' psychological states
        /// </summary>
        private void UpdateAllCharacters()
        {
            var characterManager = CharacterManager.Instance;
            
            // Iterate through all characters and update their desires and mental states
            foreach (var characterEntry in characterManager.GetAllCharacters())
            {
                var character = characterEntry.Value;
                
                // Store previous states for change detection
                string oldDominantDesire = character.desires.dominantDesire;
                string oldMood = character.mentalState.currentMood;
                Dictionary<string, float> oldDesireValues = new Dictionary<string, float>();
                
                foreach (var desire in character.desires.desireTypes)
                {
                    oldDesireValues[desire.type] = desire.currentValue;
                }
                
                // Update desires (natural decay)
                UpdateDesires(character);
                
                // Update mental state (apply mood modifiers, decay emotions)
                UpdateMentalState(character.baseInfo.characterId, null);
                
                // Update emotional memories (reduce intensity over time)
                UpdateEmotionalMemories(character);
                
                // Check for significant changes and publish events
                
                // Dominant desire change
                if (character.desires.dominantDesire != oldDominantDesire)
                {
                    var desireChangeEvent = new DesireChangeEvent
                    {
                        characterId = character.baseInfo.characterId,
                        desireType = character.desires.dominantDesire,
                        oldValue = 0, // Not applicable for dominant change
                        newValue = 0, // Not applicable for dominant change
                        isDominant = true
                    };
                    
                    CharacterEventBus.Instance.Publish(desireChangeEvent);
                }
                
                // Mood change
                if (character.mentalState.currentMood != oldMood)
                {
                    var moodChangeEvent = new MoodChangeEvent
                    {
                        characterId = character.baseInfo.characterId,
                        oldMood = oldMood,
                        newMood = character.mentalState.currentMood
                    };
                    
                    // Add emotion values
                    foreach (var emotion in character.mentalState.emotionalStates)
                    {
                        moodChangeEvent.emotionValues[emotion.type] = emotion.currentValue;
                    }
                    
                    CharacterEventBus.Instance.Publish(moodChangeEvent);
                }
                
                // Desire value changes
                foreach (var desire in character.desires.desireTypes)
                {
                    if (oldDesireValues.TryGetValue(desire.type, out float oldValue) && 
                        Mathf.Abs(desire.currentValue - oldValue) >= 5f) // 5% threshold for event
                    {
                        var desireChangeEvent = new DesireChangeEvent
                        {
                            characterId = character.baseInfo.characterId,
                            desireType = desire.type,
                            oldValue = oldValue,
                            newValue = desire.currentValue,
                            isDominant = (character.desires.dominantDesire == desire.type)
                        };
                        
                        CharacterEventBus.Instance.Publish(desireChangeEvent);
                    }
                }
            }
        }
        
        /// <summary>
        /// Update desires based on natural decay
        /// </summary>
        private void UpdateDesires(CharacterManager.Character character)
        {
            foreach (var desire in character.desires.desireTypes)
            {
                // Apply decay rate based on time unit
                desire.currentValue = Mathf.Max(0, desire.currentValue - desire.decayRate);
            }
            
            // Update the dominant desire
            character.desires.UpdateDominantDesire();
        }
        
        /// <summary>
        /// Update emotional memories (reduce intensity over time)
        /// </summary>
        private void UpdateEmotionalMemories(CharacterManager.Character character)
        {
            foreach (var memory in character.complexEmotions.emotionalMemory)
            {
                // Memories fade over time (very slowly)
                memory.intensity *= 0.999f; // Slight reduction each update
            }
            
            // Optionally remove memories that have faded almost completely
            character.complexEmotions.emotionalMemory.RemoveAll(m => m.intensity < 1f);
        }
        #endregion
        
        #region Desire Functions
        
        /// <summary>
        /// Calculate how a specific action affects a character's desires
        /// </summary>
        public Dictionary<string, float> CalculateDesireEffect(string characterId, string actionId, Dictionary<string, object> contextParameters)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            var results = new Dictionary<string, float>();
            
            if (character == null)
                return results;
                
            // Find the action definition from the action database
            var actionDef = ActionDatabase.Instance.GetAction(actionId);
            
            if (actionDef == null)
                return results;
                
            // Calculate effect on each desire
            foreach (var desire in character.desires.desireTypes)
            {
                // Check if this action affects this desire
                if (desire.satisfactionMultipliers.TryGetValue(actionId, out float multiplier))
                {
                    // Base effect from the action
                    float baseEffect = actionDef.GetBaseEffect(desire.type);
                    
                    // Apply character-specific multiplier
                    float totalEffect = baseEffect * multiplier;
                    
                    // Apply context modifiers if any
                    if (contextParameters != null)
                    {
                        totalEffect = ApplyContextModifiers(totalEffect, desire.type, actionId, contextParameters);
                    }
                    
                    // Record the effect for this desire
                    results.Add(desire.type, totalEffect);
                    
                    // Apply the effect
                    desire.currentValue = Mathf.Clamp(desire.currentValue + totalEffect, 0f, 100f);
                }
            }
            
            // Update the dominant desire after changes
            character.desires.UpdateDominantDesire();
            
            return results;
        }
        
        /// <summary>
        /// Apply context-specific modifiers to desire effects
        /// </summary>
        private float ApplyContextModifiers(float baseEffect, string desireType, string actionId, Dictionary<string, object> contextParameters)
        {
            float modifiedEffect = baseEffect;
            
            // Example: if the action happens in a location the character likes, increase the effect
            if (contextParameters.TryGetValue("location", out object locationObj) && locationObj is string location)
            {
                // The effect could be multiplied by a location preference factor
                // This would be defined elsewhere in the system
            }
            
            // Example: time of day might affect certain desires
            if (contextParameters.TryGetValue("timeOfDay", out object timeObj) && timeObj is float timeOfDay)
            {
                // Modify effect based on time of day
            }
            
            return modifiedEffect;
        }
        #endregion
        
        #region Mental State Functions
        
        /// <summary>
        /// Update a character's mental state based on current conditions and triggers
        /// </summary>
        public void UpdateMentalState(string characterId, Dictionary<string, object> triggeringFactors)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return;
                
            // 1. Process existing mood modifiers and decrease their duration
            ProcessMoodModifiers(character);
            
            // 2. Apply natural decay to emotions
            DecayEmotions(character);
            
            // 3. Apply new triggering factors if any
            if (triggeringFactors != null)
            {
                ApplyTriggeringFactors(character, triggeringFactors);
            }
            
            // 4. Update the current mood based on emotional states
            character.mentalState.CalculateCurrentMood();
            
            // 5. Check for potential internal conflicts that might arise
            CheckForConflictTriggers(character);
        }
        
        /// <summary>
        /// Process and update mood modifiers
        /// </summary>
        private void ProcessMoodModifiers(CharacterManager.Character character)
        {
            // Create a new list to hold modifiers that are still active
            var activeModifiers = new List<MentalState.MoodModifier>();
            
            foreach (var modifier in character.mentalState.moodModifiers)
            {
                // Decrease remaining time
                modifier.remainingTime--;
                
                // If the modifier is still active, keep it and apply its effects
                if (modifier.remainingTime > 0)
                {
                    activeModifiers.Add(modifier);
                    
                    // Apply the modifier's effects to the emotional states
                    ApplyMoodModifierEffects(character, modifier);
                }
            }
            
            // Replace the list with only active modifiers
            character.mentalState.moodModifiers = activeModifiers;
        }
        
        /// <summary>
        /// Apply the effects of a mood modifier to emotional states
        /// </summary>
        private void ApplyMoodModifierEffects(CharacterManager.Character character, MentalState.MoodModifier modifier)
        {
            foreach (var effect in modifier.effects)
            {
                // Find the matching emotional state
                var emotionalState = character.mentalState.emotionalStates.Find(e => e.type == effect.emotionType);
                
                if (emotionalState != null)
                {
                    // Apply the effect (with some emotional inertia to prevent sudden changes)
                    float inertiaFactor = 0.8f; // 80% of effect applied
                    emotionalState.currentValue += effect.effectValue * inertiaFactor;
                    
                    // Clamp the value to valid range
                    emotionalState.currentValue = Mathf.Clamp(emotionalState.currentValue, -100f, 100f);
                }
            }
        }
        
        /// <summary>
        /// Apply natural decay to emotions
        /// </summary>
        private void DecayEmotions(CharacterManager.Character character)
        {
            foreach (var emotion in character.mentalState.emotionalStates)
            {
                // Emotions decay toward 0 (neutral)
                if (emotion.currentValue > 0)
                {
                    emotion.currentValue = Mathf.Max(0, emotion.currentValue - emotion.decayRate);
                }
                else if (emotion.currentValue < 0)
                {
                    emotion.currentValue = Mathf.Min(0, emotion.currentValue + emotion.decayRate);
                }
            }
        }
        
        /// <summary>
        /// Apply new triggering factors to emotions
        /// </summary>
        private void ApplyTriggeringFactors(CharacterManager.Character character, Dictionary<string, object> triggeringFactors)
        {
            // Example: If an event happened that affects happiness
            if (triggeringFactors.TryGetValue("eventType", out object eventObj) && eventObj is string eventType)
            {
                // Get event definition from event database
                var eventDef = EventDatabase.Instance.GetEvent(eventType);
                
                if (eventDef != null)
                {
                    // Apply emotional effects defined by the event
                    foreach (var emotionEffect in eventDef.emotionEffects)
                    {
                        // Find the matching emotional state
                        var emotionalState = character.mentalState.emotionalStates.Find(e => e.type == emotionEffect.emotionType);
                        
                        if (emotionalState != null)
                        {
                            // Apply the effect
                            emotionalState.currentValue += emotionEffect.value;
                            
                            // Clamp to valid range
                            emotionalState.currentValue = Mathf.Clamp(emotionalState.currentValue, -100f, 100f);
                        }
                    }
                    
                    // Add a new mood modifier if applicable
                    if (eventDef.createsMoodModifier)
                    {
                        var newModifier = new MentalState.MoodModifier
                        {
                            source = eventType,
                            effects = ConvertEventEffectsToModifierEffects(eventDef.emotionEffects),
                            duration = eventDef.moodModifierDuration,
                            remainingTime = eventDef.moodModifierDuration
                        };
                        
                        character.mentalState.moodModifiers.Add(newModifier);
                    }
                    
                    // Create emotional memory if this is a significant event
                    if (eventDef.isSignificant)
                    {
                        CreateEmotionalMemory(character, eventType, eventDef.emotionEffects);
                    }
                }
            }
        }
        
        /// <summary>
        /// Convert event effects to mood modifier effects
        /// </summary>
        private List<MentalState.MoodModifier.EmotionEffect> ConvertEventEffectsToModifierEffects(List<EventEmotionEffect> eventEffects)
        {
            var modifierEffects = new List<MentalState.MoodModifier.EmotionEffect>();
            
            foreach (var effect in eventEffects)
            {
                modifierEffects.Add(new MentalState.MoodModifier.EmotionEffect
                {
                    emotionType = effect.emotionType,
                    effectValue = effect.value
                });
            }
            
            return modifierEffects;
        }
        
        /// <summary>
        /// Create an emotional memory from an event
        /// </summary>
        private void CreateEmotionalMemory(CharacterManager.Character character, string eventType, List<EventEmotionEffect> emotionEffects)
        {
            var memory = new ComplexEmotions.EmotionalMemory
            {
                eventId = eventType,
                emotions = new List<ComplexEmotions.EmotionalMemory.EmotionImpact>(),
                intensity = CalculateEventIntensity(emotionEffects),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            // Convert event effects to emotional impacts
            foreach (var effect in emotionEffects)
            {
                memory.emotions.Add(new ComplexEmotions.EmotionalMemory.EmotionImpact
                {
                    emotionType = effect.emotionType,
                    impact = effect.value
                });
            }
            
            // Add to character's emotional memories
            character.complexEmotions.emotionalMemory.Add(memory);
        }
        
        /// <summary>
        /// Calculate the overall intensity of an event
        /// </summary>
        private float CalculateEventIntensity(List<EventEmotionEffect> emotionEffects)
        {
            float totalIntensity = 0f;
            
            foreach (var effect in emotionEffects)
            {
                totalIntensity += Mathf.Abs(effect.value);
            }
            
            return Mathf.Min(100f, totalIntensity);
        }
        
        /// <summary>
        /// Check for potential conflicts that might be triggered by current state
        /// </summary>
        private void CheckForConflictTriggers(CharacterManager.Character character)
        {
            foreach (var conflict in character.complexEmotions.internalConflicts)
            {
                foreach (var trigger in conflict.triggerConditions)
                {
                    // Check if this trigger condition exists in the current state
                    bool isTriggered = CheckTriggerCondition(character, trigger);
                    
                    if (isTriggered)
                    {
                        // Intensify the conflict
                        IntensifyConflict(conflict);
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if a specific trigger condition is met
        /// </summary>
        private bool CheckTriggerCondition(CharacterManager.Character character, string trigger)
        {
            // Example implementation: Check if the trigger matches a high/low desire state
            foreach (var desire in character.desires.desireTypes)
            {
                string lowDesireTrigger = $"{desire.type}_low";
                string highDesireTrigger = $"{desire.type}_high";
                
                if (trigger == lowDesireTrigger && desire.currentValue < desire.threshold.low)
                {
                    return true;
                }
                
                if (trigger == highDesireTrigger && desire.currentValue > desire.threshold.high)
                {
                    return true;
                }
            }
            
            // Could also check for specific emotional states, mood modifiers, etc.
            
            return false;
        }
        
        /// <summary>
        /// Intensify an internal conflict
        /// </summary>
        private void IntensifyConflict(ComplexEmotions.InternalConflict conflict)
        {
            // Increase the value of both sides of the conflict
            conflict.values.firstValue = Mathf.Min(100f, conflict.values.firstValue + 10f);
            conflict.values.secondValue = Mathf.Min(100f, conflict.values.secondValue + 8f);
            
            // Update the dominant side
            if (conflict.values.firstValue > conflict.values.secondValue)
            {
                conflict.dominantSide = "first";
            }
            else
            {
                conflict.dominantSide = "second";
            }
        }
        #endregion
        
        #region Emotional Response Functions
        
        /// <summary>
        /// Generate an emotional response to a situation
        /// </summary>
        public EmotionalResponse GenerateEmotionalResponse(string characterId, string situationId, float intensityLevel)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return null;
                
            // Get the situation definition
            var situationDef = SituationDatabase.Instance.GetSituation(situationId);
            
            if (situationDef == null)
                return null;
                
            // Create a response object
            var response = new EmotionalResponse
            {
                characterId = characterId,
                situationId = situationId,
                responseType = DetermineResponseType(character, situationDef),
                intensity = CalculateResponseIntensity(character, situationDef, intensityLevel),
                emotionalComponents = new List<EmotionalComponent>(),
                triggeredMemories = new List<TriggeredMemory>()
            };
            
            // Add emotional components to the response
            foreach (var emotion in character.mentalState.emotionalStates)
            {
                if (Mathf.Abs(emotion.currentValue) > 20f) // Only include significant emotions
                {
                    response.emotionalComponents.Add(new EmotionalComponent
                    {
                        emotionType = emotion.type,
                        strength = Mathf.Abs(emotion.currentValue) / 100f
                    });
                }
            }
            
            // Check if any past emotional memories are relevant to this situation
            ApplyEmotionalMemories(character, situationId, response);
            
            return response;
        }
        
        /// <summary>
        /// Determine the type of response based on character and situation
        /// </summary>
        private string DetermineResponseType(CharacterManager.Character character, SituationDefinition situation)
        {
            // Start with the default response
            string responseType = "neutral";
            
            // Check character's dominant desire
            if (situation.desireResponseMap.TryGetValue(character.desires.dominantDesire, out string desireResponse))
            {
                responseType = desireResponse;
            }
            
            // Check if any strong emotions override this
            foreach (var emotion in character.mentalState.emotionalStates)
            {
                if (Mathf.Abs(emotion.currentValue) > 70f && situation.emotionResponseOverrides.TryGetValue(emotion.type, out string emotionResponse))
                {
                    responseType = emotionResponse;
                    break;
                }
            }
            
            // Check if any internal conflict affects the response
            foreach (var conflict in character.complexEmotions.internalConflicts)
            {
                if (conflict.values.firstValue > 80f || conflict.values.secondValue > 80f)
                {
                    // Check if this conflict has a specific response for this situation
                    string conflictKey = $"{conflict.type}_{conflict.dominantSide}";
                    if (situation.conflictResponseOverrides.TryGetValue(conflictKey, out string conflictResponse))
                    {
                        responseType = conflictResponse;
                        break;
                    }
                }
            }
            
            return responseType;
        }
        
        /// <summary>
        /// Calculate how intense the response should be
        /// </summary>
        private float CalculateResponseIntensity(CharacterManager.Character character, SituationDefinition situation, float baseLevelIntensity)
        {
            float intensity = baseLevelIntensity;
            
            // Modify based on character's personality
            switch (character.baseInfo.personalityType)
            {
                case "extroverted":
                    intensity *= 1.2f; // More expressive
                    break;
                case "introverted":
                    intensity *= 0.8f; // Less expressive
                    break;
            }
            
            // Stress level affects intensity
            intensity *= (1f + (character.mentalState.stressLevel / 200f)); // Max 50% boost at max stress
            
            // Cap at 100%
            return Mathf.Min(1f, intensity);
        }
        
        /// <summary>
        /// Apply the influence of past emotional memories
        /// </summary>
        private void ApplyEmotionalMemories(CharacterManager.Character character, string situationId, EmotionalResponse response)
        {
            // Look for memories related to similar situations
            foreach (var memory in character.complexEmotions.emotionalMemory)
            {
                // Check if this memory is relevant to the current situation
                if (IsMemoryRelevantToSituation(memory.eventId, situationId))
                {
                    // Add a note about the triggered memory
                    response.triggeredMemories.Add(new TriggeredMemory
                    {
                        memoryId = memory.eventId,
                        relevance = memory.intensity / 100f
                    });
                    
                    // Modify the response based on the memory
                    foreach (var emotionImpact in memory.emotions)
                    {
                        // Find if this emotion is already in the response
                        var existingEmotion = response.emotionalComponents.Find(e => e.emotionType == emotionImpact.emotionType);
                        
                        if (existingEmotion != null)
                        {
                            // Enhance the existing emotion
                            existingEmotion.strength += (emotionImpact.impact / 200f) * (memory.intensity / 100f);
                            existingEmotion.strength = Mathf.Min(1f, existingEmotion.strength);
                        }
                        else
                        {
                            // Add a new emotion triggered by the memory
                            response.emotionalComponents.Add(new EmotionalComponent
                            {
                                emotionType = emotionImpact.emotionType,
                                strength = (emotionImpact.impact / 100f) * (memory.intensity / 100f)
                            });
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Determine if a memory is relevant to the current situation
        /// </summary>
        private bool IsMemoryRelevantToSituation(string memoryEventId, string situationId)
        {
            // This would be implemented based on how events and situations are categorized
            // For example, events and situations could share tags or categories
            
            // Simple implementation: check if they share the same prefix
            string memoryPrefix = memoryEventId.Split('_')[0];
            string situationPrefix = situationId.Split('_')[0];
            
            return memoryPrefix == situationPrefix;
        }
        #endregion
        
        #region Conflict Evaluation Functions
        
        /// <summary>
        /// Evaluate and potentially resolve an internal conflict
        /// </summary>
        public ConflictResolution EvaluateInternalConflict(string characterId, string decisionPoint)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return null;
                
            // Get the decision point definition
            var decisionDef = DecisionDatabase.Instance.GetDecision(decisionPoint);
            
            if (decisionDef == null)
                return null;
                
            // Find all conflicts that this decision point might trigger
            var relevantConflicts = FindRelevantConflicts(character, decisionDef);
            
            if (relevantConflicts.Count == 0)
                return null;
                
            // Create a conflict resolution object
            var resolution = new ConflictResolution
            {
                characterId = characterId,
                decisionPoint = decisionPoint,
                conflicts = new List<ConflictResolutionItem>()
            };
            
            // Evaluate each relevant conflict
            foreach (var conflict in relevantConflicts)
            {
                var resolutionItem = EvaluateSingleConflict(character, conflict, decisionDef);
                resolution.conflicts.Add(resolutionItem);
                
                // Apply the effects of the resolution
                ApplyConflictResolution(character, conflict, resolutionItem);
            }
            
            return resolution;
        }
        
        /// <summary>
        /// Find conflicts that are relevant to a decision point
        /// </summary>
        private List<ComplexEmotions.InternalConflict> FindRelevantConflicts(CharacterManager.Character character, DecisionDefinition decision)
        {
            var relevantConflicts = new List<ComplexEmotions.InternalConflict>();
            
            foreach (var conflict in character.complexEmotions.internalConflicts)
            {
                // Check if this conflict type is relevant to this decision
                if (decision.relevantConflictTypes.Contains(conflict.type))
                {
                    relevantConflicts.Add(conflict);
                }
            }
            
            return relevantConflicts;
        }
        
        /// <summary>
        /// Evaluate a single conflict
        /// </summary>
        private ConflictResolutionItem EvaluateSingleConflict(
            CharacterManager.Character character, 
            ComplexEmotions.InternalConflict conflict, 
            DecisionDefinition decision)
        {
            var item = new ConflictResolutionItem
            {
                conflictType = conflict.type,
                initialDominantSide = conflict.dominantSide,
                resolutionChoice = "",
                impactedDesires = new List<DesireImpact>(),
                impactedEmotions = new List<EmotionImpact>()
            };
            
            // Calculate the strength of each side of the conflict
            float firstSideStrength = conflict.values.firstValue;
            float secondSideStrength = conflict.values.secondValue;
            
            // Factor in character's personal values
            foreach (var value in character.complexEmotions.personalValues)
            {
                if (decision.valueInfluences.TryGetValue(value.valueType, out var influence))
                {
                    if (influence.targetSide == "first")
                    {
                        firstSideStrength += value.importance * influence.strengthModifier;
                    }
                    else if (influence.targetSide == "second")
                    {
                        secondSideStrength += value.importance * influence.strengthModifier;
                    }
                }
            }
            
            // Factor in current desires
            foreach (var desire in character.desires.desireTypes)
            {
                if (decision.desireInfluences.TryGetValue(desire.type, out var influence))
                {
                    float desireStrength = Mathf.Max(0, desire.currentValue - desire.threshold.low) / 100f;
                    
                    if (influence.targetSide == "first")
                    {
                        firstSideStrength += desireStrength * influence.strengthModifier;
                    }
                    else if (influence.targetSide == "second")
                    {
                        secondSideStrength += desireStrength * influence.strengthModifier;
                    }
                }
            }
            
            // Determine which side prevails
            if (firstSideStrength > secondSideStrength)
            {
                item.resolutionChoice = "first";
                
                // Apply first-side outcomes
                ApplyResolutionOutcomes(item, decision.firstSideOutcomes);
            }
            else
            {
                item.resolutionChoice = "second";
                
                // Apply second-side outcomes
                ApplyResolutionOutcomes(item, decision.secondSideOutcomes);
            }
            
            return item;
        }
        
        /// <summary>
        /// Apply outcomes to the resolution item
        /// </summary>
        private void ApplyResolutionOutcomes(ConflictResolutionItem item, DecisionOutcomes outcomes)
        {
            // Add desire impacts
            foreach (var desireImpact in outcomes.desireImpacts)
            {
                item.impactedDesires.Add(new DesireImpact
                {
                    desireType = desireImpact.desireType,
                    changeAmount = desireImpact.changeAmount
                });
            }
            
            // Add emotion impacts
            foreach (var emotionImpact in outcomes.emotionImpacts)
            {
                item.impactedEmotions.Add(new EmotionImpact
                {
                    emotionType = emotionImpact.emotionType,
                    changeAmount = emotionImpact.changeAmount
                });
            }
        }
        
        /// <summary>
        /// Apply the resolution to the character
        /// </summary>
        private void ApplyConflictResolution(
            CharacterManager.Character character, 
            ComplexEmotions.InternalConflict conflict, 
            ConflictResolutionItem resolution)
        {
            // Update the conflict's dominant side
            conflict.dominantSide = resolution.resolutionChoice;
            
            // If first side prevailed, increase first value and decrease second
            if (resolution.resolutionChoice == "first")
            {
                conflict.values.firstValue = Mathf.Min(100f, conflict.values.firstValue + 10f);
                conflict.values.secondValue = Mathf.Max(0f, conflict.values.secondValue - 10f);
            }
            // If second side prevailed, increase second value and decrease first
            else
            {
                conflict.values.secondValue = Mathf.Min(100f, conflict.values.secondValue + 10f);
                conflict.values.firstValue = Mathf.Max(0f, conflict.values.firstValue - 10f);
            }
            
            // Apply changes to desires
            foreach (var desireImpact in resolution.impactedDesires)
            {
                var desire = character.desires.desireTypes.Find(d => d.type == desireImpact.desireType);
                
                if (desire != null)
                {
                    desire.currentValue = Mathf.Clamp(desire.currentValue + desireImpact.changeAmount, 0f, 100f);
                }
            }
            
            // Apply changes to emotions
            foreach (var emotionImpact in resolution.impactedEmotions)
            {
                var emotion = character.mentalState.emotionalStates.Find(e => e.type == emotionImpact.emotionType);
                
                if (emotion != null)
                {
                    emotion.currentValue = Mathf.Clamp(emotion.currentValue + emotionImpact.changeAmount, -100f, 100f);
                }
            }
            
            // Update the dominant desire after these changes
            character.desires.UpdateDominantDesire();
            
            // Update the current mood after these changes
            character.mentalState.CalculateCurrentMood();
        }
        #endregion
    }
    
    #region Response Classes
    
    /// <summary>
    /// Class to represent an emotional response
    /// </summary>
    [Serializable]
    public class EmotionalResponse
    {
        public string characterId;
        public string situationId;
        public string responseType;
        public float intensity;
        public List<EmotionalComponent> emotionalComponents = new List<EmotionalComponent>();
        public List<TriggeredMemory> triggeredMemories = new List<TriggeredMemory>();
    }
    
    [Serializable]
    public class EmotionalComponent
    {
        public string emotionType;
        public float strength; // 0-1 scale
    }
    
    [Serializable]
    public class TriggeredMemory
    {
        public string memoryId;
        public float relevance; // 0-1 scale
    }
    
    /// <summary>
    /// Class to represent a conflict resolution
    /// </summary>
    [Serializable]
    public class ConflictResolution
    {
        public string characterId;
        public string decisionPoint;
        public List<ConflictResolutionItem> conflicts = new List<ConflictResolutionItem>();
    }
    
    [Serializable]
    public class ConflictResolutionItem
    {
        public string conflictType;
        public string initialDominantSide;
        public string resolutionChoice;
        public List<DesireImpact> impactedDesires = new List<DesireImpact>();
        public List<EmotionImpact> impactedEmotions = new List<EmotionImpact>();
    }
    
    [Serializable]
    public class DesireImpact
    {
        public string desireType;
        public float changeAmount;
    }
    
    [Serializable]
    public class EmotionImpact
    {
        public string emotionType;
        public float changeAmount;
    }
    #endregion
    
    #region Database Interfaces
    
    // These would be implemented as ScriptableObjects in Unity
    
    /// <summary>
    /// Interface for the action database
    /// </summary>
    public interface IActionDatabase
    {
        ActionDefinition GetAction(string actionId);
    }
    
    /// <summary>
    /// Interface for the event database
    /// </summary>
    public interface IEventDatabase
    {
        EventDefinition GetEvent(string eventId);
    }
    
    /// <summary>
    /// Interface for the situation database
    /// </summary>
    public interface ISituationDatabase
    {
        SituationDefinition GetSituation(string situationId);
    }
    
    /// <summary>
    /// Interface for the decision database
    /// </summary>
    public interface IDecisionDatabase
    {
        DecisionDefinition GetDecision(string decisionId);
    }
    
    /// <summary>
    /// Mock implementation with static instance
    /// </summary>
    public class ActionDatabase : MonoBehaviour, IActionDatabase
    {
        private static ActionDatabase _instance;
        public static ActionDatabase Instance => _instance;
        
        private void Awake()
        {
            _instance = this;
        }
        
        public ActionDefinition GetAction(string actionId)
        {
            // In a real implementation, this would load from a resource or database
            return null;
        }
    }
    
    // Other database implementations would be similar
    public class EventDatabase : MonoBehaviour, IEventDatabase
    {
        private static EventDatabase _instance;
        public static EventDatabase Instance => _instance;
        
        private void Awake()
        {
            _instance = this;
        }
        
        public EventDefinition GetEvent(string eventId)
        {
            // In a real implementation, this would load from a resource or database
            return null;
        }
    }
    
    public class SituationDatabase : MonoBehaviour, ISituationDatabase
    {
        private static SituationDatabase _instance;
        public static SituationDatabase Instance => _instance;
        
        private void Awake()
        {
            _instance = this;
        }
        
        public SituationDefinition GetSituation(string situationId)
        {
            // In a real implementation, this would load from a resource or database
            return null;
        }
    }
    
    public class DecisionDatabase : MonoBehaviour, IDecisionDatabase
    {
        private static DecisionDatabase _instance;
        public static DecisionDatabase Instance => _instance;
        
        private void Awake()
        {
            _instance = this;
        }
        
        public DecisionDefinition GetDecision(string decisionId)
        {
            // In a real implementation, this would load from a resource or database
            return null;
        }
    }
    
    /// <summary>
    /// Sample action definition
    /// </summary>
    [Serializable]
    public class ActionDefinition
    {
        public string actionId;
        public string displayName;
        public Dictionary<string, float> baseEffects = new Dictionary<string, float>();
        
        public float GetBaseEffect(string desireType)
        {
            if (baseEffects.TryGetValue(desireType, out float effect))
            {
                return effect;
            }
            return 0f;
        }
    }
    
    /// <summary>
    /// Sample event definition
    /// </summary>
    [Serializable]
    public class EventDefinition
    {
        public string eventId;
        public string displayName;
        public List<EventEmotionEffect> emotionEffects = new List<EventEmotionEffect>();
        public bool createsMoodModifier;
        public int moodModifierDuration;
        public bool isSignificant;
    }
    
    [Serializable]
    public class EventEmotionEffect
    {
        public string emotionType;
        public float value;
    }
    
    /// <summary>
    /// Sample situation definition
    /// </summary>
    [Serializable]
    public class SituationDefinition
    {
        public string situationId;
        public string displayName;
        public Dictionary<string, string> desireResponseMap = new Dictionary<string, string>();
        public Dictionary<string, string> emotionResponseOverrides = new Dictionary<string, string>();
        public Dictionary<string, string> conflictResponseOverrides = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Sample decision definition
    /// </summary>
    [Serializable]
    public class DecisionDefinition
    {
        public string decisionId;
        public string displayName;
        public List<string> relevantConflictTypes = new List<string>();
        public Dictionary<string, ValueInfluence> valueInfluences = new Dictionary<string, ValueInfluence>();
        public Dictionary<string, DesireInfluence> desireInfluences = new Dictionary<string, DesireInfluence>();
        public DecisionOutcomes firstSideOutcomes = new DecisionOutcomes();
        public DecisionOutcomes secondSideOutcomes = new DecisionOutcomes();
    }
    
    [Serializable]
    public class ValueInfluence
    {
        public string targetSide;
        public float strengthModifier;
    }
    
    [Serializable]
    public class DesireInfluence
    {
        public string targetSide;
        public float strengthModifier;
    }
    
    [Serializable]
    public class DecisionOutcomes
    {
        public List<DesireImpactDefinition> desireImpacts = new List<DesireImpactDefinition>();
        public List<EmotionImpactDefinition> emotionImpacts = new List<EmotionImpactDefinition>();
    }
    
    [Serializable]
    public class DesireImpactDefinition
    {
        public string desireType;
        public float changeAmount;
    }
    
    [Serializable]
    public class EmotionImpactDefinition
    {
        public string emotionType;
        public float changeAmount;
    }
    #endregion
}