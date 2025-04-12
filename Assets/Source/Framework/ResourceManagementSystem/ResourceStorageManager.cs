using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ResourceManagement
{
    /// <summary>
    /// Manages resource storage in the resource management system
    /// </summary>
    public class ResourceStorageManager : IResourceStorageSystem
    {
        #region Private Fields
        private string _playerId;
        private ResourceSystemConfig.StorageConfig _config;
        private List<StoredResource> _storedResources = new List<StoredResource>();
        private List<StorageMethod> _availableStorageMethods = new List<StorageMethod>();
        private float _totalCapacity;
        private float _lastUpdateTime;
        private System.Random _random = new System.Random();
        #endregion

        #region Events
        public event Action<ResourceType, string, float, float> OnResourceStored;
        public event Action<ResourceType, string, float, float> OnResourceDeteriorated;
        #endregion

        #region Constructor
        public ResourceStorageManager(ResourceSystemConfig.StorageConfig config)
        {
            _config = config;
            _totalCapacity = config.baseTotalCapacity;
        }
        #endregion

        #region Initialization Methods
        public void Initialize(string playerId)
        {
            _playerId = playerId;
            InitializeStorageMethods();
            _lastUpdateTime = Time.time;
        }
        
        private void InitializeStorageMethods()
        {
            _availableStorageMethods.Clear();
            
            // Initialize storage methods from config
            if (_config.availableMethods != null)
            {
                foreach (var methodDefinition in _config.availableMethods)
                {
                    if (methodDefinition.availableByDefault)
                    {
                        var storageMethod = new StorageMethod
                        {
                            methodId = methodDefinition.methodId,
                            methodName = methodDefinition.methodName,
                            deteriorationMultiplier = methodDefinition.deteriorationMultiplier,
                            capacityMultiplier = methodDefinition.capacityMultiplier,
                            energyCost = methodDefinition.energyCost,
                            maintenanceCost = methodDefinition.maintenanceCost
                        };
                        
                        _availableStorageMethods.Add(storageMethod);
                    }
                }
            }
            
            // Add default storage method if none defined
            if (_availableStorageMethods.Count == 0)
            {
                _availableStorageMethods.Add(new StorageMethod
                {
                    methodId = "standard_storage",
                    methodName = "Standard Storage",
                    deteriorationMultiplier = 1.0f,
                    capacityMultiplier = 1.0f,
                    energyCost = 0,
                    maintenanceCost = 0
                });
            }
        }
        #endregion

        #region Storage Operations
        public bool StoreResource(ResourceType resourceType, string resourceId, float amount, float quality = 1.0f)
        {
            if (amount <= 0 || quality <= 0 || quality > 1.0f)
                return false;
            
            // Check if there's enough storage capacity
            float usedCapacity = GetUsedCapacity();
            if (usedCapacity + amount > _totalCapacity)
                return false;
            
            // Default storage method
            StorageMethod storageMethod = _availableStorageMethods.FirstOrDefault();
            
            // Create stored resource
            var storedResource = new StoredResource
            {
                storageId = GenerateStorageId(resourceType, resourceId),
                resourceType = resourceType,
                resourceId = resourceId,
                amount = amount,
                quality = quality,
                deteriorationRate = _config.baseItemDeteriorationRate * storageMethod.deteriorationMultiplier,
                storageDate = DateTime.Now,
                expirationDate = DateTime.Now.AddDays(30), // Example: 30-day expiration
                storageMethod = storageMethod
            };
            
            // Add to stored resources
            _storedResources.Add(storedResource);
            
            // Trigger event
            OnResourceStored?.Invoke(resourceType, resourceId, amount, quality);
            
            return true;
        }
        
        public bool RetrieveResource(ResourceType resourceType, string resourceId, float amount)
        {
            if (amount <= 0)
                return false;
            
            // Find matching resources
            var matchingResources = _storedResources
                .Where(r => r.resourceType == resourceType && r.resourceId == resourceId)
                .OrderByDescending(r => r.quality) // Get highest quality first
                .ToList();
            
            if (matchingResources.Count == 0)
                return false;
            
            // Calculate total available amount
            float totalAvailable = matchingResources.Sum(r => r.amount);
            if (totalAvailable < amount)
                return false;
            
            // Remove resources, starting with highest quality
            float remainingToRetrieve = amount;
            
            foreach (var resource in matchingResources)
            {
                if (remainingToRetrieve <= 0)
                    break;
                
                if (resource.amount <= remainingToRetrieve)
                {
                    // Retrieve entire resource
                    remainingToRetrieve -= resource.amount;
                    _storedResources.Remove(resource);
                }
                else
                {
                    // Retrieve partial amount
                    resource.amount -= remainingToRetrieve;
                    remainingToRetrieve = 0;
                }
            }
            
            return true;
        }
        
        public float GetStoredAmount(ResourceType resourceType, string resourceId)
        {
            return _storedResources
                .Where(r => r.resourceType == resourceType && r.resourceId == resourceId)
                .Sum(r => r.amount);
        }
        
        public float GetStoredQuality(ResourceType resourceType, string resourceId)
        {
            var resources = _storedResources
                .Where(r => r.resourceType == resourceType && r.resourceId == resourceId)
                .ToList();
            
            if (resources.Count == 0)
                return 0;
            
            // Calculate weighted average quality
            float totalAmount = resources.Sum(r => r.amount);
            if (totalAmount <= 0)
                return 0;
                
            float weightedQualitySum = resources.Sum(r => r.quality * r.amount);
            return weightedQualitySum / totalAmount;
        }
        
        public List<StoredResource> GetAllStoredResources()
        {
            return new List<StoredResource>(_storedResources);
        }
        
        public StorageStatus GetStorageStatus()
        {
            float usedCapacity = GetUsedCapacity();
            float capacityPercentage = (_totalCapacity > 0) ? (usedCapacity / _totalCapacity) * 100 : 0;
            
            // Count items at risk (low quality or near expiration)
            int itemsAtRisk = 0;
            float totalQuality = 0;
            Dictionary<string, float> resourceBreakdown = new Dictionary<string, float>();
            
            foreach (var resource in _storedResources)
            {
                // Add to resource breakdown
                string key = $"{resource.resourceType}:{resource.resourceId}";
                if (!resourceBreakdown.ContainsKey(key))
                    resourceBreakdown[key] = 0;
                    
                resourceBreakdown[key] += resource.amount;
                
                // Check if at risk
                if (resource.quality < 0.3f || (resource.expirationDate - DateTime.Now).TotalDays < 3)
                {
                    itemsAtRisk++;
                }
                
                totalQuality += resource.quality * resource.amount;
            }
            
            // Calculate average quality
            float averageQuality = (_storedResources.Count > 0) 
                ? totalQuality / _storedResources.Sum(r => r.amount)
                : 0;
            
            return new StorageStatus
            {
                totalCapacity = _totalCapacity,
                usedCapacity = usedCapacity,
                capacityUsedPercentage = capacityPercentage,
                totalStoredItems = _storedResources.Count,
                itemsAtRisk = itemsAtRisk,
                averageItemQuality = averageQuality,
                resourceBreakdown = resourceBreakdown
            };
        }
        
        private float GetUsedCapacity()
        {
            return _storedResources.Sum(r => r.amount);
        }
        
        private string GenerateStorageId(ResourceType resourceType, string resourceId)
        {
            return $"storage_{resourceType}_{resourceId}_{DateTime.Now.Ticks}_{_random.Next(10000, 99999)}";
        }
        #endregion

        #region Deterioration Processing
        public void ProcessDegradation(float deltaTime)
        {
            if (!_config.enableQualityDeterioration)
                return;
            
            List<StoredResource> expiredResources = new List<StoredResource>();
            
            foreach (var resource in _storedResources)
            {
                // Skip resources with zero deterioration rate
                if (resource.deteriorationRate <= 0)
                    continue;
                
                // Calculate quality loss
                float qualityLoss = resource.deteriorationRate * deltaTime;
                float previousQuality = resource.quality;
                
                // Update quality
                resource.quality = Mathf.Max(0, resource.quality - qualityLoss);
                
                // Check if quality reached zero or item expired
                if (resource.quality <= 0 || DateTime.Now > resource.expirationDate)
                {
                    expiredResources.Add(resource);
                }
                else if (qualityLoss > 0)
                {
                    // Trigger deterioration event
                    OnResourceDeteriorated?.Invoke(
                        resource.resourceType, 
                        resource.resourceId, 
                        resource.amount, 
                        previousQuality - resource.quality);
                }
            }
            
            // Remove expired resources
            foreach (var resource in expiredResources)
            {
                _storedResources.Remove(resource);
                
                // Trigger final deterioration event
                OnResourceDeteriorated?.Invoke(
                    resource.resourceType,
                    resource.resourceId,
                    resource.amount,
                    resource.quality); // Full quality loss
            }
        }
        #endregion

        #region Storage Method Management
        public bool UpgradeStorageCapacity(float additionalCapacity)
        {
            if (additionalCapacity <= 0 || !_config.enableCapacityUpgrades)
                return false;
            
            _totalCapacity += additionalCapacity;
            return true;
        }
        
        public bool AddStorageMethod(StorageMethod method)
        {
            if (_availableStorageMethods.Any(m => m.methodId == method.methodId))
                return false;
            
            _availableStorageMethods.Add(method);
            return true;
        }
        
        public bool ChangeStorageMethod(string resourceId, string newMethodId)
        {
            // Find the resource
            var resource = _storedResources.FirstOrDefault(r => r.storageId == resourceId);
            if (resource == null)
                return false;
            
            // Find the new storage method
            var newMethod = _availableStorageMethods.FirstOrDefault(m => m.methodId == newMethodId);
            if (newMethod == null)
                return false;
            
            // Update storage method and deterioration rate
            resource.storageMethod = newMethod;
            resource.deteriorationRate = _config.baseItemDeteriorationRate * newMethod.deteriorationMultiplier;
            
            return true;
        }
        #endregion

        #region Action Processing
        public ActionValidationResult ValidateAction(string actionId)
        {
            // Check for storage-related actions
            if (actionId.StartsWith("store_resource_"))
            {
                // Extract resource information from action id
                string resourceInfo = actionId.Replace("store_resource_", "");
                string[] parts = resourceInfo.Split('_');
                
                if (parts.Length >= 3)
                {
                    ResourceType resourceType;
                    if (Enum.TryParse(parts[0], out resourceType))
                    {
                        string resourceId = parts[1];
                        float amount = float.Parse(parts[2]);
                        
                        // Check storage capacity
                        float usedCapacity = GetUsedCapacity();
                        if (usedCapacity + amount > _totalCapacity)
                        {
                            return new ActionValidationResult
                            {
                                isValid = false,
                                message = $"Not enough storage capacity. Need {amount}, have {_totalCapacity - usedCapacity} available."
                            };
                        }
                    }
                }
            }
            
            return new ActionValidationResult { isValid = true };
        }
        
        public void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch)
        {
            if (actionId.StartsWith("store_resource_"))
            {
                ProcessStoreAction(actionId, parameters, batch);
            }
            else if (actionId.StartsWith("retrieve_resource_"))
            {
                ProcessRetrieveAction(actionId, parameters, batch);
            }
            else if (actionId == "upgrade_storage_capacity")
            {
                float additionalCapacity = 100f; // Default value
                
                if (parameters != null && parameters.TryGetValue("amount", out object amountObj))
                {
                    if (amountObj is float floatValue)
                    {
                        additionalCapacity = floatValue;
                    }
                }
                
                ProcessStorageUpgrade(additionalCapacity, batch);
            }
        }
        
        private void ProcessStoreAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch)
        {
            ResourceType resourceType = ResourceType.Item; // Default
            string resourceId = "";
            float amount = 0;
            float quality = 1.0f;
            
            // Extract from action ID if present
            string resourceInfo = actionId.Replace("store_resource_", "");
            string[] parts = resourceInfo.Split('_');
            
            if (parts.Length >= 3)
            {
                Enum.TryParse(parts[0], out resourceType);
                resourceId = parts[1];
                float.TryParse(parts[2], out amount);
                
                if (parts.Length > 3)
                {
                    float.TryParse(parts[3], out quality);
                }
            }
            
            // Override with parameters if provided
            if (parameters != null)
            {
                if (parameters.TryGetValue("resourceType", out object typeObj) && typeObj is ResourceType type)
                    resourceType = type;
                    
                if (parameters.TryGetValue("resourceId", out object idObj) && idObj is string id)
                    resourceId = id;
                    
                if (parameters.TryGetValue("amount", out object amountObj) && amountObj is float amt)
                    amount = amt;
                    
                if (parameters.TryGetValue("quality", out object qualityObj) && qualityObj is float qual)
                    quality = qual;
            }
            
            // Validate parameters
            if (string.IsNullOrEmpty(resourceId) || amount <= 0)
                return;
                
            // Create resource
            string storageId = GenerateStorageId(resourceType, resourceId);
            
            // Default storage method
            StorageMethod storageMethod = _availableStorageMethods.FirstOrDefault();
            
            // Allow specific storage method if provided
            if (parameters != null && parameters.TryGetValue("storageMethodId", out object methodObj) && methodObj is string methodId)
            {
                var method = _availableStorageMethods.FirstOrDefault(m => m.methodId == methodId);
                if (method != null)
                    storageMethod = method;
            }
            
            // Create stored resource
            var storedResource = new StoredResource
            {
                storageId = storageId,
                resourceType = resourceType,
                resourceId = resourceId,
                amount = amount,
                quality = quality,
                deteriorationRate = _config.baseItemDeteriorationRate * storageMethod.deteriorationMultiplier,
                storageDate = DateTime.Now,
                expirationDate = DateTime.Now.AddDays(30), // Example: 30-day expiration
                storageMethod = storageMethod
            };
            
            // Create resource change record
            ResourceChange change = new ResourceChange
            {
                resourceType = resourceType,
                resourceId = resourceId,
                amount = amount,
                source = "storage",
                qualityChange = quality
            };
            
            // Create rollback action
            Action rollback = () => 
            {
                _storedResources.Remove(storedResource);
            };
            
            // Add to batch
            batch.AddChange(change, rollback);
            
            // Apply change
            _storedResources.Add(storedResource);
        }
        
        private void ProcessRetrieveAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch)
        {
            ResourceType resourceType = ResourceType.Item; // Default
            string resourceId = "";
            float amount = 0;
            
            // Extract from action ID if present
            string resourceInfo = actionId.Replace("retrieve_resource_", "");
            string[] parts = resourceInfo.Split('_');
            
            if (parts.Length >= 3)
            {
                Enum.TryParse(parts[0], out resourceType);
                resourceId = parts[1];
                float.TryParse(parts[2], out amount);
            }
            
            // Override with parameters if provided
            if (parameters != null)
            {
                if (parameters.TryGetValue("resourceType", out object typeObj) && typeObj is ResourceType type)
                    resourceType = type;
                    
                if (parameters.TryGetValue("resourceId", out object idObj) && idObj is string id)
                    resourceId = id;
                    
                if (parameters.TryGetValue("amount", out object amountObj) && amountObj is float amt)
                    amount = amt;
            }
            
            // Validate parameters
            if (string.IsNullOrEmpty(resourceId) || amount <= 0)
                return;
                
            // Find matching resources
            var matchingResources = _storedResources
                .Where(r => r.resourceType == resourceType && r.resourceId == resourceId)
                .OrderByDescending(r => r.quality) // Get highest quality first
                .ToList();
            
            if (matchingResources.Count == 0 || matchingResources.Sum(r => r.amount) < amount)
                return;
                
            // Create backup for rollback
            var resourceBackup = matchingResources.Select(r => new { Resource = r, Amount = r.amount }).ToList();
            
            // Create resource change record
            ResourceChange change = new ResourceChange
            {
                resourceType = resourceType,
                resourceId = resourceId,
                amount = -amount, // Negative for retrieval
                source = "storage_retrieval"
            };
            
            // Create rollback action
            Action rollback = () => 
            {
                foreach (var backup in resourceBackup)
                {
                    backup.Resource.amount = backup.Amount;
                }
                
                // Re-add any resources that were fully removed
                foreach (var backup in resourceBackup)
                {
                    if (!_storedResources.Contains(backup.Resource))
                    {
                        _storedResources.Add(backup.Resource);
                    }
                }
            };
            
            // Add to batch
            batch.AddChange(change, rollback);
            
            // Apply change
            float remainingToRetrieve = amount;
            List<StoredResource> resourcesToRemove = new List<StoredResource>();
            
            foreach (var resource in matchingResources)
            {
                if (remainingToRetrieve <= 0)
                    break;
                
                if (resource.amount <= remainingToRetrieve)
                {
                    // Retrieve entire resource
                    remainingToRetrieve -= resource.amount;
                    resourcesToRemove.Add(resource);
                }
                else
                {
                    // Retrieve partial amount
                    resource.amount -= remainingToRetrieve;
                    remainingToRetrieve = 0;
                }
            }
            
            // Remove fully depleted resources
            foreach (var resource in resourcesToRemove)
            {
                _storedResources.Remove(resource);
            }
        }
        
        private void ProcessStorageUpgrade(float additionalCapacity, ResourceTransactionBatch batch)
        {
            if (additionalCapacity <= 0 || !_config.enableCapacityUpgrades)
                return;
                
            float originalCapacity = _totalCapacity;
            
            // Create resource change record
            ResourceChange change = new ResourceChange
            {
                resourceType = ResourceType.Currency, // Currency used for upgrade
                resourceId = "storage_upgrade",
                amount = 0, // Cost would be handled by currency system
                source = "storage_capacity_upgrade"
            };
            
            // Create rollback action
            Action rollback = () => 
            {
                _totalCapacity = originalCapacity;
            };
            
            // Add to batch
            batch.AddChange(change, rollback);
            
            // Apply change
            _totalCapacity += additionalCapacity;
        }
        #endregion

        #region Optimization Suggestions
        public List<ResourceOptimizationSuggestion> GetOptimizationSuggestions()
        {
            List<ResourceOptimizationSuggestion> suggestions = new List<ResourceOptimizationSuggestion>();
            
            // Get storage status
            StorageStatus status = GetStorageStatus();
            
            // Check for storage space issues
            if (status.capacityUsedPercentage > 80)
            {
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "storage_capacity",
                    title = "Storage Capacity Warning",
                    description = $"Storage is at {status.capacityUsedPercentage:F0}% capacity. Consider upgrading storage or removing less valuable items.",
                    potentialBenefit = status.usedCapacity * 0.2f, // 20% of used space
                    primaryResourceType = ResourceType.Item,
                    priority = 9,
                    actionSteps = new List<string>
                    {
                        "Upgrade storage capacity if possible",
                        "Remove or use low-quality or low-value items",
                        "Process raw materials to reduce storage needs"
                    }
                });
            }
            
            // Check for deteriorating items
            if (status.itemsAtRisk > 0)
            {
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "deteriorating_items",
                    title = "At-Risk Items",
                    description = $"You have {status.itemsAtRisk} items at risk of expiring or with low quality. Use or transfer these items soon.",
                    potentialBenefit = status.itemsAtRisk * 10, // Estimated value
                    primaryResourceType = ResourceType.Item,
                    priority = 8,
                    actionSteps = new List<string>
                    {
                        "Use at-risk items before they deteriorate further",
                        "Transfer items to better storage if available",
                        "Convert items to more stable forms if possible"
                    }
                });
            }
            
            // Check for inefficient storage methods
            var inefficientResources = _storedResources
                .Where(r => r.deteriorationRate > _config.baseItemDeteriorationRate * 1.2f)
                .ToList();
                
            if (inefficientResources.Count > 0)
            {
                var resource = inefficientResources.OrderByDescending(r => r.amount * r.quality).First();
                var betterMethod = _availableStorageMethods
                    .Where(m => m.deteriorationMultiplier < resource.storageMethod.deteriorationMultiplier)
                    .OrderBy(m => m.deteriorationMultiplier)
                    .FirstOrDefault();
                    
                if (betterMethod != null)
                {
                    suggestions.Add(new ResourceOptimizationSuggestion
                    {
                        suggestionId = "storage_method",
                        title = "Improve Storage Method",
                        description = $"Using {resource.storageMethod.methodName} for {resource.resourceId} causes faster deterioration. Consider using {betterMethod.methodName} instead.",
                        potentialBenefit = resource.amount * resource.quality * (resource.deteriorationRate - _config.baseItemDeteriorationRate * betterMethod.deteriorationMultiplier) * 100, // Estimated saving
                        primaryResourceType = resource.resourceType,
                        priority = 6,
                        actionSteps = new List<string>
                        {
                            $"Transfer resources to {betterMethod.methodName}",
                            "Balance storage efficiency with energy costs",
                            "Consider upgrading storage facilities"
                        }
                    });
                }
            }
            
            // Check for storage organization
            if (_config.enableItemCategories && _storedResources.Count > 10)
            {
                // Simple heuristic: if many different types of items are stored randomly
                int uniqueCategories = _storedResources.Select(r => r.resourceId).Distinct().Count();
                
                if (uniqueCategories > 5) // Arbitrary threshold
                {
                    suggestions.Add(new ResourceOptimizationSuggestion
                    {
                        suggestionId = "storage_organization",
                        title = "Organize Storage",
                        description = $"Your storage contains {uniqueCategories} different types of resources. Organizing by category would improve efficiency.",
                        potentialBenefit = uniqueCategories * 5, // Estimated time saving
                        primaryResourceType = ResourceType.Time,
                        priority = 4,
                        actionSteps = new List<string>
                        {
                            "Organize resources by type and quality",
                            "Group similar items together for easier access",
                            "Consider setting up automatic sorting if available"
                        }
                    });
                }
            }
            
            return suggestions;
        }
        #endregion

        #region Save/Load
        public StorageSaveData GenerateSaveData()
        {
            StorageSaveData saveData = new StorageSaveData
            {
                playerId = _playerId,
                storedResources = new List<StoredResource>(_storedResources),
                availableMethods = new List<StorageMethod>(_availableStorageMethods),
                totalCapacity = _totalCapacity
            };
            
            return saveData;
        }
        
        public void RestoreFromSaveData(StorageSaveData saveData)
        {
            if (saveData == null)
                return;
            
            _playerId = saveData.playerId;
            _storedResources = new List<StoredResource>(saveData.storedResources);
            _availableStorageMethods = new List<StorageMethod>(saveData.availableMethods);
            _totalCapacity = saveData.totalCapacity;
        }
        #endregion
    }
}