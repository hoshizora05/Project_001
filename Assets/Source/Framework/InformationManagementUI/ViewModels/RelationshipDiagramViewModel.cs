using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using System.Collections;

namespace InformationManagementUI
{
    /// <summary>
    /// ViewModel for the relationship diagram component
    /// </summary>
    public class RelationshipDiagramViewModel
    {
        private readonly IRelationshipDataProvider _dataProvider;
        
        // Observable properties
        private readonly ReactiveCollection<CharacterNode> _characters = new ReactiveCollection<CharacterNode>();
        private readonly ReactiveCollection<RelationshipEdge> _relationships = new ReactiveCollection<RelationshipEdge>();
        private readonly ReactiveCollection<CharacterGroup> _groups = new ReactiveCollection<CharacterGroup>();
        
        private readonly ReactiveProperty<string> _focusCharacterId = new ReactiveProperty<string>();
        private readonly ReactiveProperty<CharacterNode> _selectedCharacter = new ReactiveProperty<CharacterNode>();
        private readonly ReactiveProperty<RelationshipEdge> _selectedRelationship = new ReactiveProperty<RelationshipEdge>();
        private readonly ReactiveProperty<CharacterGroup> _selectedGroup = new ReactiveProperty<CharacterGroup>();
        
        private readonly ReactiveProperty<float> _zoomLevel = new ReactiveProperty<float>(1.0f);
        private readonly ReactiveProperty<Vector2> _panPosition = new ReactiveProperty<Vector2>(Vector2.zero);
        private readonly ReactiveProperty<string> _searchQuery = new ReactiveProperty<string>(string.Empty);
        
        // Filtered collections
        private readonly ReactiveCollection<CharacterNode> _filteredCharacters = new ReactiveCollection<CharacterNode>();
        private readonly ReactiveCollection<RelationshipEdge> _filteredRelationships = new ReactiveCollection<RelationshipEdge>();
        
        // Observable commands
        private readonly ReactiveCommand<string> _selectCharacterCommand = new ReactiveCommand<string>();
        private readonly ReactiveCommand<string> _selectRelationshipCommand = new ReactiveCommand<string>();
        private readonly ReactiveCommand<string> _selectGroupCommand = new ReactiveCommand<string>();
        private readonly ReactiveCommand<float> _zoomCommand = new ReactiveCommand<float>();
        private readonly ReactiveCommand<Vector2> _panCommand = new ReactiveCommand<Vector2>();
        private readonly ReactiveCommand<string> _searchCommand = new ReactiveCommand<string>();
        private readonly ReactiveCommand _resetViewCommand = new ReactiveCommand();
        private readonly ReactiveCommand<Tuple<string, Vector2>> _moveCharacterCommand = new ReactiveCommand<Tuple<string, Vector2>>();
        
        private readonly ReactiveCommand<List<string>> _filterCharacterTypesCommand = new ReactiveCommand<List<string>>();
        private readonly ReactiveCommand<List<RelationshipType>> _filterRelationshipTypesCommand = new ReactiveCommand<List<RelationshipType>>();
        
        // Public properties
        public IReadOnlyReactiveCollection<CharacterNode> Characters => _characters;
        public IReadOnlyReactiveCollection<RelationshipEdge> Relationships => _relationships;
        public IReadOnlyReactiveCollection<CharacterGroup> Groups => _groups;
        
        public IReactiveProperty<string> FocusCharacterId => _focusCharacterId;
        public IReactiveProperty<CharacterNode> SelectedCharacter => _selectedCharacter;
        public IReactiveProperty<RelationshipEdge> SelectedRelationship => _selectedRelationship;
        public IReactiveProperty<CharacterGroup> SelectedGroup => _selectedGroup;
        
        public IReactiveProperty<float> ZoomLevel => _zoomLevel;
        public IReactiveProperty<Vector2> PanPosition => _panPosition;
        public IReactiveProperty<string> SearchQuery => _searchQuery;
        
