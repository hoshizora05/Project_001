using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceManagement
{
    /// <summary>
    /// ScriptableObject that holds all configuration for the Resource Management System
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceSystemConfig", menuName = "Systems/Resource System Config")]
    public class ResourceSystemConfig : ScriptableObject
    {
        [Header("System Settings")]
        public bool enableResourceLogging = true;
        public bool enableOptimizationSuggestions = true;
        public float optimizationSuggestionInterval = 60f; // seconds
        
        [Header("Subsystem Configurations")]
        public CurrencyConfig currencyConfig;
        public InventoryConfig inventoryConfig;
        public TimeConfig timeConfig;
        public ConversionConfig conversionConfig;
        public GenerationConfig generationConfig;
        public StorageConfig storageConfig;
        
        #region Currency Configuration
        [Serializable]
        public class CurrencyConfig
        {
            [Header("Currencies")]
            public List<CurrencyDefinition> availableCurrencies = new List<CurrencyDefinition>();
            
            [Header("Economy Settings")]
            public float baseTransactionTax = 0.02f;
            public float economyUpdateInterval = 24f; // in game hours
            public bool enableInflation = true;
            public bool enableMarketFluctuation = true;
            
            [Header("Investment")]
            public List<InvestmentOptionDefinition> investmentOptions = new List<InvestmentOptionDefinition>();
        }
        
        [Serializable]
        public class CurrencyDefinition
        {
            public string currencyId;
            public string name;
            public CurrencyType type;
            public float startingAmount;
            public float maxCapacity;
            public CurrencyProperties properties;
            public Sprite icon;
            
            [TextArea(2, 4)]
            public string description;
        }
        
        [Serializable]
        public class InvestmentOptionDefinition
        {
            public string investmentId;
            public string name;
            public float minInvestmentAmount;
            public float baseReturnRate;
            public float riskFactor;
            public int durationDays;
            
            [TextArea(2, 4)]
            public string description;
            
            public List<InvestmentOutcomeDefinition> possibleOutcomes = new List<InvestmentOutcomeDefinition>();
        }
        
        [Serializable]
        public class InvestmentOutcomeDefinition
        {
            public string outcomeId;
            public string description;
            public float returnMultiplier;
            public float probability;
        }
        #endregion
        
        #region Inventory Configuration
        [Serializable]
        public class InventoryConfig
        {
            [Header("Containers")]
            public List<ContainerDefinition> defaultContainers = new List<ContainerDefinition>();
            
            [Header("Inventory Settings")]
            public float baseWeightCapacity = 50f;
            public int baseSlotCapacity = 20;
            public bool enableItemDegradation = true;
            public bool enableItemExpiration = true;
            public bool restrictEquipmentByLevel = true;
            
            [Header("Item Database")]
            public string itemDatabasePath = "Data/ItemDatabase";
            public bool useExternalItemDatabase = false;
        }
        
        [Serializable]
        public class ContainerDefinition
        {
            public string containerId;
            public string name;
            public float maxWeight;
            public int maxSlots;
            public ContainerUISettings uiSettings;
            public ContainerAccessRestrictions accessRestrictions;
            
            [Header("Slot Restrictions")]
            public bool useSlotRestrictions;
            public List<SlotRestrictionGroup> slotRestrictionGroups = new List<SlotRestrictionGroup>();
        }
        
        [Serializable]
        public class SlotRestrictionGroup
        {
            public int startSlotIndex;
            public int endSlotIndex;
            public SlotRestrictions restrictions;
        }
        #endregion
        
        #region Time Configuration
        [Serializable]
        public class TimeConfig
        {
            [Header("Time Settings")]
            public float gameTimeToRealTimeRatio = 60f; // 1 real second = 60 game seconds (1 minute)
            public int startingDay = 1;
            public DayOfWeek startingDayOfWeek = DayOfWeek.Monday;
            public float startingHour = 8f; // 8 AM
            
            [Header("Time Blocks")]
            public List<TimeBlockTemplate> timeBlockTemplates = new List<TimeBlockTemplate>();
            
            [Header("Action Time Requirements")]
            public List<ActionTimeRequirementDefinition> actionTimeRequirements = new List<ActionTimeRequirementDefinition>();
            
            [Header("Time Control")]
            public bool pauseTimeWhenInMenu = true;
            public bool enableFastForward = true;
            public float[] fastForwardSpeeds = { 2f, 5f, 10f };
        }
        
        [Serializable]
        public class TimeBlockTemplate
        {
            public string id;
            public string name;
            public float startHour;
            public float endHour;
            
            [TextArea(1, 3)]
            public string description;
        }
        
        [Serializable]
        public class ActionTimeRequirementDefinition
        {
            public string actionId;
            public string actionName;
            public float baseTimeCost;
            public bool isScalableBySkill;
            public List<SkillEfficiencyDefinition> skillEfficiencyFactors = new List<SkillEfficiencyDefinition>();
            public bool requiresContinuousTime;
            public bool canBeInterrupted;
            
            [TextArea(1, 3)]
            public string description;
        }
        
        [Serializable]
        public class SkillEfficiencyDefinition
        {
            public string skillId;
            public float efficiencyFactor;
        }
        #endregion
        
        #region Conversion Configuration
        [Serializable]
        public class ConversionConfig
        {
            [Header("Conversion Settings")]
            public List<ConversionDefinition> availableConversions = new List<ConversionDefinition>();
            public float baseConversionEfficiency = 0.9f;
            public bool enableSkillEfficiency = true;
            public bool enableLocationEfficiency = true;
            
            [Header("Conversion UI")]
            public bool showConversionPreview = true;
            public bool highlightProfitableConversions = true;
        }
        
        [Serializable]
        public class ConversionDefinition
        {
            public string conversionId;
            public ResourceType fromType;
            public string fromId;
            public ResourceType toType;
            public string toId;
            public float baseRate;
            public List<string> requirements = new List<string>();
            
            [TextArea(1, 3)]
            public string description;
        }
        #endregion
        
        #region Generation Configuration
        [Serializable]
        public class GenerationConfig
        {
            [Header("Generation Settings")]
            public List<GenerationSourceDefinition> defaultSources = new List<GenerationSourceDefinition>();
            public float baseGenerationUpdateInterval = 1f; // in game hours
            public bool enablePassiveGeneration = true;
            public bool applyTimeOfDayEfficiency = true;
            
            [Header("Generation Limits")]
            public bool enableDailyGenerationCaps = false;
            public bool enableGlobalGenerationLimit = false;
            public float globalGenerationCapacity = 1000f;
        }
        
        [Serializable]
        public class GenerationSourceDefinition
        {
            public string sourceId;
            public string sourceName;
            public ResourceType resourceType;
            public string resourceId;
            public float baseGenerationRate;
            public float initialEfficiency = 1f;
            public bool activeByDefault = true;
            public GenerationSchedule defaultSchedule;
            public List<GenerationUpgrade> upgrades = new List<GenerationUpgrade>();
            
            [TextArea(1, 3)]
            public string description;
        }
        #endregion
        
        #region Storage Configuration
        [Serializable]
        public class StorageConfig
        {
            [Header("Storage Settings")]
            public float baseTotalCapacity = 1000f;
            public List<StorageMethodDefinition> availableMethods = new List<StorageMethodDefinition>();
            public float baseItemDeteriorationRate = 0.001f; // per game hour
            public bool enableQualityDeterioration = true;
            public bool enableCapacityUpgrades = true;
            
            [Header("Storage Organization")]
            public bool enableAutomaticSorting = false;
            public bool enableItemCategories = true;
            public int defaultCategoryCapacity = 100;
        }
        
        [Serializable]
        public class StorageMethodDefinition
        {
            public string methodId;
            public string methodName;
            public float deteriorationMultiplier;
            public float capacityMultiplier;
            public float energyCost;
            public float maintenanceCost;
            public bool availableByDefault = true;
            public List<string> requirements = new List<string>();
            
            [TextArea(1, 3)]
            public string description;
        }
        #endregion
        
        #region Runtime Helper Methods
        /// <summary>
        /// Creates a simple default configuration for testing
        /// </summary>
        public static ResourceSystemConfig CreateDefaultConfig()
        {
            var config = CreateInstance<ResourceSystemConfig>();
            
            // Initialize currency config
            config.currencyConfig = new CurrencyConfig();
            config.currencyConfig.availableCurrencies.Add(new CurrencyDefinition
            {
                currencyId = "standard_currency",
                name = "Gold",
                type = CurrencyType.StandardCurrency,
                startingAmount = 1000f,
                maxCapacity = 0f, // Unlimited
                properties = new CurrencyProperties
                {
                    isMainCurrency = true,
                    isTradeableBetweenPlayers = true,
                    deterioratesOverTime = false,
                    canBeNegative = false,
                    interestRate = 0f
                },
                description = "Standard currency used for most transactions"
            });
            
            config.currencyConfig.availableCurrencies.Add(new CurrencyDefinition
            {
                currencyId = "premium_currency",
                name = "Gems",
                type = CurrencyType.PremiumCurrency,
                startingAmount = 50f,
                maxCapacity = 0f, // Unlimited
                properties = new CurrencyProperties
                {
                    isMainCurrency = false,
                    isTradeableBetweenPlayers = false,
                    deterioratesOverTime = false,
                    canBeNegative = false,
                    interestRate = 0f
                },
                description = "Premium currency used for special purchases"
            });
            
            // Initialize inventory config
            config.inventoryConfig = new InventoryConfig();
            config.inventoryConfig.defaultContainers.Add(new ContainerDefinition
            {
                containerId = "main_inventory",
                name = "Inventory",
                maxWeight = 50f,
                maxSlots = 20,
                uiSettings = new ContainerUISettings
                {
                    displayName = "Inventory",
                    containerColor = Color.white,
                    isVisible = true,
                    displayOrder = 0,
                    showInHUD = true
                },
                accessRestrictions = new ContainerAccessRestrictions()
            });
            
            // Initialize time config
            config.timeConfig = new TimeConfig();
            config.timeConfig.timeBlockTemplates.Add(new TimeBlockTemplate
            {
                id = "morning",
                name = "Morning",
                startHour = 6f,
                endHour = 12f,
                description = "Morning time block"
            });
            
            config.timeConfig.timeBlockTemplates.Add(new TimeBlockTemplate
            {
                id = "afternoon",
                name = "Afternoon",
                startHour = 12f,
                endHour = 18f,
                description = "Afternoon time block"
            });
            
            config.timeConfig.timeBlockTemplates.Add(new TimeBlockTemplate
            {
                id = "evening",
                name = "Evening",
                startHour = 18f,
                endHour = 22f,
                description = "Evening time block"
            });
            
            config.timeConfig.timeBlockTemplates.Add(new TimeBlockTemplate
            {
                id = "night",
                name = "Night",
                startHour = 22f,
                endHour = 6f,
                description = "Night time block"
            });
            
            // Initialize conversion config
            config.conversionConfig = new ConversionConfig();
            
            // Initialize generation config
            config.generationConfig = new GenerationConfig();
            
            // Initialize storage config
            config.storageConfig = new StorageConfig();
            
            return config;
        }
        #endregion
    }
}