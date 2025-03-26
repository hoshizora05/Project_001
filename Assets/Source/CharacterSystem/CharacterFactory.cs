using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Factory for creating character instances from templates
    /// </summary>
    public class CharacterFactory : MonoBehaviour
    {
        private static CharacterFactory _instance;
        public static CharacterFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CharacterFactory>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CharacterFactory");
                        _instance = go.AddComponent<CharacterFactory>();
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Create a character from a template
        /// </summary>
        public CharacterManager.Character CreateCharacter(string templateId, string characterId = null, string name = null)
        {
            var template = CharacterSystemDatabase.Instance.GetCharacterTemplate(templateId);
            
            if (template == null)
            {
                Debug.LogError($"Character template with ID {templateId} not found!");
                return null;
            }
            
            // Generate a unique ID if not provided
            if (string.IsNullOrEmpty(characterId))
            {
                characterId = System.Guid.NewGuid().ToString();
            }
            
            // Use template name if not provided
            if (string.IsNullOrEmpty(name))
            {
                name = template.displayName;
            }
            
            // Create the character
            var character = new CharacterManager.Character
            {
                baseInfo = CreateBaseInfo(template, characterId, name),
                desires = CreateDesires(template, characterId),
                mentalState = CreateMentalState(template, characterId),
                complexEmotions = CreateComplexEmotions(template, characterId)
            };
            
            // Register with the character manager
            CharacterManager.Instance.RegisterCharacter(character);
            
            return character;
        }
        
        /// <summary>
        /// Create base info from template
        /// </summary>
        private CharacterBase CreateBaseInfo(CharacterTemplateDefinition template, string characterId, string name)
        {
            return new CharacterBase
            {
                characterId = characterId,
                name = name,
                age = template.defaultAge,
                gender = template.defaultGender,
                occupation = template.defaultOccupation,
                personalityType = template.personalityType,
                backstory = template.defaultBackstory,
                relationshipStatus = template.defaultRelationshipStatus,
                visualAssets = new CharacterBase.CharacterVisualAssets
                {
                    defaultPortrait = template.defaultPortrait,
                    characterModel = template.defaultModel
                }
            };
        }
        
        /// <summary>
        /// Create desires from template
        /// </summary>
        private DesireParameters CreateDesires(CharacterTemplateDefinition template, string characterId)
        {
            var desires = new DesireParameters
            {
                characterId = characterId,
                desireTypes = new List<DesireParameters.Desire>(),
                dominantDesire = ""
            };
            
            // Get personality type for modifiers
            var personalityType = CharacterSystemDatabase.Instance.GetPersonalityType(template.personalityType);
            
            // Add all desire types from the database
            foreach (var desireTypeDef in CharacterSystemDatabase.Instance.GetAllDesireTypes())
            {
                // Look for a preset for this desire type
                var preset = template.desirePresets.Find(d => d.desireType == desireTypeDef.id);
                
                // Create the desire
                var desire = new DesireParameters.Desire
                {
                    type = desireTypeDef.id,
                    baseLevel = desireTypeDef.defaultBaseLevel,
                    currentValue = preset != null ? preset.initialValue : desireTypeDef.defaultBaseLevel,
                    threshold = new DesireParameters.DesireThreshold
                    {
                        low = desireTypeDef.defaultThresholdLow,
                        high = desireTypeDef.defaultThresholdHigh
                    },
                    decayRate = preset != null && preset.decayRateOverride > 0 ? 
                        preset.decayRateOverride : desireTypeDef.defaultDecayRate,
                    satisfactionMultipliers = new Dictionary<string, float>(),
                    serializedMultipliers = new List<DesireParameters.Desire.SatisfactionMultiplier>()
                };
                
                // Apply personality modifiers if available
                if (personalityType != null)
                {
                    var modifier = personalityType.desireModifiers.Find(m => m.desireType == desireTypeDef.id);
                    if (modifier != null)
                    {
                        desire.baseLevel += modifier.baseModifier;
                        desire.currentValue += modifier.baseModifier;
                        desire.decayRate *= modifier.decayRateMultiplier;
                    }
                }
                
                // Add default satisfaction multipliers
                foreach (var action in CharacterSystemDatabase.Instance.GetAllActions())
                {
                    if (action.baseEffects.TryGetValue(desireTypeDef.id, out _))
                    {
                        desire.serializedMultipliers.Add(new DesireParameters.Desire.SatisfactionMultiplier
                        {
                            actionId = action.actionId,
                            multiplier = 1.0f  // Default multiplier
                        });
                    }
                }
                
                // Initialize multipliers dictionary
                desire.InitializeMultipliers();
                
                // Add to the list
                desires.desireTypes.Add(desire);
            }
            
            // Update the dominant desire
            desires.UpdateDominantDesire();
            
            return desires;
        }
        
        /// <summary>
        /// Create mental state from template
        /// </summary>
        private MentalState CreateMentalState(CharacterTemplateDefinition template, string characterId)
        {
            var mentalState = new MentalState
            {
                characterId = characterId,
                emotionalStates = new List<MentalState.EmotionalState>(),
                moodModifiers = new List<MentalState.MoodModifier>(),
                currentMood = "neutral",
                stressLevel = 0f
            };
            
            // Get personality type for modifiers
            var personalityType = CharacterSystemDatabase.Instance.GetPersonalityType(template.personalityType);
            
            // Add all emotion types from the database
            foreach (var emotionTypeDef in CharacterSystemDatabase.Instance.GetAllEmotionTypes())
            {
                // Look for a preset for this emotion type
                var preset = template.emotionPresets.Find(e => e.emotionType == emotionTypeDef.id);
                
                // Create the emotional state
                var emotionalState = new MentalState.EmotionalState
                {
                    type = emotionTypeDef.id,
                    currentValue = preset != null ? preset.initialValue : 0f,
                    volatility = emotionTypeDef.defaultVolatility,
                    decayRate = emotionTypeDef.defaultDecayRate
                };
                
                // Apply personality modifiers if available
                if (personalityType != null)
                {
                    var modifier = personalityType.emotionModifiers.Find(m => m.emotionType == emotionTypeDef.id);
                    if (modifier != null)
                    {
                        emotionalState.volatility *= modifier.volatilityMultiplier;
                        emotionalState.decayRate *= modifier.decayRateMultiplier;
                    }
                }
                
                // Add to the list
                mentalState.emotionalStates.Add(emotionalState);
            }
            
            // Calculate initial mood
            mentalState.CalculateCurrentMood();
            
            return mentalState;
        }
        
        /// <summary>
        /// Create complex emotions from template
        /// </summary>
        private ComplexEmotions CreateComplexEmotions(CharacterTemplateDefinition template, string characterId)
        {
            var complexEmotions = new ComplexEmotions
            {
                characterId = characterId,
                internalConflicts = new List<ComplexEmotions.InternalConflict>(),
                emotionalMemory = new List<ComplexEmotions.EmotionalMemory>(),
                personalValues = new List<ComplexEmotions.PersonalValue>()
            };
            
            // Get personality type for value preferences
            var personalityType = CharacterSystemDatabase.Instance.GetPersonalityType(template.personalityType);
            
            // Add personal values from personality type
            if (personalityType != null)
            {
                foreach (var valuePreference in personalityType.valuePreferences)
                {
                    complexEmotions.personalValues.Add(new ComplexEmotions.PersonalValue
                    {
                        valueType = valuePreference.valueType,
                        importance = valuePreference.initialImportance
                    });
                }
            }
            
            // Add internal conflicts from template
            foreach (var conflictPreset in template.conflictPresets)
            {
                complexEmotions.internalConflicts.Add(new ComplexEmotions.InternalConflict
                {
                    type = conflictPreset.conflictType,
                    values = new ComplexEmotions.InternalConflict.ConflictingValues
                    {
                        firstValue = conflictPreset.firstValueInitial,
                        secondValue = conflictPreset.secondValueInitial
                    },
                    dominantSide = conflictPreset.firstValueInitial >= conflictPreset.secondValueInitial ? "first" : "second",
                    triggerConditions = new List<string>()  // Would be populated from a conflict definition
                });
            }
            
            return complexEmotions;
        }
    }
}