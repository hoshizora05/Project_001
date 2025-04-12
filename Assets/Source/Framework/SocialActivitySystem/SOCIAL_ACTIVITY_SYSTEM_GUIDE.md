# Social Activity System - Usage Guide

This guide explains how to implement and use the Social Activity System in your Unity project. The system enables players to interact with various locations and NPCs, participate in activities, and build relationships through a time-based gameplay framework.

## Table of Contents
1. [System Overview](#system-overview)
2. [Getting Started](#getting-started)
3. [Location Management](#location-management)
4. [Time System](#time-system)
5. [Activities System](#activities-system)
6. [Character Integration](#character-integration)
7. [Events and Callbacks](#events-and-callbacks)
8. [Common Use Cases](#common-use-cases)
9. [Example Implementation](#example-implementation)

## System Overview

The Social Activity System consists of three main components:

- **Location System**: Manages game locations, travel between locations, and NPC presence
- **Time System**: Handles game time, scheduling, and time-based changes in the world
- **Activity System**: Manages available activities, their requirements, and outcomes

These components work together to create a dynamic social simulation where players can visit locations, interact with NPCs, and participate in various activities.

## Getting Started

### 1. Setup the System

Add the `SocialActivitySystem` component to a GameObject in your scene:

```csharp
// Create a new GameObject for the system
GameObject systemObject = new GameObject("SocialActivitySystem");
SocialActivitySystem socialSystem = systemObject.AddComponent<SocialActivitySystem>();
DontDestroyOnLoad(systemObject);
```

Alternatively, you can reference the system from the Unity Inspector by creating a serialized field:

```csharp
[SerializeField] private SocialActivitySystem _socialSystem;
```

### 2. Access the System Components

The `SocialActivitySystem` provides access to its three main components:

```csharp
// Get references to the subsystems
ILocationSystem locationSystem = SocialActivitySystem.Instance.LocationSystem;
ITimeSystem timeSystem = SocialActivitySystem.Instance.TimeSystem;
IActivitySystem activitySystem = SocialActivitySystem.Instance.ActivitySystem;
```

### 3. Implement Required Interfaces

Your player and NPC classes must implement the `ICharacter` interface to interact with the system:

```csharp
public class PlayerCharacter : MonoBehaviour, ICharacter
{
    public string Name { get; private set; }
    public Dictionary<PlayerSkill, float> Skills { get; private set; }
    public Dictionary<string, float> Traits { get; private set; }
    public List<ScheduledActivity> Schedule { get; private set; }
    
    // Implement interface methods
    public void ModifyRelationship(ICharacter target, RelationshipParameter parameter, float amount) { /* ... */ }
    public float GetRelationshipValue(ICharacter target, RelationshipParameter parameter) { /* ... */ }
    public void AddMemory(MemoryRecord memory) { /* ... */ }
    public bool HasRequiredItems(List<string> requiredItems) { /* ... */ }
}
```

## Location Management

### Creating Locations

Define game locations with their properties:

```csharp
GameLocation parkLocation = new GameLocation
{
    Name = "Central Park",
    Description = "A peaceful urban park with walking paths and green spaces.",
    Type = LocationType.Public,
    MapPosition = new Vector2(100, 200),
    OpeningHours = new List<TimeSlot>
    {
        new TimeSlot { Day = DayOfWeek.Monday, StartTime = TimeOfDay.Morning, EndTime = TimeOfDay.Evening, IsAvailable = true },
        // Add other days...
    },
    AvailableActivities = new List<Activity>(),
    NpcSpawnRates = new Dictionary<TimeOfDay, float>
    {
        { TimeOfDay.Morning, 0.7f },
        { TimeOfDay.Afternoon, 0.9f },
        { TimeOfDay.Evening, 0.5f },
        // Other time periods...
    },
    EnvironmentalBonuses = new List<LocationBonus>
    {
        new LocationBonus { BonusType = "RelaxationEfficiency", BonusValue = 0.2f, Description = "The peaceful surroundings enhance relaxation." }
    },
    TravelTimeMinutes = 15
};

// Add the location to the system
LocationManager locationManager = (LocationManager)SocialActivitySystem.Instance.LocationSystem;
locationManager.AddLocation(parkLocation);
```

### Traveling to Locations

Move characters between locations:

```csharp
// Get available locations
List<GameLocation> availableLocations = locationSystem.GetAvailableLocations(timeSystem.GetCurrentTime());

// Travel to a location
if (availableLocations.Count > 0)
{
    locationSystem.TravelToLocation(playerCharacter, availableLocations[0]);
}
```

### Finding NPCs at Locations

Get characters at a specific location:

```csharp
// Get NPCs at current location
GameLocation currentLocation = locationSystem.GetCharacterLocation(playerCharacter);
List<ICharacter> npcsAtLocation = locationSystem.GetNpcsAtLocation(currentLocation, timeSystem.GetCurrentTime());

// Display NPCs
foreach (var npc in npcsAtLocation)
{
    Debug.Log($"NPC present: {npc.Name}");
}
```

### Location Exploration

Discover new locations:

```csharp
// Explore for new locations
List<GameLocation> discoveredLocations = SocialActivitySystem.Instance.ExploreForNewLocations(playerCharacter);

// Display discovered locations
foreach (var location in discoveredLocations)
{
    Debug.Log($"Discovered: {location.Name} - {location.Description}");
}
```

## Time System

### Managing Game Time

Control the flow of time:

```csharp
// Get current time
TimeOfDay currentTime = timeSystem.GetCurrentTime();

// Advance time by a specific number of minutes
timeSystem.AdvanceTime(60); // Advance by 1 hour

// Set time scale (1.0 is real-time, 2.0 is double speed, etc.)
TimeManager timeManager = (TimeManager)timeSystem;
timeManager.SetTimeScale(2.0f);

// Pause/resume time
timeManager.PauseTime(true); // Pause
timeManager.PauseTime(false); // Resume

// Skip to a specific time of day
timeManager.SkipToTime(TimeOfDay.Evening);
```

### Working with Schedules

Create and manage scheduled activities:

```csharp
// Create a new scheduled activity
Activity dinnerActivity = /* reference to a dinner activity */;
GameDate tomorrowEvening = new GameDate
{
    Year = timeManager.GetCurrentGameDate().Year,
    Season = timeManager.GetCurrentGameDate().Season,
    Day = timeManager.GetCurrentGameDate().Day + 1,
    TimeOfDay = TimeOfDay.Evening
};

List<ICharacter> invitees = new List<ICharacter> { friendCharacter1, friendCharacter2 };

// Schedule the activity
activitySystem.ScheduleActivity(playerCharacter, dinnerActivity, tomorrowEvening, invitees);

// Check for any activities scheduled for the current time
SocialActivitySystem.Instance.CheckScheduledActivities(playerCharacter);
```

## Activities System

### Creating Activities

Define activities with their properties:

```csharp
Activity fishingActivity = new Activity
{
    Name = "Fishing",
    Description = "Relax by the water and try to catch fish.",
    Type = ActivityType.Leisure,
    TimeCost = 120, // 2 hours
    MoneyCost = 10, // 10 currency units
    FatigueCost = 20,
    Requirements = new List<ActivityRequirement>
    {
        new ActivityRequirement
        {
            Type = ActivityRequirement.RequirementType.Item,
            Parameter = "FishingRod",
            MinValue = 1,
            IsMandatory = true,
            FailMessage = "You need a fishing rod to go fishing."
        },
        new ActivityRequirement
        {
            Type = ActivityRequirement.RequirementType.Skill,
            Parameter = "Fitness",
            MinValue = 2,
            IsMandatory = false
        }
    },
    RelationshipEffects = new Dictionary<RelationshipParameter, float>
    {
        { RelationshipParameter.Friendship, 5f }
    },
    SkillGainChances = new Dictionary<PlayerSkill, float>
    {
        { PlayerSkill.Fitness, 0.3f }
    },
    PossibleRewards = new List<string> { "Fish", "RareShell", "OldBoot" },
    AvailableTimes = new List<TimeOfDay> { TimeOfDay.Morning, TimeOfDay.Afternoon },
    CompatibleLocationTypes = new List<LocationType> { LocationType.Public },
    SuccessBaseProbability = 0.7f
};
```

### Performing Activities

Execute activities with characters:

```csharp
// Get available activities at current location
GameLocation currentLocation = locationSystem.GetCharacterLocation(playerCharacter);
List<Activity> availableActivities = activitySystem.GetAvailableActivities(
    playerCharacter, 
    currentLocation, 
    timeSystem.GetCurrentTime()
);

// Perform an activity
if (availableActivities.Count > 0)
{
    List<ICharacter> participants = locationSystem.GetNpcsAtLocation(currentLocation, timeSystem.GetCurrentTime());
    
    // Limit to 3 participants (optional)
    if (participants.Count > 3)
    {
        participants = participants.GetRange(0, 3);
    }
    
    ActivityResult result = SocialActivitySystem.Instance.PerformActivity(
        playerCharacter, 
        availableActivities[0], 
        participants
    );
    
    // Process the result
    if (result.Success)
    {
        Debug.Log($"Activity succeeded! Enjoyment level: {result.EnjoymentLevel}");
        
        // Display relationship changes
        foreach (var change in result.RelationshipChanges)
        {
            Debug.Log($"Relationship with {change.Target.Name} changed by {change.Amount} ({change.Parameter})");
        }
        
        // Display skill gains
        foreach (var gain in result.SkillGains)
        {
            Debug.Log($"Gained {gain.Amount} points in {gain.Skill}");
        }
        
        // Display acquired items
        foreach (var item in result.AcquiredItems)
        {
            Debug.Log($"Received {item.Quantity}x {item.ItemName} (Quality: {item.Quality})");
        }
    }
    else
    {
        Debug.Log($"Activity failed: {result.ResultMessage}");
    }
}
```

## Character Integration

### Character Requirements

To integrate a character with the system, implement the `ICharacter` interface:

```csharp
public class MyCharacter : MonoBehaviour, ICharacter
{
    // Required properties
    public string Name { get; private set; } = "Character Name";
    public Dictionary<PlayerSkill, float> Skills { get; private set; }
    public Dictionary<string, float> Traits { get; private set; }
    public List<ScheduledActivity> Schedule { get; private set; }
    
    // Relationship data
    private Dictionary<ICharacter, Dictionary<RelationshipParameter, float>> _relationships;
    
    // Memory collection
    private List<MemoryRecord> _memories;
    
    // Reference to inventory system
    [SerializeField] private PlayerInventory _inventory;
    
    private void Awake()
    {
        // Initialize dictionaries
        Skills = new Dictionary<PlayerSkill, float>();
        Traits = new Dictionary<string, float>();
        Schedule = new List<ScheduledActivity>();
        _relationships = new Dictionary<ICharacter, Dictionary<RelationshipParameter, float>>();
        _memories = new List<MemoryRecord>();
        
        // Initialize skills with default values
        foreach (PlayerSkill skill in Enum.GetValues(typeof(PlayerSkill)))
        {
            Skills[skill] = 1.0f;
        }
    }
    
    public void ModifyRelationship(ICharacter target, RelationshipParameter parameter, float amount)
    {
        // Ensure the relationship dictionary exists for this target
        if (!_relationships.ContainsKey(target))
        {
            _relationships[target] = new Dictionary<RelationshipParameter, float>();
        }
        
        // Ensure the parameter exists for this relationship
        if (!_relationships[target].ContainsKey(parameter))
        {
            _relationships[target][parameter] = 0f;
        }
        
        // Modify the relationship value
        _relationships[target][parameter] += amount;
        
        // Clamp values between 0 and 100
        _relationships[target][parameter] = Mathf.Clamp(_relationships[target][parameter], 0f, 100f);
    }
    
    public float GetRelationshipValue(ICharacter target, RelationshipParameter parameter)
    {
        // Check if relationship exists
        if (!_relationships.ContainsKey(target))
        {
            return 0f;
        }
        
        // Check if parameter exists
        if (!_relationships[target].ContainsKey(parameter))
        {
            return 0f;
        }
        
        return _relationships[target][parameter];
    }
    
    public void AddMemory(MemoryRecord memory)
    {
        _memories.Add(memory);
        
        // Optionally, sort memories by importance or recency
        _memories = _memories.OrderByDescending(m => m.Date.Year * 10000 + m.Date.Season * 100 + m.Date.Day).ToList();
        
        // Optionally, limit memory count
        if (_memories.Count > 100)
        {
            _memories = _memories.Take(100).ToList();
        }
    }
    
    public bool HasRequiredItems(List<string> requiredItems)
    {
        // Integrate with your inventory system
        if (_inventory == null)
        {
            return false;
        }
        
        foreach (var item in requiredItems)
        {
            if (!_inventory.HasItem(item))
            {
                return false;
            }
        }
        
        return true;
    }
}
```

## Events and Callbacks

Subscribe to system events to respond to changes:

```csharp
private void OnEnable()
{
    // Subscribe to location events
    SocialActivitySystem.Instance.OnLocationChanged += HandleLocationChanged;
    SocialActivitySystem.Instance.OnNpcEncountered += HandleNpcEncountered;
    SocialActivitySystem.Instance.OnSpecialLocationDiscovered += HandleLocationDiscovered;
    
    // Subscribe to time events
    SocialActivitySystem.Instance.OnTimeChanged += HandleTimeChanged;
    
    // Subscribe to activity events
    SocialActivitySystem.Instance.OnActivityStarted += HandleActivityStarted;
    SocialActivitySystem.Instance.OnActivityCompleted += HandleActivityCompleted;
    SocialActivitySystem.Instance.OnScheduleCreated += HandleScheduleCreated;
}

private void OnDisable()
{
    // Unsubscribe from events
    SocialActivitySystem.Instance.OnLocationChanged -= HandleLocationChanged;
    SocialActivitySystem.Instance.OnNpcEncountered -= HandleNpcEncountered;
    SocialActivitySystem.Instance.OnSpecialLocationDiscovered -= HandleLocationDiscovered;
    SocialActivitySystem.Instance.OnTimeChanged -= HandleTimeChanged;
    SocialActivitySystem.Instance.OnActivityStarted -= HandleActivityStarted;
    SocialActivitySystem.Instance.OnActivityCompleted -= HandleActivityCompleted;
    SocialActivitySystem.Instance.OnScheduleCreated -= HandleScheduleCreated;
}

// Example event handlers
private void HandleLocationChanged(object sender, LocationChangedEventArgs args)
{
    Debug.Log($"{args.Character.Name} traveled from {args.PreviousLocation?.Name ?? "nowhere"} to {args.NewLocation.Name}.");
    
    // Update UI, trigger animations, etc.
}

private void HandleNpcEncountered(object sender, NpcEncounteredEventArgs args)
{
    Debug.Log($"{args.Player.Name} encountered {args.EncounteredNpc.Name} at {args.Location.Name}.");
    
    // Trigger dialogue, show interaction UI, etc.
}

private void HandleActivityCompleted(object sender, ActivityCompletedEventArgs args)
{
    Debug.Log($"Activity {args.Activity.Name} completed with {(args.Result.Success ? "success" : "failure")}");
    
    // Show result UI, update character stats, etc.
}
```

## Common Use Cases

### Creating a Daily Routine for NPCs

```csharp
// Set up NPC daily schedule
void SetupNpcDailySchedule(ICharacter npc)
{
    TimeManager timeManager = (TimeManager)SocialActivitySystem.Instance.TimeSystem;
    GameDate currentDate = timeManager.GetCurrentGameDate();
    
    // Morning activity
    GameDate morningSlot = currentDate;
    morningSlot.TimeOfDay = TimeOfDay.Morning;
    Activity breakfastActivity = /* reference to breakfast activity */;
    SocialActivitySystem.Instance.ActivitySystem.ScheduleActivity(npc, breakfastActivity, morningSlot, new List<ICharacter>());
    
    // Afternoon activity
    GameDate afternoonSlot = currentDate;
    afternoonSlot.TimeOfDay = TimeOfDay.Afternoon;
    Activity workActivity = /* reference to work activity */;
    SocialActivitySystem.Instance.ActivitySystem.ScheduleActivity(npc, workActivity, afternoonSlot, new List<ICharacter>());
    
    // Evening activity
    GameDate eveningSlot = currentDate;
    eveningSlot.TimeOfDay = TimeOfDay.Evening;
    Activity dinnerActivity = /* reference to dinner activity */;
    SocialActivitySystem.Instance.ActivitySystem.ScheduleActivity(npc, dinnerActivity, eveningSlot, new List<ICharacter>());
}
```

### Creating a Social Event

```csharp
void OrganizeParty(ICharacter host, List<ICharacter> guests)
{
    // Get the host's home location
    LocationManager locationManager = (LocationManager)SocialActivitySystem.Instance.LocationSystem;
    GameLocation hostHome = /* get host's home location */;
    
    // Create a party activity
    Activity partyActivity = new Activity
    {
        Name = "House Party",
        Description = $"Party at {host.Name}'s place",
        Type = ActivityType.Social,
        TimeCost = 180, // 3 hours
        MoneyCost = 50,
        FatigueCost = 30,
        Requirements = new List<ActivityRequirement>(),
        RelationshipEffects = new Dictionary<RelationshipParameter, float>
        {
            { RelationshipParameter.Friendship, 10f },
            { RelationshipParameter.Trust, 5f }
        },
        SkillGainChances = new Dictionary<PlayerSkill, float>
        {
            { PlayerSkill.Communication, 0.5f }
        },
        AvailableTimes = new List<TimeOfDay> { TimeOfDay.Evening, TimeOfDay.Night },
        CompatibleLocationTypes = new List<LocationType> { LocationType.Private }
    };
    
    // Schedule the party for tomorrow evening
    TimeManager timeManager = (TimeManager)SocialActivitySystem.Instance.TimeSystem;
    GameDate partyDate = timeManager.GetCurrentGameDate();
    partyDate.Day += 1;
    partyDate.TimeOfDay = TimeOfDay.Evening;
    
    // Send invitations
    SocialActivitySystem.Instance.ActivitySystem.ScheduleActivity(host, partyActivity, partyDate, guests);
    
    Debug.Log($"{host.Name} has planned a party for {partyDate}!");
}
```

### Time-Based Location Changes

```csharp
// Subscribe to time change events
void SubscribeToTimeChanges()
{
    SocialActivitySystem.Instance.OnTimeChanged += HandleTimeChange;
}

// Handle time changes
void HandleTimeChange(object sender, TimeChangedEventArgs args)
{
    // Update location availability based on time of day
    LocationManager locationManager = (LocationManager)SocialActivitySystem.Instance.LocationSystem;
    List<GameLocation> allLocations = locationManager.AllLocations;
    
    foreach (var location in allLocations)
    {
        // Update NPC spawn rates based on time
        if (location.NpcSpawnRates.TryGetValue(args.CurrentTime, out float spawnRate))
        {
            // Generate or remove NPCs based on spawn rate
            GenerateNpcsForLocation(location, spawnRate);
        }
        
        // Update environmental effects based on time
        UpdateLocationEnvironment(location, args.CurrentTime);
    }
    
    // Check for time-specific events
    if (args.PreviousTime != TimeOfDay.Night && args.CurrentTime == TimeOfDay.Night)
    {
        // Transition to night time
        Debug.Log("Night has fallen. Some locations are now closed.");
    }
}
```

## Example Implementation

Here's a complete example of how to integrate the Social Activity System into a game controller:

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;

public class GameController : MonoBehaviour
{
    [SerializeField] private PlayerCharacter _player;
    private SocialActivitySystem _socialSystem;
    
    private void Start()
    {
        // Initialize the social system
        _socialSystem = SocialActivitySystem.Instance;
        
        // Subscribe to events
        _socialSystem.OnLocationChanged += HandleLocationChanged;
        _socialSystem.OnNpcEncountered += HandleNpcEncountered;
        _socialSystem.OnActivityCompleted += HandleActivityCompleted;
        
        // Initialize example locations
        InitializeGameLocations();
        
        // Set starting location for player
        ILocationSystem locationSystem = _socialSystem.LocationSystem;
        locationSystem.TravelToLocation(_player, GetStartingLocation());
        
        // Update UI with current time
        UpdateTimeDisplay();
    }
    
    private void Update()
    {
        // Check for scheduled activities
        _socialSystem.CheckScheduledActivities(_player);
        
        // Update UI elements as needed
        UpdateTimeDisplay();
        UpdateLocationDisplay();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        _socialSystem.OnLocationChanged -= HandleLocationChanged;
        _socialSystem.OnNpcEncountered -= HandleNpcEncountered;
        _socialSystem.OnActivityCompleted -= HandleActivityCompleted;
    }
    
    private void InitializeGameLocations()
    {
        LocationManager locationManager = (LocationManager)_socialSystem.LocationSystem;
        
        // Create and add locations
        GameLocation home = CreateHomeLocation();
        GameLocation park = CreateParkLocation();
        GameLocation cafe = CreateCafeLocation();
        
        locationManager.AddLocation(home);
        locationManager.AddLocation(park);
        locationManager.AddLocation(cafe);
    }
    
    private GameLocation GetStartingLocation()
    {
        // Get home location (assuming it's the first location)
        LocationManager locationManager = (LocationManager)_socialSystem.LocationSystem;
        return locationManager.AllLocations[0];
    }
    
    // Example location creation methods
    private GameLocation CreateHomeLocation()
    {
        // Create home location
        return new GameLocation
        {
            Name = "Home",
            Description = "Your cozy apartment",
            Type = LocationType.Private,
            MapPosition = new Vector2(0, 0),
            OpeningHours = CreateAlwaysOpenTimeSlots(),
            AvailableActivities = CreateHomeActivities(),
            NpcSpawnRates = new Dictionary<TimeOfDay, float>(),
            EnvironmentalBonuses = new List<LocationBonus>
            {
                new LocationBonus { BonusType = "Rest", BonusValue = 0.25f, Description = "Comfortable surroundings" }
            },
            IsDiscovered = true,
            TravelTimeMinutes = 0
        };
    }
    
    private GameLocation CreateParkLocation()
    {
        // Create park location
        return new GameLocation
        {
            Name = "Central Park",
            Description = "A large urban park with paths and open spaces",
            Type = LocationType.Public,
            MapPosition = new Vector2(200, 100),
            OpeningHours = CreateParkTimeSlots(),
            AvailableActivities = CreateParkActivities(),
            NpcSpawnRates = new Dictionary<TimeOfDay, float>
            {
                { TimeOfDay.Morning, 0.7f },
                { TimeOfDay.Afternoon, 0.9f },
                { TimeOfDay.Evening, 0.5f },
                { TimeOfDay.Night, 0.1f }
            },
            EnvironmentalBonuses = new List<LocationBonus>
            {
                new LocationBonus { BonusType = "Relaxation", BonusValue = 0.2f, Description = "Fresh air and nature" }
            },
            IsDiscovered = true,
            TravelTimeMinutes = 15
        };
    }
    
    private GameLocation CreateCafeLocation()
    {
        // Create cafe location
        return new GameLocation
        {
            Name = "Bella's Cafe",
            Description = "A cozy cafe with great coffee",
            Type = LocationType.Public,
            MapPosition = new Vector2(150, 50),
            OpeningHours = CreateCafeTimeSlots(),
            AvailableActivities = CreateCafeActivities(),
            NpcSpawnRates = new Dictionary<TimeOfDay, float>
            {
                { TimeOfDay.Morning, 0.8f },
                { TimeOfDay.Afternoon, 0.6f },
                { TimeOfDay.Evening, 0.7f }
            },
            EnvironmentalBonuses = new List<LocationBonus>
            {
                new LocationBonus { BonusType = "Social", BonusValue = 0.15f, Description = "Friendly atmosphere" }
            },
            IsDiscovered = true,
            TravelTimeMinutes = 10
        };
    }
    
    // Helper methods for time slots and activities
    private List<TimeSlot> CreateAlwaysOpenTimeSlots()
    {
        List<TimeSlot> slots = new List<TimeSlot>();
        
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            foreach (TimeOfDay time in Enum.GetValues(typeof(TimeOfDay)))
            {
                TimeOfDay nextTime = (TimeOfDay)(((int)time + 1) % 6);
                slots.Add(new TimeSlot
                {
                    Day = day,
                    StartTime = time,
                    EndTime = nextTime,
                    IsAvailable = true
                });
            }
        }
        
        return slots;
    }
    
    private List<TimeSlot> CreateParkTimeSlots()
    {
        List<TimeSlot> slots = new List<TimeSlot>();
        
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            foreach (TimeOfDay time in Enum.GetValues(typeof(TimeOfDay)))
            {
                if (time != TimeOfDay.LateNight)
                {
                    TimeOfDay nextTime = (TimeOfDay)(((int)time + 1) % 6);
                    slots.Add(new TimeSlot
                    {
                        Day = day,
                        StartTime = time,
                        EndTime = nextTime,
                        IsAvailable = true
                    });
                }
            }
        }
        
        return slots;
    }
    
    private List<TimeSlot> CreateCafeTimeSlots()
    {
        List<TimeSlot> slots = new List<TimeSlot>();
        
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            // Cafe open from Morning to Evening
            for (int i = (int)TimeOfDay.Morning; i <= (int)TimeOfDay.Evening; i++)
            {
                TimeOfDay time = (TimeOfDay)i;
                TimeOfDay nextTime = (TimeOfDay)(((int)time + 1) % 6);
                slots.Add(new TimeSlot
                {
                    Day = day,
                    StartTime = time,
                    EndTime = nextTime,
                    IsAvailable = true
                });
            }
        }
        
        return slots;
    }
    
    private List<Activity> CreateHomeActivities()
    {
        // Create activities available at home
        return new List<Activity>
        {
            new Activity
            {
                Name = "Sleep",
                Description = "Rest and recover energy",
                Type = ActivityType.Leisure,
                TimeCost = 480, // 8 hours
                MoneyCost = 0,
                FatigueCost = -50, // Reduces fatigue
                Requirements = new List<ActivityRequirement>(),
                RelationshipEffects = new Dictionary<RelationshipParameter, float>(),
                SkillGainChances = new Dictionary<PlayerSkill, float>(),
                AvailableTimes = new List<TimeOfDay> { TimeOfDay.Night, TimeOfDay.LateNight },
                CompatibleLocationTypes = new List<LocationType> { LocationType.Private }
            },
            new Activity
            {
                Name = "Read a Book",
                Description = "Improve your knowledge through reading",
                Type = ActivityType.Leisure,
                TimeCost = 60, // 1 hour
                MoneyCost = 0,
                FatigueCost = 5,
                Requirements = new List<ActivityRequirement>(),
                RelationshipEffects = new Dictionary<RelationshipParameter, float>(),
                SkillGainChances = new Dictionary<PlayerSkill, float>
                {
                    { PlayerSkill.Intellect, 0.8f }
                },
                AvailableTimes = new List<TimeOfDay> { 
                    TimeOfDay.Morning, TimeOfDay.Afternoon, 
                    TimeOfDay.Evening, TimeOfDay.Night 
                },
                CompatibleLocationTypes = new List<LocationType> { LocationType.Private, LocationType.Public }
            }
        };
    }
    
    private List<Activity> CreateParkActivities()
    {
        // Create activities available at the park
        return new List<Activity>
        {
            new Activity
            {
                Name = "Jogging",
                Description = "Run around the park for exercise",
                Type = ActivityType.Leisure,
                TimeCost = 45, // 45 minutes
                MoneyCost = 0,
                FatigueCost = 15,
                Requirements = new List<ActivityRequirement>(),
                RelationshipEffects = new Dictionary<RelationshipParameter, float>(),
                SkillGainChances = new Dictionary<PlayerSkill, float>
                {
                    { PlayerSkill.Fitness, 0.9f }
                },
                AvailableTimes = new List<TimeOfDay> { 
                    TimeOfDay.EarlyMorning, TimeOfDay.Morning, 
                    TimeOfDay.Afternoon, TimeOfDay.Evening 
                },
                CompatibleLocationTypes = new List<LocationType> { LocationType.Public }
            },
            new Activity
            {
                Name = "Picnic",
                Description = "Enjoy a meal outdoors",
                Type = ActivityType.Social,
                TimeCost = 90, // 1.5 hours
                MoneyCost = 15,
                FatigueCost = 10,
                Requirements = new List<ActivityRequirement>(),
                RelationshipEffects = new Dictionary<RelationshipParameter, float>
                {
                    { RelationshipParameter.Friendship, 8f },
                    { RelationshipParameter.Trust, 3f }
                },
                SkillGainChances = new Dictionary<PlayerSkill, float>
                {
                    { PlayerSkill.Communication, 0.4f }
                },
                AvailableTimes = new List<TimeOfDay> { 
                    TimeOfDay.Morning, TimeOfDay.Afternoon 
                },
                CompatibleLocationTypes = new List<LocationType> { LocationType.Public }
            }
        };
    }
    
    private List<Activity> CreateCafeActivities()
    {
        // Create activities available at the cafe
        return new List<Activity>
        {
            new Activity
            {
                Name = "Have Coffee",
                Description = "Enjoy a hot beverage",
                Type = ActivityType.Leisure,
                TimeCost = 30, // 30 minutes
                MoneyCost = 5,
                FatigueCost = -10, // Reduces fatigue
                Requirements = new List<ActivityRequirement>(),
                RelationshipEffects = new Dictionary<RelationshipParameter, float>(),
                SkillGainChances = new Dictionary<PlayerSkill, float>(),
                AvailableTimes = new List<TimeOfDay> { 
                    TimeOfDay.Morning, TimeOfDay.Afternoon, 
                    TimeOfDay.Evening 
                },
                CompatibleLocationTypes = new List<LocationType> { LocationType.Public }
            },
            new Activity
            {
                Name = "Coffee Date",
                Description = "Meet with someone over coffee",
                Type = ActivityType.Social,
                TimeCost = 60, // 1 hour
                MoneyCost = 10,
                FatigueCost = 5,
                Requirements = new List<ActivityRequirement>(),
                RelationshipEffects = new Dictionary<RelationshipParameter, float>
                {
                    { RelationshipParameter.Friendship, 5f },
                    { RelationshipParameter.Romance, 5f }
                },
                SkillGainChances = new Dictionary<PlayerSkill, float>
                {
                    { PlayerSkill.Communication, 0.6f }
                },
                AvailableTimes = new List<TimeOfDay> { 
                    TimeOfDay.Morning, TimeOfDay.Afternoon, 
                    TimeOfDay.Evening 
                },
                CompatibleLocationTypes = new List<LocationType> { LocationType.Public }
            }
        };
    }
    
    // Event handlers
    private void HandleLocationChanged(object sender, LocationChangedEventArgs args)
    {
        if (args.Character == _player)
        {
            Debug.Log($"Player arrived at {args.NewLocation.Name}");
            
            // Update UI or trigger location-specific events
            UpdateLocationDisplay();
        }
    }
    
    private void HandleNpcEncountered(object sender, NpcEncounteredEventArgs args)
    {
        if (args.Player == _player)
        {
            Debug.Log($"Player encountered {args.EncounteredNpc.Name}");
            
            // Show interaction options UI
            ShowNpcInteractionUI(args.EncounteredNpc);
        }
    }
    
    private void HandleActivityCompleted(object sender, ActivityCompletedEventArgs args)
    {
        if (args.Initiator == _player)
        {
            Debug.Log($"Activity {args.Activity.Name} completed");
            
            // Show results UI
            ShowActivityResultsUI(args.Result);
        }
    }
    
    // UI update methods
    private void UpdateTimeDisplay()
    {
        TimeManager timeManager = (TimeManager)_socialSystem.TimeSystem;
        GameDate currentDate = timeManager.GetCurrentGameDate();
        
        // Update UI with current time and date
        string timeText = $"{currentDate}";
        Debug.Log($"Current time: {timeText}");
        
        // Update your UI elements here
    }
    
    private void UpdateLocationDisplay()
    {
        LocationManager locationManager = (LocationManager)_socialSystem.LocationSystem;
        GameLocation currentLocation = locationManager.GetCharacterLocation(_player);
        
        if (currentLocation != null)
        {
            // Update UI with current location info
            string locationText = $"{currentLocation.Name} - {currentLocation.Description}";
            Debug.Log($"Current location: {locationText}");
            
            // Show available activities
            List<Activity> activities = _socialSystem.GetAvailableActivities(_player, currentLocation);
            Debug.Log($"Available activities: {activities.Count}");
            
            // Update your UI elements here
        }
    }
    
    private void ShowNpcInteractionUI(ICharacter npc)
    {
        // Show UI for interacting with an NPC
        Debug.Log($"Showing interaction options for {npc.Name}");
        
        // Implement your UI code here
    }
    
    private void ShowActivityResultsUI(ActivityResult result)
    {
        // Show activity results UI
        Debug.Log($"Showing activity results: {(result.Success ? "Success" : "Failure")}");
        
        // Implement your UI code here
    }
}
```

This completes the implementation and usage guide for the Social Activity System. If you have any questions or need further clarification, please refer to the system's source code or contact the development team.