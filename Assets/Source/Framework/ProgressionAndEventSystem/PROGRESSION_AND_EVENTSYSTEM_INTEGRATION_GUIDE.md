# Progression and Event System Integration Guide

This guide walks you through the process of integrating the Progression and Event System into your Unity project.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [System Setup](#system-setup)
3. [Character Integration](#character-integration)
4. [Creating Events](#creating-events)
5. [UI Integration](#ui-integration)
6. [Saving and Loading](#saving-and-loading)
7. [Testing](#testing)
8. [Common Issues](#common-issues)

## Prerequisites

Before beginning integration, ensure you have:

- Unity version 2020.3 or newer
- Basic understanding of C# and Unity
- A project with character/player systems

## System Setup

### Step 1: Add the System Component

Add the Progression and Event System to your scene:

1. Create a new GameObject in your scene and name it "EventSystem"
2. Add the `ProgressionAndEventSystem` component to it
3. The required sub-components will be automatically added

```csharp
// Alternatively, you can create it via script:
GameObject eventSystemObj = new GameObject("EventSystem");
eventSystemObj.AddComponent<ProgressionAndEventSystem>();

// Keep a reference to access the system later
ProgressionAndEventSystem eventSystem = eventSystemObj.GetComponent<ProgressionAndEventSystem>();
```

### Step 2: Configure References

If your game uses a dependency injection system or service locator pattern, register the event system:

```csharp
// Example using a service locator
ServiceLocator.Register<IEventManager>(eventSystemObj.GetComponent<IEventManager>());
ServiceLocator.Register<IEventTypeSystem>(eventSystemObj.GetComponent<IEventTypeSystem>());
ServiceLocator.Register<IEventEffectSystem>(eventSystemObj.GetComponent<IEventEffectSystem>());
ServiceLocator.Register<IEventUIManager>(eventSystemObj.GetComponent<IEventUIManager>());
```

## Character Integration

### Step 1: Implement the ICharacter Interface

Your player/character class must implement the `ICharacter` interface:

```csharp
public class PlayerCharacter : MonoBehaviour, ICharacter
{
    // Required ID property
    public string Id { get; private set; } = "player";
    
    // Internal state storage
    private Dictionary<string, float> _stats = new Dictionary<string, float>();
    private Dictionary<string, object> _state = new Dictionary<string, object>();
    private Dictionary<string, float> _relationships = new Dictionary<string, float>();
    private Dictionary<string, bool> _flags = new Dictionary<string, bool>();
    private Dictionary<string, DateTime> _eventHistory = new Dictionary<string, DateTime>();
    private string _currentLocation = "start_area";
    
    // Interface implementations
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
    
    // Add your own methods to modify stats, relationships, etc.
    public void ModifyStat(string statName, float delta)
    {
        if (!_stats.ContainsKey(statName))
            _stats[statName] = 0;
            
        _stats[statName] += delta;
    }
    
    public void SetLocation(string locationId)
    {
        _currentLocation = locationId;
    }
    
    public void AdjustRelationship(string characterId, float delta)
    {
        if (!_relationships.ContainsKey(characterId))
            _relationships[characterId] = 0;
            
        _relationships[characterId] += delta;
        
        // Clamp to valid range (0-100)
        _relationships[characterId] = Mathf.Clamp(_relationships[characterId], 0f, 100f);
    }
}
```

### Step 2: Connect Character to Event System

Make sure your character can be accessed by the event system:

```csharp
// Example in a GameManager class
public class GameManager : MonoBehaviour
{
    public PlayerCharacter playerCharacter;
    private ProgressionAndEventSystem _eventSystem;
    
    private void Start()
    {
        _eventSystem = FindObjectOfType<ProgressionAndEventSystem>();
        
        // Set up any initial events
        InitializeEvents();
    }
    
    private void Update()
    {
        // Optional: Check for location-based events
        CheckLocationEvents();
    }
    
    private void CheckLocationEvents()
    {
        List<GameEvent> availableEvents = _eventSystem.GetAvailableEvents(playerCharacter);
        
        // Optionally, automatically trigger highest-priority event
        if (availableEvents.Count > 0)
        {
            // Only trigger if it's a high-priority event
            if (availableEvents[0].Priority > 50)
            {
                _eventSystem.TriggerEvent(availableEvents[0].Id, playerCharacter);
            }
        }
    }
}
```

## Creating Events

### Step 1: Event Data Structure

Choose how you'll store event definitions:

1. **ScriptableObjects**: Create event assets in the Unity editor
2. **JSON Files**: Load events from external data
3. **Hardcoded**: Define events directly in code

Here's an example using ScriptableObjects:

```csharp
// Create a ScriptableObject wrapper for GameEvent
[CreateAssetMenu(fileName = "NewEvent", menuName = "Events/Game Event")]
public class GameEventData : ScriptableObject
{
    public GameEvent eventData;
    
    public GameEvent GetGameEvent()
    {
        // Create a deep copy to avoid modifying the asset
        // Implement proper deep cloning here
        return eventData;
    }
}
```

### Step 2: Event Registration

Register events with the system at an appropriate time (game start, level load, etc.):

```csharp
// Example loading from ScriptableObjects
public class EventLoader : MonoBehaviour
{
    public List<GameEventData> eventDataList;
    private ProgressionAndEventSystem _eventSystem;
    
    private void Start()
    {
        _eventSystem = FindObjectOfType<ProgressionAndEventSystem>();
        
        foreach (var eventData in eventDataList)
        {
            _eventSystem.RegisterEvent(eventData.GetGameEvent());
        }
        
        Debug.Log($"Registered {eventDataList.Count} events");
    }
}
```

## UI Integration

### Step 1: Create Event UI Elements

Create the UI components needed for events:

1. Event panel
2. Dialogue text
3. Character portraits
4. Choice buttons
5. Progress indicators

### Step 2: Connect UI to Event Manager

Register callbacks with the Event UI Manager:

```csharp
public class EventUIController : MonoBehaviour
{
    // UI References
    public GameObject eventPanel;
    public Text titleText;
    public Text descriptionText;
    public Text dialogueText;
    public Image characterPortrait;
    public Transform choiceButtonsContainer;
    public Button choiceButtonPrefab;
    
    private IEventUIManager _eventUIManager;
    private ProgressionAndEventSystem _eventSystem;
    private IEventManager _eventManager;
    
    private void Start()
    {
        _eventSystem = FindObjectOfType<ProgressionAndEventSystem>();
        _eventUIManager = _eventSystem.GetComponent<IEventUIManager>();
        _eventManager = _eventSystem.GetComponent<IEventManager>();
        
        // Register UI callbacks
        _eventUIManager.RegisterEventUICallback(EventUICallbackType.EventStart, OnEventStart);
        _eventUIManager.RegisterEventUICallback(EventUICallbackType.ChoicesShown, OnChoicesShown);
        _eventUIManager.RegisterEventUICallback(EventUICallbackType.ProgressUpdated, OnProgressUpdated);
        _eventUIManager.RegisterEventUICallback(EventUICallbackType.ResultShown, OnResultShown);
        
        // Hide UI initially
        eventPanel.SetActive(false);
    }
    
    private void OnEventStart(object data)
    {
        GameEvent gameEvent = data as GameEvent;
        
        // Show the event panel
        eventPanel.SetActive(true);
        
        // Set basic info
        titleText.text = gameEvent.Title;
        descriptionText.text = gameEvent.Description;
        
        // Clear any previous choices
        foreach (Transform child in choiceButtonsContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    private void OnChoicesShown(object data)
    {
        List<EventChoice> choices = data as List<EventChoice>;
        
        // Create choice buttons
        foreach (var choice in choices)
        {
            Button choiceButton = Instantiate(choiceButtonPrefab, choiceButtonsContainer);
            choiceButton.GetComponentInChildren<Text>().text = choice.Text;
            
            // Store the choice for the callback
            EventChoice capturedChoice = choice;
            
            // Set up button click handler
            choiceButton.onClick.AddListener(() => {
                // This is a simplified example - you'd need to pass the current event 
                // and get player from somewhere
                GameEvent currentEvent = null; // Get this from your system
                ICharacter player = null; // Get this from your system
                
                // Handle the choice
                _eventManager.MakeChoice(currentEvent, capturedChoice, player);
            });
        }
    }
    
    private void OnProgressUpdated(object data)
    {
        Dictionary<string, object> progressData = data as Dictionary<string, object>;
        
        float progress = (float)progressData["progress"];
        string description = (string)progressData["description"];
        
        // Update progress UI
        // ...
    }
    
    private void OnResultShown(object data)
    {
        EventResult result = data as EventResult;
        
        // Show result UI
        // ...
        
        // Automatically hide after a delay
        StartCoroutine(HideEventPanelAfterDelay(3.0f));
    }
    
    private IEnumerator HideEventPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        eventPanel.SetActive(false);
    }
}
```

## Saving and Loading

### Step 1: Create Save Data Structure

Define a class to save event system state:

```csharp
[Serializable]
public class EventSystemSaveData
{
    public List<string> triggeredEvents = new List<string>();
    public List<string> activeEvents = new List<string>();
    public Dictionary<string, long> lastTriggeredTicks = new Dictionary<string, long>();
}
```

### Step 2: Implement Save/Load Methods

Add methods to save and load event system state:

```csharp
// In your save system
public class SaveSystem : MonoBehaviour
{
    private ProgressionAndEventSystem _eventSystem;
    private EventManager _eventManager;
    
    private void Start()
    {
        _eventSystem = FindObjectOfType<ProgressionAndEventSystem>();
        _eventManager = _eventSystem.GetComponent<EventManager>();
    }
    
    public void SaveGameData(GameSaveData saveData)
    {
        // Save event system data
        EventSystemSaveData eventSaveData = new EventSystemSaveData();
        
        // Get player character
        ICharacter player = FindObjectOfType<PlayerCharacter>();
        
        // Save triggered events
        foreach (var entry in player.GetEventHistory())
        {
            eventSaveData.triggeredEvents.Add(entry.Key);
            eventSaveData.lastTriggeredTicks.Add(entry.Key, entry.Value.Ticks);
        }
        
        // Save to main game data
        saveData.eventSystemData = eventSaveData;
        
        // Save to disk/cloud/etc.
        SaveToStorage(saveData);
    }
    
    public void LoadGameData(GameSaveData saveData)
    {
        // Get event save data
        EventSystemSaveData eventSaveData = saveData.eventSystemData;
        
        // Get player character
        PlayerCharacter player = FindObjectOfType<PlayerCharacter>();
        
        // Clear current event history
        player.ClearEventHistory();
        
        // Restore event history
        foreach (var eventId in eventSaveData.triggeredEvents)
        {
            long ticks = eventSaveData.lastTriggeredTicks[eventId];
            DateTime timestamp = new DateTime(ticks);
            
            player.SetEventOccurrence(eventId, timestamp);
        }
    }
}
```

## Testing

### Step 1: Create Test Events

Create simple test events to verify your integration:

```csharp
// Example test method
private void CreateTestEvents()
{
    // Simple greeting event
    GameEvent testEvent = new GameEvent
    {
        Id = "test_event_001",
        Title = "Test Event",
        Description = "This is a test event to verify the system works.",
        Type = EventType.Normal,
        Priority = 10,
        IsRepeatable = true,
        Conditions = new List<EventCondition>(),
        Stages = new List<EventStage>
        {
            new EventStage
            {
                StageId = "test_stage",
                Description = "A test stage",
                Dialogue = new List<DialogueLine>
                {
                    new DialogueLine
                    {
                        CharacterId = "system",
                        Text = "Hello! This is a test message.",
                        Duration = 2.0f
                    }
                },
                Choices = new List<EventChoice>
                {
                    new EventChoice
                    {
                        Id = "test_choice_1",
                        Text = "This is working great!"
                    },
                    new EventChoice
                    {
                        Id = "test_choice_2",
                        Text = "I need to fix something."
                    }
                }
            }
        }
    };
    
    _eventSystem.RegisterEvent(testEvent);
}
```

### Step 2: Test Different Event Types

Create events that test different aspects of the system:

1. Events with multiple stages
2. Events with complex conditions
3. Events with effects
4. Event chains with dependencies

### Step 3: Debug Utilities

Add debug tools to monitor the system:

```csharp
public class EventSystemDebugger : MonoBehaviour
{
    private ProgressionAndEventSystem _eventSystem;
    private ICharacter _player;
    
    public bool showDebugGUI = true;
    
    private void Start()
    {
        _eventSystem = FindObjectOfType<ProgressionAndEventSystem>();
        _player = FindObjectOfType<PlayerCharacter>();
    }
    
    private void OnGUI()
    {
        if (!showDebugGUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        
        GUILayout.Label("Event System Debugger", GUI.skin.box);
        
        if (GUILayout.Button("Trigger Test Event"))
        {
            _eventSystem.TriggerEvent("test_event_001", _player);
        }
        
        GUILayout.Label("Available Events:");
        
        var availableEvents = _eventSystem.GetAvailableEvents(_player);
        foreach (var evt in availableEvents)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{evt.Id} - {evt.Title}");
            if (GUILayout.Button("Trigger", GUILayout.Width(60)))
            {
                _eventSystem.TriggerEvent(evt.Id, _player);
            }
            GUILayout.EndHorizontal();
        }
        
        GUILayout.Label("Event History:");
        foreach (var entry in _player.GetEventHistory())
        {
            GUILayout.Label($"{entry.Key} - {entry.Value}");
        }
        
        GUILayout.EndArea();
    }
}
```

## Common Issues

### Issue: Events not triggering when expected

**Possible causes:**
- Event conditions are not being met
- Event has dependencies that haven't been satisfied
- Event is not repeatable and has already been triggered
- Event is in its cooldown period

**Solution:**
Use the debugger to check event availability and conditions.

### Issue: Event effects not applying

**Possible causes:**
- Effect target IDs don't match expected format
- Character system isn't correctly implementing the interface
- Effect conditions aren't being satisfied

**Solution:**
Check the format of target IDs (e.g., "relationship:character_name") and verify your character system is correctly implementing the interface methods.

### Issue: UI not updating

**Possible causes:**
- Callbacks not registered correctly
- Type casting issues in callbacks
- UI references missing

**Solution:**
Verify all UI references are assigned and callbacks are properly registered. Check for null values and correct type casting in callback methods.

### Issue: Performance concerns with many events

**Possible causes:**
- Too many events checked every frame
- Complex condition evaluations

**Solution:**
- Only check a subset of events each frame
- Cache condition results when possible
- Use more specific conditions to filter events earlier

---

For additional support or questions about the integration process, please contact the development team.