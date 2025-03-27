using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LifeResourceSystem
{
    public class SocialCreditManager
    {
        private SocialCreditSystem socialData;
        private LifeResourceConfig.SocialCreditConfig config;
        
        // Events
        public event Action<string, float, float> OnSocialCreditChanged; // Context ID, new value, delta
        
        public SocialCreditManager(string playerId, LifeResourceConfig.SocialCreditConfig config)
        {
            this.config = config;
            
            socialData = new SocialCreditSystem
            {
                playerId = playerId,
                socialCredits = new Dictionary<string, SocialCreditSystem.SocialCredit>()
            };
            
            // Initialize social contexts from config
            foreach (var context in config.socialContexts)
            {
                // Find current tier for this starting score
                var currentTier = FindTierForScore(context.tiers, context.startingScore);
                
                socialData.socialCredits[context.contextId] = new SocialCreditSystem.SocialCredit
                {
                    contextId = context.contextId,
                    contextName = context.contextName,
                    creditScore = context.startingScore,
                    currentTier = currentTier,
                    recentEvents = new List<SocialCreditSystem.CreditEvent>()
                };
            }
        }
        
        public void UpdateSocialCredit(string contextId, float deltaValue, string description)
        {
            // Ensure the context exists
            if (!socialData.socialCredits.ContainsKey(contextId))
            {
                Debug.LogWarning($"Social context {contextId} not found.");
                return;
            }
            
            var context = socialData.socialCredits[contextId];
            float oldScore = context.creditScore;
            
            // Update credit score
            context.creditScore += deltaValue;
            
            // Create event record
            var creditEvent = new SocialCreditSystem.CreditEvent
            {
                eventId = $"credit_{DateTime.Now.Ticks}",
                description = description,
                impact = deltaValue,
                timestamp = Time.time,
                decayTime = 72f // Default 72 hours decay time
            };
            
            context.recentEvents.Add(creditEvent);
            
            // Limit recent events list size
            if (context.recentEvents.Count > 20)
            {
                context.recentEvents.RemoveAt(0);
            }
            
            // Check for tier change
            var configContext = config.socialContexts.Find(c => c.contextId == contextId);
            if (configContext != null)
            {
                var newTier = FindTierForScore(configContext.tiers, context.creditScore);
                if (newTier.tierId != context.currentTier.tierId)
                {
                    context.currentTier = newTier;
                    // TODO: Handle tier change effects
                }
            }
            
            // Fire event
            OnSocialCreditChanged?.Invoke(contextId, context.creditScore, deltaValue);
        }
        
        public void DecayCreditEvents(float deltaTime)
        {
            foreach (var contextPair in socialData.socialCredits)
            {
                var context = contextPair.Value;
                
                for (int i = context.recentEvents.Count - 1; i >= 0; i--)
                {
                    var eventData = context.recentEvents[i];
                    eventData.decayTime -= deltaTime;
                    
                    // Remove expired events
                    if (eventData.decayTime <= 0)
                    {
                        context.recentEvents.RemoveAt(i);
                    }
                }
            }
        }
        
        private SocialCreditSystem.SocialTier FindTierForScore(List<SocialCreditSystem.SocialTier> tiers, float score)
        {
            foreach (var tier in tiers)
            {
                if (score >= tier.minThreshold && score <= tier.maxThreshold)
                {
                    return tier;
                }
            }
            
            // Default to first tier if no match found
            return tiers.Count > 0 ? tiers[0] : new SocialCreditSystem.SocialTier
            {
                tierId = "default",
                tierName = "Default",
                minThreshold = 0,
                maxThreshold = 100,
                benefits = new List<string>(),
                restrictions = new List<string>()
            };
        }
        
        public bool CanPerformAction(string actionId)
        {
            // Check social tier restrictions for all contexts
            foreach (var contextPair in socialData.socialCredits)
            {
                var context = contextPair.Value;
                
                // Check if this action is restricted by current tier
                if (context.currentTier.restrictions.Contains(actionId))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public Dictionary<string, float> GetAllCreditScores()
        {
            Dictionary<string, float> scores = new Dictionary<string, float>();
            
            foreach (var pair in socialData.socialCredits)
            {
                scores[pair.Key] = pair.Value.creditScore;
            }
            
            return scores;
        }
        
        public SocialCreditSaveData GenerateSaveData()
        {
            var saveData = new SocialCreditSaveData
            {
                playerId = socialData.playerId,
                creditEntries = new List<SocialCreditSaveData.SocialCreditEntry>()
            };
            
            foreach (var pair in socialData.socialCredits)
            {
                saveData.creditEntries.Add(new SocialCreditSaveData.SocialCreditEntry
                {
                    contextId = pair.Value.contextId,
                    contextName = pair.Value.contextName,
                    creditScore = pair.Value.creditScore,
                    currentTier = pair.Value.currentTier,
                    recentEvents = new List<SocialCreditSystem.CreditEvent>(pair.Value.recentEvents)
                });
            }
            
            return saveData;
        }
        
        public void RestoreFromSaveData(SocialCreditSaveData saveData)
        {
            socialData.playerId = saveData.playerId;
            socialData.socialCredits.Clear();
            
            foreach (var entry in saveData.creditEntries)
            {
                socialData.socialCredits[entry.contextId] = new SocialCreditSystem.SocialCredit
                {
                    contextId = entry.contextId,
                    contextName = entry.contextName,
                    creditScore = entry.creditScore,
                    currentTier = entry.currentTier,
                    recentEvents = new List<SocialCreditSystem.CreditEvent>(entry.recentEvents)
                };
            }
        }
    }
}