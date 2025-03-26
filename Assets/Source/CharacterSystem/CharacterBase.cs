using System;
using UnityEngine;

namespace CharacterSystem
{
    [Serializable]
    public class CharacterBase
    {
        public string characterId;           // Unique identifier
        public string name;                  // Character name
        public int age;                      // Age
        public string gender;                // Gender
        public string occupation;            // Occupation
        public string personalityType;       // Personality type (introverted, extroverted, etc.)
        public string backstory;             // Background setting
        public string relationshipStatus;    // Relationship status (single, married, etc.)
        public CharacterVisualAssets visualAssets; // References to related visual assets
        
        [Serializable]
        public class CharacterVisualAssets
        {
            public Sprite defaultPortrait;
            public Sprite[] emotionalExpressions;  // Array of facial expressions
            public GameObject characterModel;      // 3D model reference if applicable
            public RuntimeAnimatorController animatorController;
            // Other visual assets as needed
        }
    }
}