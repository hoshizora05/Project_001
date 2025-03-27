using System;
using System.Collections.Generic;
using UnityEngine;

namespace LifeResourceSystem
{
    public class TimeManager
    {
        private TimeResourceSystem timeData;
        private LifeResourceConfig.TimeConfig config;
        
        // Events
        public event Action<float, float> OnTimeAdvanced; // New time, delta
        public event Action<int, TimeResourceSystem.DayOfWeek> OnDayChanged; // New day, day of week
        
        public TimeManager(LifeResourceConfig.TimeConfig config)
        {
            this.config = config;
            
            // Initialize time data
            timeData = new TimeResourceSystem
            {
                currentDay = config.startingDay,
                currentHour = 8.0f, // Start at 8:00 AM
                currentDayOfWeek = config.startingDayOfWeek,
                currentWeek = 1,
                currentMonth = config.startingMonth,
                currentYear = config.startingYear,
                dailyTimeBlocks = new List<TimeResourceSystem.TimeBlock>(),
                actionTimeCosts = new Dictionary<string, float>()
            };
            
            // Initialize time blocks from templates
            InitializeTimeBlocks();
        }
        
        private void InitializeTimeBlocks()
        {
            timeData.dailyTimeBlocks.Clear();
            
            // Create time blocks from templates
            foreach (var template in config.timeBlockTemplates)
            {
                timeData.dailyTimeBlocks.Add(new TimeResourceSystem.TimeBlock
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
        
        public void AdvanceTime(float hoursDelta)
        {
            float oldHour = timeData.currentHour;
            timeData.currentHour += hoursDelta;
            
            // Handle day change
            bool dayChanged = false;
            while (timeData.currentHour >= 24.0f)
            {
                timeData.currentHour -= 24.0f;
                timeData.currentDay++;
                dayChanged = true;
                
                // Update day of week
                int dayOfWeekIndex = (int)timeData.currentDayOfWeek;
                dayOfWeekIndex = (dayOfWeekIndex + 1) % 7;
                timeData.currentDayOfWeek = (TimeResourceSystem.DayOfWeek)dayOfWeekIndex;
                
                // Handle week change
                if (timeData.currentDayOfWeek == TimeResourceSystem.DayOfWeek.Monday)
                {
                    timeData.currentWeek++;
                }
                
                // Handle month change (simplified, assuming 30 days per month)
                if (timeData.currentDay > 30)
                {
                    timeData.currentDay = 1;
                    timeData.currentMonth++;
                    
                    // Handle year change
                    if (timeData.currentMonth > 12)
                    {
                        timeData.currentMonth = 1;
                        timeData.currentYear++;
                    }
                }
            }
            
            // Fire events
            OnTimeAdvanced?.Invoke(timeData.currentHour, hoursDelta);
            
            if (dayChanged)
            {
                OnDayChanged?.Invoke(timeData.currentDay, timeData.currentDayOfWeek);
            }
        }
        
        public void ResetTimeBlocks()
        {
            // Clear all time block allocations
            foreach (var block in timeData.dailyTimeBlocks)
            {
                block.isAvailable = true;
                block.allocatedActionId = "";
            }
        }
        
        public bool AllocateTimeBlock(string blockId, string actionId)
        {
            // Find the time block
            var block = timeData.dailyTimeBlocks.Find(b => b.blockId == blockId);
            if (block == null || !block.isAvailable)
                return false;
                
            // Check if current time allows allocation (can't allocate past blocks)
            if (block.endHour < timeData.currentHour)
                return false;
                
            // Allocate the block
            block.isAvailable = false;
            block.allocatedActionId = actionId;
            
            return true;
        }
        
        public bool HasTimeForAction(string actionId)
        {
            // Check if we have the time cost for this action
            if (!timeData.actionTimeCosts.ContainsKey(actionId))
                return true; // No time cost defined
                
            float timeCost = timeData.actionTimeCosts[actionId];
            
            // Find available time blocks that cover the needed time
            float availableTime = 0;
            foreach (var block in timeData.dailyTimeBlocks)
            {
                if (block.isAvailable && block.startHour >= timeData.currentHour)
                {
                    availableTime += (block.endHour - block.startHour);
                    
                    if (availableTime >= timeCost)
                        return true;
                }
            }
            
            return false;
        }
        
        public TimeState GetTimeState()
        {
            List<TimeResourceSystem.TimeBlock> availableBlocks = new List<TimeResourceSystem.TimeBlock>();
            
            foreach (var block in timeData.dailyTimeBlocks)
            {
                if (block.isAvailable && block.startHour >= timeData.currentHour)
                {
                    availableBlocks.Add(block);
                }
            }
            
            return new TimeState
            {
                day = timeData.currentDay,
                hour = timeData.currentHour,
                dayOfWeek = timeData.currentDayOfWeek,
                week = timeData.currentWeek,
                month = timeData.currentMonth,
                year = timeData.currentYear,
                availableTimeBlocks = availableBlocks
            };
        }
        
        public TimeResourceSaveData GenerateSaveData()
        {
            var serializableBlocks = new List<TimeResourceSaveData.SerializableTimeBlock>();
            
            foreach (var block in timeData.dailyTimeBlocks)
            {
                serializableBlocks.Add(new TimeResourceSaveData.SerializableTimeBlock
                {
                    blockId = block.blockId,
                    blockName = block.blockName,
                    startHour = block.startHour,
                    endHour = block.endHour,
                    isAvailable = block.isAvailable,
                    allocatedActionId = block.allocatedActionId
                });
            }
            
            return new TimeResourceSaveData
            {
                currentDay = timeData.currentDay,
                currentHour = timeData.currentHour,
                currentDayOfWeek = (int)timeData.currentDayOfWeek,
                currentWeek = timeData.currentWeek,
                currentMonth = timeData.currentMonth,
                currentYear = timeData.currentYear,
                timeBlocks = serializableBlocks
            };
        }
        
        public void RestoreFromSaveData(TimeResourceSaveData saveData)
        {
            timeData.currentDay = saveData.currentDay;
            timeData.currentHour = saveData.currentHour;
            timeData.currentDayOfWeek = (TimeResourceSystem.DayOfWeek)saveData.currentDayOfWeek;
            timeData.currentWeek = saveData.currentWeek;
            timeData.currentMonth = saveData.currentMonth;
            timeData.currentYear = saveData.currentYear;
            
            // Restore time blocks
            timeData.dailyTimeBlocks.Clear();
            
            foreach (var savedBlock in saveData.timeBlocks)
            {
                timeData.dailyTimeBlocks.Add(new TimeResourceSystem.TimeBlock
                {
                    blockId = savedBlock.blockId,
                    blockName = savedBlock.blockName,
                    startHour = savedBlock.startHour,
                    endHour = savedBlock.endHour,
                    isAvailable = savedBlock.isAvailable,
                    allocatedActionId = savedBlock.allocatedActionId
                });
            }
        }
    }
}