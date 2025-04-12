# Relationship Progression System Guide

## Overview

The Relationship Progression System is a powerful framework for managing character relationships in your game. It handles relationship stages, progression triggers, and key decision points that allow for dynamic storytelling and character development.

This system is designed to work alongside the larger Progression and Event System, providing specialized functionality for relationship management while integrating with existing game systems.

## Key Features

- **Relationship Stage Management**: Define and progress through different relationship stages like acquaintance, friend, close friend, etc.
- **Customizable Relationship Types**: Support for various relationship types such as friendship, romance, mentor, rival, etc.
- **Progression Triggers**: Automatically trigger events and stage advancements based on in-game conditions
- **Key Decision Points**: Present important decisions to players that impact relationship development
- **Event Integration**: Ties into the game's event system for seamless storytelling
- **Flexible Requirements**: Configure various requirement types for stage advancement (stats, events, time, skills, etc.)

## Getting Started

### 1. Setup in Unity

Add the `RelationshipProgressionSystem` component to a GameObject in your scene:

```csharp
// In your scene setup code or editor
GameObject systemObject = new GameObject("RelationshipSystems");
RelationshipProgressionSystem relationshipSystem = systemObject.AddComponent<RelationshipProgressionSystem>();
```

Typically, you'll want to add this to the same GameObject as your `ProgressionAndEventSystem` component or create a dedicated relationship manager GameObject.

### 2. Define Relationship Stages

Create relationship stages in your configuration:

```csharp
// Example of creating stages programmatically
RelationshipStage friendshipStage1 = new RelationshipStage
{
    Id = "friendship_acquaintance",
    Name = "Acquaintance",
    Description = "You've just met and are getting to know each other.",
    Level = 1,
    Type = RelationshipType.Friendship,
    AdvancementRequirements = new List<RelationshipRequirement>
    {
        new RelationshipRequirement
        {
            Type = RequirementType.StatValue,
            ParameterId = "trust",
            RequiredValue = 30.0f,
            IsMandatory = true,
            Description = "Build trust to 30"
        },
        new RelationshipRequirement
        {
            Type = RequirementType.EventCompletion,
            ParameterId = "event_introduction_complete",
            RequiredValue = 1.0f,
            IsMandatory = true,
            Description = "Complete introduction"
        }
    },
    UnlockedDialogueTopics = new List<string> { "basic_conversation", "background_questions" }
};

// Register the stage with the system
relationshipSystem.RegisterStage(friendshipStage1);
```

More commonly, you'll define these in JSON or ScriptableObjects and load them at runtime:

```csharp
// Example of stage defined in JSON (simplified)
{
  "Id": "romance_dating",
  "Name": "Dating",
  "Description": "You are in a romantic relationship.",
  "Level": 2,
  "Type": "Romance",
  "AdvancementRequirements": [
    {
      "Type": "StatValue",
      "ParameterId": "romance",
      "RequiredValue": 75.0,
      "IsMandatory": true,
      "Description": "Build romance to 75",
      "IsHidden": false
    },
    {
      "Type": "EventCompletion",
      "ParameterId": "event_special_date",
      "RequiredValue": 1.0,
      "IsMandatory": true,
      "Description": "Have a special date",
      "IsHidden": false
    }
  ],
  "UnlockedDialogueTopics": ["romantic_conversations", "future_plans", "personal_history"]
}
```

### 3. Define Progression Triggers

Progression triggers allow the game to automatically advance relationships or trigger events based on conditions:

```csharp
// Example trigger that activates after completing a specific event
ProgressionTrigger specialEventTrigger = new ProgressionTrigger
{
    Id = "trigger_friendship_activity",
    Description = "Triggers after completing a fun activity together",
    Type = TriggerType.EventCompletion,
    Conditions = new List<EventCondition>
    {
        // This would use an EventCondition that checks for event completion
        // Implementation depends on your event system
    },
    Effects = new List<TriggerEffect>
    {
        new TriggerEffect
        {
            Type = EffectType.ModifyStat,
            TargetId = "friendship",
            Value = 15.0f,
            Description = "Increase friendship"
        }
    },
    IsOneTime = true,
    Priority = 10
};

relationshipSystem.RegisterTrigger(specialEventTrigger);
```

