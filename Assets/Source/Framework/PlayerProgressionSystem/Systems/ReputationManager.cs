using System;
using System.Collections.Generic;
using UnityEngine;
using PlayerProgression.Data;
using PlayerProgression.Interfaces;

namespace PlayerProgression.Systems
{
    public class ReputationManager : IReputationSystem
    {
        private SocialStandingSystem socialSystem = new SocialStandingSystem();
        private Dictionary<string, Dictionary<string, SocialStandingSystem.SocialLabel>> contextLabels = 
            new Dictionary<string, Dictionary<string, SocialStandingSystem.SocialLabel>>();
        
        public void Initialize(string playerId, PlayerProgressionConfig config)
        {
            socialSystem.playerId = playerId;
            
            foreach (var contextConfig in config.reputationContexts)
            {
                var reputation = new SocialStandingSystem.Reputation(
                    contextConfig.contextId,
                    contextConfig.contextName
                );
                
                foreach (var trait in contextConfig.relevantTraits)
                {
                    reputation.traitScores[trait] = 0;
                }
                
                socialSystem.reputations[contextConfig.contextId] = reputation;
            }
        }
        
        public void Update(float deltaTime)
        {
            // Update reputation event decay
            foreach (var reputationPair in socialSystem.reputations)
            {
                var reputation = reputationPair.Value;
                
                // Process reputation event decay
                for (int i = reputation.recentEvents.Count - 1; i >= 0; i--)
                {
                    var evt = reputation.recentEvents[i];
                    bool eventExpired = true;
                    
                    foreach (var impactPair in evt.impacts)
                    {
                        float decayAmount = evt.decayRate * deltaTime;
                        
                        if (Mathf.Abs(impactPair.Value) > decayAmount)
                        {
                            // Event still has impact
                            float newImpact = impactPair.Value > 0 ? 
                                impactPair.Value - decayAmount : 
                                impactPair.Value + decayAmount;
                            
                            evt.impacts[impactPair.Key] = newImpact;
                            eventExpired = false;
                        }
                        else
                        {
                            // Impact has decayed to zero
                            evt.impacts[impactPair.Key] = 0;
                        }
                    }
                    
                    if (eventExpired)
                    {
                        reputation.recentEvents.RemoveAt(i);
                    }
                }
                
                // Recalculate reputation scores
                RecalculateReputationScores(reputationPair.Key);
            }
            
            // Update social labels
            foreach (var contextPair in contextLabels)
            {
                string contextId = contextPair.Key;
                
                if (socialSystem.reputations.TryGetValue(contextId, out SocialStandingSystem.Reputation reputation))
                {
                    foreach (var labelPair in contextPair.Value)
                    {
                        var label = labelPair.Value;
                        bool shouldBeActive = true;
                        
                        foreach (var thresholdPair in label.thresholds)
                        {
                            string traitId = thresholdPair.Key;
                            float requiredValue = thresholdPair.Value;
                            
                            if (reputation.traitScores.TryGetValue(traitId, out float traitScore))
                            {
                                if (traitScore < requiredValue)
                                {
                                    shouldBeActive = false;
                                    break;
                                }
                            }
                            else
                            {
                                shouldBeActive = false;
                                break;
                            }
                        }
                        
                        label.isActive = shouldBeActive;
                    }
                }
            }
        }
        
        public void ProcessEvent(ProgressionEvent evt)
        {
            if (evt.type == ProgressionEvent.ProgressionEventType.ReputationImpact)
            {
                string contextId = (string)evt.parameters["contextId"];
                
                if (evt.parameters.TryGetValue("reputationEvent", out object reputationEventObj))
                {
                    var reputationEvent = (SocialStandingSystem.ReputationEvent)reputationEventObj;
                    AddReputationEvent(contextId, reputationEvent);
                }
                else if (evt.parameters.TryGetValue("traitImpacts", out object traitImpactsObj))
                {
                    var traitImpacts = (Dictionary<string, float>)traitImpactsObj;
                    string eventId = evt.parameters.TryGetValue("eventId", out object eventIdObj) ? 
                        (string)eventIdObj : "generic_event_" + DateTime.Now.Ticks;
                    
                    string description = evt.parameters.TryGetValue("description", out object descriptionObj) ? 
                        (string)descriptionObj : "Generic reputation event";
                    
                    float decayRate = evt.parameters.TryGetValue("decayRate", out object decayRateObj) ? 
                        Convert.ToSingle(decayRateObj) : 0.1f;
                    
                    var reputationEvent = new SocialStandingSystem.ReputationEvent(
                        eventId,
                        description,
                        Time.time,
                        decayRate
                    );
                    
                    foreach (var impact in traitImpacts)
                    {
                        reputationEvent.impacts[impact.Key] = impact.Value;
                    }
                    
                    AddReputationEvent(contextId, reputationEvent);
                }
            }
        }
        
