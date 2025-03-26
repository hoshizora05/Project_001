using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Main public API for interacting with character psychology system
    /// Provides a clean interface that hides implementation details
    /// </summary>
    public class CharacterInteractionAPI : MonoBehaviour
    {
        private static CharacterInteractionAPI _instance;
        public static CharacterInteractionAPI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CharacterInteractionAPI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CharacterInteractionAPI");
                        _instance = go.AddComponent<CharacterInteractionAPI>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        [SerializeField] private CharacterPsychologySystem psychologySystem;
        
        private void Awake()
        {
            if (psychologySystem == null)
            {
                psychologySystem = FindFirstObjectByType<CharacterPsychologySystem>();
                
                if (psychologySystem == null)
                {
                    Debug.LogError("CharacterInteractionAPI requires a CharacterPsychologySystem in the scene!");
                }
            }
        }
        
        /// <summary>
        /// Get the psychology system implementation
        /// </summary>
        /// <returns>IPsychologySystem interface</returns>
        public IPsychologySystem GetPsychologySystem()
        {
            return psychologySystem;
        }
        
        #region API Methods
        
        /// <summary>
        /// Calculate how an action affects a character's desires
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="actionId">Action ID</param>
        /// <param name="contextParameters">Optional context parameters that modify the effect</param>
        /// <returns>Dictionary of desire types and their change values</returns>
        public Dictionary<string, float> CalculateDesireEffect(
            string characterId, 
            string actionId, 
            Dictionary<string, object> contextParameters = null)
        {
            // Simply delegate to the psychology system
            return psychologySystem.CalculateDesireEffect(characterId, actionId, contextParameters);
        }
        
        /// <summary>
        /// Update a character's mental state based on triggering factors
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="triggeringFactors">Factors that trigger the update</param>
        public void UpdateMentalState(
            string characterId, 
            Dictionary<string, object> triggeringFactors = null)
        {
            psychologySystem.UpdateMentalState(characterId, triggeringFactors);
        }
        
        /// <summary>
        /// Generate an emotional response to a situation
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="situationId">Situation ID</param>
        /// <param name="intensityLevel">Base intensity level (0-1)</param>
        /// <returns>Emotional response object</returns>
        public EmotionalResponse GenerateEmotionalResponse(
            string characterId, 
            string situationId, 
            float intensityLevel = 0.5f)
        {
            return psychologySystem.GenerateEmotionalResponse(characterId, situationId, intensityLevel);
        }
        
        /// <summary>
        /// Evaluate and potentially resolve internal conflicts at a decision point
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="decisionPoint">Decision point ID</param>
        /// <returns>Conflict resolution object</returns>
        public ConflictResolution EvaluateInternalConflict(
            string characterId, 
            string decisionPoint)
        {
            return psychologySystem.EvaluateInternalConflict(characterId, decisionPoint);
        }
        
        /// <summary>
        /// Get a character's current state data
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="queryType">Type of data to query (desire, emotion, mood, conflict)</param>
        /// <param name="specificId">Specific item ID if querying a specific item</param>
        /// <param name="detailLevel">Detail level for the query</param>
        /// <returns>Read-only state data</returns>
        public ReadOnlyStateData GetState(
            string characterId,
            string queryType,
            string specificId = null,
            int detailLevel = 0)
        {
            var query = new StateQuery
            {
                queryType = queryType,
                specificId = specificId,
                detailLevel = detailLevel
            };
            
            return psychologySystem.QueryState(characterId, query);
        }
        
        /// <summary>
        /// Get a character's current mood
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="detailLevel">Detail level (0: basic mood, 1: with dominant emotion, 2: full breakdown)</param>
        /// <returns>Mood information object</returns>
        public MoodInfo GetCurrentMood(
            string characterId, 
            int detailLevel = 0)
        {
            var stateData = GetState(characterId, "mood", null, detailLevel);
            
            if (stateData == null)
                return null;
                
            var moodInfo = new MoodInfo
            {
                characterId = characterId,
                basicMood = stateData.values["currentMood"] as string,
                detailLevel = detailLevel
            };
            
            if (detailLevel >= 1 && stateData.values.ContainsKey("dominantEmotion"))
            {
                moodInfo.dominantEmotion = stateData.values["dominantEmotion"] as string;
                moodInfo.dominantEmotionStrength = (float)stateData.values["dominantEmotionStrength"];
            }
            
            if (detailLevel >= 2)
            {
                moodInfo.allEmotions = stateData.values["allEmotions"] as Dictionary<string, float>;
                moodInfo.stressLevel = (float)stateData.values["stressLevel"];
                moodInfo.activeMoodModifiers = stateData.values["activeMoodModifiers"] as List<string>;
            }
            
            return moodInfo;
        }
        
        /// <summary>
        /// Get the current value of a specific desire
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="desireType">Desire type</param>
        /// <returns>Desire information object</returns>
        public DesireInfo GetDesireLevel(
            string characterId, 
            string desireType)
        {
            var stateData = GetState(characterId, "desire", desireType);
            
            if (stateData == null)
                return null;
                
            var desireInfo = new DesireInfo
            {
                characterId = characterId,
                desireType = desireType,
                currentValue = (float)stateData.values["currentValue"] / 100f,
                isDominant = (bool)stateData.values["isDominant"],
                status = stateData.values["status"] as string
            };
            
            return desireInfo;
        }
        
        /// <summary>
        /// Get all desire levels for a character
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <returns>Dictionary of desire types and info objects</returns>
        public Dictionary<string, DesireInfo> GetAllDesireLevels(string characterId)
        {
            var result = new Dictionary<string, DesireInfo>();
            var stateData = GetState(characterId, "desire");
            
            if (stateData == null)
                return result;
                
            if (stateData.values.TryGetValue("desires", out object desiresObj) && 
                desiresObj is Dictionary<string, object> desireDict)
            {
                foreach (var kvp in desireDict)
                {
                    if (kvp.Value is Dictionary<string, object> desireData)
                    {
                        var desireInfo = new DesireInfo
                        {
                            characterId = characterId,
                            desireType = kvp.Key,
                            currentValue = (float)desireData["currentValue"] / 100f,
                            isDominant = (bool)desireData["isDominant"],
                            status = desireData["status"] as string
                        };
                        
                        result[kvp.Key] = desireInfo;
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Trigger an action event (published to the event bus)
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="actionId">Action ID</param>
        /// <param name="parameters">Optional parameters</param>
        public void TriggerAction(
            string characterId,
            string actionId,
            Dictionary<string, object> parameters = null)
        {
            // Create the action event
            var actionEvent = new PsychologyEvent
            {
                characterId = characterId,
                eventType = "action_performed",
                parameters = parameters ?? new Dictionary<string, object>()
            };
            
            // Add action ID to parameters
            actionEvent.parameters["actionId"] = actionId;
            
            // Publish to the event bus
            CharacterEventBus.Instance.Publish(actionEvent);
        }
        
        /// <summary>
        /// Trigger a situation event (published to the event bus)
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="situationId">Situation ID</param>
        /// <param name="intensity">Intensity level</param>
        /// <param name="parameters">Optional additional parameters</param>
        public void TriggerSituation(
            string characterId,
            string situationId,
            float intensity = 0.5f,
            Dictionary<string, object> parameters = null)
        {
            // Create the situation event
            var situationEvent = new PsychologyEvent
            {
                characterId = characterId,
                eventType = "situation_encountered",
                parameters = parameters ?? new Dictionary<string, object>()
            };
            
            // Add situation ID and intensity to parameters
            situationEvent.parameters["situationId"] = situationId;
            situationEvent.parameters["intensity"] = intensity;
            
            // Publish to the event bus
            CharacterEventBus.Instance.Publish(situationEvent);
        }
        
        /// <summary>
        /// Trigger a decision event (published to the event bus)
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="decisionPoint">Decision point ID</param>
        /// <param name="parameters">Optional additional parameters</param>
        public void TriggerDecision(
            string characterId,
            string decisionPoint,
            Dictionary<string, object> parameters = null)
        {
            // Create the decision event
            var decisionEvent = new PsychologyEvent
            {
                characterId = characterId,
                eventType = "decision_required",
                parameters = parameters ?? new Dictionary<string, object>()
            };
            
            // Add decision point to parameters
            decisionEvent.parameters["decisionPoint"] = decisionPoint;
            
            // Publish to the event bus
            CharacterEventBus.Instance.Publish(decisionEvent);
        }
        
        /// <summary>
        /// Create a new interaction event and process its effects immediately
        /// </summary>
        /// <param name="characterId">Character ID</param>
        /// <param name="actionId">Action ID</param>
        /// <param name="parameters">Optional parameters</param>
        /// <returns>Interaction result</returns>
        public InteractionResult ProcessInteraction(
            string characterId, 
            string actionId, 
            Dictionary<string, object> parameters = null)
        {
            parameters = parameters ?? new Dictionary<string, object>();
            
            // Create interaction event parameters
            var eventParams = new Dictionary<string, object>(parameters);
            eventParams["actionId"] = actionId;
            
            // Create the event
            var interactionEvent = new PsychologyEvent
            {
                characterId = characterId,
                eventType = "action_performed",
                parameters = eventParams
            };
            
            // Process the event
            EmotionalResponse response = psychologySystem.ProcessEvent(interactionEvent);
            
            // Get the current state
            var moodInfo = GetCurrentMood(characterId, 2); // Full detail level
            
            // Get the desires that were affected
            Dictionary<string, float> desireEffects = new Dictionary<string, float>();
            if (eventParams.ContainsKey("desireEffects") && eventParams["desireEffects"] is Dictionary<string, float> effectDict)
            {
                desireEffects = effectDict;
            }
            
            // Create conflict resolution if a decision point was specified
            ConflictResolution conflictResolution = null;
            if (parameters.TryGetValue("decisionPoint", out object decisionObj) && decisionObj is string decisionPoint)
            {
                conflictResolution = EvaluateInternalConflict(characterId, decisionPoint);
            }
            
            // Create and return interaction result
            return new InteractionResult
            {
                characterId = characterId,
                actionId = actionId,
                desireEffects = desireEffects,
                emotionalResponse = response,
                conflictResolution = conflictResolution,
                currentMood = moodInfo
            };
        }
        #endregion
    }
    
    #region API Result Classes
    
    /// <summary>
    /// Information about a character's mood
    /// </summary>
    [System.Serializable]
    public class MoodInfo
    {
        public string characterId;
        public string basicMood;
        public int detailLevel;
        
        // Detail level 1+
        public string dominantEmotion;
        public float dominantEmotionStrength;
        
        // Detail level 2+
        public Dictionary<string, float> allEmotions;
        public float stressLevel;
        public List<string> activeMoodModifiers;
    }
    
    /// <summary>
    /// Information about a character's desire
    /// </summary>
    [System.Serializable]
    public class DesireInfo
    {
        public string characterId;
        public string desireType;
        public float currentValue;  // 0-1 scale
        public bool isDominant;
        public string status;       // "unsatisfied", "neutral", "satisfied"
    }
    
    /// <summary>
    /// Result of a character interaction
    /// </summary>
    [System.Serializable]
    public class InteractionResult
    {
        public string characterId;
        public string actionId;
        public Dictionary<string, float> desireEffects;
        public EmotionalResponse emotionalResponse;
        public ConflictResolution conflictResolution;
        public MoodInfo currentMood;
    }
    #endregion
}