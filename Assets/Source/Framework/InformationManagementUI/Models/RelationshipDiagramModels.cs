using System;
using System.Collections.Generic;
using UnityEngine;

namespace InformationManagementUI
{
    /// <summary>
    /// Data structure for relationship graph data
    /// </summary>
    [CreateAssetMenu(fileName = "RelationshipGraphData", menuName = "Project_001/Information UI/Relationship Graph Data")]
    public class RelationshipGraphData : ScriptableObject, IRelationshipDataProvider
    {
        [SerializeField] private List<CharacterNode> _characters = new List<CharacterNode>();
        [SerializeField] private List<RelationshipEdge> _relationships = new List<RelationshipEdge>();
        [SerializeField] private List<CharacterGroup> _groups = new List<CharacterGroup>();
        
        private Dictionary<string, CharacterNode> _characterLookup = new Dictionary<string, CharacterNode>();
        private Dictionary<string, CharacterGroup> _groupLookup = new Dictionary<string, CharacterGroup>();
        private Dictionary<string, List<RelationshipEdge>> _characterRelationships = new Dictionary<string, List<RelationshipEdge>>();
        
        public IReadOnlyList<CharacterNode> Characters => _characters;
        public IReadOnlyList<RelationshipEdge> Relationships => _relationships;
        public IReadOnlyList<CharacterGroup> Groups => _groups;
        
        public event Action OnGraphDataChanged;
        public event Action<CharacterNode> OnCharacterAdded;
        public event Action<string> OnCharacterRemoved;
        public event Action<RelationshipEdge> OnRelationshipChanged;
        
        private void OnEnable()
        {
            RebuildLookups();
        }
        
        private void RebuildLookups()
        {
            _characterLookup.Clear();
            _groupLookup.Clear();
            _characterRelationships.Clear();
            
            // Build character lookup
            foreach (var character in _characters)
            {
                _characterLookup[character.CharacterID] = character;
                _characterRelationships[character.CharacterID] = new List<RelationshipEdge>();
            }
            
            // Build group lookup
            foreach (var group in _groups)
            {
                _groupLookup[group.GroupID] = group;
            }
            
            // Build relationship lookup
            foreach (var relationship in _relationships)
            {
                if (_characterRelationships.ContainsKey(relationship.SourceCharacterID))
                {
                    _characterRelationships[relationship.SourceCharacterID].Add(relationship);
                }
                
                if (_characterRelationships.ContainsKey(relationship.TargetCharacterID))
                {
                    _characterRelationships[relationship.TargetCharacterID].Add(relationship);
                }
            }
        }
        
        public void AddCharacter(CharacterNode character)
        {
            if (!_characterLookup.ContainsKey(character.CharacterID))
            {
                _characters.Add(character);
                _characterLookup[character.CharacterID] = character;
                _characterRelationships[character.CharacterID] = new List<RelationshipEdge>();
                OnCharacterAdded?.Invoke(character);
                OnGraphDataChanged?.Invoke();
            }
        }
        
        public void UpdateCharacter(CharacterNode character)
        {
            if (_characterLookup.ContainsKey(character.CharacterID))
            {
                int index = _characters.FindIndex(c => c.CharacterID == character.CharacterID);
                if (index >= 0)
                {
                    _characters[index] = character;
                    _characterLookup[character.CharacterID] = character;
                    OnGraphDataChanged?.Invoke();
                }
            }
        }
        
        public void RemoveCharacter(string characterID)
        {
            if (_characterLookup.ContainsKey(characterID))
            {
                _characters.RemoveAll(c => c.CharacterID == characterID);
                
                // Also remove any relationships involving this character
                _relationships.RemoveAll(r => 
                    r.SourceCharacterID == characterID || 
                    r.TargetCharacterID == characterID
                );
                
                // Update lookups
                _characterLookup.Remove(characterID);
                _characterRelationships.Remove(characterID);
                
                OnCharacterRemoved?.Invoke(characterID);
                OnGraphDataChanged?.Invoke();
            }
        }
        
