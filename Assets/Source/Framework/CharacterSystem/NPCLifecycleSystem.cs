using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Interface for the Lifecycle System
    /// </summary>
    public interface ILifecycleSystem : ICharacterSubsystem
    {
        // Update based on the passage of time only
        void AdvanceTime(GameTime gameTime);
        
        // Retrieve the status of a specific character (referenced only by ID)
        CharacterStatusInfo GetCharacterStatus(string characterId);
        
        // Request a schedule change (returns success/failure)
        ScheduleModificationResult RequestScheduleModification(ScheduleModificationRequest request);
    }

    /// <summary>
    /// Represents game time for the lifecycle system
    /// </summary>
    [Serializable]
    public class GameTime
    {
        public enum DayType
        {
            Monday,
            Tuesday,
            Wednesday,
            Thursday,
            Friday,
            Saturday,
            Sunday,
            Holiday
        }

        public int day;        // Day number (increases from start of game)
        public DayType dayType; // Type of day (weekday, weekend, holiday)
        public int minutes;     // Minutes since start of day (0-1439)
        public string season;   // Current season

        // Helper properties
        public int Hours => minutes / 60;
        public int MinutesPastHour => minutes % 60;
        public string FormattedTime => $"{Hours:00}:{MinutesPastHour:00}";
        public bool IsWeekend => dayType == DayType.Saturday || dayType == DayType.Sunday;
        public bool IsHoliday => dayType == DayType.Holiday;
    }

    /// <summary>
    /// Information about a character's current status
    /// </summary>
    [Serializable]
    public class CharacterStatusInfo
    {
        public string characterId;
        public string currentActivity;
        public string currentLocation;
        public float energyLevel;
        public string healthStatus;
        public List<ScheduleItem> upcomingSchedule = new List<ScheduleItem>();
        public bool isAvailable;
        public Dictionary<string, object> additionalInfo = new Dictionary<string, object>();
    }

    /// <summary>
    /// Request to modify a character's schedule
    /// </summary>
    [Serializable]
    public class ScheduleModificationRequest
    {
        public string characterId;
        public string requesterId;  // Who is requesting the change
        public ScheduleItem newActivity;
        public TimeSlot timeSlot;
        public float priority;      // How important this request is (0-100)
        public string reason;
    }

    /// <summary>
    /// Result of a schedule modification request
    /// </summary>
    [Serializable]
    public class ScheduleModificationResult
    {
        public bool success;
        public string reason;
        public ScheduleItem conflictingItem; // If there was a conflict
        public List<ScheduleItem> newSchedule = new List<ScheduleItem>(); // Updated schedule if successful
    }

    /// <summary>
    /// Represents a time slot
    /// </summary>
    [Serializable]
    public class TimeSlot
    {
        public int start;  // Start time in minutes (0-1439)
        public int end;    // End time in minutes (0-1439)

        public TimeSlot(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        public bool Overlaps(TimeSlot other)
        {
            return (start < other.end && end > other.start);
        }

        public int Duration => end - start;
        
        public override string ToString()
        {
            return $"{start / 60:00}:{start % 60:00} - {end / 60:00}:{end % 60:00}";
        }
    }

    /// <summary>
    /// Represents an item in a schedule
    /// </summary>
    [Serializable]
    public class ScheduleItem
    {
        public TimeSlot timeSlot;
        public string activity;
        public string location;
        public float importance;     // Importance (0-100) - how difficult to change
        public ScheduleFlexibility flexibility;
        public List<string> associatedCharacters = new List<string>();

        public bool IsFlexible(int requestedTimeShift)
        {
            return Math.Abs(requestedTimeShift) <= flexibility.timeShift;
        }
    }

    /// <summary>
    /// Represents the flexibility of a schedule item
    /// </summary>
    [Serializable]
    public class ScheduleFlexibility
    {
        public int timeShift;        // Possible time shift range (minutes)
        public float skipProbability; // Skip probability (0-1)
    }

    /// <summary>
    /// Represents a daily routine for a character
    /// </summary>
    [Serializable]
    public class DailyRoutine
    {
        public string characterId;
        public Dictionary<string, List<ScheduleItem>> weekdaySchedules = new Dictionary<string, List<ScheduleItem>>();
        public Dictionary<int, List<ScheduleItem>> specialDaySchedules = new Dictionary<int, List<ScheduleItem>>();
        public Dictionary<string, List<ScheduleItem>> defaultActivities = new Dictionary<string, List<ScheduleItem>>();

        // Get the schedule for a specific day
        public List<ScheduleItem> GetScheduleForDay(GameTime.DayType dayType, int day)
        {
            // Check if there's a special schedule for this day
            if (specialDaySchedules.TryGetValue(day, out var specialSchedule))
            {
                return specialSchedule;
            }

            // Otherwise, use the weekday schedule
            string dayTypeName = dayType.ToString();
            if (weekdaySchedules.TryGetValue(dayTypeName, out var weekdaySchedule))
            {
                return weekdaySchedule;
            }

            // If no schedule is defined, return an empty list
            return new List<ScheduleItem>();
        }

        // Add an item to a day's schedule
        public void AddScheduleItem(GameTime.DayType dayType, ScheduleItem item)
        {
            string dayTypeName = dayType.ToString();
            if (!weekdaySchedules.ContainsKey(dayTypeName))
            {
                weekdaySchedules[dayTypeName] = new List<ScheduleItem>();
            }
            weekdaySchedules[dayTypeName].Add(item);
            
            // Sort by start time
            weekdaySchedules[dayTypeName] = weekdaySchedules[dayTypeName]
                .OrderBy(i => i.timeSlot.start)
                .ToList();
        }

        // Get schedule item at a specific time
        public ScheduleItem GetScheduleItemAt(GameTime gameTime)
        {
            var schedule = GetScheduleForDay(gameTime.dayType, gameTime.day);
            return schedule.FirstOrDefault(item => 
                gameTime.minutes >= item.timeSlot.start && 
                gameTime.minutes < item.timeSlot.end);
        }
    }

    /// <summary>
    /// Represents the health system of a character
    /// </summary>
    [Serializable]
    public class HealthSystem
    {
        public string characterId;
        public float baseHealth;
        public float currentHealth;
        public float energyLevel;
        public List<HealthCondition> conditions = new List<HealthCondition>();
        public float recoveryRate;
        public Dictionary<string, float> energyConsumptionRates = new Dictionary<string, float>();

        [Serializable]
        public class HealthCondition
        {
            public string type;
            public float severity;
            public float duration;
            public Dictionary<string, float> effects = new Dictionary<string, float>();
        }

        // Update health based on activity
        public void UpdateHealth(string activity, float duration)
        {
            // Apply energy consumption based on activity
            if (energyConsumptionRates.TryGetValue(activity, out float energyRate))
            {
                energyLevel -= energyRate * duration;
                energyLevel = Mathf.Clamp(energyLevel, 0, 100);
            }

            // Apply conditions effects
            foreach (var condition in conditions)
            {
                condition.duration -= duration;
                foreach (var effect in condition.effects)
                {
                    if (effect.Key == "energy")
                    {
                        energyLevel += effect.Value * duration;
                        energyLevel = Mathf.Clamp(energyLevel, 0, 100);
                    }
                    else if (effect.Key == "health")
                    {
                        currentHealth += effect.Value * duration;
                        currentHealth = Mathf.Clamp(currentHealth, 0, baseHealth);
                    }
                }
            }

            // Remove expired conditions
            conditions.RemoveAll(c => c.duration <= 0);

            // Recovery over time
            if (activity == "sleep" || activity == "rest")
            {
                energyLevel += recoveryRate * duration;
                energyLevel = Mathf.Clamp(energyLevel, 0, 100);
                
                currentHealth += (recoveryRate * 0.5f) * duration;
                currentHealth = Mathf.Clamp(currentHealth, 0, baseHealth);
            }
        }

        // Add a new health condition
        public void AddCondition(HealthCondition condition)
        {
            // Check if this condition type already exists
            var existingCondition = conditions.FirstOrDefault(c => c.type == condition.type);
            if (existingCondition != null)
            {
                // Update severity and duration if worse
                if (condition.severity > existingCondition.severity)
                {
                    existingCondition.severity = condition.severity;
                    existingCondition.duration = Mathf.Max(existingCondition.duration, condition.duration);
                    existingCondition.effects = condition.effects;
                }
                else
                {
                    existingCondition.duration = Mathf.Max(existingCondition.duration, condition.duration);
                }
            }
            else
            {
                conditions.Add(condition);
            }
        }

        // Get current health status as string
        public string GetHealthStatus()
        {
            if (conditions.Count == 0 && energyLevel > 70 && currentHealth > baseHealth * 0.9f)
            {
                return "Healthy";
            }
            else if (energyLevel < 30)
            {
                return "Tired";
            }
            else if (currentHealth < baseHealth * 0.5f)
            {
                return "Sick";
            }
            else if (conditions.Any(c => c.severity > 0.7f))
            {
                return conditions.OrderByDescending(c => c.severity).First().type;
            }
            else
            {
                return "Normal";
            }
        }
    }

    /// <summary>
    /// Represents a character's background situation
    /// </summary>
    [Serializable]
    public class BackgroundSituation
    {
        public string characterId;
        public List<Environment> environments = new List<Environment>();
        public List<Challenge> currentChallenges = new List<Challenge>();
        public List<LifeEvent> lifeEvents = new List<LifeEvent>();

        [Serializable]
        public class Environment
        {
            public string type;
            public Dictionary<string, object> status = new Dictionary<string, object>();
            public List<string> relatedCharacters = new List<string>();
            public float volatility;
        }

        [Serializable]
        public class Challenge
        {
            public string type;
            public float urgency;
            public float impact;
            public float progress;
            public float deadline;
        }

        [Serializable]
        public class LifeEvent
        {
            public string type;
            public Dictionary<string, object> triggerConditions = new Dictionary<string, object>();
            public float probability;
            public Dictionary<string, float> impact = new Dictionary<string, float>();
        }

        // Update the background situation
        public void Update(float deltaTime, GameTime gameTime)
        {
            // Update environments based on volatility
            foreach (var env in environments)
            {
                if (UnityEngine.Random.value < env.volatility * deltaTime * 0.01f)
                {
                    // Small random change to environment status
                    foreach (var statusKey in env.status.Keys.ToList())
                    {
                        if (env.status[statusKey] is float floatValue)
                        {
                            env.status[statusKey] = floatValue + (UnityEngine.Random.value * 0.2f - 0.1f);
                        }
                    }
                }
            }

            // Update challenges
            foreach (var challenge in currentChallenges.ToList())
            {
                if (gameTime.minutes >= challenge.deadline)
                {
                    // Challenge deadline reached
                    if (challenge.progress < 1.0f)
                    {
                        // Failed challenge - might trigger an event
                        var failEvent = lifeEvents.FirstOrDefault(e => e.type == $"{challenge.type}_failed");
                        if (failEvent != null && UnityEngine.Random.value < failEvent.probability)
                        {
                            // Apply impact
                            // (This would typically be sent as an event to the event bus)
                        }
                    }
                    
                    // Remove completed or failed challenges
                    currentChallenges.Remove(challenge);
                }
            }

            // Check for random life events
            foreach (var lifeEvent in lifeEvents)
            {
                if (UnityEngine.Random.value < lifeEvent.probability * deltaTime * 0.01f)
                {
                    bool conditionsMet = true;
                    foreach (var condition in lifeEvent.triggerConditions)
                    {
                        // Check if trigger conditions are met
                        // This is a simple check, real implementation would be more complex
                        if (condition.Value is float targetValue)
                        {
                            // Find relevant parameter in environments
                            bool foundParameter = false;
                            foreach (var env in environments)
                            {
                                if (env.status.TryGetValue(condition.Key, out object value) && value is float currentValue)
                                {
                                    foundParameter = true;
                                    if (currentValue < targetValue)
                                    {
                                        conditionsMet = false;
                                        break;
                                    }
                                }
                            }
                            
                            if (!foundParameter)
                            {
                                conditionsMet = false;
                                break;
                            }
                        }
                    }
                    
                    if (conditionsMet)
                    {
                        // Trigger life event
                        // (This would typically be sent as an event to the event bus)
                        Debug.Log($"Life event triggered: {lifeEvent.type} for character {characterId}");
                    }
                }
            }
        }

        // Add a new challenge
        public void AddChallenge(Challenge challenge)
        {
            currentChallenges.Add(challenge);
        }

        // Add a new life event
        public void AddLifeEvent(LifeEvent lifeEvent)
        {
            lifeEvents.Add(lifeEvent);
        }
    }

    /// <summary>
    /// Main implementation of the NPC Lifecycle System
    /// </summary>
    public class NPCLifecycleSystem : MonoBehaviour, ILifecycleSystem
    {
        private static NPCLifecycleSystem _instance;
        public static NPCLifecycleSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<NPCLifecycleSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("NPCLifecycleSystem");
                        _instance = go.AddComponent<NPCLifecycleSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Current game time
        [SerializeField] private GameTime _currentGameTime = new GameTime();
        public GameTime CurrentGameTime => _currentGameTime;

        // Dictionaries to store character data
        private Dictionary<string, DailyRoutine> _routines = new Dictionary<string, DailyRoutine>();
        private Dictionary<string, HealthSystem> _healthSystems = new Dictionary<string, HealthSystem>();
        private Dictionary<string, BackgroundSituation> _situations = new Dictionary<string, BackgroundSituation>();
        
        // Cache for character status
        private Dictionary<string, CharacterStatusInfo> _characterStatusCache = new Dictionary<string, CharacterStatusInfo>();
        
        // Notification event (can be subscribed to by UI or other systems)
        public delegate void CharacterStatusChangedEvent(string characterId, CharacterStatusInfo newStatus);
        public event CharacterStatusChangedEvent OnCharacterStatusChanged;
        
        // Health condition notification event
        public delegate void HealthConditionEvent(string characterId, string conditionType, float severity);
        public event HealthConditionEvent OnHealthConditionChanged;

        // Life event notification
        public delegate void LifeEventOccurredEvent(string characterId, string eventType, Dictionary<string, float> impact);
        public event LifeEventOccurredEvent OnLifeEventOccurred;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            // Real-time update (in a real game, this might be controlled by a time system)
            float deltaTime = Time.deltaTime;
            Update(deltaTime);
        }

        #region ICharacterSubsystem Implementation

        public void Initialize()
        {
            // Subscribe to character registration events
            // This allows the system to be notified when new characters are added
            
            // Initialize the game time
            _currentGameTime.day = 1;
            _currentGameTime.dayType = GameTime.DayType.Monday;
            _currentGameTime.minutes = 480; // 8:00 AM
            _currentGameTime.season = "Spring";
            
            // Subscribe to the event bus for relevant events
            CharacterEventBus.Instance.Subscribe("character_registered", OnCharacterRegistered);
            CharacterEventBus.Instance.Subscribe("character_interaction", OnCharacterInteraction);
            CharacterEventBus.Instance.Subscribe("game_time_changed", OnGameTimeChanged);
            
            Debug.Log("NPC Lifecycle System initialized");
        }

        public void Update(float deltaTime)
        {
            // Update game time
            UpdateGameTime(deltaTime);
            
            // Update all characters' health systems
            foreach (var characterId in _healthSystems.Keys.ToList())
            {
                UpdateCharacterHealth(characterId, deltaTime);
            }
            
            // Update all characters' background situations
            foreach (var characterId in _situations.Keys.ToList())
            {
                _situations[characterId].Update(deltaTime, _currentGameTime);
            }
            
            // Update status cache
            UpdateStatusCache();
        }

        public void Reset()
        {
            _routines.Clear();
            _healthSystems.Clear();
            _situations.Clear();
            _characterStatusCache.Clear();
            
            Initialize();
        }

        #endregion

        #region ILifecycleSystem Implementation

        public void AdvanceTime(GameTime gameTime)
        {
            // Set the game time directly
            _currentGameTime = gameTime;
            
            // Update all systems to reflect the new time
            UpdateStatusCache();
            
            // Notify of time change
            var timeEvent = new PsychologyEvent
            {
                eventType = "game_time_changed",
                parameters = { { "game_time", gameTime } }
            };
            CharacterEventBus.Instance.Publish(timeEvent);
        }

        public CharacterStatusInfo GetCharacterStatus(string characterId)
        {
            // Return cached status if available
            if (_characterStatusCache.TryGetValue(characterId, out var cachedStatus))
            {
                return cachedStatus;
            }
            
            // Otherwise, compute and cache the status
            var status = ComputeCharacterStatus(characterId);
            _characterStatusCache[characterId] = status;
            return status;
        }

        public ScheduleModificationResult RequestScheduleModification(ScheduleModificationRequest request)
        {
            // Create result object
            var result = new ScheduleModificationResult();
            
            // Validate request
            if (!_routines.TryGetValue(request.characterId, out var routine))
            {
                result.success = false;
                result.reason = "Character not found";
                return result;
            }
            
            // Get the current schedule
            var schedule = routine.GetScheduleForDay(_currentGameTime.dayType, _currentGameTime.day);
            
            // Check for conflicts
            var conflictingItems = schedule.Where(item => item.timeSlot.Overlaps(request.timeSlot))
                                          .OrderByDescending(item => item.importance)
                                          .ToList();
            
            if (conflictingItems.Count > 0)
            {
                var mostImportantConflict = conflictingItems[0];
                
                // If the conflicting item is more important, reject
                if (mostImportantConflict.importance > request.priority)
                {
                    result.success = false;
                    result.reason = $"Conflicts with more important activity: {mostImportantConflict.activity}";
                    result.conflictingItem = mostImportantConflict;
                    return result;
                }
                
                // If the conflicting item is flexible, try to shift it
                if (mostImportantConflict.IsFlexible(30)) // Try 30 min shift
                {
                    // Create a modified schedule with the item shifted
                    var shiftedSchedule = new List<ScheduleItem>(schedule);
                    shiftedSchedule.Remove(mostImportantConflict);
                    
                    // Create shifted item
                    var shiftedItem = new ScheduleItem
                    {
                        activity = mostImportantConflict.activity,
                        location = mostImportantConflict.location,
                        importance = mostImportantConflict.importance,
                        flexibility = mostImportantConflict.flexibility,
                        associatedCharacters = mostImportantConflict.associatedCharacters
                    };
                    
                    // Shift by 30 min later
                    shiftedItem.timeSlot = new TimeSlot(
                        mostImportantConflict.timeSlot.start + 30,
                        mostImportantConflict.timeSlot.end + 30
                    );
                    
                    // Add the shifted item
                    shiftedSchedule.Add(shiftedItem);
                    
                    // Add the new activity
                    var newItem = new ScheduleItem
                    {
                        activity = request.newActivity.activity,
                        location = request.newActivity.location,
                        importance = request.priority,
                        flexibility = request.newActivity.flexibility,
                        associatedCharacters = request.newActivity.associatedCharacters,
                        timeSlot = request.timeSlot
                    };
                    shiftedSchedule.Add(newItem);
                    
                    // Sort by start time
                    shiftedSchedule = shiftedSchedule.OrderBy(i => i.timeSlot.start).ToList();
                    
                    // Update the schedule
                    routine.weekdaySchedules[_currentGameTime.dayType.ToString()] = shiftedSchedule;
                    
                    // Success!
                    result.success = true;
                    result.newSchedule = shiftedSchedule;
                    return result;
                }
                else if (mostImportantConflict.flexibility.skipProbability > 0.7f)
                {
                    // High skip probability - can skip this item
                    var modifiedSchedule = new List<ScheduleItem>(schedule);
                    modifiedSchedule.Remove(mostImportantConflict);
                    
                    // Add the new activity
                    var newItem = new ScheduleItem
                    {
                        activity = request.newActivity.activity,
                        location = request.newActivity.location,
                        importance = request.priority,
                        flexibility = request.newActivity.flexibility,
                        associatedCharacters = request.newActivity.associatedCharacters,
                        timeSlot = request.timeSlot
                    };
                    modifiedSchedule.Add(newItem);
                    
                    // Sort by start time
                    modifiedSchedule = modifiedSchedule.OrderBy(i => i.timeSlot.start).ToList();
                    
                    // Update the schedule
                    routine.weekdaySchedules[_currentGameTime.dayType.ToString()] = modifiedSchedule;
                    
                    // Success!
                    result.success = true;
                    result.newSchedule = modifiedSchedule;
                    return result;
                }
                else
                {
                    // Cannot modify the schedule
                    result.success = false;
                    result.reason = $"Conflicting activity {mostImportantConflict.activity} cannot be changed or skipped";
                    result.conflictingItem = mostImportantConflict;
                    return result;
                }
            }
            else
            {
                // No conflicts, can add directly
                var newItem = new ScheduleItem
                {
                    activity = request.newActivity.activity,
                    location = request.newActivity.location,
                    importance = request.priority,
                    flexibility = request.newActivity.flexibility,
                    associatedCharacters = request.newActivity.associatedCharacters,
                    timeSlot = request.timeSlot
                };
                
                // Add to schedule
                schedule.Add(newItem);
                
                // Sort by start time
                schedule = schedule.OrderBy(i => i.timeSlot.start).ToList();
                
                // Update the schedule
                routine.weekdaySchedules[_currentGameTime.dayType.ToString()] = schedule;
                
                // Success!
                result.success = true;
                result.newSchedule = schedule;
                
                // Notify of schedule change
                NotifyScheduleChanged(request.characterId, newItem);
                
                return result;
            }
        }

        #endregion

        #region Public API Methods

        // Get a character's scheduled activity at a specific time
        public ScheduleItem GetScheduledActivity(string characterId, GameTime gameTime)
        {
            if (_routines.TryGetValue(characterId, out var routine))
            {
                var schedule = routine.GetScheduleForDay(gameTime.dayType, gameTime.day);
                return schedule.FirstOrDefault(item => 
                    gameTime.minutes >= item.timeSlot.start && 
                    gameTime.minutes < item.timeSlot.end);
            }
            return null;
        }

        // Request a schedule change
        public bool RequestScheduleChange(string characterId, ScheduleItem newActivity, TimeSlot timeSlot)
        {
            var request = new ScheduleModificationRequest
            {
                characterId = characterId,
                requesterId = "player", // Assuming player is making the request
                newActivity = newActivity,
                timeSlot = timeSlot,
                priority = 75, // Default priority for player requests
                reason = "Player request"
            };
            
            var result = RequestScheduleModification(request);
            return result.success;
        }

        // Get a character's current health status
        public string GetCurrentHealthStatus(string characterId)
        {
            if (_healthSystems.TryGetValue(characterId, out var healthSystem))
            {
                return healthSystem.GetHealthStatus();
            }
            return "Unknown";
        }

        // Update a character's energy level
        public void UpdateEnergyLevel(string characterId, string activityType, float duration)
        {
            if (_healthSystems.TryGetValue(characterId, out var healthSystem))
            {
                healthSystem.UpdateHealth(activityType, duration);
                
                // Notify of health change
                NotifyHealthChanged(characterId, healthSystem);
            }
        }

        // Update a character's background situation
        public void UpdateBackgroundSituation(string characterId, string environmentType, Dictionary<string, object> changes)
        {
            if (_situations.TryGetValue(characterId, out var situation))
            {
                var env = situation.environments.FirstOrDefault(e => e.type == environmentType);
                if (env != null)
                {
                    foreach (var change in changes)
                    {
                        env.status[change.Key] = change.Value;
                    }
                }
            }
        }

        // Check for life events
        public List<string> CheckForLifeEvents(string characterId, Dictionary<string, object> currentGameContext)
        {
            var triggeredEvents = new List<string>();
            
            if (_situations.TryGetValue(characterId, out var situation))
            {
                foreach (var lifeEvent in situation.lifeEvents)
                {
                    bool conditionsMet = true;
                    foreach (var condition in lifeEvent.triggerConditions)
                    {
                        if (currentGameContext.TryGetValue(condition.Key, out var contextValue))
                        {
                            // Simple string comparison for this example
                            if (contextValue.ToString() != condition.Value.ToString())
                            {
                                conditionsMet = false;
                                break;
                            }
                        }
                        else
                        {
                            conditionsMet = false;
                            break;
                        }
                    }
                    
                    if (conditionsMet && UnityEngine.Random.value < lifeEvent.probability)
                    {
                        triggeredEvents.Add(lifeEvent.type);
                        
                        // Notify of life event
                        OnLifeEventOccurred?.Invoke(characterId, lifeEvent.type, lifeEvent.impact);
                    }
                }
            }
            
            return triggeredEvents;
        }

        // Get a character's future schedule
        public List<ScheduleItem> GetFutureSchedule(string characterId, int daysAhead)
        {
            var futureSchedule = new List<ScheduleItem>();
            
            if (_routines.TryGetValue(characterId, out var routine))
            {
                // Current day
                var currentDayType = _currentGameTime.dayType;
                var currentDay = _currentGameTime.day;
                
                // Add current day's remaining schedule
                var todaySchedule = routine.GetScheduleForDay(currentDayType, currentDay);
                var remainingItems = todaySchedule.Where(item => item.timeSlot.start >= _currentGameTime.minutes).ToList();
                futureSchedule.AddRange(remainingItems);
                
                // Add future days
                for (int i = 1; i <= daysAhead; i++)
                {
                    var futureDay = currentDay + i;
                    var futureDayType = (GameTime.DayType)(((int)currentDayType + i) % 7);
                    
                    var daySchedule = routine.GetScheduleForDay(futureDayType, futureDay);
                    futureSchedule.AddRange(daySchedule);
                }
            }
            
            return futureSchedule;
        }

        // Create/register a new daily routine
        public void RegisterDailyRoutine(DailyRoutine routine)
        {
            _routines[routine.characterId] = routine;
        }

        // Register a new health system
        public void RegisterHealthSystem(HealthSystem healthSystem)
        {
            _healthSystems[healthSystem.characterId] = healthSystem;
        }

        // Register a new background situation
        public void RegisterBackgroundSituation(BackgroundSituation situation)
        {
            _situations[situation.characterId] = situation;
        }

        #endregion

        #region Private Helper Methods

        private void UpdateGameTime(float deltaTime)
        {
            // Update minutes (each real second might be multiple game minutes)
            float gameMinutesPerRealSecond = 10f; // Configurable rate
            int minutesToAdd = Mathf.FloorToInt(deltaTime * gameMinutesPerRealSecond);
            
            if (minutesToAdd > 0)
            {
                _currentGameTime.minutes += minutesToAdd;
                
                // Handle day change
                if (_currentGameTime.minutes >= 1440) // 24 hours * 60 minutes
                {
                    _currentGameTime.minutes %= 1440;
                    _currentGameTime.day++;
                    
                    // Update day type
                    int dayTypeInt = (int)_currentGameTime.dayType;
                    dayTypeInt = (dayTypeInt + 1) % 7;
                    _currentGameTime.dayType = (GameTime.DayType)dayTypeInt;
                    
                    // TODO: Handle season changes based on day count
                }
                
                // Notify time change via event bus
                var timeEvent = new PsychologyEvent
                {
                    eventType = "game_time_changed",
                    parameters = { { "game_time", _currentGameTime } }
                };
                CharacterEventBus.Instance.Publish(timeEvent);
            }
        }

        private void UpdateCharacterHealth(string characterId, float deltaTime)
        {
            if (_healthSystems.TryGetValue(characterId, out var healthSystem) &&
                _routines.TryGetValue(characterId, out var routine))
            {
                // Get current activity
                var currentScheduleItem = routine.GetScheduleItemAt(_currentGameTime);
                string currentActivity = currentScheduleItem?.activity ?? "idle";
                
                // Update health based on current activity
                healthSystem.UpdateHealth(currentActivity, deltaTime);
                
                // Check for health condition notifications
                foreach (var condition in healthSystem.conditions)
                {
                    if (condition.severity > 0.7f) // High severity
                    {
                        OnHealthConditionChanged?.Invoke(characterId, condition.type, condition.severity);
                    }
                }
            }
        }

        private void UpdateStatusCache()
        {
            foreach (var characterId in _routines.Keys.ToList())
            {
                _characterStatusCache[characterId] = ComputeCharacterStatus(characterId);
            }
        }

        private CharacterStatusInfo ComputeCharacterStatus(string characterId)
        {
            var status = new CharacterStatusInfo
            {
                characterId = characterId
            };
            
            // Get current scheduled activity
            if (_routines.TryGetValue(characterId, out var routine))
            {
                var currentItem = routine.GetScheduleItemAt(_currentGameTime);
                if (currentItem != null)
                {
                    status.currentActivity = currentItem.activity;
                    status.currentLocation = currentItem.location;
                }
                else
                {
                    status.currentActivity = "idle";
                    status.currentLocation = "unknown";
                }
                
                // Get upcoming schedule (next 5 items)
                var schedule = routine.GetScheduleForDay(_currentGameTime.dayType, _currentGameTime.day);
                var upcomingItems = schedule
                    .Where(item => item.timeSlot.start > _currentGameTime.minutes)
                    .OrderBy(item => item.timeSlot.start)
                    .Take(5)
                    .ToList();
                
                status.upcomingSchedule = upcomingItems;
            }
            
            // Get health status
            if (_healthSystems.TryGetValue(characterId, out var healthSystem))
            {
                status.energyLevel = healthSystem.energyLevel;
                status.healthStatus = healthSystem.GetHealthStatus();
            }
            
            // Determine availability
            status.isAvailable = IsCharacterAvailable(characterId);
            
            return status;
        }

        private bool IsCharacterAvailable(string characterId)
        {
            // A character is available if:
            // 1. They have no current scheduled activity OR
            // 2. Their current activity has high flexibility (can be interrupted)
            
            if (_routines.TryGetValue(characterId, out var routine))
            {
                var currentItem = routine.GetScheduleItemAt(_currentGameTime);
                if (currentItem == null)
                {
                    return true; // No scheduled activity
                }
                
                if (currentItem.flexibility.skipProbability > 0.7f)
                {
                    return true; // High flexibility, can be interrupted
                }
                
                if (currentItem.importance < 50)
                {
                    return true; // Low importance activity, can be interrupted
                }
            }
            
            return false;
        }

        private void NotifyScheduleChanged(string characterId, ScheduleItem newItem)
        {
            // Update status cache
            _characterStatusCache[characterId] = ComputeCharacterStatus(characterId);
            
            // Notify listeners
            OnCharacterStatusChanged?.Invoke(characterId, _characterStatusCache[characterId]);
            
            // Publish event to event bus
            var scheduleEvent = new PsychologyEvent
            {
                characterId = characterId,
                eventType = "schedule_changed",
                parameters =
                {
                    { "activity", newItem.activity },
                    { "location", newItem.location },
                    { "start_time", newItem.timeSlot.start },
                    { "end_time", newItem.timeSlot.end }
                }
            };
            CharacterEventBus.Instance.Publish(scheduleEvent);
        }

        private void NotifyHealthChanged(string characterId, HealthSystem healthSystem)
        {
            // Update status cache
            _characterStatusCache[characterId] = ComputeCharacterStatus(characterId);
            
            // Notify listeners
            OnCharacterStatusChanged?.Invoke(characterId, _characterStatusCache[characterId]);
            
            // Publish event to event bus
            var healthEvent = new PsychologyEvent
            {
                characterId = characterId,
                eventType = "health_changed",
                parameters =
                {
                    { "health_status", healthSystem.GetHealthStatus() },
                    { "energy_level", healthSystem.energyLevel },
                    { "current_health", healthSystem.currentHealth }
                }
            };
            CharacterEventBus.Instance.Publish(healthEvent);
        }

        #endregion

        #region Event Handlers

        private void OnCharacterRegistered(PsychologyEvent evt)
        {
            string characterId = evt.characterId;
            
            // Create default systems if not already registered
            if (!_routines.ContainsKey(characterId))
            {
                var routine = new DailyRoutine { characterId = characterId };
                _routines[characterId] = routine;
            }
            
            if (!_healthSystems.ContainsKey(characterId))
            {
                var healthSystem = new HealthSystem
                {
                    characterId = characterId,
                    baseHealth = 100f,
                    currentHealth = 100f,
                    energyLevel = 100f,
                    recoveryRate = 2f,
                    energyConsumptionRates = new Dictionary<string, float>
                    {
                        { "idle", 0.1f },
                        { "walk", 0.3f },
                        { "run", 0.7f },
                        { "work", 0.5f },
                        { "sleep", -0.8f }, // Negative to indicate recovery
                        { "eat", -0.3f }
                    }
                };
                _healthSystems[characterId] = healthSystem;
            }
            
            if (!_situations.ContainsKey(characterId))
            {
                var situation = new BackgroundSituation { characterId = characterId };
                _situations[characterId] = situation;
            }
            
            // Update the status cache
            _characterStatusCache[characterId] = ComputeCharacterStatus(characterId);
        }

        private void OnCharacterInteraction(PsychologyEvent evt)
        {
            string characterId = evt.characterId;
            string interactionType = evt.parameters.TryGetValue("interaction_type", out object type) ? type.ToString() : "";
            
            // Handle different interaction types
            switch (interactionType)
            {
                case "request_schedule_change":
                    // Extract parameters and handle schedule change request
                    if (evt.parameters.TryGetValue("new_activity", out object activityObj) && 
                        activityObj is ScheduleItem newActivity &&
                        evt.parameters.TryGetValue("time_slot", out object timeSlotObj) &&
                        timeSlotObj is TimeSlot timeSlot)
                    {
                        RequestScheduleChange(characterId, newActivity, timeSlot);
                    }
                    break;
                    
                case "health_effect":
                    // Extract parameters and handle health effect
                    if (evt.parameters.TryGetValue("condition_type", out object conditionTypeObj) &&
                        evt.parameters.TryGetValue("severity", out object severityObj) &&
                        evt.parameters.TryGetValue("duration", out object durationObj) &&
                        float.TryParse(severityObj.ToString(), out float severity) &&
                        float.TryParse(durationObj.ToString(), out float duration))
                    {
                        string conditionType = conditionTypeObj.ToString();
                        var condition = new HealthSystem.HealthCondition
                        {
                            type = conditionType,
                            severity = severity,
                            duration = duration,
                            effects = new Dictionary<string, float>()
                        };
                        
                        // Extract effects if available
                        if (evt.parameters.TryGetValue("effects", out object effectsObj) &&
                            effectsObj is Dictionary<string, float> effects)
                        {
                            condition.effects = effects;
                        }
                        
                        if (_healthSystems.TryGetValue(characterId, out var healthSystem))
                        {
                            healthSystem.AddCondition(condition);
                            NotifyHealthChanged(characterId, healthSystem);
                        }
                    }
                    break;
                    
                case "add_challenge":
                    // Extract parameters and handle adding a challenge
                    if (evt.parameters.TryGetValue("challenge_type", out object challengeTypeObj) &&
                        evt.parameters.TryGetValue("urgency", out object urgencyObj) &&
                        evt.parameters.TryGetValue("impact", out object impactObj) &&
                        evt.parameters.TryGetValue("deadline", out object deadlineObj) &&
                        float.TryParse(urgencyObj.ToString(), out float urgency) &&
                        float.TryParse(impactObj.ToString(), out float impact) &&
                        float.TryParse(deadlineObj.ToString(), out float deadline))
                    {
                        string challengeType = challengeTypeObj.ToString();
                        var challenge = new BackgroundSituation.Challenge
                        {
                            type = challengeType,
                            urgency = urgency,
                            impact = impact,
                            progress = 0f,
                            deadline = deadline
                        };
                        
                        if (_situations.TryGetValue(characterId, out var situation))
                        {
                            situation.AddChallenge(challenge);
                        }
                    }
                    break;
            }
        }

        private void OnGameTimeChanged(PsychologyEvent evt)
        {
            // This is for handling external time changes
            if (evt.parameters.TryGetValue("game_time", out object gameTimeObj) &&
                gameTimeObj is GameTime newGameTime)
            {
                _currentGameTime = newGameTime;
                UpdateStatusCache();
            }
        }

        #endregion
    }
}