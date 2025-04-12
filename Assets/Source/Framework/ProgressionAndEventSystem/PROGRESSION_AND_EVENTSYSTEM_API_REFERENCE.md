# Progression and Event System API Reference

## Core Interfaces

### IEventManager

Core interface for event management operations.

```csharp
public interface IEventManager
{
    void RegisterEvent(GameEvent gameEvent);
    void UnregisterEvent(string eventId);
    List<GameEvent> GetAvailableEvents(ICharacter player);
    void TriggerEvent(string eventId, ICharacter player);
    bool CheckEventConditions(GameEvent gameEvent, ICharacter player);
    void UpdateEventStates();
}
```

### IEventTypeSystem

Interface for event type management.

```csharp
public interface IEventTypeSystem
{
    void RegisterEventType(EventType eventType);
    EventType GetEventType(string typeId);
    List<string> GetEventsByType(EventType eventType);
    Dictionary<EventType, int> GetEventTypeStatistics(ICharacter player);
}
```

### IEventEffectSystem

Interface for event effect management.

```csharp
public interface IEventEffectSystem
{
    void ApplyEventEffects(GameEvent gameEvent, EventResult result, ICharacter player);
    void ReverseEventEffects(GameEvent gameEvent, ICharacter player);
    Dictionary<string, float> CalculateEffects(GameEvent gameEvent, List<EventChoice> choices);
    List<UnlockedContent> GetUnlockedContentFromEvent(GameEvent gameEvent, EventResult result);
}
```

### IEventUIManager

Interface for event UI management.

```csharp
public interface IEventUIManager
{
    void DisplayEventStart(GameEvent gameEvent);
    void ShowEventChoices(List<EventChoice> choices);
    void DisplayEventProgress(float progress, string stageDescription);
    void DisplayEventResult(EventResult result);
    void RegisterEventUICallback(EventUICallbackType type, Action<object> callback);
}
```

### ICharacter

Interface that must be implemented by character classes.

```csharp
public interface ICharacter
{
    string Id { get; }
    Dictionary<string, float> GetStats();
    Dictionary<string, object> GetState();
    Dictionary<string, float> GetRelationships();
    string GetCurrentLocation();
    bool HasFlag(string flagName);
    void SetFlag(string flagName, bool value);
    Dictionary<string, DateTime> GetEventHistory();
    void RecordEventOccurrence(string eventId);
}
```

## Data Structures

### GameEvent

Represents a complete event in the game.

```csharp
public class GameEvent
{
    public string Id;                               // Unique identifier
    public string Title;                            // Display title
    public string Description;                      // Event description
    public EventType Type;                          // Event category
    public int Priority;                            // Triggering priority
    public List<EventCondition> Conditions;         // Trigger conditions
    public List<EventStage> Stages;                 // Event flow stages
    public Dictionary<string, object> EventData;    // Custom event data
    public List<string> DependentEvents;            // Prerequisites
    public List<string> BlockedEvents;              // Exclusive events
    public bool IsRepeatable;                       // Can repeat?
    public TimeSpan? CooldownPeriod;                // Time between repeats
    public DateTime? ExpirationDate;                // When event expires
}
```

### EventCondition

Defines when events can trigger.

```csharp
public class EventCondition
{
    public ConditionType Type;                      // Type of condition
    public string TargetId;                         // Related entity
    public ComparisonOperator Operator;             // How to compare
    public object ExpectedValue;                    // Value to compare with
    public List<EventCondition> SubConditions;      // For compound conditions
    public LogicalOperator SubConditionOperator;    // AND/OR for subconditions
}
```

### EventStage

Represents a phase in an event sequence.

```csharp
public class EventStage
{
    public string StageId;                          // Unique stage ID
    public string Description;                      // Stage description
    public List<DialogueLine> Dialogue;             // Character dialogue
    public List<EventChoice> Choices;               // Available choices
    public Dictionary<string, StageEffect> Effects; // Stage effects
    public TimeSpan? TimeLimit;                     // Time to complete
    public List<EventCondition> ProgressConditions; // Conditions to progress
    public string NextStageId;                      // Default next stage
    public Dictionary<string, string> ConditionalNextStages; // Conditional branching
}
```

