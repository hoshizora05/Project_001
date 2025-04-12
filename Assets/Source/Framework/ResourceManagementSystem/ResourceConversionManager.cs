using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ResourceManagement
{
    /// <summary>
    /// Manages the conversion of resources from one type to another
    /// </summary>
    public class ResourceConversionManager : IResourceConversionSystem
    {
        #region Private Fields
        private string _playerId;
        private ResourceSystemConfig.ConversionConfig _config;
        private Dictionary<string, ResourceConversionOption> _conversionOptions = new Dictionary<string, ResourceConversionOption>();
        private List<ResourceConversion> _recentConversions = new List<ResourceConversion>();
        private Dictionary<string, float> _conversionRateModifiers = new Dictionary<string, float>();
        private List<string> _unlockedConversions = new List<string>();
        
        // References to other resource systems (will be assigned during system initialization)
        private ICurrencySystem _currencySystem;
        private IInventorySystem _inventorySystem;
        #endregion

        #region Events
        public event Action<ResourceConversion> OnConversionProcessed;
        #endregion

        #region Constructor
        public ResourceConversionManager(ResourceSystemConfig.ConversionConfig config)
        {
            _config = config;
            InitializeConversionOptions();
        }
        #endregion

        #region Initialization Methods
        public void Initialize(string playerId)
        {
            _playerId = playerId;
            
            // Get references to other resource systems
            if (ServiceLocator.Exists<ICurrencySystem>())
            {
                _currencySystem = ServiceLocator.Get<ICurrencySystem>();
            }
            
            if (ServiceLocator.Exists<IInventorySystem>())
            {
                _inventorySystem = ServiceLocator.Get<IInventorySystem>();
            }
            
            // Initialize base conversion rates
            _conversionRateModifiers["default"] = 1.0f;
            _conversionRateModifiers["skill_bonus"] = 0.0f;
            _conversionRateModifiers["location_bonus"] = 0.0f;
            _conversionRateModifiers["equipment_bonus"] = 0.0f;
        }
        
        private void InitializeConversionOptions()
        {
            _conversionOptions.Clear();
            
            if (_config != null && _config.availableConversions != null)
            {
                foreach (var conversionDef in _config.availableConversions)
                {
                    string optionId = $"{conversionDef.fromType}_{conversionDef.fromId}_to_{conversionDef.toType}_{conversionDef.toId}";
                    
                    ResourceConversionOption option = new ResourceConversionOption
                    {
                        optionId = optionId,
                        toType = conversionDef.toType,
                        toId = conversionDef.toId,
                        baseConversionRate = conversionDef.baseRate,
                        minConversionAmount = 1.0f,
                        maxConversionAmount = float.MaxValue,
                        conversionFeePercentage = 0.05f, // 5% fee by default
                        requirements = new List<string>(conversionDef.requirements)
                    };
                    
                    _conversionOptions[optionId] = option;
                }
            }
            else
            {
                // Create some default conversion options if none are provided
                CreateDefaultConversionOptions();
            }
        }
        
        private void CreateDefaultConversionOptions()
        {
            // Currency to currency conversions
            AddCurrencyConversion(CurrencyType.StandardCurrency, CurrencyType.FactionCurrency, 0.5f);
            AddCurrencyConversion(CurrencyType.FactionCurrency, CurrencyType.StandardCurrency, 1.8f);
            AddCurrencyConversion(CurrencyType.StandardCurrency, CurrencyType.PremiumCurrency, 0.1f);
            AddCurrencyConversion(CurrencyType.PremiumCurrency, CurrencyType.StandardCurrency, 12.0f);
            
            // Item conversion options (materials to processed items)
            AddItemConversion("wood", "processed_wood", 0.8f);
            AddItemConversion("stone", "processed_stone", 0.7f);
            AddItemConversion("herb", "medicine", 0.5f);
            
            // Currency to item conversion (purchasing)
            AddCurrencyToItemConversion(CurrencyType.StandardCurrency, "basic_supplies", 0.2f);
            AddCurrencyToItemConversion(CurrencyType.PremiumCurrency, "premium_supplies", 0.5f);
            
            // Item to currency conversion (selling)
            AddItemToCurrencyConversion("processed_wood", CurrencyType.StandardCurrency, 15.0f);
            AddItemToCurrencyConversion("processed_stone", CurrencyType.StandardCurrency, 20.0f);
            AddItemToCurrencyConversion("medicine", CurrencyType.StandardCurrency, 35.0f);
        }
        
        private void AddCurrencyConversion(CurrencyType fromType, CurrencyType toType, float rate)
        {
            string optionId = $"{ResourceType.Currency}_{fromType}_to_{ResourceType.Currency}_{toType}";
            
            ResourceConversionOption option = new ResourceConversionOption
            {
                optionId = optionId,
                toType = ResourceType.Currency,
                toId = toType.ToString(),
                baseConversionRate = rate,
                minConversionAmount = 1.0f,
                maxConversionAmount = float.MaxValue,
                conversionFeePercentage = 0.05f, // 5% fee
                requirements = new List<string>()
            };
            
            _conversionOptions[optionId] = option;
        }
        
        private void AddItemConversion(string fromId, string toId, float rate)
        {
            string optionId = $"{ResourceType.Item}_{fromId}_to_{ResourceType.Item}_{toId}";
            
            ResourceConversionOption option = new ResourceConversionOption
            {
                optionId = optionId,
                toType = ResourceType.Item,
                toId = toId,
                baseConversionRate = rate,
                minConversionAmount = 1.0f,
                maxConversionAmount = float.MaxValue,
                conversionFeePercentage = 0.1f, // 10% fee (processing loss)
                requirements = new List<string>()
            };
            
            _conversionOptions[optionId] = option;
        }
        
        private void AddCurrencyToItemConversion(CurrencyType fromType, string toId, float rate)
        {
            string optionId = $"{ResourceType.Currency}_{fromType}_to_{ResourceType.Item}_{toId}";
            
            ResourceConversionOption option = new ResourceConversionOption
            {
                optionId = optionId,
                toType = ResourceType.Item,
                toId = toId,
                baseConversionRate = rate,
                minConversionAmount = 10.0f, // Minimum purchase amount
                maxConversionAmount = 1000.0f, // Maximum purchase amount
                conversionFeePercentage = 0.02f, // 2% transaction fee
                requirements = new List<string>()
            };
            
            _conversionOptions[optionId] = option;
        }
        
        private void AddItemToCurrencyConversion(string fromId, CurrencyType toType, float rate)
        {
            string optionId = $"{ResourceType.Item}_{fromId}_to_{ResourceType.Currency}_{toType}";
            
            ResourceConversionOption option = new ResourceConversionOption
            {
                optionId = optionId,
                toType = ResourceType.Currency,
                toId = toType.ToString(),
                baseConversionRate = rate,
                minConversionAmount = 1.0f, // Sell minimum 1 item
                maxConversionAmount = float.MaxValue,
                conversionFeePercentage = 0.08f, // 8% seller fee
                requirements = new List<string>()
            };
            
            _conversionOptions[optionId] = option;
        }
        #endregion

        #region Conversion Operations
        public bool ConvertResource(ResourceType fromType, string fromId, ResourceType toType, string toId, float amount)
        {
            // Validate amount
            if (amount <= 0)
                return false;
            
            // Get conversion option
            string optionId = $"{fromType}_{fromId}_to_{toType}_{toId}";
            if (!_conversionOptions.TryGetValue(optionId, out var option))
            {
                Debug.LogWarning($"No conversion option found for {fromType} {fromId} to {toType} {toId}");
                return false;
            }
            
            // Validate conversion requirements
            if (!MeetsConversionRequirements(option))
            {
                Debug.LogWarning($"Requirements not met for conversion {optionId}");
                return false;
            }
            
            // Validate amount limits
            if (amount < option.minConversionAmount || amount > option.maxConversionAmount)
            {
                Debug.LogWarning($"Amount {amount} is outside allowed range [{option.minConversionAmount}, {option.maxConversionAmount}]");
                return false;
            }
            
            // Check if source resource is available
            if (!HasEnoughResource(fromType, fromId, amount))
            {
                Debug.LogWarning($"Not enough resource {fromType} {fromId} for conversion");
                return false;
            }
            
            // Calculate conversion result
            float conversionRate = CalculateConversionRate(option);
            float resultAmount = amount * conversionRate;
            float fee = resultAmount * option.conversionFeePercentage;
            resultAmount -= fee;
            
            // Ensure minimum result
            if (resultAmount < 0.01f)
            {
                Debug.LogWarning($"Conversion would result in too small amount ({resultAmount})");
                return false;
            }
            
            // Process the conversion - remove source resource
            if (!RemoveResource(fromType, fromId, amount))
            {
                Debug.LogError($"Failed to remove source resource {fromType} {fromId}");
                return false;
            }
            
            // Add target resource
            if (!AddResource(toType, toId, resultAmount))
            {
                // If failed to add, restore the source resource
                AddResource(fromType, fromId, amount);
                Debug.LogError($"Failed to add target resource {toType} {toId}");
                return false;
            }
            
            // Record the conversion
            RecordConversion(fromType, fromId, amount, toType, toId, resultAmount, conversionRate);
            
            return true;
        }
        
        private bool HasEnoughResource(ResourceType type, string id, float amount)
        {
            switch (type)
            {
                case ResourceType.Currency:
                    if (_currencySystem == null)
                        return false;
                    
                    if (Enum.TryParse<CurrencyType>(id, out var currencyType))
                    {
                        return _currencySystem.GetCurrencyAmount(currencyType) >= amount;
                    }
                    return false;
                    
                case ResourceType.Item:
                    if (_inventorySystem == null)
                        return false;
                    
                    return _inventorySystem.GetItemQuantity(id) >= (int)amount;
                    
                default:
                    return false;
            }
        }
        
        private bool RemoveResource(ResourceType type, string id, float amount)
        {
            switch (type)
            {
                case ResourceType.Currency:
                    if (_currencySystem == null)
                        return false;
                    
                    if (Enum.TryParse<CurrencyType>(id, out var currencyType))
                    {
                        return _currencySystem.RemoveCurrency(currencyType, amount, "resource_conversion", "Resource conversion cost");
                    }
                    return false;
                    
                case ResourceType.Item:
                    if (_inventorySystem == null)
                        return false;
                    
                    return _inventorySystem.RemoveItem(id, (int)amount);
                    
                default:
                    return false;
            }
        }
        
        private bool AddResource(ResourceType type, string id, float amount)
        {
            switch (type)
            {
                case ResourceType.Currency:
                    if (_currencySystem == null)
                        return false;
                    
                    if (Enum.TryParse<CurrencyType>(id, out var currencyType))
                    {
                        return _currencySystem.AddCurrency(currencyType, amount, "resource_conversion", "Resource conversion result");
                    }
                    return false;
                    
                case ResourceType.Item:
                    if (_inventorySystem == null)
                        return false;
                    
                    return _inventorySystem.AddItem(id, (int)amount);
                    
                default:
                    return false;
            }
        }
        
        private void RecordConversion(ResourceType fromType, string fromId, float fromAmount, 
                                      ResourceType toType, string toId, float toAmount, float conversionRate)
        {
            ResourceConversion conversion = new ResourceConversion
            {
                conversionId = GenerateConversionId(),
                fromType = fromType,
                fromId = fromId,
                fromAmount = fromAmount,
                toType = toType,
                toId = toId,
                toAmount = toAmount,
                conversionEfficiency = conversionRate,
                timestamp = DateTime.Now
            };
            
            _recentConversions.Add(conversion);
            
            // Keep list at a reasonable size
            const int maxConversions = 20;
            if (_recentConversions.Count > maxConversions)
            {
                _recentConversions.RemoveAt(0);
            }
            
            // Trigger event
            OnConversionProcessed?.Invoke(conversion);
        }
        
        private string GenerateConversionId()
        {
            return $"conv_{DateTime.Now.Ticks}_{UnityEngine.Random.Range(1000, 9999)}";
        }
        #endregion

        #region Conversion Rate Calculation
        public float GetConversionRate(ResourceType fromType, string fromId, ResourceType toType, string toId)
        {
            string optionId = $"{fromType}_{fromId}_to_{toType}_{toId}";
            if (!_conversionOptions.TryGetValue(optionId, out var option))
            {
                return 0f; // No conversion available
            }
            
            return CalculateConversionRate(option);
        }
        
        private float CalculateConversionRate(ResourceConversionOption option)
        {
            float baseRate = option.baseConversionRate;
            
            // Apply efficiency multiplier from configuration
            float efficiencyMultiplier = _config.baseConversionEfficiency;
            
            // Apply skill efficiency if enabled
            if (_config.enableSkillEfficiency)
            {
                float skillBonus = _conversionRateModifiers.TryGetValue("skill_bonus", out float bonus) ? bonus : 0f;
                efficiencyMultiplier += skillBonus;
            }
            
            // Apply location efficiency if enabled
            if (_config.enableLocationEfficiency)
            {
                float locationBonus = _conversionRateModifiers.TryGetValue("location_bonus", out float bonus) ? bonus : 0f;
                efficiencyMultiplier += locationBonus;
            }
            
            // Calculate final rate
            float finalRate = baseRate * efficiencyMultiplier;
            
            // Ensure minimum conversion rate
            return Mathf.Max(0.01f, finalRate);
        }
        
        private bool MeetsConversionRequirements(ResourceConversionOption option)
        {
            if (!_unlockedConversions.Contains(option.optionId) && option.requirements.Count > 0)
            {
                // Check each requirement
                foreach (var requirement in option.requirements)
                {
                    // In a real implementation, this would check against player progress, skills, etc.
                    // For now, we'll just assume all requirements are met
                }
                
                return true;
            }
            
            return true; // No requirements or already unlocked
        }
        #endregion

        #region Public Interface Methods
        public List<ResourceConversionOption> GetConversionOptions(ResourceType fromType, string fromId)
        {
            return _conversionOptions.Values
                .Where(o => IsValidConversionSource(o, fromType, fromId))
                .ToList();
        }
        
        private bool IsValidConversionSource(ResourceConversionOption option, ResourceType fromType, string fromId)
        {
            // Extract the source information from the option ID
            string[] parts = option.optionId.Split('_');
            if (parts.Length < 4)
                return false;
            
            // Format should be "{fromType}_{fromId}_to_{toType}_{toId}"
            if (parts[0] == fromType.ToString() && parts[1] == fromId)
                return true;
            
            return false;
        }
        
        public bool IsConversionPossible(ResourceType fromType, string fromId, ResourceType toType, string toId)
        {
            string optionId = $"{fromType}_{fromId}_to_{toType}_{toId}";
            return _conversionOptions.ContainsKey(optionId) && 
                   MeetsConversionRequirements(_conversionOptions[optionId]);
        }
        
        public void SetConversionRateModifier(string modifierKey, float value)
        {
            _conversionRateModifiers[modifierKey] = value;
        }
        
        public void UnlockConversion(string conversionId)
        {
            if (!_unlockedConversions.Contains(conversionId))
            {
                _unlockedConversions.Add(conversionId);
            }
        }
        
        public List<ResourceOptimizationSuggestion> GetOptimizationSuggestions()
        {
            List<ResourceOptimizationSuggestion> suggestions = new List<ResourceOptimizationSuggestion>();
            
            // Find favorable conversion rates
            var favorableConversions = _conversionOptions.Values
                .Where(o => CalculateConversionRate(o) > 1.2f * o.baseConversionRate)
                .ToList();
            
            if (favorableConversions.Count > 0)
            {
                // Sort by benefit
                favorableConversions.Sort((a, b) => 
                    (CalculateConversionRate(b) / b.baseConversionRate)
                    .CompareTo(CalculateConversionRate(a) / a.baseConversionRate));
                
                var bestOption = favorableConversions[0];
                string[] sourceParts = bestOption.optionId.Split('_');
                
                if (sourceParts.Length >= 4)
                {
                    ResourceType fromType = (ResourceType)Enum.Parse(typeof(ResourceType), sourceParts[0]);
                    string fromId = sourceParts[1];
                    
                    suggestions.Add(new ResourceOptimizationSuggestion
                    {
                        suggestionId = $"favorable_conversion_{bestOption.optionId}",
                        title = "Favorable Conversion Rate",
                        description = $"Current conversion rate for {fromType} {fromId} to {bestOption.toType} {bestOption.toId} is exceptionally good (+" +
                                     $"{(CalculateConversionRate(bestOption) / bestOption.baseConversionRate - 1) * 100:F0}%). Consider converting resources now.",
                        potentialBenefit = CalculateConversionRate(bestOption) - bestOption.baseConversionRate,
                        primaryResourceType = fromType,
                        priority = 7,
                        actionSteps = new List<string>
                        {
                            $"Convert {fromType} {fromId} to {bestOption.toType} {bestOption.toId} while rates are favorable",
                            "Consider acquiring more source resources to maximize this opportunity",
                            "Check for additional favorable conversion chains"
                        }
                    });
                }
            }
            
            // Find conversion chains for profit
            List<string> profitableChains = FindProfitableConversionChains();
            if (profitableChains.Count > 0)
            {
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "conversion_chain_profit",
                    title = "Profitable Conversion Chain",
                    description = $"You can profit by following this conversion chain: {profitableChains[0]}",
                    potentialBenefit = 20f, // This would be calculated from the actual chain in a real implementation
                    primaryResourceType = ResourceType.Currency,
                    priority = 8,
                    actionSteps = profitableChains.Take(3).Select(chain => $"Follow conversion chain: {chain}").ToList()
                });
            }
            
            // Suggest improving conversion efficiency
            float currentEfficiency = _config.baseConversionEfficiency;
            if (currentEfficiency < 0.85f)
            {
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "improve_conversion_efficiency",
                    title = "Improve Conversion Efficiency",
                    description = $"Your current conversion efficiency is {currentEfficiency:P0}. " +
                                 "Improving related skills could significantly increase resource yields.",
                    potentialBenefit = 0.15f, // Potential efficiency gain
                    primaryResourceType = ResourceType.Item,
                    priority = 5,
                    actionSteps = new List<string>
                    {
                        "Train crafting or alchemy skills to improve conversion rates",
                        "Use specialized conversion equipment",
                        "Perform conversions in locations with efficiency bonuses"
                    }
                });
            }
            
            return suggestions;
        }
        
        private List<string> FindProfitableConversionChains()
        {
            // In a real implementation, this would analyze the conversion graph to find profitable cycles
            // For this example, we'll return a hard-coded example
            List<string> chains = new List<string>();
            
            chains.Add("StandardCurrency → raw_materials → processed_materials → PremiumCurrency → StandardCurrency (profit: ~8%)");
            
            return chains;
        }
        #endregion

        #region Action Processing
        public ActionValidationResult ValidateAction(string actionId)
        {
            // This would validate conversion-related actions
            // For this example, we'll just validate a few example actions
            switch (actionId)
            {
                case "currency_exchange":
                    // Validate currency exchange action
                    if (_currencySystem == null)
                    {
                        return new ActionValidationResult
                        {
                            isValid = false,
                            message = "Currency system not available for exchange"
                        };
                    }
                    return new ActionValidationResult { isValid = true };
                    
                case "process_materials":
                    // Validate material processing action
                    if (_inventorySystem == null)
                    {
                        return new ActionValidationResult
                        {
                            isValid = false,
                            message = "Inventory system not available for processing"
                        };
                    }
                    return new ActionValidationResult { isValid = true };
                    
                default:
                    // No conversion requirements for this action
                    return new ActionValidationResult { isValid = true };
            }
        }
        
        public void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch)
        {
            switch (actionId)
            {
                case "currency_exchange":
                    // Process currency exchange
                    if (parameters != null && 
                        parameters.TryGetValue("fromCurrency", out object fromCurrObj) &&
                        parameters.TryGetValue("toCurrency", out object toCurrObj) &&
                        parameters.TryGetValue("amount", out object amountObj))
                    {
                        if (fromCurrObj is CurrencyType fromCurrency &&
                            toCurrObj is CurrencyType toCurrency &&
                            amountObj is float amount)
                        {
                            // Create resource change records
                            ResourceChange sourceChange = new ResourceChange
                            {
                                resourceType = ResourceType.Currency,
                                resourceId = fromCurrency.ToString(),
                                amount = -amount,
                                source = actionId
                            };
                            
                            // Create rollback action
                            Action rollback = () => 
                            {
                                // Rollback will be handled by the currency system if needed
                            };
                            
                            // Add to batch
                            batch.AddChange(sourceChange, rollback);
                            
                            // The actual conversion will be handled by the currency system
                            // through the ICurrencySystem.ProcessAction call
                        }
                    }
                    break;
                    
                case "process_materials":
                    // Process material processing
                    if (parameters != null && 
                        parameters.TryGetValue("fromItem", out object fromItemObj) &&
                        parameters.TryGetValue("toItem", out object toItemObj) &&
                        parameters.TryGetValue("amount", out object itemAmountObj))
                    {
                        if (fromItemObj is string fromItem &&
                            toItemObj is string toItem &&
                            itemAmountObj is int amount)
                        {
                            // Create resource change records
                            ResourceChange sourceChange = new ResourceChange
                            {
                                resourceType = ResourceType.Item,
                                resourceId = fromItem,
                                amount = -amount,
                                source = actionId
                            };
                            
                            // Create rollback action
                            Action rollback = () => 
                            {
                                // Rollback will be handled by the inventory system if needed
                            };
                            
                            // Add to batch
                            batch.AddChange(sourceChange, rollback);
                            
                            // The actual conversion will be handled by the inventory system
                        }
                    }
                    break;
            }
        }
        #endregion

        #region Save/Load
        public ConversionSaveData GenerateSaveData()
        {
            ConversionSaveData saveData = new ConversionSaveData
            {
                playerId = _playerId,
                recentConversions = new List<ResourceConversion>(_recentConversions),
                conversionRateModifiers = new Dictionary<string, float>(_conversionRateModifiers),
                unlockedConversions = new List<string>(_unlockedConversions)
            };
            
            return saveData;
        }
        
        public void RestoreFromSaveData(ConversionSaveData saveData)
        {
            if (saveData == null)
                return;
            
            _playerId = saveData.playerId;
            _recentConversions = new List<ResourceConversion>(saveData.recentConversions);
            _conversionRateModifiers = new Dictionary<string, float>(saveData.conversionRateModifiers);
            _unlockedConversions = new List<string>(saveData.unlockedConversions);
        }
        #endregion
    }
}