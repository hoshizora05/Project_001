# NPC Lifecycle System User Guide

## Overview

The NPC Lifecycle System simulates realistic daily routines, health dynamics, and background situation changes for NPCs in your Unity game. This system enables NPCs to:

- Follow daily schedules that vary by day of the week
- Experience health and energy fluctuations
- React to changing situations in their environment
- Participate in life events based on probabilistic triggers

## Getting Started

### System Setup

1. Add the `NPCLifecycleSystem` component to a GameObject in your scene, or let it be created automatically on first access
2. Access the system through the singleton: `NPCLifecycleSystem.Instance`

```csharp
// Example: Getting a reference to the lifecycle system
NPCLifecycleSystem lifecycleSystem = NPCLifecycleSystem.Instance;
```

### Registering Characters

Before using the system with a character, you need to register them:

```csharp
// Example: Creating and registering a daily routine for a character
DailyRoutine routine = new DailyRoutine
{
    characterId = "npc_001",
    weekdaySchedules = new Dictionary<string, List<ScheduleItem>>()
};

// Add a schedule for Monday
routine.weekdaySchedules["Monday"] = new List<ScheduleItem>
{
    new ScheduleItem
    {
        timeSlot = new TimeSlot(480, 540), // 8:00 AM - 9:00 AM
        activity = "breakfast",
        location = "kitchen",
        importance = 70,
        flexibility = new ScheduleFlexibility { timeShift = 30, skipProbability = 0.2f }
    },
    // Add more schedule items as needed
};

// Register with the system
NPCLifecycleSystem.Instance.RegisterDailyRoutine(routine);

// Register health system
HealthSystem health = new HealthSystem
{
    characterId = "npc_001",
    baseHealth = 100f,
    currentHealth = 100f,
    energyLevel = 100f,
    recoveryRate = 2f,
    energyConsumptionRates = new Dictionary<string, float>
    {
        { "idle", 0.1f },
        { "walk", 0.3f },
        { "work", 0.5f },
        { "sleep", -0.8f }, // Negative means recovery
    }
};
NPCLifecycleSystem.Instance.RegisterHealthSystem(health);

// Register background situation
BackgroundSituation situation = new BackgroundSituation
{
    characterId = "npc_001",
    environments = new List<BackgroundSituation.Environment>
    {
        new BackgroundSituation.Environment
        {
            type = "home",
            status = new Dictionary<string, object>
            {
                { "cleanliness", 80f },
                { "safety", 90f }
            },
            volatility = 0.2f
        }
    }
};
NPCLifecycleSystem.Instance.RegisterBackgroundSituation(situation);
```

Alternatively, you can let the system create default data structures when registering a character through the event bus:

```csharp
// Publish character registration event
var registrationEvent = new PsychologyEvent
{
    characterId = "npc_001",
    eventType = "character_registered"
};
CharacterEventBus.Instance.Publish(registrationEvent);
```

## Core Features

### 1. Schedule Management

#### Checking Current Activity

```csharp
// Get a character's current activity
ScheduleItem currentActivity = lifecycleSystem.GetScheduledActivity("npc_001", lifecycleSystem.CurrentGameTime);

if (currentActivity != null)
{
    Debug.Log($"NPC is currently: {currentActivity.activity} at {currentActivity.location}");
    Debug.Log($"Time slot: {currentActivity.timeSlot}");
}
else
{
    Debug.Log("NPC has no scheduled activity right now");
}

// Get a character's full status
CharacterStatusInfo status = lifecycleSystem.GetCharacterStatus("npc_001");
Debug.Log($"Current activity: {status.currentActivity}");
Debug.Log($"Current location: {status.currentLocation}");
Debug.Log($"Is available: {status.isAvailable}");
```

#### Requesting Schedule Changes

```csharp
// Request a schedule change
ScheduleItem newActivity = new ScheduleItem
{
    activity = "meeting with player",
    location = "town square",
    importance = 80,
    flexibility = new ScheduleFlexibility { timeShift = 15, skipProbability = 0.1f },
    associatedCharacters = new List<string> { "player" }
};

TimeSlot timeSlot = new TimeSlot(600, 660); // 10:00 AM - 11:00 AM

bool success = lifecycleSystem.RequestScheduleChange("npc_001", newActivity, timeSlot);

if (success)
{
    Debug.Log("Schedule change accepted");
}
else
{
    Debug.Log("Schedule change rejected - NPC is busy with something more important");
}
```

#### Viewing Future Schedule

```csharp
// Get a character's schedule for the next 3 days
List<ScheduleItem> futureSchedule = lifecycleSystem.GetFutureSchedule("npc_001", 3);

Debug.Log($"Upcoming activities ({futureSchedule.Count}):");
foreach (var item in futureSchedule)
{
    Debug.Log($"{item.timeSlot}: {item.activity} at {item.location}");
}
```