### DialogueLine

Represents character dialogue in an event.

```csharp
public class DialogueLine
{
    public string CharacterId;                      // Speaking character
    public string Text;                             // Dialogue content
    public string EmotionState;                     // Character emotion
    public float Duration;                          // Display duration
    public List<EventCondition> VisibilityConditions; // When to show
}
```

### StageEffect

Defines an effect applied during an event stage.

```csharp
public class StageEffect
{
    public string TargetId;                         // Effect target
    public string EffectType;                       // Type of effect
    public float Value;                             // Effect magnitude
    public List<EventCondition> EffectConditions;   // When to apply
}
```

### EventChoice

Represents a player choice during an event.

```csharp
public class EventChoice
{
    public string Id;                              // Choice identifier
    public string Text;                            // Display text
    public string Description;                     // Detailed description
    public List<EventCondition> AvailabilityConditions; // When available
    public Dictionary<string, float> Effects;      // Choice effects
    public float SuccessRate;                      // Chance of success
    public Dictionary<string, object> ResultData;  // Result data
    public List<EventCondition> UnlockConditions;  // When to unlock
}
```

### EventResult

Represents the outcome of an event.

```csharp
public class EventResult
{
    public bool Success;                           // Was successful?
    public string CompletedStageId;                // Last completed stage
    public List<string> ChoicesMade;               // Player choices
    public Dictionary<string, float> AppliedEffects; // Effects applied
    public List<UnlockedContent> NewlyUnlockedContent; // New content
    public Dictionary<string, object> ResultData;  // Custom result data
    public DateTime CompletionTime;                // When completed
}
```

### UnlockedContent

Represents content unlocked by an event.

```csharp
public class UnlockedContent
{
    public string ContentId;                       // Content identifier
    public string ContentType;                     // Type of content
    public string Description;                     // Content description
    public Dictionary<string, object> ContentData; // Content details
}
```

## Enums

### EventType

```csharp
public enum EventType
{
    Normal,      // Regular events
    Special,     // Important story events
    Seasonal,    // Time-limited seasonal events
    Anniversary, // Recurring annual events 
    Crisis       // Critical/emergency events
}
```

### ConditionType

```csharp
public enum ConditionType
{
    Time,        // Time-based conditions
    Relationship, // Character relationship conditions
    State,       // Game state conditions
    Location,    // Location-based conditions
    Compound     // Multiple conditions combined
}
```

### ComparisonOperator

```csharp
public enum ComparisonOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    NotContains
}
```

### LogicalOperator

```csharp
public enum LogicalOperator
{
    AND,  // All must be true
    OR    // At least one must be true
}
```

### EventUICallbackType

```csharp
public enum EventUICallbackType
{
    EventStart,      // Event begins
    ChoicesShown,    // Choices displayed
    ProgressUpdated, // Progress changed
    ResultShown      // Result displayed
}
```

## ProgressionAndEventSystem Class

Main MonoBehaviour component that ties all systems together.

### Public Methods

```csharp
public void RegisterEvent(GameEvent gameEvent)
// Registers a new event with the system

public List<GameEvent> GetAvailableEvents(ICharacter player)
// Gets all events available to the player

public void TriggerEvent(string eventId, ICharacter player)
// Triggers a specific event

public Dictionary<EventType, int> GetEventStatistics(ICharacter player)
// Gets statistics about experienced events

public void LogEventStructure(GameEvent gameEvent)
// Debug utility to log event structure
```

## EventManager Class

Implementation of IEventManager with additional functionality.

### Public Methods

```csharp
public void MakeChoice(GameEvent gameEvent, EventChoice choice, ICharacter player)
// Process a player choice in an event

public void CancelEvent(string eventId, ICharacter player)
// Cancels an active event
```

### Events

```csharp
public event Action<GameEvent> OnEventRegistered;
// Fired when an event is registered

public event Action<GameEvent, ICharacter> OnEventTriggered;
// Fired when an event is triggered

public event Action<GameEvent, EventResult, ICharacter> OnEventCompleted;
// Fired when an event is completed

public event Action<UnlockedContent> OnNewContentUnlocked;
// Fired when new content is unlocked
```