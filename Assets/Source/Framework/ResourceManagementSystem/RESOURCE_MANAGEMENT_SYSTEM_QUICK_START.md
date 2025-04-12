# Resource Management System - Quick Start Guide

This guide will help you quickly integrate and use the Resource Management System in your Unity project.

## Setup

### 1. Configuration

The system uses ScriptableObject-based configuration. To create a new configuration:

1. Right-click in the Project window
2. Select `Create > Systems > Resource System Config`
3. Name your configuration (e.g., "DefaultResourceConfig")

Alternatively, create a configuration at runtime:

```csharp
var config = ResourceSystemConfig.CreateDefaultConfig();
```

### 2. System Initialization

Add the ResourceManagementSystem to a GameObject in your scene:

```csharp
// Option 1: Add to existing GameObject
gameObject.AddComponent<ResourceManagementSystem>();

// Option 2: Create dedicated GameObject
GameObject resourceSystemObj = new GameObject("ResourceManagementSystem");
var resourceSystem = resourceSystemObj.AddComponent<ResourceManagementSystem>();
DontDestroyOnLoad(resourceSystemObj); // Make it persistent
```

Initialize the system with a player identifier:

```csharp
ResourceManagementSystem.Instance.Initialize("player1", customConfig);
```

## Basic Operations

### Currency Management

```csharp
// Add currency
ResourceManagementSystem.Instance.GetCurrencySystem().AddCurrency(
    CurrencyType.StandardCurrency, 
    100f, 
    "quest_reward", 
    "Quest completion bonus"
);

// Remove currency (returns false if insufficient)
bool success = ResourceManagementSystem.Instance.GetCurrencySystem().RemoveCurrency(
    CurrencyType.StandardCurrency,
    50f,
    "shop_purchase",
    "Bought health potion"
);

// Get current amount
float currentAmount = ResourceManagementSystem.Instance.GetCurrencySystem()
    .GetCurrencyAmount(CurrencyType.StandardCurrency);

// Convert between currencies
ResourceManagementSystem.Instance.GetCurrencySystem().ConvertCurrency(
    CurrencyType.PremiumCurrency,
    CurrencyType.StandardCurrency,
    10f
);
```

### Inventory Management

```csharp
// Add items
ResourceManagementSystem.Instance.GetInventorySystem().AddItem("health_potion", 5);

// Remove items
ResourceManagementSystem.Instance.GetInventorySystem().RemoveItem("health_potion", 2);

// Use an item
ResourceManagementSystem.Instance.GetInventorySystem().UseItem("health_potion");

// Check item quantity
int potionCount = ResourceManagementSystem.Instance.GetInventorySystem()
    .GetItemQuantity("health_potion");

// Equip/unequip items
ResourceManagementSystem.Instance.GetInventorySystem()
    .EquipItem("steel_sword", EquipmentSlotType.MainHand);

ResourceManagementSystem.Instance.GetInventorySystem()
    .UnequipItem(EquipmentSlotType.MainHand);
```

### Time Resource Management

```csharp
// Get current time
float currentHour = ResourceManagementSystem.Instance.GetTimeSystem().GetCurrentTime();

// Advance time
ResourceManagementSystem.Instance.GetTimeSystem().AdvanceTime(1.5f); // 1.5 hours

// Schedule an action
ResourceManagementSystem.Instance.GetTimeSystem().ScheduleAction(
    "craft_item",
    currentHour,
    new Dictionary<string, object> { { "itemId", "steel_sword" } }
);

// Cancel a scheduled action
ResourceManagementSystem.Instance.GetTimeSystem().CancelAction("craft_item");

// Check time requirements
float timeCost = ResourceManagementSystem.Instance.GetTimeSystem()
    .GetActionTimeCost("craft_item");

bool hasTime = ResourceManagementSystem.Instance.GetTimeSystem()
    .HasTimeForAction("craft_item");
```

### Resource Conversion

```csharp
// Convert resources
ResourceManagementSystem.Instance.GetConversionSystem().ConvertResource(
    ResourceType.Item,
    "raw_ore",
    ResourceType.Item,
    "refined_metal",
    10f
);

// Get conversion rate
float rate = ResourceManagementSystem.Instance.GetConversionSystem()
    .GetConversionRate(ResourceType.Item, "raw_ore", ResourceType.Item, "refined_metal");

// Get conversion options
var options = ResourceManagementSystem.Instance.GetConversionSystem()
    .GetConversionOptions(ResourceType.Item, "raw_ore");
```

## Processing Actions

For complex operations involving multiple resources, use the action processing system:

```csharp
// Craft an item (consumes time, materials, and possibly currency)
var parameters = new Dictionary<string, object>
{
    { "itemId", "steel_sword" },
    { "quantity", 1 }
};

ActionResult result = ResourceManagementSystem.Instance.ProcessAction(
    "craft_item", 
    parameters
);

if (result.success)
{
    Debug.Log("Item crafted successfully!");
}
else
{
    Debug.LogWarning($"Crafting failed: {result.message}");
}
```

