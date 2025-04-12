using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskManagement
{
    #region Data Structures

    [Serializable]
    public enum ActionType
    {
        Theft,
        Trespassing,
        Assault,
        Vandalism,
        Hacking,
        Sabotage,
        FalseIdentity,
        Bribery,
        Blackmail,
        Intimidation,
        Espionage,
        Impersonation
    }

    [Serializable]
    public enum ModifierType
    {
        Additive,
        Multiplicative,
        Exponential,
        Flat
    }

    [Serializable]
    public enum RiskCategory
    {
        Legal,
        Social,
        Physical,
        Digital,
        Psychological
    }

    [Serializable]
    public enum CrisisType
    {
        Any,
        Legal,
        Social,
        Physical,
        Digital,
        Psychological,
        Compound
    }

    [Serializable]
    public enum StageEscalationModel
    {
        Linear,
        Exponential,
        Stepped,
        Random
    }

    [Serializable]
    public enum CrisisStatus
    {
        Pending,
        Active,
        Resolved,
        Failed,
        Expired
    }

    [Serializable]
    public enum ResourceType
    {
        Money,
        Influence,
        Materials,
        Information,
        Time
    }

    [Serializable]
    public enum SkillType
    {
        Persuasion,
        Stealth,
        Combat,
        Hacking,
        Intelligence,
        Connections
    }

    [Serializable]
    public class RiskModifierCondition
    {
        public string targetEntityID;
        public RiskCategory category;
        public bool requiresNighttime;
        public bool requiresIsolation;
        public float crowdDensityMaximum = 1.0f;
        public List<string> requiredItems = new List<string>();
        public List<string> forbiddenItems = new List<string>();
        public List<ActionType> applicableActionTypes = new List<ActionType>();

        public bool IsSatisfied(ActionType actionType, string entityID, ContextData context)
        {
            // Check if action type is applicable or if no specific action types are defined
            if (applicableActionTypes.Count > 0 && !applicableActionTypes.Contains(actionType))
                return false;

            // Check target entity
            if (!string.IsNullOrEmpty(targetEntityID) && targetEntityID != entityID)
                return false;

            // Check nighttime requirement
            if (requiresNighttime && !context.isNighttime)
                return false;

            // Check isolation requirement
            if (requiresIsolation && context.hasWitnesses)
                return false;

            // Check crowd density
            if (context.crowdDensity > crowdDensityMaximum)
                return false;

            // Check required items
            foreach (var item in requiredItems)
            {
                if (!context.availableItems.Contains(item))
                    return false;
            }

            // Check forbidden items
            foreach (var item in forbiddenItems)
            {
                if (context.availableItems.Contains(item))
                    return false;
            }

            return true;
        }
    }

    [Serializable]
    public class RiskModifier
    {
        public string modifierID;
        public ModifierType type;
        public float value;
        public RiskModifierCondition condition;
        public DateTime? expirationTime;

        public bool IsExpired()
        {
            return expirationTime.HasValue && DateTime.Now > expirationTime.Value;
        }

        public float Apply(float baseValue)
        {
            switch (type)
            {
                case ModifierType.Additive:
                    return baseValue + value;
                case ModifierType.Multiplicative:
                    return baseValue * value;
                case ModifierType.Exponential:
                    return baseValue * Mathf.Pow(value, baseValue);
                case ModifierType.Flat:
                    return value;
                default:
                    return baseValue;
            }
        }
    }

    [Serializable]
    public class RiskProfile
    {
        public string entityID;
        public float baseRiskMultiplier = 1.0f;
        public List<RiskModifier> specificModifiers = new List<RiskModifier>();
        public Dictionary<RiskCategory, float> categoryMultipliers = new Dictionary<RiskCategory, float>();

        public RiskProfile(string id)
        {
            entityID = id;
            // Initialize default category multipliers
            foreach (RiskCategory category in Enum.GetValues(typeof(RiskCategory)))
            {
                categoryMultipliers[category] = 1.0f;
            }
        }

        public void AddModifier(RiskModifier modifier)
        {
            // Replace existing modifier with same ID if found
            for (int i = 0; i < specificModifiers.Count; i++)
            {
                if (specificModifiers[i].modifierID == modifier.modifierID)
                {
                    specificModifiers[i] = modifier;
                    return;
                }
            }
            // Otherwise add new modifier
            specificModifiers.Add(modifier);
        }

        public void RemoveModifier(string modifierID)
        {
            specificModifiers.RemoveAll(m => m.modifierID == modifierID);
        }

        public void CleanExpiredModifiers()
        {
            specificModifiers.RemoveAll(m => m.IsExpired());
        }
    }

    [Serializable]
    public class RiskAccumulationModel
    {
        public float decayRate = 0.1f;
        public float plateauThreshold = 5.0f;
        public float maximumMultiplier = 3.0f;
        public Dictionary<ActionType, float> actionSpecificDecayRates = new Dictionary<ActionType, float>();

        public float CalculateAccumulationMultiplier(List<ActionRecord> recentActions, ActionType currentAction)
        {
            if (recentActions == null || recentActions.Count == 0)
                return 1.0f;

            // Count recent actions of the same type and calculate time factor
            float accumulation = 0;
            foreach (var action in recentActions)
            {
                if (action.actionType == currentAction)
                {
                    // Calculate time decay
                    float timeDecay = GetTimeDecayFactor(action, currentAction);
                    accumulation += timeDecay;
                }
            }

            // Apply plateau model (diminishing returns)
            float multiplier = 1.0f + Mathf.Min(maximumMultiplier - 1.0f, 
                                              (maximumMultiplier - 1.0f) * 
                                              (1.0f - Mathf.Exp(-accumulation / plateauThreshold)));
            
            return multiplier;
        }

        private float GetTimeDecayFactor(ActionRecord action, ActionType currentAction)
        {
            // Calculate time since action in minutes
            float minutesSinceAction = (float)(DateTime.Now - action.timestamp).TotalMinutes;
            
            // Get appropriate decay rate for action type or use default
            float actionDecayRate = actionSpecificDecayRates.ContainsKey(currentAction) ? 
                                    actionSpecificDecayRates[currentAction] : 
                                    decayRate;
            
            // Exponential decay formula
            return Mathf.Exp(-actionDecayRate * minutesSinceAction);
        }
    }

    [Serializable]
    public class ActionRecord
    {
        public ActionType actionType;
        public DateTime timestamp;
        public string context;
        public float riskValue;
        public bool successful;

        public ActionRecord(ActionType type, float risk, bool success, string contextInfo = "")
        {
            actionType = type;
            timestamp = DateTime.Now;
            riskValue = risk;
            successful = success;
            context = contextInfo;
        }
    }

    [Serializable]
    public class ContextData
    {
        public bool isNighttime;
        public bool hasWitnesses;
        public float crowdDensity;
        public List<string> availableItems = new List<string>();
        public string locationID;
        public float securityLevel;
        public Dictionary<string, float> contextualFactors = new Dictionary<string, float>();
    }

    [Serializable]
    public class ActionData
    {
        public ActionType actionType;
        public string actionID;
        public string actionName;
        public float baseRiskValue;
        public float minimumRisk;
        public float maximumRisk;
        public List<RiskCategory> riskCategories = new List<RiskCategory>();
        public Dictionary<string, float> specificContextFactors = new Dictionary<string, float>();
    }

    [Serializable]
    public class CharacterData
    {
        public string characterID;
        public Dictionary<SkillType, float> skills = new Dictionary<SkillType, float>();
        public List<string> traits = new List<string>();
        public List<ActionRecord> recentActions = new List<ActionRecord>();
    }

    [Serializable]
    public class ActionResult
    {
        public string actionId;
        public float riskValue;
        public float exposureFactor;
        public bool isSuccessful;
        public bool evidenceLeft;
        public string contextDescription;
    }

    [Serializable]
    public class CharacterSuspicion
    {
        public string characterID;
        public float currentSuspicion; // 0.0 to 1.0
        public List<SuspicionRecord> suspicionHistory = new List<SuspicionRecord>();
        public Dictionary<RiskCategory, float> categoryModifiers = new Dictionary<RiskCategory, float>();
        public DateTime lastUpdateTime;

        public CharacterSuspicion(string id)
        {
            characterID = id;
            currentSuspicion = 0f;
            lastUpdateTime = DateTime.Now;
            
            // Initialize default category multipliers
            foreach (RiskCategory category in Enum.GetValues(typeof(RiskCategory)))
            {
                categoryModifiers[category] = 1.0f;
            }
        }
    }

    [Serializable]
    public class SuspicionRecord
    {
        public DateTime timestamp;
        public float suspicionChange;
        public string sourceAction;
        public string context;

        public SuspicionRecord()
        {
            timestamp = DateTime.Now;
        }
    }

    [Serializable]
    public class GroupSuspicion
    {
        public string groupID;
        public float averageSuspicion;
        public float leaderInfluenceFactor;
        public Dictionary<string, float> memberContributions = new Dictionary<string, float>();
        public SuspicionSpreadModel spreadModel = new SuspicionSpreadModel();

        public GroupSuspicion(string id)
        {
            groupID = id;
            averageSuspicion = 0f;
            leaderInfluenceFactor = 1.5f;
        }
    }

    [Serializable]
    public class SuspicionSpreadModel
    {
        public float baseSpreadRate = 0.2f;
        public float proximityFactor = 1.5f;
        public float relationshipStrengthFactor = 2.0f;
        public float leadershipInfluenceFactor = 2.0f;
        public float maximumSpreadPerUpdate = 0.1f;
    }

    [Serializable]
    public class SuspicionDecayModel
    {
        public float baseDecayRate = 0.01f; // Per hour
        public Dictionary<string, float> characterSpecificDecayRates = new Dictionary<string, float>();
        public float minimumSuspicionThreshold = 0.05f;
        public float[] tieredDecayRates = new float[] { 0.01f, 0.007f, 0.005f, 0.003f, 0.001f };
        
        public float GetDecayRate(string characterId, float currentSuspicion)
        {
            // Get character-specific rate or default
            float rate = characterSpecificDecayRates.ContainsKey(characterId) ? 
                         characterSpecificDecayRates[characterId] : 
                         baseDecayRate;
            
            // Apply tiered decay rates based on suspicion level
            int tier = Mathf.FloorToInt(currentSuspicion * tieredDecayRates.Length);
            tier = Mathf.Clamp(tier, 0, tieredDecayRates.Length - 1);
            
            return rate * tieredDecayRates[tier];
        }
    }

    [Serializable]
    public class SuspicionThresholds
    {
        public float warningThreshold = 0.3f;
        public float dangerThreshold = 0.6f;
        public float criticalThreshold = 0.85f;
        public Dictionary<CrisisType, float> crisisThresholds = new Dictionary<CrisisType, float>();

        public SuspicionThresholds()
        {
            // Initialize default crisis thresholds
            foreach (CrisisType type in Enum.GetValues(typeof(CrisisType)))
            {
                if (type != CrisisType.Any)
                {
                    crisisThresholds[type] = 0.7f;
                }
            }
        }

        public bool IsCrisisTriggerMet(float suspicionValue, CrisisType crisisType)
        {
            if (crisisType == CrisisType.Any)
            {
                return suspicionValue >= criticalThreshold;
            }
            
            return crisisThresholds.ContainsKey(crisisType) && 
                   suspicionValue >= crisisThresholds[crisisType];
        }
    }

    [Serializable]
    public class CrisisTrigger
    {
        public string triggerID;
        public float suspicionThreshold;
        public List<RiskCategory> relevantCategories = new List<RiskCategory>();
        public Dictionary<string, float> conditionParameters = new Dictionary<string, float>();
        public string description;
    }

    [Serializable]
    public class CrisisStage
    {
        public string stageID;
        public string description;
        public float durationFactor; // Ratio to overall crisis duration
        public StageEscalationModel escalation;
        public List<StageEffect> effects = new List<StageEffect>();
        public List<string> availableResolutions = new List<string>();
    }

    [Serializable]
    public class StageEffect
    {
        public string effectID;
        public string description;
        public float magnitude;
        public string targetSystemID;
        public string targetParameterID;
        public EffectType effectType;
    }

    [Serializable]
    public enum EffectType
    {
        StatModifier,
        EnvironmentChange,
        AIBehaviorChange,
        UINotification,
        QuestTrigger,
        ResourceDrain
    }

    [Serializable]
    public class CrisisResolution
    {
        public string resolutionID;
        public string name;
        public string description;
        public Dictionary<ResourceType, float> cost = new Dictionary<ResourceType, float>();
        public Dictionary<SkillType, float> skillRequirements = new Dictionary<SkillType, float>();
        public float baseSuccessRate;
        public List<ResolutionModifier> modifiers = new List<ResolutionModifier>();
        public List<ResolutionOutcome> possibleOutcomes = new List<ResolutionOutcome>();
        public ResolutionRestrictions restrictions = new ResolutionRestrictions();
    }

    [Serializable]
    public class ResolutionModifier
    {
        public string modifierID;
        public float valueChange;
        public string contextualFactor;
        public string requiredItem;
    }

    [Serializable]
    public class ResolutionOutcome
    {
        public string outcomeID;
        public string description;
        public float probabilityWeight;
        public Dictionary<string, float> effects = new Dictionary<string, float>();
    }

    [Serializable]
    public class ResolutionRestrictions
    {
        public List<string> requiredItems = new List<string>();
        public List<string> requiredTraits = new List<string>();
        public List<string> requiredRelationships = new List<string>();
        public float minimumReputation;
    }

    [Serializable]
    public class ActiveResolutionAttempt
    {
        public string resolutionID;
        public DateTime attemptTime;
        public float successProbability;
        public Dictionary<string, float> contributingFactors = new Dictionary<string, float>();
        public bool isSuccessful;
        public string resultOutcomeID;
    }

    [Serializable]
    public class CrisisAftereffect
    {
        public string aftereffectID;
        public string description;
        public float duration; // In hours
        public Dictionary<string, float> parameterModifications = new Dictionary<string, float>();
    }

    [Serializable]
    public class CrisisTemplate
    {
        public string crisisID;
        public string name;
        public CrisisType type;
        public List<CrisisTrigger> possibleTriggers = new List<CrisisTrigger>();
        public List<CrisisStage> stages = new List<CrisisStage>();
        public List<CrisisResolution> possibleResolutions = new List<CrisisResolution>();
        public List<CrisisAftereffect> aftereffects = new List<CrisisAftereffect>();
    }

    [Serializable]
    public class ActiveCrisis
    {
        public string instanceID;
        public string templateID;
        public int currentStageIndex;
        public float progressWithinStage; // 0.0 to 1.0
        public DateTime startTime;
        public DateTime estimatedResolutionDeadline;
        public List<string> involvedEntities = new List<string>();
        public Dictionary<string, float> entityExposureLevels = new Dictionary<string, float>();
        public List<ActiveResolutionAttempt> resolutionAttempts = new List<ActiveResolutionAttempt>();
        public CrisisStatus status = CrisisStatus.Pending;
    }

    [Serializable]
    public class CrisisHistory
    {
        public string crisisID;
        public string crisisName;
        public CrisisType crisisType;
        public DateTime startTime;
        public DateTime endTime;
        public CrisisStatus finalStatus;
        public List<string> involvedEntities = new List<string>();
        public List<string> resolutionsMade = new List<string>();
    }

    [Serializable]
    public class CrisisEscalationModel
    {
        public float baseEscalationRate = 1.0f;
        public Dictionary<CrisisType, float> typeSpecificRates = new Dictionary<CrisisType, float>();
        public float playerActionInfluenceMultiplier = 2.0f;
        public float randomVariationRange = 0.2f;
        public bool allowDeescalation = true;
    }

    #endregion

    #region Events

    public class RiskEvent
    {
        public string EventID { get; private set; }
        public DateTime Timestamp { get; private set; }

        public RiskEvent()
        {
            EventID = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
        }
    }

    public class RiskValueCalculatedEvent : RiskEvent
    {
        public ActionType ActionType { get; set; }
        public string CharacterID { get; set; }
        public float RiskValue { get; set; }
        public Dictionary<string, float> ContributingFactors { get; set; } = new Dictionary<string, float>();
    }

    public class SuspicionChangedEvent : RiskEvent
    {
        public string TargetID { get; set; }
        public float OldValue { get; set; }
        public float NewValue { get; set; }
        public string SourceAction { get; set; }
        public List<RiskCategory> Categories { get; set; } = new List<RiskCategory>();
    }

    public class CrisisTriggeredEvent : RiskEvent
    {
        public string CrisisID { get; set; }
        public string CrisisName { get; set; }
        public CrisisType CrisisType { get; set; }
        public List<string> InvolvedEntities { get; set; } = new List<string>();
        public DateTime Deadline { get; set; }
    }

    public class CrisisStageChangedEvent : RiskEvent
    {
        public string CrisisID { get; set; }
        public int OldStage { get; set; }
        public int NewStage { get; set; }
        public string StageDescription { get; set; }
    }

    public class CrisisResolvedEvent : RiskEvent
    {
        public string CrisisID { get; set; }
        public bool Successful { get; set; }
        public List<string> ResolutionMethods { get; set; } = new List<string>();
        public string OutcomeDescription { get; set; }
    }

    #endregion

    #region ScriptableObject Data

    [CreateAssetMenu(fileName = "RiskActionData", menuName = "Risk Management/Risk Action Data")]
    public class RiskActionDataSO : ScriptableObject
    {
        public List<ActionData> actions = new List<ActionData>();

        public ActionData GetAction(string actionID)
        {
            return actions.Find(a => a.actionID == actionID);
        }

        public ActionData GetAction(ActionType actionType)
        {
            return actions.Find(a => a.actionType == actionType);
        }
    }

    [CreateAssetMenu(fileName = "CrisisTemplateData", menuName = "Risk Management/Crisis Template Data")]
    public class CrisisTemplateSO : ScriptableObject
    {
        public List<CrisisTemplate> templates = new List<CrisisTemplate>();

        public CrisisTemplate GetTemplate(string templateID)
        {
            return templates.Find(t => t.crisisID == templateID);
        }

        public List<CrisisTemplate> GetTemplatesByType(CrisisType type)
        {
            if (type == CrisisType.Any)
                return templates;

            return templates.Where(t => t.type == type).ToList();
        }
    }

    [CreateAssetMenu(fileName = "RiskModifierProfiles", menuName = "Risk Management/Risk Modifier Profiles")]
    public class RiskModifierProfileSO : ScriptableObject
    {
        public List<RiskModifier> globalModifiers = new List<RiskModifier>();
        public Dictionary<string, List<RiskModifier>> entityModifiers = new Dictionary<string, List<RiskModifier>>();
        
        public List<RiskModifier> GetModifiersForEntity(string entityID)
        {
            if (entityModifiers.ContainsKey(entityID))
                return entityModifiers[entityID];
            
            return new List<RiskModifier>();
        }
    }

    [CreateAssetMenu(fileName = "RiskManagementConfig", menuName = "Risk Management/Configuration")]
    public class RiskManagementConfigSO : ScriptableObject
    {
        [Header("Risk Calculation")]
        public RiskAccumulationModel accumulationModel = new RiskAccumulationModel();
        public float defaultBaseRisk = 0.3f;
        public float maximumRiskValue = 1.0f;
        public float minimumRiskValue = 0.05f;
        
        [Header("Suspicion")]
        public SuspicionDecayModel suspicionDecay = new SuspicionDecayModel();
        public SuspicionThresholds suspicionThresholds = new SuspicionThresholds();
        public float defaultSuspicionIncrease = 0.1f;
        
        [Header("Crisis")]
        public float defaultCrisisDuration = 24f; // Hours
        public float crisisStageProgression = 0.25f; // Per hour
        public CrisisEscalationModel escalationModel = new CrisisEscalationModel();
        
        [Header("System")]
        public float systemUpdateInterval = 10f; // Seconds
        public bool enableDebugLogging = false;
        public bool pauseCrisisProgressionWhenGamePaused = true;
    }

    [CreateAssetMenu(fileName = "RiskEventBus", menuName = "Risk Management/Event Bus")]
    public class RiskEventBusSO : ScriptableObject
    {
        private Dictionary<Type, List<object>> _subscribers = new Dictionary<Type, List<object>>();

        public void Subscribe<T>(Action<T> callback) where T : RiskEvent
        {
            Type eventType = typeof(T);
            
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<object>();
            }
            
            _subscribers[eventType].Add(callback);
        }

        public void Unsubscribe<T>(Action<T> callback) where T : RiskEvent
        {
            Type eventType = typeof(T);
            
            if (_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType].Remove(callback);
            }
        }

        public void Publish<T>(T eventData) where T : RiskEvent
        {
            Type eventType = typeof(T);
            
            if (!_subscribers.ContainsKey(eventType))
                return;
            
            // Create a defensive copy to allow for subscription/unsubscription during event handling
            var subscribers = new List<object>(_subscribers[eventType]);
            
            foreach (var subscriber in subscribers)
            {
                var callback = subscriber as Action<T>;
                callback?.Invoke(eventData);
            }
        }

        public void ClearAllSubscribers()
        {
            _subscribers.Clear();
        }
    }

    #endregion

    #region Core Components

    public class RiskManager : MonoBehaviour
    {
        private static RiskManager _instance;
        public static RiskManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RiskManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RiskManager");
                        _instance = go.AddComponent<RiskManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                
                return _instance;
            }
        }
        
        [SerializeField] private RiskActionDataSO riskActionData;
        [SerializeField] private CrisisTemplateSO crisisTemplates;
        [SerializeField] private RiskModifierProfileSO modifierProfiles;
        [SerializeField] private RiskManagementConfigSO configData;
        [SerializeField] public RiskEventBusSO eventBus;
        
        private RiskCalculator _riskCalculator;
        private SuspicionTracker _suspicionTracker;
        private CrisisGenerator _crisisGenerator;
        private CrisisTimeline _crisisTimeline;
        private ResolutionHandler _resolutionHandler;
        private RiskFeedback _riskFeedback;
        
        private float _timer;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize components
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            _riskCalculator = new RiskCalculator(riskActionData, modifierProfiles, configData);
            _suspicionTracker = new SuspicionTracker(configData);
            _crisisGenerator = new CrisisGenerator(crisisTemplates, configData);
            _crisisTimeline = new CrisisTimeline(configData);
            _resolutionHandler = new ResolutionHandler();
            _riskFeedback = new RiskFeedback();
            
            // Connect components to event bus
            ConnectEvents();
        }
        
        private void ConnectEvents()
        {
            // Subscribe to risk calculation events
            eventBus.Subscribe<RiskValueCalculatedEvent>(OnRiskValueCalculated);
            
            // Subscribe to suspicion events
            eventBus.Subscribe<SuspicionChangedEvent>(OnSuspicionChanged);
            
            // Subscribe to crisis events
            eventBus.Subscribe<CrisisTriggeredEvent>(OnCrisisTriggered);
            eventBus.Subscribe<CrisisStageChangedEvent>(OnCrisisStageChanged);
            eventBus.Subscribe<CrisisResolvedEvent>(OnCrisisResolved);
        }
        
        private void Update()
        {
            _timer += Time.deltaTime;
            
            if (_timer >= configData.systemUpdateInterval)
            {
                UpdateSystems();
                _timer = 0;
            }
        }
        
        private void UpdateSystems()
        {
            // Update suspicion decay
            _suspicionTracker.ProcessDecay(Time.deltaTime);
            
            // Update crisis progression
            if (!configData.pauseCrisisProgressionWhenGamePaused || !Time.timeScale.Equals(0))
            {
                _crisisTimeline.UpdateCrisisProgression(Time.deltaTime);
            }
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            eventBus.Unsubscribe<RiskValueCalculatedEvent>(OnRiskValueCalculated);
            eventBus.Unsubscribe<SuspicionChangedEvent>(OnSuspicionChanged);
            eventBus.Unsubscribe<CrisisTriggeredEvent>(OnCrisisTriggered);
            eventBus.Unsubscribe<CrisisStageChangedEvent>(OnCrisisStageChanged);
            eventBus.Unsubscribe<CrisisResolvedEvent>(OnCrisisResolved);
        }
        
        #region Event Handlers
        
        private void OnRiskValueCalculated(RiskValueCalculatedEvent evt)
        {
            // Process risk calculation result
            if (configData.enableDebugLogging)
            {
                Debug.Log($"Risk calculated for {evt.CharacterID}: {evt.RiskValue} for action {evt.ActionType}");
            }
            
            // Feedback to player
            _riskFeedback.DisplayRiskLevel(evt.ActionType, evt.RiskValue);
        }
        
        private void OnSuspicionChanged(SuspicionChangedEvent evt)
        {
            // Process suspicion change
            if (configData.enableDebugLogging)
            {
                Debug.Log($"Suspicion for {evt.TargetID} changed from {evt.OldValue} to {evt.NewValue}");
            }
            
            // Check for crisis trigger
            if (evt.NewValue > configData.suspicionThresholds.criticalThreshold &&
                evt.OldValue <= configData.suspicionThresholds.criticalThreshold)
            {
                _crisisGenerator.CheckForCrisisTrigger(evt.TargetID, evt.NewValue, evt.Categories);
            }
            
            // Feedback to player
            _riskFeedback.UpdateSuspicionDisplay(evt.TargetID, evt.NewValue);
        }
        
        private void OnCrisisTriggered(CrisisTriggeredEvent evt)
        {
            // React to crisis being triggered
            if (configData.enableDebugLogging)
            {
                Debug.Log($"Crisis triggered: {evt.CrisisName} affecting {evt.InvolvedEntities.Count} entities");
            }
            
            // Register crisis with timeline
            _crisisTimeline.RegisterCrisis(evt.CrisisID);
            
            // Feedback to player
            _riskFeedback.DisplayCrisisNotification(evt.CrisisName, evt.CrisisType, evt.Deadline);
        }
        
        private void OnCrisisStageChanged(CrisisStageChangedEvent evt)
        {
            // React to crisis stage change
            if (configData.enableDebugLogging)
            {
                Debug.Log($"Crisis {evt.CrisisID} changed from stage {evt.OldStage} to {evt.NewStage}: {evt.StageDescription}");
            }
            
            // Feedback to player
            _riskFeedback.UpdateCrisisDisplay(evt.CrisisID, evt.NewStage, evt.StageDescription);
        }
        
        private void OnCrisisResolved(CrisisResolvedEvent evt)
        {
            // React to crisis resolution
            if (configData.enableDebugLogging)
            {
                Debug.Log($"Crisis {evt.CrisisID} resolved. Success: {evt.Successful}. Outcome: {evt.OutcomeDescription}");
            }
            
            // Unregister from timeline
            _crisisTimeline.UnregisterCrisis(evt.CrisisID);
            
            // Feedback to player
            _riskFeedback.DisplayResolutionOutcome(evt.CrisisID, evt.Successful, evt.OutcomeDescription);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Calculates risk value for a player action
        /// </summary>
        public float CalculateActionRisk(ActionType actionType, string characterId, ContextData context)
        {
            return _riskCalculator.CalculateRisk(actionType, characterId, context);
        }
        
        /// <summary>
        /// Records an action taken by a character with its risk value and success/failure
        /// </summary>
        public void RecordAction(ActionType actionType, string characterId, float riskValue, bool successful, string context = "")
        {
            _riskCalculator.RecordAction(characterId, new ActionRecord(actionType, riskValue, successful, context));
            
            // Update suspicion based on action outcome
            float suspicionChange = riskValue * (successful ? 0.5f : 2.0f);
            _suspicionTracker.UpdateSuspicion(characterId, suspicionChange, actionType.ToString());
        }
        
        /// <summary>
        /// Adds a temporary risk modifier to a character
        /// </summary>
        public void AddRiskModifier(string characterId, RiskModifier modifier)
        {
            _riskCalculator.AddModifier(characterId, modifier);
        }
        
        /// <summary>
        /// Removes a risk modifier from a character
        /// </summary>
        public void RemoveRiskModifier(string characterId, string modifierId)
        {
            _riskCalculator.RemoveModifier(characterId, modifierId);
        }
        
        /// <summary>
        /// Gets the current suspicion level for a character
        /// </summary>
        public float GetSuspicionLevel(string characterId)
        {
            return _suspicionTracker.GetSuspicionLevel(characterId);
        }
        
        /// <summary>
        /// Modifies suspicion level directly
        /// </summary>
        public void ModifySuspicion(string characterId, float amount, string source)
        {
            _suspicionTracker.UpdateSuspicion(characterId, amount, source);
        }
        
        /// <summary>
        /// Gets all active crises
        /// </summary>
        public List<ActiveCrisis> GetActiveCrises()
        {
            return _crisisTimeline.GetActiveCrises();
        }
        
        /// <summary>
        /// Gets a specific active crisis
        /// </summary>
        public ActiveCrisis GetCrisis(string crisisId)
        {
            return _crisisTimeline.GetCrisis(crisisId);
        }
        
        /// <summary>
        /// Attempts a resolution for a crisis
        /// </summary>
        public bool AttemptCrisisResolution(string crisisId, string resolutionId, Dictionary<string, float> parameters = null)
        {
            return _resolutionHandler.AttemptResolution(crisisId, resolutionId, parameters);
        }
        
        /// <summary>
        /// Gets available resolutions for a crisis
        /// </summary>
        public List<CrisisResolution> GetAvailableResolutions(string crisisId)
        {
            return _resolutionHandler.GetAvailableResolutions(crisisId);
        }
        
        /// <summary>
        /// Predicts risk for an action before taking it
        /// </summary>
        public float PredictRisk(ActionType actionType, string characterId, ContextData context)
        {
            return _riskCalculator.PredictRisk(actionType, characterId, context);
        }
        
        /// <summary>
        /// Gets the CrisisGenerator instance
        /// </summary>
        public CrisisGenerator GetCrisisGenerator()
        {
            return _crisisGenerator;
        }
        
        internal CrisisGenerator CrisisGenerator => _crisisGenerator;
        
        /// <summary>
        /// Gets a crisis template by ID
        /// </summary>
        public CrisisTemplate GetCrisisTemplate(string templateId)
        {
            return crisisTemplates.GetTemplate(templateId);
        }
        
        #endregion
    }

    #endregion

    #region Implementation Components

    public class RiskCalculator
    {
        private RiskActionDataSO _actionData;
        private RiskModifierProfileSO _modifierProfiles;
        private RiskManagementConfigSO _config;
        
        private Dictionary<string, RiskProfile> _characterRiskProfiles = new Dictionary<string, RiskProfile>();
        private Dictionary<string, List<ActionRecord>> _characterActionHistory = new Dictionary<string, List<ActionRecord>>();
        
        public RiskCalculator(RiskActionDataSO actionData, RiskModifierProfileSO modifierProfiles, RiskManagementConfigSO config)
        {
            _actionData = actionData;
            _modifierProfiles = modifierProfiles;
            _config = config;
        }
        
        public float CalculateRisk(ActionType actionType, string characterId, ContextData context)
        {
            // Get action data or use default values
            ActionData actionData = _actionData.GetAction(actionType);
            if (actionData == null)
            {
                Debug.LogWarning($"No action data found for {actionType}. Using default values.");
                actionData = new ActionData
                {
                    actionType = actionType,
                    actionID = actionType.ToString(),
                    actionName = actionType.ToString(),
                    baseRiskValue = _config.defaultBaseRisk,
                    minimumRisk = _config.minimumRiskValue,
                    maximumRisk = _config.maximumRiskValue
                };
            }
            
            // Get base risk value
            float baseRisk = actionData.baseRiskValue;
            
            // Apply global modifiers
            foreach (var modifier in _modifierProfiles.globalModifiers)
            {
                if (!modifier.IsExpired() && 
                    (modifier.condition == null || modifier.condition.IsSatisfied(actionType, characterId, context)))
                {
                    baseRisk = modifier.Apply(baseRisk);
                }
            }
            
            // Apply character-specific modifiers
            EnsureCharacterProfile(characterId);
            RiskProfile profile = _characterRiskProfiles[characterId];
            
            // Apply base character multiplier
            baseRisk *= profile.baseRiskMultiplier;
            
            // Apply category multipliers if applicable
            foreach (var category in actionData.riskCategories)
            {
                if (profile.categoryMultipliers.ContainsKey(category))
                {
                    baseRisk *= profile.categoryMultipliers[category];
                }
            }
            
            // Apply specific modifiers
            foreach (var modifier in profile.specificModifiers)
            {
                if (!modifier.IsExpired() && 
                    (modifier.condition == null || modifier.condition.IsSatisfied(actionType, characterId, context)))
                {
                    baseRisk = modifier.Apply(baseRisk);
                }
            }
            
            // Apply contextual modifiers
            if (context != null)
            {
                // Apply time of day modifier
                if (context.isNighttime)
                {
                    baseRisk *= 0.7f; // Less visible at night
                }
                
                // Apply crowd density modifier
                baseRisk *= 1.0f + (context.crowdDensity * 0.5f); // More witnesses = more risk
                
                // Apply security level modifier
                baseRisk *= 1.0f + (context.securityLevel * 0.8f); // Higher security = more risk
                
                // Apply specific action contextual factors
                foreach (var factor in actionData.specificContextFactors)
                {
                    if (context.contextualFactors.ContainsKey(factor.Key))
                    {
                        baseRisk *= 1.0f + (context.contextualFactors[factor.Key] * factor.Value);
                    }
                }
            }
            
            // Apply risk accumulation based on recent actions
            List<ActionRecord> recentActions = GetRecentActions(characterId);
            if (recentActions.Count > 0)
            {
                float accumulationMultiplier = _config.accumulationModel.CalculateAccumulationMultiplier(recentActions, actionType);
                baseRisk *= accumulationMultiplier;
            }
            
            // Clamp final risk value
            float finalRisk = Mathf.Clamp(baseRisk, 
                               actionData.minimumRisk, 
                               actionData.maximumRisk);
            
            // Create and publish event
            var evt = new RiskValueCalculatedEvent
            {
                ActionType = actionType,
                CharacterID = characterId,
                RiskValue = finalRisk,
                ContributingFactors = new Dictionary<string, float>()
            };
            
            RiskManager.Instance.eventBus.Publish(evt);
            
            return finalRisk;
        }
        
        public float PredictRisk(ActionType actionType, string characterId, ContextData context)
        {
            // This is essentially the same as CalculateRisk but without publishing the event
            // and potentially with some predictive elements
            return CalculateRisk(actionType, characterId, context);
        }
        
        public void RecordAction(string characterId, ActionRecord action)
        {
            if (!_characterActionHistory.ContainsKey(characterId))
            {
                _characterActionHistory[characterId] = new List<ActionRecord>();
            }
            
            _characterActionHistory[characterId].Add(action);
            
            // Trim history if needed
            const int maxHistorySize = 50;
            if (_characterActionHistory[characterId].Count > maxHistorySize)
            {
                _characterActionHistory[characterId] = _characterActionHistory[characterId]
                    .OrderByDescending(a => a.timestamp)
                    .Take(maxHistorySize)
                    .ToList();
            }
        }
        
        public void AddModifier(string characterId, RiskModifier modifier)
        {
            EnsureCharacterProfile(characterId);
            _characterRiskProfiles[characterId].AddModifier(modifier);
        }
        
        public void RemoveModifier(string characterId, string modifierId)
        {
            if (_characterRiskProfiles.ContainsKey(characterId))
            {
                _characterRiskProfiles[characterId].RemoveModifier(modifierId);
            }
        }
        
        private List<ActionRecord> GetRecentActions(string characterId)
        {
            if (!_characterActionHistory.ContainsKey(characterId))
            {
                return new List<ActionRecord>();
            }
            
            // Get actions from the last 24 hours
            return _characterActionHistory[characterId]
                .Where(a => (DateTime.Now - a.timestamp).TotalHours < 24)
                .OrderByDescending(a => a.timestamp)
                .ToList();
        }
        
        private void EnsureCharacterProfile(string characterId)
        {
            if (!_characterRiskProfiles.ContainsKey(characterId))
            {
                _characterRiskProfiles[characterId] = new RiskProfile(characterId);
                
                // Apply any predefined modifiers for this entity
                var entityModifiers = _modifierProfiles.GetModifiersForEntity(characterId);
                foreach (var modifier in entityModifiers)
                {
                    _characterRiskProfiles[characterId].AddModifier(modifier);
                }
            }
            else
            {
                // Clean up expired modifiers
                _characterRiskProfiles[characterId].CleanExpiredModifiers();
            }
        }
    }

    public class SuspicionTracker
    {
        private Dictionary<string, CharacterSuspicion> _individualSuspicions = new Dictionary<string, CharacterSuspicion>();
        private Dictionary<string, GroupSuspicion> _groupSuspicions = new Dictionary<string, GroupSuspicion>();
        private RiskManagementConfigSO _config;
        
        public SuspicionTracker(RiskManagementConfigSO config)
        {
            _config = config;
        }
        
        public void UpdateSuspicion(string characterId, float change, string source, string context = "")
        {
            EnsureCharacterSuspicion(characterId);
            
            CharacterSuspicion suspicion = _individualSuspicions[characterId];
            float oldValue = suspicion.currentSuspicion;
            
            // Apply change and clamp
            suspicion.currentSuspicion = Mathf.Clamp01(suspicion.currentSuspicion + change);
            suspicion.lastUpdateTime = DateTime.Now;
            
            // Record in history
            suspicion.suspicionHistory.Add(new SuspicionRecord
            {
                suspicionChange = change,
                sourceAction = source,
                context = context
            });
            
            // Trim history if needed
            const int maxHistorySize = 30;
            if (suspicion.suspicionHistory.Count > maxHistorySize)
            {
                suspicion.suspicionHistory = suspicion.suspicionHistory
                    .OrderByDescending(s => s.timestamp)
                    .Take(maxHistorySize)
                    .ToList();
            }
            
            // Create and publish event
            var evt = new SuspicionChangedEvent
            {
                TargetID = characterId,
                OldValue = oldValue,
                NewValue = suspicion.currentSuspicion,
                SourceAction = source
            };
            
            RiskManager.Instance.eventBus.Publish(evt);
        }
        
        public float GetSuspicionLevel(string characterId)
        {
            EnsureCharacterSuspicion(characterId);
            return _individualSuspicions[characterId].currentSuspicion;
        }
        
        public void ProcessDecay(float deltaTime)
        {
            // Convert deltaTime to hours for decay calculation
            float hoursElapsed = deltaTime / 3600.0f;
            
            foreach (var suspicion in _individualSuspicions.Values)
            {
                if (suspicion.currentSuspicion > _config.suspicionDecay.minimumSuspicionThreshold)
                {
                    float decayRate = _config.suspicionDecay.GetDecayRate(suspicion.characterID, suspicion.currentSuspicion);
                    float decayAmount = decayRate * hoursElapsed;
                    
                    // Don't publish event for minor decay
                    float oldValue = suspicion.currentSuspicion;
                    suspicion.currentSuspicion = Mathf.Max(_config.suspicionDecay.minimumSuspicionThreshold, 
                                                         suspicion.currentSuspicion - decayAmount);
                    
                    // Only publish significant changes
                    if (oldValue - suspicion.currentSuspicion > 0.01f)
                    {
                        var evt = new SuspicionChangedEvent
                        {
                            TargetID = suspicion.characterID,
                            OldValue = oldValue,
                            NewValue = suspicion.currentSuspicion,
                            SourceAction = "Decay"
                        };
                        
                        RiskManager.Instance.eventBus.Publish(evt);
                    }
                }
            }
            
            // Process group suspicion afterwards
            UpdateGroupSuspicions();
        }
        
        public void PropagateToGroup(string characterId, string groupId, float suspicionChange)
        {
            if (!_groupSuspicions.ContainsKey(groupId))
            {
                _groupSuspicions[groupId] = new GroupSuspicion(groupId);
            }
            
            GroupSuspicion group = _groupSuspicions[groupId];
            
            // Update member contribution
            if (!group.memberContributions.ContainsKey(characterId))
            {
                group.memberContributions[characterId] = 0f;
            }
            
            group.memberContributions[characterId] = Mathf.Clamp01(
                group.memberContributions[characterId] + 
                suspicionChange * group.spreadModel.baseSpreadRate);
            
            // Recalculate average suspicion
            UpdateGroupSuspicion(groupId);
        }
        
        private void UpdateGroupSuspicion(string groupId)
        {
            if (!_groupSuspicions.ContainsKey(groupId))
                return;
            
            GroupSuspicion group = _groupSuspicions[groupId];
            float oldAverage = group.averageSuspicion;
            
            // Calculate weighted average
            float sum = 0f;
            float weightSum = 0f;
            
            foreach (var member in group.memberContributions)
            {
                float weight = 1.0f;
                sum += member.Value * weight;
                weightSum += weight;
            }
            
            if (weightSum > 0)
            {
                group.averageSuspicion = sum / weightSum;
                
                // Publish event for significant changes
                if (Mathf.Abs(group.averageSuspicion - oldAverage) > 0.01f)
                {
                    var evt = new SuspicionChangedEvent
                    {
                        TargetID = "Group:" + groupId,
                        OldValue = oldAverage,
                        NewValue = group.averageSuspicion,
                        SourceAction = "Group Propagation"
                    };
                    
                    RiskManager.Instance.eventBus.Publish(evt);
                }
            }
        }
        
        private void UpdateGroupSuspicions()
        {
            foreach (var groupId in _groupSuspicions.Keys.ToList())
            {
                UpdateGroupSuspicion(groupId);
            }
        }
        
        private void EnsureCharacterSuspicion(string characterId)
        {
            if (!_individualSuspicions.ContainsKey(characterId))
            {
                _individualSuspicions[characterId] = new CharacterSuspicion(characterId);
            }
        }
    }

    public class CrisisGenerator
    {
        private CrisisTemplateSO _templates;
        private RiskManagementConfigSO _config;
        private List<ActiveCrisis> _activeCrises = new List<ActiveCrisis>();
        private Dictionary<string, List<CrisisHistory>> _crisisHistory = new Dictionary<string, List<CrisisHistory>>();
        
        public CrisisGenerator(CrisisTemplateSO templates, RiskManagementConfigSO config)
        {
            _templates = templates;
            _config = config;
        }
        
        public void CheckForCrisisTrigger(string characterId, float suspicionLevel, List<RiskCategory> categories = null)
        {
            // Check each crisis template to see if it should be triggered
            foreach (var template in _templates.templates)
            {
                // Skip if a crisis of this type is already active for this character
                if (_activeCrises.Any(c => c.templateID == template.crisisID && 
                                         c.involvedEntities.Contains(characterId) &&
                                         c.status == CrisisStatus.Active))
                {
                    continue;
                }
                
                // Check each potential trigger
                foreach (var trigger in template.possibleTriggers)
                {
                    // Check suspicion threshold
                    if (suspicionLevel < trigger.suspicionThreshold)
                        continue;
                    
                    // Check relevant categories if specified
                    if (categories != null && categories.Count > 0 && trigger.relevantCategories.Count > 0)
                    {
                        bool categoryMatch = false;
                        foreach (var category in categories)
                        {
                            if (trigger.relevantCategories.Contains(category))
                            {
                                categoryMatch = true;
                                break;
                            }
                        }
                        
                        if (!categoryMatch)
                            continue;
                    }
                    
                    // All conditions met, generate the crisis
                    GenerateCrisis(template, trigger, characterId, suspicionLevel);
                    
                    // Only generate one crisis per check
                    return;
                }
            }
        }
        
        private void GenerateCrisis(CrisisTemplate template, CrisisTrigger trigger, string characterId, float suspicionLevel)
        {
            // Create new crisis instance
            ActiveCrisis crisis = new ActiveCrisis
            {
                instanceID = Guid.NewGuid().ToString(),
                templateID = template.crisisID,
                currentStageIndex = 0,
                progressWithinStage = 0f,
                startTime = DateTime.Now,
                estimatedResolutionDeadline = DateTime.Now.AddHours(_config.defaultCrisisDuration),
                involvedEntities = new List<string> { characterId },
                status = CrisisStatus.Active
            };
            
            // Set initial exposure level
            crisis.entityExposureLevels[characterId] = suspicionLevel;
            
            // Register the crisis
            _activeCrises.Add(crisis);
            
            // Create and publish event
            var evt = new CrisisTriggeredEvent
            {
                CrisisID = crisis.instanceID,
                CrisisName = template.name,
                CrisisType = template.type,
                InvolvedEntities = new List<string>(crisis.involvedEntities),
                Deadline = crisis.estimatedResolutionDeadline
            };
            
            // Get the event bus from the RiskManager instance
            var eventBus = RiskManager.Instance.GetComponent<RiskManager>().eventBus;
            eventBus.Publish(evt);
            
            if (_config.enableDebugLogging)
            {
                Debug.Log($"Crisis '{template.name}' generated for {characterId} with suspicion {suspicionLevel}");
            }
        }
        
        public ActiveCrisis GetCrisis(string crisisId)
        {
            return _activeCrises.Find(c => c.instanceID == crisisId);
        }
        
        public List<ActiveCrisis> GetActiveCrises()
        {
            return new List<ActiveCrisis>(_activeCrises.Where(c => c.status == CrisisStatus.Active));
        }
        
        public List<ActiveCrisis> GetActiveCrisesForEntity(string entityId)
        {
            return _activeCrises.Where(c => c.status == CrisisStatus.Active && 
                                        c.involvedEntities.Contains(entityId)).ToList();
        }
        
        public void ResolveCrisis(string crisisId, bool successful, List<string> resolutionMethods, string outcome)
        {
            ActiveCrisis crisis = GetCrisis(crisisId);
            if (crisis == null)
                return;
            
            // Update crisis status
            crisis.status = successful ? CrisisStatus.Resolved : CrisisStatus.Failed;
            
            // Create history record
            CrisisTemplate template = _templates.GetTemplate(crisis.templateID);
            CrisisHistory history = new CrisisHistory
            {
                crisisID = crisis.instanceID,
                crisisName = template?.name ?? "Unknown Crisis",
                crisisType = template?.type ?? CrisisType.Any,
                startTime = crisis.startTime,
                endTime = DateTime.Now,
                finalStatus = crisis.status,
                involvedEntities = new List<string>(crisis.involvedEntities),
                resolutionsMade = resolutionMethods
            };
            
            // Store history for each involved entity
            foreach (var entity in crisis.involvedEntities)
            {
                if (!_crisisHistory.ContainsKey(entity))
                {
                    _crisisHistory[entity] = new List<CrisisHistory>();
                }
                
                _crisisHistory[entity].Add(history);
            }
            
            // Create and publish event
            var evt = new CrisisResolvedEvent
            {
                CrisisID = crisis.instanceID,
                Successful = successful,
                ResolutionMethods = resolutionMethods,
                OutcomeDescription = outcome
            };
            
            // Get the event bus from the RiskManager instance
            var eventBus = RiskManager.Instance.GetComponent<RiskManager>().eventBus;
            eventBus.Publish(evt);
        }
        
        public List<CrisisHistory> GetEntityCrisisHistory(string entityId)
        {
            if (!_crisisHistory.ContainsKey(entityId))
                return new List<CrisisHistory>();
            
            return _crisisHistory[entityId];
        }
    }

    public class CrisisTimeline
    {
        private CrisisGenerator _crisisGenerator;
        private RiskManagementConfigSO _config;
        private Dictionary<string, float> _crisisProgressionTimers = new Dictionary<string, float>();
        
        public CrisisTimeline(RiskManagementConfigSO config)
        {
            _config = config;
            _crisisGenerator = RiskManager.Instance.GetComponent<RiskManager>().GetCrisisGenerator();
        }
        
        public void RegisterCrisis(string crisisId)
        {
            if (!_crisisProgressionTimers.ContainsKey(crisisId))
            {
                _crisisProgressionTimers[crisisId] = 0f;
            }
        }
        
        public void UnregisterCrisis(string crisisId)
        {
            if (_crisisProgressionTimers.ContainsKey(crisisId))
            {
                _crisisProgressionTimers.Remove(crisisId);
            }
        }
        
        public void UpdateCrisisProgression(float deltaTime)
        {
            // Convert deltaTime to hours for progression
            float hoursElapsed = deltaTime / 3600.0f;
            
            foreach (var crisisId in _crisisProgressionTimers.Keys.ToList())
            {
                ActiveCrisis crisis = _crisisGenerator.GetCrisis(crisisId);
                if (crisis == null || crisis.status != CrisisStatus.Active)
                {
                    _crisisProgressionTimers.Remove(crisisId);
                    continue;
                }
                
                // Get template for crisis
                CrisisTemplate template = RiskManager.Instance.GetCrisisTemplate(crisis.templateID);
                if (template == null)
                    continue;
                
                // Update timer
                _crisisProgressionTimers[crisisId] += hoursElapsed;
                
                // Calculate progression
                float progressionPerHour = _config.crisisStageProgression;
                float progression = progressionPerHour * hoursElapsed;
                
                // Update crisis progression
                UpdateCrisisStage(crisis, template, progression);
                
                // Check for expiration
                if (DateTime.Now > crisis.estimatedResolutionDeadline && crisis.status == CrisisStatus.Active)
                {
                    // Crisis expired without resolution
                    crisis.status = CrisisStatus.Expired;
                    
                    // Create and publish event
                    var evt = new CrisisResolvedEvent
                    {
                        CrisisID = crisis.instanceID,
                        Successful = false,
                        ResolutionMethods = new List<string>(),
                        OutcomeDescription = "Crisis expired without resolution"
                    };
                    
                    // Get the event bus from the RiskManager instance
                    var eventBus = RiskManager.Instance.GetComponent<RiskManager>().eventBus;
                    eventBus.Publish(evt);
                }
            }
        }
        
        private void UpdateCrisisStage(ActiveCrisis crisis, CrisisTemplate template, float progression)
        {
            // Get current stage
            if (template.stages.Count <= crisis.currentStageIndex)
                return;
            
            CrisisStage currentStage = template.stages[crisis.currentStageIndex];
            
            // Update progress within stage
            float oldProgress = crisis.progressWithinStage;
            crisis.progressWithinStage += progression;
            
            // Check if moved to next stage
            if (crisis.progressWithinStage >= 1.0f && crisis.currentStageIndex < template.stages.Count - 1)
            {
                int oldStage = crisis.currentStageIndex;
                crisis.currentStageIndex++;
                crisis.progressWithinStage = 0f;
                
                // Get new stage
                CrisisStage newStage = template.stages[crisis.currentStageIndex];
                
                // Create and publish event
                var evt = new CrisisStageChangedEvent
                {
                    CrisisID = crisis.instanceID,
                    OldStage = oldStage,
                    NewStage = crisis.currentStageIndex,
                    StageDescription = newStage.description
                };
                
                // Get the event bus from the RiskManager instance
                var eventBus = RiskManager.Instance.GetComponent<RiskManager>().eventBus;
                eventBus.Publish(evt);
            }
        }
        
        public List<ActiveCrisis> GetActiveCrises()
        {
            return _crisisGenerator.GetActiveCrises();
        }
        
        public ActiveCrisis GetCrisis(string crisisId)
        {
            return _crisisGenerator.GetCrisis(crisisId);
        }
    }

    public class ResolutionHandler
    {
        public bool AttemptResolution(string crisisId, string resolutionId, Dictionary<string, float> parameters = null)
        {
            ActiveCrisis crisis = RiskManager.Instance.GetCrisis(crisisId);
            if (crisis == null || crisis.status != CrisisStatus.Active)
                return false;
            
            // Get crisis template
            CrisisTemplate template = RiskManager.Instance.GetCrisisTemplate(crisis.templateID);
            if (template == null)
                return false;
            
            // Find the resolution
            CrisisResolution resolution = template.possibleResolutions.Find(r => r.resolutionID == resolutionId);
            if (resolution == null)
                return false;
            
            // Check if available at current stage
            CrisisStage currentStage = template.stages[crisis.currentStageIndex];
            if (!currentStage.availableResolutions.Contains(resolutionId))
                return false;
            
            // Calculate success probability
            float successProbability = CalculateSuccessProbability(resolution, parameters);
            
            // Create resolution attempt record
            ActiveResolutionAttempt attempt = new ActiveResolutionAttempt
            {
                resolutionID = resolutionId,
                attemptTime = DateTime.Now,
                successProbability = successProbability,
                contributingFactors = parameters ?? new Dictionary<string, float>()
            };
            
            // Determine outcome
            bool isSuccessful = UnityEngine.Random.value < successProbability;
            attempt.isSuccessful = isSuccessful;
            
            // Select outcome
            ResolutionOutcome outcome = SelectOutcome(resolution, isSuccessful);
            if (outcome != null)
            {
                attempt.resultOutcomeID = outcome.outcomeID;
            }
            
            // Add to crisis
            crisis.resolutionAttempts.Add(attempt);
            
            // If successful and the resolution is sufficient, resolve the crisis
            if (isSuccessful)
            {
                RiskManager.Instance.CrisisGenerator.ResolveCrisis(
                    crisisId, 
                    true, 
                    new List<string> { resolutionId }, 
                    outcome?.description ?? "Crisis resolved successfully");
            }
            
            return isSuccessful;
        }
        
        public List<CrisisResolution> GetAvailableResolutions(string crisisId)
        {
            ActiveCrisis crisis = RiskManager.Instance.GetCrisis(crisisId);
            if (crisis == null || crisis.status != CrisisStatus.Active)
                return new List<CrisisResolution>();
            
            // Get crisis template
            CrisisTemplate template = RiskManager.Instance.GetCrisisTemplate(crisis.templateID);
            if (template == null)
                return new List<CrisisResolution>();
            
            // Get current stage
            if (template.stages.Count <= crisis.currentStageIndex)
                return new List<CrisisResolution>();
            
            CrisisStage currentStage = template.stages[crisis.currentStageIndex];
            
            // Return available resolutions
            return template.possibleResolutions
                .Where(r => currentStage.availableResolutions.Contains(r.resolutionID))
                .ToList();
        }
        
        private float CalculateSuccessProbability(CrisisResolution resolution, Dictionary<string, float> parameters)
        {
            float probability = resolution.baseSuccessRate;
            
            // Apply modifiers based on parameters
            if (parameters != null)
            {
                foreach (var modifier in resolution.modifiers)
                {
                    if (parameters.ContainsKey(modifier.contextualFactor))
                    {
                        probability += modifier.valueChange * parameters[modifier.contextualFactor];
                    }
                    
                    if (!string.IsNullOrEmpty(modifier.requiredItem) && 
                        parameters.ContainsKey("item:" + modifier.requiredItem))
                    {
                        probability += modifier.valueChange;
                    }
                }
            }
            
            // Clamp probability
            return Mathf.Clamp01(probability);
        }
        
        private ResolutionOutcome SelectOutcome(CrisisResolution resolution, bool isSuccessful)
        {
            var eligibleOutcomes = resolution.possibleOutcomes
                .Where(o => (isSuccessful && o.probabilityWeight > 0) || 
                           (!isSuccessful && o.probabilityWeight < 0))
                .ToList();
            
            if (eligibleOutcomes.Count == 0)
                return null;
            
            // Calculate absolute weights
            float totalWeight = eligibleOutcomes.Sum(o => Mathf.Abs(o.probabilityWeight));
            if (totalWeight <= 0)
                return eligibleOutcomes[0];
            
            // Select weighted random outcome
            float random = UnityEngine.Random.Range(0, totalWeight);
            float cumulative = 0;
            
            foreach (var outcome in eligibleOutcomes)
            {
                cumulative += Mathf.Abs(outcome.probabilityWeight);
                if (random <= cumulative)
                {
                    return outcome;
                }
            }
            
            // Fallback
            return eligibleOutcomes[eligibleOutcomes.Count - 1];
        }
    }

    public class RiskFeedback
    {
        private Dictionary<string, float> _lastDisplayedRisks = new Dictionary<string, float>();
        private Dictionary<string, float> _lastDisplayedSuspicions = new Dictionary<string, float>();
        
        // Default threshold values that could be customized
        private float _minorRiskThreshold = 0.3f;
        private float _moderateRiskThreshold = 0.6f;
        private float _highRiskThreshold = 0.8f;
        
        private float _minorSuspicionThreshold = 0.3f;
        private float _moderateSuspicionThreshold = 0.6f;
        private float _criticalSuspicionThreshold = 0.8f;
        
        public void DisplayRiskLevel(ActionType actionType, float riskValue)
        {
            string actionKey = actionType.ToString();
            
            // Only display significant changes to reduce spam
            if (_lastDisplayedRisks.ContainsKey(actionKey) && 
                Mathf.Abs(_lastDisplayedRisks[actionKey] - riskValue) < 0.1f)
            {
                return;
            }
            
            _lastDisplayedRisks[actionKey] = riskValue;
            
            // Get risk level description
            string riskLevel = GetRiskLevelDescription(riskValue);
            
            // Display to UI (example implementation)
            // This would be connected to the game's UI system
            Debug.Log($"Risk Level for {actionType}: {riskLevel} ({riskValue:F2})");
            
            // Here you would implement UI feedback
            // Example:
            // UIManager.Instance.UpdateRiskIndicator(actionType, riskValue, riskLevel);
        }
        
        public void UpdateSuspicionDisplay(string entityId, float suspicionValue)
        {
            // Only display significant changes to reduce spam
            if (_lastDisplayedSuspicions.ContainsKey(entityId) && 
                Mathf.Abs(_lastDisplayedSuspicions[entityId] - suspicionValue) < 0.1f)
            {
                return;
            }
            
            _lastDisplayedSuspicions[entityId] = suspicionValue;
            
            // Get suspicion level description
            string suspicionLevel = GetSuspicionLevelDescription(suspicionValue);
            
            // Display to UI (example implementation)
            Debug.Log($"Suspicion Level for {entityId}: {suspicionLevel} ({suspicionValue:F2})");
            
            // Here you would implement UI feedback
            // Example:
            // UIManager.Instance.UpdateSuspicionIndicator(entityId, suspicionValue, suspicionLevel);
        }
        
        public void DisplayCrisisNotification(string crisisName, CrisisType crisisType, DateTime deadline)
        {
            // Display to UI (example implementation)
            Debug.Log($"CRISIS ALERT: {crisisName} ({crisisType}) - Resolve by {deadline.ToString("g")}");
            
            // Here you would implement UI notification
            // Example:
            // UIManager.Instance.ShowCrisisNotification(crisisName, crisisType, deadline);
        }
        
        public void UpdateCrisisDisplay(string crisisId, int stage, string stageDescription)
        {
            // Display to UI (example implementation)
            Debug.Log($"Crisis {crisisId} - Stage {stage + 1}: {stageDescription}");
            
            // Here you would implement UI update
            // Example:
            // UIManager.Instance.UpdateCrisisStage(crisisId, stage, stageDescription);
        }
        
        public void DisplayResolutionOutcome(string crisisId, bool successful, string outcomeDescription)
        {
            // Display to UI (example implementation)
            if (successful)
            {
                Debug.Log($"Crisis {crisisId} RESOLVED: {outcomeDescription}");
            }
            else
            {
                Debug.Log($"Crisis {crisisId} FAILED: {outcomeDescription}");
            }
            
            // Here you would implement UI notification
            // Example:
            // UIManager.Instance.ShowResolutionOutcome(crisisId, successful, outcomeDescription);
        }
        
        private string GetRiskLevelDescription(float riskValue)
        {
            if (riskValue < _minorRiskThreshold)
                return "Low Risk";
            if (riskValue < _moderateRiskThreshold)
                return "Moderate Risk";
            if (riskValue < _highRiskThreshold)
                return "High Risk";
            return "Extreme Risk";
        }
        
        private string GetSuspicionLevelDescription(float suspicionValue)
        {
            if (suspicionValue < _minorSuspicionThreshold)
                return "Unnoticed";
            if (suspicionValue < _moderateSuspicionThreshold)
                return "Suspicious";
            if (suspicionValue < _criticalSuspicionThreshold)
                return "Highly Suspicious";
            return "Critical Alert";
        }
    }

    #endregion
}