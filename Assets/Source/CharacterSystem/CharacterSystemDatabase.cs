using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Database for character system definitions
    /// </summary>
    public class CharacterSystemDatabase : MonoBehaviour
    {
        private static CharacterSystemDatabase _instance;
        public static CharacterSystemDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CharacterSystemDatabase>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CharacterSystemDatabase");
                        _instance = go.AddComponent<CharacterSystemDatabase>();
                    }
                }
                return _instance;
            }
        }
        
        // Lists of all available definitions
        [SerializeField] private List<DesireTypeDefinition> desireTypes = new List<DesireTypeDefinition>();
        [SerializeField] private List<EmotionTypeDefinition> emotionTypes = new List<EmotionTypeDefinition>();
        [SerializeField] private List<PersonalityTypeDefinition> personalityTypes = new List<PersonalityTypeDefinition>();
        [SerializeField] private List<CharacterTemplateDefinition> characterTemplates = new List<CharacterTemplateDefinition>();
        
        // Lists of all available action, event and situation definitions
        [SerializeField] private List<ActionDefinition> actions = new List<ActionDefinition>();
        [SerializeField] private List<EventDefinition> events = new List<EventDefinition>();
        [SerializeField] private List<SituationDefinition> situations = new List<SituationDefinition>();
        [SerializeField] private List<DecisionDefinition> decisions = new List<DecisionDefinition>();
        
        // Dictionaries for fast lookup
        private Dictionary<string, DesireTypeDefinition> desireTypesDict = new Dictionary<string, DesireTypeDefinition>();
        private Dictionary<string, EmotionTypeDefinition> emotionTypesDict = new Dictionary<string, EmotionTypeDefinition>();
        private Dictionary<string, PersonalityTypeDefinition> personalityTypesDict = new Dictionary<string, PersonalityTypeDefinition>();
        private Dictionary<string, CharacterTemplateDefinition> characterTemplatesDict = new Dictionary<string, CharacterTemplateDefinition>();
        private Dictionary<string, ActionDefinition> actionsDict = new Dictionary<string, ActionDefinition>();
        private Dictionary<string, EventDefinition> eventsDict = new Dictionary<string, EventDefinition>();
        private Dictionary<string, SituationDefinition> situationsDict = new Dictionary<string, SituationDefinition>();
        private Dictionary<string, DecisionDefinition> decisionsDict = new Dictionary<string, DecisionDefinition>();
        
        private void Awake()
        {
            InitializeDictionaries();
        }
        
        private void InitializeDictionaries()
        {
            // Initialize the dictionaries for fast lookup
            foreach (var desireType in desireTypes) desireTypesDict[desireType.id] = desireType;
            foreach (var emotionType in emotionTypes) emotionTypesDict[emotionType.id] = emotionType;
            foreach (var personalityType in personalityTypes) personalityTypesDict[personalityType.id] = personalityType;
            foreach (var characterTemplate in characterTemplates) characterTemplatesDict[characterTemplate.id] = characterTemplate;
            foreach (var action in actions) actionsDict[action.actionId] = action;
            foreach (var eventDef in events) eventsDict[eventDef.eventId] = eventDef;
            foreach (var situation in situations) situationsDict[situation.situationId] = situation;
            foreach (var decision in decisions) decisionsDict[decision.decisionId] = decision;
        }
        
        // Methods to get definitions by ID
        public DesireTypeDefinition GetDesireType(string id) => 
            desireTypesDict.TryGetValue(id, out var def) ? def : null;
            
        public EmotionTypeDefinition GetEmotionType(string id) => 
            emotionTypesDict.TryGetValue(id, out var def) ? def : null;
            
        public PersonalityTypeDefinition GetPersonalityType(string id) => 
            personalityTypesDict.TryGetValue(id, out var def) ? def : null;
            
        public CharacterTemplateDefinition GetCharacterTemplate(string id) => 
            characterTemplatesDict.TryGetValue(id, out var def) ? def : null;
            
        public ActionDefinition GetAction(string id) => 
            actionsDict.TryGetValue(id, out var def) ? def : null;
            
        public EventDefinition GetEvent(string id) => 
            eventsDict.TryGetValue(id, out var def) ? def : null;
            
        public SituationDefinition GetSituation(string id) => 
            situationsDict.TryGetValue(id, out var def) ? def : null;
            
        public DecisionDefinition GetDecision(string id) => 
            decisionsDict.TryGetValue(id, out var def) ? def : null;
            
        // Methods to get all definitions
        public List<DesireTypeDefinition> GetAllDesireTypes() => desireTypes;
        public List<EmotionTypeDefinition> GetAllEmotionTypes() => emotionTypes;
        public List<PersonalityTypeDefinition> GetAllPersonalityTypes() => personalityTypes;
        public List<CharacterTemplateDefinition> GetAllCharacterTemplates() => characterTemplates;
        public List<ActionDefinition> GetAllActions() => actions;
        public List<EventDefinition> GetAllEvents() => events;
        public List<SituationDefinition> GetAllSituations() => situations;
        public List<DecisionDefinition> GetAllDecisions() => decisions;
        
        // Methods to add new definitions at runtime
        public void RegisterDesireType(DesireTypeDefinition desireType)
        {
            if (!desireTypesDict.ContainsKey(desireType.id))
            {
                desireTypes.Add(desireType);
                desireTypesDict[desireType.id] = desireType;
            }
        }
        
        public void RegisterEmotionType(EmotionTypeDefinition emotionType)
        {
            if (!emotionTypesDict.ContainsKey(emotionType.id))
            {
                emotionTypes.Add(emotionType);
                emotionTypesDict[emotionType.id] = emotionType;
            }
        }
        
        public void RegisterPersonalityType(PersonalityTypeDefinition personalityType)
        {
            if (!personalityTypesDict.ContainsKey(personalityType.id))
            {
                personalityTypes.Add(personalityType);
                personalityTypesDict[personalityType.id] = personalityType;
            }
        }
        
        public void RegisterCharacterTemplate(CharacterTemplateDefinition characterTemplate)
        {
            if (!characterTemplatesDict.ContainsKey(characterTemplate.id))
            {
                characterTemplates.Add(characterTemplate);
                characterTemplatesDict[characterTemplate.id] = characterTemplate;
            }
        }
        
        public void RegisterAction(ActionDefinition action)
        {
            if (!actionsDict.ContainsKey(action.actionId))
            {
                actions.Add(action);
                actionsDict[action.actionId] = action;
            }
        }
        
        public void RegisterEvent(EventDefinition eventDef)
        {
            if (!eventsDict.ContainsKey(eventDef.eventId))
            {
                events.Add(eventDef);
                eventsDict[eventDef.eventId] = eventDef;
            }
        }
        
        public void RegisterSituation(SituationDefinition situation)
        {
            if (!situationsDict.ContainsKey(situation.situationId))
            {
                situations.Add(situation);
                situationsDict[situation.situationId] = situation;
            }
        }
        
        public void RegisterDecision(DecisionDefinition decision)
        {
            if (!decisionsDict.ContainsKey(decision.decisionId))
            {
                decisions.Add(decision);
                decisionsDict[decision.decisionId] = decision;
            }
        }
    }
}