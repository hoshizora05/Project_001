# Life Resource Management System - Usage Guide

## Overview

The Life Resource Management System provides a comprehensive framework for managing player resources in simulation and role-playing games. This system tracks and handles:

- **Time**: Day/night cycles, scheduling, and time-based events
- **Energy**: Player stamina, fatigue states, and activity costs
- **Finances**: Money, income sources, expenses, and financial stability
- **Social Credit**: Reputation across different contexts with benefits and restrictions

## Getting Started

### 1. Setup

1. **Create Configuration Asset**:
   - In the Project window, right-click and select `Create > Systems > Life Resource Config`
   - This creates a ScriptableObject that will store all your system settings

2. **Configure the Asset**:
   - Set up time blocks (morning, afternoon, evening, etc.)
   - Define energy states and costs
   - Configure income sources and expenses
   - Set up social contexts and reputation tiers

3. **Create Event Bus**:
   - Create a new Event Bus by right-clicking and selecting `Create > Systems > Life Resource System > Event Bus`
   - This will handle communication between the resource system and other game systems

4. **Add Manager to Scene**:
   - Add an empty GameObject to your scene
   - Attach the `LifeResourceManager` component
   - Assign your configuration asset and event bus

### 2. Basic Usage

```csharp
// Get reference to the manager
LifeResourceManager resourceManager = LifeResourceManager.Instance;

// Initialize with player ID
resourceManager.Initialize("player1");

// Advance time (e.g., in Update method)
resourceManager.AdvanceTime(Time.deltaTime * timeScale);

// Check action feasibility
if (resourceManager.CanPerformAction("workout"))
{
    // Allow player to perform action
}

// Consume energy for an activity
resourceManager.ConsumeEnergy("running", 1.5f); // With 1.5x multiplier

// Process financial transaction
resourceManager.ProcessTransaction("Bought coffee", -4.50f, "Food");

// Update social standing
resourceManager.UpdateSocialCredit("townspeople", 5.0f, "Helped local farmer");
```

## Detailed System Usage

### Time Management

The time system tracks day/night cycles, days of the week, and allows scheduling activities in time blocks.

#### Time Blocks

Time blocks represent periods of the day that can be allocated to activities:

```csharp
// Allocate a time block to an activity
resourceManager.AllocateTimeBlock("afternoon", "workout");

// Get current time state
TimeState timeState = resourceManager.GetResourceState().time;
Debug.Log($"Current time: Day {timeState.day}, {timeState.hour:F1} hours");
```

#### Time Advancement

Time progresses either automatically or manually:

```csharp
// Advance time by 2 hours
resourceManager.AdvanceTime(2.0f);

// Subscribe to time events
resourceManager.SubscribeToResourceEvents(OnResourceEvent);

private void OnResourceEvent(ResourceEvent evt)
{
    if (evt.type == ResourceEvent.ResourceEventType.DayChanged)
    {
        Debug.Log("A new day has dawned!");
    }
}
```

### Energy System

The energy system manages player stamina with different states that affect gameplay.

#### Energy States

Players transition between energy states (Energized, Normal, Fatigued, Exhausted) based on their current energy level:

```csharp
// Check current energy state
EnergyState energyState = resourceManager.GetResourceState().energy;
Debug.Log($"Energy: {energyState.currentEnergy}/{energyState.maxEnergy}");
Debug.Log($"Current state: {energyState.stateName}");
```

#### Energy Consumption and Recovery

```csharp
// Consume energy for an activity
if (resourceManager.ConsumeEnergy("heavyLabor"))
{
    // Activity succeeded
}
else
{
    // Not enough energy
    ShowNotEnoughEnergyMessage();
}

// Energy recovers automatically over time and when sleeping
```

### Finance System

Manages player money, income sources, expenses, and financial stability metrics.

#### Transactions

```csharp
// Process income
resourceManager.ProcessTransaction("Salary", 1000.0f, "Income");

// Process expense
if (resourceManager.ProcessTransaction("Rent", -500.0f, "Housing"))
{
    // Payment successful
}
else
{
    // Not enough money
    ShowNotEnoughMoneyMessage();
}
```

#### Financial Status

```csharp
// Get current financial state
FinancialState finances = resourceManager.GetResourceState().finance;
Debug.Log($"Money: ${finances.currentMoney:F2}");
Debug.Log($"Monthly balance: ${finances.monthlyBalance:F2}");
Debug.Log($"Financial stability: {finances.financialStability:F0}/100");
```

