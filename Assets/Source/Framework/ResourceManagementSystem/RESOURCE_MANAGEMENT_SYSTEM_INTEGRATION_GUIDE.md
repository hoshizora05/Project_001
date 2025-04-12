# Resource Management System - Integration Guide

This guide explains how to integrate the Resource Management System with other game systems to create a cohesive game economy and progression experience.

## Table of Contents
1. [Core System Integration](#core-system-integration)
2. [Character System Integration](#character-system-integration)
3. [Progression System Integration](#progression-system-integration)
4. [Quest and Event System Integration](#quest-and-event-system-integration)
5. [UI System Integration](#ui-system-integration)
6. [Risk Management Integration](#risk-management-integration)
7. [Social Activity Integration](#social-activity-integration)
8. [Custom System Integration](#custom-system-integration)

## Core System Integration

The Resource Management System uses a loosely coupled architecture with a service locator pattern for core integration.

### Initialization Order

Proper initialization order is important:

1. Initialize ResourceManagementSystem first
2. Register with ServiceLocator
3. Initialize dependent systems afterward

```csharp
// Example initialization sequence
void InitializeGameSystems()
{
    // First, initialize the resource system
    ResourceManagementSystem.Instance.Initialize("player1");
    
    // Register with service locator
    ServiceLocator.Register<IResourceManagementSystem>(ResourceManagementSystem.Instance);
    
    // Now initialize systems that depend on the resource system
    CharacterSystem.Instance.Initialize();
    QuestSystem.Instance.Initialize();
    // ... other systems
}
```

### Event Communication

The system uses the EventBusReference for event communication:

```csharp
// Create an event bus
var eventBus = ScriptableObject.CreateInstance<EventBusReference>();

// Assign to the resource system
ResourceManagementSystem resourceSystem = ResourceManagementSystem.Instance;
resourceSystem.eventBus = eventBus;

// Subscribe other systems to resource events
eventBus.Subscribe<ResourceEvent>(OnResourceEvent);
```

## Character System Integration

### Skill Effects on Resource Efficiency

Link character skills to resource efficiency:

```csharp
// In CharacterSystem
public void OnSkillLevelChanged(string skillId, int newLevel)
{
    // Update resource efficiency based on skills
    switch (skillId)
    {
        case "crafting":
            float craftingBonus = newLevel * 0.05f; // 5% per level
            ResourceManagementSystem.Instance.GetConversionSystem()
                .SetConversionRateModifier("skill_bonus", craftingBonus);
            break;
            
        case "bargaining":
            float bargainingBonus = newLevel * 0.03f; // 3% per level
            // Affect currency exchange rates
            break;
            
        case "time_management":
            float timeBonus = newLevel * 0.02f; // 2% per level
            ResourceManagementSystem.Instance.GetTimeSystem()
                .SetActionEfficiency("default", timeBonus);
            break;
    }
}
```

### Character States Affecting Resources

Tie character states (tired, energized, etc.) to resource systems:

```csharp
// In CharacterStateManager
public void OnStateChanged(CharacterState oldState, CharacterState newState)
{
    // Apply state effects to resource system
    switch (newState)
    {
        case CharacterState.Energized:
            // Add a time efficiency bonus
            TimeModifier timeBonus = new TimeModifier
            {
                modifierId = "energized_state",
                source = "character_state",
                flatBonus = 0f,
                percentageBonus = 0.15f, // 15% faster
                duration = 4f, // 4 hours
                remainingTime = 4f
            };
            
            ResourceManagementSystem.Instance.GetTimeSystem()
                .AddTimeModifier("global", timeBonus);
            break;
            
        case CharacterState.Exhausted:
            // Reduce carrying capacity
            ResourceManagementSystem.Instance.GetInventorySystem()
                .ModifyCarryingCapacity(0.7f); // 70% of normal
            break;
    }
}
```

## Progression System Integration

Link player progression with resource capabilities:

### Unlocking New Resource Types

```csharp
// In PlayerProgressionSystem
public void OnLevelUp(int newLevel)
{
    // Unlock new features based on level
    switch (newLevel)
    {
        case 5:
            // Unlock premium currency
            var premiumCurrency = new ResourceSystemConfig.CurrencyDefinition
            {
                currencyId = "premium_currency",
                name = "Gems",
                type = CurrencyType.PremiumCurrency,
                startingAmount = 50f,
                maxCapacity = 0f, // Unlimited
                properties = new CurrencyProperties
                {
                    isMainCurrency = false,
                    isTradeableBetweenPlayers = false
                }
            };
            
            ResourceManagementSystem.Instance.AddCurrencyType(premiumCurrency);
            break;
            
        case 10:
            // Unlock advanced conversions
            ResourceManagementSystem.Instance.GetConversionSystem()
                .UnlockConversion("item_rare_ore_to_item_legendary_metal");
            break;
    }
}
```

### Experience from Resource Activities

```csharp
// Subscribe to resource events for XP rewards
void OnResourceEvent(ResourceEvent resourceEvent)
{
    if (resourceEvent.type == ResourceEventType.ResourceConverted)
    {
        if (resourceEvent.parameters.TryGetValue("conversion", out object convObj))
        {
            ResourceConversion conversion = convObj as ResourceConversion;
            if (conversion != null)
            {
                // Award XP based on conversion value
                float xpAmount = conversion.fromAmount * 0.5f;
                PlayerProgressionSystem.Instance.AddExperience("crafting", xpAmount);
            }
        }
    }
}
```

## Quest and Event System Integration

### Resource Collection Objectives

```csharp
// In QuestSystem
public class ResourceCollectionObjective : IQuestObjective
{
    public string resourceType;
    public string resourceId;
    public float requiredAmount;
    public float currentAmount;
    
    // Register for resource events when objective becomes active
    public void Activate()
    {
        ResourceManagementSystem.Instance.SubscribeToResourceEvents(OnResourceEvent);
    }
    
    private void OnResourceEvent(ResourceEvent resourceEvent)
    {
        // Check for relevant events
        if (resourceEvent.type == ResourceEventType.ItemAdded &&
            resourceEvent.parameters.TryGetValue("item", out object itemObj) &&
            resourceEvent.parameters.TryGetValue("quantity", out object quantityObj))
        {
            Item item = itemObj as Item;
            if (item != null && item.itemId == resourceId && resourceType == "Item")
            {
                int quantity = Convert.ToInt32(quantityObj);
                currentAmount += quantity;
                CheckCompletion();
            }
        }
    }
    
    private void CheckCompletion()
    {
        if (currentAmount >= requiredAmount)
        {
            OnCompleted?.Invoke(this);
        }
    }
    
    public event Action<IQuestObjective> OnCompleted;
    
    public void Deactivate()
    {
        ResourceManagementSystem.Instance.UnsubscribeFromResourceEvents(OnResourceEvent);
    }
}
```

### Resource Rewards

```csharp
// In QuestManager
public void CompleteQuest(string questId)
{
    Quest quest = GetQuest(questId);
    if (quest == null || !quest.IsCompletable())
        return;
        
    // Grant rewards
    foreach (var reward in quest.rewards)
    {
        switch (reward.type)
        {
            case RewardType.Currency:
                ResourceManagementSystem.Instance.GetCurrencySystem().AddCurrency(
                    (CurrencyType)reward.currencyType,
                    reward.amount,
                    "quest_reward",
                    $"Quest reward: {quest.title}"
                );
                break;
                
            case RewardType.Item:
                ResourceManagementSystem.Instance.GetInventorySystem().AddItem(
                    reward.itemId,
                    reward.quantity
                );
                break;
                
            case RewardType.GenerationSource:
                ResourceManagementSystem.Instance.GetGenerationSystem().AddGenerationSource(
                    new GenerationSource
                    {
                        sourceId = reward.sourceId,
                        sourceName = reward.sourceName,
                        resourceType = (ResourceType)reward.resourceType,
                        resourceId = reward.resourceId,
                        baseGenerationRate = reward.generationRate,
                        isActive = true
                    }
                );
                break;
        }
    }
    
    quest.Complete();
}
```

## UI System Integration

### Resource Display Updates

```csharp
// In UIManager
private void InitializeResourceUI()
{
    // Subscribe to events
    ResourceManagementSystem.Instance.SubscribeToResourceEvents(OnResourceChanged);
    
    // Initial update
    UpdateAllResourceDisplays();
}

private void OnResourceChanged(ResourceEvent resourceEvent)
{
    switch (resourceEvent.type)
    {
        case ResourceEventType.CurrencyChanged:
            UpdateCurrencyDisplay();
            break;
            
        case ResourceEventType.ItemAdded:
        case ResourceEventType.ItemRemoved:
            UpdateInventoryDisplay();
            break;
            
        case ResourceEventType.ResourceCritical:
            ShowResourceWarning(resourceEvent);
            break;
    }
}

private void UpdateCurrencyDisplay()
{
    var currencies = ResourceManagementSystem.Instance.GetCurrencySystem().GetAllCurrencies();
    foreach (var currency in currencies)
    {
        var display = GetCurrencyDisplay(currency.type);
        if (display != null)
        {
            display.UpdateValue(currency.currentAmount);
            
            // Animate change if needed
            if (display.previousValue != currency.currentAmount)
            {
                display.AnimateChange(display.previousValue, currency.currentAmount);
            }
        }
    }
}
```

### Resource Transaction UI

```csharp
// In ShopUI
public void Purchase(string itemId, int quantity)
{
    // Get item cost
    Item item = ItemDatabase.Instance.GetItem(itemId);
    float totalCost = item.properties.baseValue * quantity;
    
    // Check if player can afford
    bool canAfford = ResourceManagementSystem.Instance.GetCurrencySystem()
        .GetCurrencyAmount(CurrencyType.StandardCurrency) >= totalCost;
        
    if (!canAfford)
    {
        ShowInsufficientFundsMessage();
        return;
    }
    
    // Process transaction
    bool success = ResourceManagementSystem.Instance.GetCurrencySystem()
        .RemoveCurrency(
            CurrencyType.StandardCurrency,
            totalCost,
            "shop_purchase",
            $"Purchased {quantity}x {item.name}"
        );
        
    if (success)
    {
        ResourceManagementSystem.Instance.GetInventorySystem()
            .AddItem(itemId, quantity);
            
        ShowPurchaseSuccessMessage();
    }
}
```

## Risk Management Integration

Link the Risk Management System with resource decisions:

```csharp
// In RiskManagementSystem
public void AssessResourceRisk(ResourceAction action)
{
    RiskAssessment assessment = new RiskAssessment();
    
    // Analyze resource state
    ResourceState state = ResourceManagementSystem.Instance.GetResourceState();
    
    // Check financial stability
    if (state.finance.financialStability < 0.3f)
    {
        assessment.AddRiskFactor(
            "financial_instability", 
            "Low financial stability increases risk of bankruptcy", 
            0.4f
        );
    }
    
    // Check resource depletion risk
    foreach (var currency in state.currencies)
    {
        if (currency.currentAmount < currency.properties.baseValue * 0.2f)
        {
            assessment.AddRiskFactor(
                $"low_{currency.type}", 
                $"Low {currency.name} increases risk of insolvency", 
                0.3f
            );
        }
    }
    
    // Account for mitigation factors
    if (ResourceManagementSystem.Instance.GetInventorySystem()
        .GetItemQuantity("safety_net") > 0)
    {
        assessment.AddMitigationFactor(
            "safety_net",
            "Safety net item reduces financial risk",
            0.2f
        );
    }
    
    // Calculate final risk
    float riskScore = assessment.CalculateRiskScore();
    
    // Apply risk thresholds
    if (riskScore > 0.7f)
    {
        ShowHighRiskWarning(action, riskScore);
    }
    
    return assessment;
}
```

## Social Activity Integration

Connect social interactions with resource exchanges:

```csharp
// In SocialActivitySystem
public void ProcessGiftGiving(string targetCharacterId, string itemId, int quantity)
{
    // Check if player has the item
    if (ResourceManagementSystem.Instance.GetInventorySystem()
        .GetItemQuantity(itemId) < quantity)
    {
        ShowInsufficientItemsMessage();
        return;
    }
    
    // Remove from player inventory
    bool success = ResourceManagementSystem.Instance.GetInventorySystem()
        .RemoveItem(itemId, quantity);
        
    if (success)
    {
        // Get item value
        Item item = ItemDatabase.Instance.GetItem(itemId);
        float itemValue = item.properties.baseValue * quantity;
        
        // Increase relationship
        float relationshipBoost = itemValue * 0.01f; // 1% of value
        RelationshipSystem.Instance.ModifyRelationship(
            targetCharacterId, 
            "friendship", 
            relationshipBoost
        );
        
        // Record social transaction
        RecordSocialTransaction(
            "gift_giving",
            targetCharacterId,
            itemId,
            quantity,
            itemValue
        );
        
        ShowGiftSuccessMessage();
    }
}
```

## Custom System Integration

For integrating with custom systems, use the ServiceLocator pattern:

```csharp
// Register your custom system
ServiceLocator.Register<IMyCustomSystem>(MyCustomSystem.Instance);

// In your custom system
public class MyCustomSystem : IMyCustomSystem
{
    private IResourceManagementSystem _resourceSystem;
    
    public void Initialize()
    {
        // Get resource system reference
        _resourceSystem = ServiceLocator.Get<IResourceManagementSystem>();
        
        // Subscribe to events
        if (_resourceSystem != null)
        {
            _resourceSystem.SubscribeToResourceEvents(OnResourceEvent);
        }
    }
    
    private void OnResourceEvent(ResourceEvent resourceEvent)
    {
        // Process relevant events
    }
    
    public void ProcessCustomAction(string actionType, Dictionary<string, object> parameters)
    {
        // Check resource requirements
        ActionValidationResult validation = _resourceSystem.ValidateAction(actionType);
        if (!validation.isValid)
        {
            ShowValidationFailure(validation.message);
            return;
        }
        
        // Process the action through the resource system
        ActionResult result = _resourceSystem.ProcessAction(actionType, parameters);
        
        // Handle result
        if (result.success)
        {
            ApplyCustomActionEffects(actionType, parameters);
        }
        else
        {
            HandleActionFailure(result.message);
        }
    }
}
```

## Performance Considerations

When integrating with multiple systems, keep these performance tips in mind:

1. **Batch processing**: Use BatchTool for multiple operations
2. **Event filtering**: Only subscribe to events you actually need
3. **Lazy initialization**: Initialize resource-heavy features only when needed
4. **Update frequency**: Stagger updates for non-critical systems
5. **Cache reference**: Keep references to frequently used subsystems

```csharp
// Cache subsystem references for better performance
private ICurrencySystem _currencySystem;
private IInventorySystem _inventorySystem;

void Start()
{
    _currencySystem = ResourceManagementSystem.Instance.GetCurrencySystem();
    _inventorySystem = ResourceManagementSystem.Instance.GetInventorySystem();
}

// Use cached references for frequent operations
void ProcessDailyTransactions()
{
    foreach (var transaction in dailyTransactions)
    {
        _currencySystem.ProcessTransaction(
            transaction.description,
            transaction.amount,
            transaction.category
        );
    }
}
```

---

This guide covers the main integration points, but the system is designed to be flexible. For more specific integration needs, consult the API documentation or extend the system with custom components.

For further assistance, contact the system maintainers or refer to the code examples in the project.