        public IReadOnlyReactiveCollection<CharacterNode> FilteredCharacters => _filteredCharacters;
        public IReadOnlyReactiveCollection<RelationshipEdge> FilteredRelationships => _filteredRelationships;
        
        // Commands
        public IReactiveCommand<string> SelectCharacterCommand => _selectCharacterCommand;
        public IReactiveCommand<string> SelectRelationshipCommand => _selectRelationshipCommand;
        public IReactiveCommand<string> SelectGroupCommand => _selectGroupCommand;
        public IReactiveCommand<float> ZoomCommand => _zoomCommand;
        public IReactiveCommand<Vector2> PanCommand => _panCommand;
        public IReactiveCommand<string> SearchCommand => _searchCommand;
        public IReactiveCommand<Unit> ResetViewCommand => _resetViewCommand;
        public IReactiveCommand<Tuple<string, Vector2>> MoveCharacterCommand => _moveCharacterCommand;
        
        public IReactiveCommand<List<string>> FilterCharacterTypesCommand => _filterCharacterTypesCommand;
        public IReactiveCommand<List<RelationshipType>> FilterRelationshipTypesCommand => _filterRelationshipTypesCommand;
        
        // Filter states
        private readonly ReactiveCollection<string> _activeCharacterTypeFilters = new ReactiveCollection<string>();
        private readonly ReactiveCollection<RelationshipType> _activeRelationshipTypeFilters = new ReactiveCollection<RelationshipType>();
        
        public IReadOnlyReactiveCollection<string> ActiveCharacterTypeFilters => _activeCharacterTypeFilters;
        public IReadOnlyReactiveCollection<RelationshipType> ActiveRelationshipTypeFilters => _activeRelationshipTypeFilters;
        
        // CompositeDisposable for cleanup
        private CompositeDisposable CompositeDisposable { get; } = new CompositeDisposable();
        
        public RelationshipDiagramViewModel(IRelationshipDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            
            // Setup command handlers
            SetupCommands();
            
            // Setup data listeners
            SetupDataListeners();
            
            // Initial data load
            RefreshData();
            
            // Setup filtered collections
            SetupFilteredCollections();
        }
        
        private void SetupCommands()
        {
            _selectCharacterCommand.Subscribe(id => SelectCharacter(id)).AddTo(CompositeDisposable);
            _selectRelationshipCommand.Subscribe(id => SelectRelationship(id)).AddTo(CompositeDisposable);
            _selectGroupCommand.Subscribe(id => SelectGroup(id)).AddTo(CompositeDisposable);
            _zoomCommand.Subscribe(level => SetZoomLevel(level)).AddTo(CompositeDisposable);
            _panCommand.Subscribe(position => SetPanPosition(position)).AddTo(CompositeDisposable);
            _searchCommand.Subscribe(query => SetSearchQuery(query)).AddTo(CompositeDisposable);
            _resetViewCommand.Subscribe(_ => ResetView()).AddTo(CompositeDisposable);
            _moveCharacterCommand.Subscribe(tuple => MoveCharacter(tuple.Item1, tuple.Item2)).AddTo(CompositeDisposable);
            
            _filterCharacterTypesCommand.Subscribe(types => FilterCharacterTypes(types)).AddTo(CompositeDisposable);
            _filterRelationshipTypesCommand.Subscribe(types => FilterRelationshipTypes(types)).AddTo(CompositeDisposable);
        }
        
        private void SetupDataListeners()
        {
            if (_dataProvider != null)
            {
                // Subscribe to data provider events
                _dataProvider.OnGraphDataChanged += RefreshData;
                _dataProvider.OnCharacterAdded += _ => RefreshCharacters();
                _dataProvider.OnCharacterRemoved += _ => RefreshCharacters();
                _dataProvider.OnRelationshipChanged += _ => RefreshRelationships();
            }
            
            // When focus character changes, update related collections
            _focusCharacterId.Subscribe(_ => ApplyFilters()).AddTo(CompositeDisposable);
        }
        
