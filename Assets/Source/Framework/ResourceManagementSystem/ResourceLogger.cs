using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceManagement
{
    /// <summary>
    /// Responsible for logging resource events, system messages, and collecting analytics data
    /// </summary>
    public class ResourceLogger : IResourceLogger
    {
        #region Private Fields
        private List<LogEntry> _systemLogs = new List<LogEntry>();
        private List<ResourceEvent> _resourceEvents = new List<ResourceEvent>();
        private ResourceAnalytics _analytics = new ResourceAnalytics();
        private Dictionary<string, List<float>> _resourceHistory = new Dictionary<string, List<float>>();
        private Dictionary<string, DateTime> _lastAnalyzedTimes = new Dictionary<string, DateTime>();
        
        // Configuration
        private int _maxLogEntries = 100;
        private int _maxEventEntries = 200;
        private int _historyDataPoints = 30;
        private bool _logToConsole = true;
        private bool _detectTrends = true;
        #endregion

        #region Constructor
        public ResourceLogger()
        {
            InitializeAnalytics();
        }
        
        private void InitializeAnalytics()
        {
            _analytics.resourceHistory = new Dictionary<string, List<float>>();
            _analytics.acquisitionRates = new Dictionary<string, float>();
            _analytics.consumptionRates = new Dictionary<string, float>();
            _analytics.efficiencyMetrics = new Dictionary<string, float>();
            _analytics.detectedTrends = new List<ResourceTrend>();
        }
        #endregion

        #region Logging Methods
        public void LogResourceEvent(ResourceEvent resourceEvent)
        {
            // Add to event history
            _resourceEvents.Add(resourceEvent);
            
            // Cap event list size
            if (_resourceEvents.Count > _maxEventEntries)
            {
                _resourceEvents.RemoveAt(0);
            }
            
            // Log to console if enabled
            if (_logToConsole)
            {
                LogEventToConsole(resourceEvent);
            }
            
            // Update analytics data
            UpdateAnalyticsForEvent(resourceEvent);
        }
        
        public void LogSystemMessage(string message)
        {
            LogEntry entry = new LogEntry
            {
                message = message,
                timestamp = DateTime.Now,
                level = LogLevel.Info
            };
            
            // Add to log
            _systemLogs.Add(entry);
            
            // Cap log size
            if (_systemLogs.Count > _maxLogEntries)
            {
                _systemLogs.RemoveAt(0);
            }
            
            // Log to console
            if (_logToConsole)
            {
                Debug.Log($"[ResourceSystem] {message}");
            }
        }
        
        public void LogWarning(string message)
        {
            LogEntry entry = new LogEntry
            {
                message = message,
                timestamp = DateTime.Now,
                level = LogLevel.Warning
            };
            
            // Add to log
            _systemLogs.Add(entry);
            
            // Cap log size
            if (_systemLogs.Count > _maxLogEntries)
            {
                _systemLogs.RemoveAt(0);
            }
            
            // Log to console
            Debug.LogWarning($"[ResourceSystem] {message}");
        }
        
        public void LogError(string message)
        {
            LogEntry entry = new LogEntry
            {
                message = message,
                timestamp = DateTime.Now,
                level = LogLevel.Error
            };
            
            // Add to log
            _systemLogs.Add(entry);
            
            // Cap log size
            if (_systemLogs.Count > _maxLogEntries)
            {
                _systemLogs.RemoveAt(0);
            }
            
            // Log to console
            Debug.LogError($"[ResourceSystem] {message}");
        }
        
        private void LogEventToConsole(ResourceEvent resourceEvent)
        {
            switch (resourceEvent.type)
            {
                case ResourceEventType.CurrencyChanged:
                    if (resourceEvent.parameters.TryGetValue("currencyType", out object currType) &&
                        resourceEvent.parameters.TryGetValue("delta", out object delta))
                    {
                        string change = (float)delta >= 0 ? "+" + delta : delta.ToString();
                        Debug.Log($"[ResourceSystem] Currency '{currType}' changed: {change}");
                    }
                    break;
                    
                case ResourceEventType.ItemAdded:
                    if (resourceEvent.parameters.TryGetValue("item", out object item) &&
                        resourceEvent.parameters.TryGetValue("quantity", out object quantity))
                    {
                        Debug.Log($"[ResourceSystem] Item added: {quantity}x {((Item)item).name}");
                    }
                    break;
                    
                case ResourceEventType.ItemRemoved:
                    if (resourceEvent.parameters.TryGetValue("item", out object remItem) &&
                        resourceEvent.parameters.TryGetValue("quantity", out object remQuantity))
                    {
                        Debug.Log($"[ResourceSystem] Item removed: {remQuantity}x {((Item)remItem).name}");
                    }
                    break;
                    
                case ResourceEventType.ResourceCritical:
                    if (resourceEvent.parameters.TryGetValue("resourceType", out object resType) &&
                        resourceEvent.parameters.TryGetValue("resourceId", out object resId))
                    {
                        Debug.LogWarning($"[ResourceSystem] CRITICAL: Resource '{resType} {resId}' at critical level!");
                    }
                    break;
                    
                default:
                    // Don't log every event type to avoid console spam
                    break;
            }
        }
        #endregion

        #region Analytics Methods
        public void UpdateResourceAnalytics(Dictionary<string, object> currencyData, 
                                            Dictionary<string, object> inventoryData, 
                                            Dictionary<string, object> timeData)
        {
            // Update currency analytics
            if (currencyData != null)
            {
                if (currencyData.TryGetValue("currencyValues", out object currencyValues) && 
                    currencyValues is Dictionary<string, float> values)
                {
                    foreach (var pair in values)
                    {
                        UpdateResourceHistory($"currency_{pair.Key}", pair.Value);
                    }
                }
            }
            
            // Update inventory analytics
            if (inventoryData != null)
            {
                if (inventoryData.TryGetValue("totalItems", out object totalItems))
                {
                    UpdateResourceHistory("inventory_totalItems", Convert.ToSingle(totalItems));
                }
                
                if (inventoryData.TryGetValue("totalWeight", out object totalWeight))
                {
                    UpdateResourceHistory("inventory_totalWeight", Convert.ToSingle(totalWeight));
                }
                
                if (inventoryData.TryGetValue("inventoryValue", out object inventoryValue))
                {
                    UpdateResourceHistory("inventory_value", Convert.ToSingle(inventoryValue));
                }
            }
            
            // Update time analytics
            if (timeData != null)
            {
                // Time-based analytics like time usage efficiency, etc.
            }
            
            // Detect trends
            if (_detectTrends)
            {
                DetectResourceTrends();
            }
            
            // Calculate derived metrics
            CalculateDerivedMetrics();
            
            // Synchronize with analytics object
            SynchronizeAnalyticsObject();
        }
        
        private void UpdateResourceHistory(string resourceKey, float value)
        {
            if (!_resourceHistory.ContainsKey(resourceKey))
            {
                _resourceHistory[resourceKey] = new List<float>();
            }
            
            var history = _resourceHistory[resourceKey];
            
            // Add the new value
            history.Add(value);
            
            // Cap history size
            if (history.Count > _historyDataPoints)
            {
                history.RemoveAt(0);
            }
        }
        
        private void UpdateAnalyticsForEvent(ResourceEvent resourceEvent)
        {
            switch (resourceEvent.type)
            {
                case ResourceEventType.CurrencyChanged:
                    if (resourceEvent.parameters.TryGetValue("currencyType", out object currType) &&
                        resourceEvent.parameters.TryGetValue("newAmount", out object newAmount))
                    {
                        UpdateResourceHistory($"currency_{currType}", Convert.ToSingle(newAmount));
                        
                        // Track acquisition/consumption rates
                        if (resourceEvent.parameters.TryGetValue("delta", out object delta))
                        {
                            float deltaValue = Convert.ToSingle(delta);
                            string rateKey = $"currency_{currType}";
                            
                            if (deltaValue > 0)
                            {
                                IncrementRate(_analytics.acquisitionRates, rateKey, deltaValue);
                            }
                            else if (deltaValue < 0)
                            {
                                IncrementRate(_analytics.consumptionRates, rateKey, -deltaValue);
                            }
                        }
                    }
                    break;
                    
                case ResourceEventType.ItemAdded:
                case ResourceEventType.ItemRemoved:
                    if (resourceEvent.parameters.TryGetValue("item", out object item) &&
                        resourceEvent.parameters.TryGetValue("quantity", out object quantity))
                    {
                        Item itemObj = item as Item;
                        if (itemObj != null)
                        {
                            string itemKey = $"item_{itemObj.itemId}";
                            
                            // For tracking acquisition/consumption
                            float quantityValue = Convert.ToSingle(quantity);
                            
                            if (resourceEvent.type == ResourceEventType.ItemAdded)
                            {
                                IncrementRate(_analytics.acquisitionRates, itemKey, quantityValue);
                            }
                            else
                            {
                                IncrementRate(_analytics.consumptionRates, itemKey, quantityValue);
                            }
                        }
                    }
                    break;
                    
                case ResourceEventType.ResourceConverted:
                    if (resourceEvent.parameters.TryGetValue("conversion", out object convObj))
                    {
                        ResourceConversion conversion = convObj as ResourceConversion;
                        if (conversion != null)
                        {
                            // Track conversion efficiency
                            string conversionKey = $"conversion_{conversion.fromType}_{conversion.fromId}_to_{conversion.toType}_{conversion.toId}";
                            _analytics.efficiencyMetrics[conversionKey] = conversion.conversionEfficiency;
                        }
                    }
                    break;
            }
        }
        
        private void IncrementRate(Dictionary<string, float> rateDict, string key, float amount)
        {
            if (!rateDict.ContainsKey(key))
            {
                rateDict[key] = 0;
            }
            
            rateDict[key] += amount;
        }
        
        private void DetectResourceTrends()
        {
            foreach (var pair in _resourceHistory)
            {
                string resourceKey = pair.Key;
                List<float> history = pair.Value;
                
                // Only analyze resources with enough history
                if (history.Count < 10)
                    continue;
                
                // Check if we've analyzed this resource recently
                if (_lastAnalyzedTimes.TryGetValue(resourceKey, out DateTime lastTime))
                {
                    // Don't analyze too frequently
                    if ((DateTime.Now - lastTime).TotalMinutes < 10)
                        continue;
                }
                
                // Calculate trend slope
                float slope = CalculateTrendSlope(history);
                
                // Check if slope is significant
                if (Math.Abs(slope) > 0.1f)
                {
                    // Create or update trend
                    bool trendExists = false;
                    foreach (var trend in _analytics.detectedTrends)
                    {
                        if (trend.trendId == resourceKey)
                        {
                            // Update existing trend
                            trend.slope = slope;
                            trend.isPositive = slope > 0;
                            trend.detectionTime = DateTime.Now;
                            trend.confidence = CalculateTrendConfidence(history, slope);
                            trendExists = true;
                            break;
                        }
                    }
                    
                    if (!trendExists)
                    {
                        // Create new trend
                        _analytics.detectedTrends.Add(new ResourceTrend
                        {
                            trendId = resourceKey,
                            description = $"{resourceKey} is {(slope > 0 ? "increasing" : "decreasing")}",
                            slope = slope,
                            isPositive = slope > 0,
                            detectionTime = DateTime.Now,
                            confidence = CalculateTrendConfidence(history, slope)
                        });
                    }
                }
                else
                {
                    // Remove any existing trend for this resource
                    _analytics.detectedTrends.RemoveAll(t => t.trendId == resourceKey);
                }
                
                // Update last analyzed time
                _lastAnalyzedTimes[resourceKey] = DateTime.Now;
            }
            
            // Clean up old trends
            _analytics.detectedTrends.RemoveAll(t => (DateTime.Now - t.detectionTime).TotalHours > 24);
        }
        
        private float CalculateTrendSlope(List<float> history)
        {
            if (history.Count < 2)
                return 0f;
                
            // Simple linear regression
            int n = history.Count;
            float sumX = 0f;
            float sumY = 0f;
            float sumXY = 0f;
            float sumXX = 0f;
            
            for (int i = 0; i < n; i++)
            {
                float x = i;
                float y = history[i];
                
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumXX += x * x;
            }
            
            float denominator = (n * sumXX) - (sumX * sumX);
            if (denominator == 0)
                return 0f;
                
            // Calculate slope
            float slope = ((n * sumXY) - (sumX * sumY)) / denominator;
            
            // Normalize relative to data magnitude
            float avgY = sumY / n;
            if (avgY != 0)
            {
                slope = slope / avgY;
            }
            
            return slope;
        }
        
        private float CalculateTrendConfidence(List<float> history, float slope)
        {
            // Simple confidence calculation based on data points and consistency
            int n = history.Count;
            
            // More data points = more confidence
            float dataSizeConfidence = Mathf.Clamp01((float)n / 20f);
            
            // Calculate R-squared to measure how well the trend line fits the data
            float rSquared = CalculateRSquared(history, slope);
            
            // Combined confidence
            return (dataSizeConfidence + rSquared) * 0.5f;
        }
        
        private float CalculateRSquared(List<float> history, float slope)
        {
            if (history.Count < 3)
                return 0.5f; // Default medium confidence
                
            int n = history.Count;
            float avgY = 0f;
            foreach (var y in history)
            {
                avgY += y;
            }
            avgY /= n;
            
            float totalSS = 0f;
            float residualSS = 0f;
            
            for (int i = 0; i < n; i++)
            {
                float x = i;
                float actualY = history[i];
                float predictedY = avgY + slope * (x - (n - 1) / 2f);
                
                totalSS += (actualY - avgY) * (actualY - avgY);
                residualSS += (actualY - predictedY) * (actualY - predictedY);
            }
            
            if (totalSS == 0)
                return 1f; // Perfectly flat line
                
            float rSquared = 1f - (residualSS / totalSS);
            return Mathf.Clamp01(rSquared);
        }
        
        private void CalculateDerivedMetrics()
        {
            // Calculate acquisition and consumption rates per game day
            foreach (var key in _analytics.acquisitionRates.Keys)
            {
                // Example: Convert absolute values to per-day rates
                // In a real implementation, this would be based on actual game time tracking
                _analytics.acquisitionRates[key] /= 24f; // per day
            }
            
            foreach (var key in _analytics.consumptionRates.Keys)
            {
                _analytics.consumptionRates[key] /= 24f; // per day
            }
            
            // Calculate efficiency metrics - here we could calculate various efficiency indicators
            // For example, resource generation efficiency, conversion efficiency, etc.
        }
        
        private void SynchronizeAnalyticsObject()
        {
            // Copy resource history
            _analytics.resourceHistory.Clear();
            foreach (var pair in _resourceHistory)
            {
                _analytics.resourceHistory[pair.Key] = new List<float>(pair.Value);
            }
        }
        
        public ResourceAnalytics GetResourceAnalytics()
        {
            return _analytics;
        }
        
        public List<LogEntry> GetRecentLogs(int count = 10)
        {
            count = Mathf.Min(count, _systemLogs.Count);
            return _systemLogs.GetRange(_systemLogs.Count - count, count);
        }
        
        public List<ResourceEvent> GetRecentEvents(int count = 10)
        {
            count = Mathf.Min(count, _resourceEvents.Count);
            return _resourceEvents.GetRange(_resourceEvents.Count - count, count);
        }
        #endregion

        #region Supporting Classes
        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }
        
        [Serializable]
        public class LogEntry
        {
            public string message;
            public DateTime timestamp;
            public LogLevel level;
            
            public override string ToString()
            {
                return $"[{timestamp:HH:mm:ss}] [{level}] {message}";
            }
        }
        #endregion
    }
}