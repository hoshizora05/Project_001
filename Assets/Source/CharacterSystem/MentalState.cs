using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    [Serializable]
    public class MentalState
    {
        public string characterId;               // Character ID
        public List<EmotionalState> emotionalStates; // Array of emotional states
        public List<MoodModifier> moodModifiers; // Mood modifiers
        public string currentMood;               // Current overall mood
        public float stressLevel;                // Stress level (0-100)
        
        [Serializable]
        public class EmotionalState
        {
            public string type;          // Emotion type (happiness, anxiety, anger, etc.)
            public float currentValue;   // Current value (-100 to 100)
            public float volatility;     // Volatility
            public float decayRate;      // Decay rate over time
        }
        
        [Serializable]
        public class MoodModifier
        {
            public string source;               // Source
            public List<EmotionEffect> effects; // Effect on each emotion
            public int duration;                // Duration (number of turns)
            public int remainingTime;           // Remaining time
            
            [Serializable]
            public class EmotionEffect
            {
                public string emotionType;
                public float effectValue;
            }
        }
        
        // Calculate the current mood based on emotional states
        public void CalculateCurrentMood()
        {
            // Simple implementation - find the strongest emotion
            float strongestEmotion = 0f;
            
            foreach (var emotion in emotionalStates)
            {
                if (Mathf.Abs(emotion.currentValue) > Mathf.Abs(strongestEmotion))
                {
                    strongestEmotion = emotion.currentValue;
                    currentMood = emotion.type;
                }
            }
            
            // More complex implementations could blend emotions
        }
    }
}