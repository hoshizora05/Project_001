using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    [Serializable]
    public class ComplexEmotions
    {
        public string characterId;                  // Character ID
        public List<InternalConflict> internalConflicts; // Internal conflicts
        public List<EmotionalMemory> emotionalMemory;   // Emotional memory
        public List<PersonalValue> personalValues;      // Personal values and their importance
        
        [Serializable]
        public class InternalConflict
        {
            public string type;                      // Conflict type (duty vs. desire, fear vs. curiosity, etc.)
            public ConflictingValues values;         // Conflicting values
            public string dominantSide;              // Dominant side
            public List<string> triggerConditions;   // Conditions that intensify the conflict
            
            [Serializable]
            public class ConflictingValues
            {
                public float firstValue;  // First value (0-100)
                public float secondValue; // Second value (0-100)
            }
        }
        
        [Serializable]
        public class EmotionalMemory
        {
            public string eventId;           // Related event ID
            public List<EmotionImpact> emotions; // Emotions related to the event
            public float intensity;          // Intensity (decreases over time)
            public long timestamp;           // Occurrence time
            
            [Serializable]
            public class EmotionImpact
            {
                public string emotionType;
                public float impact;
            }
        }
        
        [Serializable]
        public class PersonalValue
        {
            public string valueType;      // Type of value (honesty, loyalty, etc.)
            public float importance;      // Importance level (0-100)
        }
    }
}