# Life Resource Management System - Quick Start Guide

This guide provides a quick overview of how to integrate and use the Life Resource Management System in your Unity project.

## Installation

1. Ensure the `LifeResourceSystem` folder is in your project's Assets directory
2. Create necessary configuration assets as described below

## Basic Setup

### 1. Create Configuration Assets

First, create the required ScriptableObject assets:

- Create a **Life Resource Config**:
  - Right-click in Project window → `Create > Systems > Life Resource Config`
  - This will store all system settings

- Create an **Event Bus**:
  - Right-click in Project window → `Create > Systems > Life Resource System > Event Bus`
  - This handles communication between systems

### 2. Configure the System

Edit your Life Resource Config asset:

**Time Configuration**:
```
- Set Hours Per Real Second: 0.2 (adjust for desired time flow)
- Add time blocks:
  * Morning: 6:00 - 12:00
  * Afternoon: 12:00 - 18:00
  * Evening: 18:00 - 22:00
  * Night: 22:00 - 6:00
```

**Energy Configuration**:
```
- Base Max Energy: 100
- Base Recovery Rate: 2 (per hour)
- Default Energy Costs:
  * walk: 5
  * run: 10
  * workout: 20
  * study: 15
```

**Finance Configuration**:
```
- Starting Money: 1000
- Default Income Sources:
  * Add salary or initial income
- Default Expenses:
  * Add rent, food, etc.
```

**Social Credit Configuration**:
```
- Add Social Contexts:
  * townspeople (50 starting score)
  * merchants (50 starting score)
  * authorities (50 starting score)
- For each context, define tiers with thresholds and effects
```

### 3. Add Manager to Scene

- Create an empty GameObject named "ResourceManager"
- Add the `LifeResourceManager` component
- Assign your Configuration asset and Event Bus

## Basic Usage

```csharp
// Reference the manager (singleton pattern)
private LifeResourceManager resourceManager;

private void Start()
{
    // Get reference and initialize
    resourceManager = LifeResourceManager.Instance;
    resourceManager.Initialize("player1");
    
    // Subscribe to events
    resourceManager.SubscribeToResourceEvents(OnResourceEvent);
}

private void Update()
{
    // Advance time based on real time
    resourceManager.AdvanceTime(Time.deltaTime * timeScale);
}

// Handle resource events
private void OnResourceEvent(ResourceEvent evt)
{
    switch (evt.type)
    {
        case ResourceEvent.ResourceEventType.EnergyChanged:
            UpdateEnergyUI();
            break;
        
        case ResourceEvent.ResourceEventType.MoneyChanged:
            UpdateMoneyUI();
            break;
            
        case ResourceEvent.ResourceEventType.TimeAdvanced:
            UpdateTimeUI();
            break;
    }
}

// Example: Perform an activity
public void DoWorkout()
{
    if (resourceManager.CanPerformAction("workout"))
    {
        // Consume energy
        resourceManager.ConsumeEnergy("workout");
        
        // Advance time
        resourceManager.AdvanceTime(1.0f); // 1 hour
        
        // Update skill or attribute
        // ...
    }
    else
    {
        ShowCannotPerformActionMessage();
    }
}

// Example: Make a purchase
public void BuyCoffee()
{
    if (resourceManager.ProcessTransaction("Coffee", -4.50f, "Food"))
    {
        // Give player coffee item
        // ...
        
        // Add energy modifier
        AddCaffeineEffect();
    }
    else
    {
        ShowNotEnoughMoneyMessage();
    }
}

// Example: Gain reputation
public void HelpTownsperson()
{
    // Complete quest or activity
    // ...
    
    // Reward reputation
    resourceManager.UpdateSocialCredit("townspeople", 5.0f, "Helped with chores");
}
```

## Saving and Loading

```csharp
// Save game state
public void SaveGame()
{
    LifeResourceSaveData saveData = resourceManager.GenerateSaveData();
    string json = JsonUtility.ToJson(saveData);
    PlayerPrefs.SetString("ResourceSaveData", json);
}

// Load game state
public void LoadGame()
{
    string json = PlayerPrefs.GetString("ResourceSaveData");
    if (!string.IsNullOrEmpty(json))
    {
        LifeResourceSaveData saveData = JsonUtility.FromJson<LifeResourceSaveData>(json);
        resourceManager.RestoreFromSaveData(saveData);
    }
}
```

## Next Steps

For more detailed usage information and advanced features, refer to:

- **USAGE_GUIDE.md**: Full documentation with detailed examples
- **Code Documentation**: In-code comments and method descriptions

For specific implementation questions, examine the source code of the relevant manager classes:
- `TimeManager.cs`
- `EnergyManager.cs`
- `FinanceManager.cs`
- `SocialCreditManager.cs`