### 4. Define Key Decisions

Key decisions are important choice points that can significantly impact relationship development:

```csharp
// Example of a key decision point
KeyDecision romanticConfession = new KeyDecision
{
    Id = "decision_romantic_confession",
    Title = "Romantic Confession",
    Description = "Do you want to confess your romantic feelings?",
    Choices = new List<DecisionChoice>
    {
        new DecisionChoice
        {
            Id = "choice_confess",
            Text = "Confess your feelings",
            Description = "Tell them how you feel romantically.",
            RelationshipChanges = new List<RelationshipEffect>
            {
                new RelationshipEffect
                {
                    CharacterId = "npc_alex",
                    ParameterId = "romance",
                    Value = 25.0f,
                    Description = "Increased romantic interest"
                }
            },
            UnlockedContent = new List<string>
            {
                "romance_path"
            }
        },
        new DecisionChoice
        {
            Id = "choice_stay_friends",
            Text = "Keep things friendly",
            Description = "Maintain your friendship for now.",
            RelationshipChanges = new List<RelationshipEffect>
            {
                new RelationshipEffect
                {
                    CharacterId = "npc_alex",
                    ParameterId = "friendship",
                    Value = 10.0f,
                    Description = "Strengthened friendship"
                }
            },
            UnlockedContent = new List<string>
            {
                "friendship_path"
            }
        }
    },
    IsForcedChoice = true
};

relationshipSystem.RegisterKeyDecision(romanticConfession);
```

## Using the System in Your Game

### Checking Relationship Stages

```csharp
// Get the current relationship stage between two characters
ICharacter player = GetPlayer();
ICharacter npc = GetNPC("npc_alex");

RelationshipStage currentStage = relationshipSystem.GetCurrentStage(player, npc);
Debug.Log($"Current relationship: {currentStage.Name} - {currentStage.Description}");

// Check progress to next stage
float progress = relationshipSystem.GetProgressToNextStage(player, npc);
Debug.Log($"Progress to next stage: {progress * 100}%");

// Get requirements for next stage advancement
List<RelationshipRequirement> requirements = relationshipSystem.GetNextStageRequirements(player, npc);
foreach (var req in requirements)
{
    Debug.Log($"Requirement: {req.Description}");
}
```

### Modifying Relationship Parameters

```csharp
// Increase a relationship parameter (e.g., after a positive interaction)
relationshipSystem.ModifyRelationshipParameter(player, npc, "trust", 5.0f);
relationshipSystem.ModifyRelationshipParameter(player, npc, "friendship", 3.0f);

// This automatically updates progress and may trigger stage advancements 
// or activate progression triggers
```

### Recording Events

```csharp
// Record that an event has been completed that affects the relationship
relationshipSystem.RecordEventCompletion(player, npc, "event_had_dinner_together");

// This will update relationship progress and may trigger progression events
```

### Presenting Key Decisions

```csharp
// Get pending decisions for a player
List<KeyDecision> pendingDecisions = relationshipSystem.GetPendingKeyDecisions(player);

// Present a decision to the player through your UI
if (pendingDecisions.Count > 0)
{
    KeyDecision decision = pendingDecisions[0];
    PresentDecisionInUI(decision); // Your UI presentation method
}

// When the player makes a choice:
string choiceId = "choice_confess"; // Example choice ID
relationshipSystem.MakeDecision("decision_romantic_confession", choiceId, player);
```

### Changing Relationship Types

```csharp
// Change a relationship from friendship to romance
relationshipSystem.ChangeRelationshipType(player, npc, RelationshipType.Romance);

// This will update the stage and fire appropriate events
```

## Saving and Loading

The system provides methods to save and load relationship data:

```csharp
// Get serializable relationship data for saving
Dictionary<string, RelationshipData> saveData = relationshipSystem.GetSerializableRelationshipData();
SaveToPlayerPrefs(JsonUtility.ToJson(saveData)); // Your save method

// Load relationship data when loading a game
string savedDataJson = LoadFromPlayerPrefs(); // Your load method
Dictionary<string, RelationshipData> loadedData = JsonUtility.FromJson<Dictionary<string, RelationshipData>>(savedDataJson);
relationshipSystem.LoadRelationshipData(loadedData);
```

