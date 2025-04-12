using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using System.Collections;

namespace InformationManagementUI
{
    /// <summary>
    /// ViewModel for the status management component
    /// </summary>
    public class StatusManagementViewModel
    {
        private readonly ICharacterStatusDataProvider _dataProvider;
        
        // Observable properties
        private readonly ReactiveCollection<CharacterStatus> _characters = new ReactiveCollection<CharacterStatus>();
        private readonly ReactiveProperty<string> _selectedCharacterId = new ReactiveProperty<string>();
        private readonly ReactiveProperty<CharacterStatus> _selectedCharacter = new ReactiveProperty<CharacterStatus>();
        private readonly ReactiveProperty<int> _selectedTabIndex = new ReactiveProperty<int>(0);
        
        // Comparison mode properties
        private readonly ReactiveProperty<bool> _comparisonModeActive = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<string> _comparisonCharacterId = new ReactiveProperty<string>();
        private readonly ReactiveProperty<DateTime> _historicalComparisonDate = new ReactiveProperty<DateTime>();
        
        // Filtered and derived collections
        private readonly ReactiveDictionary<StatType, ReactiveProperty<float>> _statValues = 
            new ReactiveDictionary<StatType, ReactiveProperty<float>>();
        private readonly ReactiveCollection<Skill> _skills = new ReactiveCollection<Skill>();
        private readonly ReactiveCollection<MoodEffect> _moodEffects = new ReactiveCollection<MoodEffect>();
        private readonly ReactiveCollection<ConditionEffect> _conditionEffects = new ReactiveCollection<ConditionEffect>();
        private readonly ReactiveCollection<InventoryItem> _inventoryItems = new ReactiveCollection<InventoryItem>();
        private readonly ReactiveDictionary<EquipSlot, ReactiveProperty<InventoryItem>> _equippedItems = 
            new ReactiveDictionary<EquipSlot, ReactiveProperty<InventoryItem>>();
        private readonly ReactiveDictionary<string, ReactiveProperty<float>> _reputationValues = 
            new ReactiveDictionary<string, ReactiveProperty<float>>();
        private readonly ReactiveCollection<SocialAchievement> _achievements = new ReactiveCollection<SocialAchievement>();
        private readonly ReactiveCollection<Goal> _activeGoals = new ReactiveCollection<Goal>();
        private readonly ReactiveCollection<Goal> _completedGoals = new ReactiveCollection<Goal>();
        
        // Observable commands
        private readonly ReactiveCommand<string> _selectCharacterCommand = new ReactiveCommand<string>();
        private readonly ReactiveCommand<int> _selectTabCommand = new ReactiveCommand<int>();
        private readonly ReactiveCommand<Tuple<StatType, float>> _updateStatCommand = new ReactiveCommand<Tuple<StatType, float>>();
        private readonly ReactiveCommand<Tuple<string, float>> _updateSkillCommand = new ReactiveCommand<Tuple<string, float>>();
        private readonly ReactiveCommand<Unit> _toggleComparisonModeCommand = new ReactiveCommand<Unit>();
        private readonly ReactiveCommand<string> _selectComparisonCharacterCommand = new ReactiveCommand<string>();
        private readonly ReactiveCommand<DateTime> _selectHistoricalDateCommand = new ReactiveCommand<DateTime>();
        private readonly ReactiveCommand<Tuple<string, float>> _updateGoalProgressCommand = new ReactiveCommand<Tuple<string, float>>();
        
        // Public properties
        public IReadOnlyReactiveCollection<CharacterStatus> Characters => _characters;
        public IReactiveProperty<string> SelectedCharacterId => _selectedCharacterId;
        public IReactiveProperty<CharacterStatus> SelectedCharacter => _selectedCharacter;
        public IReactiveProperty<int> SelectedTabIndex => _selectedTabIndex;
        
        public IReactiveProperty<bool> ComparisonModeActive => _comparisonModeActive;
        public IReactiveProperty<string> ComparisonCharacterId => _comparisonCharacterId;
        public IReactiveProperty<DateTime> HistoricalComparisonDate => _historicalComparisonDate;
        
