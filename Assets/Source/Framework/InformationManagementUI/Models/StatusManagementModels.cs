using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace InformationManagementUI
{
    /// <summary>
    /// Data structure for character status data
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterStatusData", menuName = "Project_001/Information UI/Character Status Data")]
    public class CharacterStatusData : ScriptableObject, ICharacterStatusDataProvider
    {
        [SerializeField] private List<CharacterStatus> _characterStatuses = new List<CharacterStatus>();
        private Dictionary<string, CharacterStatus> _characterLookup = new Dictionary<string, CharacterStatus>();
        
        public IReadOnlyList<CharacterStatus> Characters => _characterStatuses;
        
        public event Action<string> OnCharacterStatusChanged;
        public event Action<string> OnCharacterAdded;
        public event Action<string> OnCharacterRemoved;
        
        private void OnEnable()
        {
            RebuildLookup();
        }
        
        private void RebuildLookup()
        {
            _characterLookup.Clear();
            foreach (var character in _characterStatuses)
            {
                _characterLookup[character.CharacterID] = character;
            }
        }
        
        public void AddCharacter(CharacterStatus character)
        {
            if (!_characterLookup.ContainsKey(character.CharacterID))
            {
                _characterStatuses.Add(character);
                _characterLookup[character.CharacterID] = character;
                OnCharacterAdded?.Invoke(character.CharacterID);
            }
        }
        
        public void UpdateCharacter(CharacterStatus character)
        {
            if (_characterLookup.ContainsKey(character.CharacterID))
            {
                int index = _characterStatuses.FindIndex(c => c.CharacterID == character.CharacterID);
                if (index >= 0)
                {
                    _characterStatuses[index] = character;
                    _characterLookup[character.CharacterID] = character;
                    OnCharacterStatusChanged?.Invoke(character.CharacterID);
                }
            }
            else
            {
                AddCharacter(character);
            }
        }
        
        public void RemoveCharacter(string characterID)
        {
            if (_characterLookup.ContainsKey(characterID))
            {
                _characterStatuses.RemoveAll(c => c.CharacterID == characterID);
                _characterLookup.Remove(characterID);
                OnCharacterRemoved?.Invoke(characterID);
            }
        }
        
        public CharacterStatus GetCharacter(string characterID)
        {
            return _characterLookup.TryGetValue(characterID, out var character) ? character : null;
        }
        
        public List<CharacterStatus> GetAllCharacters()
        {
            return new List<CharacterStatus>(_characterStatuses);
        }
        
        public void UpdateStat(string characterID, StatType statType, float value)
        {
            if (_characterLookup.TryGetValue(characterID, out var character))
            {
                var updatedStats = new Dictionary<StatType, float>(character.Stats);
                updatedStats[statType] = Mathf.Clamp01(value);
                
                var updatedCharacter = new CharacterStatus(
                    character.CharacterID,
                    character.BasicInfo,
                    updatedStats,
                    new List<Skill>(character.Skills),
                    character.MentalState,
                    character.PhysicalCondition,
                    character.Inventory,
                    character.SocialStatus,
                    new List<Goal>(character.Goals),
                    character.History
                );
                
                UpdateCharacter(updatedCharacter);
            }
        }
        
        public void UpdateSkill(string characterID, string skillID, float level, float experience = 0, float nextLevelThreshold = 1)
        {
            if (_characterLookup.TryGetValue(characterID, out var character))
            {
                var updatedSkills = new List<Skill>(character.Skills);
                int index = updatedSkills.FindIndex(s => s.SkillID == skillID);
                
                if (index >= 0)
                {
                    updatedSkills[index] = new Skill(
                        skillID,
                        updatedSkills[index].Name,
                        Mathf.Clamp01(level),
                        experience,
                        nextLevelThreshold
                    );
                }
                else
                {
                    updatedSkills.Add(new Skill(
                        skillID,
                        skillID, // Default name to skillID if not found
                        Mathf.Clamp01(level),
                        experience,
                        nextLevelThreshold
                    ));
                }
                
                var updatedCharacter = new CharacterStatus(
                    character.CharacterID,
                    character.BasicInfo,
                    new Dictionary<StatType, float>(character.Stats),
                    updatedSkills,
                    character.MentalState,
                    character.PhysicalCondition,
                    character.Inventory,
                    character.SocialStatus,
                    new List<Goal>(character.Goals),
                    character.History
                );
                
                UpdateCharacter(updatedCharacter);
            }
        }
        
        public void UpdateMentalState(string characterID, MentalState mentalState)
        {
            if (_characterLookup.TryGetValue(characterID, out var character))
            {
                var updatedCharacter = new CharacterStatus(
                    character.CharacterID,
                    character.BasicInfo,
                    new Dictionary<StatType, float>(character.Stats),
                    new List<Skill>(character.Skills),
                    mentalState,
                    character.PhysicalCondition,
                    character.Inventory,
                    character.SocialStatus,
                    new List<Goal>(character.Goals),
                    character.History
                );
                
                UpdateCharacter(updatedCharacter);
            }
        }
        
        public void UpdatePhysicalCondition(string characterID, PhysicalCondition physicalCondition)
        {
            if (_characterLookup.TryGetValue(characterID, out var character))
            {
                var updatedCharacter = new CharacterStatus(
                    character.CharacterID,
                    character.BasicInfo,
                    new Dictionary<StatType, float>(character.Stats),
                    new List<Skill>(character.Skills),
                    character.MentalState,
                    physicalCondition,
                    character.Inventory,
                    character.SocialStatus,
                    new List<Goal>(character.Goals),
                    character.History
                );
                
                UpdateCharacter(updatedCharacter);
            }
        }
        
        public void UpdateSocialStatus(string characterID, SocialStatus socialStatus)
        {
            if (_characterLookup.TryGetValue(characterID, out var character))
            {
                var updatedCharacter = new CharacterStatus(
                    character.CharacterID,
                    character.BasicInfo,
                    new Dictionary<StatType, float>(character.Stats),
                    new List<Skill>(character.Skills),
                    character.MentalState,
                    character.PhysicalCondition,
                    character.Inventory,
                    socialStatus,
                    new List<Goal>(character.Goals),
                    character.History
                );
                
                UpdateCharacter(updatedCharacter);
            }
        }
        
        public void UpdateGoalProgress(string characterID, string goalID, float progress)
        {
            if (_characterLookup.TryGetValue(characterID, out var character))
            {
                var updatedGoals = new List<Goal>(character.Goals);
                int index = updatedGoals.FindIndex(g => g.GoalID == goalID);
                
                if (index >= 0)
                {
                    var currentGoal = updatedGoals[index];
                    List<SubGoal> subGoal = new List<SubGoal>(currentGoal.SubGoals);
                    updatedGoals[index] = new Goal(
                        currentGoal.GoalID,
                        currentGoal.Description,
                        Mathf.Clamp01(progress),
                        currentGoal.Deadline,
                        subGoal,
                        progress >= 1.0f ? GoalStatus.Completed : currentGoal.Status
                    );
                }
                
                var updatedCharacter = new CharacterStatus(
                    character.CharacterID,
                    character.BasicInfo,
                    new Dictionary<StatType, float>(character.Stats),
                    new List<Skill>(character.Skills),
                    character.MentalState,
                    character.PhysicalCondition,
                    character.Inventory,
                    character.SocialStatus,
                    updatedGoals,
                    character.History
                );
                
                UpdateCharacter(updatedCharacter);
            }
        }
        
        public void AddItemToInventory(string characterID, InventoryItem item)
        {
            if (_characterLookup.TryGetValue(characterID, out var character))
            {
                Dictionary<EquipSlot, InventoryItem> equippedItems = new Dictionary<EquipSlot, InventoryItem>(character.Inventory.EquippedItems);
                Dictionary<ResourceType, float> resources = new Dictionary<ResourceType, float>(character.Inventory.Resources);
                var updatedInventory = new Inventory(
                    new List<InventoryItem>(character.Inventory.Items) { item },
                    equippedItems,
                    resources
                );
                
                var updatedCharacter = new CharacterStatus(
                    character.CharacterID,
                    character.BasicInfo,
                    new Dictionary<StatType, float>(character.Stats),
                    new List<Skill>(character.Skills),
                    character.MentalState,
                    character.PhysicalCondition,
                    updatedInventory,
                    character.SocialStatus,
                    new List<Goal>(character.Goals),
                    character.History
                );
                
                UpdateCharacter(updatedCharacter);
            }
        }
        
        public void RecordStatusSnapshot(string characterID)
        {
            if (_characterLookup.TryGetValue(characterID, out var character))
            {
                var newSkills = character.Skills
                .Select(s => new Skill(s.SkillID, s.Name, s.Level, s.Experience, s.NextLevelThreshold))
                .ToList();
                var currentSnapshot = new StatusSnapshot(
                    DateTime.Now,
                    new Dictionary<StatType, float>(character.Stats),
                    newSkills,
                    new MentalState(character.MentalState),
                    new PhysicalCondition(character.PhysicalCondition)
                );
                
                var updatedHistory = new StatusHistory(
                    new List<StatusSnapshot>(character.History.Snapshots) { currentSnapshot }
                );
                
                var updatedCharacter = new CharacterStatus(
                    character.CharacterID,
                    character.BasicInfo,
                    new Dictionary<StatType, float>(character.Stats),
                    new List<Skill>(character.Skills),
                    character.MentalState,
                    character.PhysicalCondition,
                    character.Inventory,
                    character.SocialStatus,
                    new List<Goal>(character.Goals),
                    updatedHistory
                );
                
                UpdateCharacter(updatedCharacter);
            }
        }
    }
    
    /// <summary>
    /// Interface for character status data providers
    /// </summary>
    public interface ICharacterStatusDataProvider
    {
        IReadOnlyList<CharacterStatus> Characters { get; }
        
        event Action<string> OnCharacterStatusChanged;
        event Action<string> OnCharacterAdded;
        event Action<string> OnCharacterRemoved;
        
        void AddCharacter(CharacterStatus character);
        void UpdateCharacter(CharacterStatus character);
        void RemoveCharacter(string characterID);
        CharacterStatus GetCharacter(string characterID);
        List<CharacterStatus> GetAllCharacters();
        void UpdateStat(string characterID, StatType statType, float value);
        void UpdateSkill(string characterID, string skillID, float level, float experience = 0, float nextLevelThreshold = 1);
        void UpdateMentalState(string characterID, MentalState mentalState);
        void UpdatePhysicalCondition(string characterID, PhysicalCondition physicalCondition);
        void UpdateSocialStatus(string characterID, SocialStatus socialStatus);
        void UpdateGoalProgress(string characterID, string goalID, float progress);
        void AddItemToInventory(string characterID, InventoryItem item);
        void RecordStatusSnapshot(string characterID);
    }
    
    /// <summary>
    /// Complete character status data structure
    /// </summary>
    [Serializable]
    public class CharacterStatus
    {
        [SerializeField] private string _characterID;
        [SerializeField] private BasicInfo _basicInfo;
        [SerializeField] private Dictionary<StatType, float> _stats = new Dictionary<StatType, float>();
        [SerializeField] private List<Skill> _skills = new List<Skill>();
        [SerializeField] private MentalState _mentalState;
        [SerializeField] private PhysicalCondition _physicalCondition;
        [SerializeField] private Inventory _inventory;
        [SerializeField] private SocialStatus _socialStatus;
        [SerializeField] private List<Goal> _goals = new List<Goal>();
        [SerializeField] private StatusHistory _history;
        
        public string CharacterID => _characterID;
        public BasicInfo BasicInfo => _basicInfo;
        public IReadOnlyDictionary<StatType, float> Stats => _stats;
        public IReadOnlyList<Skill> Skills => _skills;
        public MentalState MentalState => _mentalState;
        public PhysicalCondition PhysicalCondition => _physicalCondition;
        public Inventory Inventory => _inventory;
        public SocialStatus SocialStatus => _socialStatus;
        public IReadOnlyList<Goal> Goals => _goals;
        public StatusHistory History => _history;
        
        public CharacterStatus(
            string characterID,
            BasicInfo basicInfo,
            Dictionary<StatType, float> stats,
            List<Skill> skills,
            MentalState mentalState,
            PhysicalCondition physicalCondition,
            Inventory inventory,
            SocialStatus socialStatus,
            List<Goal> goals,
            StatusHistory history)
        {
            _characterID = characterID;
            _basicInfo = basicInfo;
            _stats = stats ?? new Dictionary<StatType, float>();
            _skills = skills ?? new List<Skill>();
            _mentalState = mentalState ?? new MentalState();
            _physicalCondition = physicalCondition ?? new PhysicalCondition();
            _inventory = inventory ?? new Inventory();
            _socialStatus = socialStatus ?? new SocialStatus();
            _goals = goals ?? new List<Goal>();
            _history = history ?? new StatusHistory();
        }
        
        public float GetStatValue(StatType statType, float defaultValue = 0)
        {
            return _stats.TryGetValue(statType, out var value) ? value : defaultValue;
        }
        
        public Skill GetSkill(string skillID)
        {
            return _skills.Find(s => s.SkillID == skillID);
        }
        
        public float GetEmotionValue(EmotionType emotionType, float defaultValue = 0)
        {
            return _mentalState.GetEmotionValue(emotionType, defaultValue);
        }
        
        public List<Goal> GetActiveGoals()
        {
            return _goals.FindAll(g => g.Status == GoalStatus.Active || g.Status == GoalStatus.InProgress);
        }
        
        public List<Goal> GetCompletedGoals()
        {
            return _goals.FindAll(g => g.Status == GoalStatus.Completed);
        }
    }
    
    /// <summary>
    /// Basic character information
    /// </summary>
    [Serializable]
    public class BasicInfo
    {
        [SerializeField] private string _name;
        [SerializeField] private int _age;
        [SerializeField] private string _occupation;
        [SerializeField] private string _background;
        [SerializeField] private Sprite _portrait;
        
        public string Name => _name;
        public int Age => _age;
        public string Occupation => _occupation;
        public string Background => _background;
        public Sprite Portrait => _portrait;
        
        public BasicInfo(
            string name,
            int age,
            string occupation,
            string background,
            Sprite portrait)
        {
            _name = name;
            _age = age;
            _occupation = occupation;
            _background = background;
            _portrait = portrait;
        }
    }
    
    /// <summary>
    /// Character skill data
    /// </summary>
    [Serializable]
    public class Skill
    {
        [SerializeField] private string _skillID;
        [SerializeField] private string _name;
        [SerializeField] private float _level; // 0.0 to 1.0
        [SerializeField] private float _experience;
        [SerializeField] private float _nextLevelThreshold;
        
        public string SkillID => _skillID;
        public string Name => _name;
        public float Level => _level;
        public float Experience => _experience;
        public float NextLevelThreshold => _nextLevelThreshold;
        
        public Skill(
            string skillID,
            string name,
            float level,
            float experience,
            float nextLevelThreshold)
        {
            _skillID = skillID;
            _name = name;
            _level = Mathf.Clamp01(level);
            _experience = experience;
            _nextLevelThreshold = nextLevelThreshold;
        }
        
        public float GetProgressToNextLevel()
        {
            return Mathf.Clamp01(_experience / _nextLevelThreshold);
        }
    }
    
    /// <summary>
    /// Character mental state data
    /// </summary>
    [Serializable]
    public class MentalState
    {
        [SerializeField] private Dictionary<EmotionType, float> _emotions = new Dictionary<EmotionType, float>();
        [SerializeField] private float _stress;
        [SerializeField] private float _happiness;
        [SerializeField] private float _motivation;
        [SerializeField] private List<MoodEffect> _activeEffects = new List<MoodEffect>();
        
        public IReadOnlyDictionary<EmotionType, float> Emotions => _emotions;
        public float Stress => _stress;
        public float Happiness => _happiness;
        public float Motivation => _motivation;
        public IReadOnlyList<MoodEffect> ActiveEffects => _activeEffects;
        
        public MentalState()
        {
            _stress = 0;
            _happiness = 0.5f;
            _motivation = 0.5f;
        }
        
        public MentalState(
            Dictionary<EmotionType, float> emotions,
            float stress,
            float happiness,
            float motivation,
            List<MoodEffect> activeEffects)
        {
            _emotions = emotions ?? new Dictionary<EmotionType, float>();
            _stress = Mathf.Clamp01(stress);
            _happiness = Mathf.Clamp01(happiness);
            _motivation = Mathf.Clamp01(motivation);
            _activeEffects = activeEffects ?? new List<MoodEffect>();
        }
        
        public MentalState(MentalState other)
        {
            _emotions = new Dictionary<EmotionType, float>(other.Emotions);
            _stress = other.Stress;
            _happiness = other.Happiness;
            _motivation = other.Motivation;
            _activeEffects = new List<MoodEffect>(other.ActiveEffects);
        }
        
        public float GetEmotionValue(EmotionType emotionType, float defaultValue = 0)
        {
            return _emotions.TryGetValue(emotionType, out var value) ? value : defaultValue;
        }
    }
    
    /// <summary>
    /// Mood effect data
    /// </summary>
    [Serializable]
    public class MoodEffect
    {
        [SerializeField] private string _effectID;
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private float _intensity; // 0.0 to 1.0
        [SerializeField] private DateTime _expiryTime; // When the effect expires
        
        public string EffectID => _effectID;
        public string Name => _name;
        public string Description => _description;
        public float Intensity => _intensity;
        public DateTime ExpiryTime => _expiryTime;
        
        public MoodEffect(
            string effectID,
            string name,
            string description,
            float intensity,
            DateTime expiryTime)
        {
            _effectID = effectID;
            _name = name;
            _description = description;
            _intensity = Mathf.Clamp01(intensity);
            _expiryTime = expiryTime;
        }
        
        public bool IsActive(DateTime currentTime)
        {
            return currentTime < _expiryTime;
        }
        
        public TimeSpan GetRemainingTime(DateTime currentTime)
        {
            return _expiryTime > currentTime ? _expiryTime - currentTime : TimeSpan.Zero;
        }
    }
    
    /// <summary>
    /// Character physical condition data
    /// </summary>
    [Serializable]
    public class PhysicalCondition
    {
        [SerializeField] private float _stamina;
        [SerializeField] private float _energy;
        [SerializeField] private float _health;
        [SerializeField] private List<ConditionEffect> _activeConditions = new List<ConditionEffect>();
        
        public float Stamina => _stamina;
        public float Energy => _energy;
        public float Health => _health;
        public IReadOnlyList<ConditionEffect> ActiveConditions => _activeConditions;
        
        public PhysicalCondition()
        {
            _stamina = 1.0f;
            _energy = 1.0f;
            _health = 1.0f;
        }
        
        public PhysicalCondition(
            float stamina,
            float energy,
            float health,
            List<ConditionEffect> activeConditions)
        {
            _stamina = Mathf.Clamp01(stamina);
            _energy = Mathf.Clamp01(energy);
            _health = Mathf.Clamp01(health);
            _activeConditions = activeConditions ?? new List<ConditionEffect>();
        }
        
        public PhysicalCondition(PhysicalCondition other)
        {
            _stamina = other.Stamina;
            _energy = other.Energy;
            _health = other.Health;
            _activeConditions = new List<ConditionEffect>(other.ActiveConditions);
        }
        
        public bool HasCondition(ConditionType conditionType)
        {
            return _activeConditions.Exists(c => c.Type == conditionType);
        }
        
        public ConditionEffect GetCondition(ConditionType conditionType)
        {
            return _activeConditions.Find(c => c.Type == conditionType);
        }
    }
    
    /// <summary>
    /// Physical condition effect data
    /// </summary>
    [Serializable]
    public class ConditionEffect
    {
        [SerializeField] private string _effectID;
        [SerializeField] private string _name;
        [SerializeField] private ConditionType _type;
        [SerializeField] private string _description;
        [SerializeField] private float _severity; // 0.0 to 1.0
        [SerializeField] private DateTime _expiryTime; // When the condition expires, null if permanent
        
        public string EffectID => _effectID;
        public string Name => _name;
        public ConditionType Type => _type;
        public string Description => _description;
        public float Severity => _severity;
        public DateTime ExpiryTime => _expiryTime;
        
        public ConditionEffect(
            string effectID,
            string name,
            ConditionType type,
            string description,
            float severity,
            DateTime expiryTime)
        {
            _effectID = effectID;
            _name = name;
            _type = type;
            _description = description;
            _severity = Mathf.Clamp01(severity);
            _expiryTime = expiryTime;
        }
        
        public bool IsActive(DateTime currentTime)
        {
            return currentTime < _expiryTime;
        }
        
        public TimeSpan GetRemainingTime(DateTime currentTime)
        {
            return _expiryTime > currentTime ? _expiryTime - currentTime : TimeSpan.Zero;
        }
        
        public bool IsPermanent()
        {
            return _expiryTime == DateTime.MaxValue;
        }
    }
    
    /// <summary>
    /// Character inventory data
    /// </summary>
    [Serializable]
    public class Inventory
    {
        [SerializeField] private List<InventoryItem> _items = new List<InventoryItem>();
        [SerializeField] private Dictionary<EquipSlot, InventoryItem> _equippedItems = new Dictionary<EquipSlot, InventoryItem>();
        [SerializeField] private Dictionary<ResourceType, float> _resources = new Dictionary<ResourceType, float>();
        
        public IReadOnlyList<InventoryItem> Items => _items;
        public IReadOnlyDictionary<EquipSlot, InventoryItem> EquippedItems => _equippedItems;
        public IReadOnlyDictionary<ResourceType, float> Resources => _resources;
        
        public Inventory()
        {
        }
        
        public Inventory(
            List<InventoryItem> items,
            Dictionary<EquipSlot, InventoryItem> equippedItems,
            Dictionary<ResourceType, float> resources)
        {
            _items = items ?? new List<InventoryItem>();
            _equippedItems = equippedItems ?? new Dictionary<EquipSlot, InventoryItem>();
            _resources = resources ?? new Dictionary<ResourceType, float>();
        }
        
        public float GetResourceAmount(ResourceType resourceType, float defaultValue = 0)
        {
            return _resources.TryGetValue(resourceType, out var amount) ? amount : defaultValue;
        }
        
        public InventoryItem GetEquippedItem(EquipSlot slot)
        {
            return _equippedItems.TryGetValue(slot, out var item) ? item : null;
        }
    }
    
    /// <summary>
    /// Inventory item data
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        [SerializeField] private string _itemID;
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private ItemType _type;
        [SerializeField] private int _quantity;
        [SerializeField] private float _value;
        [SerializeField] private Sprite _icon;
        [SerializeField] private List<ItemAttribute> _attributes = new List<ItemAttribute>();
        
        public string ItemID => _itemID;
        public string Name => _name;
        public string Description => _description;
        public ItemType Type => _type;
        public int Quantity => _quantity;
        public float Value => _value;
        public Sprite Icon => _icon;
        public IReadOnlyList<ItemAttribute> Attributes => _attributes;
        
        public InventoryItem(
            string itemID,
            string name,
            string description,
            ItemType type,
            int quantity,
            float value,
            Sprite icon,
            List<ItemAttribute> attributes = null)
        {
            _itemID = itemID;
            _name = name;
            _description = description;
            _type = type;
            _quantity = quantity;
            _value = value;
            _icon = icon;
            _attributes = attributes ?? new List<ItemAttribute>();
        }
        
        public bool IsEquippable()
        {
            return _type == ItemType.Equipment || _type == ItemType.Weapon || _type == ItemType.Apparel;
        }
        
        public bool IsConsumable()
        {
            return _type == ItemType.Consumable;
        }
        
        public bool HasAttribute(string attributeName)
        {
            return _attributes.Exists(a => a.Name == attributeName);
        }
        
        public ItemAttribute GetAttribute(string attributeName)
        {
            return _attributes.Find(a => a.Name == attributeName);
        }
    }
    
    /// <summary>
    /// Item attribute data
    /// </summary>
    [Serializable]
    public class ItemAttribute
    {
        [SerializeField] private string _name;
        [SerializeField] private float _value;
        
        public string Name => _name;
        public float Value => _value;
        
        public ItemAttribute(string name, float value)
        {
            _name = name;
            _value = value;
        }
    }
    
    /// <summary>
    /// Character social status data
    /// </summary>
    [Serializable]
    public class SocialStatus
    {
        [SerializeField] private Dictionary<string, float> _reputation = new Dictionary<string, float>();
        [SerializeField] private float _overallFame;
        [SerializeField] private List<string> _titles = new List<string>();
        [SerializeField] private List<SocialAchievement> _achievements = new List<SocialAchievement>();
        
        public IReadOnlyDictionary<string, float> Reputation => _reputation;
        public float OverallFame => _overallFame;
        public IReadOnlyList<string> Titles => _titles;
        public IReadOnlyList<SocialAchievement> Achievements => _achievements;
        
        public SocialStatus()
        {
            _overallFame = 0;
        }
        
        public SocialStatus(
            Dictionary<string, float> reputation,
            float overallFame,
            List<string> titles,
            List<SocialAchievement> achievements)
        {
            _reputation = reputation ?? new Dictionary<string, float>();
            _overallFame = Mathf.Clamp01(overallFame);
            _titles = titles ?? new List<string>();
            _achievements = achievements ?? new List<SocialAchievement>();
        }
        
        public float GetReputationWith(string groupID, float defaultValue = 0)
        {
            return _reputation.TryGetValue(groupID, out var value) ? value : defaultValue;
        }
        
        public List<SocialAchievement> GetUnlockedAchievements()
        {
            return _achievements.FindAll(a => a.IsUnlocked);
        }
    }
    
    /// <summary>
    /// Social achievement data
    /// </summary>
    [Serializable]
    public class SocialAchievement
    {
        [SerializeField] private string _achievementID;
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private bool _isUnlocked;
        [SerializeField] private DateTime _unlockDate;
        [SerializeField] private float _prestigeValue;
        
        public string AchievementID => _achievementID;
        public string Name => _name;
        public string Description => _description;
        public bool IsUnlocked => _isUnlocked;
        public DateTime UnlockDate => _unlockDate;
        public float PrestigeValue => _prestigeValue;
        
        public SocialAchievement(
            string achievementID,
            string name,
            string description,
            bool isUnlocked,
            DateTime unlockDate,
            float prestigeValue)
        {
            _achievementID = achievementID;
            _name = name;
            _description = description;
            _isUnlocked = isUnlocked;
            _unlockDate = unlockDate;
            _prestigeValue = prestigeValue;
        }
    }
    
    /// <summary>
    /// Character goal data
    /// </summary>
    [Serializable]
    public class Goal
    {
        [SerializeField] private string _goalID;
        [SerializeField] private string _description;
        [SerializeField] private float _progress; // 0.0 to 1.0
        [SerializeField] private DateTime _deadline;
        [SerializeField] private List<SubGoal> _subGoals = new List<SubGoal>();
        [SerializeField] private GoalStatus _status;
        
        public string GoalID => _goalID;
        public string Description => _description;
        public float Progress => _progress;
        public DateTime Deadline => _deadline;
        public IReadOnlyList<SubGoal> SubGoals => _subGoals;
        public GoalStatus Status => _status;
        
        public Goal(
            string goalID,
            string description,
            float progress,
            DateTime deadline,
            List<SubGoal> subGoals,
            GoalStatus status)
        {
            _goalID = goalID;
            _description = description;
            _progress = Mathf.Clamp01(progress);
            _deadline = deadline;
            _subGoals = subGoals ?? new List<SubGoal>();
            _status = status;
        }
        
        public bool IsOverdue(DateTime currentTime)
        {
            return _status != GoalStatus.Completed && _deadline < currentTime;
        }
        
        public TimeSpan GetTimeRemaining(DateTime currentTime)
        {
            return _deadline > currentTime ? _deadline - currentTime : TimeSpan.Zero;
        }
        
        public float GetSubGoalCompletionPercentage()
        {
            if (_subGoals.Count == 0) return 0;
            
            int completedCount = 0;
            foreach (var subGoal in _subGoals)
            {
                if (subGoal.IsCompleted)
                {
                    completedCount++;
                }
            }
            
            return (float)completedCount / _subGoals.Count;
        }
    }
    
    /// <summary>
    /// Sub-goal data
    /// </summary>
    [Serializable]
    public class SubGoal
    {
        [SerializeField] private string _subGoalID;
        [SerializeField] private string _description;
        [SerializeField] private bool _isCompleted;
        
        public string SubGoalID => _subGoalID;
        public string Description => _description;
        public bool IsCompleted => _isCompleted;
        
        public SubGoal(
            string subGoalID,
            string description,
            bool isCompleted)
        {
            _subGoalID = subGoalID;
            _description = description;
            _isCompleted = isCompleted;
        }
    }
    
    /// <summary>
    /// Character status history
    /// </summary>
    [Serializable]
    public class StatusHistory
    {
        [SerializeField] private List<StatusSnapshot> _snapshots = new List<StatusSnapshot>();
        
        public IReadOnlyList<StatusSnapshot> Snapshots => _snapshots;
        
        public StatusHistory()
        {
        }
        
        public StatusHistory(List<StatusSnapshot> snapshots)
        {
            _snapshots = snapshots ?? new List<StatusSnapshot>();
        }
        
        public StatusSnapshot GetLatestSnapshot()
        {
            return _snapshots.Count > 0 ? _snapshots[_snapshots.Count - 1] : null;
        }
        
        public List<StatusSnapshot> GetSnapshotsInRange(DateTime startTime, DateTime endTime)
        {
            return _snapshots.FindAll(s => s.Timestamp >= startTime && s.Timestamp <= endTime);
        }
    }
    
    /// <summary>
    /// Status snapshot at a specific point in time
    /// </summary>
    [Serializable]
    public class StatusSnapshot
    {
        [SerializeField] private DateTime _timestamp;
        [SerializeField] private Dictionary<StatType, float> _stats = new Dictionary<StatType, float>();
        [SerializeField] private List<Skill> _skills = new List<Skill>();
        [SerializeField] private MentalState _mentalState;
        [SerializeField] private PhysicalCondition _physicalCondition;
        
        public DateTime Timestamp => _timestamp;
        public IReadOnlyDictionary<StatType, float> Stats => _stats;
        public IReadOnlyList<Skill> Skills => _skills;
        public MentalState MentalState => _mentalState;
        public PhysicalCondition PhysicalCondition => _physicalCondition;
        
        public StatusSnapshot(
            DateTime timestamp,
            Dictionary<StatType, float> stats,
            List<Skill> skills,
            MentalState mentalState,
            PhysicalCondition physicalCondition)
        {
            _timestamp = timestamp;
            _stats = stats ?? new Dictionary<StatType, float>();
            _skills = skills ?? new List<Skill>();
            _mentalState = mentalState;
            _physicalCondition = physicalCondition;
        }
    }
    
    /// <summary>
    /// Enum representing the type of character stat
    /// </summary>
    public enum StatType
    {
        Strength,
        Dexterity,
        Constitution,
        Intelligence,
        Wisdom,
        Charisma,
        Luck,
        Custom
    }
    
    /// <summary>
    /// Enum representing the type of emotion
    /// </summary>
    public enum EmotionType
    {
        Joy,
        Sadness,
        Anger,
        Fear,
        Disgust,
        Surprise,
        Trust,
        Anticipation,
        Love,
        Jealousy,
        Pride,
        Shame,
        Guilt,
        Custom
    }
    
    /// <summary>
    /// Enum representing the type of condition
    /// </summary>
    public enum ConditionType
    {
        Healthy,
        Injured,
        Sick,
        Exhausted,
        Hungry,
        Thirsty,
        Intoxicated,
        Custom
    }
    
    /// <summary>
    /// Enum representing the type of equip slot
    /// </summary>
    public enum EquipSlot
    {
        Head,
        Body,
        Legs,
        Feet,
        Hands,
        MainHand,
        OffHand,
        Accessory1,
        Accessory2,
        Custom
    }
    
    /// <summary>
    /// Enum representing the type of item
    /// </summary>
    public enum ItemType
    {
        Equipment,
        Weapon,
        Apparel,
        Consumable,
        Crafting,
        Quest,
        Valuable,
        Miscellaneous,
        Custom
    }
    
    /// <summary>
    /// Enum representing the type of resource
    /// </summary>
    public enum ResourceType
    {
        Money,
        Experience,
        Energy,
        Food,
        Materials,
        Fuel,
        Custom
    }
    
    /// <summary>
    /// Enum representing the status of a goal
    /// </summary>
    public enum GoalStatus
    {
        Active,
        InProgress,
        Completed,
        Failed,
        Abandoned
    }
}