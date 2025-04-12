using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LifeResourceSystem
{
    #region Core Data Structures

    #region Time Management
    [System.Serializable]
    public class TimeResourceSystem
    {
        public int currentDay;              // Current day
        public float currentHour;           // Current hour (24-hour format)
        public DayOfWeek currentDayOfWeek;  // Current day of week
        public int currentWeek;             // Current week
        public int currentMonth;            // Current month
        public int currentYear;             // Current year
        
        public enum DayOfWeek
        {
            Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday
        }
        
        [System.Serializable]
        public class TimeBlock
        {
            public string blockId;          // Block ID
            public string blockName;        // Block name (Morning, Afternoon, etc.)
            public float startHour;         // Start time
            public float endHour;           // End time
            public bool isAvailable;        // Is available
            public string allocatedActionId; // Allocated action ID
        }
        
        public List<TimeBlock> dailyTimeBlocks = new List<TimeBlock>(); // Daily time blocks
        public Dictionary<string, float> actionTimeCosts = new Dictionary<string, float>(); // Time cost per action
    }
    #endregion

    #region Energy Management
    [System.Serializable]
    public class EnergySystem
    {
        public string playerId;                   // Player ID
        public float maxEnergy;                   // Maximum energy
        public float currentEnergy;               // Current energy
        public float energyRecoveryRate;          // Recovery rate (per hour)
        
        public Dictionary<string, float> activityEnergyCosts = new Dictionary<string, float>(); // Energy cost per activity
        
        [System.Serializable]
        public class EnergyModifier
        {
            public string source;                 // Source of modifier
            public float recoveryRateModifier;    // Recovery rate modifier
            public float maxEnergyModifier;       // Maximum energy modifier
            public float duration;                // Duration (-1 is permanent)
            public float remainingTime;           // Time remaining
        }
        
        public List<EnergyModifier> activeModifiers = new List<EnergyModifier>(); // Active modifiers
        
        [System.Serializable]
        public class EnergyState
        {
            public string stateId;                // State ID (Fatigued, Energetic, etc.)
            public string stateName;              // State name
            public float minThreshold;            // Minimum threshold (%)
            public float maxThreshold;            // Maximum threshold (%)
            public List<StatEffect> effects = new List<StatEffect>();      // State effects
        }
        
        [System.Serializable]
        public class StatEffect
        {
            public string targetStat;             // Target statistic
            public float effectValue;             // Effect value
        }
        
        public List<EnergyState> possibleStates = new List<EnergyState>();  // Possible states list
        public string currentStateId;             // Current state ID
    }
    #endregion

    #region Finance Management
    [System.Serializable]
    public class FinanceSystem
    {
        public string playerId;                   // Player ID
        public float currentMoney;                // Current money
        public List<IncomeSource> incomeSources = new List<IncomeSource>();  // Income sources
        public List<RecurringExpense> expenses = new List<RecurringExpense>();   // Recurring expenses
        public List<Transaction> transactions = new List<Transaction>();    // Transaction history
        
        [System.Serializable]
        public class IncomeSource
        {
            public string sourceId;               // Income source ID
            public string sourceName;             // Income source name
            public float amount;                  // Amount
            public PaymentFrequency frequency;    // Payment frequency
            public float nextPaymentTime;         // Next payment time
            public bool isActive;                 // Is active
        }
        
        [System.Serializable]
        public class RecurringExpense
        {
            public string expenseId;              // Expense ID
            public string expenseName;            // Expense name
            public float amount;                  // Amount
            public PaymentFrequency frequency;    // Payment frequency
            public float nextPaymentTime;         // Next payment time
            public bool isOptional;               // Is optional (can be skipped)
            public List<string> consequences = new List<string>();     // Consequences if skipped
        }
        
        public enum PaymentFrequency
        {
            Daily,
            Weekly,
            BiWeekly,
            Monthly,
            Yearly,
            OneTime
        }
        
        [System.Serializable]
        public class Transaction
        {
            public string transactionId;          // Transaction ID
            public string description;            // Description
            public float amount;                  // Amount (negative for expenses)
            public string category;               // Category
            public float timestamp;               // Timestamp
            public string relatedEntityId;        // Related entity ID
        }
        
        // Calculated financial metrics
        public float dailyIncome;                 // Daily income
        public float dailyExpenses;               // Daily expenses
        public float monthlyBalance;              // Monthly balance
        public float financialStability;          // Financial stability (0-100)
    }
    #endregion

    #region Social Credit Management
    [System.Serializable]
    public class SocialCreditSystem
    {
        public string playerId;                      // Player ID
        public Dictionary<string, SocialCredit> socialCredits = new Dictionary<string, SocialCredit>(); // Social credits
        
        [System.Serializable]
        public class SocialCredit
        {
            public string contextId;                 // Context ID
            public string contextName;               // Context name
            public float creditScore;                // Credit score
            public SocialTier currentTier;           // Current tier
            public List<CreditEvent> recentEvents = new List<CreditEvent>();   // Recent events
        }
        
        [System.Serializable]
        public class SocialTier
        {
            public string tierId;                    // Tier ID
            public string tierName;                  // Tier name
            public float minThreshold;               // Minimum threshold
            public float maxThreshold;               // Maximum threshold
            public List<string> benefits = new List<string>();            // Benefits
            public List<string> restrictions = new List<string>();        // Restrictions
        }
        
        [System.Serializable]
        public class CreditEvent
        {
            public string eventId;                   // Event ID
            public string description;               // Description
            public float impact;                     // Impact value
            public float timestamp;                  // Timestamp
            public float decayTime;                  // Decay time
        }
    }
    #endregion

    #endregion

    #region System Interface

    // Unified Life Resource Management Interface
    public interface ILifeResourceSystem
    {
        // System initialization
        void Initialize(string playerId, LifeResourceConfig config);
        
        // Time advancement
        void AdvanceTime(float hoursDelta);
        
        // Energy consumption
        bool ConsumeEnergy(string activityId, float multiplier = 1.0f);
        
        // Money transaction
        bool ProcessTransaction(string description, float amount, string category);
        
        // Social credit update
        void UpdateSocialCredit(string contextId, float deltaValue, string description);
        
        // Resource state retrieval
        ResourceState GetResourceState();
        
        // Action feasibility check
        bool CanPerformAction(string actionId);
        
        // Time block allocation
        bool AllocateTimeBlock(string blockId, string actionId);
        
        // State save/load
        LifeResourceSaveData GenerateSaveData();
        void RestoreFromSaveData(LifeResourceSaveData saveData);
        
        // Event notification subscription
        void SubscribeToResourceEvents(Action<ResourceEvent> callback);
    }

    // Resource event definition
    [System.Serializable]
    public class ResourceEvent
    {
        public ResourceEventType type;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
        
        public enum ResourceEventType
        {
            TimeAdvanced,
            DayChanged,
            EnergyChanged,
            EnergyStateChanged,
            MoneyChanged,
            TransactionProcessed,
            SocialCreditChanged,
            ResourceCritical,
            ResourceRestored
            // Others
        }
    }

    // Integrated resource state structure
    [System.Serializable]
    public struct ResourceState
    {
        public TimeState time;
        public EnergyState energy;
        public FinancialState finance;
        public Dictionary<string, float> socialCredits;
    }

    [System.Serializable]
    public struct TimeState
    {
        public int day;
        public float hour;
        public TimeResourceSystem.DayOfWeek dayOfWeek;
        public int week;
        public int month;
        public int year;
        public List<TimeResourceSystem.TimeBlock> availableTimeBlocks;
    }

    [System.Serializable]
    public struct EnergyState
    {
        public float currentEnergy;
        public float maxEnergy;
        public float energyRatio;
        public string stateId;
        public string stateName;
        public List<string> stateEffects;
    }

    [System.Serializable]
    public struct FinancialState
    {
        public float currentMoney;
        public float dailyIncome;
        public float dailyExpenses;
        public float monthlyBalance;
        public float financialStability;
        public List<FinanceSystem.Transaction> recentTransactions;
    }
    #endregion

    #region Implementation (Unity)

    #region Component Structure
    // Integrated Resource Manager
    public class LifeResourceManager : MonoBehaviour, ILifeResourceSystem
    {
        [SerializeField] private LifeResourceConfig config;
        [SerializeField] private EventBusReference eventBus;
        
        private TimeManager timeManager;
        private EnergyManager energyManager;
        private FinanceManager financeManager;
        private SocialCreditManager socialCreditManager;
        
        private List<Action<ResourceEvent>> eventSubscribers = new List<Action<ResourceEvent>>();
        
        // Singleton instance
        private static LifeResourceManager _instance;
        public static LifeResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LifeResourceManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("LifeResourceManager");
                        _instance = obj.AddComponent<LifeResourceManager>();
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
        }
        
        // Initialization
        public void Initialize(string playerId, LifeResourceConfig config = null)
        {
            if (config != null) this.config = config;
            
            timeManager = new TimeManager(this.config.timeConfig);
            energyManager = new EnergyManager(playerId, this.config.energyConfig);
            financeManager = new FinanceManager(playerId, this.config.financeConfig);
            socialCreditManager = new SocialCreditManager(playerId, this.config.socialConfig);
            
            // Event connections
            timeManager.OnTimeAdvanced += OnTimeAdvanced;
            timeManager.OnDayChanged += OnDayChanged;
            energyManager.OnEnergyChanged += OnEnergyChanged;
            energyManager.OnEnergyStateChanged += OnEnergyStateChanged;
            financeManager.OnTransactionProcessed += OnTransactionProcessed;
            socialCreditManager.OnSocialCreditChanged += OnSocialCreditChanged;
        }
        
        private void OnDestroy()
        {
            if (timeManager != null)
            {
                timeManager.OnTimeAdvanced -= OnTimeAdvanced;
                timeManager.OnDayChanged -= OnDayChanged;
            }
            
            if (energyManager != null)
            {
                energyManager.OnEnergyChanged -= OnEnergyChanged;
                energyManager.OnEnergyStateChanged -= OnEnergyStateChanged;
            }
            
            if (financeManager != null)
            {
                financeManager.OnTransactionProcessed -= OnTransactionProcessed;
            }
            
            if (socialCreditManager != null)
            {
                socialCreditManager.OnSocialCreditChanged -= OnSocialCreditChanged;
            }
        }
        
        #region Event Handlers
        // Time advancement event handler
        private void OnTimeAdvanced(float newTime, float deltaTime)
        {
            // Energy recovery processing
            energyManager.RecoverEnergy(deltaTime);
            
            // Time-based transaction processing
            financeManager.ProcessTimeBasedTransactions(newTime);
            
            // Credit decay processing
            socialCreditManager.DecayCreditEvents(deltaTime);
            
            // Publish event
            var timeEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.TimeAdvanced,
                parameters = new Dictionary<string, object>
                {
                    { "newTime", newTime },
                    { "deltaTime", deltaTime }
                }
            };
            PublishEvent(timeEvent);
        }
        
        // Day change event handler
        private void OnDayChanged(int newDay, TimeResourceSystem.DayOfWeek newDayOfWeek)
        {
            // Reset time blocks
            timeManager.ResetTimeBlocks();
            
            // Daily energy processing
            energyManager.ProcessDayChange();
            
            // Daily financial processing
            financeManager.ProcessDailyFinancials();
            
            // Publish event
            var dayEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.DayChanged,
                parameters = new Dictionary<string, object>
                {
                    { "newDay", newDay },
                    { "dayOfWeek", newDayOfWeek }
                }
            };
            PublishEvent(dayEvent);
        }
        
        // Energy change event handler
        private void OnEnergyChanged(float newEnergy, float delta)
        {
            var energyEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.EnergyChanged,
                parameters = new Dictionary<string, object>
                {
                    { "newEnergy", newEnergy },
                    { "delta", delta },
                    { "ratio", energyManager.GetCurrentEnergyRatio() }
                }
            };
            PublishEvent(energyEvent);
            
            // Check for critical resources
            if (energyManager.GetCurrentEnergyRatio() < 0.1f)
            {
                var criticalEvent = new ResourceEvent
                {
                    type = ResourceEvent.ResourceEventType.ResourceCritical,
                    parameters = new Dictionary<string, object>
                    {
                        { "resourceType", "energy" },
                        { "currentValue", newEnergy },
                        { "maxValue", energyManager.GetMaxEnergy() }
                    }
                };
                PublishEvent(criticalEvent);
            }
        }
        
        // Energy state change event handler
        private void OnEnergyStateChanged(string newStateId)
        {
            var stateEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.EnergyStateChanged,
                parameters = new Dictionary<string, object>
                {
                    { "newStateId", newStateId },
                    { "stateName", energyManager.GetCurrentState().stateName }
                }
            };
            PublishEvent(stateEvent);
        }
        
        // Transaction processed event handler
        private void OnTransactionProcessed(FinanceSystem.Transaction transaction)
        {
            var moneyEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.MoneyChanged,
                parameters = new Dictionary<string, object>
                {
                    { "newBalance", financeManager.GetCurrentMoney() },
                    { "delta", transaction.amount }
                }
            };
            PublishEvent(moneyEvent);
            
            var transactionEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.TransactionProcessed,
                parameters = new Dictionary<string, object>
                {
                    { "transactionId", transaction.transactionId },
                    { "description", transaction.description },
                    { "amount", transaction.amount },
                    { "category", transaction.category }
                }
            };
            PublishEvent(transactionEvent);
            
            // Check for critical resources
            if (financeManager.GetCurrentMoney() < financeManager.GetDailyExpenses())
            {
                var criticalEvent = new ResourceEvent
                {
                    type = ResourceEvent.ResourceEventType.ResourceCritical,
                    parameters = new Dictionary<string, object>
                    {
                        { "resourceType", "money" },
                        { "currentValue", financeManager.GetCurrentMoney() },
                        { "requiredValue", financeManager.GetDailyExpenses() }
                    }
                };
                PublishEvent(criticalEvent);
            }
        }
        
        // Social credit change event handler
        private void OnSocialCreditChanged(string contextId, float newValue, float delta)
        {
            var creditEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.SocialCreditChanged,
                parameters = new Dictionary<string, object>
                {
                    { "contextId", contextId },
                    { "newValue", newValue },
                    { "delta", delta }
                }
            };
            PublishEvent(creditEvent);
        }
        
        // Event publishing helper
        private void PublishEvent(ResourceEvent resourceEvent)
        {
            // Notify internal subscribers
            foreach (var subscriber in eventSubscribers)
            {
                subscriber?.Invoke(resourceEvent);
            }
            
            // Publish to event bus if available
            if (eventBus != null)
            {
                eventBus.Publish(resourceEvent);
            }
        }
        #endregion
        
        #region Interface Implementation
        // Advance time
        public void AdvanceTime(float hoursDelta)
        {
            timeManager.AdvanceTime(hoursDelta);
        }
        
        // Consume energy
        public bool ConsumeEnergy(string activityId, float multiplier = 1.0f)
        {
            return energyManager.ConsumeEnergy(activityId, multiplier);
        }
        
        // Process transaction
        public bool ProcessTransaction(string description, float amount, string category)
        {
            string transactionId = $"txn_{DateTime.Now.Ticks}";
            return financeManager.ProcessTransaction(transactionId, description, amount, category);
        }
        
        // Update social credit
        public void UpdateSocialCredit(string contextId, float deltaValue, string description)
        {
            socialCreditManager.UpdateSocialCredit(contextId, deltaValue, description);
        }
        
        // Get resource state
        public ResourceState GetResourceState()
        {
            return new ResourceState
            {
                time = timeManager.GetTimeState(),
                energy = energyManager.GetEnergyState(),
                finance = financeManager.GetFinancialState(),
                socialCredits = socialCreditManager.GetAllCreditScores()
            };
        }
        
        // Check if action can be performed
        public bool CanPerformAction(string actionId)
        {
            // Time check
            if (!timeManager.HasTimeForAction(actionId))
                return false;
                
            // Energy check
            if (!energyManager.CanPerformAction(actionId))
                return false;
                
            // Money check (if the action has a cost)
            float actionCost = financeManager.GetActionCost(actionId);
            if (actionCost > 0 && !financeManager.HasEnoughMoney(actionCost))
                return false;
                
            // Social credit check (if applicable)
            if (!socialCreditManager.CanPerformAction(actionId))
                return false;
                
            return true;
        }
        
        // Allocate time block
        public bool AllocateTimeBlock(string blockId, string actionId)
        {
            return timeManager.AllocateTimeBlock(blockId, actionId);
        }
        
        // Generate save data
        public LifeResourceSaveData GenerateSaveData()
        {
            LifeResourceSaveData saveData = new LifeResourceSaveData
            {
                timeData = timeManager.GenerateSaveData(),
                energyData = energyManager.GenerateSaveData(),
                financeData = financeManager.GenerateSaveData(),
                socialData = socialCreditManager.GenerateSaveData()
            };
            
            return saveData;
        }
        
        // Restore from save data
        public void RestoreFromSaveData(LifeResourceSaveData saveData)
        {
            timeManager.RestoreFromSaveData(saveData.timeData);
            energyManager.RestoreFromSaveData(saveData.energyData);
            financeManager.RestoreFromSaveData(saveData.financeData);
            socialCreditManager.RestoreFromSaveData(saveData.socialData);
        }
        
        // Subscribe to resource events
        public void SubscribeToResourceEvents(Action<ResourceEvent> callback)
        {
            if (callback != null && !eventSubscribers.Contains(callback))
            {
                eventSubscribers.Add(callback);
            }
        }
        
        // Unsubscribe from resource events
        public void UnsubscribeFromResourceEvents(Action<ResourceEvent> callback)
        {
            if (callback != null)
            {
                eventSubscribers.Remove(callback);
            }
        }
        #endregion
    }
    #endregion

    #region Subsystem Example (Energy Manager)
    // Energy management subsystem
    public class EnergyManager
    {
        private EnergySystem energyData;
        private EnergyConfig config;
        
        // Events
        public event Action<float, float> OnEnergyChanged; // Current value, delta
        public event Action<string> OnEnergyStateChanged;  // New state ID
        
        private Dictionary<string, IEnergyState> states = new Dictionary<string, IEnergyState>();
        private IEnergyState currentState;
        
        public EnergyManager(string playerId, EnergyConfig config)
        {
            this.config = config;
            energyData = new EnergySystem
            {
                playerId = playerId,
                maxEnergy = config.baseMaxEnergy,
                currentEnergy = config.baseMaxEnergy,
                energyRecoveryRate = config.baseRecoveryRate,
                activityEnergyCosts = new Dictionary<string, float>(config.defaultEnergyCosts),
                activeModifiers = new List<EnergySystem.EnergyModifier>(),
                possibleStates = new List<EnergySystem.EnergyState>(config.defaultStates),
                currentStateId = config.defaultStateId
            };
            
            // Initialize states
            InitializeStates();
            
            // Set initial state
            SetState(config.defaultStateId);
        }
        
        private void InitializeStates()
        {
            // Register default states
            states.Add("normal", new NormalState());
            states.Add("energized", new EnergizedState());
            states.Add("fatigued", new FatiguedState());
            states.Add("exhausted", new ExhaustedState());
            
            // Register custom states from config
            foreach (var state in config.customStates)
            {
                if (!states.ContainsKey(state.stateId))
                {
                    states.Add(state.stateId, new CustomEnergyState(state));
                }
            }
        }
        
        private void SetState(string stateId)
        {
            if (!states.ContainsKey(stateId))
            {
                Debug.LogWarning($"Energy state {stateId} not found. Using 'normal' instead.");
                stateId = "normal";
            }
            
            // Exit current state if exists
            if (currentState != null)
            {
                currentState.OnExit(this);
            }
            
            // Set new state
            currentState = states[stateId];
            energyData.currentStateId = stateId;
            
            // Enter new state
            currentState.OnEnter(this);
            
            // Trigger event
            OnEnergyStateChanged?.Invoke(stateId);
        }
        
        // Energy consumption
        public bool ConsumeEnergy(string activityId, float multiplier = 1.0f)
        {
            if (!energyData.activityEnergyCosts.ContainsKey(activityId))
                return false;
                
            float cost = energyData.activityEnergyCosts[activityId] * multiplier;
            
            // Consumption check
            if (energyData.currentEnergy < cost)
                return false;
                
            // Apply state-specific modifiers
            if (currentState != null)
            {
                cost = currentState.ModifyEnergyCost(activityId, cost);
            }
            
            // Execute energy consumption
            float oldEnergy = energyData.currentEnergy;
            energyData.currentEnergy -= cost;
            
            // Update state
            UpdateEnergyState();
            
            // Trigger event
            OnEnergyChanged?.Invoke(energyData.currentEnergy, -cost);
            
            return true;
        }
        
        // Energy recovery
        public void RecoverEnergy(float hoursDelta)
        {
            // Calculate recovery rate (apply modifiers)
            float recoveryRate = energyData.energyRecoveryRate;
            foreach (var modifier in energyData.activeModifiers)
            {
                recoveryRate += modifier.recoveryRateModifier;
            }
            
            // State-specific recovery modification
            if (currentState != null)
            {
                recoveryRate = currentState.ModifyRecoveryRate(recoveryRate);
            }
            
            // Don't allow negative recovery rate
            recoveryRate = Mathf.Max(0, recoveryRate);
            
            // Calculate recovery amount
            float recovery = recoveryRate * hoursDelta;
            float oldEnergy = energyData.currentEnergy;
            
            // Calculate max energy (apply modifiers)
            float maxEnergy = energyData.maxEnergy;
            foreach (var modifier in energyData.activeModifiers)
            {
                maxEnergy += modifier.maxEnergyModifier;
            }
            
            // Apply recovery
            energyData.currentEnergy = Mathf.Min(energyData.currentEnergy + recovery, maxEnergy);
            
            // Update state
            UpdateEnergyState();
            
            // Only trigger event if there was a significant change
            if (Mathf.Abs(energyData.currentEnergy - oldEnergy) > 0.01f)
            {
                OnEnergyChanged?.Invoke(energyData.currentEnergy, energyData.currentEnergy - oldEnergy);
            }
        }
        
        // Update energy state
        private void UpdateEnergyState()
        {
            // Calculate energy ratio
            float energyRatio = GetCurrentEnergyRatio();
            
            // Find appropriate state
            string newStateId = energyData.currentStateId;
            
            foreach (var state in energyData.possibleStates)
            {
                if (energyRatio >= state.minThreshold && energyRatio <= state.maxThreshold)
                {
                    newStateId = state.stateId;
                    break;
                }
            }
            
            // Only change state if it's different
            if (newStateId != energyData.currentStateId)
            {
                SetState(newStateId);
            }
        }
        
        // Daily processing
        public void ProcessDayChange()
        {
            // Update modifier durations
            for (int i = energyData.activeModifiers.Count - 1; i >= 0; i--)
            {
                var modifier = energyData.activeModifiers[i];
                if (modifier.duration > 0)
                {
                    modifier.remainingTime -= 24; // Decrease by 24 hours
                    
                    // Remove expired modifiers
                    if (modifier.remainingTime <= 0)
                    {
                        energyData.activeModifiers.RemoveAt(i);
                    }
                }
            }
            
            // State-specific daily processing
            if (currentState != null)
            {
                currentState.OnDayChanged(this);
            }
            
            // Update state after modifier changes
            UpdateEnergyState();
        }
        
        // Helper methods
        public void AddModifier(EnergySystem.EnergyModifier modifier)
        {
            energyData.activeModifiers.Add(modifier);
            UpdateEnergyState(); // Modifiers might affect max energy
        }

        public EnergySystem.EnergyState GetCurrentState()
        {
            return energyData.possibleStates.Find(s => s.stateId == energyData.currentStateId);
        }

        public float GetCurrentEnergyRatio()
        {
            float maxEnergy = energyData.maxEnergy;
            foreach (var modifier in energyData.activeModifiers)
            {
                maxEnergy += modifier.maxEnergyModifier;
            }
            return energyData.currentEnergy / maxEnergy;
        }
        
        public float GetMaxEnergy()
        {
            float maxEnergy = energyData.maxEnergy;
            foreach (var modifier in energyData.activeModifiers)
            {
                maxEnergy += modifier.maxEnergyModifier;
            }
            return maxEnergy;
        }
        
        public EnergyState GetEnergyState()
        {
            var currentStateObj = GetCurrentState();
            List<string> effects = new List<string>();
            
            if (currentStateObj != null && currentStateObj.effects != null)
            {
                foreach (var effect in currentStateObj.effects)
                {
                    effects.Add($"{effect.targetStat}:{effect.effectValue}");
                }
            }
            
            return new EnergyState
            {
                currentEnergy = energyData.currentEnergy,
                maxEnergy = GetMaxEnergy(),
                energyRatio = GetCurrentEnergyRatio(),
                stateId = energyData.currentStateId,
                stateName = currentStateObj?.stateName ?? "Unknown",
                stateEffects = effects
            };
        }
        
        public bool CanPerformAction(string actionId)
        {
            // Check if we have enough energy
            if (!energyData.activityEnergyCosts.ContainsKey(actionId))
                return true; // No energy cost defined for this action
                
            float cost = energyData.activityEnergyCosts[actionId];
            
            if (energyData.currentEnergy < cost)
                return false;
                
            // Check state-specific restrictions
            if (currentState != null)
            {
                return currentState.CanPerformAction(actionId, this);
            }
            
            return true;
        }
        
        public EnergySaveData GenerateSaveData()
        {
            // Convert dictionaries to serializable lists
            List<EnergySaveData.ActivityCost> activityCosts = new List<EnergySaveData.ActivityCost>();
            foreach (var pair in energyData.activityEnergyCosts)
            {
                activityCosts.Add(new EnergySaveData.ActivityCost
                {
                    activityId = pair.Key,
                    cost = pair.Value
                });
            }
            
            return new EnergySaveData
            {
                playerId = energyData.playerId,
                maxEnergy = energyData.maxEnergy,
                currentEnergy = energyData.currentEnergy,
                recoveryRate = energyData.energyRecoveryRate,
                activityCosts = activityCosts,
                activeModifiers = new List<EnergySystem.EnergyModifier>(energyData.activeModifiers),
                currentStateId = energyData.currentStateId
            };
        }
        
        public void RestoreFromSaveData(EnergySaveData saveData)
        {
            energyData.playerId = saveData.playerId;
            energyData.maxEnergy = saveData.maxEnergy;
            energyData.currentEnergy = saveData.currentEnergy;
            energyData.energyRecoveryRate = saveData.recoveryRate;
            energyData.activeModifiers = new List<EnergySystem.EnergyModifier>(saveData.activeModifiers);
            
            // Restore activity costs
            energyData.activityEnergyCosts.Clear();
            foreach (var cost in saveData.activityCosts)
            {
                energyData.activityEnergyCosts[cost.activityId] = cost.cost;
            }
            
            // Restore state
            SetState(saveData.currentStateId);
        }
    }
    #endregion

    #region State Pattern (for Energy States)
    // Energy state interface
    public interface IEnergyState
    {
        string GetStateId();
        string GetStateName();
        void OnEnter(EnergyManager manager);
        void OnExit(EnergyManager manager);
        void OnDayChanged(EnergyManager manager);
        float ModifyEnergyCost(string actionId, float baseCost);
        float ModifyRecoveryRate(float baseRate);
        bool CanPerformAction(string actionId, EnergyManager manager);
    }

    // Normal energy state
    public class NormalState : IEnergyState
    {
        public string GetStateId() => "normal";
        public string GetStateName() => "Normal";
        
        public void OnEnter(EnergyManager manager) { }
        
        public void OnExit(EnergyManager manager) { }
        
        public void OnDayChanged(EnergyManager manager) { }
        
        public float ModifyEnergyCost(string actionId, float baseCost)
        {
            return baseCost; // No modification
        }
        
        public float ModifyRecoveryRate(float baseRate)
        {
            return baseRate; // No modification
        }
        
        public bool CanPerformAction(string actionId, EnergyManager manager)
        {
            return true; // No restrictions
        }
    }

    // Energized state
    public class EnergizedState : IEnergyState
    {
        public string GetStateId() => "energized";
        public string GetStateName() => "Energized";
        
        public void OnEnter(EnergyManager manager)
        {
            // Apply temporary stat bonuses
            var statEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.EnergyStateChanged,
                parameters = new Dictionary<string, object>
                {
                    { "statId", "charisma" },
                    { "modifierValue", 5.0f },
                    { "modifierDuration", 24.0f },
                    { "source", "energized" }
                }
            };
            if (ServiceLocator.Exists<EventBusReference>())
            {
                ServiceLocator.Get<EventBusReference>().Publish(statEvent);
            }
        }
        
        public void OnExit(EnergyManager manager)
        {
            // Remove bonuses
        }
        
        public void OnDayChanged(EnergyManager manager) { }
        
        public float ModifyEnergyCost(string actionId, float baseCost)
        {
            return baseCost * 0.8f; // 20% energy efficiency
        }
        
        public float ModifyRecoveryRate(float baseRate)
        {
            return baseRate * 1.2f; // 20% faster recovery
        }
        
        public bool CanPerformAction(string actionId, EnergyManager manager)
        {
            return true; // All actions allowed
        }
    }

    // Fatigued state
    public class FatiguedState : IEnergyState
    {
        public string GetStateId() => "fatigued";
        public string GetStateName() => "Fatigued";
        
        public void OnEnter(EnergyManager manager)
        {
            // Apply fatigue penalties
            var statEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.EnergyStateChanged,
                parameters = new Dictionary<string, object>
                {
                    { "statId", "charisma" },
                    { "modifierValue", -10.0f },
                    { "modifierDuration", -1.0f }, // Permanent until state changes
                    { "source", "fatigued" }
                }
            };
            if (ServiceLocator.Exists<EventBusReference>())
            {
                ServiceLocator.Get<EventBusReference>().Publish(statEvent);
            }
        }
        
        public void OnExit(EnergyManager manager)
        {
            // Remove penalties
        }
        
        public void OnDayChanged(EnergyManager manager) { }
        
        public float ModifyEnergyCost(string actionId, float baseCost)
        {
            return baseCost * 1.25f; // 25% more energy cost
        }
        
        public float ModifyRecoveryRate(float baseRate)
        {
            return baseRate * 0.8f; // 20% slower recovery
        }
        
        public bool CanPerformAction(string actionId, EnergyManager manager)
        {
            // Restrict certain actions when fatigued
            string[] restrictedActions = { "workout", "heavyLabor", "allNighter" };
            return !Array.Exists(restrictedActions, action => action == actionId);
        }
    }
    
    // Exhausted state
    public class ExhaustedState : IEnergyState
    {
        public string GetStateId() => "exhausted";
        public string GetStateName() => "Exhausted";
        
        public void OnEnter(EnergyManager manager)
        {
            // Apply exhaustion penalties
            var statEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.EnergyStateChanged,
                parameters = new Dictionary<string, object>
                {
                    { "statId", "charisma" },
                    { "modifierValue", -20.0f },
                    { "modifierDuration", -1.0f }, // Permanent until state changes
                    { "source", "exhausted" }
                }
            };
            if (ServiceLocator.Exists<EventBusReference>())
            {
                ServiceLocator.Get<EventBusReference>().Publish(statEvent);
            }
            
            // Add status effect
            var statusEvent = new ResourceEvent
            {
                type = ResourceEvent.ResourceEventType.EnergyStateChanged,
                parameters = new Dictionary<string, object>
                {
                    { "statusEffect", "dizzy" },
                    { "duration", 24.0f },
                    { "source", "exhausted" }
                }
            };
            if (ServiceLocator.Exists<EventBusReference>())
            {
                ServiceLocator.Get<EventBusReference>().Publish(statusEvent);
            }
        }
        
        public void OnExit(EnergyManager manager)
        {
            // Remove penalties
        }
        
        public void OnDayChanged(EnergyManager manager) { }
        
        public float ModifyEnergyCost(string actionId, float baseCost)
        {
            return baseCost * 1.5f; // 50% more energy cost
        }
        
        public float ModifyRecoveryRate(float baseRate)
        {
            return baseRate * 0.5f; // 50% slower recovery
        }
        
        public bool CanPerformAction(string actionId, EnergyManager manager)
        {
            // Severely restrict actions when exhausted
            string[] allowedActions = { "sleep", "rest", "eatMeal", "takeMedicine" };
            return Array.Exists(allowedActions, action => action == actionId);
        }
    }
    
    // Custom energy state
    public class CustomEnergyState : IEnergyState
    {
        private string stateId;
        private string stateName;
        private List<string> restrictedActions;
        private List<string> allowedActions;
        private float energyCostMultiplier;
        private float recoveryRateMultiplier;
        
        public CustomEnergyState(CustomEnergyStateConfig config)
        {
            stateId = config.stateId;
            stateName = config.stateName;
            restrictedActions = new List<string>(config.restrictedActions);
            allowedActions = new List<string>(config.allowedActions);
            energyCostMultiplier = config.energyCostMultiplier;
            recoveryRateMultiplier = config.recoveryRateMultiplier;
        }
        
        public string GetStateId() => stateId;
        public string GetStateName() => stateName;
        
        public void OnEnter(EnergyManager manager)
        {
            // Apply custom effects
        }
        
        public void OnExit(EnergyManager manager)
        {
            // Remove custom effects
        }
        
        public void OnDayChanged(EnergyManager manager) { }
        
        public float ModifyEnergyCost(string actionId, float baseCost)
        {
            return baseCost * energyCostMultiplier;
        }
        
        public float ModifyRecoveryRate(float baseRate)
        {
            return baseRate * recoveryRateMultiplier;
        }
        
        public bool CanPerformAction(string actionId, EnergyManager manager)
        {
            if (allowedActions.Count > 0)
            {
                // Whitelist approach
                return allowedActions.Contains(actionId);
            }
            else if (restrictedActions.Count > 0)
            {
                // Blacklist approach
                return !restrictedActions.Contains(actionId);
            }
            
            return true;
        }
    }
    #endregion

    #region Service Locator
    // Service locator pattern
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> services = new Dictionary<Type, object>();
        
        public static void Register<T>(T service)
        {
            services[typeof(T)] = service;
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
        }
    }
    #endregion

    #endregion

    #region Configuration
    // ScriptableObject configuration
    [CreateAssetMenu(fileName = "LifeResourceConfig", menuName = "Systems/Life Resource Config")]
    public class LifeResourceConfig : ScriptableObject
    {
        public TimeConfig timeConfig;
        public EnergyConfig energyConfig;
        public FinanceConfig financeConfig;
        public SocialCreditConfig socialConfig;
        
        [System.Serializable]
        public class TimeConfig
        {
            public float hoursPerRealSecond = 0.2f;
            public int startingDay = 1;
            public int startingMonth = 1;
            public int startingYear = 2023;
            public TimeResourceSystem.DayOfWeek startingDayOfWeek;
            public List<TimeBlockTemplate> timeBlockTemplates;
        }
        
        [System.Serializable]
        public class TimeBlockTemplate
        {
            public string id;
            public string name;
            public float startHour;
            public float endHour;
        }
        
        [System.Serializable]
        public class CustomEnergyStateConfig
        {
            public string stateId;
            public string stateName;
            public List<string> restrictedActions = new List<string>();
            public List<string> allowedActions = new List<string>();
            public float energyCostMultiplier = 1f;
            public float recoveryRateMultiplier = 1f;
        }
        
        [System.Serializable]
        public class FinanceConfig
        {
            public float startingMoney = 1000f;
            public List<FinanceSystem.IncomeSource> defaultIncomeSources = new List<FinanceSystem.IncomeSource>();
            public List<FinanceSystem.RecurringExpense> defaultExpenses = new List<FinanceSystem.RecurringExpense>();
            public Dictionary<string, float> actionCosts = new Dictionary<string, float>();
        }
        
        [System.Serializable]
        public class SocialCreditConfig
        {
            public List<SocialContextTemplate> socialContexts = new List<SocialContextTemplate>();
            
            [System.Serializable]
            public class SocialContextTemplate
            {
                public string contextId;
                public string contextName;
                public float startingScore = 50f;
                public List<SocialCreditSystem.SocialTier> tiers = new List<SocialCreditSystem.SocialTier>();
            }
        }
    }
    #endregion

    #region Persistence
    // Save data structures
    [System.Serializable]
    public class LifeResourceSaveData
    {
        public TimeResourceSaveData timeData;
        public EnergySaveData energyData;
        public FinanceSaveData financeData;
        public SocialCreditSaveData socialData;
    }

    [System.Serializable]
    public class TimeResourceSaveData
    {
        public int currentDay;
        public float currentHour;
        public int currentDayOfWeek;
        public int currentWeek;
        public int currentMonth;
        public int currentYear;
        public List<SerializableTimeBlock> timeBlocks = new List<SerializableTimeBlock>();
        
        [System.Serializable]
        public class SerializableTimeBlock
        {
            public string blockId;
            public string blockName;
            public float startHour;
            public float endHour;
            public bool isAvailable;
            public string allocatedActionId;
        }
    }

    [System.Serializable]
    public class EnergySaveData
    {
        public string playerId;
        public float maxEnergy;
        public float currentEnergy;
        public float recoveryRate;
        public List<ActivityCost> activityCosts = new List<ActivityCost>();
        public List<EnergySystem.EnergyModifier> activeModifiers = new List<EnergySystem.EnergyModifier>();
        public string currentStateId;
        
        [System.Serializable]
        public class ActivityCost
        {
            public string activityId;
            public float cost;
        }
    }

    [System.Serializable]
    public class FinanceSaveData
    {
        public string playerId;
        public float currentMoney;
        public List<FinanceSystem.IncomeSource> incomeSources = new List<FinanceSystem.IncomeSource>();
        public List<FinanceSystem.RecurringExpense> expenses = new List<FinanceSystem.RecurringExpense>();
        public List<FinanceSystem.Transaction> recentTransactions = new List<FinanceSystem.Transaction>();
        public float dailyIncome;
        public float dailyExpenses;
        public float monthlyBalance;
        public float financialStability;
    }

    [System.Serializable]
    public class SocialCreditSaveData
    {
        public string playerId;
        public List<SocialCreditEntry> creditEntries = new List<SocialCreditEntry>();
        
        [System.Serializable]
        public class SocialCreditEntry
        {
            public string contextId;
            public string contextName;
            public float creditScore;
            public SocialCreditSystem.SocialTier currentTier;
            public List<SocialCreditSystem.CreditEvent> recentEvents = new List<SocialCreditSystem.CreditEvent>();
        }
    }

    // Data persistence helper
    public class ResourceDataPersistence
    {
        private const string SaveFileName = "life_resources.json";
        
        public static void SaveResourceData(LifeResourceSaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);
            File.WriteAllText(path, json);
        }
        
        public static LifeResourceSaveData LoadResourceData()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<LifeResourceSaveData>(json);
            }
            return null;
        }
    }
    #endregion
}