using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ResourceManagement
{
    /// <summary>
    /// ResourceManagementSystem - Core system for managing player resources including currency, items, and time.
    /// Provides a comprehensive solution for resource acquisition, consumption, conversion, and storage.
    /// </summary>
    public class ResourceManagementSystem : MonoBehaviour
    {
        #region Singleton Implementation
        private static ResourceManagementSystem _instance;
        public static ResourceManagementSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ResourceManagementSystem>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("ResourceManagementSystem");
                        _instance = obj.AddComponent<ResourceManagementSystem>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            
            // Initialize the subsystems
            InitializeSubsystems();
        }
        #endregion

        #region Dependencies and Configuration
        [SerializeField] private ResourceSystemConfig systemConfig;
        [SerializeField] private LifeResourceSystem.EventBusReference eventBus;

        // Subsystem managers
        private CurrencyManager _currencyManager;
        private InventoryManager _inventoryManager;
        private TimeResourceManager _timeResourceManager;
        private ResourceConversionManager _conversionManager;
        private ResourceGenerationManager _generationManager;
        private ResourceStorageManager _storageManager;

        // Analytics and logging
        private ResourceLogger _resourceLogger;

        // Event subscriptions
        private List<Action<ResourceEvent>> _eventSubscribers = new List<Action<ResourceEvent>>();
        #endregion

        #region Initialization
        private void InitializeSubsystems()
        {
            // Initialize the logger first for debugging
            _resourceLogger = new ResourceLogger();
            
            // Initialize subsystems with configuration
            _currencyManager = new CurrencyManager(systemConfig.currencyConfig);
            _inventoryManager = new InventoryManager(systemConfig.inventoryConfig);
            _timeResourceManager = new TimeResourceManager(systemConfig.timeConfig);
            _conversionManager = new ResourceConversionManager(systemConfig.conversionConfig);
            _generationManager = new ResourceGenerationManager(systemConfig.generationConfig);
            _storageManager = new ResourceStorageManager(systemConfig.storageConfig);

            // Connect event handlers
            ConnectEventHandlers();
            
            // Register services with service locator
            RegisterServices();

            _resourceLogger.LogSystemMessage("Resource Management System initialized successfully");
        }

        private void ConnectEventHandlers()
        {
            // Currency events
            _currencyManager.OnCurrencyChanged += HandleCurrencyChanged;
            _currencyManager.OnTransactionProcessed += HandleTransactionProcessed;
            _currencyManager.OnCurrencyConverted += HandleCurrencyConverted;
            
            // Inventory events
            _inventoryManager.OnItemAdded += HandleItemAdded;
            _inventoryManager.OnItemRemoved += HandleItemRemoved;
            _inventoryManager.OnItemUsed += HandleItemUsed;
            _inventoryManager.OnItemCrafted += HandleItemCrafted;
            
            // Time resource events
            _timeResourceManager.OnTimeAdvanced += HandleTimeAdvanced;
            _timeResourceManager.OnActionScheduled += HandleActionScheduled;
            _timeResourceManager.OnActionCompleted += HandleActionCompleted;
            
            // Generation events
            _generationManager.OnResourceGenerated += HandleResourceGenerated;
            
            // Storage events
            _storageManager.OnResourceStored += HandleResourceStored;
            _storageManager.OnResourceDeteriorated += HandleResourceDeteriorated;
            
            // Conversion events
            _conversionManager.OnConversionProcessed += HandleConversionProcessed;
        }

        private void RegisterServices()
        {
            ServiceLocator.Register<ICurrencySystem>(_currencyManager);
            ServiceLocator.Register<IInventorySystem>(_inventoryManager);
            ServiceLocator.Register<ITimeResourceSystem>(_timeResourceManager);
            ServiceLocator.Register<IResourceConversionSystem>(_conversionManager);
            ServiceLocator.Register<IResourceGenerationSystem>(_generationManager);
            ServiceLocator.Register<IResourceStorageSystem>(_storageManager);
            ServiceLocator.Register<IResourceLogger>(_resourceLogger);
        }
        #endregion

        #region Public Interface
        
        /// <summary>
        /// Initializes the resource system with player identifier and optional custom configuration
        /// </summary>
        public void Initialize(string playerId, ResourceSystemConfig customConfig = null)
        {
            if (customConfig != null)
            {
                systemConfig = customConfig;
            }
            
            _currencyManager.Initialize(playerId);
            _inventoryManager.Initialize(playerId);
            _timeResourceManager.Initialize(playerId);
            _conversionManager.Initialize(playerId);
            _generationManager.Initialize(playerId);
            _storageManager.Initialize(playerId);
            
            _resourceLogger.LogSystemMessage($"Resource Management System initialized for player: {playerId}");
        }
        
        /// <summary>
        /// Subscribes to resource events
        /// </summary>
        public void SubscribeToResourceEvents(Action<ResourceEvent> callback)
        {
            if (callback != null && !_eventSubscribers.Contains(callback))
            {
                _eventSubscribers.Add(callback);
            }
        }
        
        /// <summary>
        /// Unsubscribes from resource events
        /// </summary>
        public void UnsubscribeFromResourceEvents(Action<ResourceEvent> callback)
        {
            if (callback != null)
            {
                _eventSubscribers.Remove(callback);
            }
        }
        
        /// <summary>
        /// Gets the complete resource state for the player
        /// </summary>
        public ResourceState GetResourceState()
        {
            return new ResourceState
            {
                currencies = _currencyManager.GetAllCurrencies(),
                inventory = _inventoryManager.GetInventoryState(),
                time = _timeResourceManager.GetTimeState(),
                economyStatus = _currencyManager.GetEconomyStatus(),
                generationSources = _generationManager.GetActiveGenerationSources(),
                storageStatus = _storageManager.GetStorageStatus()
            };
        }

        /// <summary>
        /// Generates save data for the resource system
        /// </summary>
        public ResourceSaveData GenerateSaveData()
        {
            _resourceLogger.LogSystemMessage("Generating resource system save data");
            
            ResourceSaveData saveData = new ResourceSaveData
            {
                currencyData = _currencyManager.GenerateSaveData(),
                inventoryData = _inventoryManager.GenerateSaveData(),
                timeData = _timeResourceManager.GenerateSaveData(),
                conversionData = _conversionManager.GenerateSaveData(),
                generationData = _generationManager.GenerateSaveData(),
                storageData = _storageManager.GenerateSaveData()
            };
            
            return saveData;
        }
        
        /// <summary>
        /// Restores resource system from save data
        /// </summary>
        public void RestoreFromSaveData(ResourceSaveData saveData)
        {
            _resourceLogger.LogSystemMessage("Restoring resource system from save data");
            
            _currencyManager.RestoreFromSaveData(saveData.currencyData);
            _inventoryManager.RestoreFromSaveData(saveData.inventoryData);
            _timeResourceManager.RestoreFromSaveData(saveData.timeData);
            _conversionManager.RestoreFromSaveData(saveData.conversionData);
            _generationManager.RestoreFromSaveData(saveData.generationData);
            _storageManager.RestoreFromSaveData(saveData.storageData);
        }
        
        /// <summary>
        /// Validates an action considering all resource requirements
        /// </summary>
        public ActionValidationResult ValidateAction(string actionId)
        {
            ActionValidationResult result = new ActionValidationResult { isValid = true };
            
            // Currency validation
            var currencyResult = _currencyManager.ValidateAction(actionId);
            if (!currencyResult.isValid)
            {
                result.isValid = false;
                result.failureReasons.Add(currencyResult.message);
            }
            
            // Item requirement validation
            var itemResult = _inventoryManager.ValidateAction(actionId);
            if (!itemResult.isValid)
            {
                result.isValid = false;
                result.failureReasons.Add(itemResult.message);
            }
            
            // Time validation
            var timeResult = _timeResourceManager.ValidateAction(actionId);
            if (!timeResult.isValid)
            {
                result.isValid = false;
                result.failureReasons.Add(timeResult.message);
            }
            
            // Storage validation (for actions producing items)
            var storageResult = _storageManager.ValidateAction(actionId);
            if (!storageResult.isValid)
            {
                result.isValid = false;
                result.failureReasons.Add(storageResult.message);
            }
            
            return result;
        }
        
        /// <summary>
        /// Processes and applies the effects of an action across all resource systems
        /// </summary>
        public ActionResult ProcessAction(string actionId, Dictionary<string, object> parameters = null)
        {
            _resourceLogger.LogSystemMessage($"Processing action: {actionId}");

            // Validate the action first
            var validation = ValidateAction(actionId);
            if (!validation.isValid)
            {
                _resourceLogger.LogWarning($"Action validation failed for {actionId}: {string.Join(", ", validation.failureReasons)}");
                return new ActionResult 
                { 
                    success = false, 
                    message = $"Cannot perform action: {string.Join(", ", validation.failureReasons)}" 
                };
            }
            
            // Begin transaction batch to ensure atomicity
            ResourceTransactionBatch batch = new ResourceTransactionBatch();
            
            try
            {
                // Process action components for each subsystem
                _currencyManager.ProcessAction(actionId, parameters, batch);
                _inventoryManager.ProcessAction(actionId, parameters, batch);
                _timeResourceManager.ProcessAction(actionId, parameters, batch);
                _generationManager.ProcessAction(actionId, parameters, batch);
                _storageManager.ProcessAction(actionId, parameters, batch);
                
                // Commit the transaction
                batch.Commit();
                
                // Publish comprehensive action event
                PublishActionCompletedEvent(actionId, parameters, batch.GetChanges());
                
                return new ActionResult { success = true };
            }
            catch (Exception ex)
            {
                // Rollback on failure
                batch.Rollback();
                _resourceLogger.LogError($"Action processing error for {actionId}: {ex.Message}");
                
                return new ActionResult 
                { 
                    success = false, 
                    message = $"Error processing action: {ex.Message}" 
                };
            }
        }
        
        /// <summary>
        /// Gets optimization suggestions for resource usage
        /// </summary>
        public List<ResourceOptimizationSuggestion> GetOptimizationSuggestions()
        {
            List<ResourceOptimizationSuggestion> suggestions = new List<ResourceOptimizationSuggestion>();
            
            // Get suggestions from each subsystem
            suggestions.AddRange(_currencyManager.GetOptimizationSuggestions());
            suggestions.AddRange(_inventoryManager.GetOptimizationSuggestions());
            suggestions.AddRange(_timeResourceManager.GetOptimizationSuggestions());
            suggestions.AddRange(_conversionManager.GetOptimizationSuggestions());
            suggestions.AddRange(_generationManager.GetOptimizationSuggestions());
            suggestions.AddRange(_storageManager.GetOptimizationSuggestions());
            
            // Sort suggestions by priority
            return suggestions.OrderByDescending(s => s.priority).ToList();
        }
        
        /// <summary>
        /// Updates resource analytics
        /// </summary>
        public void UpdateAnalytics()
        {
            _resourceLogger.UpdateResourceAnalytics(
                _currencyManager.GetAnalyticsData(),
                _inventoryManager.GetAnalyticsData(),
                _timeResourceManager.GetAnalyticsData()
            );
        }
        #endregion

        #region Event Handlers
        private void HandleCurrencyChanged(CurrencyType type, float newAmount, float delta)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.CurrencyChanged,
                parameters = new Dictionary<string, object>
                {
                    { "currencyType", type },
                    { "newAmount", newAmount },
                    { "delta", delta }
                }
            };
            
            PublishEvent(resourceEvent);
            
            // Check for critical resource levels
            if (_currencyManager.IsCurrencyCritical(type))
            {
                PublishCriticalResourceEvent("currency", type.ToString(), newAmount);
            }
        }

        private void HandleTransactionProcessed(FinancialTransaction transaction)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.TransactionProcessed,
                parameters = new Dictionary<string, object>
                {
                    { "transaction", transaction }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleCurrencyConverted(CurrencyType fromType, CurrencyType toType, float fromAmount, float toAmount)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.CurrencyConverted,
                parameters = new Dictionary<string, object>
                {
                    { "fromCurrency", fromType },
                    { "toCurrency", toType },
                    { "fromAmount", fromAmount },
                    { "toAmount", toAmount }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleItemAdded(Item item, int quantity, InventoryContainer container)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ItemAdded,
                parameters = new Dictionary<string, object>
                {
                    { "item", item },
                    { "quantity", quantity },
                    { "containerId", container.containerId }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleItemRemoved(Item item, int quantity, InventoryContainer container)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ItemRemoved,
                parameters = new Dictionary<string, object>
                {
                    { "item", item },
                    { "quantity", quantity },
                    { "containerId", container.containerId }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleItemUsed(Item item, InventoryContainer container)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ItemUsed,
                parameters = new Dictionary<string, object>
                {
                    { "item", item },
                    { "containerId", container.containerId }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleItemCrafted(Item item, int quantity, List<ItemConsumption> consumedItems)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ItemCrafted,
                parameters = new Dictionary<string, object>
                {
                    { "item", item },
                    { "quantity", quantity },
                    { "consumedItems", consumedItems }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleTimeAdvanced(float newTime, float deltaTime)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.TimeAdvanced,
                parameters = new Dictionary<string, object>
                {
                    { "newTime", newTime },
                    { "deltaTime", deltaTime }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleActionScheduled(ScheduledAction action)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ActionScheduled,
                parameters = new Dictionary<string, object>
                {
                    { "action", action }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleActionCompleted(ScheduledAction action)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ActionCompleted,
                parameters = new Dictionary<string, object>
                {
                    { "action", action }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleResourceGenerated(ResourceType resourceType, string resourceId, float amount)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ResourceGenerated,
                parameters = new Dictionary<string, object>
                {
                    { "resourceType", resourceType },
                    { "resourceId", resourceId },
                    { "amount", amount }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleResourceStored(ResourceType resourceType, string resourceId, float amount, float quality)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ResourceStored,
                parameters = new Dictionary<string, object>
                {
                    { "resourceType", resourceType },
                    { "resourceId", resourceId },
                    { "amount", amount },
                    { "quality", quality }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleResourceDeteriorated(ResourceType resourceType, string resourceId, float amount, float qualityLost)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ResourceDeteriorated,
                parameters = new Dictionary<string, object>
                {
                    { "resourceType", resourceType },
                    { "resourceId", resourceId },
                    { "amount", amount },
                    { "qualityLost", qualityLost }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void HandleConversionProcessed(ResourceConversion conversion)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ResourceConverted,
                parameters = new Dictionary<string, object>
                {
                    { "conversion", conversion }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void PublishCriticalResourceEvent(string resourceCategory, string resourceId, float currentAmount)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ResourceCritical,
                parameters = new Dictionary<string, object>
                {
                    { "resourceCategory", resourceCategory },
                    { "resourceId", resourceId },
                    { "currentAmount", currentAmount }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void PublishActionCompletedEvent(string actionId, Dictionary<string, object> parameters, List<ResourceChange> changes)
        {
            var resourceEvent = new ResourceEvent
            {
                type = ResourceEventType.ActionProcessed,
                parameters = new Dictionary<string, object>
                {
                    { "actionId", actionId },
                    { "actionParameters", parameters },
                    { "resourceChanges", changes }
                }
            };
            
            PublishEvent(resourceEvent);
        }

        private void PublishEvent(ResourceEvent resourceEvent)
        {
            // Log event
            _resourceLogger.LogResourceEvent(resourceEvent);
            
            // Notify subscribers
            foreach (var subscriber in _eventSubscribers)
            {
                try
                {
                    subscriber?.Invoke(resourceEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in event subscriber: {ex.Message}");
                }
            }
            
            // Publish to event bus if available
            if (eventBus != null)
            {
                eventBus.Publish(resourceEvent);
            }
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            // Disconnect event handlers to prevent memory leaks
            if (_currencyManager != null)
            {
                _currencyManager.OnCurrencyChanged -= HandleCurrencyChanged;
                _currencyManager.OnTransactionProcessed -= HandleTransactionProcessed;
                _currencyManager.OnCurrencyConverted -= HandleCurrencyConverted;
            }
            
            if (_inventoryManager != null)
            {
                _inventoryManager.OnItemAdded -= HandleItemAdded;
                _inventoryManager.OnItemRemoved -= HandleItemRemoved;
                _inventoryManager.OnItemUsed -= HandleItemUsed;
                _inventoryManager.OnItemCrafted -= HandleItemCrafted;
            }
            
            if (_timeResourceManager != null)
            {
                _timeResourceManager.OnTimeAdvanced -= HandleTimeAdvanced;
                _timeResourceManager.OnActionScheduled -= HandleActionScheduled;
                _timeResourceManager.OnActionCompleted -= HandleActionCompleted;
            }
            
            if (_generationManager != null)
            {
                _generationManager.OnResourceGenerated -= HandleResourceGenerated;
            }
            
            if (_storageManager != null)
            {
                _storageManager.OnResourceStored -= HandleResourceStored;
                _storageManager.OnResourceDeteriorated -= HandleResourceDeteriorated;
            }
            
            if (_conversionManager != null)
            {
                _conversionManager.OnConversionProcessed -= HandleConversionProcessed;
            }
            
            // Clear service locator registrations
            ServiceLocator.ClearCategory("ResourceManagement");
            
            _resourceLogger.LogSystemMessage("Resource Management System destroyed");
        }
        #endregion

        #region Debug Methods
        [ContextMenu("Debug: Log System Status")]
        public void DebugLogSystemStatus()
        {
            var state = GetResourceState();
            Debug.Log($"=== Resource System Status ===");
            
            // Log currency info
            Debug.Log($"Currencies: {state.currencies.Count}");
            foreach (var currency in state.currencies)
            {
                Debug.Log($"  {currency.name}: {currency.currentAmount}/{currency.maxCapacity}");
            }
            
            // Log inventory info
            Debug.Log($"Inventory Containers: {state.inventory.containers.Count}");
            foreach (var container in state.inventory.containers)
            {
                int itemCount = container.slots.Count(s => s.item != null);
                Debug.Log($"  {container.name}: {itemCount}/{container.maxSlots} slots used, {container.currentWeight}/{container.maxWeight} weight");
            }
            
            // Log time info
            Debug.Log($"Time - Day: {state.time.currentDay}, Hour: {state.time.currentHour}");
            Debug.Log($"  Scheduled Actions: {state.time.scheduledActions.Count}");
            
            // Log generation sources
            Debug.Log($"Active Generation Sources: {state.generationSources.Count}");
            
            // Log storage status
            Debug.Log($"Storage Status - Capacity used: {state.storageStatus.capacityUsedPercentage}%");
            Debug.Log($"  Items at risk: {state.storageStatus.itemsAtRisk}");
        }
        #endregion
    }

    #region System Interfaces
    
    /// <summary>
    /// Interface for currency management system
    /// </summary>
    public interface ICurrencySystem
    {
        void Initialize(string playerId);
        bool AddCurrency(CurrencyType type, float amount, string source, string description);
        bool RemoveCurrency(CurrencyType type, float amount, string destination, string description);
        bool ConvertCurrency(CurrencyType fromType, CurrencyType toType, float amount);
        float GetCurrencyAmount(CurrencyType type);
        Currency GetCurrency(CurrencyType type);
        List<Currency> GetAllCurrencies();
        EconomyStatus GetEconomyStatus();
        ActionValidationResult ValidateAction(string actionId);
        void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch);
        List<ResourceOptimizationSuggestion> GetOptimizationSuggestions();
        bool IsCurrencyCritical(CurrencyType type);
        Dictionary<string, object> GetAnalyticsData();
        CurrencySaveData GenerateSaveData();
        void RestoreFromSaveData(CurrencySaveData saveData);
        
        // Events
        event Action<CurrencyType, float, float> OnCurrencyChanged;
        event Action<FinancialTransaction> OnTransactionProcessed;
        event Action<CurrencyType, CurrencyType, float, float> OnCurrencyConverted;
    }
    
    /// <summary>
    /// Interface for inventory management system
    /// </summary>
    public interface IInventorySystem
    {
        void Initialize(string playerId);
        bool AddItem(string itemId, int quantity, string containerId = "");
        bool RemoveItem(string itemId, int quantity, string containerId = "");
        bool UseItem(string itemId, string containerId = "");
        bool MoveItem(string itemId, string sourceContainerId, string targetContainerId, int quantity = 1);
        bool CraftItem(string recipeId, int quantity = 1);
        bool EquipItem(string itemId, EquipmentSlotType slotType);
        bool UnequipItem(EquipmentSlotType slotType);
        Item GetItem(string itemId);
        int GetItemQuantity(string itemId);
        InventoryContainer GetContainer(string containerId);
        InventoryState GetInventoryState();
        ActionValidationResult ValidateAction(string actionId);
        void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch);
        List<ResourceOptimizationSuggestion> GetOptimizationSuggestions();
        Dictionary<string, object> GetAnalyticsData();
        InventorySaveData GenerateSaveData();
        void RestoreFromSaveData(InventorySaveData saveData);
        
        // Events
        event Action<Item, int, InventoryContainer> OnItemAdded;
        event Action<Item, int, InventoryContainer> OnItemRemoved;
        event Action<Item, InventoryContainer> OnItemUsed;
        event Action<Item, int, List<ItemConsumption>> OnItemCrafted;
        event Action<Item, EquipmentSlotType> OnItemEquipped;
        event Action<EquipmentSlotType> OnItemUnequipped;
    }
    
    /// <summary>
    /// Interface for time resource management system
    /// </summary>
    public interface ITimeResourceSystem
    {
        void Initialize(string playerId);
        void AdvanceTime(float hours);
        bool ScheduleAction(string actionId, float startTime, Dictionary<string, object> parameters = null);
        bool CancelAction(string actionId);
        float GetCurrentTime();
        TimeState GetTimeState();
        bool HasTimeForAction(string actionId);
        float GetActionTimeCost(string actionId);
        List<ScheduledAction> GetScheduledActions();
        ActionValidationResult ValidateAction(string actionId);
        void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch);
        List<ResourceOptimizationSuggestion> GetOptimizationSuggestions();
        Dictionary<string, object> GetAnalyticsData();
        TimeResourceSaveData GenerateSaveData();
        void RestoreFromSaveData(TimeResourceSaveData saveData);
        
        // Events
        event Action<float, float> OnTimeAdvanced;
        event Action<ScheduledAction> OnActionScheduled;
        event Action<ScheduledAction> OnActionCompleted;
        event Action<int, DayOfWeek> OnDayChanged;
    }
    
    /// <summary>
    /// Interface for resource conversion system
    /// </summary>
    public interface IResourceConversionSystem
    {
        void Initialize(string playerId);
        bool ConvertResource(ResourceType fromType, string fromId, ResourceType toType, string toId, float amount);
        float GetConversionRate(ResourceType fromType, string fromId, ResourceType toType, string toId);
        List<ResourceConversionOption> GetConversionOptions(ResourceType fromType, string fromId);
        bool IsConversionPossible(ResourceType fromType, string fromId, ResourceType toType, string toId);
        ActionValidationResult ValidateAction(string actionId);
        void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch);
        List<ResourceOptimizationSuggestion> GetOptimizationSuggestions();
        ConversionSaveData GenerateSaveData();
        void RestoreFromSaveData(ConversionSaveData saveData);
        
        // Events
        event Action<ResourceConversion> OnConversionProcessed;
    }
    
    /// <summary>
    /// Interface for resource generation system
    /// </summary>
    public interface IResourceGenerationSystem
    {
        void Initialize(string playerId);
        void AddGenerationSource(GenerationSource source);
        void RemoveGenerationSource(string sourceId);
        void UpdateGenerationSource(string sourceId, float efficiency);
        List<GenerationSource> GetActiveGenerationSources();
        float GetGenerationRate(ResourceType resourceType, string resourceId);
        void ProcessGeneration(float deltaTime);
        ActionValidationResult ValidateAction(string actionId);
        void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch);
        List<ResourceOptimizationSuggestion> GetOptimizationSuggestions();
        GenerationSaveData GenerateSaveData();
        void RestoreFromSaveData(GenerationSaveData saveData);
        
        // Events
        event Action<ResourceType, string, float> OnResourceGenerated;
    }
    
    /// <summary>
    /// Interface for resource storage system
    /// </summary>
    public interface IResourceStorageSystem
    {
        void Initialize(string playerId);
        bool StoreResource(ResourceType resourceType, string resourceId, float amount, float quality = 1.0f);
        bool RetrieveResource(ResourceType resourceType, string resourceId, float amount);
        float GetStoredAmount(ResourceType resourceType, string resourceId);
        float GetStoredQuality(ResourceType resourceType, string resourceId);
        List<StoredResource> GetAllStoredResources();
        StorageStatus GetStorageStatus();
        void ProcessDegradation(float deltaTime);
        ActionValidationResult ValidateAction(string actionId);
        void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch);
        List<ResourceOptimizationSuggestion> GetOptimizationSuggestions();
        StorageSaveData GenerateSaveData();
        void RestoreFromSaveData(StorageSaveData saveData);
        
        // Events
        event Action<ResourceType, string, float, float> OnResourceStored;
        event Action<ResourceType, string, float, float> OnResourceDeteriorated;
    }
    
    /// <summary>
    /// Interface for resource logging
    /// </summary>
    public interface IResourceLogger
    {
        void LogResourceEvent(ResourceEvent resourceEvent);
        void LogSystemMessage(string message);
        void LogWarning(string message);
        void LogError(string message);
        void UpdateResourceAnalytics(Dictionary<string, object> currencyData, Dictionary<string, object> inventoryData, Dictionary<string, object> timeData);
        ResourceAnalytics GetResourceAnalytics();
    }
    #endregion

    #region Data Structures
    
    /// <summary>
    /// Resource event definition
    /// </summary>
    public class ResourceEvent
    {
        public ResourceEventType type;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
        public DateTime timestamp = DateTime.Now;
    }
    
    /// <summary>
    /// Resource event types
    /// </summary>
    public enum ResourceEventType
    {
        CurrencyChanged,
        TransactionProcessed,
        CurrencyConverted,
        ItemAdded,
        ItemRemoved,
        ItemUsed,
        ItemCrafted,
        ItemEquipped,
        ItemUnequipped,
        TimeAdvanced,
        ActionScheduled,
        ActionCompleted,
        DayChanged,
        ResourceGenerated,
        ResourceStored,
        ResourceDeteriorated,
        ResourceConverted,
        ResourceCritical,
        ResourceRestored,
        ActionProcessed
    }
    
    /// <summary>
    /// Resource type enumeration
    /// </summary>
    public enum ResourceType
    {
        Currency,
        Item,
        Time,
        Energy,
        Reputation
    }
    
    /// <summary>
    /// Currency types
    /// </summary>
    public enum CurrencyType
    {
        StandardCurrency,
        PremiumCurrency,
        ExperiencePoints,
        SkillPoints,
        FactionCurrency,
        ReputationPoints,
        TradeCredits,
        SeasonalCurrency
    }
    
    /// <summary>
    /// Day of week enumeration
    /// </summary>
    public enum DayOfWeek
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }
    
    /// <summary>
    /// Equipment slot types
    /// </summary>
    public enum EquipmentSlotType
    {
        Head,
        Body,
        Legs,
        Feet,
        Hands,
        MainHand,
        OffHand,
        Accessory1,
        Accessory2,
        Tool,
        Special
    }
    
    /// <summary>
    /// Transaction type enumeration
    /// </summary>
    public enum TransactionType
    {
        Income,
        Expense,
        Investment,
        Loan,
        Conversion,
        Trade,
        Gift,
        Tax,
        Fine,
        Reward
    }
    
    /// <summary>
    /// Transaction category enumeration
    /// </summary>
    public enum TransactionCategory
    {
        Shopping,
        Services,
        Housing,
        Transportation,
        Entertainment,
        Food,
        Education,
        Healthcare,
        Investment,
        Quest,
        Trading,
        Crafting,
        Maintenance,
        Taxes,
        Other
    }
    
    /// <summary>
    /// Item type enumeration
    /// </summary>
    public enum ItemType
    {
        Consumable,
        Equipment,
        Material,
        Quest,
        Collectible,
        Currency,
        Tool,
        Container,
        Vehicle,
        Augmentation,
        Special
    }
    
    /// <summary>
    /// Item category enumeration
    /// </summary>
    public enum ItemCategory
    {
        Weapon,
        Armor,
        Accessory,
        Potion,
        Food,
        Crafting,
        Reagent,
        Document,
        Key,
        Treasure,
        Decoration,
        Technology,
        Special
    }
    
    /// <summary>
    /// Action status enumeration
    /// </summary>
    public enum ActionStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
    
    /// <summary>
    /// Currency class definition
    /// </summary>
    [Serializable]
    public class Currency
    {
        public string currencyId;
        public string name;
        public CurrencyType type;
        public float currentAmount;
        public float maxCapacity;
        public CurrencyProperties properties;
        public List<CurrencyModifier> activeModifiers = new List<CurrencyModifier>();
    }
    
    /// <summary>
    /// Currency properties class
    /// </summary>
    [Serializable]
    public class CurrencyProperties
    {
        public bool isMainCurrency;
        public bool isTradeableBetweenPlayers;
        public bool deterioratesOverTime;
        public float deteriorationRate;
        public bool canBeNegative;
        public float interestRate;
        public Sprite icon;
        public string description;
    }
    
    /// <summary>
    /// Currency modifier class
    /// </summary>
    [Serializable]
    public class CurrencyModifier
    {
        public string modifierId;
        public string source;
        public float flatBonus;
        public float percentageBonus;
        public float duration;
        public float remainingTime;
    }
    
    /// <summary>
    /// Financial transaction class
    /// </summary>
    [Serializable]
    public class FinancialTransaction
    {
        public string transactionId;
        public DateTime timestamp;
        public TransactionType type;
        public Dictionary<CurrencyType, float> amountsChanged = new Dictionary<CurrencyType, float>();
        public string description;
        public string sourceId;
        public TransactionCategory category;
    }
    
    /// <summary>
    /// Economy status class
    /// </summary>
    [Serializable]
    public class EconomyStatus
    {
        public Dictionary<CurrencyType, float> currencyInflationRates = new Dictionary<CurrencyType, float>();
        public Dictionary<string, float> marketPrices = new Dictionary<string, float>();
        public float economicStability;
        public float averageTransactionVolume;
        public bool isRecession;
    }
    
    /// <summary>
    /// Investment option class
    /// </summary>
    [Serializable]
    public class InvestmentOption
    {
        public string investmentId;
        public string name;
        public float minInvestmentAmount;
        public float baseReturnRate;
        public float riskFactor;
        public int durationDays;
        public List<InvestmentOutcome> possibleOutcomes = new List<InvestmentOutcome>();
    }
    
    /// <summary>
    /// Investment outcome class
    /// </summary>
    [Serializable]
    public class InvestmentOutcome
    {
        public string outcomeId;
        public string description;
        public float returnMultiplier;
        public float probability;
    }
    
    /// <summary>
    /// Item class definition
    /// </summary>
    [Serializable]
    public class Item
    {
        public string itemId;
        public string uniqueInstanceId;
        public string name;
        public ItemType type;
        public ItemCategory category;
        public ItemProperties properties;
        public ItemState currentState;
        public List<ItemEffect> effects = new List<ItemEffect>();
        public List<ItemRequirement> requirements = new List<ItemRequirement>();
        public List<string> tags = new List<string>();
    }
    
    /// <summary>
    /// Item properties class
    /// </summary>
    [Serializable]
    public class ItemProperties
    {
        public float baseValue;
        public float weight;
        public bool isStackable;
        public int maxStackSize;
        public bool isConsumable;
        public bool isEquippable;
        public EquipmentSlotType equipSlot;
        public Sprite icon;
        public GameObject prefab;
        public string description;
        public int tier;
        public bool isQuestItem;
    }
    
    /// <summary>
    /// Item state class
    /// </summary>
    [Serializable]
    public class ItemState
    {
        public float durability;
        public float quality;
        public float chargesLeft;
        public DateTime expirationDate;
        public List<ItemStateModifier> activeModifiers = new List<ItemStateModifier>();
    }
    
    /// <summary>
    /// Item state modifier class
    /// </summary>
    [Serializable]
    public class ItemStateModifier
    {
        public string modifierId;
        public string source;
        public float durabilityModifier;
        public float qualityModifier;
        public float duration;
        public float remainingTime;
    }
    
    /// <summary>
    /// Item effect class
    /// </summary>
    [Serializable]
    public class ItemEffect
    {
        public string effectId;
        public string effectName;
        public string targetStat;
        public float effectValue;
        public float duration;
        public bool isStackable;
        public int maxStacks;
    }
    
    /// <summary>
    /// Item requirement class
    /// </summary>
    [Serializable]
    public class ItemRequirement
    {
        public string requirementId;
        public string requirementType;
        public string targetStat;
        public float requiredValue;
        public string failureMessage;
    }
    
    /// <summary>
    /// Item consumption record
    /// </summary>
    [Serializable]
    public class ItemConsumption
    {
        public string itemId;
        public int quantity;
        public float qualityFactor;
    }
    
    /// <summary>
    /// Inventory container class
    /// </summary>
    [Serializable]
    public class InventoryContainer
    {
        public string containerId;
        public string name;
        public float maxWeight;
        public int maxSlots;
        public List<InventorySlot> slots = new List<InventorySlot>();
        public ContainerAccessRestrictions accessRestrictions;
        public ContainerUISettings uiSettings;
        
        public float currentWeight;
        public int usedSlots;
    }
    
    /// <summary>
    /// Inventory slot class
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public int slotIndex;
        public Item item;
        public int stackSize;
        public SlotRestrictions restrictions;
        public bool isLocked;
    }
    
    /// <summary>
    /// Slot restrictions class
    /// </summary>
    [Serializable]
    public class SlotRestrictions
    {
        public List<ItemType> allowedItemTypes = new List<ItemType>();
        public List<ItemCategory> allowedItemCategories = new List<ItemCategory>();
        public List<string> allowedItemTags = new List<string>();
        public float maxItemWeight;
        public int maxItemTier;
    }
    
    /// <summary>
    /// Container access restrictions class
    /// </summary>
    [Serializable]
    public class ContainerAccessRestrictions
    {
        public bool isLocked;
        public string requiredKeyId;
        public int requiredLockpickingSkill;
        public bool requiresPermission;
        public List<string> allowedPlayerIds = new List<string>();
        public List<string> allowedFactionIds = new List<string>();
    }
    
    /// <summary>
    /// Container UI settings class
    /// </summary>
    [Serializable]
    public class ContainerUISettings
    {
        public string displayName;
        public Sprite containerIcon;
        public Color containerColor = Color.white;
        public bool isVisible = true;
        public int displayOrder;
        public bool showInHUD;
    }
    
    /// <summary>
    /// Scheduled action class
    /// </summary>
    [Serializable]
    public class ScheduledAction
    {
        public string actionId;
        public float startTime;
        public float estimatedEndTime;
        public ActionStatus status;
        public float progressPercentage;
        public float timeRemaining;
        public List<string> prerequisites = new List<string>();
        public List<string> blockedActions = new List<string>();
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Time requirement class
    /// </summary>
    [Serializable]
    public class TimeRequirement
    {
        public float baseTimeCost;
        public bool isScalableBySkill;
        public Dictionary<string, float> skillEfficiencyFactors = new Dictionary<string, float>();
        public List<TimeModifier> possibleModifiers = new List<TimeModifier>();
        public bool requiresContinuousTime;
        public bool canBeInterrupted;
    }
    
    /// <summary>
    /// Time modifier class
    /// </summary>
    [Serializable]
    public class TimeModifier
    {
        public string modifierId;
        public string source;
        public float flatBonus;
        public float percentageBonus;
        public float duration;
        public float remainingTime;
    }
    
    /// <summary>
    /// Resource conversion class
    /// </summary>
    [Serializable]
    public class ResourceConversion
    {
        public string conversionId;
        public ResourceType fromType;
        public string fromId;
        public float fromAmount;
        public ResourceType toType;
        public string toId;
        public float toAmount;
        public float conversionEfficiency;
        public DateTime timestamp;
    }
    
    /// <summary>
    /// Resource conversion option class
    /// </summary>
    [Serializable]
    public class ResourceConversionOption
    {
        public string optionId;
        public ResourceType toType;
        public string toId;
        public float baseConversionRate;
        public float minConversionAmount;
        public float maxConversionAmount;
        public float conversionFeePercentage;
        public List<string> requirements = new List<string>();
    }
    
    /// <summary>
    /// Generation source class
    /// </summary>
    [Serializable]
    public class GenerationSource
    {
        public string sourceId;
        public string sourceName;
        public ResourceType resourceType;
        public string resourceId;
        public float baseGenerationRate;
        public float currentEfficiency;
        public bool isActive;
        public float cooldownTime;
        public float remainingCooldown;
        public GenerationSchedule schedule;
        public List<GenerationUpgrade> availableUpgrades = new List<GenerationUpgrade>();
        public int currentUpgradeLevel;
    }
    
    /// <summary>
    /// Generation schedule class
    /// </summary>
    [Serializable]
    public class GenerationSchedule
    {
        public bool hasSchedule;
        public List<DayOfWeek> activeDays = new List<DayOfWeek>();
        public float startHour;
        public float endHour;
        public bool isRecurring;
        public int totalCycles;
        public int completedCycles;
    }
    
    /// <summary>
    /// Generation upgrade class
    /// </summary>
    [Serializable]
    public class GenerationUpgrade
    {
        public string upgradeId;
        public string upgradeName;
        public int level;
        public float efficiencyBonus;
        public float capacityBonus;
        public Dictionary<string, float> resourceCosts = new Dictionary<string, float>();
        public List<string> requirements = new List<string>();
    }
    
    /// <summary>
    /// Stored resource class
    /// </summary>
    [Serializable]
    public class StoredResource
    {
        public string storageId;
        public ResourceType resourceType;
        public string resourceId;
        public float amount;
        public float quality;
        public float deteriorationRate;
        public DateTime storageDate;
        public DateTime expirationDate;
        public StorageMethod storageMethod;
    }
    
    /// <summary>
    /// Storage method class
    /// </summary>
    [Serializable]
    public class StorageMethod
    {
        public string methodId;
        public string methodName;
        public float deteriorationMultiplier;
        public float capacityMultiplier;
        public float energyCost;
        public float maintenanceCost;
    }
    
    /// <summary>
    /// Storage status class
    /// </summary>
    [Serializable]
    public class StorageStatus
    {
        public float totalCapacity;
        public float usedCapacity;
        public float capacityUsedPercentage;
        public int totalStoredItems;
        public int itemsAtRisk;
        public float averageItemQuality;
        public Dictionary<string, float> resourceBreakdown = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// Resource state structure for player
    /// </summary>
    [Serializable]
    public class ResourceState
    {
        public List<Currency> currencies = new List<Currency>();
        public InventoryState inventory;
        public TimeState time;
        public EconomyStatus economyStatus;
        public List<GenerationSource> generationSources = new List<GenerationSource>();
        public StorageStatus storageStatus;
    }
    
    /// <summary>
    /// Inventory state structure
    /// </summary>
    [Serializable]
    public class InventoryState
    {
        public List<InventoryContainer> containers = new List<InventoryContainer>();
        public Dictionary<EquipmentSlotType, Item> equippedItems = new Dictionary<EquipmentSlotType, Item>();
        public float totalWeight;
        public int totalItems;
        public float inventoryValue;
    }
    
    /// <summary>
    /// Time state structure
    /// </summary>
    [Serializable]
    public class TimeState
    {
        public int currentDay;
        public float currentHour;
        public DayOfWeek currentDayOfWeek;
        public int currentWeek;
        public int currentMonth;
        public int currentYear;
        public List<ScheduledAction> scheduledActions = new List<ScheduledAction>();
        public Dictionary<string, float> actionTimeEfficiency = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// Action validation result
    /// </summary>
    [Serializable]
    public class ActionValidationResult
    {
        public bool isValid = true;
        public string message = "";
        public List<string> failureReasons = new List<string>();
    }
    
    /// <summary>
    /// Action result
    /// </summary>
    [Serializable]
    public class ActionResult
    {
        public bool success;
        public string message = "";
        public Dictionary<string, object> resultData = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Resource change record
    /// </summary>
    [Serializable]
    public class ResourceChange
    {
        public ResourceType resourceType;
        public string resourceId;
        public float amount;
        public string source;
        public float qualityChange;
    }
    
    /// <summary>
    /// Resource optimization suggestion
    /// </summary>
    [Serializable]
    public class ResourceOptimizationSuggestion
    {
        public string suggestionId;
        public string title;
        public string description;
        public float potentialBenefit;
        public ResourceType primaryResourceType;
        public int priority;
        public List<string> actionSteps = new List<string>();
    }
    
    /// <summary>
    /// Resource analytics structure
    /// </summary>
    [Serializable]
    public class ResourceAnalytics
    {
        public Dictionary<string, List<float>> resourceHistory = new Dictionary<string, List<float>>();
        public Dictionary<string, float> acquisitionRates = new Dictionary<string, float>();
        public Dictionary<string, float> consumptionRates = new Dictionary<string, float>();
        public Dictionary<string, float> efficiencyMetrics = new Dictionary<string, float>();
        public List<ResourceTrend> detectedTrends = new List<ResourceTrend>();
    }
    
    /// <summary>
    /// Resource trend structure
    /// </summary>
    [Serializable]
    public class ResourceTrend
    {
        public string trendId;
        public string description;
        public float slope;
        public bool isPositive;
        public DateTime detectionTime;
        public float confidence;
    }
    
    /// <summary>
    /// Resource transaction batch for atomic operations
    /// </summary>
    public class ResourceTransactionBatch
    {
        private List<ResourceChange> changes = new List<ResourceChange>();
        private List<Action> rollbackActions = new List<Action>();
        private bool isCommitted = false;
        
        public void AddChange(ResourceChange change, Action rollbackAction)
        {
            if (!isCommitted)
            {
                changes.Add(change);
                rollbackActions.Add(rollbackAction);
            }
        }
        
        public void Commit()
        {
            isCommitted = true;
        }
        
        public void Rollback()
        {
            if (!isCommitted)
            {
                for (int i = rollbackActions.Count - 1; i >= 0; i--)
                {
                    rollbackActions[i]?.Invoke();
                }
            }
        }
        
        public List<ResourceChange> GetChanges()
        {
            return new List<ResourceChange>(changes);
        }
    }
    #endregion

    #region Save Data Structures
    
    /// <summary>
    /// Complete resource system save data
    /// </summary>
    [Serializable]
    public class ResourceSaveData
    {
        public CurrencySaveData currencyData;
        public InventorySaveData inventoryData;
        public TimeResourceSaveData timeData;
        public ConversionSaveData conversionData;
        public GenerationSaveData generationData;
        public StorageSaveData storageData;
    }
    
    /// <summary>
    /// Currency system save data
    /// </summary>
    [Serializable]
    public class CurrencySaveData
    {
        public string playerId;
        public List<SerializedCurrency> currencies = new List<SerializedCurrency>();
        public List<FinancialTransaction> recentTransactions = new List<FinancialTransaction>();
        public EconomyStatus economyStatus;
        
        [Serializable]
        public class SerializedCurrency
        {
            public string currencyId;
            public CurrencyType type;
            public float currentAmount;
            public List<CurrencyModifier> activeModifiers = new List<CurrencyModifier>();
        }
    }
    
    /// <summary>
    /// Inventory system save data
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public string playerId;
        public List<SerializedContainer> containers = new List<SerializedContainer>();
        public List<SerializedEquippedItem> equippedItems = new List<SerializedEquippedItem>();
        
        [Serializable]
        public class SerializedContainer
        {
            public string containerId;
            public List<SerializedSlot> slots = new List<SerializedSlot>();
        }
        
        [Serializable]
        public class SerializedSlot
        {
            public int slotIndex;
            public string itemId;
            public string uniqueInstanceId;
            public int stackSize;
            public ItemState itemState;
        }
        
        [Serializable]
        public class SerializedEquippedItem
        {
            public int slotType;
            public string itemId;
            public string uniqueInstanceId;
            public ItemState itemState;
        }
    }
    
    /// <summary>
    /// Time resource system save data
    /// </summary>
    [Serializable]
    public class TimeResourceSaveData
    {
        public string playerId;
        public int currentDay;
        public float currentHour;
        public DayOfWeek currentDayOfWeek;
        public int currentWeek;
        public int currentMonth;
        public int currentYear;
        public List<ScheduledAction> scheduledActions = new List<ScheduledAction>();
        public List<SerializedTimeModifier> activeModifiers = new List<SerializedTimeModifier>();
        
        [Serializable]
        public class SerializedTimeModifier
        {
            public string actionId;
            public TimeModifier modifier;
        }
    }
    
    /// <summary>
    /// Conversion system save data
    /// </summary>
    [Serializable]
    public class ConversionSaveData
    {
        public string playerId;
        public List<ResourceConversion> recentConversions = new List<ResourceConversion>();
        public Dictionary<string, float> conversionRateModifiers = new Dictionary<string, float>();
        public List<string> unlockedConversions = new List<string>();
    }
    
    /// <summary>
    /// Generation system save data
    /// </summary>
    [Serializable]
    public class GenerationSaveData
    {
        public string playerId;
        public List<GenerationSource> activeSources = new List<GenerationSource>();
        public Dictionary<string, float> resourcePoolAmounts = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// Storage system save data
    /// </summary>
    [Serializable]
    public class StorageSaveData
    {
        public string playerId;
        public List<StoredResource> storedResources = new List<StoredResource>();
        public List<StorageMethod> availableMethods = new List<StorageMethod>();
        public float totalCapacity;
    }
    #endregion

    #region Service Locator
    
    /// <summary>
    /// Service locator pattern implementation
    /// </summary>
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static Dictionary<string, List<Type>> categories = new Dictionary<string, List<Type>>();
        
        public static void Register<T>(T service)
        {
            var type = typeof(T);
            services[type] = service;
            
            // Add to ResourceManagement category by default
            string category = "ResourceManagement";
            if (!categories.ContainsKey(category))
            {
                categories[category] = new List<Type>();
            }
            if (!categories[category].Contains(type))
            {
                categories[category].Add(type);
            }
        }
        
        public static void Register<T>(T service, string category)
        {
            var type = typeof(T);
            services[type] = service;
            
            if (!categories.ContainsKey(category))
            {
                categories[category] = new List<Type>();
            }
            if (!categories[category].Contains(type))
            {
                categories[category].Add(type);
            }
        }
        
        public static T Get<T>()
        {
            if (services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            
            Debug.LogWarning($"Service of type {typeof(T)} not registered!");
            return default;
        }
        
        public static bool Exists<T>()
        {
            return services.ContainsKey(typeof(T));
        }
        
        public static void Clear()
        {
            services.Clear();
            categories.Clear();
        }
        
        public static void ClearCategory(string category)
        {
            if (categories.TryGetValue(category, out var types))
            {
                foreach (var type in types)
                {
                    services.Remove(type);
                }
                categories.Remove(category);
            }
        }
    }
    #endregion
}