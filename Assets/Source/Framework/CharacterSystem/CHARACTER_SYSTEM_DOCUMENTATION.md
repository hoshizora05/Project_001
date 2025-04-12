# Character System Documentation

## Overview

The Character System provides a comprehensive framework for implementing realistic characters with psychology, emotions, desires, and memory. This system allows your characters to respond realistically to events, develop over time, and maintain consistent personalities.

## Core Components

### 1. Character Base
Characters are built from the `CharacterBase` class which integrates all subsystems:
- **Psychology System**: Handles emotions, desires, and personality traits
- **Memory System**: Stores experiences and influences future decisions
- **State Management**: Tracks and transitions character states

### 2. Event System
The event system drives character reactions and updates:
- Uses a central `CharacterEventBus` to dispatch events
- Event handlers translate events to character state changes
- UI components can subscribe to character events

### 3. Customization System
Characters can be customized via:
- `CharacterTemplateDefinition` for base templates
- `CharacterCustomizationAPI` for runtime modifications

## Getting Started

### 1. Creating a Character

#### Using Character Factory
```csharp
// Get a reference to the factory
CharacterFactory factory = CharacterFactory.Instance;

// Create a character from a template
string characterId = factory.CreateCharacter("template_villager");

// Get the character reference
CharacterBase character = CharacterManager.Instance.GetCharacter(characterId);
```

#### Customizing at Creation Time
```csharp
// Create a character with custom parameters
Dictionary<string, object> customParams = new Dictionary<string, object>()
{
    { "age", 35 },
    { "occupation", "Blacksmith" },
    { "personalityType", "Determined" }
};

string characterId = factory.CreateCharacter("template_villager", customParams);
```

### 2. Interacting with Characters

#### Using the Interaction API
```csharp
// Get a reference to the interaction API
CharacterInteractionAPI api = CharacterInteractionAPI.Instance;

// Trigger an action
api.TriggerAction("character_123", "greet");

// Trigger a situation with intensity
api.TriggerSituation("character_123", "danger", 0.8f);

// Trigger a decision point 
api.TriggerDecision("character_123", "moral_choice");
```

#### Adding Parameters to Interactions
```csharp
// Create parameters for more detailed interactions
Dictionary<string, object> parameters = new Dictionary<string, object>()
{
    { "target", "character_456" },
    { "location", "tavern" },
    { "time", "night" }
};

api.TriggerAction("character_123", "greet", parameters);
```

### 3. Setting Up UI Updates

#### Using the Event Handler
```csharp
// On your UI GameObject, add CharacterEventHandler component
// Then hook up your UI elements to the UnityEvents
CharacterEventHandler eventHandler = GetComponent<CharacterEventHandler>();

// In your UI script
public Text moodText;
public Text desireText;
public Slider desireSlider;

void Start() {
    eventHandler.onMoodChanged.AddListener(UpdateMoodUI);
    eventHandler.onDesireChanged.AddListener(UpdateDesireUI);
}

void UpdateMoodUI(string mood) {
    moodText.text = $"Current Mood: {mood}";
}

void UpdateDesireUI(string desireType, float value) {
    desireText.text = desireType;
    desireSlider.value = value / 100f; // Assuming desires range from 0-100
}
```

## Psychology System

### Desires
Desires drive character motivation and behavior:

```csharp
// Check a character's current desires
var character = CharacterManager.Instance.GetCharacter("character_123");
float hungerValue = character.desires.GetDesireValue("hunger");
string dominantDesire = character.desires.dominantDesireType;

// Or query the state directly through the psychology system
IPsychologySystem psychSystem = CharacterManager.Instance.GetSubsystem<IPsychologySystem>();
StateQuery query = new StateQuery { queryType = "desire", specificId = "hunger" };
ReadOnlyStateData data = psychSystem.QueryState("character_123", query);
float hungerValue = (float)data.values["currentValue"];
```

### Emotions
Emotions define a character's current emotional state:

```csharp
// Generate an emotional response to a situation
IPsychologySystem psychSystem = CharacterManager.Instance.GetSubsystem<IPsychologySystem>();
EmotionalResponse response = psychSystem.GenerateEmotionalResponse("character_123", "insult", 0.7f);

// Check properties of the response
string emotionType = response.responseType; // e.g., "anger"
float intensity = response.intensity;       // e.g., 0.7
```

### Conflict Resolution
When characters have competing desires or values:

```csharp
// Trigger a decision that involves conflicting values
IPsychologySystem psychSystem = CharacterManager.Instance.GetSubsystem<IPsychologySystem>();
ConflictResolution resolution = psychSystem.EvaluateInternalConflict("character_123", "moral_dilemma");

// Check the outcome
string chosenValue = resolution.chosenValue;         // e.g., "honesty" 
string rejectedValue = resolution.rejectedValue;     // e.g., "loyalty"
float conflictIntensity = resolution.conflictIntensity;
```

## Memory System

### Storing Memories
Characters remember experiences and interactions:

```csharp
// Get the memory manager
CharacterMemoryManager memoryManager = CharacterManager.Instance.GetSubsystem<CharacterMemoryManager>();

// Create a new memory
Dictionary<string, object> memoryData = new Dictionary<string, object>()
{
    { "type", "interaction" },
    { "with", "character_456" },
    { "action", "helped" },
    { "sentiment", "positive" }
};

memoryManager.StoreMemory("character_123", "interaction_help", memoryData);
```

### Retrieving Memories
```csharp
// Query a specific memory
var memory = memoryManager.RetrieveMemory("character_123", "interaction_help");

// Query all memories of a certain type
var interactions = memoryManager.QueryMemories("character_123", "type", "interaction");

// Query by sentiment
var positiveMemories = memoryManager.QueryMemories("character_123", "sentiment", "positive");
```

## Performance Optimization

The system includes optimization features to scale with many characters:

```csharp
// Characters are automatically managed by the CharacterSystemOptimizer
// which reduces update frequency for distant characters

// You can also manually control the simulation level
var handler = character.gameObject.GetComponent<CharacterSimulationHandler>();
handler.EnableFullSimulation();     // Full updates
handler.EnableSimplifiedSimulation(); // Less frequent updates
handler.DisableSimulation();        // Pause updates
```

## Defining New Character Templates

Create ScriptableObject assets for new character templates:

1. In Unity, go to Assets > Create > Character System > Character Template
2. Set base properties, desires, and emotional states
3. Reference the template when creating characters

## Event Types Reference

| Event Type | Description | Key Properties |
|------------|-------------|----------------|
| `desire_change` | A desire value has changed | `desireType`, `oldValue`, `newValue`, `isDominant` |
| `mood_change` | Character's mood has changed | `oldMood`, `newMood`, `emotionValues` |
| `emotional_response` | Character responded emotionally | `response.responseType`, `response.intensity` |
| `conflict_resolution` | Internal conflict was resolved | `resolution.chosenValue`, `resolution.rejectedValue` |
| `action_effect` | An action affected desires | `actionId`, `desireEffects` |

## Best Practices

1. **Start Simple**: Begin with a few core desires and emotions before adding complexity
2. **Balance Update Frequency**: Use the optimizer to prevent performance issues
3. **Context Matters**: Always include context parameters in interactions for realistic responses
4. **Life-like Changes**: Make psychology changes gradual for realistic character development
5. **Group Similar Characters**: Use templates for similar character types to maintain consistency

## Troubleshooting

### Character Not Responding to Events
- Ensure the character ID is correct
- Check that the event bus is properly initialized
- Verify the event type matches what handlers are listening for

### Performance Issues
- Use the CharacterSystemOptimizer to reduce updates for distant characters
- Consider simplifying complex characters in crowded scenes
- Profile your game to identify specific bottlenecks

### Inconsistent Behavior
- Review character templates for balanced desire/emotion settings
- Ensure memory system is storing relevant interactions
- Check for conflicting desire modifiers