        public IReadOnlyReactiveDictionary<StatType, ReactiveProperty<float>> StatValues => _statValues;
        public IReadOnlyReactiveCollection<Skill> Skills => _skills;
        public IReadOnlyReactiveCollection<MoodEffect> MoodEffects => _moodEffects;
        public IReadOnlyReactiveCollection<ConditionEffect> ConditionEffects => _conditionEffects;
        public IReadOnlyReactiveCollection<InventoryItem> InventoryItems => _inventoryItems;
        public IReadOnlyReactiveDictionary<EquipSlot, ReactiveProperty<InventoryItem>> EquippedItems => _equippedItems;
        public IReadOnlyReactiveDictionary<string, ReactiveProperty<float>> ReputationValues => _reputationValues;
        public IReadOnlyReactiveCollection<SocialAchievement> Achievements => _achievements;
        public IReadOnlyReactiveCollection<Goal> ActiveGoals => _activeGoals;
        public IReadOnlyReactiveCollection<Goal> CompletedGoals => _completedGoals;
        
        // Computed properties
        private readonly ReactiveProperty<float> _stressLevel = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _happinessLevel = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _motivationLevel = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _staminaLevel = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _energyLevel = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _healthLevel = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _overallFame = new ReactiveProperty<float>();
        
        public IReactiveProperty<float> StressLevel => _stressLevel;
        public IReactiveProperty<float> HappinessLevel => _happinessLevel;
        public IReactiveProperty<float> MotivationLevel => _motivationLevel;
        public IReactiveProperty<float> StaminaLevel => _staminaLevel;
        public IReactiveProperty<float> EnergyLevel => _energyLevel;
        public IReactiveProperty<float> HealthLevel => _healthLevel;
        public IReactiveProperty<float> OverallFame => _overallFame;
        
        // Commands
        public IReactiveCommand<string> SelectCharacterCommand => _selectCharacterCommand;
        public IReactiveCommand<int> SelectTabCommand => _selectTabCommand;
        public IReactiveCommand<Tuple<StatType, float>> UpdateStatCommand => _updateStatCommand;
        public IReactiveCommand<Tuple<string, float>> UpdateSkillCommand => _updateSkillCommand;
        public IReactiveCommand<Unit> ToggleComparisonModeCommand => _toggleComparisonModeCommand;
        public IReactiveCommand<string> SelectComparisonCharacterCommand => _selectComparisonCharacterCommand;
        public IReactiveCommand<DateTime> SelectHistoricalDateCommand => _selectHistoricalDateCommand;
        public IReactiveCommand<Tuple<string, float>> UpdateGoalProgressCommand => _updateGoalProgressCommand;
        
        // CompositeDisposable for cleanup
        private CompositeDisposable CompositeDisposable { get; } = new CompositeDisposable();
        
        public StatusManagementViewModel(ICharacterStatusDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            
            // Setup command handlers
            SetupCommands();
            
            // Setup data listeners
            SetupDataListeners();
            
            // Initial data load
            RefreshData();
        }
        
        private void SetupCommands()
        {
            _selectCharacterCommand.Subscribe(id => SetSelectedCharacter(id)).AddTo(CompositeDisposable);
            _selectTabCommand.Subscribe(index => _selectedTabIndex.Value = index).AddTo(CompositeDisposable);
            _updateStatCommand.Subscribe(tuple => UpdateStat(tuple.Item1, tuple.Item2)).AddTo(CompositeDisposable);
            _updateSkillCommand.Subscribe(tuple => UpdateSkill(tuple.Item1, tuple.Item2)).AddTo(CompositeDisposable);
            _toggleComparisonModeCommand.Subscribe(_ => ToggleComparisonMode()).AddTo(CompositeDisposable);
            _selectComparisonCharacterCommand.Subscribe(id => SelectComparisonCharacter(id)).AddTo(CompositeDisposable);
            _selectHistoricalDateCommand.Subscribe(date => SelectHistoricalDate(date)).AddTo(CompositeDisposable);
            _updateGoalProgressCommand.Subscribe(tuple => UpdateGoalProgress(tuple.Item1, tuple.Item2)).AddTo(CompositeDisposable);
        }
        
        private void SetupDataListeners()
        {
            if (_dataProvider != null)
            {
                // Subscribe to data provider events
                _dataProvider.OnCharacterStatusChanged += id => 
                {
                    if (id == _selectedCharacterId.Value)
                    {
                        RefreshSelectedCharacter();
                    }
                    RefreshCharacters();
                };
                
                _dataProvider.OnCharacterAdded += _ => RefreshCharacters();
                _dataProvider.OnCharacterRemoved += _ => RefreshCharacters();
            }
            
            // When selected character changes, update derived collections
            _selectedCharacter.Subscribe(_ => RefreshDerivedCollections()).AddTo(CompositeDisposable);
        }
        
