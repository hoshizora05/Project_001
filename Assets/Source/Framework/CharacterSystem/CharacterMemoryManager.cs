using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Manager for handling character memories and interactions
    /// </summary>
    public class CharacterMemoryManager : MonoBehaviour
    {
        [Tooltip("Maximum number of emotional memories per character")]
        [SerializeField] private int maxEmotionalMemories = 20;
        
        [Tooltip("Maximum number of interaction memories per character")]
        [SerializeField] private int maxInteractionMemories = 30;
        
        [Tooltip("Maximum number of gift memories per character")]
        [SerializeField] private int maxGiftMemories = 15;
        
        [Tooltip("How often to clean up old memories (seconds)")]
        [SerializeField] private float cleanupInterval = 60f;
        
        private float timeSinceLastCleanup = 0f;
        
        // Dictionary to store interaction memories
        private Dictionary<string, List<InteractionMemory>> _interactionMemories = new Dictionary<string, List<InteractionMemory>>();
        
        // Dictionary to store gift memories
        private Dictionary<string, List<GiftMemory>> _giftMemories = new Dictionary<string, List<GiftMemory>>();
        
        // Dictionary to store negative impression memories
        private Dictionary<string, List<NegativeImpressionMemory>> _negativeImpressions = new Dictionary<string, List<NegativeImpressionMemory>>();
        
        // Dictionary to store experienced events
        private Dictionary<string, HashSet<string>> _experiencedEvents = new Dictionary<string, HashSet<string>>();
        
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
            
            // Clean up interaction memories
            foreach (var characterId in _interactionMemories.Keys)
            {
                var memories = _interactionMemories[characterId];
                if (memories.Count > maxInteractionMemories)
                {
                    // Sort by timestamp (most recent first)
                    memories.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                    
                    // Remove the oldest memories
                    memories.RemoveRange(maxInteractionMemories, memories.Count - maxInteractionMemories);
                }
            }
            
            // Clean up gift memories
            foreach (var characterId in _giftMemories.Keys)
            {
                var memories = _giftMemories[characterId];
                if (memories.Count > maxGiftMemories)
                {
                    // Sort by timestamp (most recent first)
                    memories.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                    
                    // Remove the oldest memories
                    memories.RemoveRange(maxGiftMemories, memories.Count - maxGiftMemories);
                }
            }
        }
        
        /// <summary>
        /// Record an interaction between characters
        /// </summary>
        /// <param name="sourceId">ID of the source character</param>
        /// <param name="targetId">ID of the target character</param>
        /// <param name="topic">Topic of the interaction</param>
        /// <param name="wasSuccessful">Whether the interaction was successful</param>
        public void RecordInteraction(string sourceId, string targetId, object topic, bool wasSuccessful)
        {
            string topicStr = topic.ToString();
            
            // Create memory for the target
            var memory = new InteractionMemory
            {
                WithCharacterId = sourceId,
                Topic = topicStr,
                WasSuccessful = wasSuccessful,
                Timestamp = DateTime.Now
            };
            
            // Ensure the character has a memory list
            if (!_interactionMemories.ContainsKey(targetId))
            {
                _interactionMemories[targetId] = new List<InteractionMemory>();
            }
            
            // Add the memory
            _interactionMemories[targetId].Add(memory);
            
            // Record as an experienced event
            RecordExperiencedEvent(targetId, "interaction_" + topicStr);
        }
        
        /// <summary>
        /// Record a gift received by a character
        /// </summary>
        /// <param name="giverId">ID of the gift giver</param>
        /// <param name="receiverId">ID of the gift receiver</param>
        /// <param name="giftName">Name of the gift</param>
        public void RecordGiftReceived(string giverId, string receiverId, string giftName)
        {
            // Create memory
            var memory = new GiftMemory
            {
                FromCharacterId = giverId,
                GiftName = giftName,
                Timestamp = DateTime.Now
            };
            
            // Ensure the character has a memory list
            if (!_giftMemories.ContainsKey(receiverId))
            {
                _giftMemories[receiverId] = new List<GiftMemory>();
            }
            
            // Add the memory
            _giftMemories[receiverId].Add(memory);
            
            // Record as an experienced event
            RecordExperiencedEvent(receiverId, "gift_received_" + giftName);
        }
        
        /// <summary>
        /// Record a negative impression from an interaction
        /// </summary>
        /// <param name="sourceId">ID of the source character</param>
        /// <param name="targetId">ID of the target character</param>
        /// <param name="topic">Topic of the interaction</param>
        public void RecordNegativeImpression(string sourceId, string targetId, string topic)
        {
            // Create memory
            var memory = new NegativeImpressionMemory
            {
                WithCharacterId = sourceId,
                Context = topic,
                Timestamp = DateTime.Now
            };
            
            // Ensure the character has a memory list
            if (!_negativeImpressions.ContainsKey(targetId))
            {
                _negativeImpressions[targetId] = new List<NegativeImpressionMemory>();
            }
            
            // Add the memory
            _negativeImpressions[targetId].Add(memory);
        }
        
        /// <summary>
        /// Record that a character has experienced an event
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="eventId">Event ID</param>
        public void RecordExperiencedEvent(string characterId, string eventId)
        {
            if (!_experiencedEvents.ContainsKey(characterId))
            {
                _experiencedEvents[characterId] = new HashSet<string>();
            }
            
            _experiencedEvents[characterId].Add(eventId);
        }
        
        /// <summary>
        /// Check if a character has experienced a particular event
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="eventId">Event ID</param>
        /// <returns>True if the character has experienced the event</returns>
        public bool HasExperiencedEvent(string characterId, string eventId)
        {
            if (!_experiencedEvents.ContainsKey(characterId))
                return false;
                
            return _experiencedEvents[characterId].Contains(eventId);
        }
        
        /// <summary>
        /// Get all interaction memories for a character
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <returns>List of interaction memories</returns>
        public List<InteractionMemory> GetInteractionMemories(string characterId)
        {
            if (!_interactionMemories.ContainsKey(characterId))
                return new List<InteractionMemory>();
                
            return new List<InteractionMemory>(_interactionMemories[characterId]);
        }
        
        /// <summary>
        /// Get all gift memories for a character
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <returns>List of gift memories</returns>
        public List<GiftMemory> GetGiftMemories(string characterId)
        {
            if (!_giftMemories.ContainsKey(characterId))
                return new List<GiftMemory>();
                
            return new List<GiftMemory>(_giftMemories[characterId]);
        }
    }
    
    /// <summary>
    /// Represents a memory of an interaction between characters
    /// </summary>
    [Serializable]
    public class InteractionMemory
    {
        public string WithCharacterId;
        public string Topic;
        public bool WasSuccessful;
        public DateTime Timestamp;
    }
    
    /// <summary>
    /// Represents a memory of a gift received
    /// </summary>
    [Serializable]
    public class GiftMemory
    {
        public string FromCharacterId;
        public string GiftName;
        public DateTime Timestamp;
    }
    
    /// <summary>
    /// Represents a memory of a negative impression
    /// </summary>
    [Serializable]
    public class NegativeImpressionMemory
    {
        public string WithCharacterId;
        public string Context;
        public DateTime Timestamp;
    }
}