        public void AddOrUpdateRelationship(RelationshipEdge relationship)
        {
            int index = _relationships.FindIndex(r => 
                r.SourceCharacterID == relationship.SourceCharacterID && 
                r.TargetCharacterID == relationship.TargetCharacterID
            );
            
            if (index >= 0)
            {
                _relationships[index] = relationship;
            }
            else
            {
                _relationships.Add(relationship);
            }
            
            // Update relationship lookup
            if (_characterRelationships.ContainsKey(relationship.SourceCharacterID))
            {
                _characterRelationships[relationship.SourceCharacterID].RemoveAll(r => 
                    r.SourceCharacterID == relationship.SourceCharacterID && 
                    r.TargetCharacterID == relationship.TargetCharacterID
                );
                _characterRelationships[relationship.SourceCharacterID].Add(relationship);
            }
            
            if (_characterRelationships.ContainsKey(relationship.TargetCharacterID))
            {
                _characterRelationships[relationship.TargetCharacterID].RemoveAll(r => 
                    r.SourceCharacterID == relationship.SourceCharacterID && 
                    r.TargetCharacterID == relationship.TargetCharacterID
                );
                _characterRelationships[relationship.TargetCharacterID].Add(relationship);
            }
            
            OnRelationshipChanged?.Invoke(relationship);
            OnGraphDataChanged?.Invoke();
        }
        
        public void RemoveRelationship(string sourceCharacterID, string targetCharacterID)
        {
            _relationships.RemoveAll(r => 
                r.SourceCharacterID == sourceCharacterID && 
                r.TargetCharacterID == targetCharacterID
            );
            
            // Update relationship lookup
            if (_characterRelationships.ContainsKey(sourceCharacterID))
            {
                _characterRelationships[sourceCharacterID].RemoveAll(r => 
                    r.SourceCharacterID == sourceCharacterID && 
                    r.TargetCharacterID == targetCharacterID
                );
            }
            
            if (_characterRelationships.ContainsKey(targetCharacterID))
            {
                _characterRelationships[targetCharacterID].RemoveAll(r => 
                    r.SourceCharacterID == sourceCharacterID && 
                    r.TargetCharacterID == targetCharacterID
                );
            }
            
            OnGraphDataChanged?.Invoke();
        }
        
        public void AddGroup(CharacterGroup group)
        {
            if (!_groupLookup.ContainsKey(group.GroupID))
            {
                _groups.Add(group);
                _groupLookup[group.GroupID] = group;
                OnGraphDataChanged?.Invoke();
            }
        }
        
        public void UpdateGroup(CharacterGroup group)
        {
            if (_groupLookup.ContainsKey(group.GroupID))
            {
                int index = _groups.FindIndex(g => g.GroupID == group.GroupID);
                if (index >= 0)
                {
                    _groups[index] = group;
                    _groupLookup[group.GroupID] = group;
                    OnGraphDataChanged?.Invoke();
                }
            }
        }
        
        public void RemoveGroup(string groupID)
        {
            if (_groupLookup.ContainsKey(groupID))
            {
                _groups.RemoveAll(g => g.GroupID == groupID);
                _groupLookup.Remove(groupID);
                OnGraphDataChanged?.Invoke();
            }
        }
        
        public CharacterNode GetCharacter(string characterID)
        {
            return _characterLookup.TryGetValue(characterID, out var character) ? character : null;
        }
        
        public List<RelationshipEdge> GetCharacterRelationships(string characterID)
        {
            return _characterRelationships.TryGetValue(characterID, out var relationships) 
                ? new List<RelationshipEdge>(relationships) 
                : new List<RelationshipEdge>();
        }
        
        public List<CharacterNode> GetGroupMembers(string groupID)
        {
            if (!_groupLookup.TryGetValue(groupID, out var group))
            {
                return new List<CharacterNode>();
            }
            
            List<CharacterNode> members = new List<CharacterNode>();
            foreach (var memberID in group.MemberIDs)
            {
                if (_characterLookup.TryGetValue(memberID, out var character))
                {
                    members.Add(character);
                }
            }
            
            return members;
        }
        
