using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProgressionAndEventSystem
{
    #region Multi-Ending System Enums
    public enum EndingCategory
    {
        Main,
        Character,
        Special,
        Hidden,
        TimeBase
    }

    public enum EndingType
    {
        Good,
        Bad,
        Neutral,
        Secret,
        True
    }

    // Use our own enum since the existing one has different values
    public enum EndingConditionType
    {
        RelationshipValue,
        EventCompleted,
        ItemPossession,
        PlayerStat,
        WorldState,
        CharacterState,
        TimeBased
    }

    public enum EndingCombinationType
    {
        Complementary,
        Contradictory,
        Synergistic,
        Special,
        Hidden
    }
    #endregion

    #region Multi-Ending System Data Structures
    public class GameEnding
    {
        public string Id;
        public string Title;
        public string Description;
        public EndingCategory Category;
        public EndingType Type;
        public List<EndingCondition> Conditions;
        public int QualityLevel; // Achievement difficulty/quality
        public List<string> UnlockedRewards;
        public Dictionary<string, object> EndingData;
        public string EndingSceneId;
        public List<string> EndingCutsceneIds;
        public List<string> EpilogueTextIds;
    }

    public class EndingCondition
    {
        public EndingConditionType Type;
        public string TargetId;
        public ComparisonOperator Operator;
        public object RequiredValue;
        public float Weight; // Condition importance
        public bool IsMandatory;
        public string Description;
        public bool IsHidden;
    }

    public class EndingBranch
    {
        public string Id;
        public string CharacterId; // Related NPC ID
        public RelationshipType RelationshipType;
        public string BranchName;
        public string Description;
        public List<BranchCondition> Conditions;
        public Dictionary<string, object> BranchData;
        public List<string> UnlockedContent;
        public List<string> BlockedEndings;
    }

    public class BranchCondition
    {
        public EndingConditionType Type;
        public string ParameterId;
        public ComparisonOperator Operator;
        public object ThresholdValue;
        public bool IsCritical;
        public string HintText;
    }

    public class EndingCombination
    {
        public string Id;
        public string Name;
        public string Description;
        public List<string> RequiredEndingIds;
        public List<string> ExcludedEndingIds;
        public EndingCombinationType Type;
        public string CombinedEndingId;
        public bool IsHidden;
        public List<string> UnlockedRewards;
    }

    public class RelationshipSummary
    {
        public string CharacterId;
        public RelationshipType Type;
        public int StageLevel;
        public Dictionary<string, float> FinalParameters;
        public List<string> CompletedEvents;
        public List<string> PotentialEndings;
        public string MostLikelyEndingId;
        public float SatisfactionScore;
    }

    public class GameDate
    {
        public int Year;
        public int Month;
        public int Day;
        public int Hour;
        public int Minute;
        
        public static bool operator <(GameDate a, GameDate b)
        {
            if (a.Year != b.Year) return a.Year < b.Year;
            if (a.Month != b.Month) return a.Month < b.Month;
            if (a.Day != b.Day) return a.Day < b.Day;
            if (a.Hour != b.Hour) return a.Hour < b.Hour;
            return a.Minute < b.Minute;
        }
        
        public static bool operator >(GameDate a, GameDate b)
        {
            if (a.Year != b.Year) return a.Year > b.Year;
            if (a.Month != b.Month) return a.Month > b.Month;
            if (a.Day != b.Day) return a.Day > b.Day;
            if (a.Hour != b.Hour) return a.Hour > b.Hour;
            return a.Minute > b.Minute;
        }
        
        public static bool operator ==(GameDate a, GameDate b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Year == b.Year && a.Month == b.Month && a.Day == b.Day &&
                   a.Hour == b.Hour && a.Minute == b.Minute;
        }
        
        public static bool operator !=(GameDate a, GameDate b)
        {
            return !(a == b);
        }
        
        public override bool Equals(object obj)
        {
            if (obj is GameDate date)
            {
                return this == date;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Year, Month, Day, Hour, Minute);
        }
    }
    #endregion
    
    #region Multi-Ending System Character Extensions
    // Extensions to provide required functionality for the ICharacter interface
    public static class MultiEndingCharacterExtensions
    {
        public static Dictionary<string, float> GetRelationshipValues(this ICharacter character, string characterId)
        {
            // Adapt to the existing interface
            var relationships = character.GetRelationships();
            // In a real implementation, we would need to filter these by character ID
            return relationships;
        }
        
        public static List<string> GetCompletedEvents(this ICharacter character)
        {
            // Convert from the character's event history
            var eventHistory = character.GetEventHistory();
            return eventHistory.Keys.ToList();
        }
        
        public static List<string> GetPossessedItems(this ICharacter character)
        {
            // Adapt to the existing interface
            // In a real implementation, we might check specific flags or states
            var possessedItems = new List<string>();
            var state = character.GetState();
            
            // Look for items in the state
            foreach (var key in state.Keys)
            {
                if (key.StartsWith("item_") && state[key] is bool hasItem && hasItem)
                {
                    possessedItems.Add(key.Substring(5)); // Remove "item_" prefix
                }
            }
            
            return possessedItems;
        }
    }
    #endregion
    
    #region Multi-Ending System Interfaces
    // Using existing ICharacter interface from ProgressionAndEventSystem

    public interface IEndingManager
    {
        void RegisterEnding(GameEnding ending);
        List<GameEnding> GetPotentialEndings(ICharacter player);
        GameEnding GetCurrentMostLikelyEnding(ICharacter player);
        float GetEndingProgress(string endingId, ICharacter player);
        Dictionary<string, float> GetAllEndingProgresses(ICharacter player);
    }
    
    public interface IEndingBranchSystem
    {
        void RegisterEndingBranch(EndingBranch branch);
        List<EndingBranch> GetAvailableBranches(ICharacter player, ICharacter npc);
        EndingBranch GetCurrentBranch(ICharacter player, ICharacter npc);
        List<BranchCondition> GetRemainingConditions(string branchId, ICharacter player, ICharacter npc);
    }
    
    public interface IEndingCombinationSystem
    {
        void RegisterEndingCombination(EndingCombination combination);
        EndingCombination GetMatchingCombination(List<string> achievedEndingIds);
        List<string> GetRecommendedAdditionalEndings(List<string> currentEndingIds);
        GameEnding GetCombinedEnding(List<string> endingIds);
    }
    
    public interface IProgressionEndingSystem
    {
        GameEnding GetTimeBasedEnding(ICharacter player, GameDate currentDate);
        float GetTimeBasedEndingQuality(ICharacter player, GameDate currentDate);
        List<RelationshipSummary> GetRelationshipSummaries(ICharacter player);
        Dictionary<string, float> GetGlobalAchievementRates(ICharacter player);
    }
    #endregion
    
    /// <summary>
    /// The MultiEndingSystem manages the game's ending system, including individual character endings,
    /// ending combinations, time-based endings, and progression-based endings.
    /// </summary>
    public class MultiEndingSystem : MonoBehaviour, IEndingManager, IEndingBranchSystem, IEndingCombinationSystem, IProgressionEndingSystem
    {
        private static MultiEndingSystem _instance;
        public static MultiEndingSystem Instance => _instance;

        #region Events
        public delegate void EndingConditionMetHandler(string endingId, string conditionId);
        public delegate void EndingUnlockedHandler(string endingId);
        public delegate void EndingPlayedHandler(string endingId);
        public delegate void EndingCombinationDiscoveredHandler(string combinationId);
        public delegate void TimeBasedEndingTriggeredHandler(string endingId, GameDate date);
        public delegate void EndingGalleryUpdatedHandler(string endingId);

        public event EndingConditionMetHandler OnEndingConditionMet;
        public event EndingUnlockedHandler OnEndingUnlocked;
        public event EndingPlayedHandler OnEndingPlayed;
        public event EndingCombinationDiscoveredHandler OnEndingCombinationDiscovered;
        public event TimeBasedEndingTriggeredHandler OnTimeBasedEndingTriggered;
        public event EndingGalleryUpdatedHandler OnEndingGalleryUpdated;
        #endregion

        #region Private Fields
        private Dictionary<string, GameEnding> _endings = new Dictionary<string, GameEnding>();
        private Dictionary<string, EndingBranch> _branches = new Dictionary<string, EndingBranch>();
        private Dictionary<string, EndingCombination> _combinations = new Dictionary<string, EndingCombination>();
        private Dictionary<string, List<string>> _characterToBranchesMap = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _achievedEndings = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _unlockedRewards = new Dictionary<string, List<string>>();
        
        private Dictionary<string, List<string>> _characterToEndingsMap = new Dictionary<string, List<string>>();
        private Dictionary<string, GameEnding> _timeBasedEndings = new Dictionary<string, GameEnding>();
        private Dictionary<string, Dictionary<string, float>> _endingProgress = new Dictionary<string, Dictionary<string, float>>();
        private Dictionary<string, GameDate> _endingAchievementDate = new Dictionary<string, GameDate>();
        #endregion

        #region Unity Lifecycle Methods
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeSystem();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        #endregion

        #region Initialization
        private void InitializeSystem()
        {
            // Initialize data structures and load any saved data
            _endings.Clear();
            _branches.Clear();
            _combinations.Clear();
            _characterToBranchesMap.Clear();
            _achievedEndings.Clear();
            _unlockedRewards.Clear();
            _characterToEndingsMap.Clear();
            _timeBasedEndings.Clear();
            _endingProgress.Clear();
            _endingAchievementDate.Clear();
            
            // Load data from resources or saved data
            LoadEndingsFromResources();
            LoadBranchesFromResources();
            LoadCombinationsFromResources();
        }
        
        private void LoadEndingsFromResources()
        {
            // Load ending definitions from resources or config files
            // For implementation, this would typically load from ScriptableObjects or JSON files
            
            // Example implementation:
            /*
            var endingAssets = Resources.LoadAll<EndingDefinitionAsset>("Endings");
            foreach (var asset in endingAssets)
            {
                var ending = new GameEnding
                {
                    Id = asset.Id,
                    Title = asset.Title,
                    Description = asset.Description,
                    Category = asset.Category,
                    Type = asset.Type,
                    Conditions = asset.Conditions.Select(c => new EndingCondition
                    {
                        Type = c.Type,
                        TargetId = c.TargetId,
                        Operator = c.Operator,
                        RequiredValue = c.RequiredValue,
                        Weight = c.Weight,
                        IsMandatory = c.IsMandatory,
                        Description = c.Description,
                        IsHidden = c.IsHidden
                    }).ToList(),
                    QualityLevel = asset.QualityLevel,
                    UnlockedRewards = asset.UnlockedRewards,
                    EndingData = asset.EndingData,
                    EndingSceneId = asset.EndingSceneId,
                    EndingCutsceneIds = asset.EndingCutsceneIds,
                    EpilogueTextIds = asset.EpilogueTextIds
                };
                
                RegisterEnding(ending);
            }
            */
        }
        
        private void LoadBranchesFromResources()
        {
            // Load branch definitions from resources or config files
            // Similar to LoadEndingsFromResources
        }
        
        private void LoadCombinationsFromResources()
        {
            // Load combination definitions from resources or config files
            // Similar to LoadEndingsFromResources
        }
        #endregion

        #region IEndingManager Implementation
        public void RegisterEnding(GameEnding ending)
        {
            if (string.IsNullOrEmpty(ending.Id))
            {
                Debug.LogError("Cannot register ending with null or empty ID");
                return;
            }
            
            if (_endings.ContainsKey(ending.Id))
            {
                Debug.LogWarning($"Ending with ID {ending.Id} already registered. Overwriting.");
            }
            
            _endings[ending.Id] = ending;
            
            // If this is a character-specific ending, update the character to endings map
            var characterId = GetRelatedCharacterId(ending);
            if (!string.IsNullOrEmpty(characterId))
            {
                if (!_characterToEndingsMap.ContainsKey(characterId))
                {
                    _characterToEndingsMap[characterId] = new List<string>();
                }
                
                if (!_characterToEndingsMap[characterId].Contains(ending.Id))
                {
                    _characterToEndingsMap[characterId].Add(ending.Id);
                }
            }
            
            // If this is a time-based ending, add it to the time-based endings map
            if (IsTimeBasedEnding(ending))
            {
                _timeBasedEndings[ending.Id] = ending;
            }
            
            Debug.Log($"Registered ending: {ending.Id} - {ending.Title}");
        }

        public List<GameEnding> GetPotentialEndings(ICharacter player)
        {
            var potentialEndings = new List<GameEnding>();
            
            foreach (var ending in _endings.Values)
            {
                // Skip endings that are already achieved
                if (_achievedEndings.ContainsKey(player.Id) && _achievedEndings[player.Id].Contains(ending.Id))
                {
                    continue;
                }
                
                // Check if the ending is potentially achievable
                var progress = GetEndingProgress(ending.Id, player);
                if (progress > 0)
                {
                    potentialEndings.Add(ending);
                }
            }
            
            // Sort by progress descending
            potentialEndings.Sort((a, b) => 
                GetEndingProgress(b.Id, player).CompareTo(GetEndingProgress(a.Id, player)));
            
            return potentialEndings;
        }

        public GameEnding GetCurrentMostLikelyEnding(ICharacter player)
        {
            var potentialEndings = GetPotentialEndings(player);
            
            if (potentialEndings.Count == 0)
            {
                return null;
            }
            
            // Return the ending with the highest progress
            return potentialEndings[0];
        }

        public float GetEndingProgress(string endingId, ICharacter player)
        {
            if (!_endings.TryGetValue(endingId, out var ending))
            {
                Debug.LogWarning($"Ending with ID {endingId} not found");
                return 0f;
            }
            
            // Check if we have cached progress
            if (_endingProgress.TryGetValue(player.Id, out var playerProgress) &&
                playerProgress.TryGetValue(endingId, out var cachedProgress))
            {
                return cachedProgress;
            }
            
            // Calculate progress
            float totalWeight = 0f;
            float weightedProgress = 0f;
            bool allMandatoryConditionsMet = true;
            
            foreach (var condition in ending.Conditions)
            {
                float conditionProgress = EvaluateConditionProgress(condition, player, endingId);
                
                // If a mandatory condition is not met, the ending cannot be achieved
                if (condition.IsMandatory && conditionProgress < 1f)
                {
                    allMandatoryConditionsMet = false;
                }
                
                totalWeight += condition.Weight;
                weightedProgress += conditionProgress * condition.Weight;
            }
            
            float progress = totalWeight > 0 ? weightedProgress / totalWeight : 0f;
            
            // If not all mandatory conditions are met, cap progress at 0.99
            if (!allMandatoryConditionsMet && progress > 0.99f)
            {
                progress = 0.99f;
            }
            
            // Cache the progress
            if (!_endingProgress.ContainsKey(player.Id))
            {
                _endingProgress[player.Id] = new Dictionary<string, float>();
            }
            
            _endingProgress[player.Id][endingId] = progress;
            
            // If progress is 1, the ending is achieved
            if (progress >= 1f && (!_achievedEndings.ContainsKey(player.Id) || !_achievedEndings[player.Id].Contains(endingId)))
            {
                AchieveEnding(player.Id, endingId);
            }
            
            return progress;
        }

        public Dictionary<string, float> GetAllEndingProgresses(ICharacter player)
        {
            var result = new Dictionary<string, float>();
            
            foreach (var ending in _endings.Values)
            {
                result[ending.Id] = GetEndingProgress(ending.Id, player);
            }
            
            return result;
        }
        #endregion

        #region IEndingBranchSystem Implementation
        public void RegisterEndingBranch(EndingBranch branch)
        {
            if (string.IsNullOrEmpty(branch.Id))
            {
                Debug.LogError("Cannot register branch with null or empty ID");
                return;
            }
            
            if (_branches.ContainsKey(branch.Id))
            {
                Debug.LogWarning($"Branch with ID {branch.Id} already registered. Overwriting.");
            }
            
            _branches[branch.Id] = branch;
            
            // Update character to branches map
            if (!string.IsNullOrEmpty(branch.CharacterId))
            {
                if (!_characterToBranchesMap.ContainsKey(branch.CharacterId))
                {
                    _characterToBranchesMap[branch.CharacterId] = new List<string>();
                }
                
                if (!_characterToBranchesMap[branch.CharacterId].Contains(branch.Id))
                {
                    _characterToBranchesMap[branch.CharacterId].Add(branch.Id);
                }
            }
            
            Debug.Log($"Registered ending branch: {branch.Id} - {branch.BranchName}");
        }

        public List<EndingBranch> GetAvailableBranches(ICharacter player, ICharacter npc)
        {
            var availableBranches = new List<EndingBranch>();
            
            if (!_characterToBranchesMap.TryGetValue(npc.Id, out var branchIds))
            {
                return availableBranches;
            }
            
            foreach (var branchId in branchIds)
            {
                if (!_branches.TryGetValue(branchId, out var branch))
                {
                    continue;
                }
                
                // Check if the branch conditions are met
                if (AreConditionsMet(branch.Conditions, player, npc))
                {
                    availableBranches.Add(branch);
                }
            }
            
            return availableBranches;
        }

        public EndingBranch GetCurrentBranch(ICharacter player, ICharacter npc)
        {
            var availableBranches = GetAvailableBranches(player, npc);
            
            if (availableBranches.Count == 0)
            {
                return null;
            }
            
            // Return the most significant branch (could be based on various criteria)
            // Here we'll use the one with the highest number of conditions met
            return availableBranches.OrderByDescending(b => CountMetConditions(b.Conditions, player, npc)).First();
        }

        public List<BranchCondition> GetRemainingConditions(string branchId, ICharacter player, ICharacter npc)
        {
            if (!_branches.TryGetValue(branchId, out var branch))
            {
                Debug.LogWarning($"Branch with ID {branchId} not found");
                return new List<BranchCondition>();
            }
            
            var remainingConditions = new List<BranchCondition>();
            
            foreach (var condition in branch.Conditions)
            {
                if (!IsConditionMet(condition, player, npc))
                {
                    remainingConditions.Add(condition);
                }
            }
            
            return remainingConditions;
        }
        #endregion

        #region IEndingCombinationSystem Implementation
        public void RegisterEndingCombination(EndingCombination combination)
        {
            if (string.IsNullOrEmpty(combination.Id))
            {
                Debug.LogError("Cannot register combination with null or empty ID");
                return;
            }
            
            if (_combinations.ContainsKey(combination.Id))
            {
                Debug.LogWarning($"Combination with ID {combination.Id} already registered. Overwriting.");
            }
            
            _combinations[combination.Id] = combination;
            
            Debug.Log($"Registered ending combination: {combination.Id} - {combination.Name}");
        }

        public EndingCombination GetMatchingCombination(List<string> achievedEndingIds)
        {
            foreach (var combination in _combinations.Values)
            {
                // Check if all required endings are achieved
                bool allRequired = true;
                foreach (var requiredId in combination.RequiredEndingIds)
                {
                    if (!achievedEndingIds.Contains(requiredId))
                    {
                        allRequired = false;
                        break;
                    }
                }
                
                if (!allRequired)
                {
                    continue;
                }
                
                // Check if none of the excluded endings are achieved
                bool noExcluded = true;
                foreach (var excludedId in combination.ExcludedEndingIds)
                {
                    if (achievedEndingIds.Contains(excludedId))
                    {
                        noExcluded = false;
                        break;
                    }
                }
                
                if (noExcluded)
                {
                    return combination;
                }
            }
            
            return null;
        }

        public List<string> GetRecommendedAdditionalEndings(List<string> currentEndingIds)
        {
            var recommendations = new List<string>();
            
            // Find combinations that are close to being achieved
            foreach (var combination in _combinations.Values)
            {
                // Skip combinations with excluded endings
                bool hasExcluded = false;
                foreach (var excludedId in combination.ExcludedEndingIds)
                {
                    if (currentEndingIds.Contains(excludedId))
                    {
                        hasExcluded = true;
                        break;
                    }
                }
                
                if (hasExcluded)
                {
                    continue;
                }
                
                // Count missing required endings
                List<string> missing = new List<string>();
                foreach (var requiredId in combination.RequiredEndingIds)
                {
                    if (!currentEndingIds.Contains(requiredId))
                    {
                        missing.Add(requiredId);
                    }
                }
                
                // If only a few endings are missing, recommend them
                if (missing.Count > 0 && missing.Count <= 3)
                {
                    recommendations.AddRange(missing);
                }
            }
            
            // Remove duplicates
            return recommendations.Distinct().ToList();
        }

        public GameEnding GetCombinedEnding(List<string> endingIds)
        {
            // Find a matching combination
            var combination = GetMatchingCombination(endingIds);
            
            if (combination == null || string.IsNullOrEmpty(combination.CombinedEndingId))
            {
                return null;
            }
            
            // Return the combined ending
            if (_endings.TryGetValue(combination.CombinedEndingId, out var combinedEnding))
            {
                return combinedEnding;
            }
            
            return null;
        }
        #endregion

        #region IProgressionEndingSystem Implementation
        public GameEnding GetTimeBasedEnding(ICharacter player, GameDate currentDate)
        {
            // Find applicable time-based endings
            var applicableEndings = new List<GameEnding>();
            
            foreach (var ending in _timeBasedEndings.Values)
            {
                if (IsEndingApplicableAtDate(ending, player, currentDate))
                {
                    applicableEndings.Add(ending);
                }
            }
            
            if (applicableEndings.Count == 0)
            {
                return null;
            }
            
            // Sort by quality
            applicableEndings.Sort((a, b) => b.QualityLevel.CompareTo(a.QualityLevel));
            
            // Return the highest quality applicable ending
            var selectedEnding = applicableEndings[0];
            
            // Trigger event
            OnTimeBasedEndingTriggered?.Invoke(selectedEnding.Id, currentDate);
            
            return selectedEnding;
        }

        public float GetTimeBasedEndingQuality(ICharacter player, GameDate currentDate)
        {
            var ending = GetTimeBasedEnding(player, currentDate);
            
            if (ending == null)
            {
                return 0f;
            }
            
            // Calculate quality normalized to 0-1 range
            return CalculateEndingQuality(ending, player);
        }

        public List<RelationshipSummary> GetRelationshipSummaries(ICharacter player)
        {
            var summaries = new List<RelationshipSummary>();
            
            // This would require access to the relationship system
            // For demonstration purposes, we'll return a mock implementation
            
            // In a real implementation, we would iterate through all NPCs and generate summaries
            /*
            foreach (var npc in characterManager.GetAllNPCs())
            {
                var summary = new RelationshipSummary
                {
                    CharacterId = npc.Id,
                    Type = GetRelationshipType(player, npc),
                    StageLevel = GetRelationshipStage(player, npc),
                    FinalParameters = player.GetRelationshipValues(npc.Id),
                    CompletedEvents = GetCompletedEventsWithNPC(player, npc),
                    PotentialEndings = GetPotentialEndingsForNPC(player, npc),
                    MostLikelyEndingId = GetMostLikelyEndingForNPC(player, npc),
                    SatisfactionScore = CalculateRelationshipSatisfaction(player, npc)
                };
                
                summaries.Add(summary);
            }
            */
            
            return summaries;
        }

        public Dictionary<string, float> GetGlobalAchievementRates(ICharacter player)
        {
            var rates = new Dictionary<string, float>();
            
            // Calculate rates for different categories
            rates["MainStory"] = CalculateCategoryCompletionRate(player, EndingCategory.Main);
            rates["CharacterEndings"] = CalculateCategoryCompletionRate(player, EndingCategory.Character);
            rates["SpecialEndings"] = CalculateCategoryCompletionRate(player, EndingCategory.Special);
            rates["HiddenEndings"] = CalculateCategoryCompletionRate(player, EndingCategory.Hidden);
            rates["TrueEndings"] = CalculateTypeCompletionRate(player, EndingType.True);
            rates["Overall"] = CalculateOverallCompletionRate(player);
            
            return rates;
        }
        #endregion

        #region Helper Methods
        private float EvaluateConditionProgress(EndingCondition condition, ICharacter player, string endingId = null)
        {
            float progress = 0f;
            
            switch (condition.Type)
            {
                case EndingConditionType.RelationshipValue:
                    // Format: "CharacterId:ParameterName"
                    string[] parts = condition.TargetId.Split(':');
                    if (parts.Length != 2)
                    {
                        Debug.LogError($"Invalid relationship value target ID format: {condition.TargetId}");
                        return 0f;
                    }
                    
                    string characterId = parts[0];
                    string parameterName = parts[1];
                    
                    var relationshipValues = player.GetRelationshipValues(characterId);
                    if (!relationshipValues.TryGetValue(parameterName, out float value))
                    {
                        return 0f;
                    }
                    
                    progress = EvaluateProgressForNumericCondition(value, condition.Operator, Convert.ToSingle(condition.RequiredValue));
                    break;
                
                case EndingConditionType.EventCompleted:
                    var completedEvents = player.GetCompletedEvents();
                    
                    if (condition.Operator == ComparisonOperator.Contains)
                    {
                        progress = completedEvents.Contains(condition.TargetId) ? 1f : 0f;
                    }
                    else if (condition.Operator == ComparisonOperator.NotContains)
                    {
                        progress = !completedEvents.Contains(condition.TargetId) ? 1f : 0f;
                    }
                    break;
                
                case EndingConditionType.ItemPossession:
                    var possessedItems = player.GetPossessedItems();
                    
                    if (condition.Operator == ComparisonOperator.Contains)
                    {
                        progress = possessedItems.Contains(condition.TargetId) ? 1f : 0f;
                    }
                    else if (condition.Operator == ComparisonOperator.NotContains)
                    {
                        progress = !possessedItems.Contains(condition.TargetId) ? 1f : 0f;
                    }
                    break;
                
                case EndingConditionType.PlayerStat:
                    var stats = player.GetStats();
                    if (!stats.TryGetValue(condition.TargetId, out float statValue))
                    {
                        return 0f;
                    }
                    
                    progress = EvaluateProgressForNumericCondition(statValue, condition.Operator, Convert.ToSingle(condition.RequiredValue));
                    break;
                
                case EndingConditionType.WorldState:
                    // This would require access to the world state system
                    // For demonstration purposes, we'll return 0 for now
                    progress = 0f;
                    break;
                
                case EndingConditionType.CharacterState:
                    var state = player.GetState();
                    if (!state.TryGetValue(condition.TargetId, out object stateValue))
                    {
                        return 0f;
                    }
                    
                    if (stateValue is float floatValue && condition.RequiredValue is float requiredFloatValue)
                    {
                        progress = EvaluateProgressForNumericCondition(floatValue, condition.Operator, requiredFloatValue);
                    }
                    else if (stateValue is int intValue && condition.RequiredValue is int requiredIntValue)
                    {
                        progress = EvaluateProgressForNumericCondition(intValue, condition.Operator, requiredIntValue);
                    }
                    else if (stateValue is string stringValue && condition.RequiredValue is string requiredStringValue)
                    {
                        progress = EvaluateConditionForStringValue(stringValue, condition.Operator, requiredStringValue);
                    }
                    else if (stateValue is bool boolValue && condition.RequiredValue is bool requiredBoolValue)
                    {
                        progress = boolValue == requiredBoolValue ? 1f : 0f;
                    }
                    break;
                
                case EndingConditionType.TimeBased:
                    // This would require access to the time system
                    // For demonstration purposes, we'll return 0 for now
                    progress = 0f;
                    break;
                
                default:
                    Debug.LogWarning($"Unsupported condition type: {condition.Type}");
                    return 0f;
            }
            
            // Trigger the event if a condition is fully met and ending ID is provided
            if (progress >= 1f && !string.IsNullOrEmpty(endingId))
            {
                OnEndingConditionMet?.Invoke(endingId, condition.Description ?? condition.TargetId);
            }
            
            return progress;
        }
        
        private float EvaluateProgressForNumericCondition(float actual, ComparisonOperator op, float required)
        {
            switch (op)
            {
                case ComparisonOperator.Equal:
                    return actual == required ? 1f : 0f;
                
                case ComparisonOperator.NotEqual:
                    return actual != required ? 1f : 0f;
                
                case ComparisonOperator.GreaterThan:
                    return actual > required ? 1f : (actual / required);
                
                case ComparisonOperator.LessThan:
                    if (required <= 0)
                    {
                        return actual < required ? 1f : 0f;
                    }
                    else
                    {
                        return actual < required ? 1f : (required / actual);
                    }
                
                case ComparisonOperator.GreaterThanOrEqual:
                    return actual >= required ? 1f : (actual / required);
                
                case ComparisonOperator.LessThanOrEqual:
                    if (required <= 0)
                    {
                        return actual <= required ? 1f : 0f;
                    }
                    else
                    {
                        return actual <= required ? 1f : (required / actual);
                    }
                
                default:
                    return 0f;
            }
        }
        
        private float EvaluateConditionForStringValue(string actual, ComparisonOperator op, string required)
        {
            switch (op)
            {
                case ComparisonOperator.Equal:
                    return actual == required ? 1f : 0f;
                
                case ComparisonOperator.NotEqual:
                    return actual != required ? 1f : 0f;
                
                case ComparisonOperator.Contains:
                    return actual.Contains(required) ? 1f : 0f;
                
                case ComparisonOperator.NotContains:
                    return !actual.Contains(required) ? 1f : 0f;
                
                default:
                    return 0f;
            }
        }
        
        private bool AreConditionsMet(List<BranchCondition> conditions, ICharacter player, ICharacter npc)
        {
            foreach (var condition in conditions)
            {
                if (condition.IsCritical && !IsConditionMet(condition, player, npc))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private bool IsConditionMet(BranchCondition condition, ICharacter player, ICharacter npc)
        {
            // Similar to EvaluateConditionProgress but for branch conditions
            // Simplified implementation for brevity
            float progress = 0f;
            
            switch (condition.Type)
            {
                case EndingConditionType.RelationshipValue:
                    var relationshipValues = player.GetRelationshipValues(npc.Id);
                    if (!relationshipValues.TryGetValue(condition.ParameterId, out float value))
                    {
                        return false;
                    }
                    
                    progress = EvaluateProgressForNumericCondition(value, condition.Operator, Convert.ToSingle(condition.ThresholdValue));
                    break;
                
                case EndingConditionType.EventCompleted:
                    var completedEvents = player.GetCompletedEvents();
                    
                    if (condition.Operator == ComparisonOperator.Contains)
                    {
                        progress = completedEvents.Contains(condition.ParameterId) ? 1f : 0f;
                    }
                    else if (condition.Operator == ComparisonOperator.NotContains)
                    {
                        progress = !completedEvents.Contains(condition.ParameterId) ? 1f : 0f;
                    }
                    break;
                
                // Additional condition type handling
                // ...
            }
            
            return progress >= 1f;
        }
        
        private int CountMetConditions(List<BranchCondition> conditions, ICharacter player, ICharacter npc)
        {
            int count = 0;
            
            foreach (var condition in conditions)
            {
                if (IsConditionMet(condition, player, npc))
                {
                    count++;
                }
            }
            
            return count;
        }
        
        private string GetRelatedCharacterId(GameEnding ending)
        {
            // Look for character-related conditions
            foreach (var condition in ending.Conditions)
            {
                if (condition.Type == EndingConditionType.RelationshipValue && condition.TargetId.Contains(':'))
                {
                    string[] parts = condition.TargetId.Split(':');
                    return parts[0]; // Character ID
                }
            }
            
            // Look for ending data
            if (ending.EndingData != null && ending.EndingData.TryGetValue("CharacterId", out object characterId))
            {
                return characterId.ToString();
            }
            
            return null;
        }
        
        private bool IsTimeBasedEnding(GameEnding ending)
        {
            // Check if the ending has time-based conditions
            foreach (var condition in ending.Conditions)
            {
                if (condition.Type == EndingConditionType.TimeBased)
                {
                    return true;
                }
            }
            
            // Check ending data
            if (ending.EndingData != null && ending.EndingData.TryGetValue("IsTimeBased", out object isTimeBased))
            {
                return Convert.ToBoolean(isTimeBased);
            }
            
            return false;
        }
        
        private bool IsEndingApplicableAtDate(GameEnding ending, ICharacter player, GameDate currentDate)
        {
            // Check if the ending is time-based and applicable at the current date
            // This would require more specific implementation based on game requirements
            
            // For demonstration purposes, we'll check if the player has met all other conditions
            float progress = GetEndingProgress(ending.Id, player);
            
            // Time-based endings typically have a specific date or time period
            // Here we're assuming the ending data contains this information
            if (ending.EndingData != null)
            {
                if (ending.EndingData.TryGetValue("MinDate", out object minDateObj) && minDateObj is GameDate minDate)
                {
                    if (currentDate < minDate)
                    {
                        return false;
                    }
                }
                
                if (ending.EndingData.TryGetValue("MaxDate", out object maxDateObj) && maxDateObj is GameDate maxDate)
                {
                    if (currentDate > maxDate)
                    {
                        return false;
                    }
                }
            }
            
            // The ending is applicable if progress is high enough and date conditions are met
            return progress >= 0.5f;
        }
        
        private float CalculateEndingQuality(GameEnding ending, ICharacter player)
        {
            // Calculate quality based on various factors
            float baseQuality = ending.QualityLevel / 10f; // Assuming QualityLevel is 1-10
            float progressQuality = GetEndingProgress(ending.Id, player);
            
            // Additional factors could be included:
            // - Number of related character relationships maxed
            // - Special item collection rate
            // - Quest completion rate
            // - etc.
            
            return (baseQuality + progressQuality) / 2f;
        }
        
        private float CalculateCategoryCompletionRate(ICharacter player, EndingCategory category)
        {
            int total = 0;
            int achieved = 0;
            
            foreach (var ending in _endings.Values)
            {
                if (ending.Category == category)
                {
                    total++;
                    
                    if (_achievedEndings.TryGetValue(player.Id, out var playerAchievedEndings) &&
                        playerAchievedEndings.Contains(ending.Id))
                    {
                        achieved++;
                    }
                }
            }
            
            return total > 0 ? (float)achieved / total : 0f;
        }
        
        private float CalculateTypeCompletionRate(ICharacter player, EndingType type)
        {
            int total = 0;
            int achieved = 0;
            
            foreach (var ending in _endings.Values)
            {
                if (ending.Type == type)
                {
                    total++;
                    
                    if (_achievedEndings.TryGetValue(player.Id, out var playerAchievedEndings) &&
                        playerAchievedEndings.Contains(ending.Id))
                    {
                        achieved++;
                    }
                }
            }
            
            return total > 0 ? (float)achieved / total : 0f;
        }
        
        private float CalculateOverallCompletionRate(ICharacter player)
        {
            int total = _endings.Count;
            int achieved = _achievedEndings.TryGetValue(player.Id, out var playerAchievedEndings) ?
                          playerAchievedEndings.Count : 0;
            
            return total > 0 ? (float)achieved / total : 0f;
        }
        
        private void AchieveEnding(string playerId, string endingId)
        {
            if (!_achievedEndings.ContainsKey(playerId))
            {
                _achievedEndings[playerId] = new List<string>();
            }
            
            if (!_achievedEndings[playerId].Contains(endingId))
            {
                _achievedEndings[playerId].Add(endingId);
                
                // Update ending gallery
                UpdateEndingGallery(playerId, endingId);
                
                // Trigger ending unlocked event
                OnEndingUnlocked?.Invoke(endingId);
                
                // Unlock rewards
                UnlockEndingRewards(playerId, endingId);
                
                // Check for combinations
                CheckForEndingCombinations(playerId);
            }
        }
        
        private void UpdateEndingGallery(string playerId, string endingId)
        {
            // In a real implementation, this would update the UI or save data
            
            // Trigger gallery updated event
            OnEndingGalleryUpdated?.Invoke(endingId);
        }
        
        private void UnlockEndingRewards(string playerId, string endingId)
        {
            if (!_endings.TryGetValue(endingId, out var ending) || ending.UnlockedRewards == null)
            {
                return;
            }
            
            if (!_unlockedRewards.ContainsKey(playerId))
            {
                _unlockedRewards[playerId] = new List<string>();
            }
            
            foreach (var reward in ending.UnlockedRewards)
            {
                if (!_unlockedRewards[playerId].Contains(reward))
                {
                    _unlockedRewards[playerId].Add(reward);
                    
                    // In a real implementation, this would trigger reward unlocking in other systems
                    // For example: inventory.AddItem(reward);
                }
            }
        }
        
        private void CheckForEndingCombinations(string playerId)
        {
            if (!_achievedEndings.TryGetValue(playerId, out var achievedEndings) || achievedEndings.Count < 2)
            {
                return;
            }
            
            foreach (var combination in _combinations.Values)
            {
                bool allRequired = true;
                
                // Check if all required endings are achieved
                foreach (var requiredId in combination.RequiredEndingIds)
                {
                    if (!achievedEndings.Contains(requiredId))
                    {
                        allRequired = false;
                        break;
                    }
                }
                
                if (!allRequired)
                {
                    continue;
                }
                
                // Check if none of the excluded endings are achieved
                bool noExcluded = true;
                foreach (var excludedId in combination.ExcludedEndingIds)
                {
                    if (achievedEndings.Contains(excludedId))
                    {
                        noExcluded = false;
                        break;
                    }
                }
                
                if (noExcluded)
                {
                    // Trigger combination discovered event
                    OnEndingCombinationDiscovered?.Invoke(combination.Id);
                    
                    // If the combination has a special ending, achieve it
                    if (!string.IsNullOrEmpty(combination.CombinedEndingId) &&
                        !achievedEndings.Contains(combination.CombinedEndingId))
                    {
                        AchieveEnding(playerId, combination.CombinedEndingId);
                    }
                    
                    // Unlock combination rewards
                    UnlockCombinationRewards(playerId, combination);
                }
            }
        }
        
        private void UnlockCombinationRewards(string playerId, EndingCombination combination)
        {
            if (combination.UnlockedRewards == null)
            {
                return;
            }
            
            if (!_unlockedRewards.ContainsKey(playerId))
            {
                _unlockedRewards[playerId] = new List<string>();
            }
            
            foreach (var reward in combination.UnlockedRewards)
            {
                if (!_unlockedRewards[playerId].Contains(reward))
                {
                    _unlockedRewards[playerId].Add(reward);
                    
                    // In a real implementation, this would trigger reward unlocking in other systems
                }
            }
        }
        
        public void PlayEnding(string endingId, ICharacter player)
        {
            if (!_endings.TryGetValue(endingId, out var ending))
            {
                Debug.LogWarning($"Ending with ID {endingId} not found");
                return;
            }
            
            // In a real implementation, this would trigger the ending sequence
            // For example: SceneManager.LoadScene(ending.EndingSceneId);
            
            // Trigger ending played event
            OnEndingPlayed?.Invoke(endingId);
            
            // Ensure the ending is marked as achieved
            AchieveEnding(player.Id, endingId);
        }
        #endregion
    }
}