        private void SetupFilteredCollections()
        {
            // Set up subscriptions to update filtered collections when source collections or filters change
            _searchQuery.Subscribe(_ => ApplyFilters()).AddTo(CompositeDisposable);
            
            // Apply initial filters
            ApplyFilters();
        }
        
        /// <summary>
        /// Refreshes all relationship data from the data provider
        /// </summary>
        public void RefreshData()
        {
            RefreshCharacters();
            RefreshRelationships();
            RefreshGroups();
            ApplyFilters();
        }
        
        /// <summary>
        /// Refreshes the characters collection from the data provider
        /// </summary>
        private void RefreshCharacters()
        {
            if (_dataProvider == null) return;
            
            _characters.Clear();
            foreach (var character in _dataProvider.Characters)
            {
                _characters.Add(character);
            }
            
            ApplyFilters();
            
            // If selected character was removed, clear the selection
            if (_selectedCharacter.Value != null && 
                !_characters.Contains(_selectedCharacter.Value))
            {
                _selectedCharacter.Value = null;
            }
        }
        
        /// <summary>
        /// Refreshes the relationships collection from the data provider
        /// </summary>
        private void RefreshRelationships()
        {
            if (_dataProvider == null) return;
            
            _relationships.Clear();
            foreach (var relationship in _dataProvider.Relationships)
            {
                _relationships.Add(relationship);
            }
            
            ApplyFilters();
            
            // If selected relationship was removed, clear the selection
            if (_selectedRelationship.Value != null && 
                !_relationships.Contains(_selectedRelationship.Value))
            {
                _selectedRelationship.Value = null;
            }
        }
        
        /// <summary>
        /// Refreshes the groups collection from the data provider
        /// </summary>
        private void RefreshGroups()
        {
            if (_dataProvider == null) return;
            
            _groups.Clear();
            foreach (var group in _dataProvider.Groups)
            {
                _groups.Add(group);
            }
            
            // If selected group was removed, clear the selection
            if (_selectedGroup.Value != null && 
                !_groups.Contains(_selectedGroup.Value))
            {
                _selectedGroup.Value = null;
            }
        }
        
        /// <summary>
        /// Applies all active filters to refresh the filtered collections
        /// </summary>
        private void ApplyFilters()
        {
            // Clear filtered collections
            _filteredCharacters.Clear();
            _filteredRelationships.Clear();
            
            // First, filter characters
            var filteredCharacters = new List<CharacterNode>();
            foreach (var character in _characters)
            {
                bool includeCharacter = true;
                
                // Apply search filter if applicable
                if (!string.IsNullOrEmpty(_searchQuery.Value))
                {
                    if (!character.Name.ToLower().Contains(_searchQuery.Value.ToLower()))
                    {
                        includeCharacter = false;
                    }
                }
                
                // Apply character type filter if applicable
                if (_activeCharacterTypeFilters.Count > 0)
                {
                    if (!_activeCharacterTypeFilters.Contains(character.Type.ToString()))
                    {
                        includeCharacter = false;
                    }
                }
                
                // Apply focus character filter if applicable
                if (!string.IsNullOrEmpty(_focusCharacterId.Value) && 
                    character.CharacterID != _focusCharacterId.Value)
                {
                    bool isRelated = false;
                    
                    // Check if this character has a relationship with the focus character
                    foreach (var relationship in _relationships)
                    {
                        if ((relationship.SourceCharacterID == _focusCharacterId.Value && 
                             relationship.TargetCharacterID == character.CharacterID) ||
                            (relationship.TargetCharacterID == _focusCharacterId.Value && 
                             relationship.SourceCharacterID == character.CharacterID))
                        {
                            isRelated = true;
                            break;
                        }
                    }
                    
                    if (!isRelated)
                    {
                        includeCharacter = false;
                    }
                }
                
                if (includeCharacter)
                {
                    filteredCharacters.Add(character);
                }
            }
            
            // Then, filter relationships
            var filteredRelationships = new List<RelationshipEdge>();
            foreach (var relationship in _relationships)
            {
                bool includeRelationship = true;
                
                // Apply relationship type filter if applicable
                if (_activeRelationshipTypeFilters.Count > 0)
                {
                    if (!_activeRelationshipTypeFilters.Contains(relationship.Type))
                    {
                        includeRelationship = false;
                    }
                }
                
                // Only include relationships between filtered characters
                bool sourceIncluded = filteredCharacters.Exists(c => c.CharacterID == relationship.SourceCharacterID);
                bool targetIncluded = filteredCharacters.Exists(c => c.CharacterID == relationship.TargetCharacterID);
                
                if (!sourceIncluded || !targetIncluded)
                {
                    includeRelationship = false;
                }
                
                if (includeRelationship)
                {
                    filteredRelationships.Add(relationship);
                }
            }
            
            // Update the filtered collections
            foreach (var character in filteredCharacters)
            {
                _filteredCharacters.Add(character);
            }
            
            foreach (var relationship in filteredRelationships)
            {
                _filteredRelationships.Add(relationship);
            }
        }
        
