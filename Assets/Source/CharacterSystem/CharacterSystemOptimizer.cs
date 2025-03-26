using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Manager for optimizing character system performance
    /// </summary>
    public class CharacterSystemOptimizer : MonoBehaviour
    {
        [Tooltip("Characters within this radius of the player are fully simulated")]
        [SerializeField] private float fullSimulationRadius = 50f;
        
        [Tooltip("Characters outside full simulation radius but within this radius are simplified")]
        [SerializeField] private float simplifiedSimulationRadius = 200f;
        
        [Tooltip("How often to update the list of characters in range (seconds)")]
        [SerializeField] private float updateInterval = 1f;
        
        private Transform playerTransform;
        private float timeSinceLastUpdate = 0f;
        
        // Lists of characters at different detail levels
        private List<string> fullSimulationCharacters = new List<string>();
        private List<string> simplifiedSimulationCharacters = new List<string>();
        private List<string> pausedCharacters = new List<string>();
        
        private void Start()
        {
            // Find the player
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            if (playerTransform == null)
            {
                Debug.LogWarning("Player not found. Character system optimizer requires a GameObject with the 'Player' tag.");
            }
        }
        
        private void Update()
        {
            if (playerTransform == null)
                return;
                
            timeSinceLastUpdate += Time.deltaTime;
            
            // Update character lists at the specified interval
            if (timeSinceLastUpdate >= updateInterval)
            {
                UpdateCharacterLists();
                timeSinceLastUpdate = 0f;
            }
        }
        
        /// <summary>
        /// Update the lists of characters based on distance from player
        /// </summary>
        private void UpdateCharacterLists()
        {
            // Clear the lists
            fullSimulationCharacters.Clear();
            simplifiedSimulationCharacters.Clear();
            pausedCharacters.Clear();
            
            // Get all characters
            var allCharacters = CharacterManager.Instance.GetAllCharacters();
            
            foreach (var characterEntry in allCharacters)
            {
                var character = characterEntry.Value;
                
                // Skip characters without a game object
                if (character.gameObject == null)
                    continue;
                    
                float distanceToPlayer = Vector3.Distance(playerTransform.position, character.gameObject.transform.position);
                
                // Assign to appropriate list based on distance
                if (distanceToPlayer <= fullSimulationRadius)
                {
                    fullSimulationCharacters.Add(character.baseInfo.characterId);
                }
                else if (distanceToPlayer <= simplifiedSimulationRadius)
                {
                    simplifiedSimulationCharacters.Add(character.baseInfo.characterId);
                }
                else
                {
                    pausedCharacters.Add(character.baseInfo.characterId);
                }
            }
            
            // Apply appropriate update method to each list
            ApplyUpdateMethods();
        }
        
        /// <summary>
        /// Apply the appropriate update method to each list of characters
        /// </summary>
        private void ApplyUpdateMethods()
        {
            // For characters in full simulation range
            foreach (var characterId in fullSimulationCharacters)
            {
                var character = CharacterManager.Instance.GetCharacter(characterId);
                if (character == null) continue;
                
                // Apply all updates
                character.gameObject.SendMessage("EnableFullSimulation", SendMessageOptions.DontRequireReceiver);
            }
            
            // For characters in simplified simulation range
            foreach (var characterId in simplifiedSimulationCharacters)
            {
                var character = CharacterManager.Instance.GetCharacter(characterId);
                if (character == null) continue;
                
                // Apply simplified updates
                character.gameObject.SendMessage("EnableSimplifiedSimulation", SendMessageOptions.DontRequireReceiver);
            }
            
            // For characters outside simulation range
            foreach (var characterId in pausedCharacters)
            {
                var character = CharacterManager.Instance.GetCharacter(characterId);
                if (character == null) continue;
                
                // Pause updates
                character.gameObject.SendMessage("DisableSimulation", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
    
    /// <summary>
    /// Component for handling different simulation levels for a character
    /// </summary>
    public class CharacterSimulationHandler : MonoBehaviour
    {
        [SerializeField] private CharacterPsychologySystem psychologySystem;
        [SerializeField] private bool isSimulationEnabled = true;
        [SerializeField] private bool isFullSimulation = true;
        
        // Simplified simulation variables
        private float simplifiedUpdateInterval = 5f;
        private float timeSinceLastUpdate = 0f;
        
        private void Start()
        {
            if (psychologySystem == null)
            {
                psychologySystem = GetComponent<CharacterPsychologySystem>();
            }
        }
        
        private void Update()
        {
            if (!isSimulationEnabled)
                return;
                
            if (isFullSimulation)
            {
                // Full simulation - let the psychology system update normally
            }
            else
            {
                // Simplified simulation - update less frequently
                timeSinceLastUpdate += Time.deltaTime;
                
                if (timeSinceLastUpdate >= simplifiedUpdateInterval)
                {
                    // Apply simplified update
                    SimplifiedUpdate();
                    timeSinceLastUpdate = 0f;
                }
            }
        }
        
        /// <summary>
        /// Enable full simulation mode
        /// </summary>
        public void EnableFullSimulation()
        {
            isSimulationEnabled = true;
            isFullSimulation = true;
        }
        
        /// <summary>
        /// Enable simplified simulation mode
        /// </summary>
        public void EnableSimplifiedSimulation()
        {
            isSimulationEnabled = true;
            isFullSimulation = false;
        }
        
        /// <summary>
        /// Disable simulation
        /// </summary>
        public void DisableSimulation()
        {
            isSimulationEnabled = false;
        }
        
        /// <summary>
        /// Simplified update method that only updates essential elements
        /// </summary>
        private void SimplifiedUpdate()
        {
            if (psychologySystem == null)
                return;
                
            // Only update decay for desires and emotions
            var characterId = GetComponent<CharacterIdentifier>()?.characterId;
            
            if (string.IsNullOrEmpty(characterId))
                return;
                
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return;
                
            // Simplified desire update - only decay
            foreach (var desire in character.desires.desireTypes)
            {
                desire.currentValue = Mathf.Max(0, desire.currentValue - desire.decayRate);
            }
            
            // Update dominant desire
            character.desires.UpdateDominantDesire();
            
            // Simplified emotion update - only decay
            foreach (var emotion in character.mentalState.emotionalStates)
            {
                if (emotion.currentValue > 0)
                {
                    emotion.currentValue = Mathf.Max(0, emotion.currentValue - emotion.decayRate);
                }
                else if (emotion.currentValue < 0)
                {
                    emotion.currentValue = Mathf.Min(0, emotion.currentValue + emotion.decayRate);
                }
            }
            
            // Update mood
            character.mentalState.CalculateCurrentMood();
        }
    }
}