## Event Handling

Subscribe to resource events to update your UI or trigger game logic:

```csharp
// Subscribe to resource events
ResourceManagementSystem.Instance.SubscribeToResourceEvents(OnResourceEvent);

// Event handler
private void OnResourceEvent(ResourceEvent resourceEvent)
{
    switch (resourceEvent.type)
    {
        case ResourceEventType.CurrencyChanged:
            UpdateCurrencyUI();
            break;
            
        case ResourceEventType.ItemAdded:
            UpdateInventoryUI();
            PlayItemAcquiredEffect();
            break;
            
        case ResourceEventType.ResourceCritical:
            ShowResourceWarning();
            break;
    }
}

// Remember to unsubscribe when no longer needed
ResourceManagementSystem.Instance.UnsubscribeFromResourceEvents(OnResourceEvent);
```

## Save and Load

Save and restore the system's state:

```csharp
// Save to a data object
ResourceSaveData saveData = ResourceManagementSystem.Instance.GenerateSaveData();

// Convert to JSON or binary for storage
string json = JsonUtility.ToJson(saveData);
PlayerPrefs.SetString("ResourceSystemSave", json);

// Load from saved data
string savedJson = PlayerPrefs.GetString("ResourceSystemSave");
if (!string.IsNullOrEmpty(savedJson))
{
    ResourceSaveData loadedData = JsonUtility.FromJson<ResourceSaveData>(savedJson);
    ResourceManagementSystem.Instance.RestoreFromSaveData(loadedData);
}
```

## Optimization Suggestions

Get AI-driven optimization suggestions for resource usage:

```csharp
List<ResourceOptimizationSuggestion> suggestions = 
    ResourceManagementSystem.Instance.GetOptimizationSuggestions();

foreach (var suggestion in suggestions)
{
    Debug.Log($"Suggestion: {suggestion.title} - {suggestion.description}");
    
    // Example: Show top suggestion to player
    if (suggestion.priority >= 8)
    {
        ShowSuggestionToPlayer(suggestion);
    }
}
```

## Common Customizations

### Adding a New Currency Type

1. Add a new value to the `CurrencyType` enum
2. Create a currency definition in the config:

```csharp
var newCurrency = new ResourceSystemConfig.CurrencyDefinition
{
    currencyId = "faction_currency",
    name = "Faction Points",
    type = CurrencyType.FactionCurrency,
    startingAmount = 0f,
    maxCapacity = 1000f,
    properties = new CurrencyProperties
    {
        isMainCurrency = false,
        isTradeableBetweenPlayers = false,
        deterioratesOverTime = true,
        deteriorationRate = 0.01f,
        canBeNegative = false,
        interestRate = 0f
    }
};

config.currencyConfig.availableCurrencies.Add(newCurrency);
```

### Creating a New Item

Items are managed through the item database. To add a new item:

```csharp
var itemDefinition = new Item
{
    itemId = "legendary_sword",
    name = "Legendary Sword",
    type = ItemType.Equipment,
    category = ItemCategory.Weapon,
    properties = new ItemProperties
    {
        baseValue = 1000f,
        weight = 5f,
        isStackable = false,
        isEquippable = true,
        equipSlot = EquipmentSlotType.MainHand,
        description = "A legendary sword of immense power"
    },
    effects = new List<ItemEffect>
    {
        new ItemEffect { 
            effectId = "damage", 
            effectName = "Damage Bonus", 
            targetStat = "damage", 
            effectValue = 50f 
        },
        new ItemEffect { 
            effectId = "critical", 
            effectName = "Critical Chance", 
            targetStat = "criticalChance", 
            effectValue = 0.1f 
        }
    },
    tags = new List<string> { "weapon", "legendary", "sword" }
};

// Register with item database
ItemDatabase.Instance.RegisterItem(itemDefinition);
```

## Troubleshooting

### System Not Initializing

- Ensure you have a ResourceSystemConfig asset created
- Check that the ResourceManagementSystem component is properly added
- Verify initialization is called with a valid player ID

### Resource Changes Not Reflecting

- Make sure you're accessing the system through the singleton Instance
- Verify that events are properly subscribed to update UI elements
- Check for error messages in the console

### Performance Issues

- Use BatchTool for multiple operations
- Consider increasing update intervals for non-critical resources
- Optimize event subscriptions to handle only necessary changes

## Next Steps

After setting up the basics, consider exploring:

- Custom conversion recipes
- Resource generation sources
- Advanced economic modeling
- Custom optimization suggestion strategies

For full details on these topics, see the main README.md and API documentation.

---

For additional assistance, please contact the system maintainers or check the full documentation.