## Event Callbacks

You can subscribe to various events to react to relationship changes:

```csharp
// Subscribe to relationship stage change events
relationshipSystem.OnRelationshipStageChanged += (player, npc, oldStage, newStage) =>
{
    Debug.Log($"Relationship advanced from {oldStage.Name} to {newStage.Name}!");
    ShowCelebrationEffect(); // Your visual effect
};

// Subscribe to key decision events
relationshipSystem.OnKeyDecisionRequired += (decision, player) =>
{
    Debug.Log($"Important decision required: {decision.Title}");
    ShowDecisionAlert(); // Your UI notification
};

// Subscribe to trigger activation
relationshipSystem.OnProgressionTriggerActivated += (triggerId, player, npc) =>
{
    Debug.Log($"Trigger activated: {triggerId}");
    // React to the trigger
};
```

## Integration with Dialog Systems

The relationship system works well with dialog systems by providing access to relationship stages and parameters:

```csharp
// Example of checking relationship status for dialog options
bool CanShowRomanticOption(ICharacter player, ICharacter npc)
{
    RelationshipStage stage = relationshipSystem.GetCurrentStage(player, npc);
    
    // Only show romantic options if in a romantic relationship or close friendship
    return stage.Type == RelationshipType.Romance || 
           (stage.Type == RelationshipType.Friendship && stage.Level >= 3);
}

// Example of unlocking dialog topics based on relationship stage
List<string> GetAvailableDialogTopics(ICharacter player, ICharacter npc)
{
    RelationshipStage stage = relationshipSystem.GetCurrentStage(player, npc);
    return stage.UnlockedDialogueTopics;
}
```

## Best Practices

1. **Start Simple**: Begin with a few basic relationship stages and expand as needed.
2. **Balance Requirements**: Make sure relationship advancement requirements are achievable through normal gameplay.
3. **Use Key Decisions Sparingly**: Reserve key decisions for truly important moments to avoid decision fatigue.
4. **Test Thoroughly**: Relationship systems can have complex interactions - test all possible paths.
5. **Provide Feedback**: Always inform players of relationship changes and progress.
6. **Consider Relationship Regression**: Sometimes relationships should be able to deteriorate, not just improve.

## Troubleshooting

### Relationships Not Progressing
- Check that all mandatory requirements for stage advancement are being met
- Verify that events are being properly recorded
- Ensure relationship parameters are being modified correctly

### Triggers Not Activating
- Check trigger conditions and dependencies
- Make sure the trigger hasn't already been activated (if it's one-time)
- Verify that the trigger priority isn't being overridden by higher-priority triggers

### Key Decisions Not Appearing
- Check that activation conditions are met
- Verify the decision hasn't already been made
- Ensure the decision hasn't expired

## Example: Full Relationship Path

Here's an example of how a complete friendship path might be set up:

1. **First Meeting**
   - Triggered by first conversation event
   - Basic interactions only

2. **Acquaintance**
   - Requirements: Complete introduction event, Trust > 20
   - Unlocks: Basic conversation topics
   - Decision Point: Exchange contact information

3. **Friend**
   - Requirements: 3+ shared activities, Trust > 40
   - Unlocks: Personal conversations, ability to invite to activities
   - Decision Point: Share a personal secret

4. **Close Friend**
   - Requirements: Trust > 70, Complete a personal quest for them
   - Unlocks: Deep conversations, special activities
   - Trigger: They may offer help with your quests
   - Decision Point: Support them in a conflict

5. **Best Friend**
   - Requirements: Trust > 90, Loyalty > 80
   - Unlocks: Special rewards, unique dialog options
   - Special event: Friendship celebration

This creates a clear progression path with meaningful stages, requirements, and rewards that enhance the gameplay experience.

## Advanced Configuration

For advanced users, the system supports:

- Complex conditional triggers with multiple conditions
- Stage regression based on negative actions
- Relationship type transitions (e.g., friendship to romance)
- Cross-character relationship effects
- Time-based requirements and cooldowns

Consult the API reference for more detailed information on these advanced features.