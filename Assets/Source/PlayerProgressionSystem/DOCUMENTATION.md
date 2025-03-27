# Player Progression System Documentation

## Overview

The Player Progression System provides a comprehensive framework for managing player character growth and progression in your Unity game. It handles:

- **Stats**: Core character attributes that can be modified with temporary or permanent modifiers
- **Skills**: Abilities that can be leveled up through experience gain
- **Social Standing**: Reputation within different social contexts that evolves based on player actions

The system is designed to be flexible, extensible, and easy to integrate into your existing project.

## Table of Contents

1. [Setup](#setup)
2. [Core Components](#core-components)
3. [Working with Stats](#working-with-stats)
4. [Working with Skills](#working-with-skills)
5. [Working with Social Standing](#working-with-social-standing)
6. [Events and Cross-System Effects](#events-and-cross-system-effects)
7. [Saving and Loading](#saving-and-loading)
8. [Extending the System](#extending-the-system)
9. [Example Usage](#example-usage)

## Setup

### 1. Create Required Configuration Assets

First, create the configuration assets that the system needs:

1. Right-click in the Project window and select **Create > Systems > Player Progression Config**
2. Right-click in the Project window and select **Create > Systems > Event Bus**

### 2. Configure the PlayerProgressionManager

1. Add the `PlayerProgressionManager` component to a GameObject in your scene (preferably one that persists across scenes)
2. Assign your created `PlayerProgressionConfig` asset to the `config` field
3. Assign your created `EventBusReference` asset to the `eventBus` field

### 3. Initialize the System

Call `Initialize` on your `PlayerProgressionManager` instance when starting a new game or loading a saved game:

```csharp
// In your GameManager or similar class
[SerializeField] private PlayerProgressionManager progressionManager;

void StartNewGame()
{
    string playerId = "player1"; // or generate a unique ID
    progressionManager.Initialize(playerId);
}
```

## Core Components

### PlayerProgressionManager

This is the central component that coordinates the stat, skill, and reputation subsystems. It:
- Processes events
- Updates all subsystems
- Handles cross-system effects
- Provides access to player stats, skills, and reputation

### EventBusReference

The event bus is a ScriptableObject that enables loose coupling between game systems. It allows any system to:
- Publish events that affect player progression
- Subscribe to player progression events
- Maintain clean separation between systems

### PlayerProgressionConfig

This ScriptableObject defines the initial configuration for the player progression systems:
- Stats with their base values, min/max limits, and growth rates
- Skill categories and individual skills with their requirements
- Reputation contexts with relevant traits

## Working with Stats

Stats represent a character's core attributes (e.g., Strength, Intelligence, Health) that can be modified by various effects.

### Configuring Stats

In the PlayerProgressionConfig asset:
1. Add entries to the `initialStats` list
2. For each stat, specify:
   - `statId`: Unique identifier (e.g., "strength", "intelligence")
   - `statName`: Display name
   - `baseValue`: Starting value
   - `minValue`: Minimum allowed value
   - `maxValue`: Maximum allowed value
   - `growthRate`: How fast the stat can grow naturally

### Accessing Stats

```csharp
// Get a stat's current value
StatValue strengthStat = progressionManager.GetStatValue("strength");
float currentStrength = strengthStat.CurrentValue;

// You can also access base value, min, and max
float baseStrength = strengthStat.BaseValue;
float minStrength = strengthStat.MinValue;
float maxStrength = strengthStat.MaxValue;
```

### Modifying Stats

Stats can be modified through events:

```csharp
// Create an event to change a stat
var statChangeEvent = new ProgressionEvent
{
    type = ProgressionEvent.ProgressionEventType.StatChange,
    parameters = new Dictionary<string, object>
    {
        { "statId", "strength" },
        { "baseValueChange", 1.0f } // Permanently increase strength by 1
    }
};

// Or you can add a temporary modifier
var statModifier = new PlayerStats.StatModifier(
    "potion_effect", // Source of modifier
    5.0f,            // Value to add/multiply/override
    PlayerStats.ModifierType.Additive, // Type of modification
    60.0f            // Duration in seconds (-1 for permanent)
);

var statChangeEvent = new ProgressionEvent
{
    type = ProgressionEvent.ProgressionEventType.StatChange,
    parameters = new Dictionary<string, object>
    {
        { "statId", "strength" },
        { "modifier", statModifier }
    }
};

// Publish the event through the event bus
eventBus.Publish(statChangeEvent);
```

### Modifier Types

- `Additive`: Adds the modifier value to the base value
- `Multiplicative`: Multiplies the (base + additives) by (1 + mod value)
- `Override`: Overrides the final value if higher than other calculations

## Working with Skills

Skills represent abilities that can be improved through practice and experience gain.

### Configuring Skills

In the PlayerProgressionConfig asset:
1. Add entries to the `skillCategories` list (e.g., "Combat", "Crafting", "Social")
2. For each category, add individual skills:
   - `skillId`: Unique identifier
   - `skillName`: Display name
   - `initialLevelThreshold`: XP needed for first level-up
   - `levelThresholdMultiplier`: How much the threshold increases per level
   - `requirements`: Prerequisites for learning this skill

### Accessing Skills

```csharp
// Get a skill's current level
float swordSkillLevel = progressionManager.GetSkillLevel("sword");
```

### Adding Experience

```csharp
// Create an event to add experience to a skill
var skillExpEvent = new ProgressionEvent
{
    type = ProgressionEvent.ProgressionEventType.SkillExperience,
    parameters = new Dictionary<string, object>
    {
        { "skillId", "sword" },
        { "experienceAmount", 25.0f }
    }
};

// Publish the event
eventBus.Publish(skillExpEvent);
```

### Handling Action Completion

When a player completes an action that should improve multiple skills:

```csharp
// Create a dictionary of skills and XP amounts
var relevantSkills = new Dictionary<string, float>
{
    { "sword", 15.0f },
    { "block", 5.0f },
    { "stamina", 10.0f }
};

// Create an event for completing an action
var actionEvent = new ProgressionEvent
{
    type = ProgressionEvent.ProgressionEventType.CompleteAction,
    parameters = new Dictionary<string, object>
    {
        { "actionId", "defeat_enemy" },
        { "relevantSkills", relevantSkills }
    }
};

// Publish the event
eventBus.Publish(actionEvent);
```

## Working with Social Standing

The Social Standing system tracks the player's reputation within different social contexts (e.g., factions, towns, social groups).

### Configuring Reputation Contexts

In the PlayerProgressionConfig asset:
1. Add entries to the `reputationContexts` list
2. For each context, specify:
   - `contextId`: Unique identifier (e.g., "town_riverwood", "faction_thieves_guild")
   - `contextName`: Display name
   - `relevantTraits`: List of reputation traits that matter in this context (e.g., "honesty", "strength", "generosity")

### Accessing Reputation

```csharp
// Get overall reputation in a context
float townReputation = progressionManager.GetReputationScore("town_riverwood");

// Get reputation for a specific trait
float honestyReputation = progressionManager.GetReputationScore("town_riverwood", "honesty");
```

### Modifying Reputation

Reputation changes through events:

```csharp
// Create a dictionary of trait impacts
var traitImpacts = new Dictionary<string, float>
{
    { "honesty", 5.0f },
    { "generosity", 3.0f }
};

// Create a reputation event
var reputationEvent = new ProgressionEvent
{
    type = ProgressionEvent.ProgressionEventType.ReputationImpact,
    parameters = new Dictionary<string, object>
    {
        { "contextId", "town_riverwood" },
        { "eventId", "returned_lost_item" },
        { "description", "Returned a valuable lost item to its owner" },
        { "traitImpacts", traitImpacts },
        { "decayRate", 0.05f } // How quickly this event's impact fades
    }
};

// Publish the event
eventBus.Publish(reputationEvent);
```

## Events and Cross-System Effects

The systems interact with each other through cross-system effects:

### Skill Effects on Stats

When a skill level increases, it can automatically boost related stats. For example, increasing the "weightlifting" skill could boost the "strength" stat.

To implement this, add skill effects when initializing your skills:

```csharp
var skillEffect = new SkillSystem.SkillEffect(
    "strength",                     // Target stat
    0.5f,                           // Effect value per skill level
    SkillSystem.EffectType.StatBoost // Effect type
);

skill.effects.Add(skillEffect);
```

### Command Pattern for Advanced Control

For more advanced operations or when you need undo functionality, use the Command pattern:

```csharp
// Create a command to add skill experience
var addExpCommand = new AddSkillExperienceCommand(skillSystem, "sword", 25.0f);

// Execute the command
addExpCommand.Execute();

// If needed, undo the command
addExpCommand.Undo();
```

## Saving and Loading

The system provides built-in serialization for saving and loading progress:

### Saving

```csharp
// Get serializable save data
ProgressionSaveData saveData = progressionManager.GenerateSaveData();

// Save this data to your game's save system
SaveSystem.SaveProgressionData(saveData);
```

### Loading

```csharp
// Load data from your game's save system
ProgressionSaveData saveData = SaveSystem.LoadProgressionData();

// Restore the progression system from save data
progressionManager.RestoreFromSaveData(saveData);
```

## Extending the System

The system is designed to be extensible through interfaces and dependency injection.

### Custom Implementations

1. Create a custom implementation of an interface:
```csharp
public class CustomStatSystem : IStatSystem
{
    // Implement all interface methods
    ...
}
```

2. Inject your custom implementation:
```csharp
progressionManager.InjectDependencies(
    new CustomStatSystem(),
    skillSystem,
    reputationSystem
);
```

### Testing with Mock Systems

For testing, you can inject mock implementations:

```csharp
progressionManager.InjectForTesting(
    new MockStatSystem(),
    new MockSkillSystem(),
    new MockReputationSystem()
);
```

## Example Usage

### Complete Example: Combat Scenario

Here's a complete example of how the system might be used in a combat scenario:

```csharp
public class CombatManager : MonoBehaviour
{
    [SerializeField] private EventBusReference eventBus;
    
    public void HandleEnemyDefeat(EnemyType enemyType, AttackType finalBlow)
    {
        // Award skill experience based on enemy type and final blow
        Dictionary<string, float> skillExperience = new Dictionary<string, float>();
        
        // Add skill experience based on the type of enemy
        switch (enemyType)
        {
            case EnemyType.Goblin:
                skillExperience.Add("sword", 10f);
                skillExperience.Add("light_armor", 5f);
                break;
            case EnemyType.Troll:
                skillExperience.Add("sword", 25f);
                skillExperience.Add("heavy_armor", 15f);
                skillExperience.Add("block", 20f);
                break;
        }
        
        // Add skill experience based on the final attack
        switch (finalBlow)
        {
            case AttackType.QuickAttack:
                skillExperience.Add("agility", 5f);
                break;
            case AttackType.PowerAttack:
                skillExperience.Add("strength", 5f);
                break;
            case AttackType.CounterAttack:
                skillExperience.Add("block", 10f);
                break;
        }
        
        // Create action completion event
        var actionEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.CompleteAction,
            parameters = new Dictionary<string, object>
            {
                { "actionId", "defeat_enemy" },
                { "enemyType", enemyType.ToString() },
                { "relevantSkills", skillExperience }
            }
        };
        
        // Publish event
        eventBus.Publish(actionEvent);
        
        // Update reputation if this was a notable enemy
        if (enemyType == EnemyType.Troll)
        {
            var reputationEvent = new ProgressionEvent
            {
                type = ProgressionEvent.ProgressionEventType.ReputationImpact,
                parameters = new Dictionary<string, object>
                {
                    { "contextId", "town_riverwood" },
                    { "eventId", "defeated_troll" },
                    { "description", "Defeated a troll threatening the town" },
                    { "traitImpacts", new Dictionary<string, float>
                        {
                            { "bravery", 10f },
                            { "combat_prowess", 15f }
                        }
                    },
                    { "decayRate", 0.01f } // This will be remembered for a long time
                }
            };
            
            eventBus.Publish(reputationEvent);
        }
    }
}
```

This comprehensive example demonstrates how different aspects of the progression system work together to provide a rich, interconnected gameplay experience.