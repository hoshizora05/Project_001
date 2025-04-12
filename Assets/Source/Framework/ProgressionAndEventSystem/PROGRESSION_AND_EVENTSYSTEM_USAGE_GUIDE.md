# Progression and Event System Usage Guide

## Overview

The Progression and Event System provides a comprehensive framework for managing game events, storylines, and player progression. It allows you to create a dynamic narrative experience with branching paths based on player choices and actions.

This guide will help you quickly understand how to integrate and use the system in your game.

## Table of Contents

1. [System Architecture](#system-architecture)
2. [Getting Started](#getting-started)
3. [Creating Events](#creating-events)
4. [Event Conditions](#event-conditions)
5. [Event Stages and Choices](#event-stages-and-choices)
6. [Event Effects](#event-effects)
7. [Triggering Events](#triggering-events)
8. [Event UI Integration](#event-ui-integration)
9. [Event Type Management](#event-type-management)
10. [Advanced Features](#advanced-features)
11. [Best Practices](#best-practices)
12. [Examples](#examples)

## System Architecture

The Progression and Event System consists of several interconnected components:

- **Event Manager**: Core component that handles event registration, triggering, and state management
- **Event Type System**: Categorizes events and provides statistics
- **Event Effect System**: Applies event outcomes to game state
- **Event UI Manager**: Handles the display and interaction of events to players

## Getting Started

### 1. Set Up the System

Add the `ProgressionAndEventSystem` component to a GameObject in your scene. This automatically adds all required components:

```csharp
// Example: Adding the system through code
GameObject eventSystemObj = new GameObject("EventSystem");
eventSystemObj.AddComponent<ProgressionAndEventSystem>();
```

### 2. Implement the ICharacter Interface

Your player character class needs to implement the `ICharacter` interface:

```csharp
public class PlayerCharacter : MonoBehaviour, ICharacter
{
    public string Id { get; private set; } = "player";
    
    private Dictionary<string, float> _stats = new Dictionary<string, float>();
    private Dictionary<string, object> _state = new Dictionary<string, object>();
    private Dictionary<string, float> _relationships = new Dictionary<string, float>();
    private Dictionary<string, bool> _flags = new Dictionary<string, bool>();
    private Dictionary<string, DateTime> _eventHistory = new Dictionary<string, DateTime>();
    private string _currentLocation = "start_area";
    
    public Dictionary<string, float> GetStats() => _stats;
    public Dictionary<string, object> GetState() => _state;
    public Dictionary<string, float> GetRelationships() => _relationships;
    public string GetCurrentLocation() => _currentLocation;
    
    public bool HasFlag(string flagName) => 
        _flags.ContainsKey(flagName) && _flags[flagName];
        
    public void SetFlag(string flagName, bool value)
    {
        _flags[flagName] = value;
    }
    
    public Dictionary<string, DateTime> GetEventHistory() => _eventHistory;
    
    public void RecordEventOccurrence(string eventId)
    {
        _eventHistory[eventId] = DateTime.Now;
    }
}
```

## Creating Events

Events are defined using the `GameEvent` class. Here's how to create a simple event:

```csharp
// Example: Creating a basic event
GameEvent welcomeEvent = new GameEvent
{
    Id = "welcome_event_001",
    Title = "Welcome to Town",
    Description = "You arrive at the town gates. The guard approaches you.",
    Type = EventType.Normal,
    Priority = 10,
    IsRepeatable = false,
    Conditions = new List<EventCondition>
    {
        new EventCondition
        {
            Type = ConditionType.Location,
            TargetId = "location",
            Operator = ComparisonOperator.Equal,
            ExpectedValue = "town_entrance"
        }
    },
    Stages = new List<EventStage>
    {
        new EventStage
        {
            StageId = "welcome_start",
            Description = "The guard examines you closely.",
            Dialogue = new List<DialogueLine>
            {
                new DialogueLine 
                { 
                    CharacterId = "guard", 
                    Text = "Halt! State your business in our town.",
                    EmotionState = "neutral",
                    Duration = 3.0f
                }
            },
            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    Id = "choice_friendly",
                    Text = "I'm here to trade goods.",
                    Effects = new Dictionary<string, float>
                    {
                        { "relationship:guard", 5f },
                        { "flag:entered_town", 1f }
                    }
                },
                new EventChoice
                {
                    Id = "choice_aggressive",
                    Text = "Mind your own business.",
                    Effects = new Dictionary<string, float>
                    {
                        { "relationship:guard", -10f },
                        { "flag:guard_suspicious", 1f }
                    }
                }
            },
            Effects = new Dictionary<string, StageEffect>
            {
                { "reveal_town", new StageEffect 
                  { 
                      TargetId = "flag:town_visible", 
                      EffectType = "flag", 
                      Value = 1f 
                  } 
                }
            }
        }
    }
};

// Register the event with the system
ProgressionAndEventSystem eventSystem = FindObjectOfType<ProgressionAndEventSystem>();
eventSystem.RegisterEvent(welcomeEvent);
```

## Event Conditions

Events can have various conditions that determine when they're available:

### Types of Conditions

1. **Time Conditions**: Trigger events at specific times or dates
2. **Relationship Conditions**: Based on the player's relationship with characters
3. **State Conditions**: Based on game state or flags
4. **Location Conditions**: Trigger when a player visits specific locations
5. **Compound Conditions**: Group multiple conditions with AND/OR logic

```csharp
// Example: Complex condition
EventCondition complexCondition = new EventCondition
{
    Type = ConditionType.Compound,
    SubConditionOperator = LogicalOperator.AND,
    SubConditions = new List<EventCondition>
    {
        new EventCondition
        {
            Type = ConditionType.Relationship,
            TargetId = "mayor",
            Operator = ComparisonOperator.GreaterThanOrEqual,
            ExpectedValue = 50f
        },
        new EventCondition
        {
            Type = ConditionType.State,
            TargetId = "flag:quest_completed",
            Operator = ComparisonOperator.Equal,
            ExpectedValue = true
        }
    }
};
```

## Event Stages and Choices

Events consist of stages, each with dialogue, choices, and effects:

```csharp
// Example: Event with multiple stages
EventStage firstStage = new EventStage
{
    StageId = "meeting_start",
    Description = "You meet with the council members.",
    Dialogue = new List<DialogueLine> { /* dialogue lines */ },
    Choices = new List<EventChoice> { /* choices */ },
    NextStageId = "meeting_discussion" // Proceeds to next stage
};

EventStage secondStage = new EventStage
{
    StageId = "meeting_discussion",
    Description = "The council discusses your proposal.",
    Dialogue = new List<DialogueLine> { /* more dialogue */ },
    Choices = new List<EventChoice> { /* more choices */ },
    ConditionalNextStages = new Dictionary<string, string>
    {
        { "flag:council_impressed", "meeting_success" },
        { "flag:council_skeptical", "meeting_failure" }
    }
};
```

## Event Effects

Effects can modify the game state when events occur or choices are made:

```csharp
// Example: Different types of effects
Dictionary<string, StageEffect> eventEffects = new Dictionary<string, StageEffect>
{
    { "relationship_change", new StageEffect 
      { 
          TargetId = "relationship:mayor", 
          EffectType = "relationship", 
          Value = 15f 
      } 
    },
    { "unlock_area", new StageEffect 
      { 
          TargetId = "flag:castle_accessible", 
          EffectType = "flag", 
          Value = 1f 
      } 
    },
    { "skill_increase", new StageEffect 
      { 
          TargetId = "stat:diplomacy", 
          EffectType = "stat", 
          Value = 5f 
      } 
    }
};
```

## Triggering Events

There are two ways to trigger events:

1. **Automatic**: Events trigger automatically when conditions are met
2. **Manual**: Explicitly trigger events through code

```csharp
// Example: Manually triggering an event
ICharacter player = GetComponent<ICharacter>();
eventSystem.TriggerEvent("secret_meeting_001", player);
```

## Event UI Integration

The Event UI Manager provides callbacks to update your UI when events change state:

```csharp
// Example: Registering UI callbacks
EventUIManager uiManager = FindObjectOfType<EventUIManager>();

uiManager.RegisterEventUICallback(EventUICallbackType.EventStart, OnEventStarted);
uiManager.RegisterEventUICallback(EventUICallbackType.ChoicesShown, OnEventChoicesShown);
uiManager.RegisterEventUICallback(EventUICallbackType.ResultShown, OnEventResultShown);

// Callback implementation example
private void OnEventStarted(object data)
{
    GameEvent gameEvent = data as GameEvent;
    
    // Update UI elements
    eventTitleText.text = gameEvent.Title;
    eventDescriptionText.text = gameEvent.Description;
    eventPanel.SetActive(true);
}
```

## Event Type Management

The Event Type System lets you categorize and track events:

```csharp
// Example: Getting event statistics
Dictionary<EventType, int> eventStats = eventSystem.GetEventStatistics(player);

Debug.Log($"Normal events experienced: {eventStats[EventType.Normal]}");
Debug.Log($"Special events experienced: {eventStats[EventType.Special]}");
Debug.Log($"Crisis events experienced: {eventStats[EventType.Crisis]}");
```

## Advanced Features

### 1. Event Dependencies

Events can depend on other events having occurred:

```csharp
GameEvent sequelEvent = new GameEvent
{
    Id = "sequel_event_002",
    DependentEvents = new List<string> { "first_event_001" },
    // Other properties...
};
```

### 2. Event Blocking

Events can block other events from occurring:

```csharp
GameEvent exclusiveEvent = new GameEvent
{
    Id = "important_event_003",
    BlockedEvents = new List<string> { "minor_event_004", "minor_event_005" },
    // Other properties...
};
```

### 3. Event Cooldowns and Expiration

Control how often events can repeat and when they expire:

```csharp
GameEvent timedEvent = new GameEvent
{
    Id = "recurring_event_006",
    IsRepeatable = true,
    CooldownPeriod = TimeSpan.FromHours(24), // In-game time
    ExpirationDate = new DateTime(2023, 12, 31), // Expires on this date
    // Other properties...
};
```

## Best Practices

1. **Organize Events**: Use a consistent naming convention for event IDs
2. **Balance Priorities**: Set appropriate priority levels to ensure important events take precedence
3. **Test Thoroughly**: Check all branches and conditions for proper functionality
4. **Optimize Performance**: Use moderate numbers of concurrent conditions to avoid performance issues
5. **Use Meaningful Effects**: Make event outcomes have tangible impacts on the game world

## Examples

### Example 1: Quest Event Chain

```csharp
// First event in quest chain
GameEvent questStart = new GameEvent
{
    Id = "village_quest_001",
    Title = "Trouble in the Village",
    Description = "The village elder approaches you with a worried look.",
    Type = EventType.Special,
    // ...other properties
};

// Second event that depends on the first
GameEvent questProgress = new GameEvent
{
    Id = "village_quest_002",
    Title = "Investigating the Disturbance",
    Description = "You follow the trail into the forest.",
    DependentEvents = new List<string> { "village_quest_001" },
    // ...other properties
};

// Register both events
eventSystem.RegisterEvent(questStart);
eventSystem.RegisterEvent(questProgress);
```

### Example 2: Character Relationship Event

```csharp
// Event that triggers based on relationship level
GameEvent relationshipEvent = new GameEvent
{
    Id = "companion_event_001",
    Title = "A Moment of Trust",
    Conditions = new List<EventCondition>
    {
        new EventCondition
        {
            Type = ConditionType.Relationship,
            TargetId = "companion_alex",
            Operator = ComparisonOperator.GreaterThanOrEqual,
            ExpectedValue = 75f
        }
    },
    // ...other properties
};

eventSystem.RegisterEvent(relationshipEvent);
```

---

This guide covers the fundamentals of the Progression and Event System. For specific implementation questions or advanced use cases, please refer to the code documentation or contact the development team.