        public float GetReputationScore(string contextId, string traitId = "")
        {
            if (socialSystem.reputations.TryGetValue(contextId, out SocialStandingSystem.Reputation reputation))
            {
                if (string.IsNullOrEmpty(traitId))
                {
                    return reputation.overallScore;
                }
                else if (reputation.traitScores.TryGetValue(traitId, out float traitScore))
                {
                    return traitScore;
                }
            }
            
            return 0;
        }
        
        public void AddReputationEvent(string contextId, SocialStandingSystem.ReputationEvent evt)
        {
            if (socialSystem.reputations.TryGetValue(contextId, out SocialStandingSystem.Reputation reputation))
            {
                reputation.recentEvents.Add(evt);
                
                foreach (var impactPair in evt.impacts)
                {
                    string traitId = impactPair.Key;
                    float impactValue = impactPair.Value;
                    
                    if (!reputation.traitScores.ContainsKey(traitId))
                    {
                        reputation.traitScores[traitId] = 0;
                    }
                    
                    reputation.traitScores[traitId] += impactValue;
                }
                
                RecalculateReputationScores(contextId);
            }
        }
        
        private void RecalculateReputationScores(string contextId)
        {
            if (socialSystem.reputations.TryGetValue(contextId, out SocialStandingSystem.Reputation reputation))
            {
                float totalTraitScore = 0;
                int traitCount = 0;
                
                foreach (var traitPair in reputation.traitScores)
                {
                    totalTraitScore += traitPair.Value;
                    traitCount++;
                }
                
                reputation.overallScore = traitCount > 0 ? totalTraitScore / traitCount : 0;
            }
        }
        
        public ReputationSystemSaveData GenerateSaveData()
        {
            var saveData = new ReputationSystemSaveData
            {
                playerId = socialSystem.playerId
            };
            
            foreach (var reputationPair in socialSystem.reputations)
            {
                var reputationData = new ReputationSystemSaveData.ReputationData
                {
                    contextId = reputationPair.Value.contextId,
                    contextName = reputationPair.Value.contextName,
                    overallScore = reputationPair.Value.overallScore
                };
                
                foreach (var traitPair in reputationPair.Value.traitScores)
                {
                    reputationData.traitScores.Add(new ReputationSystemSaveData.TraitScoreData
                    {
                        traitId = traitPair.Key,
                        score = traitPair.Value
                    });
                }
                
                foreach (var evt in reputationPair.Value.recentEvents)
                {
                    var eventData = new ReputationSystemSaveData.ReputationEventData
                    {
                        eventId = evt.eventId,
                        description = evt.description,
                        timestamp = evt.timestamp,
                        decayRate = evt.decayRate
                    };
                    
                    foreach (var impactPair in evt.impacts)
                    {
                        eventData.impacts.Add(new ReputationSystemSaveData.TraitImpactData
                        {
                            traitId = impactPair.Key,
                            impact = impactPair.Value
                        });
                    }
                    
                    reputationData.recentEvents.Add(eventData);
                }
                
                saveData.reputations.Add(reputationData);
            }
            
            return saveData;
        }
        
        public void RestoreFromSaveData(ReputationSystemSaveData saveData)
        {
            socialSystem.playerId = saveData.playerId;
            socialSystem.reputations.Clear();
            
            foreach (var reputationData in saveData.reputations)
            {
                var reputation = new SocialStandingSystem.Reputation(
                    reputationData.contextId,
                    reputationData.contextName
                );
                
                reputation.overallScore = reputationData.overallScore;
                
                foreach (var traitData in reputationData.traitScores)
                {
                    reputation.traitScores[traitData.traitId] = traitData.score;
                }
                
                foreach (var eventData in reputationData.recentEvents)
                {
                    var evt = new SocialStandingSystem.ReputationEvent(
                        eventData.eventId,
                        eventData.description,
                        eventData.timestamp,
                        eventData.decayRate
                    );
                    
                    foreach (var impactData in eventData.impacts)
                    {
                        evt.impacts[impactData.traitId] = impactData.impact;
                    }
                    
                    reputation.recentEvents.Add(evt);
                }
                
                socialSystem.reputations[reputationData.contextId] = reputation;
            }
        }
    }
}