        public void AddCharacterToGroup(string characterID, string groupID)
        {
            if (_characterLookup.ContainsKey(characterID) && _groupLookup.ContainsKey(groupID))
            {
                var group = _groupLookup[groupID];
                if (!((List<string>)group.MemberIDs).Contains(characterID))
                {
                    int index = _groups.FindIndex(g => g.GroupID == groupID);
                    if (index >= 0)
                    {
                        var updatedGroup = new CharacterGroup(
                            group.GroupID,
                            group.Name,
                            group.Color,
                            new List<string>(group.MemberIDs) { characterID }
                        );
                        
                        _groups[index] = updatedGroup;
                        _groupLookup[groupID] = updatedGroup;
                        
                        // Also update character's group IDs
                        var character = _characterLookup[characterID];
                        var updatedGroupIDs = new List<string>(character.GroupIDs);
                        if (!updatedGroupIDs.Contains(groupID))
                        {
                            updatedGroupIDs.Add(groupID);
                            var updatedCharacter = new CharacterNode(
                                character.CharacterID,
                                character.Name,
                                character.Type,
                                character.Position,
                                updatedGroupIDs,
                                character.Portrait
                            );
                            
                            int charIndex = _characters.FindIndex(c => c.CharacterID == characterID);
                            if (charIndex >= 0)
                            {
                                _characters[charIndex] = updatedCharacter;
                                _characterLookup[characterID] = updatedCharacter;
                            }
                        }
                        
                        OnGraphDataChanged?.Invoke();
                    }
                }
            }
        }
        
        public void RemoveCharacterFromGroup(string characterID, string groupID)
        {
            if (_characterLookup.ContainsKey(characterID) && _groupLookup.ContainsKey(groupID))
            {
                var group = _groupLookup[groupID];
                if (((List<string>)group.MemberIDs).Contains(characterID))
                {
                    int index = _groups.FindIndex(g => g.GroupID == groupID);
                    if (index >= 0)
                    {
                        var updatedMemberIDs = new List<string>(group.MemberIDs);
                        updatedMemberIDs.Remove(characterID);
                        
                        var updatedGroup = new CharacterGroup(
                            group.GroupID,
                            group.Name,
                            group.Color,
                            updatedMemberIDs
                        );
                        
                        _groups[index] = updatedGroup;
                        _groupLookup[groupID] = updatedGroup;
                        
                        // Also update character's group IDs
                        var character = _characterLookup[characterID];
                        var updatedGroupIDs = new List<string>(character.GroupIDs);
                        updatedGroupIDs.Remove(groupID);
                        
                        var updatedCharacter = new CharacterNode(
                            character.CharacterID,
                            character.Name,
                            character.Type,
                            character.Position,
                            updatedGroupIDs,
                            character.Portrait
                        );
                        
                        int charIndex = _characters.FindIndex(c => c.CharacterID == characterID);
                        if (charIndex >= 0)
                        {
                            _characters[charIndex] = updatedCharacter;
                            _characterLookup[characterID] = updatedCharacter;
                        }
                        
                        OnGraphDataChanged?.Invoke();
                    }
                }
            }
        }
        
        public void UpdateCharacterPosition(string characterID, Vector2 newPosition)
        {
            if (_characterLookup.TryGetValue(characterID, out var character))
            {
                var updatedCharacter = new CharacterNode(
                    character.CharacterID,
                    character.Name,
                    character.Type,
                    newPosition,
                    new List<string>(character.GroupIDs),
                    character.Portrait
                );
                
                int index = _characters.FindIndex(c => c.CharacterID == characterID);
                if (index >= 0)
                {
                    _characters[index] = updatedCharacter;
                    _characterLookup[characterID] = updatedCharacter;
                    OnGraphDataChanged?.Invoke();
                }
            }
        }
    }
    
    /// <summary>
    /// Interface for relationship data providers
    /// </summary>
    public interface IRelationshipDataProvider
    {
        IReadOnlyList<CharacterNode> Characters { get; }
        IReadOnlyList<RelationshipEdge> Relationships { get; }
        IReadOnlyList<CharacterGroup> Groups { get; }
        
        event Action OnGraphDataChanged;
        event Action<CharacterNode> OnCharacterAdded;
        event Action<string> OnCharacterRemoved;
        event Action<RelationshipEdge> OnRelationshipChanged;
        
        void AddCharacter(CharacterNode character);
        void UpdateCharacter(CharacterNode character);
        void RemoveCharacter(string characterID);
        void AddOrUpdateRelationship(RelationshipEdge relationship);
        void RemoveRelationship(string sourceCharacterID, string targetCharacterID);
        void AddGroup(CharacterGroup group);
        void UpdateGroup(CharacterGroup group);
        void RemoveGroup(string groupID);
        CharacterNode GetCharacter(string characterID);
        List<RelationshipEdge> GetCharacterRelationships(string characterID);
        List<CharacterNode> GetGroupMembers(string groupID);
        void AddCharacterToGroup(string characterID, string groupID);
        void RemoveCharacterFromGroup(string characterID, string groupID);
        void UpdateCharacterPosition(string characterID, Vector2 newPosition);
    }
    