        /// <summary>
        /// Refreshes all character data from the data provider
        /// </summary>
        public void RefreshData()
        {
            RefreshCharacters();
            
            // If a character was previously selected, refresh it
            if (!string.IsNullOrEmpty(_selectedCharacterId.Value))
            {
                RefreshSelectedCharacter();
            }
            // Otherwise select the first character if available
            else if (_characters.Count > 0)
            {
                SetSelectedCharacter(_characters[0].CharacterID);
            }
        }
        
        /// <summary>
        /// Refreshes the characters collection from the data provider
        /// </summary>
        private void RefreshCharacters()
        {
            if (_dataProvider == null) return;
            
            _characters.Clear();
            foreach (var character in _dataProvider.GetAllCharacters())
            {
                _characters.Add(character);
            }
            
            // If selected character was removed, clear the selection
            if (!string.IsNullOrEmpty(_selectedCharacterId.Value) && 
                !_characters.Any(c => c.CharacterID == _selectedCharacterId.Value))
            {
                _selectedCharacterId.Value = string.Empty;
                _selectedCharacter.Value = null;
            }
        }
        
        /// <summary>
        /// Refreshes the selected character from the data provider
        /// </summary>
        private void RefreshSelectedCharacter()
        {
            if (_dataProvider == null || string.IsNullOrEmpty(_selectedCharacterId.Value)) return;
            
            var character = _dataProvider.GetCharacter(_selectedCharacterId.Value);
            _selectedCharacter.Value = character;
        }
        
        /// <summary>
        /// Refreshes all derived collections based on the selected character
        /// </summary>
        private void RefreshDerivedCollections()
        {
            if (_selectedCharacter.Value == null) return;
            
            RefreshStats();
            RefreshSkills();
            RefreshMoodEffects();
            RefreshConditionEffects();
            RefreshInventory();
            RefreshEquippedItems();
            RefreshReputation();
            RefreshAchievements();
            RefreshGoals();
            RefreshComputedProperties();
        }
        
        /// <summary>
        /// Refreshes the stats dictionary from the selected character
        /// </summary>
        private void RefreshStats()
        {
            _statValues.Clear();
            
            if (_selectedCharacter.Value == null) return;
            
            // Add all stats from the character
            foreach (var pair in _selectedCharacter.Value.Stats)
            {
                _statValues.Add(pair.Key, new ReactiveProperty<float>(pair.Value));
            }
            
            // Ensure all standard stats have a value
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                if (statType != StatType.Custom && !_statValues.ContainsKey(statType))
                {
                    _statValues.Add(statType, new ReactiveProperty<float>(0f));
                }
            }
        }
        
        /// <summary>
        /// Refreshes the skills collection from the selected character
        /// </summary>
        private void RefreshSkills()
        {
            _skills.Clear();
            
            if (_selectedCharacter.Value == null) return;
            
            foreach (var skill in _selectedCharacter.Value.Skills)
            {
                _skills.Add(skill);
            }
        }
        
        /// <summary>
        /// Refreshes the mood effects collection from the selected character
        /// </summary>
        private void RefreshMoodEffects()
        {
            _moodEffects.Clear();
            
            if (_selectedCharacter.Value == null || _selectedCharacter.Value.MentalState == null) return;
            
            foreach (var effect in _selectedCharacter.Value.MentalState.ActiveEffects)
            {
                _moodEffects.Add(effect);
            }
        }
        
        /// <summary>
        /// Refreshes the condition effects collection from the selected character
        /// </summary>
        private void RefreshConditionEffects()
        {
            _conditionEffects.Clear();
            
            if (_selectedCharacter.Value == null || _selectedCharacter.Value.PhysicalCondition == null) return;
            
            foreach (var effect in _selectedCharacter.Value.PhysicalCondition.ActiveConditions)
            {
                _conditionEffects.Add(effect);
            }
        }
        
        /// <summary>
        /// Refreshes the inventory items collection from the selected character
        /// </summary>
        private void RefreshInventory()
        {
            _inventoryItems.Clear();
            
            if (_selectedCharacter.Value == null || _selectedCharacter.Value.Inventory == null) return;
            
            foreach (var item in _selectedCharacter.Value.Inventory.Items)
            {
                _inventoryItems.Add(item);
            }
        }
        
