using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Manager for optimizing memory usage of the character system
    /// </summary>
    public class CharacterMemoryManager : MonoBehaviour
    {
        [Tooltip("Maximum number of emotional memories per character")]
        [SerializeField] private int maxEmotionalMemories = 20;
        
        [Tooltip("How often to clean up old memories (seconds)")]
        [SerializeField] private float cleanupInterval = 60f;
        
        private float timeSinceLastCleanup = 0f;
        
        private void Update()
        {
            timeSinceLastCleanup += Time.deltaTime;
            
            if (timeSinceLastCleanup >= cleanupInterval)
            {
                CleanupOldMemories();
                timeSinceLastCleanup = 0f;
            }
        }
        
        /// <summary>
        /// Clean up old emotional memories to prevent memory bloat
        /// </summary>
        private void CleanupOldMemories()
        {
            var allCharacters = CharacterManager.Instance.GetAllCharacters();
            
            foreach (var characterEntry in allCharacters)
            {
                var character = characterEntry.Value;
                
                // Skip if the character has fewer memories than the maximum
                if (character.complexEmotions.emotionalMemory.Count <= maxEmotionalMemories)
                    continue;
                    
                // Sort memories by intensity and timestamp (most intense and recent first)
                character.complexEmotions.emotionalMemory.Sort((a, b) => {
                    // First compare by intensity
                    int intensityCompare = b.intensity.CompareTo(a.intensity);
                    if (intensityCompare != 0)
                        return intensityCompare;
                        
                    // If intensity is equal, compare by timestamp (more recent first)
                    return b.timestamp.CompareTo(a.timestamp);
                });
                
                // Remove the excess memories (least intense and oldest)
                int excessMemories = character.complexEmotions.emotionalMemory.Count - maxEmotionalMemories;
                if (excessMemories > 0)
                {
                    character.complexEmotions.emotionalMemory.RemoveRange(maxEmotionalMemories, excessMemories);
                }
            }
        }
    }
}