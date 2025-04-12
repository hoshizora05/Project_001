using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ResourceManagement
{
    /// <summary>
    /// Manages all inventory-related operations in the resource management system
    /// </summary>
    public class InventoryManager : IInventorySystem
    {
        #region Private Fields
        private string _playerId;
        private ResourceSystemConfig.InventoryConfig _config;
        private List<InventoryContainer> _containers = new List<InventoryContainer>();
        private Dictionary<EquipmentSlotType, Item> _equippedItems = new Dictionary<EquipmentSlotType, Item>();
        private ItemDatabase _itemDatabase;
        private EquipmentManager _equipmentManager;
        private Dictionary<string, ItemAcquisitionStats> _itemStats = new Dictionary<string, ItemAcquisitionStats>();
        
        // Crafting system reference
        private CraftingSystem _craftingSystem;
        
        // Item lookup cache for better performance
        private Dictionary<string, (InventoryContainer, InventorySlot)> _itemLookupCache = 
            new Dictionary<string, (InventoryContainer, InventorySlot)>();
        #endregion

        #region Events
        public event Action<Item, int, InventoryContainer> OnItemAdded;
        public event Action<Item, int, InventoryContainer> OnItemRemoved;
        public event Action<Item, InventoryContainer> OnItemUsed;
        public event Action<Item, int, List<ItemConsumption>> OnItemCrafted;
        public event Action<Item, EquipmentSlotType> OnItemEquipped;
        public event Action<EquipmentSlotType> OnItemUnequipped;
        #endregion

        #region Constructor
        public InventoryManager(ResourceSystemConfig.InventoryConfig config)
        {
            _config = config;
            _itemDatabase = new ItemDatabase();
            _equipmentManager = new EquipmentManager();
            _craftingSystem = new CraftingSystem();
        }
        #endregion

        #region Initialization Methods
        public void Initialize(string playerId)
        {
            _playerId = playerId;
            InitializeContainers();
            InitializeEquipmentSlots();
            
            // Clear lookup cache
            _itemLookupCache.Clear();
        }
        
        private void InitializeContainers()
        {
            _containers.Clear();
            
            foreach (var containerDef in _config.defaultContainers)
            {
                var container = new InventoryContainer
                {
                    containerId = containerDef.containerId,
                    name = containerDef.name,
                    maxWeight = containerDef.maxWeight,
                    maxSlots = containerDef.maxSlots,
                    accessRestrictions = containerDef.accessRestrictions,
                    uiSettings = containerDef.uiSettings,
                    slots = new List<InventorySlot>(),
                    currentWeight = 0f,
                    usedSlots = 0
                };
                
                // Initialize slots
                for (int i = 0; i < containerDef.maxSlots; i++)
                {
                    container.slots.Add(new InventorySlot
                    {
                        slotIndex = i,
                        item = null,
                        stackSize = 0,
                        isLocked = false,
                        restrictions = new SlotRestrictions()
                    });
                }
                
                _containers.Add(container);
            }
            
            // If no containers were defined, create a default one
            if (_containers.Count == 0)
            {
                var defaultContainer = new InventoryContainer
                {
                    containerId = "default_container",
                    name = "Inventory",
                    maxWeight = _config.baseWeightCapacity,
                    maxSlots = _config.baseSlotCapacity,
                    accessRestrictions = new ContainerAccessRestrictions(),
                    uiSettings = new ContainerUISettings
                    {
                        displayName = "Inventory",
                        containerColor = Color.white,
                        isVisible = true,
                        displayOrder = 0,
                        showInHUD = true
                    },
                    slots = new List<InventorySlot>(),
                    currentWeight = 0f,
                    usedSlots = 0
                };
                
                // Initialize slots
                for (int i = 0; i < _config.baseSlotCapacity; i++)
                {
                    defaultContainer.slots.Add(new InventorySlot
                    {
                        slotIndex = i,
                        item = null,
                        stackSize = 0,
                        isLocked = false,
                        restrictions = new SlotRestrictions()
                    });
                }
                
                _containers.Add(defaultContainer);
            }
        }
        
        private void InitializeEquipmentSlots()
        {
            _equippedItems.Clear();
            
            // Initialize all equipment slots
            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                _equippedItems[slotType] = null;
            }
        }
        #endregion

        #region Inventory Operations
        public bool AddItem(string itemId, int quantity, string containerId = "")
        {
            if (quantity <= 0)
                return false;
            
            // Get item definition
            Item itemDefinition = _itemDatabase.GetItem(itemId);
            if (itemDefinition == null)
            {
                Debug.LogWarning($"Item with ID {itemId} not found in database");
                return false;
            }
            
            // Find appropriate container
            InventoryContainer container = FindContainerForItem(itemDefinition, containerId);
            if (container == null)
            {
                Debug.LogWarning($"No suitable container found for item {itemId}");
                return false;
            }
            
            // Check if item is stackable
            if (itemDefinition.properties.isStackable)
            {
                // Try to find existing stack
                var existingSlot = container.slots.FindIndex(s => 
                    s.item?.itemId == itemId && 
                    s.stackSize < itemDefinition.properties.maxStackSize);
                
                if (existingSlot >= 0)
                {
                    var slot = container.slots[existingSlot];
                    
                    // Calculate how many items can be added to this stack
                    int spaceInStack = itemDefinition.properties.maxStackSize - slot.stackSize;
                    int amountToAdd = Mathf.Min(quantity, spaceInStack);
                    
                    // Add items to existing stack
                    slot.stackSize += amountToAdd;
                    container.slots[existingSlot] = slot;
                    
                    // Update container weight
                    container.currentWeight += itemDefinition.properties.weight * amountToAdd;
                    
                    // Update item stats
                    UpdateItemAcquisitionStats(itemId, amountToAdd);
                    
                    // Update cache
                    UpdateItemLookupCache(itemId, container, slot);
                    
                    // Trigger event
                    OnItemAdded?.Invoke(itemDefinition, amountToAdd, container);
                    
                    // If we added all items, return success
                    if (amountToAdd == quantity)
                        return true;
                    
                    // Otherwise, continue with remaining items
                    quantity -= amountToAdd;
                }
            }
            
            // Need to find empty slots for remaining items
            while (quantity > 0)
            {
                // Find empty slot
                var emptySlotIndex = container.slots.FindIndex(s => s.item == null && !s.isLocked);
                
                if (emptySlotIndex < 0)
                {
                    // No empty slot found in this container, try to find another container
                    var alternateContainer = FindContainerForItem(itemDefinition, "", container.containerId);
                    if (alternateContainer == null)
                    {
                        Debug.LogWarning($"Not enough space to add all items. Added {quantity} less than requested");
                        return false;
                    }
                    
                    container = alternateContainer;
                    continue;
                }
                
                // Create item instance
                Item newItemInstance = CreateItemInstance(itemDefinition);
                
                // Calculate stack size for this slot
                int stackSize = itemDefinition.properties.isStackable ? 
                    Mathf.Min(quantity, itemDefinition.properties.maxStackSize) : 1;
                
                // Add item to slot
                var slot = container.slots[emptySlotIndex];
                slot.item = newItemInstance;
                slot.stackSize = stackSize;
                container.slots[emptySlotIndex] = slot;
                
                // Update container stats
                container.currentWeight += itemDefinition.properties.weight * stackSize;
                container.usedSlots++;
                
                // Update item stats
                UpdateItemAcquisitionStats(itemId, stackSize);
                
                // Update cache
                UpdateItemLookupCache(itemId, container, slot);
                
                // Trigger event
                OnItemAdded?.Invoke(newItemInstance, stackSize, container);
                
                // Reduce remaining quantity
                quantity -= stackSize;
            }
            
            return true;
        }
        
        public bool RemoveItem(string itemId, int quantity, string containerId = "")
        {
            if (quantity <= 0)
                return false;
            
            // Track how many items we've removed
            int removedCount = 0;
            
            // Use cache for lookup if available
            if (_itemLookupCache.TryGetValue(itemId, out var cachedItem))
            {
                InventoryContainer container = cachedItem.Item1;
                InventorySlot slot = cachedItem.Item2;
                
                // Skip if container doesn't match specified containerId
                if (!string.IsNullOrEmpty(containerId) && container.containerId != containerId)
                {
                    // Fall through to exhaustive search
                }
                else if (slot.item != null && slot.item.itemId == itemId)
                {
                    // Remove from this slot first
                    int amountToRemove = Mathf.Min(quantity, slot.stackSize);
                    
                    // Update slot
                    RemoveFromSlot(container, slot, amountToRemove);
                    
                    // Track progress
                    removedCount += amountToRemove;
                    quantity -= amountToRemove;
                    
                    if (quantity <= 0)
                        return true;
                }
            }
            
            // If we still have items to remove, do a full search
            foreach (var container in _containers)
            {
                // Skip if container doesn't match specified containerId
                if (!string.IsNullOrEmpty(containerId) && container.containerId != containerId)
                    continue;
                
                for (int i = 0; i < container.slots.Count; i++)
                {
                    var slot = container.slots[i];
                    
                    // Skip empty slots or non-matching items
                    if (slot.item == null || slot.item.itemId != itemId)
                        continue;
                    
                    // Calculate how many to remove from this slot
                    int amountToRemove = Mathf.Min(quantity, slot.stackSize);
                    
                    // Update slot
                    RemoveFromSlot(container, slot, amountToRemove);
                    
                    // Track progress
                    removedCount += amountToRemove;
                    quantity -= amountToRemove;
                    
                    if (quantity <= 0)
                        return true;
                }
            }
            
            // If we removed some but not all, it's a partial success
            return removedCount > 0;
        }
        
        private void RemoveFromSlot(InventoryContainer container, InventorySlot slot, int amount)
        {
            // Get the actual slot reference from the container
            int slotIndex = slot.slotIndex;
            slot = container.slots[slotIndex]; // Make sure we're working with the latest data
            
            Item item = slot.item;
            
            // Update weight before modifying the slot
            container.currentWeight -= item.properties.weight * amount;
            
            // Update slot
            if (slot.stackSize <= amount)
            {
                // Remove entire slot
                slot.item = null;
                slot.stackSize = 0;
                container.usedSlots--;
                
                // Remove from cache
                _itemLookupCache.Remove(item.itemId);
            }
            else
            {
                // Reduce stack
                slot.stackSize -= amount;
                
                // Update cache with new slot data
                _itemLookupCache[item.itemId] = (container, slot);
            }
            
            // Update container
            container.slots[slotIndex] = slot;
            
            // Trigger event
            OnItemRemoved?.Invoke(item, amount, container);
        }
        
        public bool UseItem(string itemId, string containerId = "")
        {
            // Find the item
            Item item = null;
            InventoryContainer container = null;
            InventorySlot slot = default;
            int slotIndex = -1;
            
            // Use cache for lookup if available
            if (_itemLookupCache.TryGetValue(itemId, out var cachedItem))
            {
                container = cachedItem.Item1;
                slot = cachedItem.Item2;
                slotIndex = slot.slotIndex;
                
                // Skip if container doesn't match specified containerId
                if (!string.IsNullOrEmpty(containerId) && container.containerId != containerId)
                {
                    // Fall through to exhaustive search
                    container = null;
                }
                else if (slot.item != null && slot.item.itemId == itemId)
                {
                    item = slot.item;
                }
            }
            
            // If not found in cache, do a full search
            if (item == null)
            {
                foreach (var cont in _containers)
                {
                    // Skip if container doesn't match specified containerId
                    if (!string.IsNullOrEmpty(containerId) && cont.containerId != containerId)
                        continue;
                    
                    for (int i = 0; i < cont.slots.Count; i++)
                    {
                        var sl = cont.slots[i];
                        
                        // Skip empty slots or non-matching items
                        if (sl.item == null || sl.item.itemId != itemId)
                            continue;
                        
                        item = sl.item;
                        container = cont;
                        slot = sl;
                        slotIndex = i;
                        break;
                    }
                    
                    if (item != null)
                        break;
                }
            }
            
            if (item == null || container == null)
            {
                Debug.LogWarning($"Item with ID {itemId} not found in inventory");
                return false;
            }
            
            // Check if item is usable
            if (!item.properties.isConsumable)
            {
                Debug.LogWarning($"Item {item.name} is not consumable");
                return false;
            }
            
            // Apply item effects
            ApplyItemEffects(item);
            
            // Trigger event
            OnItemUsed?.Invoke(item, container);
            
            // Consume one from stack
            RemoveFromSlot(container, slot, 1);
            
            return true;
        }
        
        public bool MoveItem(string itemId, string sourceContainerId, string targetContainerId, int quantity = 1)
        {
            if (quantity <= 0)
                return false;
            
            // Find source container
            var sourceContainer = _containers.Find(c => c.containerId == sourceContainerId);
            if (sourceContainer == null)
            {
                Debug.LogWarning($"Source container {sourceContainerId} not found");
                return false;
            }
            
            // Find target container
            var targetContainer = _containers.Find(c => c.containerId == targetContainerId);
            if (targetContainer == null)
            {
                Debug.LogWarning($"Target container {targetContainerId} not found");
                return false;
            }
            
            // Find the item in source container
            Item itemToMove = null;
            int availableQuantity = 0;
            
            foreach (var slot in sourceContainer.slots)
            {
                if (slot.item != null && slot.item.itemId == itemId)
                {
                    itemToMove = slot.item;
                    availableQuantity = slot.stackSize;
                    break;
                }
            }
            
            if (itemToMove == null)
            {
                Debug.LogWarning($"Item {itemId} not found in source container");
                return false;
            }
            
            // Adjust quantity if necessary
            quantity = Mathf.Min(quantity, availableQuantity);
            
            // Check if there's enough space in target container
            if (!CanContainerFitItem(targetContainer, itemToMove, quantity))
            {
                Debug.LogWarning($"Not enough space in target container for {quantity} {itemToMove.name}");
                return false;
            }
            
            // Remove from source
            if (!RemoveItem(itemId, quantity, sourceContainerId))
            {
                Debug.LogError($"Failed to remove item {itemId} from source container");
                return false;
            }
            
            // Add to target
            if (!AddItem(itemId, quantity, targetContainerId))
            {
                // If add fails, put the items back
                AddItem(itemId, quantity, sourceContainerId);
                Debug.LogError($"Failed to add item {itemId} to target container");
                return false;
            }
            
            return true;
        }
        
        public bool CraftItem(string recipeId, int quantity = 1)
        {
            if (quantity <= 0)
                return false;
            
            // Get recipe
            var recipe = _craftingSystem.GetRecipe(recipeId);
            if (recipe == null)
            {
                Debug.LogWarning($"Recipe {recipeId} not found");
                return false;
            }
            
            // Check if we have all the required ingredients
            foreach (var ingredient in recipe.ingredients)
            {
                int requiredAmount = ingredient.quantity * quantity;
                int availableAmount = GetItemQuantity(ingredient.itemId);
                
                if (availableAmount < requiredAmount)
                {
                    Debug.LogWarning($"Not enough {ingredient.itemId} for crafting. Need {requiredAmount}, have {availableAmount}");
                    return false;
                }
            }
            
            // Check if there's enough space for the crafted items
            Item resultItem = _itemDatabase.GetItem(recipe.resultItemId);
            if (resultItem == null)
            {
                Debug.LogWarning($"Result item {recipe.resultItemId} not found in database");
                return false;
            }
            
            bool hasSpace = false;
            foreach (var container in _containers)
            {
                if (CanContainerFitItem(container, resultItem, recipe.resultQuantity * quantity))
                {
                    hasSpace = true;
                    break;
                }
            }
            
            if (!hasSpace)
            {
                Debug.LogWarning($"Not enough space for crafted items");
                return false;
            }
            
            // Consume ingredients
            List<ItemConsumption> consumedItems = new List<ItemConsumption>();
            foreach (var ingredient in recipe.ingredients)
            {
                int requiredAmount = ingredient.quantity * quantity;
                
                // Track consumption for event
                consumedItems.Add(new ItemConsumption
                {
                    itemId = ingredient.itemId,
                    quantity = requiredAmount,
                    qualityFactor = 1.0f // Default quality factor
                });
                
                if (!RemoveItem(ingredient.itemId, requiredAmount))
                {
                    Debug.LogError($"Failed to consume ingredient {ingredient.itemId}");
                    
                    // Try to restore already consumed ingredients
                    for (int i = 0; i < consumedItems.Count - 1; i++)
                    {
                        AddItem(consumedItems[i].itemId, consumedItems[i].quantity);
                    }
                    
                    return false;
                }
            }
            
            // Create crafted items
            if (!AddItem(recipe.resultItemId, recipe.resultQuantity * quantity))
            {
                Debug.LogError($"Failed to add crafted items to inventory");
                
                // Restore consumed ingredients
                foreach (var consumed in consumedItems)
                {
                    AddItem(consumed.itemId, consumed.quantity);
                }
                
                return false;
            }
            
            // Trigger event
            OnItemCrafted?.Invoke(resultItem, recipe.resultQuantity * quantity, consumedItems);
            
            return true;
        }
        
        public bool EquipItem(string itemId, EquipmentSlotType slotType)
        {
            // Find the item in inventory
            Item itemToEquip = null;
            InventoryContainer sourceContainer = null;
            InventorySlot sourceSlot = default;
            
            // Use cache for lookup if available
            if (_itemLookupCache.TryGetValue(itemId, out var cachedItem))
            {
                sourceContainer = cachedItem.Item1;
                sourceSlot = cachedItem.Item2;
                
                if (sourceSlot.item != null && sourceSlot.item.itemId == itemId)
                {
                    itemToEquip = sourceSlot.item;
                }
            }
            
            // If not found in cache, do a full search
            if (itemToEquip == null)
            {
                foreach (var container in _containers)
                {
                    for (int i = 0; i < container.slots.Count; i++)
                    {
                        var slot = container.slots[i];
                        
                        if (slot.item != null && slot.item.itemId == itemId)
                        {
                            itemToEquip = slot.item;
                            sourceContainer = container;
                            sourceSlot = slot;
                            break;
                        }
                    }
                    
                    if (itemToEquip != null)
                        break;
                }
            }
            
            if (itemToEquip == null)
            {
                Debug.LogWarning($"Item {itemId} not found in inventory");
                return false;
            }
            
            // Check if item is equippable
            if (!itemToEquip.properties.isEquippable)
            {
                Debug.LogWarning($"Item {itemToEquip.name} is not equippable");
                return false;
            }
            
            // Check if item can be equipped in the specified slot
            if (itemToEquip.properties.equipSlot != slotType)
            {
                Debug.LogWarning($"Item {itemToEquip.name} cannot be equipped in slot {slotType}");
                return false;
            }
            
            // Check level requirements if enabled
            if (_config.restrictEquipmentByLevel)
            {
                bool meetsRequirements = true;
                foreach (var requirement in itemToEquip.requirements)
                {
                    // This would need integration with the progression system
                    // For now, we'll just assume requirements are met
                    meetsRequirements = true;
                }
                
                if (!meetsRequirements)
                {
                    Debug.LogWarning($"Requirements not met for equipping {itemToEquip.name}");
                    return false;
                }
            }
            
            // Unequip current item in that slot
            if (_equippedItems[slotType] != null)
            {
                if (!UnequipItem(slotType))
                {
                    Debug.LogWarning($"Failed to unequip current item in slot {slotType}");
                    return false;
                }
            }
            
            // Remove item from inventory
            if (!RemoveItem(itemId, 1, sourceContainer.containerId))
            {
                Debug.LogError($"Failed to remove item {itemId} from inventory");
                return false;
            }
            
            // Equip the item
            _equippedItems[slotType] = itemToEquip;
            
            // Apply equipment effects
            _equipmentManager.ApplyEquipmentEffects(itemToEquip);
            
            // Trigger event
            OnItemEquipped?.Invoke(itemToEquip, slotType);
            
            return true;
        }
        
        public bool UnequipItem(EquipmentSlotType slotType)
        {
            // Check if there's an item equipped in the slot
            if (_equippedItems[slotType] == null)
            {
                Debug.LogWarning($"No item equipped in slot {slotType}");
                return false;
            }
            
            Item itemToUnequip = _equippedItems[slotType];
            
            // Check if there's room in inventory
            InventoryContainer targetContainer = null;
            foreach (var container in _containers)
            {
                if (CanContainerFitItem(container, itemToUnequip, 1))
                {
                    targetContainer = container;
                    break;
                }
            }
            
            if (targetContainer == null)
            {
                Debug.LogWarning($"No space in inventory to unequip {itemToUnequip.name}");
                return false;
            }
            
            // Remove equipment effects
            _equipmentManager.RemoveEquipmentEffects(itemToUnequip);
            
            // Add item to inventory
            if (!AddItem(itemToUnequip.itemId, 1, targetContainer.containerId))
            {
                Debug.LogError($"Failed to add unequipped item to inventory");
                return false;
            }
            
            // Clear the equipment slot
            _equippedItems[slotType] = null;
            
            // Trigger event
            OnItemUnequipped?.Invoke(slotType);
            
            return true;
        }
        #endregion

        #region Helper Methods
        private InventoryContainer FindContainerForItem(Item item, string preferredContainerId = "", string excludedContainerId = "")
        {
            // First try preferred container if specified
            if (!string.IsNullOrEmpty(preferredContainerId))
            {
                var preferredContainer = _containers.Find(c => c.containerId == preferredContainerId);
                if (preferredContainer != null && CanContainerFitItem(preferredContainer, item, 1))
                {
                    return preferredContainer;
                }
            }
            
            // Try to find a container with available space
            foreach (var container in _containers)
            {
                // Skip excluded container
                if (!string.IsNullOrEmpty(excludedContainerId) && container.containerId == excludedContainerId)
                    continue;
                
                if (CanContainerFitItem(container, item, 1))
                {
                    return container;
                }
            }
            
            return null;
        }
        
        private bool CanContainerFitItem(InventoryContainer container, Item item, int quantity)
        {
            // Check weight capacity
            float totalWeight = item.properties.weight * quantity;
            if (container.currentWeight + totalWeight > container.maxWeight)
            {
                return false;
            }
            
            // For stackable items, check if we can add to existing stacks
            if (item.properties.isStackable)
            {
                int remainingQuantity = quantity;
                
                // First, check existing stacks
                foreach (var slot in container.slots)
                {
                    if (slot.item != null && slot.item.itemId == item.itemId)
                    {
                        int spaceInStack = item.properties.maxStackSize - slot.stackSize;
                        if (spaceInStack > 0)
                        {
                            remainingQuantity -= Mathf.Min(remainingQuantity, spaceInStack);
                            if (remainingQuantity <= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
                
                // If we still have items to place, check for empty slots
                if (remainingQuantity > 0)
                {
                    int emptySlots = container.slots.Count(s => s.item == null && !s.isLocked);
                    if (emptySlots == 0)
                    {
                        return false;
                    }
                    
                    // Calculate slots needed for remaining quantity
                    int slotsNeeded = Mathf.CeilToInt((float)remainingQuantity / item.properties.maxStackSize);
                    return emptySlots >= slotsNeeded;
                }
                
                return true;
            }
            else
            {
                // For non-stackable items, check if we have enough empty slots
                int emptySlots = container.slots.Count(s => s.item == null && !s.isLocked);
                return emptySlots >= quantity;
            }
        }
        
        private Item CreateItemInstance(Item template)
        {
            // Create a deep copy of the item
            Item instance = new Item
            {
                itemId = template.itemId,
                uniqueInstanceId = GenerateUniqueId(),
                name = template.name,
                type = template.type,
                category = template.category,
                properties = template.properties, // In a real implementation, this would be deep-copied
                currentState = new ItemState
                {
                    durability = 100f, // Start with full durability
                    quality = 100f,    // Start with full quality
                    chargesLeft = template.properties.isConsumable ? 1 : 0,
                    expirationDate = template.properties.isConsumable ? 
                        DateTime.Now.AddDays(30) : DateTime.MaxValue, // Example expiration
                    activeModifiers = new List<ItemStateModifier>()
                },
                effects = new List<ItemEffect>(template.effects), // In a real implementation, this would be deep-copied
                requirements = new List<ItemRequirement>(template.requirements),
                tags = new List<string>(template.tags)
            };
            
            return instance;
        }
        
        private string GenerateUniqueId()
        {
            return $"item_{Guid.NewGuid().ToString("N")}";
        }
        
        private void ApplyItemEffects(Item item)
        {
            // In a real implementation, this would apply item effects to the player
            foreach (var effect in item.effects)
            {
                Debug.Log($"Applying effect {effect.effectName} to {effect.targetStat}: {effect.effectValue}");
                
                // Integration with other systems would happen here
            }
        }
        
        private void UpdateItemAcquisitionStats(string itemId, int quantity)
        {
            if (!_itemStats.ContainsKey(itemId))
            {
                _itemStats[itemId] = new ItemAcquisitionStats { itemId = itemId };
            }
            
            _itemStats[itemId].totalAcquired += quantity;
            _itemStats[itemId].lastAcquiredTime = DateTime.Now;
        }
        
        private void UpdateItemLookupCache(string itemId, InventoryContainer container, InventorySlot slot)
        {
            _itemLookupCache[itemId] = (container, slot);
        }
        #endregion

        #region Action Processing
        public ActionValidationResult ValidateAction(string actionId)
        {
            // Get required items for the action
            var requiredItems = GetRequiredItemsForAction(actionId);
            if (requiredItems.Count == 0)
            {
                // No item requirements for this action
                return new ActionValidationResult { isValid = true };
            }
            
            // Check if all required items are available
            List<string> missingItems = new List<string>();
            
            foreach (var requirement in requiredItems)
            {
                int availableQuantity = GetItemQuantity(requirement.itemId);
                if (availableQuantity < requirement.quantity)
                {
                    Item itemDef = _itemDatabase.GetItem(requirement.itemId);
                    string itemName = itemDef != null ? itemDef.name : requirement.itemId;
                    missingItems.Add($"{itemName} (need {requirement.quantity}, have {availableQuantity})");
                }
            }
            
            if (missingItems.Count > 0)
            {
                return new ActionValidationResult
                {
                    isValid = false,
                    message = $"Missing required items: {string.Join(", ", missingItems)}"
                };
            }
            
            return new ActionValidationResult { isValid = true };
        }
        
        public void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch)
        {
            // Get required items for the action
            var requiredItems = GetRequiredItemsForAction(actionId);
            if (requiredItems.Count == 0)
            {
                return; // No items required
            }
            
            // Create snapshot of current inventory state for potential rollback
            Dictionary<string, int> inventorySnapshot = new Dictionary<string, int>();
            foreach (var requirement in requiredItems)
            {
                inventorySnapshot[requirement.itemId] = GetItemQuantity(requirement.itemId);
            }
            
            // Process required items
            foreach (var requirement in requiredItems)
            {
                // Create resource change record
                ResourceChange change = new ResourceChange
                {
                    resourceType = ResourceType.Item,
                    resourceId = requirement.itemId,
                    amount = -requirement.quantity,
                    source = actionId
                };
                
                // Create rollback action
                Action rollback = () => 
                {
                    // If rollback is needed, restore original quantities
                    foreach (var item in inventorySnapshot)
                    {
                        int currentQuantity = GetItemQuantity(item.Key);
                        int delta = item.Value - currentQuantity;
                        
                        if (delta > 0)
                        {
                            AddItem(item.Key, delta);
                        }
                    }
                };
                
                // Add to batch
                batch.AddChange(change, rollback);
                
                // Apply the change
                RemoveItem(requirement.itemId, requirement.quantity);
            }
            
            // Process reward items if any
            var rewardItems = GetRewardItemsForAction(actionId);
            foreach (var reward in rewardItems)
            {
                // Create resource change record
                ResourceChange change = new ResourceChange
                {
                    resourceType = ResourceType.Item,
                    resourceId = reward.itemId,
                    amount = reward.quantity,
                    source = actionId
                };
                
                // Create rollback action
                Action rollback = () => 
                {
                    // If rollback is needed, remove the added items
                    RemoveItem(reward.itemId, reward.quantity);
                };
                
                // Add to batch
                batch.AddChange(change, rollback);
                
                // Apply the change
                AddItem(reward.itemId, reward.quantity);
            }
        }
        
        private List<RequiredItem> GetRequiredItemsForAction(string actionId)
        {
            // In a real implementation, this would come from a data-driven source
            // For this example, we'll return a hardcoded list based on the action ID
            
            List<RequiredItem> requirements = new List<RequiredItem>();
            
            switch (actionId)
            {
                case "craft_basic_tool":
                    requirements.Add(new RequiredItem { itemId = "wood", quantity = 5 });
                    requirements.Add(new RequiredItem { itemId = "stone", quantity = 3 });
                    break;
                    
                case "repair_equipment":
                    requirements.Add(new RequiredItem { itemId = "repair_kit", quantity = 1 });
                    break;
                    
                case "cook_meal":
                    requirements.Add(new RequiredItem { itemId = "raw_food", quantity = 2 });
                    requirements.Add(new RequiredItem { itemId = "spice", quantity = 1 });
                    break;
                    
                case "brew_potion":
                    requirements.Add(new RequiredItem { itemId = "herb", quantity = 3 });
                    requirements.Add(new RequiredItem { itemId = "water", quantity = 1 });
                    break;
            }
            
            return requirements;
        }
        
        private List<ItemReward> GetRewardItemsForAction(string actionId)
        {
            // In a real implementation, this would come from a data-driven source
            // For this example, we'll return a hardcoded list based on the action ID
            
            List<ItemReward> rewards = new List<ItemReward>();
            
            switch (actionId)
            {
                case "craft_basic_tool":
                    rewards.Add(new ItemReward { itemId = "basic_tool", quantity = 1 });
                    break;
                    
                case "cook_meal":
                    rewards.Add(new ItemReward { itemId = "cooked_meal", quantity = 1 });
                    break;
                    
                case "brew_potion":
                    rewards.Add(new ItemReward { itemId = "health_potion", quantity = 1 });
                    break;
                    
                case "complete_quest":
                    rewards.Add(new ItemReward { itemId = "quest_reward", quantity = 1 });
                    rewards.Add(new ItemReward { itemId = "coin_pouch", quantity = 1 });
                    break;
            }
            
            return rewards;
        }
        #endregion

        #region Public Interface Methods
        public Item GetItem(string itemId)
        {
            // Use cache for lookup if available
            if (_itemLookupCache.TryGetValue(itemId, out var cachedItem))
            {
                if (cachedItem.Item2.item != null && cachedItem.Item2.item.itemId == itemId)
                {
                    return cachedItem.Item2.item;
                }
            }
            
            // Do a full search
            foreach (var container in _containers)
            {
                foreach (var slot in container.slots)
                {
                    if (slot.item != null && slot.item.itemId == itemId)
                    {
                        return slot.item;
                    }
                }
            }
            
            return null;
        }
        
        public int GetItemQuantity(string itemId)
        {
            int totalQuantity = 0;
            
            foreach (var container in _containers)
            {
                foreach (var slot in container.slots)
                {
                    if (slot.item != null && slot.item.itemId == itemId)
                    {
                        totalQuantity += slot.stackSize;
                    }
                }
            }
            
            return totalQuantity;
        }
        
        public InventoryContainer GetContainer(string containerId)
        {
            return _containers.Find(c => c.containerId == containerId);
        }
        
        public InventoryState GetInventoryState()
        {
            // Calculate total stats
            float totalWeight = 0f;
            int totalItems = 0;
            float totalValue = 0f;
            
            foreach (var container in _containers)
            {
                totalWeight += container.currentWeight;
                
                foreach (var slot in container.slots)
                {
                    if (slot.item != null)
                    {
                        totalItems += slot.stackSize;
                        totalValue += slot.item.properties.baseValue * slot.stackSize;
                    }
                }
            }
            
            return new InventoryState
            {
                containers = _containers,
                equippedItems = new Dictionary<EquipmentSlotType, Item>(_equippedItems),
                totalWeight = totalWeight,
                totalItems = totalItems,
                inventoryValue = totalValue
            };
        }
        
        public List<ResourceOptimizationSuggestion> GetOptimizationSuggestions()
        {
            List<ResourceOptimizationSuggestion> suggestions = new List<ResourceOptimizationSuggestion>();
            
            // Check for containers near capacity
            foreach (var container in _containers)
            {
                float capacityPercentage = container.currentWeight / container.maxWeight;
                if (capacityPercentage > 0.9f)
                {
                    suggestions.Add(new ResourceOptimizationSuggestion
                    {
                        suggestionId = $"container_capacity_{container.containerId}",
                        title = $"{container.name} Nearly Full",
                        description = $"Your {container.name} is at {capacityPercentage:P0} capacity. Consider selling or storing less essential items.",
                        potentialBenefit = container.currentWeight * 0.3f, // Estimated weight reduction
                        primaryResourceType = ResourceType.Item,
                        priority = capacityPercentage > 0.95f ? 9 : 7,
                        actionSteps = new List<string>
                        {
                            "Sell low-value items",
                            "Move rarely used items to storage",
                            "Consume usable items"
                        }
                    });
                }
            }
            
            // Check for stackable items that could be consolidated
            Dictionary<string, int> stackableItems = new Dictionary<string, int>();
            Dictionary<string, int> stackableSlots = new Dictionary<string, int>();
            
            foreach (var container in _containers)
            {
                foreach (var slot in container.slots)
                {
                    if (slot.item != null && slot.item.properties.isStackable)
                    {
                        string itemId = slot.item.itemId;
                        
                        if (!stackableItems.ContainsKey(itemId))
                        {
                            stackableItems[itemId] = 0;
                            stackableSlots[itemId] = 0;
                        }
                        
                        stackableItems[itemId] += slot.stackSize;
                        stackableSlots[itemId]++;
                    }
                }
            }
            
            List<string> consolidationCandidates = new List<string>();
            
            foreach (var pair in stackableItems)
            {
                string itemId = pair.Key;
                int totalQuantity = pair.Value;
                int slotsUsed = stackableSlots[itemId];
                
                // Get max stack size
                Item item = _itemDatabase.GetItem(itemId);
                if (item == null)
                    continue;
                
                int maxStackSize = item.properties.maxStackSize;
                int optimalSlots = Mathf.CeilToInt((float)totalQuantity / maxStackSize);
                
                if (slotsUsed > optimalSlots)
                {
                    consolidationCandidates.Add(item.name);
                }
            }
            
            if (consolidationCandidates.Count > 0)
            {
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "consolidate_stacks",
                    title = "Consolidate Item Stacks",
                    description = $"You could free up inventory slots by consolidating these items: {string.Join(", ", consolidationCandidates)}",
                    potentialBenefit = consolidationCandidates.Count, // Number of slots to be freed
                    primaryResourceType = ResourceType.Item,
                    priority = 6,
                    actionSteps = new List<string> { "Move items between containers to consolidate stacks" }
                });
            }
            
            // Check for item expiration
            if (_config.enableItemExpiration)
            {
                List<string> expiringItems = new List<string>();
                
                foreach (var container in _containers)
                {
                    foreach (var slot in container.slots)
                    {
                        if (slot.item != null && slot.item.properties.isConsumable)
                        {
                            TimeSpan timeToExpiration = slot.item.currentState.expirationDate - DateTime.Now;
                            if (timeToExpiration.TotalDays < 3 && timeToExpiration.TotalDays > 0)
                            {
                                expiringItems.Add($"{slot.item.name} ({timeToExpiration.Days} days)");
                            }
                        }
                    }
                }
                
                if (expiringItems.Count > 0)
                {
                    suggestions.Add(new ResourceOptimizationSuggestion
                    {
                        suggestionId = "expiring_items",
                        title = "Items About to Expire",
                        description = $"These items will expire soon: {string.Join(", ", expiringItems)}. Consider using them.",
                        potentialBenefit = expiringItems.Count * 10f, // Estimated value
                        primaryResourceType = ResourceType.Item,
                        priority = 8,
                        actionSteps = new List<string> { "Use consumable items before they expire" }
                    });
                }
            }
            
            // Check for low durability equipment
            if (_config.enableItemDegradation)
            {
                List<string> lowDurabilityItems = new List<string>();
                
                // Check equipped items
                foreach (var pair in _equippedItems)
                {
                    if (pair.Value != null && pair.Value.currentState.durability < 20f)
                    {
                        lowDurabilityItems.Add($"{pair.Value.name} ({pair.Value.currentState.durability:F0}%)");
                    }
                }
                
                // Check inventory items
                foreach (var container in _containers)
                {
                    foreach (var slot in container.slots)
                    {
                        if (slot.item != null && slot.item.properties.isEquippable && slot.item.currentState.durability < 20f)
                        {
                            lowDurabilityItems.Add($"{slot.item.name} ({slot.item.currentState.durability:F0}%)");
                        }
                    }
                }
                
                if (lowDurabilityItems.Count > 0)
                {
                    suggestions.Add(new ResourceOptimizationSuggestion
                    {
                        suggestionId = "low_durability",
                        title = "Items Need Repair",
                        description = $"These items have low durability: {string.Join(", ", lowDurabilityItems)}. Repair them soon.",
                        potentialBenefit = lowDurabilityItems.Count * 25f, // Estimated value
                        primaryResourceType = ResourceType.Item,
                        priority = 9,
                        actionSteps = new List<string> { "Visit a repair vendor", "Use repair kits on damaged items" }
                    });
                }
            }
            
            return suggestions;
        }
        
        public Dictionary<string, object> GetAnalyticsData()
        {
            Dictionary<string, object> analytics = new Dictionary<string, object>();
            
            // Total inventory stats
            var state = GetInventoryState();
            analytics["totalItems"] = state.totalItems;
            analytics["totalWeight"] = state.totalWeight;
            analytics["inventoryValue"] = state.inventoryValue;
            
            // Container utilization
            List<Dictionary<string, object>> containerStats = new List<Dictionary<string, object>>();
            foreach (var container in _containers)
            {
                containerStats.Add(new Dictionary<string, object>
                {
                    { "containerId", container.containerId },
                    { "name", container.name },
                    { "capacityUsed", container.currentWeight / container.maxWeight },
                    { "slotsUsed", container.usedSlots },
                    { "totalSlots", container.maxSlots }
                });
            }
            analytics["containerStats"] = containerStats;
            
            // Item type breakdown
            Dictionary<ItemType, int> itemTypeCounts = new Dictionary<ItemType, int>();
            Dictionary<ItemCategory, int> itemCategoryCounts = new Dictionary<ItemCategory, int>();
            
            foreach (var container in _containers)
            {
                foreach (var slot in container.slots)
                {
                    if (slot.item != null)
                    {
                        // Count by type
                        if (!itemTypeCounts.ContainsKey(slot.item.type))
                        {
                            itemTypeCounts[slot.item.type] = 0;
                        }
                        itemTypeCounts[slot.item.type] += slot.stackSize;
                        
                        // Count by category
                        if (!itemCategoryCounts.ContainsKey(slot.item.category))
                        {
                            itemCategoryCounts[slot.item.category] = 0;
                        }
                        itemCategoryCounts[slot.item.category] += slot.stackSize;
                    }
                }
            }
            
            analytics["itemTypeCounts"] = itemTypeCounts;
            analytics["itemCategoryCounts"] = itemCategoryCounts;
            
            // Equipment analysis
            int equippedItemCount = _equippedItems.Count(p => p.Value != null);
            float averageItemQuality = _equippedItems.Where(p => p.Value != null)
                                                  .Average(p => p.Value.currentState.quality);
            
            analytics["equippedItemCount"] = equippedItemCount;
            analytics["averageEquipmentQuality"] = averageItemQuality;
            
            return analytics;
        }
        #endregion

        #region Save/Load
        public InventorySaveData GenerateSaveData()
        {
            InventorySaveData saveData = new InventorySaveData
            {
                playerId = _playerId,
                containers = new List<InventorySaveData.SerializedContainer>(),
                equippedItems = new List<InventorySaveData.SerializedEquippedItem>()
            };
            
            // Serialize containers
            foreach (var container in _containers)
            {
                var serializedContainer = new InventorySaveData.SerializedContainer
                {
                    containerId = container.containerId,
                    slots = new List<InventorySaveData.SerializedSlot>()
                };
                
                foreach (var slot in container.slots)
                {
                    if (slot.item != null)
                    {
                        serializedContainer.slots.Add(new InventorySaveData.SerializedSlot
                        {
                            slotIndex = slot.slotIndex,
                            itemId = slot.item.itemId,
                            uniqueInstanceId = slot.item.uniqueInstanceId,
                            stackSize = slot.stackSize,
                            itemState = slot.item.currentState
                        });
                    }
                }
                
                saveData.containers.Add(serializedContainer);
            }
            
            // Serialize equipped items
            foreach (var pair in _equippedItems)
            {
                if (pair.Value != null)
                {
                    saveData.equippedItems.Add(new InventorySaveData.SerializedEquippedItem
                    {
                        slotType = (int)pair.Key,
                        itemId = pair.Value.itemId,
                        uniqueInstanceId = pair.Value.uniqueInstanceId,
                        itemState = pair.Value.currentState
                    });
                }
            }
            
            return saveData;
        }
        
        public void RestoreFromSaveData(InventorySaveData saveData)
        {
            if (saveData == null)
                return;
            
            _playerId = saveData.playerId;
            
            // Clear existing inventory
            foreach (var container in _containers)
            {
                for (int i = 0; i < container.slots.Count; i++)
                {
                    var slot = container.slots[i];
                    slot.item = null;
                    slot.stackSize = 0;
                    container.slots[i] = slot;
                }
                
                container.currentWeight = 0f;
                container.usedSlots = 0;
            }
            
            // Clear equipped items
            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                _equippedItems[slotType] = null;
            }
            
            // Restore containers
            foreach (var serializedContainer in saveData.containers)
            {
                var container = _containers.Find(c => c.containerId == serializedContainer.containerId);
                if (container == null)
                {
                    Debug.LogWarning($"Container {serializedContainer.containerId} not found during restore");
                    continue;
                }
                
                foreach (var serializedSlot in serializedContainer.slots)
                {
                    if (serializedSlot.slotIndex >= container.slots.Count)
                    {
                        Debug.LogWarning($"Slot index {serializedSlot.slotIndex} out of range for container {container.containerId}");
                        continue;
                    }
                    
                    // Get item definition
                    Item itemDefinition = _itemDatabase.GetItem(serializedSlot.itemId);
                    if (itemDefinition == null)
                    {
                        Debug.LogWarning($"Item with ID {serializedSlot.itemId} not found in database during restore");
                        continue;
                    }
                    
                    // Create item instance
                    Item itemInstance = CreateItemInstance(itemDefinition);
                    itemInstance.uniqueInstanceId = serializedSlot.uniqueInstanceId;
                    itemInstance.currentState = serializedSlot.itemState;
                    
                    // Add to container
                    var slot = container.slots[serializedSlot.slotIndex];
                    slot.item = itemInstance;
                    slot.stackSize = serializedSlot.stackSize;
                    container.slots[serializedSlot.slotIndex] = slot;
                    
                    // Update container stats
                    container.currentWeight += itemDefinition.properties.weight * serializedSlot.stackSize;
                    container.usedSlots++;
                    
                    // Update cache
                    UpdateItemLookupCache(itemInstance.itemId, container, slot);
                }
            }
            
            // Restore equipped items
            foreach (var serializedEquippedItem in saveData.equippedItems)
            {
                EquipmentSlotType slotType = (EquipmentSlotType)serializedEquippedItem.slotType;
                
                // Get item definition
                Item itemDefinition = _itemDatabase.GetItem(serializedEquippedItem.itemId);
                if (itemDefinition == null)
                {
                    Debug.LogWarning($"Item with ID {serializedEquippedItem.itemId} not found in database during restore");
                    continue;
                }
                
                // Create item instance
                Item itemInstance = CreateItemInstance(itemDefinition);
                itemInstance.uniqueInstanceId = serializedEquippedItem.uniqueInstanceId;
                itemInstance.currentState = serializedEquippedItem.itemState;
                
                // Equip item
                _equippedItems[slotType] = itemInstance;
                
                // Apply equipment effects
                _equipmentManager.ApplyEquipmentEffects(itemInstance);
            }
        }
        #endregion

        #region Supporting Classes
        public class ItemDatabase
        {
            private Dictionary<string, Item> _items = new Dictionary<string, Item>();
            
            public ItemDatabase()
            {
                // Initialize with some example items
                InitializeExampleItems();
            }
            
            private void InitializeExampleItems()
            {
                // Weapons
                RegisterItem(new Item
                {
                    itemId = "sword",
                    name = "Steel Sword",
                    type = ItemType.Equipment,
                    category = ItemCategory.Weapon,
                    properties = new ItemProperties
                    {
                        baseValue = 100f,
                        weight = 3f,
                        isStackable = false,
                        isEquippable = true,
                        equipSlot = EquipmentSlotType.MainHand,
                        description = "A sturdy steel sword"
                    },
                    effects = new List<ItemEffect>
                    {
                        new ItemEffect { effectId = "damage", effectName = "Physical Damage", targetStat = "damage", effectValue = 20f }
                    },
                    tags = new List<string> { "weapon", "sword", "metal" }
                });
                
                // Armor
                RegisterItem(new Item
                {
                    itemId = "leather_armor",
                    name = "Leather Armor",
                    type = ItemType.Equipment,
                    category = ItemCategory.Armor,
                    properties = new ItemProperties
                    {
                        baseValue = 80f,
                        weight = 5f,
                        isStackable = false,
                        isEquippable = true,
                        equipSlot = EquipmentSlotType.Body,
                        description = "Lightweight leather armor"
                    },
                    effects = new List<ItemEffect>
                    {
                        new ItemEffect { effectId = "defense", effectName = "Physical Defense", targetStat = "defense", effectValue = 15f }
                    },
                    tags = new List<string> { "armor", "leather", "light" }
                });
                
                // Consumables
                RegisterItem(new Item
                {
                    itemId = "health_potion",
                    name = "Health Potion",
                    type = ItemType.Consumable,
                    category = ItemCategory.Potion,
                    properties = new ItemProperties
                    {
                        baseValue = 20f,
                        weight = 0.5f,
                        isStackable = true,
                        maxStackSize = 10,
                        isConsumable = true,
                        description = "Restores 50 health"
                    },
                    effects = new List<ItemEffect>
                    {
                        new ItemEffect { effectId = "heal", effectName = "Healing", targetStat = "health", effectValue = 50f }
                    },
                    tags = new List<string> { "potion", "consumable", "healing" }
                });
                
                // Materials
                RegisterItem(new Item
                {
                    itemId = "wood",
                    name = "Wood",
                    type = ItemType.Material,
                    category = ItemCategory.Crafting,
                    properties = new ItemProperties
                    {
                        baseValue = 5f,
                        weight = 1f,
                        isStackable = true,
                        maxStackSize = 50,
                        description = "Basic crafting material"
                    },
                    tags = new List<string> { "material", "crafting", "wood" }
                });
                
                RegisterItem(new Item
                {
                    itemId = "stone",
                    name = "Stone",
                    type = ItemType.Material,
                    category = ItemCategory.Crafting,
                    properties = new ItemProperties
                    {
                        baseValue = 3f,
                        weight = 2f,
                        isStackable = true,
                        maxStackSize = 50,
                        description = "Basic crafting material"
                    },
                    tags = new List<string> { "material", "crafting", "stone" }
                });
                
                // Quest items
                RegisterItem(new Item
                {
                    itemId = "ancient_relic",
                    name = "Ancient Relic",
                    type = ItemType.Quest,
                    category = ItemCategory.Key,
                    properties = new ItemProperties
                    {
                        baseValue = 500f,
                        weight = 1f,
                        isStackable = false,
                        isQuestItem = true,
                        description = "A mysterious ancient artifact"
                    },
                    tags = new List<string> { "quest", "unique", "valuable" }
                });
                
                // Tools
                RegisterItem(new Item
                {
                    itemId = "repair_kit",
                    name = "Repair Kit",
                    type = ItemType.Tool,
                    category = ItemCategory.Potion,
                    properties = new ItemProperties
                    {
                        baseValue = 50f,
                        weight = 2f,
                        isStackable = true,
                        maxStackSize = 5,
                        isConsumable = true,
                        description = "Used to repair equipment"
                    },
                    effects = new List<ItemEffect>
                    {
                        new ItemEffect { effectId = "repair", effectName = "Repair", targetStat = "durability", effectValue = 50f }
                    },
                    tags = new List<string> { "tool", "repair" }
                });
                
                // Food
                RegisterItem(new Item
                {
                    itemId = "cooked_meal",
                    name = "Cooked Meal",
                    type = ItemType.Consumable,
                    category = ItemCategory.Food,
                    properties = new ItemProperties
                    {
                        baseValue = 15f,
                        weight = 0.5f,
                        isStackable = true,
                        maxStackSize = 10,
                        isConsumable = true,
                        description = "A hearty meal that restores energy"
                    },
                    effects = new List<ItemEffect>
                    {
                        new ItemEffect { effectId = "energy", effectName = "Energy Restoration", targetStat = "energy", effectValue = 30f }
                    },
                    tags = new List<string> { "food", "consumable", "energy" }
                });
                
                RegisterItem(new Item
                {
                    itemId = "raw_food",
                    name = "Raw Food",
                    type = ItemType.Material,
                    category = ItemCategory.Food,
                    properties = new ItemProperties
                    {
                        baseValue = 8f,
                        weight = 0.5f,
                        isStackable = true,
                        maxStackSize = 20,
                        description = "Raw ingredients for cooking"
                    },
                    tags = new List<string> { "food", "raw", "cooking" }
                });
                
                // Other crafting materials
                RegisterItem(new Item
                {
                    itemId = "herb",
                    name = "Herb",
                    type = ItemType.Material,
                    category = ItemCategory.Reagent,
                    properties = new ItemProperties
                    {
                        baseValue = 12f,
                        weight = 0.1f,
                        isStackable = true,
                        maxStackSize = 30,
                        description = "Used in alchemy and cooking"
                    },
                    tags = new List<string> { "herb", "alchemy", "cooking" }
                });
                
                RegisterItem(new Item
                {
                    itemId = "water",
                    name = "Water",
                    type = ItemType.Material,
                    category = ItemCategory.Reagent,
                    properties = new ItemProperties
                    {
                        baseValue = 5f,
                        weight = 0.5f,
                        isStackable = true,
                        maxStackSize = 10,
                        description = "Clean water for crafting and drinking"
                    },
                    tags = new List<string> { "water", "liquid", "crafting" }
                });
                
                RegisterItem(new Item
                {
                    itemId = "spice",
                    name = "Spice",
                    type = ItemType.Material,
                    category = ItemCategory.Reagent,
                    properties = new ItemProperties
                    {
                        baseValue = 10f,
                        weight = 0.1f,
                        isStackable = true,
                        maxStackSize = 20,
                        description = "Flavorful spices for cooking"
                    },
                    tags = new List<string> { "spice", "cooking" }
                });
            }
            
            private void RegisterItem(Item item)
            {
                _items[item.itemId] = item;
            }
            
            public Item GetItem(string itemId)
            {
                return _items.TryGetValue(itemId, out var item) ? item : null;
            }
        }
        
        public class CraftingSystem
        {
            private Dictionary<string, CraftingRecipe> _recipes = new Dictionary<string, CraftingRecipe>();
            
            public CraftingSystem()
            {
                // Initialize with some example recipes
                InitializeExampleRecipes();
            }
            
            private void InitializeExampleRecipes()
            {
                // Tool recipe
                RegisterRecipe(new CraftingRecipe
                {
                    recipeId = "craft_basic_tool",
                    name = "Basic Tool",
                    ingredients = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { itemId = "wood", quantity = 5 },
                        new CraftingIngredient { itemId = "stone", quantity = 3 }
                    },
                    resultItemId = "basic_tool",
                    resultQuantity = 1,
                    craftingTime = 5f, // in game minutes
                    requiredSkillId = "crafting",
                    requiredSkillLevel = 1
                });
                
                // Potion recipe
                RegisterRecipe(new CraftingRecipe
                {
                    recipeId = "brew_potion",
                    name = "Health Potion",
                    ingredients = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { itemId = "herb", quantity = 3 },
                        new CraftingIngredient { itemId = "water", quantity = 1 }
                    },
                    resultItemId = "health_potion",
                    resultQuantity = 1,
                    craftingTime = 3f, // in game minutes
                    requiredSkillId = "alchemy",
                    requiredSkillLevel = 1
                });
                
                // Food recipe
                RegisterRecipe(new CraftingRecipe
                {
                    recipeId = "cook_meal",
                    name = "Cooked Meal",
                    ingredients = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { itemId = "raw_food", quantity = 2 },
                        new CraftingIngredient { itemId = "spice", quantity = 1 }
                    },
                    resultItemId = "cooked_meal",
                    resultQuantity = 1,
                    craftingTime = 2f, // in game minutes
                    requiredSkillId = "cooking",
                    requiredSkillLevel = 1
                });
            }
            
            private void RegisterRecipe(CraftingRecipe recipe)
            {
                _recipes[recipe.recipeId] = recipe;
            }
            
            public CraftingRecipe GetRecipe(string recipeId)
            {
                return _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
            }
            
            public List<CraftingRecipe> GetAllRecipes()
            {
                return _recipes.Values.ToList();
            }
            
            public List<CraftingRecipe> GetRecipesForItem(string resultItemId)
            {
                return _recipes.Values.Where(r => r.resultItemId == resultItemId).ToList();
            }
        }
        
        public class EquipmentManager
        {
            public void ApplyEquipmentEffects(Item item)
            {
                // In a real implementation, this would apply equipment effects to the player
                foreach (var effect in item.effects)
                {
                    Debug.Log($"Applying equipment effect {effect.effectName} to {effect.targetStat}: {effect.effectValue}");
                    
                    // Integration with other systems would happen here
                }
            }
            
            public void RemoveEquipmentEffects(Item item)
            {
                // In a real implementation, this would remove equipment effects from the player
                foreach (var effect in item.effects)
                {
                    Debug.Log($"Removing equipment effect {effect.effectName} from {effect.targetStat}");
                    
                    // Integration with other systems would happen here
                }
            }
        }
        
        [Serializable]
        public class CraftingRecipe
        {
            public string recipeId;
            public string name;
            public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
            public string resultItemId;
            public int resultQuantity;
            public float craftingTime;
            public string requiredSkillId;
            public int requiredSkillLevel;
        }
        
        [Serializable]
        public class CraftingIngredient
        {
            public string itemId;
            public int quantity;
        }
        
        [Serializable]
        public class ItemAcquisitionStats
        {
            public string itemId;
            public int totalAcquired;
            public int totalConsumed;
            public DateTime firstAcquiredTime;
            public DateTime lastAcquiredTime;
            public string mostCommonSource;
        }
        
        [Serializable]
        public class ItemReward
        {
            public string itemId;
            public int quantity;
        }

        public class RequiredItem
        {
            public string itemId;
            public int quantity;
        }
        #endregion
    }
}