### 2. Health System

#### Checking Health Status

```csharp
// Get a character's current health status
string healthStatus = lifecycleSystem.GetCurrentHealthStatus("npc_001");
Debug.Log($"Health status: {healthStatus}");

// Get detailed status
CharacterStatusInfo status = lifecycleSystem.GetCharacterStatus("npc_001");
Debug.Log($"Energy level: {status.energyLevel}");
```

#### Updating Energy Level

```csharp
// Update a character's energy from an activity
lifecycleSystem.UpdateEnergyLevel("npc_001", "run", 1.0f);
```

#### Adding Health Conditions

```csharp
// Create a health condition via event
var healthEvent = new PsychologyEvent
{
    characterId = "npc_001",
    eventType = "character_interaction",
    parameters = new Dictionary<string, object>
    {
        { "interaction_type", "health_effect" },
        { "condition_type", "cold" },
        { "severity", 0.7f },
        { "duration", 1440f }, // One day in minutes
        { "effects", new Dictionary<string, float>
            {
                { "energy", -0.2f },
                { "health", -0.1f }
            }
        }
    }
};
CharacterEventBus.Instance.Publish(healthEvent);
```

### 3. Background Situations

#### Updating Environments

```csharp
// Update a character's home environment
Dictionary<string, object> changes = new Dictionary<string, object>
{
    { "cleanliness", 50f }, // Home is getting messier
    { "mood", "tense" }
};

lifecycleSystem.UpdateBackgroundSituation("npc_001", "home", changes);
```

#### Adding Challenges

```csharp
// Add a challenge via event
var challengeEvent = new PsychologyEvent
{
    characterId = "npc_001",
    eventType = "character_interaction",
    parameters = new Dictionary<string, object>
    {
        { "interaction_type", "add_challenge" },
        { "challenge_type", "financial_trouble" },
        { "urgency", 70f },
        { "impact", 60f },
        { "deadline", lifecycleSystem.CurrentGameTime.minutes + 4320 } // 3 days from now
    }
};
CharacterEventBus.Instance.Publish(challengeEvent);
```

#### Checking for Life Events

```csharp
// Create game context (e.g., from player actions or game state)
Dictionary<string, object> gameContext = new Dictionary<string, object>
{
    { "player_relationship", "friend" },
    { "location", "market" },
    { "time_of_day", "evening" }
};

// Check if any life events trigger
List<string> triggeredEvents = lifecycleSystem.CheckForLifeEvents("npc_001", gameContext);

foreach (var eventType in triggeredEvents)
{
    Debug.Log($"Life event triggered: {eventType}");
    // Handle the event in your game
}
```

## Event Listening

Subscribe to system events to react to changes:

```csharp
// Listen for status changes
lifecycleSystem.OnCharacterStatusChanged += HandleStatusChanged;

// Listen for health condition changes
lifecycleSystem.OnHealthConditionChanged += HandleHealthConditionChanged;

// Listen for life events
lifecycleSystem.OnLifeEventOccurred += HandleLifeEvent;

// Event handlers
private void HandleStatusChanged(string characterId, CharacterStatusInfo newStatus)
{
    Debug.Log($"Character {characterId} status changed: {newStatus.currentActivity}");
    // Update UI, trigger dialogue, etc.
}

private void HandleHealthConditionChanged(string characterId, string conditionType, float severity)
{
    Debug.Log($"Character {characterId} health condition: {conditionType} (severity: {severity})");
    // Show visual effects, change animations, etc.
}

private void HandleLifeEvent(string characterId, string eventType, Dictionary<string, float> impact)
{
    Debug.Log($"Character {characterId} experienced life event: {eventType}");
    // Trigger special dialogue, quests, etc.
}
```

## Time System

### Managing Game Time

The system automatically updates game time based on real time, but you can also control it directly:

```csharp
// Create a specific game time
GameTime specificTime = new GameTime
{
    day = 5,
    dayType = GameTime.DayType.Saturday,
    minutes = 720, // 12:00 PM
    season = "Summer"
};

// Set the game time directly
lifecycleSystem.AdvanceTime(specificTime);
```

### Time Format Helpers

```csharp
GameTime time = lifecycleSystem.CurrentGameTime;
Debug.Log($"Current game time: Day {time.day}, {time.dayType}, {time.FormattedTime}");
Debug.Log($"Is weekend: {time.IsWeekend}");
```

## Advanced Usage

### Custom Schedule Generation

You can create realistic schedules based on character traits:

```csharp
// Example: Generate a schedule based on occupation
void GenerateScheduleForOccupation(string characterId, string occupation)
{
    DailyRoutine routine = new DailyRoutine { characterId = characterId };
    
    // Weekday work schedule for different occupations
    if (occupation == "farmer")
    {
        for (int i = 0; i < 5; i++) // Monday through Friday
        {
            string dayName = ((GameTime.DayType)i).ToString();
            routine.weekdaySchedules[dayName] = new List<ScheduleItem>
            {
                new ScheduleItem {
                    timeSlot = new TimeSlot(360, 420), // 6:00 AM - 7:00 AM
                    activity = "breakfast",
                    location = "home",
                    importance = 70,
                    flexibility = new ScheduleFlexibility { timeShift = 30, skipProbability = 0.2f }
                },
                new ScheduleItem {
                    timeSlot = new TimeSlot(420, 720), // 7:00 AM - 12:00 PM
                    activity = "work_field",
                    location = "farm",
                    importance = 90,
                    flexibility = new ScheduleFlexibility { timeShift = 60, skipProbability = 0.1f }
                },
                // Add more schedule items...
            };
        }
        
        // Weekend schedule
        string saturday = GameTime.DayType.Saturday.ToString();
        routine.weekdaySchedules[saturday] = new List<ScheduleItem>
        {
            // Weekend activities...
        };
    }
    else if (occupation == "shopkeeper")
    {
        // Different schedule for shopkeeper
        // ...
    }
    
    NPCLifecycleSystem.Instance.RegisterDailyRoutine(routine);
}
```

### Creating Special Day Schedules

```csharp
// Add a special schedule for a holiday or event
void AddFestivalSchedule(string characterId, int festivalDay)
{
    if (NPCLifecycleSystem.Instance._routines.TryGetValue(characterId, out var routine))
    {
        routine.specialDaySchedules[festivalDay] = new List<ScheduleItem>
        {
            new ScheduleItem {
                timeSlot = new TimeSlot(600, 900), // 10:00 AM - 3:00 PM
                activity = "attend_festival",
                location = "town_square",
                importance = 85,
                flexibility = new ScheduleFlexibility { timeShift = 60, skipProbability = 0.3f },
                associatedCharacters = new List<string> { "all_townspeople" }
            },
            // More festival activities...
        };
    }
}
```

## Integration with Other Systems

### Psychology System Integration

The Lifecycle System works with the existing Psychology System via events:

```csharp
// Example: Generate an emotional response when schedule is interrupted
void HandleScheduleInterruption(string characterId, ScheduleItem interruptedItem)
{
    // Create a psychology event
    var event = new PsychologyEvent
    {
        characterId = characterId,
        eventType = "schedule_interrupted",
        parameters = new Dictionary<string, object>
        {
            { "activity", interruptedItem.activity },
            { "importance", interruptedItem.importance }
        }
    };
    
    // Send to event bus
    CharacterEventBus.Instance.Publish(event);
    
    // The psychology system will handle the emotional response
}
```

### Example: Player Interaction

```csharp
// Example: Player requests to talk to an NPC
void PlayerRequestsConversation(string npcId)
{
    // Check if NPC is available
    CharacterStatusInfo status = NPCLifecycleSystem.Instance.GetCharacterStatus(npcId);
    
    if (status.isAvailable)
    {
        // Start conversation directly
        StartConversation(npcId);
    }
    else
    {
        // NPC is busy, try to request schedule change
        ScheduleItem talkActivity = new ScheduleItem
        {
            activity = "talk_to_player",
            location = status.currentLocation, // Stay in current location
            importance = 60, // Medium importance
            flexibility = new ScheduleFlexibility { timeShift = 0, skipProbability = 0 },
            associatedCharacters = new List<string> { "player" }
        };
        
        // Current time + 1 minute
        int currentMinutes = NPCLifecycleSystem.Instance.CurrentGameTime.minutes;
        TimeSlot now = new TimeSlot(currentMinutes, currentMinutes + 15); // 15 min conversation
        
        bool accepted = NPCLifecycleSystem.Instance.RequestScheduleChange(npcId, talkActivity, now);
        
        if (accepted)
        {
            StartConversation(npcId);
        }
        else
        {
            // NPC refused - show appropriate dialogue
            ShowRefusalDialogue(npcId, status.currentActivity);
        }
    }
}
```

## Debugging

The NPCLifecycleSystem provides several ways to debug character schedules and states:

```csharp
// Print full character status
void DebugCharacterStatus(string characterId)
{
    CharacterStatusInfo status = NPCLifecycleSystem.Instance.GetCharacterStatus(characterId);
    
    Debug.Log($"=== Character {characterId} Status ===");
    Debug.Log($"Current activity: {status.currentActivity}");
    Debug.Log($"Current location: {status.currentLocation}");
    Debug.Log($"Energy level: {status.energyLevel}");
    Debug.Log($"Health status: {status.healthStatus}");
    Debug.Log($"Available: {status.isAvailable}");
    
    Debug.Log("Upcoming schedule:");
    foreach (var item in status.upcomingSchedule)
    {
        Debug.Log($"- {item.timeSlot}: {item.activity} at {item.location}");
    }
}
```

