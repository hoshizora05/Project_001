using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ResourceManagement
{
    /// <summary>
    /// Manages all time-related operations in the resource management system
    /// </summary>
    public class TimeResourceManager : ITimeResourceSystem
    {
        #region Private Fields
        private string _playerId;
        private ResourceSystemConfig.TimeConfig _config;
        
        // Core time state
        private int _currentDay;
        private float _currentHour;
        private DayOfWeek _currentDayOfWeek;
        private int _currentWeek;
        private int _currentMonth;
        private int _currentYear;
        
        // Time-based actions
        private List<ScheduledAction> _scheduledActions = new List<ScheduledAction>();
        private Dictionary<string, TimeRequirement> _actionTimeRequirements = new Dictionary<string, TimeRequirement>();
        private Dictionary<string, float> _actionEfficiencyData = new Dictionary<string, float>();
        
        // Time modifiers
        private Dictionary<string, List<TimeModifier>> _actionModifiers = new Dictionary<string, List<TimeModifier>>();
        
        // Time blocks for scheduling
        private List<TimeBlock> _dailyTimeBlocks = new List<TimeBlock>();
        
        // Performance optimization
        private List<ScheduledAction> _actionsToRemove = new List<ScheduledAction>();
        private List<ScheduledAction> _actionsToUpdate = new List<ScheduledAction>();
        
        // Last update time tracking
        private float _lastGameTime;
        private float _gameToRealTimeRatio;
        #endregion

        #region Events
        public event Action<float, float> OnTimeAdvanced;
        public event Action<ScheduledAction> OnActionScheduled;
        public event Action<ScheduledAction> OnActionCompleted;
        public event Action<int, DayOfWeek> OnDayChanged;
        #endregion

        #region Constructor
        public TimeResourceManager(ResourceSystemConfig.TimeConfig config)
        {
            _config = config;
            _gameToRealTimeRatio = config.gameTimeToRealTimeRatio;
            
            // Initialize action time requirements from config list
            if (config.actionTimeRequirements != null)
            {
                foreach (var requirement in config.actionTimeRequirements)
                {
                    if (!string.IsNullOrEmpty(requirement.actionId))
                    {
                        // Create a TimeRequirement from ActionTimeRequirementDefinition
                        var timeReq = new TimeRequirement
                        {
                            baseTimeCost = requirement.baseTimeCost,
                            isScalableBySkill = requirement.isScalableBySkill,
                            requiresContinuousTime = requirement.requiresContinuousTime,
                            canBeInterrupted = requirement.canBeInterrupted,
                            skillEfficiencyFactors = new Dictionary<string, float>(),
                            possibleModifiers = new List<TimeModifier>()
                        };
                        
                        // Convert skill efficiency factors if available
                        if (requirement.skillEfficiencyFactors != null)
                        {
                            foreach (var factor in requirement.skillEfficiencyFactors)
                            {
                                timeReq.skillEfficiencyFactors[factor.skillId] = factor.efficiencyFactor;
                            }
                        }
                        
                        _actionTimeRequirements[requirement.actionId] = timeReq;
                    }
                }
            }
        }
        #endregion

        #region Initialization Methods
        public void Initialize(string playerId)
        {
            _playerId = playerId;
            InitializeTime();
            InitializeTimeBlocks();
        }
        
        private void InitializeTime()
        {
            _currentDay = _config.startingDay;
            _currentHour = _config.startingHour;
            _currentDayOfWeek = _config.startingDayOfWeek;
            _currentWeek = 1;
            _currentMonth = 1;
            _currentYear = DateTime.Now.Year;
            _lastGameTime = Time.time;
        }
        
        private void InitializeTimeBlocks()
        {
            _dailyTimeBlocks.Clear();
            
            // Default time blocks if not provided in config
            if (_config == null || _config.GetType().GetField("timeBlockTemplates") == null)
            {
                // Morning block
                _dailyTimeBlocks.Add(new TimeBlock
                {
                    blockId = "morning",
                    blockName = "Morning",
                    startHour = 6,
                    endHour = 12,
                    isAvailable = true,
                    allocatedActionId = ""
                });
                
                // Afternoon block
                _dailyTimeBlocks.Add(new TimeBlock
                {
                    blockId = "afternoon",
                    blockName = "Afternoon",
                    startHour = 12,
                    endHour = 18,
                    isAvailable = true,
                    allocatedActionId = ""
                });
                
                // Evening block
                _dailyTimeBlocks.Add(new TimeBlock
                {
                    blockId = "evening",
                    blockName = "Evening",
                    startHour = 18,
                    endHour = 22,
                    isAvailable = true,
                    allocatedActionId = ""
                });
                
                // Night block
                _dailyTimeBlocks.Add(new TimeBlock
                {
                    blockId = "night",
                    blockName = "Night",
                    startHour = 22,
                    endHour = 6,
                    isAvailable = true,
                    allocatedActionId = ""
                });
            }
            else
            {
                // Use config-defined blocks if available
                var field = _config.GetType().GetField("timeBlockTemplates");
                if (field != null && field.GetValue(_config) is List<ResourceSystemConfig.TimeBlockTemplate> templates)
                {
                    foreach (var template in templates)
                    {
                        _dailyTimeBlocks.Add(new TimeBlock
                        {
                            blockId = template.id,
                            blockName = template.name,
                            startHour = template.startHour,
                            endHour = template.endHour,
                            isAvailable = true,
                            allocatedActionId = ""
                        });
                    }
                }
            }
        }
        #endregion

        #region Time Operations
        public void AdvanceTime(float hours)
        {
            if (hours <= 0)
                return;
            
            float previousHour = _currentHour;
            int previousDay = _currentDay;
            
            // Update current time
            _currentHour += hours;
            
            // Check for day change
            while (_currentHour >= 24)
            {
                _currentHour -= 24;
                _currentDay++;
                
                // Update day of week
                _currentDayOfWeek = (DayOfWeek)(((int)_currentDayOfWeek + 1) % 7);
                
                // Simplified calendar logic - could be much more complex
                if (_currentDay > 30)
                {
                    _currentDay = 1;
                    _currentMonth++;
                    
                    if (_currentMonth > 12)
                    {
                        _currentMonth = 1;
                        _currentYear++;
                    }
                }
                
                // Update week
                if (_currentDayOfWeek == DayOfWeek.Monday)
                {
                    _currentWeek++;
                }
                
                // Reset time blocks for new day
                ResetTimeBlocks();
                
                // Process day change effects
                ProcessDayChange();
                
                // Fire day changed event
                OnDayChanged?.Invoke(_currentDay, _currentDayOfWeek);
            }
            
            // Process ongoing actions
            ProcessScheduledActions(hours);
            
            // Update time modifiers
            UpdateTimeModifiers(hours);
            
            // Fire time advanced event
            OnTimeAdvanced?.Invoke(_currentHour, hours);
        }
        
        public void ProcessRealTimeUpdate(float deltaTime)
        {
            // Convert real time to game time
            float gameTimeDelta = deltaTime * _gameToRealTimeRatio;
            
            // Advance game time
            AdvanceTime(gameTimeDelta / 3600f); // Convert to hours
        }
        
        private void ProcessDayChange()
        {
            // Process daily effects
            
            // Clean up expired modifiers
            foreach (var actionId in _actionModifiers.Keys.ToList())
            {
                _actionModifiers[actionId].RemoveAll(m => m.duration > 0 && (m.remainingTime -= 24) <= 0);
            }
        }
        
        public void ResetTimeBlocks()
        {
            foreach (var block in _dailyTimeBlocks)
            {
                block.isAvailable = true;
                block.allocatedActionId = "";
            }
        }
        
        private void ProcessScheduledActions(float hours)
        {
            // Clear previous frame data
            _actionsToRemove.Clear();
            _actionsToUpdate.Clear();
            
            // Process each scheduled action
            foreach (var action in _scheduledActions)
            {
                // Skip actions not in progress
                if (action.status != ActionStatus.InProgress)
                    continue;
                
                // Update time remaining
                action.timeRemaining -= hours;
                
                // Calculate progress percentage
                float totalTime = action.estimatedEndTime - action.startTime;
                action.progressPercentage = (1f - action.timeRemaining / totalTime) * 100f;
                
                if (action.timeRemaining <= 0)
                {
                    // Action completed
                    action.status = ActionStatus.Completed;
                    action.progressPercentage = 100f;
                    
                    // Add to completion list
                    _actionsToUpdate.Add(action);
                }
            }
            
            // Process completed actions
            foreach (var action in _actionsToUpdate)
            {
                // Fire action completed event
                OnActionCompleted?.Invoke(action);
                
                // Free time blocks that were allocated for this action
                foreach (var block in _dailyTimeBlocks)
                {
                    if (block.allocatedActionId == action.actionId)
                    {
                        block.isAvailable = true;
                        block.allocatedActionId = "";
                    }
                }
                
                // Remove from scheduled list
                _actionsToRemove.Add(action);
            }
            
            // Remove completed actions
            foreach (var action in _actionsToRemove)
            {
                _scheduledActions.Remove(action);
            }
        }
        
        private void UpdateTimeModifiers(float hours)
        {
            // Update duration of temporary modifiers
            foreach (var actionId in _actionModifiers.Keys)
            {
                var modifiers = _actionModifiers[actionId];
                for (int i = modifiers.Count - 1; i >= 0; i--)
                {
                    var modifier = modifiers[i];
                    if (modifier.duration > 0)
                    {
                        modifier.remainingTime -= hours;
                        if (modifier.remainingTime <= 0)
                        {
                            modifiers.RemoveAt(i);
                        }
                    }
                }
            }
        }
        #endregion

        #region Action Scheduling
        public bool ScheduleAction(string actionId, float startTime, Dictionary<string, object> parameters = null)
        {
            // Check if action can be scheduled
            if (!HasTimeForAction(actionId))
                return false;
            
            // Get time requirement
            if (!_actionTimeRequirements.TryGetValue(actionId, out var timeRequirement))
            {
                Debug.LogWarning($"No time requirement data for action {actionId}");
                return false;
            }
            
            // Check for time block conflicts
            if (IsTimeBlockConflict(startTime, timeRequirement.baseTimeCost))
                return false;
            
            // Calculate modified time cost
            float modifiedTimeCost = CalculateTimeCost(actionId, timeRequirement);
            
            // Create scheduled action
            ScheduledAction action = new ScheduledAction
            {
                actionId = actionId,
                startTime = startTime,
                estimatedEndTime = startTime + modifiedTimeCost,
                status = ActionStatus.Pending,
                progressPercentage = 0,
                timeRemaining = modifiedTimeCost,
                prerequisites = new List<string>(),
                blockedActions = new List<string>(),
                parameters = parameters != null ? new Dictionary<string, object>(parameters) : new Dictionary<string, object>()
            };
            
            // Check for prerequisites
            if (HasUnfulfilledPrerequisites(action))
                return false;
            
            // Reserve time blocks if this is a block-based action
            ReserveTimeBlocks(action);
            
            // Add to scheduled actions
            _scheduledActions.Add(action);
            
            // Update action status
            if (startTime <= _currentHour)
            {
                action.status = ActionStatus.InProgress;
            }
            
            // Fire action scheduled event
            OnActionScheduled?.Invoke(action);
            
            return true;
        }
        
        public bool CancelAction(string actionId)
        {
            // Find action in scheduled list
            var action = _scheduledActions.Find(a => a.actionId == actionId);
            if (action == null)
                return false;
            
            // Update action status
            action.status = ActionStatus.Cancelled;
            
            // Free time blocks
            foreach (var block in _dailyTimeBlocks)
            {
                if (block.allocatedActionId == actionId)
                {
                    block.isAvailable = true;
                    block.allocatedActionId = "";
                }
            }
            
            // Remove from scheduled list
            _scheduledActions.Remove(action);
            
            return true;
        }
        
        private float CalculateTimeCost(string actionId, TimeRequirement timeRequirement)
        {
            float baseTimeCost = timeRequirement.baseTimeCost;
            
            // Apply skill efficiency if applicable
            if (timeRequirement.isScalableBySkill)
            {
                // In a real implementation, this would check player skills
                // For example:
                // foreach (var skillFactor in timeRequirement.skillEfficiencyFactors)
                // {
                //     float skillLevel = playerSkillSystem.GetSkillLevel(skillFactor.Key);
                //     baseTimeCost *= (1f - (skillLevel * skillFactor.Value));
                // }
                
                // For now, we'll use a simplified approach
                float efficiencyRating = GetEfficiencyRating(actionId);
                baseTimeCost *= (1f - efficiencyRating);
            }
            
            // Apply time modifiers
            if (_actionModifiers.TryGetValue(actionId, out var modifiers))
            {
                foreach (var modifier in modifiers)
                {
                    // Apply flat bonus (in hours)
                    baseTimeCost -= modifier.flatBonus;
                    
                    // Apply percentage modifier
                    baseTimeCost *= (1f - modifier.percentageBonus);
                }
            }
            
            // Apply time of day modifiers
            float timeOfDayFactor = GetTimeOfDayEfficiencyFactor();
            baseTimeCost *= timeOfDayFactor;
            
            // Ensure minimum time cost
            return Mathf.Max(0.1f, baseTimeCost);
        }
        
        private float GetEfficiencyRating(string actionId)
        {
            // In a real implementation, this would be based on player skills, traits, etc.
            // For now, we'll return a cached value or a default
            if (_actionEfficiencyData.TryGetValue(actionId, out float efficiency))
            {
                return efficiency;
            }
            
            return 0.0f; // Default: no efficiency bonus
        }
        
        private float GetTimeOfDayEfficiencyFactor()
        {
            // Example implementation - different times of day affect efficiency
            if (_currentHour >= 8 && _currentHour < 12)
            {
                // Morning: peak efficiency
                return 0.9f;
            }
            else if (_currentHour >= 12 && _currentHour < 15)
            {
                // Early afternoon: slightly reduced (post-lunch dip)
                return 1.1f;
            }
            else if (_currentHour >= 15 && _currentHour < 19)
            {
                // Late afternoon: moderate efficiency
                return 1.0f;
            }
            else if (_currentHour >= 19 && _currentHour < 22)
            {
                // Evening: slightly reduced
                return 1.1f;
            }
            else
            {
                // Night: significantly reduced
                return 1.3f;
            }
        }
        
        private bool IsTimeBlockConflict(float startTime, float duration)
        {
            float endTime = startTime + duration;
            
            // Check for conflicts with ongoing actions
            foreach (var action in _scheduledActions)
            {
                if (action.status != ActionStatus.Pending && action.status != ActionStatus.InProgress)
                    continue;
                
                float actionEndTime = action.startTime + (action.estimatedEndTime - action.startTime);
                
                // Check if time periods overlap
                if ((startTime >= action.startTime && startTime < actionEndTime) ||
                    (endTime > action.startTime && endTime <= actionEndTime) ||
                    (startTime <= action.startTime && endTime >= actionEndTime))
                {
                    // Check if actions can run concurrently
                    if (AreActionsExclusive(action.actionId))
                    {
                        return true; // Conflict found
                    }
                }
            }
            
            return false;
        }
        
        private bool AreActionsExclusive(string actionId)
        {
            // In a real implementation, this would check if the actions are mutually exclusive
            // For now, we'll assume most actions are exclusive (can't be performed simultaneously)
            return true;
        }
        
        private bool HasUnfulfilledPrerequisites(ScheduledAction action)
        {
            // Check if all prerequisites are completed
            foreach (var prerequisite in action.prerequisites)
            {
                bool isCompleted = _scheduledActions.Any(a => a.actionId == prerequisite && a.status == ActionStatus.Completed);
                if (!isCompleted)
                {
                    return true; // Unfulfilled prerequisite found
                }
            }
            
            return false;
        }
        
        private void ReserveTimeBlocks(ScheduledAction action)
        {
            // Find time blocks that overlap with this action
            float startTime = action.startTime;
            float endTime = action.estimatedEndTime;
            
            foreach (var block in _dailyTimeBlocks)
            {
                // Handle overnight blocks (e.g., 22:00 - 06:00)
                float blockEnd = block.endHour;
                if (blockEnd < block.startHour)
                {
                    blockEnd += 24; // Add a day
                }
                
                // Check if action overlaps with block
                if ((startTime >= block.startHour && startTime < blockEnd) ||
                    (endTime > block.startHour && endTime <= blockEnd) ||
                    (startTime <= block.startHour && endTime >= blockEnd))
                {
                    // Reserve block
                    block.isAvailable = false;
                    block.allocatedActionId = action.actionId;
                }
            }
        }
        #endregion

        #region Action Validation
        public bool HasTimeForAction(string actionId)
        {
            // Get time requirement
            if (!_actionTimeRequirements.TryGetValue(actionId, out var timeRequirement))
            {
                return true; // No time requirement, so it's allowed
            }
            
            // Calculate modified time cost
            float modifiedTimeCost = CalculateTimeCost(actionId, timeRequirement);
            
            // Check if continuous time is required
            if (timeRequirement.requiresContinuousTime)
            {
                // Check if there's a continuous block of time available
                return HasContinuousTimeBlock(_currentHour, modifiedTimeCost);
            }
            
            // Simple check - just need enough time in the day
            return (24 - _currentHour) >= modifiedTimeCost;
        }
        
        private bool HasContinuousTimeBlock(float startTime, float duration)
        {
            float endTime = startTime + duration;
            
            // Check if goes past midnight
            if (endTime >= 24)
            {
                // Simplified - assume next day is always available
                return true;
            }
            
            // Check for conflicts with scheduled actions
            return !IsTimeBlockConflict(startTime, duration);
        }
        
        public float GetActionTimeCost(string actionId)
        {
            if (!_actionTimeRequirements.TryGetValue(actionId, out var timeRequirement))
            {
                return 0f; // No time requirement
            }
            
            return CalculateTimeCost(actionId, timeRequirement);
        }
        
        public ActionValidationResult ValidateAction(string actionId)
        {
            // Check if time requirement exists
            if (!_actionTimeRequirements.TryGetValue(actionId, out var timeRequirement))
            {
                return new ActionValidationResult { isValid = true }; // No time requirement, so it's valid
            }
            
            // Check if there's enough time
            if (!HasTimeForAction(actionId))
            {
                float timeCost = CalculateTimeCost(actionId, timeRequirement);
                
                return new ActionValidationResult
                {
                    isValid = false,
                    message = $"Not enough time for this action. Requires {timeCost:F1} hours."
                };
            }
            
            // Check for time block conflicts
            if (timeRequirement.requiresContinuousTime && IsTimeBlockConflict(_currentHour, timeRequirement.baseTimeCost))
            {
                return new ActionValidationResult
                {
                    isValid = false,
                    message = "This action conflicts with already scheduled activities."
                };
            }
            
            return new ActionValidationResult { isValid = true };
        }
        #endregion

        #region Action Processing
        public void ProcessAction(string actionId, Dictionary<string, object> parameters, ResourceTransactionBatch batch)
        {
            // Check if time should be consumed immediately
            bool consumeTimeImmediately = false;
            if (parameters != null && parameters.TryGetValue("consumeTimeImmediately", out object consumeParam))
            {
                if (consumeParam is bool boolValue)
                {
                    consumeTimeImmediately = boolValue;
                }
            }
            
            // Get time requirement
            if (!_actionTimeRequirements.TryGetValue(actionId, out var timeRequirement))
            {
                // No time requirement, nothing to process
                return;
            }
            
            // Calculate time cost
            float timeCost = CalculateTimeCost(actionId, timeRequirement);
            
            if (consumeTimeImmediately)
            {
                // Store current time for rollback
                float previousHour = _currentHour;
                int previousDay = _currentDay;
                
                // Create resource change record
                ResourceChange change = new ResourceChange
                {
                    resourceType = ResourceType.Time,
                    resourceId = "gameTime",
                    amount = timeCost,
                    source = actionId
                };
                
                // Create rollback action
                Action rollback = () => 
                {
                    _currentHour = previousHour;
                    _currentDay = previousDay;
                };
                
                // Add to batch
                batch.AddChange(change, rollback);
                
                // Apply change
                AdvanceTime(timeCost);
            }
            else
            {
                // Schedule the action instead
                float startTime = _currentHour;
                
                if (parameters != null && parameters.TryGetValue("startTime", out object startParam))
                {
                    if (startParam is float floatValue)
                    {
                        startTime = floatValue;
                    }
                }
                
                // Create resource change record
                ResourceChange change = new ResourceChange
                {
                    resourceType = ResourceType.Time,
                    resourceId = "scheduledAction",
                    amount = timeCost,
                    source = actionId
                };
                
                // Create rollback action
                Action rollback = () => 
                {
                    // Cancel the scheduled action
                    var scheduledAction = _scheduledActions.Find(a => a.actionId == actionId && a.status == ActionStatus.Pending);
                    if (scheduledAction != null)
                    {
                        CancelAction(actionId);
                    }
                };
                
                // Add to batch
                batch.AddChange(change, rollback);
                
                // Schedule the action
                ScheduleAction(actionId, startTime, parameters);
            }
        }
        #endregion

        #region Public Interface Methods
        public float GetCurrentTime()
        {
            return _currentHour;
        }
        
        public TimeState GetTimeState()
        {
            // Get available time blocks
            List<ScheduledAction> activeActions = _scheduledActions
                .Where(a => a.status == ActionStatus.Pending || a.status == ActionStatus.InProgress)
                .ToList();
            
            // Get action efficiency data
            Dictionary<string, float> efficiencyData = new Dictionary<string, float>();
            foreach (var pair in _actionTimeRequirements)
            {
                efficiencyData[pair.Key] = GetEfficiencyRating(pair.Key);
            }
            
            return new TimeState
            {
                currentDay = _currentDay,
                currentHour = _currentHour,
                currentDayOfWeek = _currentDayOfWeek,
                currentWeek = _currentWeek,
                currentMonth = _currentMonth,
                currentYear = _currentYear,
                scheduledActions = activeActions,
                actionTimeEfficiency = efficiencyData
            };
        }
        
        public List<ScheduledAction> GetScheduledActions()
        {
            return new List<ScheduledAction>(_scheduledActions);
        }
        
        public bool AllocateTimeBlock(string blockId, string actionId)
        {
            // Find the block
            var block = _dailyTimeBlocks.Find(b => b.blockId == blockId);
            if (block == null)
                return false;
            
            // Check if block is available
            if (!block.isAvailable)
                return false;
            
            // Reserve block
            block.isAvailable = false;
            block.allocatedActionId = actionId;
            
            return true;
        }
        
        public void AddTimeModifier(string actionId, TimeModifier modifier)
        {
            if (!_actionModifiers.ContainsKey(actionId))
            {
                _actionModifiers[actionId] = new List<TimeModifier>();
            }
            
            _actionModifiers[actionId].Add(modifier);
        }
        
        public void SetActionEfficiency(string actionId, float efficiency)
        {
            _actionEfficiencyData[actionId] = Mathf.Clamp01(efficiency);
        }
        
        public List<ResourceOptimizationSuggestion> GetOptimizationSuggestions()
        {
            List<ResourceOptimizationSuggestion> suggestions = new List<ResourceOptimizationSuggestion>();
            
            // Check for inefficient time usage
            string currentTimeBlock = GetCurrentTimeBlock();
            if (!string.IsNullOrEmpty(currentTimeBlock))
            {
                var block = _dailyTimeBlocks.Find(b => b.blockId == currentTimeBlock);
                if (block != null && block.isAvailable)
                {
                    suggestions.Add(new ResourceOptimizationSuggestion
                    {
                        suggestionId = "unused_time_block",
                        title = $"Unused {block.blockName} Time",
                        description = $"You're currently in the {block.blockName} time block with no scheduled activity. Consider using this time productively.",
                        potentialBenefit = (block.endHour - _currentHour) * 10, // Value of time depends on duration
                        primaryResourceType = ResourceType.Time,
                        priority = 8,
                        actionSteps = new List<string>
                        {
                            "Schedule an activity for this time block",
                            "Use time for skill improvement",
                            "Complete pending tasks that require time"
                        }
                    });
                }
            }
            
            // Check for time-efficient action alternatives
            Dictionary<string, float> inefficientActions = new Dictionary<string, float>();
            
            foreach (var pair in _actionTimeRequirements)
            {
                string actionId = pair.Key;
                float efficiency = GetEfficiencyRating(actionId);
                
                if (efficiency < 0.3f) // Below 30% efficiency
                {
                    inefficientActions[actionId] = efficiency;
                }
            }
            
            if (inefficientActions.Count > 0)
            {
                var worstAction = inefficientActions.OrderBy(pair => pair.Value).First();
                
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "inefficient_action",
                    title = "Improve Action Efficiency",
                    description = $"You have low efficiency ({worstAction.Value:P0}) for the '{worstAction.Key}' action. Improving related skills would save time.",
                    potentialBenefit = 0.5f - worstAction.Value, // Potential efficiency gain
                    primaryResourceType = ResourceType.Time,
                    priority = 6,
                    actionSteps = new List<string>
                    {
                        "Train relevant skills to improve efficiency",
                        "Use tools or items that boost time efficiency",
                        "Consider delegating this action if possible"
                    }
                });
            }
            
            // Check for optimal time of day
            float currentEfficiencyFactor = GetTimeOfDayEfficiencyFactor();
            if (currentEfficiencyFactor > 1.1f) // Suboptimal time
            {
                suggestions.Add(new ResourceOptimizationSuggestion
                {
                    suggestionId = "suboptimal_time",
                    title = "Suboptimal Time of Day",
                    description = "Current time of day reduces efficiency of actions. Consider rescheduling important tasks to morning hours.",
                    potentialBenefit = currentEfficiencyFactor - 0.9f, // Potential efficiency gain
                    primaryResourceType = ResourceType.Time,
                    priority = 5,
                    actionSteps = new List<string>
                    {
                        "Schedule important tasks during morning hours",
                        "Use current time for less efficiency-sensitive tasks",
                        "Rest to recover energy if needed"
                    }
                });
            }
            
            return suggestions;
        }
        
        public Dictionary<string, object> GetAnalyticsData()
        {
            Dictionary<string, object> analytics = new Dictionary<string, object>();
            
            // Time usage
            Dictionary<string, float> timeUsage = new Dictionary<string, float>();
            foreach (var block in _dailyTimeBlocks)
            {
                if (!block.isAvailable && !string.IsNullOrEmpty(block.allocatedActionId))
                {
                    float duration = block.endHour - block.startHour;
                    if (duration < 0) duration += 24; // Handle overnight blocks
                    
                    if (!timeUsage.ContainsKey(block.allocatedActionId))
                    {
                        timeUsage[block.allocatedActionId] = 0;
                    }
                    timeUsage[block.allocatedActionId] += duration;
                }
            }
            analytics["timeUsage"] = timeUsage;
            
            // Action efficiency
            analytics["actionEfficiency"] = new Dictionary<string, float>(_actionEfficiencyData);
            
            // Schedule stats
            int pendingActions = _scheduledActions.Count(a => a.status == ActionStatus.Pending);
            int inProgressActions = _scheduledActions.Count(a => a.status == ActionStatus.InProgress);
            int completedActions = _scheduledActions.Count(a => a.status == ActionStatus.Completed);
            
            analytics["pendingActions"] = pendingActions;
            analytics["inProgressActions"] = inProgressActions;
            analytics["completedActions"] = completedActions;
            
            // Time of day distribution
            Dictionary<string, int> timeOfDayActivity = new Dictionary<string, int>();
            timeOfDayActivity["morning"] = 0;   // 6-12
            timeOfDayActivity["afternoon"] = 0; // 12-18
            timeOfDayActivity["evening"] = 0;   // 18-22
            timeOfDayActivity["night"] = 0;     // 22-6
            
            foreach (var action in _scheduledActions)
            {
                float hour = action.startTime;
                
                if (hour >= 6 && hour < 12)
                    timeOfDayActivity["morning"]++;
                else if (hour >= 12 && hour < 18)
                    timeOfDayActivity["afternoon"]++;
                else if (hour >= 18 && hour < 22)
                    timeOfDayActivity["evening"]++;
                else
                    timeOfDayActivity["night"]++;
            }
            
            analytics["timeOfDayActivity"] = timeOfDayActivity;
            
            return analytics;
        }
        
        private string GetCurrentTimeBlock()
        {
            foreach (var block in _dailyTimeBlocks)
            {
                float blockEnd = block.endHour;
                if (blockEnd < block.startHour)
                {
                    blockEnd += 24; // Add a day
                }
                
                if (_currentHour >= block.startHour && _currentHour < blockEnd)
                {
                    return block.blockId;
                }
            }
            
            return "";
        }
        #endregion

        #region Save/Load
        public TimeResourceSaveData GenerateSaveData()
        {
            TimeResourceSaveData saveData = new TimeResourceSaveData
            {
                playerId = _playerId,
                currentDay = _currentDay,
                currentHour = _currentHour,
                currentDayOfWeek = _currentDayOfWeek,
                currentWeek = _currentWeek,
                currentMonth = _currentMonth,
                currentYear = _currentYear,
                scheduledActions = new List<ScheduledAction>(_scheduledActions),
                activeModifiers = new List<TimeResourceSaveData.SerializedTimeModifier>()
            };
            
            // Serialize modifiers
            foreach (var pair in _actionModifiers)
            {
                foreach (var modifier in pair.Value)
                {
                    saveData.activeModifiers.Add(new TimeResourceSaveData.SerializedTimeModifier
                    {
                        actionId = pair.Key,
                        modifier = modifier
                    });
                }
            }
            
            return saveData;
        }
        
        public void RestoreFromSaveData(TimeResourceSaveData saveData)
        {
            if (saveData == null)
                return;
            
            _playerId = saveData.playerId;
            _currentDay = saveData.currentDay;
            _currentHour = saveData.currentHour;
            _currentDayOfWeek = saveData.currentDayOfWeek;
            _currentWeek = saveData.currentWeek;
            _currentMonth = saveData.currentMonth;
            _currentYear = saveData.currentYear;
            
            // Restore scheduled actions
            _scheduledActions = new List<ScheduledAction>(saveData.scheduledActions);
            
            // Restore modifiers
            _actionModifiers.Clear();
            foreach (var serializedModifier in saveData.activeModifiers)
            {
                if (!_actionModifiers.ContainsKey(serializedModifier.actionId))
                {
                    _actionModifiers[serializedModifier.actionId] = new List<TimeModifier>();
                }
                
                _actionModifiers[serializedModifier.actionId].Add(serializedModifier.modifier);
            }
            
            // Update time blocks based on scheduled actions
            ResetTimeBlocks();
            foreach (var action in _scheduledActions)
            {
                if (action.status == ActionStatus.Pending || action.status == ActionStatus.InProgress)
                {
                    ReserveTimeBlocks(action);
                }
            }
        }
        #endregion

        #region Supporting Classes
        [Serializable]
        public class TimeBlock
        {
            public string blockId;
            public string blockName;
            public float startHour;
            public float endHour;
            public bool isAvailable;
            public string allocatedActionId;
        }
        #endregion
    }
}