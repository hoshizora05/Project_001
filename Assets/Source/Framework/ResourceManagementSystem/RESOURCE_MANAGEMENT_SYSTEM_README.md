# Resource Management System

A comprehensive system for managing valuable player resources including currency, items, and time. Designed as a core system that significantly impacts player decision-making, game balance, and play strategies.

## Key Features

- **Loosely coupled architecture**: Minimizes dependencies on other systems
- **Extensibility**: Structure allows easy addition of new resource types
- **Balance adjustment**: Data-driven design facilitates easy balance tweaks
- **Visual feedback**: UI integration for intuitive understanding of resource changes
- **Strategic choices**: Diverse resource utilization options for different play styles
- **Performance optimization**: Efficient resource calculations and optimizations

## Core Components

### 1. Currency Management System
- Support for multiple currencies (standard, premium, faction, etc.)
- Detailed tracking of income sources and expenditures
- Transaction history recording and analysis
- Banking, loan, and investment mechanics
- Currency conversion with dynamic rates
- Inflation/deflation adjustments based on economic conditions

### 2. Item Management System
- Management of diverse item types and categories
- Inventory capacity and weight limits
- Item condition tracking (durability, quality)
- Efficient handling of stackable items
- Item search, sort, and filtering capabilities
- Item acquisition/loss history
- Equipment system with slot-based equipping
- Item usage, crafting, and breakdown

### 3. Time Resource Allocation System
- In-game time tracking and management
- Time costs for player actions
- Time-based action scheduling
- Parallel and sequential action handling
- Time periods/days/seasons
- Player schedule management with optimization suggestions
- Integration with energy/stamina systems
- Time efficiency improvements through skills/items

### 4. Resource Conversion System
- Conversion between different resource types
- Efficiency calculation based on skills and conditions
- Conversion limits and rates management
- Special conversion recipes
- Conversion optimization suggestions

### 5. Resource Generation System
- Passive resource generation (time-based)
- Active resource generation (action-based)
- Generation source management
- Generation scaling and limitations
- Generation efficiency improvement mechanics

### 6. Resource Storage and Waste System
- Resource expiration and quality degradation
- Storage methods with varying effects
- Waste detection for unused resources
- Storage capacity management
- Efficient resource utilization incentives

## Getting Started

1. Add the ResourceSystemConfig asset to your project
2. Configure the settings for each subsystem
3. Add the ResourceManagementSystem component to a persistent GameObject
4. Access the system through ResourceManagementSystem.Instance

## Usage Examples

### Currency Management

```csharp
// Add currency to player
ResourceManagementSystem.Instance.GetCurrencySystem().AddCurrency(
    CurrencyType.StandardCurrency, 
    100f, 
    "quest_reward", 
    "Completed quest: Rescue the villagers"
);

// Check if player can afford something
float cost = 50f;
bool canAfford = ResourceManagementSystem.Instance.GetCurrencySystem()
    .GetCurrencyAmount(CurrencyType.StandardCurrency) >= cost;
```

### Inventory Management

```csharp
// Add items to inventory
ResourceManagementSystem.Instance.GetInventorySystem().AddItem("potion_health", 5);

// Equip an item
ResourceManagementSystem.Instance.GetInventorySystem()
    .EquipItem("steel_sword", EquipmentSlotType.MainHand);
```

### Time Management

```csharp
// Schedule an action
ResourceManagementSystem.Instance.GetTimeSystem().ScheduleAction(
    "craft_item",
    ResourceManagementSystem.Instance.GetTimeSystem().GetCurrentTime()
);

// Advance time
ResourceManagementSystem.Instance.GetTimeSystem().AdvanceTime(2f); // 2 hours
```

## Integration

The Resource Management System is designed to integrate with:

- **Quest/Objective Systems**: Resource collection objectives and rewards
- **Character Systems**: Resource efficiency through skills, resource impacts on status
- **World Systems**: Environment-based resource generation and limitations
- **Risk Systems**: Resource-risk relationships and management

## Customization

The system is highly configurable through ScriptableObjects:

- Edit `ResourceSystemConfig` asset for global settings
- Create custom currency types, item definitions, and conversion recipes
- Override default managers with custom implementations

## Best Practices

1. Use batch transactions for complex operations
2. Subscribe to resource events for UI updates
3. Periodically check optimization suggestions
4. Balance acquisition and consumption rates carefully
5. Test with realistic player behavior patterns

## License

This system is part of the Project_001 codebase and subject to its licensing terms.

---

Created by [Your Studio/Team Name]