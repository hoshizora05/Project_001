using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ResourceManagement
{
    /// <summary>
    /// Manages resource generation in the resource management system
    /// </summary>
    public class ResourceGenerationManager : IResourceGenerationSystem
    {
        #region Private Fields
        private string _playerId;
        private ResourceSystemConfig.GenerationConfig _config;
        private List<GenerationSource> _generationSources = new List<GenerationSource>();
        private Dictionary<string, float> _resourcePoolAmounts = new Dictionary<string, float>();
        private float _lastUpdateTime;
        private System.Random _random = new System.Random();
        #endregion

        #region Events
        public event Action<ResourceType, string, float> OnResourceGenerated;
        #endregion

        #region Constructor
        public ResourceGenerationManager(ResourceSystemConfig.GenerationConfig config)
        {
            _config = config;
        }
        #endregion

        #region Initialization Methods
        public void Initialize(string playerId)
        {
            _playerId = playerId;
            InitializeGenerationSources();
            _lastUpdateTime = Time.time;
        }
        
        private void InitializeGenerationSources()
        {
            _generationSources.Clear();
            
            // Initialize from config
            if (_config.defaultSources != null)
            {
                foreach (var sourceDefinition in _config.defaultSources)
                {
                    if (sourceDefinition.activeByDefault)
                    {
                        var source = new GenerationSource
                        {
                            sourceId = sourceDefinition.sourceId,
                            sourceName = sourceDefinition.sourceName,
                            resourceType = sourceDefinition.resourceType,
                            resourceId = sourceDefinition.resourceId,
                            baseGenerationRate = sourceDefinition.baseGenerationRate,
                            currentEfficiency = sourceDefinition.initialEfficiency,
                            isActive = true,
                            cooldownTime = 0,
                            remainingCooldown = 0,
                            schedule = sourceDefinition.defaultSchedule != null 
                                ? sourceDefinition.defaultSchedule 
                                : new GenerationSchedule(),
                            availableUpgrades = sourceDefinition.upgrades != null 
                                ? new List<GenerationUpgrade>(sourceDefinition.upgrades) 
                                : new List<GenerationUpgrade>(),
                            currentUpgradeLevel = 0
                        };
                        
                        _generationSources.Add(source);
                    }
                }
            }
        }
        #endregion

        #region Generation Operations
        public void ProcessGeneration(float deltaTime)
        {
            // Skip if passive generation is disabled
            if (!_config.enablePassiveGeneration)
                return;
            
            // Process each active source
            foreach (var source in _generationSources)
            {
                // Skip inactive sources
                if (!source.isActive)
                    continue;
                
                // Check for cooldown
                if (source.remainingCooldown > 0)
                {
                    source.remainingCooldown -= deltaTime;
                    continue;
                }
                
                // Check schedule if applicable
                if (IsSourceActiveOnSchedule(source))
                {
                    // Calculate generation amount
                    float generationRate = source.baseGenerationRate * source.currentEfficiency;
                    float generatedAmount = generationRate * deltaTime;
                    
                    // Apply time of day modifier if enabled
                    if (_config.applyTimeOfDayEfficiency)
                    {
                        float timeModifier = GetTimeOfDayModifier();
                        generatedAmount *= timeModifier;
                    }
                    
                    // Check for minimum generation threshold
                    if (generatedAmount >= 0.01f) // Only generate if amount is significant
                    {
                        // Apply generation caps if enabled
                        if (_config.enableDailyGenerationCaps)
                        {
                            // Implementation would track daily caps per resource
                        }
                        
                        if (_config.enableGlobalGenerationLimit)
                        {
                            float totalGenerated = _resourcePoolAmounts.Values.Sum();
                            if (totalGenerated >= _config.globalGenerationCapacity)
                            {
                                // Skip generation if global cap reached
                                continue;
                            }
                            
                            // Adjust to stay within global cap
                            float remainingCapacity = _config.globalGenerationCapacity - totalGenerated;
                            generatedAmount = Mathf.Min(generatedAmount, remainingCapacity);
                        }
                        
                        // Add to resource pool
                        string resourceKey = $"{source.resourceType}:{source.resourceId}";
                        if (!_resourcePoolAmounts.ContainsKey(resourceKey))
                        {
                            _resourcePoolAmounts[resourceKey] = 0;
                        }
                        _resourcePoolAmounts[resourceKey] += generatedAmount;
                        
                        // Trigger resource generated event
                        OnResourceGenerated?.Invoke(source.resourceType, source.resourceId, generatedAmount);
                    }
                }
            }
        }
        
        private bool IsSourceActiveOnSchedule(GenerationSource source)
        {
            // If no schedule, always active
            if (source.schedule == null || !source.schedule.hasSchedule)
                return true;
            
            // Check day of week if specified
            if (source.schedule.activeDays.Count > 0)
            {
                DayOfWeek currentDay = (DayOfWeek)(DateTime.Now.DayOfWeek);
                if (!source.schedule.activeDays.Contains(currentDay))
                    return false;
            }
            
            // Check time of day if specified
            if (source.schedule.startHour >= 0 && source.schedule.endHour > 0)
            {
                float currentHour = DateTime.Now.Hour + DateTime.Now.Minute / 60f;
                
                // Handle overnight schedules (e.g., 22:00 - 06:00)
                if (source.schedule.endHour < source.schedule.startHour)
                {
                    // Active if current time is after start hour or before end hour
                    return currentHour >= source.schedule.startHour || currentHour < source.schedule.endHour;
                }
                else
                {
                    // Active if current time is between start and end hours
                    return currentHour >= source.schedule.startHour && currentHour < source.schedule.endHour;
                }
            }
            
            // Check cycle limits if specified
            if (source.schedule.isRecurring && source.schedule.totalCycles > 0)
            {
                if (source.schedule.completedCycles >= source.schedule.totalCycles)
                    return false;
            }
            
            return true;
        }
        
        private float GetTimeOfDayModifier()
        {
            // Simple time of day modifier example
            int hour = DateTime.Now.Hour;
            
            if (hour >= 8 && hour < 16) // Daytime: normal generation
                return 1.0f;
            else if (hour >= 16 && hour < 22) // Evening: slightly reduced
                return 0.8f;
            else // Night: significantly reduced
                return 0.5f;
        }
        #endregion

        #region Source Management
        public void AddGenerationSource(GenerationSource source)
        {
            // Check if source already exists
            if (_generationSources.Any(s => s.sourceId == source.sourceId))
                return;
            
            _generationSources.Add(source);
        }
        
        public void RemoveGenerationSource(string sourceId)
        {
            _generationSources.RemoveAll(s => s.sourceId == sourceId);
        }
        
        public void UpdateGenerationSource(string sourceId, float efficiency)
        {
            var source = _generationSources.Find(s => s.sourceId == sourceId);
            if (source != null)
            {
                source.currentEfficiency = Mathf.Clamp(efficiency, 0, 5); // Limit efficiency multiplier
            }
        }
        
        public List<GenerationSource> GetActiveGenerationSources()
        {
            return _generationSources.Where(s => s.isActive).ToList();
        }
        
        public float GetGenerationRate(ResourceType resourceType, string resourceId)
        {
            float totalRate = 0;
            
            foreach (var source in _generationSources.Where(s => s.isActive && 
                                                           s.resourceType == resourceType && 
                                                           s.resourceId == resourceId))
            {
                totalRate += source.baseGenerationRate * source.currentEfficiency;
            }
            
            return totalRate;
        }
        
        public float GetPooledResource(ResourceType resourceType, string resourceId)
        {
            string key = $"{resourceType}:{resourceId}";
            return _resourcePoolAmounts.TryGetValue(key, out float amount) ? amount : 0;
        }
        
        public bool ConsumePooledResource(ResourceType resourceType, string resourceId, float amount)
        {
            if (amount <= 0)
                return false;
            
            string key = $"{resourceType}:{resourceId}";
            if (!_resourcePoolAmounts.TryGetValue(key, out float currentAmount) || currentAmount < amount)
                return false;
            
            _resourcePoolAmounts[key] -= amount;
            return true;
        }
        #endregion

        #region Action Processing
        public ActionValidationResult ValidateAction(string actionId)
        {
            // Most generation actions don't have prerequisites
            return new ActionValidationResult { isValid = true };
        }
        
        public void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch)
        {
            // Generation actions depend on the specific action type
            if (actionId.StartsWith("activate_generator_"))
            {
                string sourceId = actionId.Replace("activate_generator_", "");
                ActivateGenerator(sourceId, batch);
            }
            else if (actionId.StartsWith("deactivate_generator_"))
            {
                string sourceId = actionId.Replace("deactivate_generator_", "");
                DeactivateGenerator(sourceId, batch);
            }
            else if (actionId.StartsWith("upgrade_generator_"))
            {
                string sourceId = null;
                int upgradeLevel = 0;
                
                if (parameters != null)
                {
                    if (parameters.TryGetValue("sourceId", out object sourceObj))
                        sourceId = sourceObj as string;
                    
                    if (parameters.TryGetValue("upgradeLevel", out object levelObj) && levelObj is int intLevel)
                        upgradeLevel = intLevel;
                }
                
                if (!string.IsNullOrEmpty(sourceId))
                    UpgradeGenerator(sourceId, upgradeLevel, batch);
            }
            else if (actionId.StartsWith("collect_resource_"))
            {
                string resourceInfo = actionId.Replace("collect_resource_", "");
                string[] parts = resourceInfo.Split('_');
                
                if (parts.Length >= 2)
                {
                    ResourceType resourceType;
                    if (Enum.TryParse(parts[0], out resourceType))
                    {
                        string resourceId = parts[1];
                        float amount = parts.Length > 2 && float.TryParse(parts[2], out float parsedAmount) 
                            ? parsedAmount 
                            : float.MaxValue; // Collect all if not specified
                        
                        CollectResource(resourceType, resourceId, amount, batch);
                    }
                }
            }
        }
        
        private void ActivateGenerator(string sourceId, ResourceTransactionBatch batch)
        {
            var source = _generationSources.Find(s => s.sourceId == sourceId);
            if (source == null)
                return;
            
            bool previousState = source.isActive;
            
            // Create resource change record
            ResourceChange change = new ResourceChange
            {
                resourceType = ResourceType.Currency, // For tracking purposes
                resourceId = "generation_activation",
                source = sourceId,
                amount = 0 // No direct resource cost, but could be energy, etc.
            };
            
            // Create rollback action
            Action rollback = () => 
            {
                source.isActive = previousState;
            };
            
            // Add to batch
            batch.AddChange(change, rollback);
            
            // Apply change
            source.isActive = true;
        }
        
        private void DeactivateGenerator(string sourceId, ResourceTransactionBatch batch)
        {
            var source = _generationSources.Find(s => s.sourceId == sourceId);
            if (source == null)
                return;
            
            bool previousState = source.isActive;
            
            // Create resource change record
            ResourceChange change = new ResourceChange
            {
                resourceType = ResourceType.Currency,
                resourceId = "generation_deactivation",
                source = sourceId,
                amount = 0
            };
            
            // Create rollback action
            Action rollback = () => 
            {
                source.isActive = previousState;
            };
            
            // Add to batch
            batch.AddChange(change, rollback);
            
            // Apply change
            source.isActive = false;
        }
        
        private void UpgradeGenerator(string sourceId, int upgradeLevel, ResourceTransactionBatch batch)
        {
            var source = _generationSources.Find(s => s.sourceId == sourceId);
            if (source == null || upgradeLevel <= source.currentUpgradeLevel)
                return;
            
            // Find the upgrade
            var upgrade = source.availableUpgrades.FirstOrDefault(u => u.level == upgradeLevel);
            if (upgrade == null)
                return;
            
            int previousLevel = source.currentUpgradeLevel;
            float previousEfficiency = source.currentEfficiency;
            
            // Create resource change record
            ResourceChange change = new ResourceChange
            {
                resourceType = ResourceType.Currency,
                resourceId = "generator_upgrade",
                source = sourceId,
                amount = 0 // Resource cost would be handled by currency system
            };
            
            // Create rollback action
            Action rollback = () => 
            {
                source.currentUpgradeLevel = previousLevel;
                source.currentEfficiency = previousEfficiency;
            };
            
            // Add to batch
            batch.AddChange(change, rollback);
            
            // Apply change
            source.currentUpgradeLevel = upgradeLevel;
            source.currentEfficiency += upgrade.efficiencyBonus;
        }
        
        private void CollectResource(ResourceType resourceType, string resourceId, float amount, ResourceTransactionBatch batch)
        {
            string key = $"{resourceType}:{resourceId}";
            if (!_resourcePoolAmounts.TryGetValue(key, out float currentAmount) || currentAmount <= 0)
                return;
            
            // Cap amount to available amount
            float collectAmount = Mathf.Min(amount, currentAmount);
            
            float previousAmount = currentAmount;
            
            // Create resource change record
            ResourceChange change = new ResourceChange
            {
                resourceType = resourceType,
                resourceId = resourceId,
                source = "resource_collection",
                amount = collectAmount
            };
            
            // Create rollback action
            Action rollback = () => 
            {
                _resourcePoolAmounts[key] = previousAmount;
            };
            
            // Add to batch
            batch.AddChange(change, rollback);
            
            // Apply change
            _resourcePoolAmounts[key] -= collectAmount;
        }
        #endregion

        #region Optimization Suggestions
        public List<ResourceOptimizationSuggestion> GetOptimizationSuggestions()
        {
            List<ResourceOptimizationSuggestion> suggestions = new List<ResourceOptimizationSuggestion>();
            
            // Check for inactive generators
            var inactiveSources = _generationSources.Where(s => !s.isActive).ToList();
            if (inactiveSources.Count > 0)
            {
                var bestInactiveSource = inactiveSources.OrderByDescending(s => s.baseGenerationRate).First();
                
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "inactive_generator",
                    title = "Inactive Resource Generator",
                    description = $"{bestInactiveSource.sourceName} is inactive. Activating it would generate {bestInactiveSource.baseGenerationRate} resources per hour.",
                    potentialBenefit = bestInactiveSource.baseGenerationRate * 24, // Daily benefit
                    primaryResourceType = bestInactiveSource.resourceType,
                    priority = 7,
                    actionSteps = new List<string>
                    {
                        $"Activate the {bestInactiveSource.sourceName} generator",
                        "Check for required maintenance or resources to run the generator",
                        "Consider upgrading the generator for better efficiency"
                    }
                });
            }
            
            // Check for inefficient generators
            var inefficientSources = _generationSources
                .Where(s => s.isActive && s.currentEfficiency < 0.7f)
                .ToList();
                
            if (inefficientSources.Count > 0)
            {
                var worstSource = inefficientSources.OrderBy(s => s.currentEfficiency).First();
                
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "inefficient_generator",
                    title = "Inefficient Resource Generator",
                    description = $"{worstSource.sourceName} is running at {worstSource.currentEfficiency:P0} efficiency. Upgrading would improve resource generation.",
                    potentialBenefit = worstSource.baseGenerationRate * (1.0f - worstSource.currentEfficiency) * 24, // Potential daily improvement
                    primaryResourceType = worstSource.resourceType,
                    priority = 6,
                    actionSteps = new List<string>
                    {
                        $"Upgrade the {worstSource.sourceName} generator to improve efficiency",
                        "Check for required maintenance to increase efficiency",
                        "Consider replacing with a higher-tier generator"
                    }
                });
            }
            
            // Check for available upgrades
            foreach (var source in _generationSources.Where(s => s.isActive))
            {
                var nextUpgrade = source.availableUpgrades
                    .FirstOrDefault(u => u.level == source.currentUpgradeLevel + 1);
                    
                if (nextUpgrade != null)
                {
                    suggestions.Add(new ResourceOptimizationSuggestion
                    {
                        suggestionId = $"available_upgrade_{source.sourceId}",
                        title = $"Available Generator Upgrade",
                        description = $"{source.sourceName} has an available upgrade that would increase efficiency by {nextUpgrade.efficiencyBonus:P0}.",
                        potentialBenefit = source.baseGenerationRate * nextUpgrade.efficiencyBonus * 24, // Daily improvement
                        primaryResourceType = source.resourceType,
                        priority = 5,
                        actionSteps = new List<string>
                        {
                            $"Upgrade {source.sourceName} to level {nextUpgrade.level}",
                            "Gather required resources for the upgrade",
                            "Consider timing upgrade during peak generation hours"
                        }
                    });
                    
                    // Only suggest one upgrade at a time
                    break;
                }
            }
            
            // Check uncollected resources
            var significantResources = _resourcePoolAmounts
                .Where(kvp => kvp.Value > 10) // Arbitrary threshold for "significant"
                .ToList();
                
            if (significantResources.Count > 0)
            {
                var largestPool = significantResources.OrderByDescending(kvp => kvp.Value).First();
                string[] resourceParts = largestPool.Key.Split(':');
                
                if (resourceParts.Length >= 2 && Enum.TryParse(resourceParts[0], out ResourceType resourceType))
                {
                    string resourceId = resourceParts[1];
                    
                    suggestions.Add(new ResourceOptimizationSuggestion
                    {
                        suggestionId = "uncollected_resources",
                        title = "Uncollected Resources",
                        description = $"You have {largestPool.Value:F0} uncollected resources of type {resourceId}. Collect these resources to use them.",
                        potentialBenefit = largestPool.Value,
                        primaryResourceType = resourceType,
                        priority = 8,
                        actionSteps = new List<string>
                        {
                            $"Collect accumulated {resourceId} resources",
                            "Use collected resources for crafting or upgrades",
                            "Consider setting up automatic collection if available"
                        }
                    });
                }
            }
            
            return suggestions;
        }
        #endregion

        #region Save/Load
        public GenerationSaveData GenerateSaveData()
        {
            GenerationSaveData saveData = new GenerationSaveData
            {
                playerId = _playerId,
                activeSources = new List<GenerationSource>(_generationSources),
                resourcePoolAmounts = new Dictionary<string, float>(_resourcePoolAmounts)
            };
            
            return saveData;
        }
        
        public void RestoreFromSaveData(GenerationSaveData saveData)
        {
            if (saveData == null)
                return;
            
            _playerId = saveData.playerId;
            _generationSources = new List<GenerationSource>(saveData.activeSources);
            _resourcePoolAmounts = new Dictionary<string, float>(saveData.resourcePoolAmounts);
        }
        #endregion
    }
}