### Social Credit System

Tracks player reputation across different social contexts with various tiers offering benefits and restrictions.

#### Updating Reputation

```csharp
// Improve reputation with townspeople
resourceManager.UpdateSocialCredit("townspeople", 10.0f, "Donated to town festival");

// Decrease reputation with nobility
resourceManager.UpdateSocialCredit("nobility", -5.0f, "Spoke rudely to duke");
```

#### Checking Social Status

```csharp
// Get all social credits
Dictionary<string, float> socialCredits = resourceManager.GetResourceState().socialCredits;
foreach (var context in socialCredits)
{
    Debug.Log($"{context.Key}: {context.Value}");
}
```

## Event System

The event system allows your game to react to changes in resources:

```csharp
// Subscribe to resource events
resourceManager.SubscribeToResourceEvents(OnResourceEvent);

private void OnResourceEvent(ResourceEvent evt)
{
    switch (evt.type)
    {
        case ResourceEvent.ResourceEventType.EnergyChanged:
            UpdateEnergyUI((float)evt.parameters["newEnergy"]);
            break;
            
        case ResourceEvent.ResourceEventType.MoneyChanged:
            UpdateMoneyUI((float)evt.parameters["newBalance"]);
            break;
            
        case ResourceEvent.ResourceEventType.ResourceCritical:
            string resourceType = (string)evt.parameters["resourceType"];
            ShowCriticalResourceWarning(resourceType);
            break;
    }
}
```

## Saving and Loading

The system provides built-in saving and loading functionality:

```csharp
// Generate save data
LifeResourceSaveData saveData = resourceManager.GenerateSaveData();

// Save to JSON
string json = JsonUtility.ToJson(saveData);
PlayerPrefs.SetString("ResourceSaveData", json);

// Load from JSON
string savedJson = PlayerPrefs.GetString("ResourceSaveData");
if (!string.IsNullOrEmpty(savedJson))
{
    LifeResourceSaveData loadedData = JsonUtility.FromJson<LifeResourceSaveData>(savedJson);
    resourceManager.RestoreFromSaveData(loadedData);
}
```

## Integration Examples

### UI Integration

```csharp
public class ResourceUI : MonoBehaviour
{
    [SerializeField] private Image energyBar;
    [SerializeField] private Text moneyText;
    [SerializeField] private Text timeText;
    
    private void Start()
    {
        LifeResourceManager.Instance.SubscribeToResourceEvents(OnResourceEvent);
        UpdateAllUI();
    }
    
    private void OnDestroy()
    {
        if (LifeResourceManager.Instance != null)
        {
            LifeResourceManager.Instance.UnsubscribeFromResourceEvents(OnResourceEvent);
        }
    }
    
    private void UpdateAllUI()
    {
        ResourceState state = LifeResourceManager.Instance.GetResourceState();
        UpdateEnergyUI(state.energy.currentEnergy, state.energy.maxEnergy);
        UpdateMoneyUI(state.finance.currentMoney);
        UpdateTimeUI(state.time.day, state.time.hour);
    }
    
    private void OnResourceEvent(ResourceEvent evt)
    {
        switch (evt.type)
        {
            case ResourceEvent.ResourceEventType.EnergyChanged:
                float newEnergy = (float)evt.parameters["newEnergy"];
                float ratio = (float)evt.parameters["ratio"];
                UpdateEnergyUI(newEnergy, newEnergy / ratio);
                break;
                
            case ResourceEvent.ResourceEventType.MoneyChanged:
                float newBalance = (float)evt.parameters["newBalance"];
                UpdateMoneyUI(newBalance);
                break;
                
            case ResourceEvent.ResourceEventType.TimeAdvanced:
                ResourceState state = LifeResourceManager.Instance.GetResourceState();
                UpdateTimeUI(state.time.day, state.time.hour);
                break;
        }
    }
    
    private void UpdateEnergyUI(float current, float max)
    {
        energyBar.fillAmount = current / max;
    }
    
    private void UpdateMoneyUI(float money)
    {
        moneyText.text = $"${money:F2}";
    }
    
    private void UpdateTimeUI(int day, float hour)
    {
        int hourInt = Mathf.FloorToInt(hour);
        int minutes = Mathf.FloorToInt((hour - hourInt) * 60);
        timeText.text = $"Day {day} - {hourInt:D2}:{minutes:D2}";
    }
}
```

