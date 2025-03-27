using System.Collections.Generic;
using UnityEngine;

namespace PlayerProgression.Data
{
    [System.Serializable]
    public class PlayerStats
    {
        public string playerId;
        public Dictionary<string, Stat> stats = new Dictionary<string, Stat>();
        
        [System.Serializable]
        public class Stat
        {
            public float baseValue;
            public float currentValue;
            public float minValue;
            public float maxValue;
            public float growthRate;
            public List<StatModifier> modifiers = new List<StatModifier>();

            public Stat(float baseVal, float min, float max, float growth)
            {
                baseValue = baseVal;
                currentValue = baseVal;
                minValue = min;
                maxValue = max;
                growthRate = growth;
            }
        }
        
        [System.Serializable]
        public class StatModifier
        {
            public string source;
            public float value;
            public ModifierType type;
            public float duration;
            public float remainingTime;

            public StatModifier(string src, float val, ModifierType modType, float dur)
            {
                source = src;
                value = val;
                type = modType;
                duration = dur;
                remainingTime = dur;
            }
        }
        
        public enum ModifierType
        {
            Additive,
            Multiplicative,
            Override
        }
    }
}