    /// <summary>
    /// Represents a character node in the relationship graph
    /// </summary>
    [Serializable]
    public class CharacterNode
    {
        [SerializeField] private string _characterID;
        [SerializeField] private string _name;
        [SerializeField] private CharacterType _type;
        [SerializeField] private Vector2 _position;
        [SerializeField] private List<string> _groupIDs = new List<string>();
        [SerializeField] private Sprite _portrait;
        
        public string CharacterID => _characterID;
        public string Name => _name;
        public CharacterType Type => _type;
        public Vector2 Position => _position;
        public IReadOnlyList<string> GroupIDs => _groupIDs;
        public Sprite Portrait => _portrait;
        
        public CharacterNode(
            string characterID,
            string name,
            CharacterType type,
            Vector2 position,
            List<string> groupIDs = null,
            Sprite portrait = null)
        {
            _characterID = characterID;
            _name = name;
            _type = type;
            _position = position;
            _groupIDs = groupIDs ?? new List<string>();
            _portrait = portrait;
        }
    }
    
    /// <summary>
    /// Represents a relationship edge between two characters
    /// </summary>
    [Serializable]
    public class RelationshipEdge
    {
        [SerializeField] private string _sourceCharacterID;
        [SerializeField] private string _targetCharacterID;
        [SerializeField] private RelationshipType _type;
        [SerializeField] private float _strength; // 0.0 to 1.0
        [SerializeField] private List<RelationshipAttribute> _attributes = new List<RelationshipAttribute>();
        
        public string SourceCharacterID => _sourceCharacterID;
        public string TargetCharacterID => _targetCharacterID;
        public RelationshipType Type => _type;
        public float Strength => _strength;
        public IReadOnlyList<RelationshipAttribute> Attributes => _attributes;
        
        public RelationshipEdge(
            string sourceCharacterID,
            string targetCharacterID,
            RelationshipType type,
            float strength,
            List<RelationshipAttribute> attributes = null)
        {
            _sourceCharacterID = sourceCharacterID;
            _targetCharacterID = targetCharacterID;
            _type = type;
            _strength = Mathf.Clamp01(strength);
            _attributes = attributes ?? new List<RelationshipAttribute>();
        }
        
        public bool HasAttribute(RelationshipAttributeType attributeType)
        {
            return _attributes.Exists(a => a.Type == attributeType);
        }
    }
    
    /// <summary>
    /// Represents a group of characters in the relationship graph
    /// </summary>
    [Serializable]
    public class CharacterGroup
    {
        [SerializeField] private string _groupID;
        [SerializeField] private string _name;
        [SerializeField] private Color _color;
        [SerializeField] private List<string> _memberIDs = new List<string>();
        
        public string GroupID => _groupID;
        public string Name => _name;
        public Color Color => _color;
        public IReadOnlyList<string> MemberIDs => _memberIDs;
        
        public CharacterGroup(
            string groupID,
            string name,
            Color color,
            List<string> memberIDs = null)
        {
            _groupID = groupID;
            _name = name;
            _color = color;
            _memberIDs = memberIDs ?? new List<string>();
        }
    }
    
    /// <summary>
    /// Represents an attribute of a relationship
    /// </summary>
    [Serializable]
    public class RelationshipAttribute
    {
        [SerializeField] private RelationshipAttributeType _type;
        [SerializeField] private float _value; // Meaning depends on attribute type
        
        public RelationshipAttributeType Type => _type;
        public float Value => _value;
        
        public RelationshipAttribute(RelationshipAttributeType type, float value)
        {
            _type = type;
            _value = value;
        }
    }
    
    /// <summary>
    /// Enum representing the type of character
    /// </summary>
    public enum CharacterType
    {
        Player,
        Ally,
        Neutral,
        Rival,
        Enemy,
        NPC
    }
    
    /// <summary>
    /// Enum representing the type of relationship
    /// </summary>
    public enum RelationshipType
    {
        Friend,
        Family,
        Romantic,
        Colleague,
        Ally,
        Rival,
        Enemy,
        Acquaintance,
        Business,
        Custom
    }
    
    /// <summary>
    /// Enum representing the type of relationship attribute
    /// </summary>
    public enum RelationshipAttributeType
    {
        Trust,
        Respect,
        Affection,
        Loyalty,
        Dependence,
        Influence,
        Conflict,
        Envy,
        History,
        Secret,
        Debt,
        Custom
    }
}