        /// <summary>
        /// Refreshes the equipped items dictionary from the selected character
        /// </summary>
        private void RefreshEquippedItems()
        {
            _equippedItems.Clear();
            
            if (_selectedCharacter.Value == null || _selectedCharacter.Value.Inventory == null) return;
            
            foreach (var pair in _selectedCharacter.Value.Inventory.EquippedItems)
            {
                _equippedItems.Add(pair.Key, new ReactiveProperty<InventoryItem>(pair.Value));
            }
            
            // Ensure all standard equipment slots have a value
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                if (slot != EquipSlot.Custom && !_equippedItems.ContainsKey(slot))
                {
                    _equippedItems.Add(slot, new ReactiveProperty<InventoryItem>(null));
                }
            }
        }
        
        /// <summary>
        /// Refreshes the reputation values dictionary from the selected character
        /// </summary>
        private void RefreshReputation()
        {
            _reputationValues.Clear();
            
            if (_selectedCharacter.Value == null || _selectedCharacter.Value.SocialStatus == null) return;
            
            foreach (var pair in _selectedCharacter.Value.SocialStatus.Reputation)
            {
                _reputationValues.Add(pair.Key, new ReactiveProperty<float>(pair.Value));
            }
        }
        
        /// <summary>
        /// Refreshes the achievements collection from the selected character
        /// </summary>
        private void RefreshAchievements()
        {
            _achievements.Clear();
            
            if (_selectedCharacter.Value == null || _selectedCharacter.Value.SocialStatus == null) return;
            
            foreach (var achievement in _selectedCharacter.Value.SocialStatus.Achievements)
            {
                _achievements.Add(achievement);
            }
        }
        
        /// <summary>
        /// Refreshes the goals collections from the selected character
        /// </summary>
        private void RefreshGoals()
        {
            _activeGoals.Clear();
            _completedGoals.Clear();
            
            if (_selectedCharacter.Value == null) return;
            
            foreach (var goal in _selectedCharacter.Value.GetActiveGoals())
            {
                _activeGoals.Add(goal);
            }
            
            foreach (var goal in _selectedCharacter.Value.GetCompletedGoals())
            {
                _completedGoals.Add(goal);
            }
        }
        
        /// <summary>
        /// Refreshes the computed properties from the selected character
        /// </summary>
        private void RefreshComputedProperties()
        {
            if (_selectedCharacter.Value == null) return;
            
            // Mental state properties
            if (_selectedCharacter.Value.MentalState != null)
            {
                _stressLevel.Value = _selectedCharacter.Value.MentalState.Stress;
                _happinessLevel.Value = _selectedCharacter.Value.MentalState.Happiness;
                _motivationLevel.Value = _selectedCharacter.Value.MentalState.Motivation;
            }
            else
            {
                _stressLevel.Value = 0f;
                _happinessLevel.Value = 0f;
                _motivationLevel.Value = 0f;
            }
            
            // Physical condition properties
            if (_selectedCharacter.Value.PhysicalCondition != null)
            {
                _staminaLevel.Value = _selectedCharacter.Value.PhysicalCondition.Stamina;
                _energyLevel.Value = _selectedCharacter.Value.PhysicalCondition.Energy;
                _healthLevel.Value = _selectedCharacter.Value.PhysicalCondition.Health;
            }
            else
            {
                _staminaLevel.Value = 0f;
                _energyLevel.Value = 0f;
                _healthLevel.Value = 0f;
            }
            
            // Social status properties
            if (_selectedCharacter.Value.SocialStatus != null)
            {
                _overallFame.Value = _selectedCharacter.Value.SocialStatus.OverallFame;
            }
            else
            {
                _overallFame.Value = 0f;
            }
        }
        
        /// <summary>
        /// Sets the selected character by ID
        /// </summary>
        /// <param name="characterId">The ID of the character to select</param>
        public void SetSelectedCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                _selectedCharacterId.Value = string.Empty;
                _selectedCharacter.Value = null;
                return;
            }
            
            _selectedCharacterId.Value = characterId;
            RefreshSelectedCharacter();
        }
        
        /// <summary>
        /// Updates a stat value for the selected character
        /// </summary>
        /// <param name="statType">The type of stat to update</param>
        /// <param name="value">The new value</param>
        public void UpdateStat(StatType statType, float value)
        {
            if (_dataProvider == null || string.IsNullOrEmpty(_selectedCharacterId.Value)) return;
            
            _dataProvider.UpdateStat(_selectedCharacterId.Value, statType, value);
        }
        
        /// <summary>
        /// Updates a skill level for the selected character
        /// </summary>
        /// <param name="skillId">The ID of the skill to update</param>
        /// <param name="level">The new skill level</param>
        public void UpdateSkill(string skillId, float level)
        {
            if (_dataProvider == null || string.IsNullOrEmpty(_selectedCharacterId.Value)) return;
            
            _dataProvider.UpdateSkill(_selectedCharacterId.Value, skillId, level);
        }
        
        /// <summary>
        /// Updates goal progress for the selected character
        /// </summary>
        /// <param name="goalId">The ID of the goal to update</param>
        /// <param name="progress">The new progress value</param>
        public void UpdateGoalProgress(string goalId, float progress)
        {
            if (_dataProvider == null || string.IsNullOrEmpty(_selectedCharacterId.Value)) return;
            
            _dataProvider.UpdateGoalProgress(_selectedCharacterId.Value, goalId, progress);
        }
        
        /// <summary>
        /// Toggles comparison mode
        /// </summary>
        public void ToggleComparisonMode()
        {
            _comparisonModeActive.Value = !_comparisonModeActive.Value;
            
            // Clear comparison selection when disabling comparison mode
            if (!_comparisonModeActive.Value)
            {
                _comparisonCharacterId.Value = string.Empty;
                _historicalComparisonDate.Value = default;
            }
        }
        
        /// <summary>
        /// Selects a character for comparison
        /// </summary>
        /// <param name="characterId">The ID of the character to compare with</param>
        public void SelectComparisonCharacter(string characterId)
        {
            _comparisonCharacterId.Value = characterId;
            
            // Turn on comparison mode if it's not already on
            if (!_comparisonModeActive.Value)
            {
                _comparisonModeActive.Value = true;
            }
            
            // Clear historical date since we're comparing with another character
            _historicalComparisonDate.Value = default;
        }
        
        /// <summary>
        /// Selects a historical date for comparison
        /// </summary>
        /// <param name="date">The historical date to compare with</param>
        public void SelectHistoricalDate(DateTime date)
        {
            _historicalComparisonDate.Value = date;
            
            // Turn on comparison mode if it's not already on
            if (!_comparisonModeActive.Value)
            {
                _comparisonModeActive.Value = true;
            }
            
            // Clear comparison character since we're comparing with historical data
            _comparisonCharacterId.Value = string.Empty;
        }
        
        /// <summary>
        /// Records a snapshot of the current character's status
        /// </summary>
        public void RecordStatusSnapshot()
        {
            if (_dataProvider == null || string.IsNullOrEmpty(_selectedCharacterId.Value)) return;
            
            _dataProvider.RecordStatusSnapshot(_selectedCharacterId.Value);
        }
        
        /// <summary>
        /// Gets a list of dates for which historical snapshots exist
        /// </summary>
        /// <returns>List of dates with snapshots</returns>
        public List<DateTime> GetHistoricalSnapshotDates()
        {
            if (_dataProvider == null || string.IsNullOrEmpty(_selectedCharacterId.Value)) 
                return new List<DateTime>();
            
            var character = _dataProvider.GetCharacter(_selectedCharacterId.Value);
            if (character == null || character.History == null || character.History.Snapshots == null)
                return new List<DateTime>();
            
            return character.History.Snapshots
                .Select(s => s.Timestamp.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();
        }
        
        /// <summary>
        /// Gets a historical snapshot for a specific date
        /// </summary>
        /// <param name="date">The date to get the snapshot for</param>
        /// <returns>The status snapshot, or null if not found</returns>
        public StatusSnapshot GetHistoricalSnapshot(DateTime date)
        {
            if (_dataProvider == null || string.IsNullOrEmpty(_selectedCharacterId.Value)) return null;
            
            var character = _dataProvider.GetCharacter(_selectedCharacterId.Value);
            if (character == null || character.History == null || character.History.Snapshots == null)
                return null;
            
            // Find the snapshot closest to the specified date
            var snapshotsOnDate = character.History.GetSnapshotsInRange(
                date.Date, 
                date.Date.AddDays(1).AddSeconds(-1)
            );
            
            if (snapshotsOnDate.Count == 0) return null;
            
            // Get the most recent snapshot for that date
            return snapshotsOnDate
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Gets comparison data for the currently selected comparison mode
        /// </summary>
        /// <returns>A comparison data structure with the differences</returns>
        public ComparisonData GetComparisonData()
        {
            if (!_comparisonModeActive.Value || _selectedCharacter.Value == null) 
                return null;
            
            // If comparing with another character
            if (!string.IsNullOrEmpty(_comparisonCharacterId.Value))
            {
                var comparisonCharacter = _dataProvider?.GetCharacter(_comparisonCharacterId.Value);
                if (comparisonCharacter == null) return null;
                
                return GenerateComparisonData(_selectedCharacter.Value, comparisonCharacter);
            }
            
            // If comparing with historical data
            if (_historicalComparisonDate.Value != default)
            {
                var snapshot = GetHistoricalSnapshot(_historicalComparisonDate.Value);
                if (snapshot == null) return null;
                
                return GenerateHistoricalComparisonData(_selectedCharacter.Value, snapshot);
            }
            
            return null;
        }
        
        /// <summary>
        /// Generates comparison data between two characters
        /// </summary>
        /// <param name="baseCharacter">The base character</param>
        /// <param name="comparisonCharacter">The character to compare with</param>
        /// <returns>A comparison data structure with the differences</returns>
        private ComparisonData GenerateComparisonData(CharacterStatus baseCharacter, CharacterStatus comparisonCharacter)
        {
            var result = new ComparisonData
            {
                ComparisonType = ComparisonType.Character,
                ComparisonName = comparisonCharacter.BasicInfo?.Name ?? "Unknown Character",
                StatDifferences = new Dictionary<StatType, float>(),
                SkillDifferences = new Dictionary<string, float>(),
                MentalStateDifferences = new Dictionary<string, float>(),
                PhysicalConditionDifferences = new Dictionary<string, float>()
            };
            
            // Compare stats
            foreach (var pair in baseCharacter.Stats)
            {
                float baseValue = pair.Value;
                float comparisonValue = comparisonCharacter.GetStatValue(pair.Key);
                
                result.StatDifferences[pair.Key] = comparisonValue - baseValue;
            }
            
            // Compare skills
            foreach (var skill in baseCharacter.Skills)
            {
                float baseValue = skill.Level;
                float comparisonValue = 0f;
                
                var comparisonSkill = comparisonCharacter.GetSkill(skill.SkillID);
                if (comparisonSkill != null)
                {
                    comparisonValue = comparisonSkill.Level;
                }
                
                result.SkillDifferences[skill.Name] = comparisonValue - baseValue;
            }
            
            // Compare mental state
            if (baseCharacter.MentalState != null && comparisonCharacter.MentalState != null)
            {
                result.MentalStateDifferences["Stress"] = 
                    comparisonCharacter.MentalState.Stress - baseCharacter.MentalState.Stress;
                result.MentalStateDifferences["Happiness"] = 
                    comparisonCharacter.MentalState.Happiness - baseCharacter.MentalState.Happiness;
                result.MentalStateDifferences["Motivation"] = 
                    comparisonCharacter.MentalState.Motivation - baseCharacter.MentalState.Motivation;
                
                foreach (var pair in baseCharacter.MentalState.Emotions)
                {
                    float baseValue = pair.Value;
                    float comparisonValue = comparisonCharacter.GetEmotionValue(pair.Key);
                    
                    result.MentalStateDifferences[$"Emotion:{pair.Key}"] = comparisonValue - baseValue;
                }
            }
            
            // Compare physical condition
            if (baseCharacter.PhysicalCondition != null && comparisonCharacter.PhysicalCondition != null)
            {
                result.PhysicalConditionDifferences["Stamina"] = 
                    comparisonCharacter.PhysicalCondition.Stamina - baseCharacter.PhysicalCondition.Stamina;
                result.PhysicalConditionDifferences["Energy"] = 
                    comparisonCharacter.PhysicalCondition.Energy - baseCharacter.PhysicalCondition.Energy;
                result.PhysicalConditionDifferences["Health"] = 
                    comparisonCharacter.PhysicalCondition.Health - baseCharacter.PhysicalCondition.Health;
            }
            
            return result;
        }
        
        /// <summary>
        /// Generates comparison data between current character and a historical snapshot
        /// </summary>
        /// <param name="currentCharacter">The current character data</param>
        /// <param name="historicalSnapshot">The historical snapshot to compare with</param>
        /// <returns>A comparison data structure with the differences</returns>
        private ComparisonData GenerateHistoricalComparisonData(CharacterStatus currentCharacter, StatusSnapshot historicalSnapshot)
        {
            var result = new ComparisonData
            {
                ComparisonType = ComparisonType.Historical,
                ComparisonName = $"Historical Data ({historicalSnapshot.Timestamp:d})",
                StatDifferences = new Dictionary<StatType, float>(),
                SkillDifferences = new Dictionary<string, float>(),
                MentalStateDifferences = new Dictionary<string, float>(),
                PhysicalConditionDifferences = new Dictionary<string, float>()
            };
            
            // Compare stats
            foreach (var pair in currentCharacter.Stats)
            {
                float currentValue = pair.Value;
                float historicalValue = 0f;
                
                if (historicalSnapshot.Stats.TryGetValue(pair.Key, out float value))
                {
                    historicalValue = value;
                }
                
                result.StatDifferences[pair.Key] = currentValue - historicalValue;
            }
            
            // Compare skills
            foreach (var skill in currentCharacter.Skills)
            {
                float currentValue = skill.Level;
                float historicalValue = 0f;
                
                var historicalSkill = historicalSnapshot.Skills.FirstOrDefault(s => s.SkillID == skill.SkillID);
                if (historicalSkill != null)
                {
                    historicalValue = historicalSkill.Level;
                }
                
                result.SkillDifferences[skill.Name] = currentValue - historicalValue;
            }
            
            // Compare mental state
            if (currentCharacter.MentalState != null && historicalSnapshot.MentalState != null)
            {
                result.MentalStateDifferences["Stress"] = 
                    currentCharacter.MentalState.Stress - historicalSnapshot.MentalState.Stress;
                result.MentalStateDifferences["Happiness"] = 
                    currentCharacter.MentalState.Happiness - historicalSnapshot.MentalState.Happiness;
                result.MentalStateDifferences["Motivation"] = 
                    currentCharacter.MentalState.Motivation - historicalSnapshot.MentalState.Motivation;
                
                foreach (var pair in currentCharacter.MentalState.Emotions)
                {
                    float currentValue = pair.Value;
                    float historicalValue = 0f;
                    
                    if (historicalSnapshot.MentalState.Emotions.TryGetValue(pair.Key, out float value))
                    {
                        historicalValue = value;
                    }
                    
                    result.MentalStateDifferences[$"Emotion:{pair.Key}"] = currentValue - historicalValue;
                }
            }
            
            // Compare physical condition
            if (currentCharacter.PhysicalCondition != null && historicalSnapshot.PhysicalCondition != null)
            {
                result.PhysicalConditionDifferences["Stamina"] = 
                    currentCharacter.PhysicalCondition.Stamina - historicalSnapshot.PhysicalCondition.Stamina;
                result.PhysicalConditionDifferences["Energy"] = 
                    currentCharacter.PhysicalCondition.Energy - historicalSnapshot.PhysicalCondition.Energy;
                result.PhysicalConditionDifferences["Health"] = 
                    currentCharacter.PhysicalCondition.Health - historicalSnapshot.PhysicalCondition.Health;
            }
            
            return result;
        }
        
        /// <summary>
        /// Cleans up resources when the view model is no longer needed
        /// </summary>
        public void Dispose()
        {
            CompositeDisposable.Dispose();
        }
    }
    
    /// <summary>
    /// Data structure for comparison results
    /// </summary>
    public class ComparisonData
    {
        public ComparisonType ComparisonType { get; set; }
        public string ComparisonName { get; set; }
        public Dictionary<StatType, float> StatDifferences { get; set; }
        public Dictionary<string, float> SkillDifferences { get; set; }
        public Dictionary<string, float> MentalStateDifferences { get; set; }
        public Dictionary<string, float> PhysicalConditionDifferences { get; set; }
    }
    
    /// <summary>
    /// Enum representing the type of comparison
    /// </summary>
    public enum ComparisonType
    {
        Character,
        Historical
    }
}