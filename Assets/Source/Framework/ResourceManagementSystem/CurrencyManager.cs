using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ResourceManagement
{
    /// <summary>
    /// Manages all currency-related operations in the resource management system
    /// </summary>
    public class CurrencyManager : ICurrencySystem
    {
        #region Private Fields
        private string _playerId;
        private ResourceSystemConfig.CurrencyConfig _config;
        private Dictionary<CurrencyType, Currency> _currencies = new Dictionary<CurrencyType, Currency>();
        private List<FinancialTransaction> _transactionHistory = new List<FinancialTransaction>();
        private EconomyStatus _economyStatus = new EconomyStatus();
        private Dictionary<string, InvestmentOption> _investmentOptions = new Dictionary<string, InvestmentOption>();
        private Dictionary<string, float> _actionCosts = new Dictionary<string, float>();
        
        // Economy simulation parameters
        private float _nextEconomyUpdateTime;
        private System.Random _random = new System.Random();
        #endregion

        #region Events
        public event Action<CurrencyType, float, float> OnCurrencyChanged;
        public event Action<FinancialTransaction> OnTransactionProcessed;
        public event Action<CurrencyType, CurrencyType, float, float> OnCurrencyConverted;
        #endregion

        #region Constructor
        public CurrencyManager(ResourceSystemConfig.CurrencyConfig config)
        {
            _config = config;
            InitializeActionCosts();
            InitializeInvestmentOptions();
            InitializeEconomyStatus();
        }
        #endregion

        #region Initialization Methods
        public void Initialize(string playerId)
        {
            _playerId = playerId;
            InitializeCurrencies();
            _nextEconomyUpdateTime = Time.time + _config.economyUpdateInterval;
        }
        
        private void InitializeCurrencies()
        {
            _currencies.Clear();
            
            foreach (var currencyDef in _config.availableCurrencies)
            {
                var currency = new Currency
                {
                    currencyId = currencyDef.currencyId,
                    name = currencyDef.name,
                    type = currencyDef.type,
                    currentAmount = currencyDef.startingAmount,
                    maxCapacity = currencyDef.maxCapacity,
                    properties = currencyDef.properties
                };
                
                _currencies[currencyDef.type] = currency;
            }
        }
        
        private void InitializeActionCosts()
        {
            // Default action costs - would normally load from configuration
            _actionCosts["purchase_basic_item"] = 10f;
            _actionCosts["purchase_premium_item"] = 50f;
            _actionCosts["fast_travel"] = 25f;
            _actionCosts["skill_training"] = 100f;
            _actionCosts["property_rent"] = 200f;
            _actionCosts["property_purchase"] = 1000f;
            _actionCosts["market_fee"] = 5f;
            _actionCosts["crafting_fee"] = 15f;
            _actionCosts["repair_items"] = 30f;
            _actionCosts["premium_service"] = 100f;
        }
        
        private void InitializeInvestmentOptions()
        {
            // Example investment options - would normally load from configuration
            _investmentOptions["low_risk_investment"] = new InvestmentOption
            {
                investmentId = "low_risk_investment",
                name = "Low Risk Investment",
                minInvestmentAmount = 100f,
                baseReturnRate = 0.05f,
                riskFactor = 0.1f,
                durationDays = 7,
                possibleOutcomes = new List<InvestmentOutcome>
                {
                    new InvestmentOutcome { 
                        outcomeId = "low_success", 
                        description = "Safe return with minimal profit", 
                        returnMultiplier = 1.05f, 
                        probability = 0.9f 
                    },
                    new InvestmentOutcome { 
                        outcomeId = "low_fail", 
                        description = "Market fluctuation caused minor loss", 
                        returnMultiplier = 0.95f, 
                        probability = 0.1f 
                    }
                }
            };
            
            _investmentOptions["medium_risk_investment"] = new InvestmentOption
            {
                investmentId = "medium_risk_investment",
                name = "Medium Risk Investment",
                minInvestmentAmount = 500f,
                baseReturnRate = 0.12f,
                riskFactor = 0.3f,
                durationDays = 14,
                possibleOutcomes = new List<InvestmentOutcome>
                {
                    new InvestmentOutcome { 
                        outcomeId = "med_big_success", 
                        description = "Market boom provided excellent returns", 
                        returnMultiplier = 1.25f, 
                        probability = 0.2f 
                    },
                    new InvestmentOutcome { 
                        outcomeId = "med_success", 
                        description = "Investment performed as expected", 
                        returnMultiplier = 1.12f, 
                        probability = 0.6f 
                    },
                    new InvestmentOutcome { 
                        outcomeId = "med_fail", 
                        description = "Investment underperformed", 
                        returnMultiplier = 0.9f, 
                        probability = 0.2f 
                    }
                }
            };
            
            _investmentOptions["high_risk_investment"] = new InvestmentOption
            {
                investmentId = "high_risk_investment",
                name = "High Risk Investment",
                minInvestmentAmount = 1000f,
                baseReturnRate = 0.3f,
                riskFactor = 0.7f,
                durationDays = 30,
                possibleOutcomes = new List<InvestmentOutcome>
                {
                    new InvestmentOutcome { 
                        outcomeId = "high_jackpot", 
                        description = "Speculative investment paid off tremendously", 
                        returnMultiplier = 2.0f, 
                        probability = 0.1f 
                    },
                    new InvestmentOutcome { 
                        outcomeId = "high_success", 
                        description = "Investment yielded high returns", 
                        returnMultiplier = 1.3f, 
                        probability = 0.3f 
                    },
                    new InvestmentOutcome { 
                        outcomeId = "high_average", 
                        description = "Investment returned moderate profit", 
                        returnMultiplier = 1.1f, 
                        probability = 0.3f 
                    },
                    new InvestmentOutcome { 
                        outcomeId = "high_fail", 
                        description = "Investment performed poorly", 
                        returnMultiplier = 0.7f, 
                        probability = 0.2f 
                    },
                    new InvestmentOutcome { 
                        outcomeId = "high_bust", 
                        description = "Investment was a complete loss", 
                        returnMultiplier = 0.1f, 
                        probability = 0.1f 
                    }
                }
            };
        }
        
        private void InitializeEconomyStatus()
        {
            _economyStatus = new EconomyStatus
            {
                economicStability = 0.8f,
                averageTransactionVolume = 500f,
                isRecession = false,
                currencyInflationRates = new Dictionary<CurrencyType, float>(),
                marketPrices = new Dictionary<string, float>()
            };
            
            // Initialize default inflation rates
            _economyStatus.currencyInflationRates[CurrencyType.StandardCurrency] = 0.02f;
            _economyStatus.currencyInflationRates[CurrencyType.PremiumCurrency] = 0.005f;
            _economyStatus.currencyInflationRates[CurrencyType.FactionCurrency] = 0.03f;
            _economyStatus.currencyInflationRates[CurrencyType.TradeCredits] = 0.015f;
            
            // Initialize some standard market prices
            _economyStatus.marketPrices["common_material"] = 5f;
            _economyStatus.marketPrices["rare_material"] = 50f;
            _economyStatus.marketPrices["common_equipment"] = 100f;
            _economyStatus.marketPrices["rare_equipment"] = 500f;
            _economyStatus.marketPrices["consumable_basic"] = 10f;
            _economyStatus.marketPrices["consumable_advanced"] = 30f;
            _economyStatus.marketPrices["premium_service"] = 200f;
        }
        #endregion

        #region Currency Operations
        public bool AddCurrency(CurrencyType type, float amount, string source, string description)
        {
            if (amount <= 0)
                return false;
            
            if (!_currencies.TryGetValue(type, out var currency))
                return false;
            
            // Check max capacity limit
            float newAmount = currency.currentAmount + amount;
            if (currency.maxCapacity > 0 && newAmount > currency.maxCapacity)
            {
                newAmount = currency.maxCapacity;
                amount = currency.maxCapacity - currency.currentAmount;
                
                if (amount <= 0)
                    return false;
            }
            
            // Store previous amount for event
            float previousAmount = currency.currentAmount;
            
            // Update currency
            currency.currentAmount = newAmount;
            
            // Record transaction
            RecordTransaction(TransactionType.Income, type, amount, source, description, TransactionCategory.Other);
            
            // Trigger event
            OnCurrencyChanged?.Invoke(type, newAmount, amount);
            
            return true;
        }
        
        public bool RemoveCurrency(CurrencyType type, float amount, string destination, string description)
        {
            if (amount <= 0)
                return false;
            
            if (!_currencies.TryGetValue(type, out var currency))
                return false;
            
            // Check if enough currency is available
            if (currency.currentAmount < amount && !currency.properties.canBeNegative)
                return false;
            
            // Store previous amount for event
            float previousAmount = currency.currentAmount;
            
            // Update currency
            currency.currentAmount -= amount;
            
            // Record transaction
            RecordTransaction(TransactionType.Expense, type, -amount, destination, description, TransactionCategory.Other);
            
            // Trigger event
            OnCurrencyChanged?.Invoke(type, currency.currentAmount, -amount);
            
            return true;
        }
        
        public bool ConvertCurrency(CurrencyType fromType, CurrencyType toType, float amount)
        {
            if (amount <= 0)
                return false;
            
            if (!_currencies.TryGetValue(fromType, out var fromCurrency) || 
                !_currencies.TryGetValue(toType, out var toCurrency))
                return false;
            
            // Check if enough source currency is available
            if (fromCurrency.currentAmount < amount && !fromCurrency.properties.canBeNegative)
                return false;
            
            // Calculate conversion rate
            float conversionRate = GetConversionRate(fromType, toType);
            float convertedAmount = amount * conversionRate;
            
            // Check max capacity limit for target currency
            float newToAmount = toCurrency.currentAmount + convertedAmount;
            if (toCurrency.maxCapacity > 0 && newToAmount > toCurrency.maxCapacity)
            {
                // Calculate maximum amount that can be converted
                float maxConvertibleAmount = (toCurrency.maxCapacity - toCurrency.currentAmount) / conversionRate;
                if (maxConvertibleAmount <= 0)
                    return false;
                
                amount = maxConvertibleAmount;
                convertedAmount = maxConvertibleAmount * conversionRate;
            }
            
            // Update currencies
            fromCurrency.currentAmount -= amount;
            toCurrency.currentAmount += convertedAmount;
            
            // Record transaction
            RecordTransaction(TransactionType.Conversion, fromType, -amount, "currency_conversion", 
                $"Converted {amount} {fromCurrency.name} to {convertedAmount} {toCurrency.name}", 
                TransactionCategory.Trading);
            
            RecordTransaction(TransactionType.Conversion, toType, convertedAmount, "currency_conversion", 
                $"Received from conversion of {amount} {fromCurrency.name}", 
                TransactionCategory.Trading);
            
            // Trigger events
            OnCurrencyChanged?.Invoke(fromType, fromCurrency.currentAmount, -amount);
            OnCurrencyChanged?.Invoke(toType, toCurrency.currentAmount, convertedAmount);
            OnCurrencyConverted?.Invoke(fromType, toType, amount, convertedAmount);
            
            return true;
        }
        
        private float GetConversionRate(CurrencyType fromType, CurrencyType toType)
        {
            // Base conversion rates - could be loaded from configuration
            Dictionary<(CurrencyType, CurrencyType), float> baseRates = new Dictionary<(CurrencyType, CurrencyType), float>
            {
                { (CurrencyType.StandardCurrency, CurrencyType.PremiumCurrency), 0.1f },
                { (CurrencyType.PremiumCurrency, CurrencyType.StandardCurrency), 12f },
                { (CurrencyType.StandardCurrency, CurrencyType.FactionCurrency), 0.5f },
                { (CurrencyType.FactionCurrency, CurrencyType.StandardCurrency), 1.8f },
                { (CurrencyType.StandardCurrency, CurrencyType.TradeCredits), 0.8f },
                { (CurrencyType.TradeCredits, CurrencyType.StandardCurrency), 1.2f },
                { (CurrencyType.PremiumCurrency, CurrencyType.FactionCurrency), 6f },
                { (CurrencyType.FactionCurrency, CurrencyType.PremiumCurrency), 0.15f },
                { (CurrencyType.PremiumCurrency, CurrencyType.TradeCredits), 9f },
                { (CurrencyType.TradeCredits, CurrencyType.PremiumCurrency), 0.1f },
                { (CurrencyType.FactionCurrency, CurrencyType.TradeCredits), 1.5f },
                { (CurrencyType.TradeCredits, CurrencyType.FactionCurrency), 0.6f }
            };
            
            var key = (fromType, toType);
            
            // For same currency, rate is 1
            if (fromType == toType)
                return 1f;
            
            // Get base rate
            float baseRate = baseRates.TryGetValue(key, out float rate) ? rate : 1f;
            
            // Apply market modifiers
            float marketModifier = 1f;
            
            // Apply inflation differential
            if (_economyStatus.currencyInflationRates.TryGetValue(fromType, out float fromInflation) &&
                _economyStatus.currencyInflationRates.TryGetValue(toType, out float toInflation))
            {
                float inflationDifferential = 1f + (fromInflation - toInflation);
                marketModifier *= inflationDifferential;
            }
            
            // Apply economic stability factor
            float stabilityFactor = Mathf.Lerp(0.8f, 1.2f, _economyStatus.economicStability);
            marketModifier *= stabilityFactor;
            
            // Apply recession effect if applicable
            if (_economyStatus.isRecession)
            {
                if (toType == CurrencyType.PremiumCurrency)
                    marketModifier *= 0.9f; // Premium currency becomes more valuable in recession
                else if (fromType == CurrencyType.PremiumCurrency)
                    marketModifier *= 1.1f; // Premium currency trades better in recession
            }
            
            // Apply tax/fee
            float taxRate = _config.baseTransactionTax;
            marketModifier *= (1f - taxRate);
            
            return baseRate * marketModifier;
        }
        
        private void RecordTransaction(TransactionType type, CurrencyType currencyType, float amount, 
                                      string source, string description, TransactionCategory category)
        {
            FinancialTransaction transaction = new FinancialTransaction
            {
                transactionId = GenerateTransactionId(),
                timestamp = DateTime.Now,
                type = type,
                amountsChanged = new Dictionary<CurrencyType, float> { { currencyType, amount } },
                description = description,
                sourceId = source,
                category = category
            };
            
            _transactionHistory.Add(transaction);
            
            // Keep transaction history at a reasonable size
            const int maxTransactions = 100;
            if (_transactionHistory.Count > maxTransactions)
            {
                _transactionHistory.RemoveAt(0);
            }
            
            // Trigger event
            OnTransactionProcessed?.Invoke(transaction);
        }
        
        private string GenerateTransactionId()
        {
            return $"txn_{DateTime.Now.Ticks}_{_random.Next(10000, 99999)}";
        }
        #endregion

        #region Investment Operations
        public bool MakeInvestment(string investmentId, float amount)
        {
            if (!_investmentOptions.TryGetValue(investmentId, out var investmentOption))
                return false;
            
            if (amount < investmentOption.minInvestmentAmount)
                return false;
            
            // Get main currency (for example purposes we'll use standard currency)
            CurrencyType currencyType = CurrencyType.StandardCurrency;
            if (!_currencies.TryGetValue(currencyType, out var currency))
                return false;
            
            // Check if enough currency is available
            if (currency.currentAmount < amount)
                return false;
            
            // Remove invested amount
            currency.currentAmount -= amount;
            
            // Record transaction
            RecordTransaction(TransactionType.Investment, currencyType, -amount, 
                              investmentId, $"Investment in {investmentOption.name}", 
                              TransactionCategory.Investment);
            
            // Schedule return - in a real implementation, this would be stored and processed with time system
            // For demonstration, we calculate the outcome immediately
            CalculateInvestmentOutcome(investmentOption, amount, currencyType);
            
            // Trigger event
            OnCurrencyChanged?.Invoke(currencyType, currency.currentAmount, -amount);
            
            return true;
        }
        
        private void CalculateInvestmentOutcome(InvestmentOption investment, float amount, CurrencyType currencyType)
        {
            // Determine outcome based on probabilities
            float roll = (float)_random.NextDouble();
            float cumulativeProbability = 0f;
            
            InvestmentOutcome selectedOutcome = investment.possibleOutcomes[0];
            
            foreach (var outcome in investment.possibleOutcomes)
            {
                cumulativeProbability += outcome.probability;
                if (roll <= cumulativeProbability)
                {
                    selectedOutcome = outcome;
                    break;
                }
            }
            
            // Calculate return amount
            float returnAmount = amount * selectedOutcome.returnMultiplier;
            
            // Log outcome (in a real implementation, this would be scheduled for future processing)
            Debug.Log($"Investment outcome: {selectedOutcome.description}, " +
                      $"Return amount: {returnAmount} (from {amount} invested)");
        }
        #endregion

        #region Economy Simulation
        public void UpdateEconomy(float gameTime)
        {
            if (gameTime < _nextEconomyUpdateTime)
                return;
            
            _nextEconomyUpdateTime = gameTime + _config.economyUpdateInterval;
            
            if (!_config.enableInflation && !_config.enableMarketFluctuation)
                return;
            
            // Update inflation rates
            if (_config.enableInflation)
            {
                foreach (var pair in _economyStatus.currencyInflationRates.ToList())
                {
                    float baseInflation = pair.Value;
                    float inflationChange = ((float)_random.NextDouble() - 0.5f) * 0.01f; // -0.5% to +0.5%
                    
                    _economyStatus.currencyInflationRates[pair.Key] = Mathf.Clamp(baseInflation + inflationChange, 0f, 0.1f);
                }
            }
            
            // Update economic stability
            float stabilityChange = ((float)_random.NextDouble() - 0.5f) * 0.05f; // -2.5% to +2.5%
            _economyStatus.economicStability = Mathf.Clamp(_economyStatus.economicStability + stabilityChange, 0.3f, 1f);
            
            // Occasionally trigger recession
            if (_random.NextDouble() < 0.05 && _economyStatus.economicStability > 0.5f)
            {
                _economyStatus.isRecession = !_economyStatus.isRecession;
                
                if (_economyStatus.isRecession)
                {
                    _economyStatus.economicStability *= 0.7f; // Recessions reduce stability
                }
            }
            
            // Update market prices
            if (_config.enableMarketFluctuation)
            {
                foreach (var key in _economyStatus.marketPrices.Keys.ToList())
                {
                    float basePrice = _economyStatus.marketPrices[key];
                    float priceChange = ((float)_random.NextDouble() - 0.5f) * 0.1f * basePrice; // -5% to +5%
                    
                    // Apply stability effect
                    if (_economyStatus.economicStability < 0.5f)
                    {
                        // More volatility in unstable economy
                        priceChange *= 2f;
                    }
                    
                    // Apply recession effect
                    if (_economyStatus.isRecession)
                    {
                        // Prices generally fall during recession (except premium goods)
                        if (key.Contains("premium"))
                        {
                            priceChange = Mathf.Abs(priceChange); // Premium goods increase in price
                        }
                        else
                        {
                            priceChange = -Mathf.Abs(priceChange) * 0.5f; // Other goods decrease in price
                        }
                    }
                    
                    float newPrice = Mathf.Max(basePrice + priceChange, 1f); // Keep prices positive
                    _economyStatus.marketPrices[key] = newPrice;
                }
            }
        }
        #endregion

        #region Action Processing
        public ActionValidationResult ValidateAction(string actionId)
        {
            if (!_actionCosts.TryGetValue(actionId, out float cost))
            {
                // No cost associated with this action
                return new ActionValidationResult { isValid = true };
            }
            
            // Default to standard currency
            CurrencyType currencyType = CurrencyType.StandardCurrency;
            if (!_currencies.TryGetValue(currencyType, out var currency))
            {
                return new ActionValidationResult 
                { 
                    isValid = false, 
                    message = $"Currency {currencyType} not available" 
                };
            }
            
            if (currency.currentAmount < cost && !currency.properties.canBeNegative)
            {
                return new ActionValidationResult 
                { 
                    isValid = false, 
                    message = $"Insufficient {currency.name}. Need {cost}, have {currency.currentAmount}" 
                };
            }
            
            return new ActionValidationResult { isValid = true };
        }
        
        public void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch)
        {
            if (!_actionCosts.TryGetValue(actionId, out float cost))
            {
                return; // No currency cost for this action
            }
            
            // Default to standard currency unless specified
            CurrencyType currencyType = CurrencyType.StandardCurrency;
            if (parameters != null && parameters.TryGetValue("currencyType", out object currencyObj))
            {
                if (currencyObj is CurrencyType typedCurrency)
                {
                    currencyType = typedCurrency;
                }
            }
            
            if (!_currencies.TryGetValue(currencyType, out var currency))
            {
                return;
            }
            
            // Store previous amount for rollback
            float previousAmount = currency.currentAmount;
            
            // Create change record
            ResourceChange change = new ResourceChange
            {
                resourceType = ResourceType.Currency,
                resourceId = currencyType.ToString(),
                amount = -cost,
                source = actionId
            };
            
            // Create rollback action
            Action rollback = () => 
            {
                currency.currentAmount = previousAmount;
            };
            
            // Add to batch
            batch.AddChange(change, rollback);
            
            // Apply change
            currency.currentAmount -= cost;
            
            // Record transaction
            string description = parameters != null && parameters.TryGetValue("description", out object desc) 
                              ? desc.ToString() 
                              : $"Cost for action: {actionId}";
                              
            RecordTransaction(TransactionType.Expense, currencyType, -cost, actionId, description, DetermineCategory(actionId));
            
            // Trigger event (will be triggered by the resource system after batch commitment)
            // OnCurrencyChanged?.Invoke(currencyType, currency.currentAmount, -cost);
        }
        
        private TransactionCategory DetermineCategory(string actionId)
        {
            if (actionId.Contains("purchase"))
                return TransactionCategory.Shopping;
            if (actionId.Contains("travel"))
                return TransactionCategory.Transportation;
            if (actionId.Contains("skill") || actionId.Contains("training"))
                return TransactionCategory.Education;
            if (actionId.Contains("property"))
                return TransactionCategory.Housing;
            if (actionId.Contains("market") || actionId.Contains("trading"))
                return TransactionCategory.Trading;
            if (actionId.Contains("craft") || actionId.Contains("repair"))
                return TransactionCategory.Crafting;
            if (actionId.Contains("service"))
                return TransactionCategory.Services;
            
            return TransactionCategory.Other;
        }
        #endregion

        #region Public Interface Methods
        public float GetCurrencyAmount(CurrencyType type)
        {
            return _currencies.TryGetValue(type, out var currency) ? currency.currentAmount : 0f;
        }
        
        public Currency GetCurrency(CurrencyType type)
        {
            return _currencies.TryGetValue(type, out var currency) ? currency : null;
        }
        
        public List<Currency> GetAllCurrencies()
        {
            return _currencies.Values.ToList();
        }
        
        public EconomyStatus GetEconomyStatus()
        {
            return _economyStatus;
        }
        
        public float GetActionCost(string actionId)
        {
            return _actionCosts.TryGetValue(actionId, out float cost) ? cost : 0f;
        }
        
        public bool IsCurrencyCritical(CurrencyType type)
        {
            if (!_currencies.TryGetValue(type, out var currency))
                return false;
            
            // Define critical thresholds for different currencies
            Dictionary<CurrencyType, float> criticalThresholds = new Dictionary<CurrencyType, float>
            {
                { CurrencyType.StandardCurrency, 50f },
                { CurrencyType.PremiumCurrency, 5f },
                { CurrencyType.FactionCurrency, 20f },
                { CurrencyType.TradeCredits, 30f }
            };
            
            float threshold = criticalThresholds.TryGetValue(type, out float value) ? value : 10f;
            
            return currency.currentAmount < threshold;
        }
        
        public List<ResourceOptimizationSuggestion> GetOptimizationSuggestions()
        {
            List<ResourceOptimizationSuggestion> suggestions = new List<ResourceOptimizationSuggestion>();
            
            // Check for low main currency
            Currency mainCurrency = GetCurrency(CurrencyType.StandardCurrency);
            if (mainCurrency != null && mainCurrency.currentAmount < 100f)
            {
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "low_main_currency",
                    title = "Low on Main Currency",
                    description = $"You're running low on {mainCurrency.name} ({mainCurrency.currentAmount:F0}). Consider completing quests or selling items.",
                    potentialBenefit = 200f, // Estimated benefit
                    primaryResourceType = ResourceType.Currency,
                    priority = 10,
                    actionSteps = new List<string> 
                    { 
                        "Complete available quests for quick income", 
                        "Sell unused items from inventory",
                        "Check for available jobs or contracts" 
                    }
                });
            }
            
            // Check for economy opportunities
            if (_economyStatus.isRecession)
            {
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "recession_investment",
                    title = "Recession Investment Opportunity",
                    description = "The economy is in recession - prices are lower. Good time to invest in standard goods.",
                    potentialBenefit = 300f, // Estimated benefit
                    primaryResourceType = ResourceType.Currency,
                    priority = 5,
                    actionSteps = new List<string> 
                    { 
                        "Purchase non-premium goods at lower prices", 
                        "Consider buying property or assets",
                        "Avoid selling standard items until recession ends" 
                    }
                });
            }
            
            // Check for inefficient currency conversion
            Currency premiumCurrency = GetCurrency(CurrencyType.PremiumCurrency);
            if (premiumCurrency != null && premiumCurrency.currentAmount > 50f)
            {
                float conversionRate = GetConversionRate(CurrencyType.PremiumCurrency, CurrencyType.StandardCurrency);
                
                if (conversionRate > 10f) // If rate is favorable
                {
                    suggestions.Add(new ResourceOptimizationSuggestion
                    {
                        suggestionId = "favorable_conversion",
                        title = "Favorable Currency Conversion",
                        description = $"Current premium to standard conversion rate is favorable (1:{conversionRate:F1}). Consider converting some premium currency.",
                        potentialBenefit = premiumCurrency.currentAmount * conversionRate,
                        primaryResourceType = ResourceType.Currency,
                        priority = 7,
                        actionSteps = new List<string> 
                        { 
                            $"Convert some premium currency at favorable rate", 
                            "Use standard currency for regular purchases",
                            "Save premium currency for special items only" 
                        }
                    });
                }
            }
            
            return suggestions;
        }
        
        public Dictionary<string, object> GetAnalyticsData()
        {
            Dictionary<string, object> analytics = new Dictionary<string, object>();
            
            // Currency values
            Dictionary<string, float> currencyValues = new Dictionary<string, float>();
            foreach (var pair in _currencies)
            {
                currencyValues[pair.Key.ToString()] = pair.Value.currentAmount;
            }
            analytics["currencyValues"] = currencyValues;
            
            // Transaction volume
            Dictionary<TransactionCategory, float> transactionVolumes = new Dictionary<TransactionCategory, float>();
            Dictionary<TransactionType, int> transactionCounts = new Dictionary<TransactionType, int>();
            
            // Analyze last 20 transactions (or fewer if not available)
            int count = Math.Min(20, _transactionHistory.Count);
            List<FinancialTransaction> recentTransactions = _transactionHistory.Skip(_transactionHistory.Count - count).ToList();
            
            foreach (var transaction in recentTransactions)
            {
                // Sum transaction volume by category
                if (!transactionVolumes.ContainsKey(transaction.category))
                {
                    transactionVolumes[transaction.category] = 0f;
                }
                
                float amount = transaction.amountsChanged.Values.Sum(v => Math.Abs(v));
                transactionVolumes[transaction.category] += amount;
                
                // Count transaction types
                if (!transactionCounts.ContainsKey(transaction.type))
                {
                    transactionCounts[transaction.type] = 0;
                }
                transactionCounts[transaction.type]++;
            }
            
            analytics["transactionVolumes"] = transactionVolumes;
            analytics["transactionCounts"] = transactionCounts;
            
            // Economy status
            analytics["economyStatus"] = _economyStatus;
            
            return analytics;
        }
        #endregion

        #region Save/Load
        public CurrencySaveData GenerateSaveData()
        {
            CurrencySaveData saveData = new CurrencySaveData
            {
                playerId = _playerId,
                currencies = new List<CurrencySaveData.SerializedCurrency>(),
                recentTransactions = new List<FinancialTransaction>(_transactionHistory.Take(20)), // Save last 20 transactions
                economyStatus = _economyStatus
            };
            
            // Serialize currencies
            foreach (var pair in _currencies)
            {
                saveData.currencies.Add(new CurrencySaveData.SerializedCurrency
                {
                    currencyId = pair.Value.currencyId,
                    type = pair.Value.type,
                    currentAmount = pair.Value.currentAmount,
                    activeModifiers = new List<CurrencyModifier>(pair.Value.activeModifiers)
                });
            }
            
            return saveData;
        }
        
        public void RestoreFromSaveData(CurrencySaveData saveData)
        {
            if (saveData == null)
                return;
            
            _playerId = saveData.playerId;
            
            // Restore currencies
            foreach (var serializedCurrency in saveData.currencies)
            {
                if (_currencies.TryGetValue(serializedCurrency.type, out var currency))
                {
                    currency.currencyId = serializedCurrency.currencyId;
                    currency.currentAmount = serializedCurrency.currentAmount;
                    currency.activeModifiers = new List<CurrencyModifier>(serializedCurrency.activeModifiers);
                }
            }
            
            // Restore transaction history
            _transactionHistory = new List<FinancialTransaction>(saveData.recentTransactions);
            
            // Restore economy status
            _economyStatus = saveData.economyStatus;
        }
        #endregion
    }
}