using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SocialActivity
{
    #region Enums
    public enum LocationType
    {
        Public,
        Special,
        Private,
        EventLimited
    }

    public enum TimeOfDay
    {
        EarlyMorning,  // 6:00-9:00
        Morning,       // 9:00-12:00
        Afternoon,     // 12:00-17:00
        Evening,       // 17:00-20:00
        Night,         // 20:00-24:00
        LateNight      // 0:00-6:00
    }

    public enum ActivityType
    {
        Social,
        Leisure,
        Cooperative,
        SpecialEvent
    }

    public enum PlayerSkill
    {
        Communication,
        Fitness,
        Intellect,
        Creativity,
        Cooking,
        Crafting,
        Music
    }

    public enum RelationshipParameter
    {
        Friendship,
        Romance,
        Trust,
        Respect,
        Intimacy
    }
    #endregion

    #region Core Interfaces
    /// <summary>
    /// Interface for location management system
    /// </summary>
    public interface ILocationSystem
    {
        List<GameLocation> GetAvailableLocations(TimeOfDay time);
        void TravelToLocation(ICharacter player, GameLocation location);
        List<ICharacter> GetNpcsAtLocation(GameLocation location, TimeOfDay time);
        List<ActivityOption> GetActivitiesAtLocation(GameLocation location, TimeOfDay time);
    }

    /// <summary>
    /// Interface for time management system
    /// </summary>
    public interface ITimeSystem
    {
        TimeOfDay GetCurrentTime();
        void AdvanceTime(int minutes);
        List<TimeSlot> GetAvailableTimeSlots();
        List<ScheduledActivity> GetScheduledActivities(ICharacter character, GameDate date);
    }

    /// <summary>
    /// Interface for activity management
    /// </summary>
    public interface IActivitySystem
    {
        List<Activity> GetAvailableActivities(ICharacter player, GameLocation location, TimeOfDay time);
        ActivityResult PerformActivity(ICharacter player, Activity activity, List<ICharacter> participants);
        List<ActivityRequirement> GetActivityRequirements(Activity activity);
        void ScheduleActivity(ICharacter player, Activity activity, GameDate date, List<ICharacter> invitees);
    }

    /// <summary>
    /// Interface for characters (both player and NPCs)
    /// </summary>
    public interface ICharacter
    {
        string Name { get; }
        Dictionary<PlayerSkill, float> Skills { get; }
        Dictionary<string, float> Traits { get; }
        List<ScheduledActivity> Schedule { get; }
        void ModifyRelationship(ICharacter target, RelationshipParameter parameter, float amount);
        float GetRelationshipValue(ICharacter target, RelationshipParameter parameter);
        void AddMemory(MemoryRecord memory);
        bool HasRequiredItems(List<string> requiredItems);
    }
    #endregion

    #region Data Structures
    /// <summary>
    /// Represents a date in the game world
    /// </summary>
    [Serializable]
    public struct GameDate
    {
        public int Year;
        public int Season; // 0-3 for Spring, Summer, Fall, Winter
        public int Day;    // 1-28 for each season
        public TimeOfDay TimeOfDay;

        public static bool operator ==(GameDate a, GameDate b)
        {
            return a.Year == b.Year && a.Season == b.Season && a.Day == b.Day && a.TimeOfDay == b.TimeOfDay;
        }

        public static bool operator !=(GameDate a, GameDate b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GameDate))
                return false;
            
            return this == (GameDate)obj;
        }

        public override int GetHashCode()
        {
            return Year.GetHashCode() ^ Season.GetHashCode() ^ Day.GetHashCode() ^ TimeOfDay.GetHashCode();
        }

        public override string ToString()
        {
            string[] seasons = { "Spring", "Summer", "Fall", "Winter" };
            return $"Year {Year}, {seasons[Season]} {Day}, {TimeOfDay}";
        }
    }

    /// <summary>
    /// Represents a game location
    /// </summary>
    [Serializable]
    public class GameLocation
    {
        public string Name;
        public string Description;
        public LocationType Type;
        public Vector2 MapPosition;
        public List<TimeSlot> OpeningHours;
        public List<Activity> AvailableActivities;
        public Dictionary<TimeOfDay, float> NpcSpawnRates;
        public List<LocationBonus> EnvironmentalBonuses;
        public float DiscoveryChance = 1.0f;
        public bool IsDiscovered = false;
        public int TravelTimeMinutes = 30;
        public List<string> TransportationOptions = new List<string>();
        public List<string> SpecialConversationTopics = new List<string>();

        public bool IsOpenAt(TimeOfDay time, DayOfWeek day)
        {
            return OpeningHours.Any(slot => slot.Day == day && 
                                         slot.StartTime <= time && 
                                         slot.EndTime >= time && 
                                         slot.IsAvailable);
        }

        public float GetEnvironmentalBonus(string bonusType)
        {
            var bonus = EnvironmentalBonuses.FirstOrDefault(b => b.BonusType == bonusType);
            return bonus != null ? bonus.BonusValue : 0f;
        }
    }

    /// <summary>
    /// Represents an activity option at a location
    /// </summary>
    [Serializable]
    public class ActivityOption
    {
        public Activity Activity;
        public bool IsAvailable;
        public string UnavailableReason;
        public int ParticipantCountMin = 1;
        public int ParticipantCountMax = 4;
    }

    /// <summary>
    /// Represents a complete activity definition
    /// </summary>
    [Serializable]
    public class Activity
    {
        public string Name;
        public string Description;
        public ActivityType Type;
        public int TimeCost;
        public int MoneyCost;
        public int FatigueCost;
        public List<ActivityRequirement> Requirements;
        public Dictionary<RelationshipParameter, float> RelationshipEffects;
        public Dictionary<PlayerSkill, float> SkillGainChances;
        public List<string> PossibleRewards = new List<string>();
        public List<TimeOfDay> AvailableTimes = new List<TimeOfDay>();
        public List<LocationType> CompatibleLocationTypes = new List<LocationType>();
        public float SuccessBaseProbability = 0.7f;
        public int RepeatCooldownDays = 0;
    }

    /// <summary>
    /// Represents a requirement for an activity
    /// </summary>
    [Serializable]
    public class ActivityRequirement
    {
        public enum RequirementType
        {
            Skill,
            Item,
            Relationship,
            Time,
            Location,
            Weather
        }

        public RequirementType Type;
        public string Parameter;
        public float MinValue;
        public bool IsMandatory;
        public string FailMessage;
    }

    /// <summary>
    /// Represents the result of an activity
    /// </summary>
    [Serializable]
    public class ActivityResult
    {
        public bool Success;
        public float EnjoymentLevel;
        public List<RelationshipChange> RelationshipChanges;
        public List<SkillGain> SkillGains;
        public List<MemoryRecord> GeneratedMemories;
        public List<ItemReceived> AcquiredItems;
        public string ResultMessage;
        public List<string> UnlockableContent = new List<string>();
    }

    /// <summary>
    /// Represents a change in relationship from an activity
    /// </summary>
    [Serializable]
    public class RelationshipChange
    {
        public ICharacter Target;
        public RelationshipParameter Parameter;
        public float Amount;
        public string Reason;
    }

    /// <summary>
    /// Represents a skill gain from an activity
    /// </summary>
    [Serializable]
    public class SkillGain
    {
        public PlayerSkill Skill;
        public float Amount;
    }

    /// <summary>
    /// Represents a memory record from an activity
    /// </summary>
    [Serializable]
    public class MemoryRecord
    {
        public string Description;
        public GameDate Date;
        public List<ICharacter> PeopleInvolved;
        public GameLocation Location;
        public Activity ActivityPerformed;
        public string Outcome;
        public float EmotionalImpact;
        public float MemoryStrength;
        public string MemoryImagePath;

        public override string ToString()
        {
            return $"{Date}: {Description} at {Location.Name}";
        }
    }

    /// <summary>
    /// Represents an item received from an activity
    /// </summary>
    [Serializable]
    public class ItemReceived
    {
        public string ItemId;
        public string ItemName;
        public int Quantity;
        public float Quality;
    }

    /// <summary>
    /// Represents a time slot for scheduling
    /// </summary>
    [Serializable]
    public struct TimeSlot
    {
        public DayOfWeek Day;
        public TimeOfDay StartTime;
        public TimeOfDay EndTime;
        public bool IsAvailable;

        public override string ToString()
        {
            return $"{Day} {StartTime} - {EndTime}";
        }
    }

    /// <summary>
    /// Represents a scheduled activity
    /// </summary>
    [Serializable]
    public class ScheduledActivity
    {
        public Activity ActivityToPerform;
        public GameDate ScheduledDate;
        public GameLocation Location;
        public List<ICharacter> Participants;
        public bool IsConfirmed;
        public float SuccessProbability;
        public string ScheduleNotes;
    }

    /// <summary>
    /// Represents an environmental bonus at a location
    /// </summary>
    [Serializable]
    public class LocationBonus
    {
        public string BonusType;
        public float BonusValue;
        public string Description;
    }
    #endregion

    #region Event Arguments
    public class LocationChangedEventArgs : EventArgs
    {
        public ICharacter Character;
        public GameLocation PreviousLocation;
        public GameLocation NewLocation;
        public int TravelTime;
    }

    public class TimeChangedEventArgs : EventArgs
    {
        public TimeOfDay PreviousTime;
        public TimeOfDay CurrentTime;
        public int MinutesPassed;
    }

    public class NpcEncounteredEventArgs : EventArgs
    {
        public ICharacter Player;
        public ICharacter EncounteredNpc;
        public GameLocation Location;
        public bool IsPlanned;
    }

    public class ActivityEventArgs : EventArgs
    {
        public ICharacter Initiator;
        public Activity Activity;
        public List<ICharacter> Participants;
        public GameLocation Location;
    }

    public class ActivityCompletedEventArgs : ActivityEventArgs
    {
        public ActivityResult Result;
    }

    public class LocationDiscoveredEventArgs : EventArgs
    {
        public ICharacter Discoverer;
        public GameLocation DiscoveredLocation;
        public string DiscoveryMethod;
    }

    public class ScheduleCreatedEventArgs : EventArgs
    {
        public ICharacter Organizer;
        public ScheduledActivity CreatedSchedule;
        public List<ICharacter> InvitedCharacters;
    }
    #endregion

    #region Implementation Classes
    /// <summary>
    /// Main manager class for the Social Activity System
    /// </summary>
    public class SocialActivitySystem : MonoBehaviour
    {
        #region Singleton
        private static SocialActivitySystem _instance;
        
        public static SocialActivitySystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SocialActivitySystem>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SocialActivitySystem");
                        _instance = go.AddComponent<SocialActivitySystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Component References
        [SerializeField] private LocationManager _locationManager;
        [SerializeField] private TimeManager _timeManager;
        [SerializeField] private ActivityManager _activityManager;
        #endregion
        
        #region Events
        public event EventHandler<LocationChangedEventArgs> OnLocationChanged;
        public event EventHandler<TimeChangedEventArgs> OnTimeChanged;
        public event EventHandler<NpcEncounteredEventArgs> OnNpcEncountered;
        public event EventHandler<ActivityEventArgs> OnActivityStarted;
        public event EventHandler<ActivityCompletedEventArgs> OnActivityCompleted;
        public event EventHandler<LocationDiscoveredEventArgs> OnSpecialLocationDiscovered;
        public event EventHandler<ScheduleCreatedEventArgs> OnScheduleCreated;
        #endregion
        
        #region Properties
        public ILocationSystem LocationSystem => _locationManager;
        public ITimeSystem TimeSystem => _timeManager;
        public IActivitySystem ActivitySystem => _activityManager;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize system components if they're not assigned
            if (_locationManager == null)
            {
                _locationManager = gameObject.AddComponent<LocationManager>();
            }
            
            if (_timeManager == null)
            {
                _timeManager = gameObject.AddComponent<TimeManager>();
            }
            
            if (_activityManager == null)
            {
                _activityManager = gameObject.AddComponent<ActivityManager>();
            }
        }
        
        private void Start()
        {
            // Subscribe to internal events
            _locationManager.LocationChanged += (sender, args) => OnLocationChanged?.Invoke(this, args);
            _timeManager.TimeChanged += (sender, args) => OnTimeChanged?.Invoke(this, args);
            _activityManager.ActivityStarted += (sender, args) => OnActivityStarted?.Invoke(this, args);
            _activityManager.ActivityCompleted += (sender, args) => OnActivityCompleted?.Invoke(this, args);
            _locationManager.LocationDiscovered += (sender, args) => OnSpecialLocationDiscovered?.Invoke(this, args);
            _activityManager.ScheduleCreated += (sender, args) => OnScheduleCreated?.Invoke(this, args);
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Get all locations available at the current time
        /// </summary>
        public List<GameLocation> GetAvailableLocations()
        {
            return _locationManager.GetAvailableLocations(_timeManager.GetCurrentTime());
        }
        
        /// <summary>
        /// Travel to a location with the specified character
        /// </summary>
        public void TravelToLocation(ICharacter character, GameLocation location)
        {
            // First check if the location is open at the current time
            if (!location.IsOpenAt(_timeManager.GetCurrentTime(), DateTime.Now.DayOfWeek))
            {
                Debug.LogWarning($"Location {location.Name} is closed at the current time.");
                return;
            }
            
            // Travel to the location
            _locationManager.TravelToLocation(character, location);
            
            // Check for NPC encounters at the new location
            CheckForNpcEncounters(character, location);
            
            // Advance time based on travel duration
            _timeManager.AdvanceTime(location.TravelTimeMinutes);
        }
        
        /// <summary>
        /// Get all activities available for the player at the current location and time
        /// </summary>
        public List<Activity> GetAvailableActivities(ICharacter player, GameLocation location)
        {
            return _activityManager.GetAvailableActivities(player, location, _timeManager.GetCurrentTime());
        }
        
        /// <summary>
        /// Perform an activity with a character and optional participants
        /// </summary>
        public ActivityResult PerformActivity(ICharacter player, Activity activity, List<ICharacter> participants = null)
        {
            if (participants == null)
            {
                participants = new List<ICharacter>();
            }
            
            GameLocation currentLocation = _locationManager.GetCharacterLocation(player);
            
            // Validate if the activity can be performed
            List<ActivityRequirement> requirements = _activityManager.GetActivityRequirements(activity);
            foreach (var req in requirements)
            {
                if (req.IsMandatory)
                {
                    // Check requirements (simplified for brevity)
                    switch (req.Type)
                    {
                        case ActivityRequirement.RequirementType.Skill:
                            if (!player.Skills.TryGetValue((PlayerSkill)Enum.Parse(typeof(PlayerSkill), req.Parameter), out float skillValue) || skillValue < req.MinValue)
                            {
                                Debug.LogWarning($"Player does not meet skill requirement: {req.Parameter} {req.MinValue}");
                                return new ActivityResult { 
                                    Success = false, 
                                    ResultMessage = req.FailMessage ?? $"You need {req.Parameter} level {req.MinValue} for this activity."
                                };
                            }
                            break;
                        
                        case ActivityRequirement.RequirementType.Item:
                            if (!player.HasRequiredItems(new List<string> { req.Parameter }))
                            {
                                Debug.LogWarning($"Player does not have required item: {req.Parameter}");
                                return new ActivityResult { 
                                    Success = false, 
                                    ResultMessage = req.FailMessage ?? $"You need {req.Parameter} for this activity."
                                };
                            }
                            break;
                    }
                }
            }
            
            // Perform the activity
            ActivityResult result = _activityManager.PerformActivity(player, activity, participants);
            
            // Advance time based on activity duration
            _timeManager.AdvanceTime(activity.TimeCost);
            
            // Process activity results
            ProcessActivityResult(player, activity, result, participants);
            
            return result;
        }
        
        /// <summary>
        /// Schedule an activity for a future date
        /// </summary>
        public void ScheduleActivity(ICharacter player, Activity activity, GameDate date, List<ICharacter> invitees)
        {
            _activityManager.ScheduleActivity(player, activity, date, invitees);
        }
        
        /// <summary>
        /// Check for any scheduled activities due at the current time
        /// </summary>
        public void CheckScheduledActivities(ICharacter player)
        {
            GameDate currentDate = _timeManager.GetCurrentGameDate();
            List<ScheduledActivity> activities = _timeManager.GetScheduledActivities(player, currentDate);
            
            foreach (var activity in activities)
            {
                if (activity.IsConfirmed)
                {
                    Debug.Log($"Starting scheduled activity: {activity.ActivityToPerform.Name}");
                    PerformActivity(player, activity.ActivityToPerform, activity.Participants);
                }
            }
        }
        
        /// <summary>
        /// Discover new locations based on current location and player skills
        /// </summary>
        public List<GameLocation> ExploreForNewLocations(ICharacter player)
        {
            GameLocation currentLocation = _locationManager.GetCharacterLocation(player);
            float explorationSkill = player.Skills.TryGetValue(PlayerSkill.Creativity, out float value) ? value : 0f;
            
            return _locationManager.DiscoverLocationsNear(currentLocation, explorationSkill);
        }
        #endregion
        
        #region Private Methods
        private void CheckForNpcEncounters(ICharacter character, GameLocation location)
        {
            List<ICharacter> npcsAtLocation = _locationManager.GetNpcsAtLocation(location, _timeManager.GetCurrentTime());
            
            foreach (var npc in npcsAtLocation)
            {
                // Determine if encounter happens (could be based on many factors)
                bool isEncounterPlanned = false; // This could be determined by schedules
                float encounterChance = 0.7f; // Base value, can be modified by relationship, etc.
                
                if (UnityEngine.Random.value < encounterChance)
                {
                    NpcEncounteredEventArgs args = new NpcEncounteredEventArgs
                    {
                        Player = character,
                        EncounteredNpc = npc,
                        Location = location,
                        IsPlanned = isEncounterPlanned
                    };
                    
                    OnNpcEncountered?.Invoke(this, args);
                }
            }
        }
        
        private void ProcessActivityResult(ICharacter player, Activity activity, ActivityResult result, List<ICharacter> participants)
        {
            if (!result.Success)
            {
                Debug.Log($"Activity {activity.Name} failed: {result.ResultMessage}");
                return;
            }
            
            // Apply relationship changes
            foreach (var change in result.RelationshipChanges)
            {
                player.ModifyRelationship(change.Target, change.Parameter, change.Amount);
            }
            
            // Apply skill gains
            foreach (var gain in result.SkillGains)
            {
                // This would be handled by the player's skill system
                Debug.Log($"Player gained {gain.Amount} points in {gain.Skill}");
            }
            
            // Record memories
            foreach (var memory in result.GeneratedMemories)
            {
                player.AddMemory(memory);
                
                // Also add memory to participants
                foreach (var participant in participants)
                {
                    participant.AddMemory(memory);
                }
            }
            
            // Process acquired items
            if (result.AcquiredItems.Count > 0)
            {
                Debug.Log($"Player received {result.AcquiredItems.Count} items from the activity");
                // This would call into the inventory system
            }
        }
        #endregion
    }

    /// <summary>
    /// Manages locations in the game world
    /// </summary>
    public class LocationManager : MonoBehaviour, ILocationSystem
    {
        #region Fields
        [SerializeField] private List<GameLocation> _allLocations = new List<GameLocation>();
        private Dictionary<ICharacter, GameLocation> _characterLocations = new Dictionary<ICharacter, GameLocation>();
        private Dictionary<GameLocation, List<ICharacter>> _locationCharacters = new Dictionary<GameLocation, List<ICharacter>>();
        #endregion
        
        #region Events
        public event EventHandler<LocationChangedEventArgs> LocationChanged;
        public event EventHandler<LocationDiscoveredEventArgs> LocationDiscovered;
        #endregion
        
        #region Properties
        public IReadOnlyList<GameLocation> AllLocations => _allLocations;
        #endregion
        
        #region Unity Lifecycle
        private void Start()
        {
            // Initialize location character maps
            foreach (var location in _allLocations)
            {
                _locationCharacters[location] = new List<ICharacter>();
            }
        }
        #endregion
        
        #region ILocationSystem Implementation
        /// <summary>
        /// Get all locations available at a specific time
        /// </summary>
        public List<GameLocation> GetAvailableLocations(TimeOfDay time)
        {
            DayOfWeek currentDay = DateTime.Now.DayOfWeek;
            
            return _allLocations
                .Where(l => l.IsDiscovered && l.IsOpenAt(time, currentDay))
                .ToList();
        }
        
        /// <summary>
        /// Travel to a location with a specific character
        /// </summary>
        public void TravelToLocation(ICharacter player, GameLocation location)
        {
            if (!_allLocations.Contains(location))
            {
                Debug.LogWarning($"Location {location.Name} does not exist in the system.");
                return;
            }
            
            GameLocation previousLocation = null;
            if (_characterLocations.ContainsKey(player))
            {
                previousLocation = _characterLocations[player];
                
                // Remove character from previous location
                if (_locationCharacters.ContainsKey(previousLocation))
                {
                    _locationCharacters[previousLocation].Remove(player);
                }
            }
            
            // Set new location for character
            _characterLocations[player] = location;
            
            // Add character to new location
            if (!_locationCharacters.ContainsKey(location))
            {
                _locationCharacters[location] = new List<ICharacter>();
            }
            _locationCharacters[location].Add(player);
            
            // Trigger location changed event
            LocationChangedEventArgs args = new LocationChangedEventArgs
            {
                Character = player,
                PreviousLocation = previousLocation,
                NewLocation = location,
                TravelTime = location.TravelTimeMinutes
            };
            
            LocationChanged?.Invoke(this, args);
        }
        
        /// <summary>
        /// Get all NPCs at a specific location and time
        /// </summary>
        public List<ICharacter> GetNpcsAtLocation(GameLocation location, TimeOfDay time)
        {
            if (!_locationCharacters.ContainsKey(location))
            {
                return new List<ICharacter>();
            }
            
            // Return a copy of the list to prevent external modifications
            return new List<ICharacter>(_locationCharacters[location]);
        }
        
        /// <summary>
        /// Get all activity options at a specific location and time
        /// </summary>
        public List<ActivityOption> GetActivitiesAtLocation(GameLocation location, TimeOfDay time)
        {
            if (!_allLocations.Contains(location))
            {
                return new List<ActivityOption>();
            }
            
            List<ActivityOption> options = new List<ActivityOption>();
            
            foreach (var activity in location.AvailableActivities)
            {
                bool isAvailable = activity.AvailableTimes.Contains(time);
                string unavailableReason = isAvailable ? "" : $"This activity is not available during {time}.";
                
                options.Add(new ActivityOption
                {
                    Activity = activity,
                    IsAvailable = isAvailable,
                    UnavailableReason = unavailableReason
                });
            }
            
            return options;
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Get the current location of a character
        /// </summary>
        public GameLocation GetCharacterLocation(ICharacter character)
        {
            return _characterLocations.TryGetValue(character, out GameLocation location) ? location : null;
        }
        
        /// <summary>
        /// Discover new locations near an existing location
        /// </summary>
        public List<GameLocation> DiscoverLocationsNear(GameLocation baseLocation, float explorationSkill)
        {
            List<GameLocation> discovered = new List<GameLocation>();
            
            // Calculate search radius based on exploration skill
            float searchRadius = 500f + (explorationSkill * 100f);
            
            foreach (var location in _allLocations.Where(l => !l.IsDiscovered))
            {
                // Calculate distance between locations
                float distance = Vector2.Distance(baseLocation.MapPosition, location.MapPosition);
                
                if (distance <= searchRadius)
                {
                    // Calculate discovery chance (based on distance, skill, and location's discovery chance)
                    float discoveryChance = location.DiscoveryChance * (1f - (distance / searchRadius)) * (0.5f + (explorationSkill / 10f));
                    
                    if (UnityEngine.Random.value < discoveryChance)
                    {
                        location.IsDiscovered = true;
                        discovered.Add(location);
                        
                        // Trigger discovery event
                        LocationDiscoveredEventArgs args = new LocationDiscoveredEventArgs
                        {
                            DiscoveredLocation = location,
                            DiscoveryMethod = "Exploration"
                        };
                        
                        LocationDiscovered?.Invoke(this, args);
                    }
                }
            }
            
            return discovered;
        }
        
        /// <summary>
        /// Add a custom location to the game world
        /// </summary>
        public void AddLocation(GameLocation location)
        {
            if (!_allLocations.Contains(location))
            {
                _allLocations.Add(location);
                _locationCharacters[location] = new List<ICharacter>();
            }
        }
        #endregion
    }

    /// <summary>
    /// Manages time and scheduling in the game world
    /// </summary>
    public class TimeManager : MonoBehaviour, ITimeSystem
    {
        #region Fields
        [SerializeField] private GameDate _currentDate;
        [SerializeField] private float _timeScale = 1f;
        [SerializeField] private int _minutesPerRealSecond = 10;
        
        private float _timeSinceLastUpdate = 0f;
        private Dictionary<ICharacter, List<ScheduledActivity>> _scheduledActivities = new Dictionary<ICharacter, List<ScheduledActivity>>();
        private Dictionary<DayOfWeek, List<TimeSlot>> _availableTimeSlotsByDay = new Dictionary<DayOfWeek, List<TimeSlot>>();
        #endregion
        
        #region Events
        public event EventHandler<TimeChangedEventArgs> TimeChanged;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Initialize default date if not set
            if (_currentDate.Equals(default(GameDate)))
            {
                _currentDate = new GameDate
                {
                    Year = 1,
                    Season = 0, // Spring
                    Day = 1,
                    TimeOfDay = TimeOfDay.Morning
                };
            }
            
            // Initialize available time slots for each day
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                _availableTimeSlotsByDay[day] = GenerateDefaultTimeSlotsForDay(day);
            }
        }
        
        private void Update()
        {
            if (_timeScale <= 0)
                return;
                
            _timeSinceLastUpdate += Time.deltaTime * _timeScale;
            
            // Calculate how many minutes have passed in game time
            int minutesPassed = Mathf.FloorToInt(_timeSinceLastUpdate * _minutesPerRealSecond);
            
            if (minutesPassed > 0)
            {
                _timeSinceLastUpdate -= (float)minutesPassed / _minutesPerRealSecond;
                AdvanceTime(minutesPassed);
            }
        }
        #endregion
        
        #region ITimeSystem Implementation
        /// <summary>
        /// Get the current time of day
        /// </summary>
        public TimeOfDay GetCurrentTime()
        {
            return _currentDate.TimeOfDay;
        }
        
        /// <summary>
        /// Advance game time by a number of minutes
        /// </summary>
        public void AdvanceTime(int minutes)
        {
            TimeOfDay previousTime = _currentDate.TimeOfDay;
            
            // Calculate time progression
            int currentTimeIndex = (int)_currentDate.TimeOfDay;
            int minutesPerTimeSlot = 180; // 3 hours per time slot
            
            int newTimeIndex = currentTimeIndex + (minutes / minutesPerTimeSlot);
            int daysToAdvance = newTimeIndex / 6; // 6 time slots per day
            
            newTimeIndex %= 6; // Wrap around to the correct time of day
            
            // Update date if day changed
            if (daysToAdvance > 0)
            {
                _currentDate.Day += daysToAdvance;
                
                // Handle season change
                if (_currentDate.Day > 28)
                {
                    _currentDate.Day = 1;
                    _currentDate.Season = (_currentDate.Season + 1) % 4;
                    
                    // Handle year change
                    if (_currentDate.Season == 0)
                    {
                        _currentDate.Year++;
                    }
                }
            }
            
            // Update time of day
            _currentDate.TimeOfDay = (TimeOfDay)newTimeIndex;
            
            // Trigger time changed event
            TimeChangedEventArgs args = new TimeChangedEventArgs
            {
                PreviousTime = previousTime,
                CurrentTime = _currentDate.TimeOfDay,
                MinutesPassed = minutes
            };
            
            TimeChanged?.Invoke(this, args);
        }
        
        /// <summary>
        /// Get all available time slots
        /// </summary>
        public List<TimeSlot> GetAvailableTimeSlots()
        {
            DayOfWeek currentDay = GetDayOfWeek();
            return _availableTimeSlotsByDay[currentDay].Where(slot => slot.IsAvailable).ToList();
        }
        
        /// <summary>
        /// Get all scheduled activities for a character on a specific date
        /// </summary>
        public List<ScheduledActivity> GetScheduledActivities(ICharacter character, GameDate date)
        {
            if (!_scheduledActivities.TryGetValue(character, out List<ScheduledActivity> activities))
            {
                return new List<ScheduledActivity>();
            }
            
            return activities.Where(a => a.ScheduledDate == date).ToList();
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Set the time scale for game time progression
        /// </summary>
        public void SetTimeScale(float scale)
        {
            _timeScale = Mathf.Max(0, scale);
        }
        
        /// <summary>
        /// Pause or resume game time
        /// </summary>
        public void PauseTime(bool pause)
        {
            _timeScale = pause ? 0 : 1;
        }
        
        /// <summary>
        /// Skip to a specific time of day
        /// </summary>
        public void SkipToTime(TimeOfDay targetTime)
        {
            TimeOfDay previousTime = _currentDate.TimeOfDay;
            
            // Calculate minutes to advance
            int currentTimeIndex = (int)_currentDate.TimeOfDay;
            int targetTimeIndex = (int)targetTime;
            
            // If target is earlier than current, add a full day
            if (targetTimeIndex <= currentTimeIndex)
            {
                targetTimeIndex += 6;
            }
            
            int timeSlotDifference = targetTimeIndex - currentTimeIndex;
            int minutesPerTimeSlot = 180; // 3 hours per time slot
            
            int minutesToAdvance = timeSlotDifference * minutesPerTimeSlot;
            
            // Advance the time
            AdvanceTime(minutesToAdvance);
        }
        
        /// <summary>
        /// Add a scheduled activity
        /// </summary>
        public void AddScheduledActivity(ICharacter character, ScheduledActivity activity)
        {
            if (!_scheduledActivities.ContainsKey(character))
            {
                _scheduledActivities[character] = new List<ScheduledActivity>();
            }
            
            _scheduledActivities[character].Add(activity);
        }
        
        /// <summary>
        /// Get the current day of week
        /// </summary>
        public DayOfWeek GetDayOfWeek()
        {
            // Calculate day of week based on total days passed
            int totalDays = (_currentDate.Year - 1) * 112 + _currentDate.Season * 28 + _currentDate.Day - 1;
            return (DayOfWeek)(totalDays % 7);
        }
        
        /// <summary>
        /// Get current game date
        /// </summary>
        public GameDate GetCurrentGameDate()
        {
            return _currentDate;
        }
        #endregion
        
        #region Private Methods
        private List<TimeSlot> GenerateDefaultTimeSlotsForDay(DayOfWeek day)
        {
            List<TimeSlot> slots = new List<TimeSlot>();
            
            // Generate standard time slots for each day
            foreach (TimeOfDay time in Enum.GetValues(typeof(TimeOfDay)))
            {
                TimeOfDay endTime = (TimeOfDay)(((int)time + 1) % 6);
                
                bool isAvailable = true;
                
                // Make late night slots unavailable on weekdays
                if (time == TimeOfDay.LateNight && day != DayOfWeek.Saturday && day != DayOfWeek.Sunday)
                {
                    isAvailable = false;
                }
                
                slots.Add(new TimeSlot
                {
                    Day = day,
                    StartTime = time,
                    EndTime = endTime,
                    IsAvailable = isAvailable
                });
            }
            
            return slots;
        }
        #endregion
    }

    /// <summary>
    /// Manages activities in the game world
    /// </summary>
    public class ActivityManager : MonoBehaviour, IActivitySystem
    {
        #region Fields
        [SerializeField] private List<Activity> _globalActivities = new List<Activity>();
        private Dictionary<string, DateTime> _lastPerformedActivities = new Dictionary<string, DateTime>();
        #endregion
        
        #region Events
        public event EventHandler<ActivityEventArgs> ActivityStarted;
        public event EventHandler<ActivityCompletedEventArgs> ActivityCompleted;
        public event EventHandler<ScheduleCreatedEventArgs> ScheduleCreated;
        #endregion
        
        #region IActivitySystem Implementation
        /// <summary>
        /// Get all available activities for a character at a specific location and time
        /// </summary>
        public List<Activity> GetAvailableActivities(ICharacter player, GameLocation location, TimeOfDay time)
        {
            if (location == null)
            {
                Debug.LogWarning("Cannot get activities for null location");
                return new List<Activity>();
            }
            
            // Combine global activities with location-specific ones
            List<Activity> allActivities = new List<Activity>(_globalActivities);
            allActivities.AddRange(location.AvailableActivities);
            
            return allActivities
                .Where(a => IsActivityAvailable(a, player, location, time))
                .ToList();
        }
        
        /// <summary>
        /// Perform an activity and get the result
        /// </summary>
        public ActivityResult PerformActivity(ICharacter player, Activity activity, List<ICharacter> participants)
        {
            // Record the time of activity for cooldown tracking
            string activityKey = $"{player.Name}_{activity.Name}";
            _lastPerformedActivities[activityKey] = DateTime.Now;
            
            // Trigger activity started event
            ActivityEventArgs startArgs = new ActivityEventArgs
            {
                Initiator = player,
                Activity = activity,
                Participants = participants,
                Location = null // This should be set to the current location
            };
            
            ActivityStarted?.Invoke(this, startArgs);
            
            // Calculate success probability
            float successProbability = CalculateSuccessProbability(player, activity, participants);
            bool isSuccess = UnityEngine.Random.value < successProbability;
            
            // Generate activity result
            ActivityResult result = GenerateActivityResult(player, activity, participants, isSuccess);
            
            // Trigger activity completed event
            ActivityCompletedEventArgs completedArgs = new ActivityCompletedEventArgs
            {
                Initiator = player,
                Activity = activity,
                Participants = participants,
                Location = null, // This should be set to the current location
                Result = result
            };
            
            ActivityCompleted?.Invoke(this, completedArgs);
            
            return result;
        }
        
        /// <summary>
        /// Get all requirements for an activity
        /// </summary>
        public List<ActivityRequirement> GetActivityRequirements(Activity activity)
        {
            return activity.Requirements;
        }
        
        /// <summary>
        /// Schedule an activity for a future date
        /// </summary>
        public void ScheduleActivity(ICharacter player, Activity activity, GameDate date, List<ICharacter> invitees)
        {
            TimeManager timeManager = GetComponent<TimeManager>();
            LocationManager locationManager = GetComponent<LocationManager>();
            
            // Get current location as default location for the activity
            GameLocation location = locationManager.GetCharacterLocation(player);
            
            // Create the scheduled activity
            ScheduledActivity scheduledActivity = new ScheduledActivity
            {
                ActivityToPerform = activity,
                ScheduledDate = date,
                Location = location,
                Participants = new List<ICharacter> { player },
                IsConfirmed = true,
                SuccessProbability = CalculateSuccessProbability(player, activity, invitees)
            };
            
            // Handle invitations
            foreach (var invitee in invitees)
            {
                float acceptanceChance = CalculateInviteAcceptanceProbability(player, invitee, activity);
                
                if (UnityEngine.Random.value < acceptanceChance)
                {
                    scheduledActivity.Participants.Add(invitee);
                }
            }
            
            // Add to the time manager's scheduled activities
            timeManager.AddScheduledActivity(player, scheduledActivity);
            
            // Trigger schedule created event
            ScheduleCreatedEventArgs args = new ScheduleCreatedEventArgs
            {
                Organizer = player,
                CreatedSchedule = scheduledActivity,
                InvitedCharacters = invitees
            };
            
            ScheduleCreated?.Invoke(this, args);
        }
        #endregion
        
        #region Private Methods
        private bool IsActivityAvailable(Activity activity, ICharacter player, GameLocation location, TimeOfDay time)
        {
            // Check if activity is available at this time
            if (!activity.AvailableTimes.Contains(time))
            {
                return false;
            }
            
            // Check if activity is compatible with location type
            if (!activity.CompatibleLocationTypes.Contains(location.Type))
            {
                return false;
            }
            
            // Check cooldown period
            string activityKey = $"{player.Name}_{activity.Name}";
            if (_lastPerformedActivities.TryGetValue(activityKey, out DateTime lastPerformed))
            {
                if (activity.RepeatCooldownDays > 0)
                {
                    TimeSpan elapsed = DateTime.Now - lastPerformed;
                    if (elapsed.TotalDays < activity.RepeatCooldownDays)
                    {
                        return false;
                    }
                }
            }
            
            // Check mandatory requirements
            foreach (var req in activity.Requirements)
            {
                if (req.IsMandatory)
                {
                    switch (req.Type)
                    {
                        case ActivityRequirement.RequirementType.Skill:
                            if (!player.Skills.TryGetValue((PlayerSkill)Enum.Parse(typeof(PlayerSkill), req.Parameter), out float skillValue) || skillValue < req.MinValue)
                            {
                                return false;
                            }
                            break;
                            
                        case ActivityRequirement.RequirementType.Item:
                            if (!player.HasRequiredItems(new List<string> { req.Parameter }))
                            {
                                return false;
                            }
                            break;
                            
                        case ActivityRequirement.RequirementType.Time:
                            // Time requirements are handled by availability check
                            break;
                            
                        case ActivityRequirement.RequirementType.Location:
                            // Location requirements are handled by compatibility check
                            break;
                    }
                }
            }
            
            return true;
        }
        
        private float CalculateSuccessProbability(ICharacter player, Activity activity, List<ICharacter> participants)
        {
            float baseProbability = activity.SuccessBaseProbability;
            float skillBonus = 0f;
            float relationshipBonus = 0f;
            
            // Skill bonus - based on relevant skills for the activity
            foreach (var skillPair in activity.SkillGainChances)
            {
                if (player.Skills.TryGetValue(skillPair.Key, out float skillValue))
                {
                    skillBonus += (skillValue / 100f) * skillPair.Value;
                }
            }
            
            // Relationship bonus - based on average relationship with participants
            if (participants.Count > 0)
            {
                float totalRelationship = 0f;
                
                foreach (var participant in participants)
                {
                    float relationshipValue = 0f;
                    
                    foreach (var relEffect in activity.RelationshipEffects)
                    {
                        relationshipValue += player.GetRelationshipValue(participant, relEffect.Key);
                    }
                    
                    totalRelationship += relationshipValue / activity.RelationshipEffects.Count;
                }
                
                relationshipBonus = (totalRelationship / participants.Count) / 100f * 0.2f;
            }
            
            // Calculate final probability
            float finalProbability = baseProbability + skillBonus + relationshipBonus;
            
            // Clamp between 0.1 and 0.95
            return Mathf.Clamp(finalProbability, 0.1f, 0.95f);
        }
        
        private float CalculateInviteAcceptanceProbability(ICharacter inviter, ICharacter invitee, Activity activity)
        {
            float baseProbability = 0.5f;
            float relationshipModifier = 0f;
            
            // Calculate relationship modifier
            float totalRelationship = 0f;
            int relationshipCount = 0;
            
            foreach (RelationshipParameter param in Enum.GetValues(typeof(RelationshipParameter)))
            {
                totalRelationship += invitee.GetRelationshipValue(inviter, param);
                relationshipCount++;
            }
            
            float averageRelationship = relationshipCount > 0 ? totalRelationship / relationshipCount : 0;
            relationshipModifier = averageRelationship / 100f * 0.4f;
            
            // Final probability calculation
            float finalProbability = baseProbability + relationshipModifier;
            
            // Clamp between 0.1 and 0.9
            return Mathf.Clamp(finalProbability, 0.1f, 0.9f);
        }
        
        private ActivityResult GenerateActivityResult(ICharacter player, Activity activity, List<ICharacter> participants, bool isSuccess)
        {
            ActivityResult result = new ActivityResult
            {
                Success = isSuccess,
                EnjoymentLevel = isSuccess ? UnityEngine.Random.Range(0.5f, 1f) : UnityEngine.Random.Range(0.1f, 0.4f),
                RelationshipChanges = new List<RelationshipChange>(),
                SkillGains = new List<SkillGain>(),
                GeneratedMemories = new List<MemoryRecord>(),
                AcquiredItems = new List<ItemReceived>(),
                ResultMessage = isSuccess ? $"You successfully completed {activity.Name}!" : $"You were unable to complete {activity.Name}.",
                UnlockableContent = new List<string>()
            };
            
            if (isSuccess)
            {
                // Generate relationship changes
                foreach (var participant in participants)
                {
                    foreach (var relEffect in activity.RelationshipEffects)
                    {
                        float effectValue = relEffect.Value * result.EnjoymentLevel;
                        
                        result.RelationshipChanges.Add(new RelationshipChange
                        {
                            Target = participant,
                            Parameter = relEffect.Key,
                            Amount = effectValue,
                            Reason = $"Enjoyed {activity.Name} together"
                        });
                    }
                }
                
                // Generate skill gains
                foreach (var skillPair in activity.SkillGainChances)
                {
                    if (UnityEngine.Random.value < skillPair.Value)
                    {
                        float gainAmount = UnityEngine.Random.Range(0.5f, 1f);
                        
                        result.SkillGains.Add(new SkillGain
                        {
                            Skill = skillPair.Key,
                            Amount = gainAmount
                        });
                    }
                }
                
                // Generate memory
                TimeManager timeManager = GetComponent<TimeManager>();
                LocationManager locationManager = GetComponent<LocationManager>();
                
                result.GeneratedMemories.Add(new MemoryRecord
                {
                    Description = $"Participated in {activity.Name}",
                    Date = timeManager.GetCurrentGameDate(),
                    PeopleInvolved = new List<ICharacter>(participants) { player },
                    Location = locationManager.GetCharacterLocation(player),
                    ActivityPerformed = activity,
                    Outcome = result.ResultMessage,
                    EmotionalImpact = result.EnjoymentLevel,
                    MemoryStrength = 0.8f
                });
                
                // Generate acquired items
                if (activity.PossibleRewards.Count > 0 && UnityEngine.Random.value < 0.3f)
                {
                    string randomReward = activity.PossibleRewards[UnityEngine.Random.Range(0, activity.PossibleRewards.Count)];
                    
                    result.AcquiredItems.Add(new ItemReceived
                    {
                        ItemId = randomReward,
                        ItemName = randomReward,
                        Quantity = 1,
                        Quality = UnityEngine.Random.Range(0.5f, 1f)
                    });
                }
            }
            
            return result;
        }
        #endregion
    }
    #endregion
}