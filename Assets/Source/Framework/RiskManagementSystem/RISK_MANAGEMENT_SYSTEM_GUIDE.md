# Risk Management System - User Guide

## Overview

The Risk Management System adds tension and strategic decision-making to your game by calculating and managing risk elements associated with player actions. This system handles:

- Risk value calculations for actions
- Suspicion accumulation and decay
- Crisis generation and management
- Strategic risk avoidance options

This guide will help you integrate and utilize the Risk Management System in your Unity project.

## Table of Contents

1. [System Setup](#system-setup)
2. [Core Concepts](#core-concepts)
3. [Calculating Risk](#calculating-risk)
4. [Managing Suspicion](#managing-suspicion)
5. [Handling Crises](#handling-crises)
6. [UI Integration](#ui-integration)
7. [Configuration](#configuration)
8. [Examples](#examples)
9. [Advanced Usage](#advanced-usage)

## System Setup

### Installation

1. Add the `RiskManagementSystem.cs` script to your project
2. Create a GameObject in your scene and attach the `RiskManager` component

```csharp
// In your game initialization script
if (RiskManager.Instance == null)
{
    GameObject go = new GameObject("RiskManager");
    go.AddComponent<RiskManager>();
    DontDestroyOnLoad(go);
}
```

### Required Assets

Create the following ScriptableObjects through the Unity menu (Assets > Create > Risk Management):

1. **RiskActionData** - Defines base risk values for different action types
2. **CrisisTemplateData** - Defines types of crises that can occur
3. **RiskModifierProfiles** - Defines global and entity-specific risk modifiers
4. **RiskManagementConfig** - Configures system parameters
5. **RiskEventBus** - Handles the event system for risk-related events

Assign these assets to the RiskManager component in the Inspector.

## Core Concepts

### Action Types

The system classifies risky actions into different types (e.g., Theft, Trespassing, Assault). Each action type has base risk values that can be modified by context, character traits, and other factors.

### Risk Categories

Risk is divided into categories:
- Legal
- Social
- Physical
- Digital
- Psychological

This allows characters to have different sensitivities to different types of risks.

### Context

Risk calculations consider the context of actions:
- Time of day
- Location
- Presence of witnesses
- Character skills
- Items carried

### Suspicion

Suspicion represents how much certain characters suspect the player. It:
- Increases when risky actions are performed
- Decays naturally over time
- Can spread between characters

### Crises

When suspicion reaches critical levels, a crisis can occur. Crises:
- Develop in stages
- Have deadlines for resolution
- Offer multiple resolution options
- Can have varied outcomes based on player actions

## Calculating Risk

### Basic Risk Calculation

To calculate risk for a player action:

```csharp
// Create context data
ContextData context = new ContextData
{
    isNighttime = TimeManager.IsNight(),
    hasWitnesses = AreaManager.HasNPCsInRange(transform.position, 10f),
    crowdDensity = AreaManager.GetCrowdDensity(transform.position),
    availableItems = playerInventory.GetItemIDs(),
    locationID = currentLocation.ID,
    securityLevel = currentLocation.SecurityLevel
};

// Calculate risk
float riskValue = RiskManager.Instance.CalculateActionRisk(
    ActionType.Theft, 
    playerCharacter.ID, 
    context
);

// Display to player
UI_Manager.ShowRiskLevel(riskValue);
```

### Predicting Risk

You can predict risk before taking an action to allow players to make informed decisions:

```csharp
float predictedRisk = RiskManager.Instance.PredictRisk(
    ActionType.Theft, 
    playerCharacter.ID, 
    context
);

// Show prediction in UI
UI_Manager.ShowPredictedRisk(predictedRisk);
```

### Recording Actions

After a player completes an action, record it to update suspicion:

```csharp
bool actionSuccessful = /* check if action succeeded */;

RiskManager.Instance.RecordAction(
    ActionType.Theft,
    playerCharacter.ID,
    riskValue,
    actionSuccessful,
    "Stealing from shop in town center"
);
```

## Managing Suspicion

### Checking Suspicion Levels

Monitor suspicion levels for key characters:

```csharp
float guardSuspicion = RiskManager.Instance.GetSuspicionLevel("Guard_Captain");

if (guardSuspicion > 0.5f)
{
    // The guard captain is getting suspicious
    // Implement appropriate game response
}
```

### Modifying Suspicion

You can directly modify suspicion for game events:

```csharp
// Reduce suspicion through bribery
RiskManager.Instance.ModifySuspicion(
    "Guard_Captain", 
    -0.2f, 
    "Bribery"
);

// Increase suspicion through evidence
RiskManager.Instance.ModifySuspicion(
    "Detective", 
    0.3f, 
    "Found incriminating evidence"
);
```

### Modifying Risk Profiles

Add temporary modifiers to affect risk calculations:

```csharp
// Create a time-limited risk modifier (e.g., disguise effect)
RiskModifier disguiseModifier = new RiskModifier
{
    modifierID = "Disguise_Effect",
    type = ModifierType.Multiplicative,
    value = 0.5f, // 50% of normal risk
    expirationTime = DateTime.Now.AddHours(1) // Lasts for 1 hour
};

// Apply to player
RiskManager.Instance.AddRiskModifier(playerCharacter.ID, disguiseModifier);

// Later, remove if needed
RiskManager.Instance.RemoveRiskModifier(playerCharacter.ID, "Disguise_Effect");
```

## Handling Crises

### Checking for Active Crises

Monitor if the player has any active crises:

```csharp
List<ActiveCrisis> activeCrises = RiskManager.Instance.GetActiveCrises();

foreach (var crisis in activeCrises)
{
    Debug.Log($"Active crisis: {crisis.templateID} at stage {crisis.currentStageIndex}");
    // Update UI or game state based on active crises
}
```

### Getting Crisis Details

Examine a specific crisis:

```csharp
ActiveCrisis crisis = RiskManager.Instance.GetCrisis(crisisID);

if (crisis != null)
{
    float timeRemaining = (crisis.estimatedResolutionDeadline - DateTime.Now).TotalHours;
    Debug.Log($"Crisis deadline in {timeRemaining} hours");
    
    // Get available resolutions
    List<CrisisResolution> resolutions = RiskManager.Instance.GetAvailableResolutions(crisisID);
    // Show these options to the player
}
```

### Resolving Crises

When the player selects a resolution method:

```csharp
// Player chooses to resolve the crisis by paying a bribe
Dictionary<string, float> parameters = new Dictionary<string, float>
{
    { "BribeAmount", 500f },
    { "item:FakeID", 1f }
};

bool success = RiskManager.Instance.AttemptCrisisResolution(
    crisisID, 
    "Bribery_Resolution", 
    parameters
);

if (success)
{
    // Handle successful resolution
    PlayerInventory.RemoveMoney(500);
    PlayerInventory.RemoveItem("FakeID");
}
else
{
    // Handle failed resolution
    // Maybe increase suspicion further or advance crisis
}
```

## UI Integration

### Risk Feedback

The system includes a `RiskFeedback` component that provides visual feedback:

```csharp
// Connect to your UI elements in your UI manager
public void ConnectRiskUIElements()
{
    // Example - listening for risk calculation events
    RiskManager.Instance.eventBus.Subscribe<RiskValueCalculatedEvent>(OnRiskCalculated);
    RiskManager.Instance.eventBus.Subscribe<SuspicionChangedEvent>(OnSuspicionChanged);
    RiskManager.Instance.eventBus.Subscribe<CrisisTriggeredEvent>(OnCrisisTriggered);
}

private void OnRiskCalculated(RiskValueCalculatedEvent evt)
{
    // Update UI risk indicator
    riskIndicator.fillAmount = evt.RiskValue;
    
    if (evt.RiskValue > 0.7f)
    {
        riskIndicator.color = Color.red;
    }
    else if (evt.RiskValue > 0.4f)
    {
        riskIndicator.color = Color.yellow;
    }
    else
    {
        riskIndicator.color = Color.green;
    }
}
```

### Crisis Notifications

Display crisis information to players:

```csharp
private void OnCrisisTriggered(CrisisTriggeredEvent evt)
{
    // Show crisis notification UI
    crisisPanel.gameObject.SetActive(true);
    crisisTitleText.text = evt.CrisisName;
    crisisDescriptionText.text = GetCrisisDescription(evt.CrisisType);
    crisisDeadlineText.text = $"Resolve by: {evt.Deadline.ToString("g")}";
    
    // Store crisis ID for resolution
    currentCrisisID = evt.CrisisID;
}
```

## Configuration

### Risk Action Data

Configure `RiskActionDataSO` to define base risk values:

```
Action: Theft
Base Risk: 0.4
Minimum Risk: 0.1
Maximum Risk: 0.8
Categories: Legal, Social
Context Factors:
  - ItemValue: 0.5 (higher value = higher risk)
  - SecurityLevel: 0.8 (higher security = higher risk)
```

### Crisis Templates

Configure `CrisisTemplateSO` to define potential crises:

```
Crisis: Police_Investigation
Type: Legal
Trigger: Suspicion > 0.7 in Legal category
Stages:
  1. Initial Inquiry (25% of total time)
  2. Active Investigation (50% of total time)
  3. Final Confrontation (25% of total time)
Resolutions:
  - Bribery (Requires: Money, Connections skill)
  - Flee City (Requires: Vehicle, reduces reputation)
  - Destroy Evidence (Requires: specific items)
```

### System Configuration

Tune the `RiskManagementConfigSO` to balance gameplay:

```
Risk Calculation:
  - Default Base Risk: 0.3
  - Maximum Risk Value: 1.0
  - Minimum Risk Value: 0.05
  
Suspicion:
  - Base Decay Rate: 0.01 per hour
  - Minimum Suspicion Threshold: 0.05
  
Crisis:
  - Default Crisis Duration: 24 hours
  - Crisis Stage Progression: 0.25 per hour
```

## Examples

### Stealth Gameplay

For a stealth sequence:

```csharp
private void UpdateStealthRisk()
{
    // Create context with current state
    ContextData context = new ContextData
    {
        isNighttime = timeSystem.IsNight,
        hasWitnesses = stealthSystem.IsPlayerDetected(),
        crowdDensity = 0f, // No crowd in stealth area
        securityLevel = currentZone.SecurityLevel,
        availableItems = playerInventory.GetItemIDs()
    };
    
    // Check risk for trespassing
    float currentRisk = RiskManager.Instance.CalculateActionRisk(
        ActionType.Trespassing,
        playerID,
        context
    );
    
    // Update UI stealth meter
    stealthMeter.SetRiskLevel(currentRisk);
    
    // If a guard spots the player
    if (guardDetectionSystem.IsPlayerSpotted())
    {
        // Record the action and increase suspicion
        RiskManager.Instance.RecordAction(
            ActionType.Trespassing,
            playerID,
            currentRisk,
            false, // Failed to remain hidden
            "Spotted during infiltration"
        );
    }
}
```

### Social Deception

For a social infiltration:

```csharp
public void AttemptDeception(NPC target)
{
    // Create context
    ContextData context = new ContextData
    {
        crowdDensity = socialSystem.GetRoomCrowdLevel(),
        hasWitnesses = socialSystem.GetNearbyNPCs().Count > 1,
        availableItems = playerInventory.GetItemIDs(),
        contextualFactors = new Dictionary<string, float>
        {
            { "AlcoholConsumed", socialSystem.GetTargetIntoxicationLevel(target.ID) },
            { "PriorRelationship", relationshipSystem.GetRelationshipValue(playerID, target.ID) }
        }
    };
    
    // Calculate risk of being caught in the lie
    float deceptionRisk = RiskManager.Instance.CalculateActionRisk(
        ActionType.FalseIdentity,
        playerID,
        context
    );
    
    // Player decides to proceed
    bool deceptionSuccessful = dialogueSystem.AttemptDeception(deceptionRisk);
    
    // Record the result
    RiskManager.Instance.RecordAction(
        ActionType.FalseIdentity,
        playerID,
        deceptionRisk,
        deceptionSuccessful,
        $"Attempted to deceive {target.Name} at {locationSystem.GetCurrentLocationName()}"
    );
    
    if (!deceptionSuccessful)
    {
        // Target becomes suspicious
        RiskManager.Instance.ModifySuspicion(
            target.ID,
            0.25f,
            "Failed deception attempt"
        );
    }
}
```

## Advanced Usage

### Custom Risk Modifiers

Create custom conditions for risk modifiers:

```csharp
// Create a custom risk modifier for sneaking
RiskModifier stealthModifier = new RiskModifier
{
    modifierID = "Stealth_Training",
    type = ModifierType.Multiplicative,
    value = 0.7f, // 70% of normal risk
    condition = new RiskModifierCondition
    {
        requiresNighttime = true,
        crowdDensityMaximum = 0.3f,
        applicableActionTypes = new List<ActionType> { 
            ActionType.Theft, 
            ActionType.Trespassing 
        }
    }
};

RiskManager.Instance.AddRiskModifier(playerID, stealthModifier);
```

### Custom Crisis Generation

Trigger specific crisis types based on game events:

```csharp
public void TriggerCustomCrisis()
{
    // Force a crisis check for the player
    // Useful after major story events
    RiskManager.Instance.CrisisGenerator.CheckForCrisisTrigger(
        playerID,
        0.8f, // High suspicion level
        new List<RiskCategory> { RiskCategory.Social, RiskCategory.Digital }
    );
}
```

### Relationship Integration

Integrate with a relationship system:

```csharp
// When reputation with a faction changes
public void OnFactionReputationChanged(string factionID, float newReputation)
{
    // Get all NPCs in this faction
    List<string> factionMembers = factionSystem.GetFactionMembers(factionID);
    
    foreach (string npcID in factionMembers)
    {
        // Modify suspicion based on reputation
        float suspicionChange = -(newReputation / 100) * 0.2f;
        
        RiskManager.Instance.ModifySuspicion(
            npcID,
            suspicionChange,
            "Faction reputation change"
        );
    }
}
```

### Event-Based Game Logic

Use the event system to trigger game logic:

```csharp
private void OnEnable()
{
    // Subscribe to crisis events
    RiskManager.Instance.eventBus.Subscribe<CrisisTriggeredEvent>(OnCrisisTriggered);
    RiskManager.Instance.eventBus.Subscribe<CrisisResolvedEvent>(OnCrisisResolved);
}

private void OnDisable()
{
    // Unsubscribe from events
    RiskManager.Instance.eventBus.Unsubscribe<CrisisTriggeredEvent>(OnCrisisTriggered);
    RiskManager.Instance.eventBus.Unsubscribe<CrisisResolvedEvent>(OnCrisisResolved);
}

private void OnCrisisTriggered(CrisisTriggeredEvent evt)
{
    // Implement game-specific responses to crisis
    if (evt.CrisisType == CrisisType.Legal)
    {
        // Increase police presence in the city
        citySystem.SetPolicePresence(0.8f);
        
        // Add checkpoints at city exits
        citySystem.ActivateCheckpoints();
    }
}

private void OnCrisisResolved(CrisisResolvedEvent evt)
{
    // Implement aftermath effects
    if (evt.Successful)
    {
        // Reward player
        playerSystem.AddExperience(100);
        
        // Decrease alert level
        citySystem.SetPolicePresence(0.2f);
    }
    else
    {
        // Implement penalties
        playerSystem.AddWantedLevel(1);
        
        // Force player to specific location
        playerSystem.TeleportToJail();
    }
}
```

---

This guide covers the basic and advanced usage of the Risk Management System. For further details, refer to the code documentation and example implementations. If you have specific implementation questions, feel free to reach out to the development team.