        /// <summary>
        /// Selects a character by ID
        /// </summary>
        /// <param name="characterId">The ID of the character to select</param>
        public void SelectCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                _selectedCharacter.Value = null;
                return;
            }
            
            foreach (var character in _characters)
            {
                if (character.CharacterID == characterId)
                {
                    _selectedCharacter.Value = character;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Selects a relationship by source and target character IDs
        /// </summary>
        /// <param name="relationshipId">The combination of source and target character IDs (format: "sourceID:targetID")</param>
        public void SelectRelationship(string relationshipId)
        {
            if (string.IsNullOrEmpty(relationshipId))
            {
                _selectedRelationship.Value = null;
                return;
            }
            
            string[] parts = relationshipId.Split(':');
            if (parts.Length != 2)
            {
                Debug.LogError($"Invalid relationship ID format: {relationshipId}");
                return;
            }
            
            string sourceId = parts[0];
            string targetId = parts[1];
            
            foreach (var relationship in _relationships)
            {
                if (relationship.SourceCharacterID == sourceId && relationship.TargetCharacterID == targetId)
                {
                    _selectedRelationship.Value = relationship;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Selects a group by ID
        /// </summary>
        /// <param name="groupId">The ID of the group to select</param>
        public void SelectGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                _selectedGroup.Value = null;
                return;
            }
            
            foreach (var group in _groups)
            {
                if (group.GroupID == groupId)
                {
                    _selectedGroup.Value = group;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Sets the zoom level of the diagram
        /// </summary>
        /// <param name="level">The new zoom level</param>
        public void SetZoomLevel(float level)
        {
            _zoomLevel.Value = Mathf.Clamp(level, 0.1f, 3.0f);
        }
        
        /// <summary>
        /// Sets the pan position of the diagram
        /// </summary>
        /// <param name="position">The new pan position</param>
        public void SetPanPosition(Vector2 position)
        {
            _panPosition.Value = position;
        }
        
        /// <summary>
        /// Sets the search query for filtering characters
        /// </summary>
        /// <param name="query">The search query</param>
        public void SetSearchQuery(string query)
        {
            _searchQuery.Value = query ?? string.Empty;
        }
        
        /// <summary>
        /// Resets the view to default settings
        /// </summary>
        public void ResetView()
        {
            _zoomLevel.Value = 1.0f;
            _panPosition.Value = Vector2.zero;
            _searchQuery.Value = string.Empty;
            _focusCharacterId.Value = string.Empty;
            _selectedCharacter.Value = null;
            _selectedRelationship.Value = null;
            _selectedGroup.Value = null;
            _activeCharacterTypeFilters.Clear();
            _activeRelationshipTypeFilters.Clear();
            ApplyFilters();
        }
        
        /// <summary>
        /// Sets the focus character
        /// </summary>
        /// <param name="characterId">The ID of the character to focus on</param>
        public void SetFocusCharacter(string characterId)
        {
            _focusCharacterId.Value = characterId;
            
            // If we're setting a focus character, also select that character
            if (!string.IsNullOrEmpty(characterId))
            {
                SelectCharacter(characterId);
            }
        }
        
        /// <summary>
        /// Moves a character to a new position
        /// </summary>
        /// <param name="characterId">The ID of the character to move</param>
        /// <param name="newPosition">The new position</param>
        public void MoveCharacter(string characterId, Vector2 newPosition)
        {
            _dataProvider?.UpdateCharacterPosition(characterId, newPosition);
        }
        
        /// <summary>
        /// Filters characters by type
        /// </summary>
        /// <param name="characterTypes">The list of character types to include</param>
        public void FilterCharacterTypes(List<string> characterTypes)
        {
            _activeCharacterTypeFilters.Clear();
            if (characterTypes != null)
            {
                foreach (var type in characterTypes)
                {
                    _activeCharacterTypeFilters.Add(type);
                }
            }
            ApplyFilters();
        }
        
        /// <summary>
        /// Filters relationships by type
        /// </summary>
        /// <param name="relationshipTypes">The list of relationship types to include</param>
        public void FilterRelationshipTypes(List<RelationshipType> relationshipTypes)
        {
            _activeRelationshipTypeFilters.Clear();
            if (relationshipTypes != null)
            {
                foreach (var type in relationshipTypes)
                {
                    _activeRelationshipTypeFilters.Add(type);
                }
            }
            ApplyFilters();
        }
        
        /// <summary>
        /// Gets the character nodes in a group
        /// </summary>
        /// <param name="groupId">The ID of the group</param>
        /// <returns>List of character nodes in the group</returns>
        public List<CharacterNode> GetGroupMembers(string groupId)
        {
            return _dataProvider?.GetGroupMembers(groupId) ?? new List<CharacterNode>();
        }
        
        /// <summary>
        /// Gets the relationships for a character
        /// </summary>
        /// <param name="characterId">The ID of the character</param>
        /// <returns>List of relationships involving the character</returns>
        public List<RelationshipEdge> GetCharacterRelationships(string characterId)
        {
            return _dataProvider?.GetCharacterRelationships(characterId) ?? new List<RelationshipEdge>();
        }
        
        /// <summary>
        /// Adds a character to a group
        /// </summary>
        /// <param name="characterId">The ID of the character</param>
        /// <param name="groupId">The ID of the group</param>
        public void AddCharacterToGroup(string characterId, string groupId)
        {
            _dataProvider?.AddCharacterToGroup(characterId, groupId);
        }
        
        /// <summary>
        /// Removes a character from a group
        /// </summary>
        /// <param name="characterId">The ID of the character</param>
        /// <param name="groupId">The ID of the group</param>
        public void RemoveCharacterFromGroup(string characterId, string groupId)
        {
            _dataProvider?.RemoveCharacterFromGroup(characterId, groupId);
        }
        
        /// <summary>
        /// Adds or updates a relationship between two characters
        /// </summary>
        /// <param name="relationship">The relationship to add or update</param>
        public void AddOrUpdateRelationship(RelationshipEdge relationship)
        {
            _dataProvider?.AddOrUpdateRelationship(relationship);
        }
        
        /// <summary>
        /// Removes a relationship between two characters
        /// </summary>
        /// <param name="sourceCharacterId">The ID of the source character</param>
        /// <param name="targetCharacterId">The ID of the target character</param>
        public void RemoveRelationship(string sourceCharacterId, string targetCharacterId)
        {
            _dataProvider?.RemoveRelationship(sourceCharacterId, targetCharacterId);
        }
        
        /// <summary>
        /// Gets a character by ID
        /// </summary>
        /// <param name="characterId">The ID of the character</param>
        /// <returns>The character, or null if not found</returns>
        public CharacterNode GetCharacter(string characterId)
        {
            return _dataProvider?.GetCharacter(characterId);
        }
        
        /// <summary>
        /// Cleans up resources when the view model is no longer needed
        /// </summary>
        public void Dispose()
        {
            CompositeDisposable.Dispose();
        }
    }
}