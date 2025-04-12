using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Manager for saving and loading character state
    /// </summary>
    public class CharacterStateManager : MonoBehaviour
    {
        [Tooltip("Directory to save character data")]
        [SerializeField] private string saveDirectory = "CharacterData";
        
        /// <summary>
        /// Save all characters to disk
        /// </summary>
        public void SaveAllCharacters()
        {
            // Create directory if it doesn't exist
            string fullPath = Path.Combine(Application.persistentDataPath, saveDirectory);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            
            // Get all characters
            var allCharacters = CharacterManager.Instance.GetAllCharacters();
            
            foreach (var characterEntry in allCharacters)
            {
                SaveCharacter(characterEntry.Key);
            }
        }
        
        /// <summary>
        /// Save a specific character to disk
        /// </summary>
        public void SaveCharacter(string characterId)
        {
            var character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
                return;
                
            // Create save data
            var saveData = new CharacterSaveData
            {
                baseInfo = character.baseInfo,
                desires = character.desires,
                mentalState = character.mentalState,
                complexEmotions = character.complexEmotions
            };
            
            // Convert to JSON
            string json = JsonUtility.ToJson(saveData, true);
            
            // Save to file
            string fullPath = Path.Combine(Application.persistentDataPath, saveDirectory, $"{characterId}.json");
            File.WriteAllText(fullPath, json);
            
            Debug.Log($"Character {characterId} saved to {fullPath}");
        }
        
        /// <summary>
        /// Load all characters from disk
        /// </summary>
        public void LoadAllCharacters()
        {
            string fullPath = Path.Combine(Application.persistentDataPath, saveDirectory);
            
            if (!Directory.Exists(fullPath))
                return;
                
            // Get all JSON files
            string[] files = Directory.GetFiles(fullPath, "*.json");
            
            foreach (string file in files)
            {
                string characterId = Path.GetFileNameWithoutExtension(file);
                LoadCharacter(characterId);
            }
        }
        
        /// <summary>
        /// Load a specific character from disk
        /// </summary>
        public bool LoadCharacter(string characterId)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, saveDirectory, $"{characterId}.json");
            
            if (!File.Exists(fullPath))
                return false;
                
            try
            {
                // Read JSON from file
                string json = File.ReadAllText(fullPath);
                
                // Convert from JSON
                var saveData = JsonUtility.FromJson<CharacterSaveData>(json);
                
                // Create or update character
                var existingCharacter = CharacterManager.Instance.GetCharacter(characterId);
                
                if (existingCharacter != null)
                {
                    // Update existing character
                    existingCharacter.baseInfo = saveData.baseInfo;
                    existingCharacter.desires = saveData.desires;
                    existingCharacter.mentalState = saveData.mentalState;
                    existingCharacter.complexEmotions = saveData.complexEmotions;
                }
                else
                {
                    // Create new character
                    var newCharacter = new CharacterManager.Character
                    {
                        baseInfo = saveData.baseInfo,
                        desires = saveData.desires,
                        mentalState = saveData.mentalState,
                        complexEmotions = saveData.complexEmotions
                    };
                    
                    CharacterManager.Instance.RegisterCharacter(newCharacter);
                }
                
                Debug.Log($"Character {characterId} loaded from {fullPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading character {characterId}: {e.Message}");
                return false;
            }
        }
    }
    
    /// <summary>
    /// Data structure for saving character state
    /// </summary>
    [Serializable]
    public class CharacterSaveData
    {
        public CharacterBase baseInfo;
        public DesireParameters desires;
        public MentalState mentalState;
        public ComplexEmotions complexEmotions;
    }
}