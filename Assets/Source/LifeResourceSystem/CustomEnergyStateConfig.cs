using System;
using System.Collections.Generic;
using UnityEngine;

namespace LifeResourceSystem 
{
    [System.Serializable]
    public class CustomEnergyStateConfig
    {
        public string stateId;
        public string stateName;
        public List<string> restrictedActions = new List<string>();
        public List<string> allowedActions = new List<string>();
        public float energyCostMultiplier = 1f;
        public float recoveryRateMultiplier = 1f;
    }
}