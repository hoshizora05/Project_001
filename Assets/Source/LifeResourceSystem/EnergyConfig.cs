using System;
using System.Collections.Generic;
using UnityEngine;

namespace LifeResourceSystem
{
    [System.Serializable]
    public class EnergyConfig
    {
        public float baseMaxEnergy = 100f;
        public float baseRecoveryRate = 2f; // Per hour
        public Dictionary<string, float> defaultEnergyCosts = new Dictionary<string, float>();
        public List<EnergySystem.EnergyState> defaultStates = new List<EnergySystem.EnergyState>();
        public string defaultStateId = "normal";
        public List<CustomEnergyStateConfig> customStates = new List<CustomEnergyStateConfig>();
    }
}