## Best Practices

1. **Initialize Early**: Register character data as early as possible, ideally during scene loading
2. **Update Gradually**: Spread character updates over multiple frames for better performance
3. **Use Events**: Rely on the event system rather than frequent polling for state changes
4. **Balance Realism vs Performance**: For large numbers of NPCs, simplify schedules for distant characters
5. **Customize Importance Values**: Set meaningful importance values for schedule items based on character traits and needs

## Troubleshooting

### Common Issues

1. **NPC ignores schedule changes**
   - Check the importance level of the new activity vs. existing activities
   - Verify the character ID is correct

2. **Health conditions not applying correctly**
   - Ensure the health system is properly registered
   - Check that condition effects use the correct parameter names

3. **Life events never trigger**
   - Verify trigger conditions match game context parameters exactly
   - Check probability values - they may be too low

### Debugging Checklist

- Confirm the NPCLifecycleSystem component is active in the scene
- Verify character registration has completed successfully
- Check Debug.Log output for event subscription confirmations
- Use the debugging tools to check character status in detail

## Performance Optimization

For games with many NPCs, consider these optimization strategies:

1. **Level of Detail**: Use simplified schedules for distant NPCs
2. **Update Throttling**: Update NPCs at different rates based on importance
3. **Pooling**: Reuse data structures for characters entering/leaving the active area
4. **Simulation Culling**: Pause simulation for NPCs outside the player's influence zone

## Example: Full Character Setup

```csharp
// Complete example of setting up a character in the lifecycle system
void SetupCharacter(string characterId, string name, string occupation)
{
    // Register with character manager first
    CharacterManager.Character character = new CharacterManager.Character
    {
        baseInfo = new CharacterBase
        {
            characterId = characterId,
            name = name,
            occupation = occupation,
            age = 35,
            gender = "female",
            personalityType = "friendly"
        }
    };
    
    CharacterManager.Instance.RegisterCharacter(character);
    
    // Set up daily routine
    DailyRoutine routine = new DailyRoutine { characterId = characterId };
    
    // Create weekday schedules based on occupation
    GenerateScheduleForOccupation(characterId, occupation);
    
    // Set up health system with parameters based on age/occupation
    HealthSystem health = new HealthSystem
    {
        characterId = characterId,
        baseHealth = 100f,
        currentHealth = 95f, // Slightly below perfect
        energyLevel = 80f,
        recoveryRate = 2.5f,
        energyConsumptionRates = new Dictionary<string, float>
        {
            { "idle", 0.05f },
            { "walk", 0.2f },
            { "run", 0.6f },
            { "work", 0.4f },
            { "sleep", -1.0f }
        }
    };
    
    // Set up background situation
    BackgroundSituation situation = new BackgroundSituation
    {
        characterId = characterId,
        environments = new List<BackgroundSituation.Environment>
        {
            new BackgroundSituation.Environment
            {
                type = "home",
                status = new Dictionary<string, object>
                {
                    { "cleanliness", 75f },
                    { "safety", 90f },
                    { "comfort", 80f }
                },
                volatility = 0.2f,
                relatedCharacters = new List<string>() // Lives alone
            },
            new BackgroundSituation.Environment
            {
                type = "workplace",
                status = new Dictionary<string, object>
                {
                    { "stress", 50f },
                    { "satisfaction", 70f }
                },
                volatility = 0.4f,
                relatedCharacters = new List<string> { "npc_002", "npc_003" } // Coworkers
            }
        },
        lifeEvents = new List<BackgroundSituation.LifeEvent>
        {
            new BackgroundSituation.LifeEvent
            {
                type = "promotion",
                triggerConditions = new Dictionary<string, object>
                {
                    { "workplace_satisfaction", 90f },
                    { "days_at_work", 30 }
                },
                probability = 0.01f, // 1% chance when conditions met
                impact = new Dictionary<string, float>
                {
                    { "happiness", 30f },
                    { "stress", 15f },
                    { "income", 20f }
                }
            }
        }
    };
    
    // Register everything with the lifecycle system
    NPCLifecycleSystem system = NPCLifecycleSystem.Instance;
    system.RegisterDailyRoutine(routine);
    system.RegisterHealthSystem(health);
    system.RegisterBackgroundSituation(situation);
    
    Debug.Log($"Character {name} fully set up in the lifecycle system");
}
```

## Conclusion

The NPC Lifecycle System provides a comprehensive framework for creating believable, dynamic NPCs that live realistic lives within your game world. By managing schedules, health, and background situations, this system enables rich character behaviors and meaningful player interactions.