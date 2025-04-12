using System.Collections.Generic;
using UnityEngine;

namespace PlayerProgression.Data
{
    [System.Serializable]
    public class SocialStandingSystem
    {
        public string playerId;
        public Dictionary<string, Reputation> reputations = new Dictionary<string, Reputation>();
        
        [System.Serializable]
        public class Reputation
        {
            public string contextId;
            public string contextName;
            public float overallScore;
            public Dictionary<string, float> traitScores = new Dictionary<string, float>();
            public List<ReputationEvent> recentEvents = new List<ReputationEvent>();

            public Reputation(string id, string name)
            {
                contextId = id;
                contextName = name;
                overallScore = 0;
            }
        }
        
        [System.Serializable]
        public class ReputationEvent
        {
            public string eventId;
            public string description;
            public Dictionary<string, float> impacts = new Dictionary<string, float>();
            public float timestamp;
            public float decayRate;

            public ReputationEvent(string id, string desc, float time, float decay)
            {
                eventId = id;
                description = desc;
                timestamp = time;
                decayRate = decay;
            }
        }
        
        [System.Serializable]
        public class SocialLabel
        {
            public string labelId;
            public string labelName;
            public Dictionary<string, float> thresholds = new Dictionary<string, float>();
            public List<string> effects = new List<string>();
            public bool isActive;

            public SocialLabel(string id, string name)
            {
                labelId = id;
                labelName = name;
                isActive = false;
            }
        }
    }
}