# Player Progression System - Quick Start Guide

This guide will help you quickly set up and integrate the Player Progression System into your Unity project.

## Basic Setup

### 1. Create Configuration Assets

1. Right-click in your Project window
2. Select **Create > Systems > Player Progression Config**
3. Select **Create > Systems > Event Bus**

### 2. Add the Manager Component

1. Create a GameObject in your scene (preferably in a scene that persists, like your main Game Manager)
2. Add the `PlayerProgressionManager` component to this GameObject
3. Assign your created Configuration and Event Bus assets to the respective fields

### 3. Initialize on Game Start

```csharp
// In your game initialization code:
playerProgressionManager.Initialize("player1");
```

## Configuring Your Progression System

### Stats Configuration

1. Select your PlayerProgressionConfig asset
2. In the Inspector, add entries to the Initial Stats list
3. For each stat, configure:
   - Stat ID (e.g., "strength", "intelligence")
   - Stat Name (display name)
   - Base Value (starting value)
   - Min and Max Values (limits)
   - Growth Rate

Example:
```
Stat ID: "strength"
Stat Name: "Strength"
Base Value: 10
Min Value: 1
Max Value: 100
Growth Rate: 0.1
```

### Skills Configuration

1. In the PlayerProgressionConfig asset, add entries to the Skill Categories list
2. For each category (e.g., "Combat", "Magic"), add individual skills
3. Configure each skill:
   - Skill ID (e.g., "sword", "fireball")
   - Skill Name (display name)
   - Initial Level Threshold (XP needed for level 1)
   - Level Threshold Multiplier (increases difficulty per level)
   - Requirements (if any)

Example:
```
Category ID: "combat"
Category Name: "Combat Skills"
Skills:
  - Skill ID: "sword"
    Skill Name: "Sword Fighting"
    Initial Level Threshold: 100
    Level Threshold Multiplier: 1.5
```

### Reputation Configuration

1. In the PlayerProgressionConfig asset, add entries to the Reputation Contexts list
2. For each context (e.g., "town_riverwood", "faction_mages_guild"), configure:
   - Context ID (unique identifier)
   - Context Name (display name)
   - Relevant Traits (e.g., "honesty", "generosity", "combat_prowess")

Example:
```
Context ID: "town_riverwood"
Context Name: "Town of Riverwood"
Relevant Traits: ["honesty", "generosity", "helpfulness"]
```

## Basic Usage

### Increasing Stats

```csharp
// Create a stat change event
var statChangeEvent = new ProgressionEvent
{
    type = ProgressionEvent.ProgressionEventType.StatChange,
    parameters = new Dictionary<string, object>
    {
        { "statId", "strength" },
        { "baseValueChange", 1.0f } // Increase strength by 1
    }
};

// Publish the event through your event bus
eventBus.Publish(statChangeEvent);
```

### Adding Temporary Stat Modifiers

```csharp
// Create a temporary stat modifier (e.g., for a potion effect)
var strengthPotion = new PlayerStats.StatModifier(
    "potion_effect", // Source identifier
    5.0f,            // Value to add
    PlayerStats.ModifierType.Additive, // Addition type
    60.0f            // Duration in seconds
);

// Create and publish the event
var modifierEvent = new ProgressionEvent
{
    type = ProgressionEvent.ProgressionEventType.StatChange,
    parameters = new Dictionary<string, object>
    {
        { "statId", "strength" },
        { "modifier", strengthPotion }
    }
};

eventBus.Publish(modifierEvent);
```

### Adding Skill Experience

```csharp
// Add experience to a skill
var skillEvent = new ProgressionEvent
{
    type = ProgressionEvent.ProgressionEventType.SkillExperience,
    parameters = new Dictionary<string, object>
    {
        { "skillId", "sword" },
        { "experienceAmount", 25.0f }
    }
};

eventBus.Publish(skillEvent);
```

### Changing Reputation

```csharp
// Create a reputation impact event
var reputationEvent = new ProgressionEvent
{
    type = ProgressionEvent.ProgressionEventType.ReputationImpact,
    parameters = new Dictionary<string, object>
    {
        { "contextId", "town_riverwood" },
        { "traitImpacts", new Dictionary<string, float>
            {
                { "honesty", 5.0f },
                { "helpfulness", 3.0f }
            }
        },
        { "description", "Returned lost items to townspeople" },
        { "decayRate", 0.05f } // How quickly the impact fades
    }
};

eventBus.Publish(reputationEvent);
```

### Reading Values

```csharp
// Get a stat value
StatValue strengthStat = playerProgressionManager.GetStatValue("strength");
float currentStrength = strengthStat.CurrentValue;

// Get a skill level
float swordSkill = playerProgressionManager.GetSkillLevel("sword");

// Get reputation
float townReputation = playerProgressionManager.GetReputationScore("town_riverwood");
float honestyReputation = playerProgressionManager.GetReputationScore("town_riverwood", "honesty");
```

### Saving and Loading

```csharp
// Save progression data
ProgressionSaveData saveData = playerProgressionManager.GenerateSaveData();
// Store this data with your game's save system

// Load progression data
playerProgressionManager.RestoreFromSaveData(loadedSaveData);
```

## Integration with Game Systems

### Example: Quest Completion Rewards

```csharp
public void CompleteQuest(string questId)
{
    // Update reputation with the quest giver
    var reputationEvent = new ProgressionEvent
    {
        type = ProgressionEvent.ProgressionEventType.ReputationImpact,
        parameters = new Dictionary<string, object>
        {
            { "contextId", "faction_mages_guild" },
            { "traitImpacts", new Dictionary<string, float>
                {
                    { "reliability", 10.0f },
                    { "magical_aptitude", 5.0f }
                }
            },
            { "description", $"Completed quest: {questId}" }
        }
    };
    
    eventBus.Publish(reputationEvent);
    
    // Award skill experience based on quest type
    if (questId == "retrieve_magical_artifact")
    {
        var skillEvent = new ProgressionEvent
        {
            type = ProgressionEvent.ProgressionEventType.SkillExperience,
            parameters = new Dictionary<string, object>
            {
                { "skillId", "arcane_knowledge" },
                { "experienceAmount", 50.0f }
            }
        };
        
        eventBus.Publish(skillEvent);
    }
}
```

### Example: Combat System Integration

```csharp
public void DealDamage(float damage, string weaponType)
{
    // Award skill experience based on weapon used
    var skillEvent = new ProgressionEvent
    {
        type = ProgressionEvent.ProgressionEventType.SkillExperience,
        parameters = new Dictionary<string, object>
        {
            { "skillId", weaponType },
            { "experienceAmount", damage / 5.0f } // Experience scales with damage
        }
    };
    
    eventBus.Publish(skillEvent);
}
```

## Next Steps

For more detailed information on the Player Progression System:

1. Review the full [DOCUMENTATION.md](./DOCUMENTATION.md) file
2. Explore the API through code comments
3. Check out the example implementations in the code

If you're extending the system:
- Create custom implementations of the interfaces
- Use the dependency injection mechanism to swap in custom systems
- Leverage the command pattern for operations that need undo functionality