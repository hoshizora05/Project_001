using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    [Serializable]
    public class DesireParameters
    {
        public string characterId;               // Character ID
        public List<Desire> desireTypes;         // Array of desire types
        public string dominantDesire;            // Current primary desire
        
        [Serializable]
        public class Desire
        {
            public string type;                  // Desire type (recognition, freedom, stability, etc.)
            public float currentValue;           // Current value (0-100)
            public float baseLevel;              // Base level (initial value based on character's personality)
            public DesireThreshold threshold;    // Threshold settings
            public float decayRate;              // Decrease rate over time
            public Dictionary<string, float> satisfactionMultipliers; // Serialized differently
            
            // Unity-friendly serialization of the multipliers dictionary
            [Serializable]
            public class SatisfactionMultiplier
            {
                public string actionId;
                public float multiplier;
            }
            
            public List<SatisfactionMultiplier> serializedMultipliers = new List<SatisfactionMultiplier>();
            
            // Methods to manage the dictionary via the serialized list
            public void InitializeMultipliers()
            {
                satisfactionMultipliers = new Dictionary<string, float>();
                foreach (var item in serializedMultipliers)
                {
                    satisfactionMultipliers[item.actionId] = item.multiplier;
                }
            }
        }
        
        [Serializable]
        public class DesireThreshold
        {
            public float low;    // Low (dissatisfied state)
            public float high;   // High (satisfied state)
        }
        
        // Calculate the dominant desire based on current values and thresholds
        public void UpdateDominantDesire()
        {
            float highestPriority = 0f;
            
            foreach (var desire in desireTypes)
            {
                // Calculate priority based on how far the desire is from its threshold
                float distanceFromThreshold = 0;
                
                if (desire.currentValue < desire.threshold.low)
                {
                    distanceFromThreshold = desire.threshold.low - desire.currentValue;
                }
                
                // If this desire has higher priority, make it dominant
                if (distanceFromThreshold > highestPriority)
                {
                    highestPriority = distanceFromThreshold;
                    dominantDesire = desire.type;
                }
            }
        }
    }
}