# Multi-Ending System Usage Guide

The Multi-Ending System provides a robust framework for implementing diverse branching endings in your game based on player choices, character relationships, and game progression. This document explains how to use the system effectively.

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [Setting Up Endings](#setting-up-endings)
4. [Character-Specific Endings](#character-specific-endings)
5. [Conditions and Progress](#conditions-and-progress)
6. [Ending Combinations](#ending-combinations)
7. [Time-Based Endings](#time-based-endings)
8. [Events and Callbacks](#events-and-callbacks)
9. [Integration Examples](#integration-examples)
10. [Best Practices](#best-practices)

## Overview

The Multi-Ending System manages:
- Individual game endings with specific achievement conditions
- Character relationship-based ending branches
- Special combination endings when multiple conditions are met
- Time-based endings triggered at specific game dates/times
- Progression tracking toward each possible ending

The system provides a way to define, track, trigger, and present different endings based on player actions and choices.

## Core Concepts

### GameEnding

A `GameEnding` represents a specific game conclusion with:
- Achievement conditions
- Quality level
- Presentation details (scenes, cutscenes, epilogue texts)
- Unlockable rewards

### EndingBranch

An `EndingBranch` defines relationship-specific outcomes for characters with:
- Character-specific conditions
- Relationship type constraints
- Unlockable content specific to that relationship path

### EndingCombination

An `EndingCombination` allows creating special endings that depend on achieving multiple other endings, with:
- Required and excluded ending combinations
- Synergistic and complementary effects
- Hidden special rewards

## Setting Up Endings

### 1. Create and Register Game Endings

```csharp
// Create a new ending
var goodEnding = new GameEnding
{
    Id = "good_ending",
    Title = "A New Beginning",
    Description = "The hero saves the kingdom and brings peace to the land.",
    Category = EndingCategory.Main,
    Type = EndingType.Good,
    QualityLevel = 8,
    Conditions = new List<EndingCondition>
    {
        new EndingCondition
        {
            Type = EndingConditionType.PlayerStat,
            TargetId = "reputation",
            Operator = ComparisonOperator.GreaterThanOrEqual,
            RequiredValue = 75f,
            Weight = 1.0f,
            IsMandatory = true,
            Description = "High reputation in the kingdom"
        },
        new EndingCondition
        {
            Type = EndingConditionType.EventCompleted,
            TargetId = "defeat_final_boss",
            Operator = ComparisonOperator.Contains,
            RequiredValue = true,
            Weight = 2.0f,
            IsMandatory = true,
            Description = "Defeated the final boss"
        }
    },
    EndingSceneId = "ending_scene_good",
    EpilogueTextIds = new List<string> { "epilogue_text_1", "epilogue_text_2" },
    UnlockedRewards = new List<string> { "achievement_hero", "gallery_good_ending" }
};

// Register the ending with the system
MultiEndingSystem.Instance.RegisterEnding(goodEnding);
```

### 2. Check Ending Progress

```csharp
// Get progress toward a specific ending
float goodEndingProgress = MultiEndingSystem.Instance.GetEndingProgress("good_ending", playerCharacter);
Debug.Log($"Progress toward good ending: {goodEndingProgress * 100}%");

// Get the most likely ending based on current progress
GameEnding likelyEnding = MultiEndingSystem.Instance.GetCurrentMostLikelyEnding(playerCharacter);
Debug.Log($"Most likely ending: {likelyEnding.Title}");

// Get all ending progresses
var allProgress = MultiEndingSystem.Instance.GetAllEndingProgresses(playerCharacter);
foreach (var pair in allProgress)
{
    Debug.Log($"Ending {pair.Key}: {pair.Value * 100}% complete");
}
```

### 3. Trigger an Ending

```csharp
// Play an ending when conditions are met
if (MultiEndingSystem.Instance.GetEndingProgress("good_ending", playerCharacter) >= 1.0f)
{
    MultiEndingSystem.Instance.PlayEnding("good_ending", playerCharacter);
}
```

## Character-Specific Endings

### 1. Create Character Ending Branches

```csharp
// Create a romantic relationship ending branch
var romanticEnding = new EndingBranch
{
    Id = "npc_ally_romance",
    CharacterId = "npc_ally",
    RelationshipType = RelationshipType.Romantic,
    BranchName = "True Love",
    Description = "A happy ending with your romantic partner",
    Conditions = new List<BranchCondition>
    {
        new BranchCondition
        {
            Type = EndingConditionType.RelationshipValue,
            ParameterId = "affection",
            Operator = ComparisonOperator.GreaterThanOrEqual, 
            ThresholdValue = 80f,
            IsCritical = true,
            HintText = "Develop a deep bond of affection"
        },
        new BranchCondition
        {
            Type = EndingConditionType.EventCompleted,
            ParameterId = "romance_confession",
            Operator = ComparisonOperator.Contains,
            ThresholdValue = true,
            IsCritical = true,
            HintText = "Confess your feelings"
        }
    },
    UnlockedContent = new List<string> { "artwork_romance", "bonus_epilogue" }
};

// Register the branch
MultiEndingSystem.Instance.RegisterEndingBranch(romanticEnding);
```

### 2. Check Available Branches

```csharp
// Get all available branches for a specific NPC
var availableBranches = MultiEndingSystem.Instance.GetAvailableBranches(playerCharacter, npcCharacter);
foreach (var branch in availableBranches)
{
    Debug.Log($"Available ending branch: {branch.BranchName}");
}

// Get the current most appropriate branch
EndingBranch currentBranch = MultiEndingSystem.Instance.GetCurrentBranch(playerCharacter, npcCharacter);
```

### 3. Check Remaining Conditions

```csharp
// Get remaining conditions to achieve a branch
var remainingConditions = MultiEndingSystem.Instance.GetRemainingConditions("npc_ally_romance", playerCharacter, npcCharacter);
foreach (var condition in remainingConditions)
{
    Debug.Log($"Remaining condition: {condition.HintText}");
}
```

## Conditions and Progress

The system supports various condition types to evaluate ending progress:

1. **RelationshipValue**: Character relationship parameters
   ```csharp
   new EndingCondition
   {
       Type = EndingConditionType.RelationshipValue,
       TargetId = "npc_ally:trust", // Format: "CharacterId:ParameterName"
       Operator = ComparisonOperator.GreaterThanOrEqual,
       RequiredValue = 75f
   }
   ```

2. **EventCompleted**: Game events that must be completed
   ```csharp
   new EndingCondition
   {
       Type = EndingConditionType.EventCompleted,
       TargetId = "defeat_dragon",
       Operator = ComparisonOperator.Contains,
       RequiredValue = true
   }
   ```

3. **ItemPossession**: Items the player must possess
   ```csharp
   new EndingCondition
   {
       Type = EndingConditionType.ItemPossession,
       TargetId = "legendary_sword",
       Operator = ComparisonOperator.Contains,
       RequiredValue = true
   }
   ```

4. **PlayerStat**: Player statistics requirements
   ```csharp
   new EndingCondition
   {
       Type = EndingConditionType.PlayerStat,
       TargetId = "strength",
       Operator = ComparisonOperator.GreaterThanOrEqual,
       RequiredValue = 50f
   }
   ```

5. **CharacterState**: Character state requirements
   ```csharp
   new EndingCondition
   {
       Type = EndingConditionType.CharacterState,
       TargetId = "is_transformed",
       Operator = ComparisonOperator.Equal,
       RequiredValue = true
   }
   ```

6. **TimeBased**: Time-related conditions
   ```csharp
   new EndingCondition
   {
       Type = EndingConditionType.TimeBased,
       TargetId = "game_days",
       Operator = ComparisonOperator.LessThan,
       RequiredValue = 30
   }
   ```

## Ending Combinations

### 1. Create Ending Combinations

```csharp
// Create a special combined ending
var trueEnding = new EndingCombination
{
    Id = "true_ending",
    Name = "The True Path",
    Description = "The ultimate ending achieved by mastering all aspects of the game",
    RequiredEndingIds = new List<string> { "good_ending", "ally_romance_ending", "secret_artifact_ending" },
    ExcludedEndingIds = new List<string> { "bad_ending", "betrayal_ending" },
    Type = EndingCombinationType.Synergistic,
    CombinedEndingId = "ultimate_ending",
    IsHidden = true,
    UnlockedRewards = new List<string> { "achievement_true_hero", "ng_plus_bonus" }
};

// Register the combination
MultiEndingSystem.Instance.RegisterEndingCombination(trueEnding);
```

### 2. Check for Combinations

```csharp
// Get player's achieved endings
var achievedEndings = _achievedEndings[playerCharacter.Id];

// Check if any combinations are possible
EndingCombination combination = MultiEndingSystem.Instance.GetMatchingCombination(achievedEndings);
if (combination != null)
{
    Debug.Log($"Unlocked combined ending: {combination.Name}");
    
    // Get the combined ending details
    GameEnding combinedEnding = MultiEndingSystem.Instance.GetCombinedEnding(achievedEndings);
    // Trigger the special ending
    MultiEndingSystem.Instance.PlayEnding(combinedEnding.Id, playerCharacter);
}
```

### 3. Get Recommendations

```csharp
// Get recommended additional endings to complete a combination
var recommendations = MultiEndingSystem.Instance.GetRecommendedAdditionalEndings(achievedEndings);
foreach (var endingId in recommendations)
{
    Debug.Log($"Consider going for ending: {endingId}");
}
```

## Time-Based Endings

### 1. Set up Time-Based Endings

```csharp
// Create a time-based ending
var timeEnding = new GameEnding
{
    Id = "time_ending",
    Title = "The Sands of Time",
    Description = "The consequences of waiting too long to act",
    Category = EndingCategory.TimeBase,
    Type = EndingType.Neutral,
    Conditions = new List<EndingCondition>
    {
        new EndingCondition
        {
            Type = EndingConditionType.TimeBased,
            TargetId = "game_day",
            Operator = ComparisonOperator.GreaterThanOrEqual,
            RequiredValue = 365,
            Weight = 1.0f,
            IsMandatory = true
        }
    },
    EndingData = new Dictionary<string, object>
    {
        { "IsTimeBased", true },
        { "MinDate", new GameDate { Year = 2, Month = 1, Day = 1, Hour = 0, Minute = 0 } },
        { "MaxDate", new GameDate { Year = 3, Month = 1, Day = 1, Hour = 0, Minute = 0 } }
    }
};

// Register it
MultiEndingSystem.Instance.RegisterEnding(timeEnding);
```

### 2. Check for Time-Based Endings

```csharp
// In your game's time update loop:
GameDate currentDate = gameWorld.CurrentDate;
GameEnding timeBasedEnding = MultiEndingSystem.Instance.GetTimeBasedEnding(playerCharacter, currentDate);

if (timeBasedEnding != null)
{
    Debug.Log($"Time-based ending triggered: {timeBasedEnding.Title}");
    float quality = MultiEndingSystem.Instance.GetTimeBasedEndingQuality(playerCharacter, currentDate);
    Debug.Log($"Ending quality: {quality * 100}%");
    
    // Optionally trigger the ending
    MultiEndingSystem.Instance.PlayEnding(timeBasedEnding.Id, playerCharacter);
}
```

## Events and Callbacks

Subscribe to the system's events to react to ending-related situations:

```csharp
void Start()
{
    // Subscribe to events
    MultiEndingSystem.Instance.OnEndingConditionMet += HandleEndingConditionMet;
    MultiEndingSystem.Instance.OnEndingUnlocked += HandleEndingUnlocked;
    MultiEndingSystem.Instance.OnEndingPlayed += HandleEndingPlayed;
    MultiEndingSystem.Instance.OnEndingCombinationDiscovered += HandleCombinationDiscovered;
    MultiEndingSystem.Instance.OnTimeBasedEndingTriggered += HandleTimeBasedEnding;
    MultiEndingSystem.Instance.OnEndingGalleryUpdated += HandleGalleryUpdated;
}

void OnDestroy()
{
    // Unsubscribe from events
    MultiEndingSystem.Instance.OnEndingConditionMet -= HandleEndingConditionMet;
    MultiEndingSystem.Instance.OnEndingUnlocked -= HandleEndingUnlocked;
    MultiEndingSystem.Instance.OnEndingPlayed -= HandleEndingPlayed;
    MultiEndingSystem.Instance.OnEndingCombinationDiscovered -= HandleCombinationDiscovered;
    MultiEndingSystem.Instance.OnTimeBasedEndingTriggered -= HandleTimeBasedEnding;
    MultiEndingSystem.Instance.OnEndingGalleryUpdated -= HandleGalleryUpdated;
}

// Event handlers
private void HandleEndingConditionMet(string endingId, string conditionId)
{
    // Show a hint or notification to the player
    UIManager.ShowNotification($"You've made progress toward an ending: {conditionId}");
}

private void HandleEndingUnlocked(string endingId)
{
    var ending = MultiEndingSystem.Instance._endings[endingId];
    UIManager.ShowAchievement($"Ending Unlocked: {ending.Title}");
}

private void HandleEndingPlayed(string endingId)
{
    // Record statistics, add to player's ending gallery, etc.
    PlayerData.AddToEndingGallery(endingId);
}

private void HandleCombinationDiscovered(string combinationId)
{
    var combination = MultiEndingSystem.Instance._combinations[combinationId];
    UIManager.ShowSpecialAchievement($"Special Ending Discovered: {combination.Name}");
}

private void HandleTimeBasedEnding(string endingId, GameDate date)
{
    Debug.Log($"Time-based ending {endingId} triggered at game date: Y{date.Year} M{date.Month} D{date.Day}");
}

private void HandleGalleryUpdated(string endingId)
{
    UIManager.UpdateEndingGallery();
}
```

## Integration Examples

### Main Game Loop Integration

```csharp
void Update()
{
    // Regularly check for potential time-based endings
    if (timeSystem.HasDayChanged)
    {
        GameEnding timeEnding = MultiEndingSystem.Instance.GetTimeBasedEnding(playerCharacter, timeSystem.CurrentDate);
        if (timeEnding != null)
        {
            // Trigger the ending sequence
            gameStateMachine.TransitionTo(GameState.EndingSequence);
            endingManager.PlayEnding(timeEnding.Id);
        }
    }
    
    // Update UI with ending progress
    if (uiUpdateTimer <= 0)
    {
        UpdateEndingProgressUI();
        uiUpdateTimer = uiUpdateInterval;
    }
    else
    {
        uiUpdateTimer -= Time.deltaTime;
    }
}

void UpdateEndingProgressUI()
{
    // Only show progress for discovered endings
    foreach (var endingId in discoveredEndings)
    {
        float progress = MultiEndingSystem.Instance.GetEndingProgress(endingId, playerCharacter);
        endingProgressUI.UpdateProgress(endingId, progress);
    }
}
```

### Dialogue System Integration

```csharp
void OnDialogueOptionSelected(string optionId, string npcId)
{
    // Update relationship values
    relationshipSystem.ModifyRelationship(playerCharacter.Id, npcId, "trust", 5f);
    
    // Check for relationship progress
    ICharacter npc = characterManager.GetCharacter(npcId);
    EndingBranch currentBranch = MultiEndingSystem.Instance.GetCurrentBranch(playerCharacter, npc);
    
    if (currentBranch != null)
    {
        var remainingConditions = MultiEndingSystem.Instance.GetRemainingConditions(currentBranch.Id, playerCharacter, npc);
        
        // If only one condition remains, give a hint
        if (remainingConditions.Count == 1 && showHints)
        {
            UIManager.ShowHint($"Hint: {remainingConditions[0].HintText}");
        }
    }
}
```

### Save/Load System Integration

```csharp
// Serialize ending progress for save data
public void SaveEndingProgress(SaveData saveData)
{
    saveData.achievedEndings = new Dictionary<string, List<string>>();
    foreach (var pair in MultiEndingSystem.Instance._achievedEndings)
    {
        saveData.achievedEndings[pair.Key] = new List<string>(pair.Value);
    }
    
    saveData.endingProgress = new Dictionary<string, Dictionary<string, float>>();
    foreach (var playerPair in MultiEndingSystem.Instance._endingProgress)
    {
        saveData.endingProgress[playerPair.Key] = new Dictionary<string, float>();
        foreach (var endingPair in playerPair.Value)
        {
            saveData.endingProgress[playerPair.Key][endingPair.Key] = endingPair.Value;
        }
    }
}

// Load ending progress from save data
public void LoadEndingProgress(SaveData saveData)
{
    if (saveData.achievedEndings != null)
    {
        MultiEndingSystem.Instance._achievedEndings.Clear();
        foreach (var pair in saveData.achievedEndings)
        {
            MultiEndingSystem.Instance._achievedEndings[pair.Key] = new List<string>(pair.Value);
        }
    }
    
    if (saveData.endingProgress != null)
    {
        MultiEndingSystem.Instance._endingProgress.Clear();
        foreach (var playerPair in saveData.endingProgress)
        {
            MultiEndingSystem.Instance._endingProgress[playerPair.Key] = new Dictionary<string, float>();
            foreach (var endingPair in playerPair.Value)
            {
                MultiEndingSystem.Instance._endingProgress[playerPair.Key][endingPair.Key] = endingPair.Value;
            }
        }
    }
}
```

## Best Practices

### Ending Design Tips

1. **Balance Visibility and Mystery**
   - Make some ending conditions visible to guide players
   - Keep some conditions hidden for surprise and replayability
   - Use the `IsHidden` flag on conditions strategically

2. **Layer Ending Requirements**
   - Create a mix of easy-to-achieve and challenging endings
   - Use `IsMandatory` for critical conditions that cannot be bypassed
   - Set appropriate `Weight` values to reflect the importance of each condition

3. **Create Meaningful Choices**
   - Ensure endings reflect player's choices throughout the game
   - Design mutually exclusive endings that encourage replaying
   - Make special combination endings feel rewarding for dedicated players

### Technical Tips

1. **Performance Optimization**
   - Cache progress calculations where possible
   - Only update progress for visible/discovered endings in the UI
   - Use the weight system to prioritize checking important conditions first

2. **Debugging**
   - Create a debugging UI to visualize ending progress
   - Log when conditions are met using the `OnEndingConditionMet` event
   - Implement cheat codes to unlock specific endings for testing

3. **Extensibility**
   - Create ScriptableObjects for ending definitions
   - Implement custom condition types for unique game mechanics
   - Build a visual editor for designers to create and edit endings

---

This system provides a robust framework for creating meaningful, branching endings in your game that reflect player choices and create a sense of agency and consequence. By following this guide, you can implement a rich set of endings that encourage replayability and player engagement.