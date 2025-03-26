using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    public class CharacterManager : MonoBehaviour
    {
        private static CharacterManager _instance;
        public static CharacterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CharacterManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CharacterManager");
                        _instance = go.AddComponent<CharacterManager>();
                    }
                }
                return _instance;
            }
        }
        
        // Dictionary to store all characters by ID
        private Dictionary<string, Character> characters = new Dictionary<string, Character>();
        
        // Main Character class that combines all the systems
        [System.Serializable]
        public class Character
        {
            public CharacterBase baseInfo;
            public DesireParameters desires;
            public MentalState mentalState;
            public ComplexEmotions complexEmotions;
            
            // Reference to the game object representing this character
            [HideInInspector]
            public GameObject gameObject;
        }
        
        // Register a character with the manager
        public void RegisterCharacter(Character character)
        {
            if (!characters.ContainsKey(character.baseInfo.characterId))
            {
                characters.Add(character.baseInfo.characterId, character);
            }
        }
        
        // Get a character by ID
        public Character GetCharacter(string characterId)
        {
            if (characters.TryGetValue(characterId, out Character character))
            {
                return character;
            }
            return null;
        }
        
        // Get all characters
        public Dictionary<string, Character> GetAllCharacters()
        {
            return characters;
        }
    }
}