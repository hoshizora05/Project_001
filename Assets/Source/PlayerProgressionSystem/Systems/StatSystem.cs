using System;
using UnityEngine;
using PlayerProgression.Data;
using PlayerProgression.Interfaces;

namespace PlayerProgression.Systems
{
    public class StatSystem : IStatSystem
    {
        private PlayerStats playerStats = new PlayerStats();
        
        public void Initialize(string playerId, PlayerProgressionConfig config)
        {
            playerStats.playerId = playerId;
            
            foreach (var statConfig in config.initialStats)
            {
                playerStats.stats[statConfig.statId] = new PlayerStats.Stat(
                    statConfig.baseValue,
                    statConfig.minValue,
                    statConfig.maxValue,
                    statConfig.growthRate
                );
            }
        }
        
        public void Update(float deltaTime)
        {
            foreach (var statPair in playerStats.stats)
            {
                var stat = statPair.Value;
                
                // Update modifiers and remove expired ones
                for (int i = stat.modifiers.Count - 1; i >= 0; i--)
                {
                    var mod = stat.modifiers[i];
                    if (mod.duration > 0)
                    {
                        mod.remainingTime -= deltaTime;
                        if (mod.remainingTime <= 0)
                        {
                            stat.modifiers.RemoveAt(i);
                        }
                    }
                }
                
                // Recalculate current value
                RecalculateStatValue(statPair.Key);
            }
        }
        
        public void ProcessEvent(ProgressionEvent evt)
        {
            if (evt.type == ProgressionEvent.ProgressionEventType.StatChange)
            {
                string statId = (string)evt.parameters["statId"];
                
                if (evt.parameters.TryGetValue("baseValueChange", out object baseValueChangeObj))
                {
                    float baseValueChange = Convert.ToSingle(baseValueChangeObj);
                    if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
                    {
                        stat.baseValue = Mathf.Clamp(stat.baseValue + baseValueChange, stat.minValue, stat.maxValue);
                        RecalculateStatValue(statId);
                    }
                }
                
                if (evt.parameters.TryGetValue("modifier", out object modifierObj))
                {
                    PlayerStats.StatModifier modifier = (PlayerStats.StatModifier)modifierObj;
                    ApplyModifier(statId, modifier);
                }
            }
        }
        
        public StatValue GetStatValue(string statId)
        {
            if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
            {
                return new StatValue(stat.baseValue, stat.currentValue, stat.minValue, stat.maxValue);
            }
            
            return new StatValue(0, 0, 0, 0);
        }
        
        public float GetStatBaseValue(string statId)
        {
            if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
            {
                return stat.baseValue;
            }
            
            return 0;
        }
        
        public void ApplyModifier(string statId, PlayerStats.StatModifier modifier)
        {
            if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
            {
                stat.modifiers.Add(modifier);
                RecalculateStatValue(statId);
            }
        }
        
        public void RemoveModifiersFromSource(string statId, string source)
        {
            if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
            {
                stat.modifiers.RemoveAll(mod => mod.source == source);
                RecalculateStatValue(statId);
            }
        }
        
        private void RecalculateStatValue(string statId)
        {
            if (playerStats.stats.TryGetValue(statId, out PlayerStats.Stat stat))
            {
                float baseValue = stat.baseValue;
                float additiveTotal = 0;
                float multiplicativeTotal = 1.0f;
                float overrideValue = float.MinValue;
                
                foreach (var mod in stat.modifiers)
                {
                    switch (mod.type)
                    {
                        case PlayerStats.ModifierType.Additive:
                            additiveTotal += mod.value;
                            break;
                        case PlayerStats.ModifierType.Multiplicative:
                            multiplicativeTotal *= (1.0f + mod.value);
                            break;
                        case PlayerStats.ModifierType.Override:
                            overrideValue = Mathf.Max(overrideValue, mod.value);
                            break;
                    }
                }
                
                if (overrideValue > float.MinValue)
                {
                    stat.currentValue = Mathf.Clamp(overrideValue, stat.minValue, stat.maxValue);
                }
                else
                {
                    stat.currentValue = Mathf.Clamp((baseValue + additiveTotal) * multiplicativeTotal, stat.minValue, stat.maxValue);
                }
            }
        }
        
        public StatSystemSaveData GenerateSaveData()
        {
            var saveData = new StatSystemSaveData
            {
                playerId = playerStats.playerId
            };
            
            foreach (var statPair in playerStats.stats)
            {
                var statData = new StatSystemSaveData.StatData
                {
                    statId = statPair.Key,
                    baseValue = statPair.Value.baseValue,
                    currentValue = statPair.Value.currentValue,
                    minValue = statPair.Value.minValue,
                    maxValue = statPair.Value.maxValue,
                    growthRate = statPair.Value.growthRate
                };
                
                foreach (var mod in statPair.Value.modifiers)
                {
                    statData.modifiers.Add(new StatSystemSaveData.StatModifierData
                    {
                        source = mod.source,
                        value = mod.value,
                        type = mod.type,
                        duration = mod.duration,
                        remainingTime = mod.remainingTime
                    });
                }
                
                saveData.stats.Add(statData);
            }
            
            return saveData;
        }
        
        public void RestoreFromSaveData(StatSystemSaveData saveData)
        {
            playerStats.playerId = saveData.playerId;
            playerStats.stats.Clear();
            
            foreach (var statData in saveData.stats)
            {
                var stat = new PlayerStats.Stat(
                    statData.baseValue,
                    statData.minValue,
                    statData.maxValue,
                    statData.growthRate
                );
                
                stat.currentValue = statData.currentValue;
                
                foreach (var modData in statData.modifiers)
                {
                    stat.modifiers.Add(new PlayerStats.StatModifier(
                        modData.source,
                        modData.value,
                        modData.type,
                        modData.duration
                    ) 
                    { 
                        remainingTime = modData.remainingTime 
                    });
                }
                
                playerStats.stats[statData.statId] = stat;
            }
        }
    }
}