### Activity System Integration

```csharp
public class ActivitySystem : MonoBehaviour
{
    [System.Serializable]
    public class Activity
    {
        public string activityId;
        public string activityName;
        public float energyCost;
        public float timeCost;
        public float moneyCost;
        public UnityEvent onActivityCompleted;
    }
    
    [SerializeField] private List<Activity> availableActivities;
    
    public bool PerformActivity(string activityId)
    {
        Activity activity = availableActivities.Find(a => a.activityId == activityId);
        if (activity == null) return false;
        
        // Check if player can perform the action
        if (!LifeResourceManager.Instance.CanPerformAction(activityId))
        {
            Debug.Log("Cannot perform activity: " + activityId);
            return false;
        }
        
        // Consume resources
        bool success = true;
        
        if (activity.energyCost > 0)
        {
            success &= LifeResourceManager.Instance.ConsumeEnergy(activityId);
        }
        
        if (activity.moneyCost > 0)
        {
            success &= LifeResourceManager.Instance.ProcessTransaction(
                "Activity: " + activity.activityName, 
                -activity.moneyCost, 
                "Activities"
            );
        }
        
        if (success && activity.timeCost > 0)
        {
            LifeResourceManager.Instance.AdvanceTime(activity.timeCost);
        }
        
        if (success)
        {
            activity.onActivityCompleted?.Invoke();
        }
        
        return success;
    }
}
```

## Advanced Features

### Custom Energy States

You can create custom energy states with unique effects:

```csharp
// In your configuration
public void SetupEnergyConfig(LifeResourceConfig config)
{
    // Add a "Caffeinated" state
    config.energyConfig.customStates.Add(new CustomEnergyStateConfig
    {
        stateId = "caffeinated",
        stateName = "Caffeinated",
        energyCostMultiplier = 0.7f,
        recoveryRateMultiplier = 0.8f,
        restrictedActions = new List<string> { "sleep" }
    });
    
    // Define regular states with thresholds
    config.energyConfig.defaultStates.Add(new EnergySystem.EnergyState
    {
        stateId = "energized",
        stateName = "Energized",
        minThreshold = 0.8f,
        maxThreshold = 1.0f
    });
    
    // ... other states
}
```

### Time Block Scheduling

Plan activities in advance by allocating time blocks:

```csharp
// Get available time blocks
List<TimeResourceSystem.TimeBlock> availableBlocks = 
    resourceManager.GetResourceState().time.availableTimeBlocks;

// Display them to player and let them choose
DisplayTimeBlockOptions(availableBlocks);

// When player selects a block and activity
public void OnTimeBlockSelected(string blockId, string activityId)
{
    if (resourceManager.AllocateTimeBlock(blockId, activityId))
    {
        Debug.Log($"Scheduled {activityId} for {blockId}");
    }
    else
    {
        Debug.Log("Could not schedule activity");
    }
}
```

## Common Issues and Solutions

### Time Not Advancing
- Ensure you're calling `AdvanceTime()` in a regular update loop
- Check if time scale is properly set

### Energy Not Recovering
- Verify energy recovery rate is greater than zero
- Check for active modifiers affecting recovery rate
- Make sure time is advancing

### Transactions Not Working
- Ensure player has enough money for expenses
- Verify transaction categories are properly defined

### Social Credit Not Updating
- Check that the social context exists in the configuration
- Verify the event system is properly connected

## Best Practices

1. **Initialize Early**: Set up the resource system early in your game initialization
2. **Subscribe to Events**: Use the event system for UI updates to avoid polling
3. **Save Regularly**: Save resource state during gameplay transitions
4. **Balance Parameters**: Carefully balance energy costs, financial values, and social credit impacts
5. **Handle Edge Cases**: Always check return values from system methods to handle failure cases

## Extensions and Customization

The system is designed to be extended with additional functionality:

- Create custom resource types by extending the base classes
- Implement custom state effects for unique gameplay mechanics
- Add new event types for specialized game events
- Create custom UI visualizations for resource states

---

This guide covers the basic usage of the Life Resource Management System. For more advanced implementation details, refer to the system's source code and inline documentation.