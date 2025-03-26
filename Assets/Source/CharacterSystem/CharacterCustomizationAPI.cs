using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// API for runtime customization of character psychology parameters
    /// </summary>
    public class CharacterCustomizationAPI : MonoBehaviour
    {
        private static CharacterCustomizationAPI _instance;
        public static CharacterCustomizationAPI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CharacterCustomizationAPI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CharacterCustomizationAPI");
                        _instance = go.AddComponent<CharacterCustomizationAPI>();
                    }
                }
                return _instance;
            }
        }
        
        #region Desire Customization
        
        /// <summary>
        /// Modify a character's desire parameters
        /// </summary>
        public void ModifyDesireParameters(
            string characterId, 
            string desireType, 
            float? baseLevel = null, 
            float? thresholdLow = null, 
            float? thresholdHigh = null, 
            float? decayRate = null)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return;
                
            var desire = character.desires.desireTypes.Find(d => d.type == desireType);
            
            if (desire == null)
                return;
                
            // Update parameters if provided
            if (baseLevel.HasValue) desire.baseLevel = Mathf.Clamp(baseLevel.Value, 0f, 100f);
            if (thresholdLow.HasValue) desire.threshold.low = Mathf.Clamp(thresholdLow.Value, 0f, desire.threshold.high);
            if (thresholdHigh.HasValue) desire.threshold.high = Mathf.Clamp(thresholdHigh.Value, desire.threshold.low, 100f);
            if (decayRate.HasValue) desire.decayRate = Mathf.Max(0f, decayRate.Value);
        }
        
        /// <summary>
        /// Set a custom satisfaction multiplier for an action-desire pair
        /// </summary>
        public void SetSatisfactionMultiplier(
            string characterId, 
            string desireType, 
            string actionId, 
            float multiplier)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return;
                
            var desire = character.desires.desireTypes.Find(d => d.type == desireType);
            
            if (desire == null)
                return;
                
            // Update or add the multiplier
            desire.satisfactionMultipliers[actionId] = multiplier;
            
            // Update the serialized version for Unity serialization
            var existingMultiplier = desire.serializedMultipliers.Find(m => m.actionId == actionId);
            if (existingMultiplier != null)
            {
                existingMultiplier.multiplier = multiplier;
            }
            else
            {
                desire.serializedMultipliers.Add(new DesireParameters.Desire.SatisfactionMultiplier
                {
                    actionId = actionId,
                    multiplier = multiplier
                });
            }
        }
        #endregion
        
        #region Emotion Customization
        
        /// <summary>
        /// Modify a character's emotional state parameters
        /// </summary>
        public void ModifyEmotionalStateParameters(
            string characterId, 
            string emotionType, 
            float? volatility = null, 
            float? decayRate = null)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return;
                
            var emotion = character.mentalState.emotionalStates.Find(e => e.type == emotionType);
            
            if (emotion == null)
                return;
                
            // Update parameters if provided
            if (volatility.HasValue) emotion.volatility = Mathf.Clamp(volatility.Value, 0f, 5f);
            if (decayRate.HasValue) emotion.decayRate = Mathf.Max(0f, decayRate.Value);
        }
        
        /// <summary>
        /// Add a new mood modifier to a character
        /// </summary>
        public void AddMoodModifier(
            string characterId,
            string source,
            Dictionary<string, float> effects,
            int duration)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return;
                
            // Create a new mood modifier
            var modifier = new MentalState.MoodModifier
            {
                source = source,
                effects = new List<MentalState.MoodModifier.EmotionEffect>(),
                duration = duration,
                remainingTime = duration
            };
            
            // Add effects
            foreach (var effect in effects)
            {
                modifier.effects.Add(new MentalState.MoodModifier.EmotionEffect
                {
                    emotionType = effect.Key,
                    effectValue = effect.Value
                });
            }
            
            // Add to character
            character.mentalState.moodModifiers.Add(modifier);
        }
        #endregion
        
        #region Complex Emotions Customization
        
        /// <summary>
        /// Modify a character's personal values
        /// </summary>
        public void ModifyPersonalValue(
            string characterId,
            string valueType,
            float importance)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return;
                
            var value = character.complexEmotions.personalValues.Find(v => v.valueType == valueType);
            
            if (value != null)
            {
                // Update existing value
                value.importance = Mathf.Clamp(importance, 0f, 100f);
            }
            else
            {
                // Add new value
                character.complexEmotions.personalValues.Add(new ComplexEmotions.PersonalValue
                {
                    valueType = valueType,
                    importance = Mathf.Clamp(importance, 0f, 100f)
                });
            }
        }
        
        /// <summary>
        /// Add a new internal conflict to a character
        /// </summary>
        public void AddInternalConflict(
            string characterId,
            string conflictType,
            float firstValue,
            float secondValue,
            List<string> triggerConditions)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return;
                
            // Create a new internal conflict
            var conflict = new ComplexEmotions.InternalConflict
            {
                type = conflictType,
                values = new ComplexEmotions.InternalConflict.ConflictingValues
                {
                    firstValue = Mathf.Clamp(firstValue, 0f, 100f),
                    secondValue = Mathf.Clamp(secondValue, 0f, 100f)
                },
                dominantSide = firstValue >= secondValue ? "first" : "second",
                triggerConditions = triggerConditions ?? new List<string>()
            };
            
            // Add to character
            character.complexEmotions.internalConflicts.Add(conflict);
        }
        
        /// <summary>
        /// Add a new emotional memory to a character
        /// </summary>
        public void AddEmotionalMemory(
            string characterId,
            string eventId,
            Dictionary<string, float> emotions,
            float intensity)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return;
                
            // Create a new emotional memory
            var memory = new ComplexEmotions.EmotionalMemory
            {
                eventId = eventId,
                emotions = new List<ComplexEmotions.EmotionalMemory.EmotionImpact>(),
                intensity = Mathf.Clamp(intensity, 0f, 100f),
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            // Add emotions
            foreach (var emotion in emotions)
            {
                memory.emotions.Add(new ComplexEmotions.EmotionalMemory.EmotionImpact
                {
                    emotionType = emotion.Key,
                    impact = emotion.Value
                });
            }
            
            // Add to character
            character.complexEmotions.emotionalMemory.Add(memory);
        }
        #endregion
    }
}