using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CharacterSystem
{
    #region Interfaces

    public interface ICharacter
    {
        string CharacterId { get; }
        string CharacterName { get; }
        Dictionary<SkillType, float> CharacterSkills { get; }
        Dictionary<AttributeType, float> CharacterAttributes { get; }
        MentalState.EmotionalState CharacterCurrentEmotionalState { get; }
        Dictionary<PreferenceType, float> CharacterPreferences { get; }
        LocationType CharacterCurrentLocation { get; }
        Schedule CharacterDailySchedule { get; }
    }

    public interface IDialogueSystem
    {
        void InitiateDialogue(ICharacter player, ICharacter npc);
        void InitiateDialogue(ICharacter player, ICharacter npc, string conversationId);
        void InitiateDialogue(ICharacter player, ICharacter npc, string conversationId, int forcedLineIndex);
        DialogueOptions GetDialogueOptions(ICharacter player, ICharacter npc, DialogueContext context);
        DialogueOptions GetDialogueOptions(ICharacter player, ICharacter npc, DialogueContext context, int forcedLineIndex);
        void ProcessDialogueChoice(DialogueChoice choice, ICharacter player, ICharacter npc);
        void EndDialogue(ICharacter player, ICharacter npc);
    }

    public interface IMessageSystem
    {
        bool CanSendMessage(ICharacter recipient, MessageType type);
        void SendMessage(ICharacter sender, ICharacter recipient, MessageContent content);
        MessageResponse GetResponse(ICharacter sender, ICharacter recipient, MessageContent content);
        List<MessageThread> GetActiveThreads(ICharacter character);
    }

    public interface IGiftSystem
    {
        List<GiftItem> GetAvailableGifts(ICharacter player);
        float CalculateGiftEffect(GiftItem gift, ICharacter recipient);
        void ProcessGiftGiving(ICharacter player, ICharacter recipient, GiftItem gift);
        List<GiftPreference> GetRevealedPreferences(ICharacter character);
    }

    #endregion

    #region Enums

    public enum LocationType
    {
        Home,
        Work,
        School,
        Restaurant,
        Park,
        Mall,
        Entertainment,
        Other
    }

    public enum TimeOfDay
    {
        Morning,
        Afternoon,
        Evening,
        Night
    }

    public enum DialogueTopic
    {
        Greeting,
        Personal,
        Work,
        Hobbies,
        Relationships,
        Weather,
        News,
        Special,
        Farewell
    }

    public enum MessageType
    {
        Text,
        Invitation,
        Gift,
        Special
    }

    public enum GiftCategory
    {
        DailyNecessity,
        HobbyRelated,
        Luxury,
        Special
    }

    public enum PreferenceType
    {
        Food,
        Drink,
        Color,
        Music,
        Art,
        Hobby,
        Fashion,
        Literature
    }

    public enum SkillType
    {
        Conversation,
        Persuasion,
        Charm,
        Empathy,
        Humor,
        Negotiation
    }

    public enum AttributeType
    {
        Intelligence,
        Charisma,
        Kindness,
        Confidence,
        Attractiveness
    }

    public enum RelationshipParameter
    {
        Friendship,
        Romance,
        Trust,
        Respect,
        Comfort,
        Familiarity
    }

    public enum EmotionalStateType
    {
        Happy,
        Sad,
        Angry,
        Excited,
        Nervous,
        Bored,
        Relaxed,
        Stressed,
        Neutral
    }

    #endregion

    #region Data Structures

    [Serializable]
    public struct DialogueContext
    {
        public LocationType Location;
        public TimeOfDay TimeOfDay;
        public DialogueTopic PreviousTopic;
        public MentalState.EmotionalState NpcCurrentEmotion;
    }

    [Serializable]
    public class DialogueOptions
    {
        public List<DialogueChoice> Choices;
        public string NpcDialogueText;
        public MentalState.EmotionalState NpcEmotion;
    }

    [Serializable]
    public class DialogueChoice
    {
        public string ChoiceText;
        public DialogueTopic Topic;
        public Dictionary<RelationshipParameter, float> PotentialEffects;
        public List<RequirementCondition> Requirements;
        public float SuccessRate;
        public string NextConversationId;
    }

    [Serializable]
    public class RequirementCondition
    {
        public RequirementType Type;
        public string Parameter;
        public float MinValue;
    }

    [Serializable]
    public enum RequirementType
    {
        Skill,
        Attribute,
        RelationshipLevel,
        TimeOfDay,
        Location,
        Event
    }

    [Serializable]
    public class MessageContent
    {
        public string Text;
        public MessageType Type;
        public DateTime SendTime;
        public List<GiftItem> AttachedGifts;
        public EventInvitation Invitation;
    }

    [Serializable]
    public class MessageResponse
    {
        public bool WillRespond;
        public float ResponseDelay;
        public string ResponseText;
        public MentalState.EmotionalState ResponseEmotion;
        public bool AcceptedInvitation;
    }

    [Serializable]
    public class MessageThread
    {
        public string ThreadId;
        public ICharacter Participant;
        public List<MessageContent> Messages;
        public DateTime LastActivity;
        public bool HasUnread;
    }

    [Serializable]
    public class EventInvitation
    {
        public string EventName;
        public LocationType Location;
        public DateTime EventTime;
        public string Description;
        public bool RequiresResponse;
    }

    [Serializable]
    public class GiftItem
    {
        public string Name;
        public string Description;
        public GiftCategory Category;
        public int Cost;
        public int BaseEffectValue;
        public Dictionary<PreferenceType, float> PreferenceMultipliers;
    }

    [Serializable]
    public class GiftPreference
    {
        public PreferenceType Type;
        public string ItemName;
        public float PreferenceLevel;
        public bool IsRevealed;
    }

    [Serializable]
    public class Schedule
    {
        public Dictionary<TimeOfDay, LocationType> DailyLocations;
        public Dictionary<DayOfWeek, Dictionary<TimeOfDay, LocationType>> SpecialSchedules;
    }

    #endregion

    #region Implementation

    public class DialogueSystem : MonoBehaviour, IDialogueSystem
    {
        // [SerializeField] references to your existing parameters remain the same...
        [SerializeField] private float _skillModifierWeight = 0.3f;
        [SerializeField] private float _affinityModifierWeight = 0.4f;
        [SerializeField] private float _situationModifierWeight = 0.3f;
        [SerializeField] private float _criticalSuccessThreshold = 0.9f;
        [SerializeField] private float _criticalFailureThreshold = 0.1f;

        // NEW: Reference your external database
        [Header("External Dialogue Data")]
        [SerializeField] private DialogueDatabase dialogueDatabase;

        private RelationshipNetwork _relationshipNetwork;
        private CharacterMemoryManager _memoryManager;
        private Dictionary<string, DialogueContext> _activeDialogues = new Dictionary<string, DialogueContext>();

        // We'll store the chosen conversationId in a separate dictionary. 
        // This is purely to demonstrate storing “which conversation is active”
        // for each (player, npc) pair. 
        private Dictionary<string, string> _conversationIDs = new Dictionary<string, string>();
        // NEW: Store forced line index if specified
        private Dictionary<string, int> _forcedLineIndexByDialogue = new Dictionary<string, int>();


        private void Awake()
        {
            _relationshipNetwork = GetComponent<RelationshipNetwork>();
            _memoryManager = GetComponent<CharacterMemoryManager>();
        }

        // -----------------------------------------------------------
        // 1) Original StartConversation, which checks IsDialogueAvailable
        //    and sets up a default context. We'll keep it as is.

        public void InitiateDialogue(ICharacter player, ICharacter npc)
        {
            if (!IsDialogueAvailable(player, npc))
            {
                Debug.LogWarning($"Dialogue not available between {player.CharacterName} and {npc.CharacterName}");
                return;
            }
            string dialogueId = GetDialogueId(player, npc);

            DialogueContext context = new DialogueContext
            {
                Location = npc.CharacterCurrentLocation,
                TimeOfDay = GetCurrentTimeOfDay(),
                PreviousTopic = DialogueTopic.Greeting,
                NpcCurrentEmotion = npc.CharacterCurrentEmotionalState
            };
            _activeDialogues[dialogueId] = context;

            // Clear old forced index if any
            if (_forcedLineIndexByDialogue.ContainsKey(dialogueId))
            {
                _forcedLineIndexByDialogue.Remove(dialogueId);
            }

            InteractionSystem.EventManager.TriggerEvent("OnDialogueStarted", new Dictionary<string, object>
            {
                { "Player", player },
                { "NPC", npc },
                { "Context", context }
            });
        }
        // -----------------------------------------------------------
        // 2) NEW Overload: Start a conversation by specifying conversationId.
        //    You can bypass “IsDialogueAvailable” if your design wants 
        //    the conversation to start no matter what. Or keep it in.

        public void InitiateDialogue(ICharacter player, ICharacter npc, string conversationId)
        {
            // Optional: skip or keep location checks if you prefer
            if (!IsDialogueAvailable(player, npc))
            {
                Debug.LogWarning($"Dialogue not available between {player.CharacterName} and {npc.CharacterName}");
                return;
            }

            // We store a special context that holds onto the chosen conversationId.
            // We'll later interpret that in GetDialogueOptions if we want to handle 
            // it differently from topic-based dialogue.
            string dialogueId = GetDialogueId(player, npc);

            // We can store conversationId in the 'PreviousTopic' or create a custom field.
            // For cleanliness, let's store it in a custom field inside DialogueContext.
            DialogueContext context = new DialogueContext
            {
                Location = npc.CharacterCurrentLocation,
                TimeOfDay = GetCurrentTimeOfDay(),
                PreviousTopic = DialogueTopic.Special,  // or any default
                NpcCurrentEmotion = npc.CharacterCurrentEmotionalState
            };

            // If you want to store the conversationId, we can do so in a 
            // custom extension of DialogueContext or an external mapping.
            // For example, let's store it in the dictionary:
            _activeDialogues[dialogueId] = context;

            // Then store it in some parallel dictionary or a new field:
            // e.g. _conversationIDs[dialogueId] = conversationId;
            if (!_conversationIDs.ContainsKey(dialogueId))
            {
                _conversationIDs[dialogueId] = conversationId;
            }

            // Clear old forced index if any
            if (_forcedLineIndexByDialogue.ContainsKey(dialogueId))
            {
                _forcedLineIndexByDialogue.Remove(dialogueId);
            }

            InteractionSystem.EventManager.TriggerEvent("OnDialogueStarted", new Dictionary<string, object>
            {
                { "Player", player },
                { "NPC", npc },
                { "ConversationId", conversationId }
            });
        }
        // ----------------------------------------
        // NEW Overload: specify conversationId + forced line index
        public void InitiateDialogue(ICharacter player, ICharacter npc, string conversationId, int forcedLineIndex)
        {
            if (!IsDialogueAvailable(player, npc))
            {
                Debug.LogWarning($"Dialogue not available between {player.CharacterName} and {npc.CharacterName}");
                return;
            }

            string dialogueId = GetDialogueId(player, npc);

            DialogueContext context = new DialogueContext
            {
                Location = npc.CharacterCurrentLocation,
                TimeOfDay = GetCurrentTimeOfDay(),
                PreviousTopic = DialogueTopic.Special,
                NpcCurrentEmotion = npc.CharacterCurrentEmotionalState
            };
            _activeDialogues[dialogueId] = context;

            // Store the conversationId
            _conversationIDs[dialogueId] = conversationId;

            // Store the forced line index
            _forcedLineIndexByDialogue[dialogueId] = forcedLineIndex;

            InteractionSystem.EventManager.TriggerEvent("OnDialogueStarted", new Dictionary<string, object>
            {
                { "Player", player },
                { "NPC", npc },
                { "ConversationId", conversationId },
                { "ForcedLineIndex", forcedLineIndex }
            });
        }

        // -----------------------------------------------------------
        // 3) Modify GetDialogueOptions to handle conversationId selection

        public DialogueOptions GetDialogueOptions(ICharacter player, ICharacter npc, DialogueContext context)
        {
            string dialogueId = GetDialogueId(player, npc);
            if (_conversationIDs.ContainsKey(dialogueId))
            {
                // We are in a conversation chosen by ID
                string conversationId = _conversationIDs[dialogueId];
                return GetDialogueOptionsByConversationId(player, npc, context, conversationId);
            }
            else
            {
                // We are using your original topic-based approach 
                return GetDialogueOptionsByTopic(player, npc, context);
            }
        }
        public DialogueOptions GetDialogueOptions(ICharacter player, ICharacter npc, DialogueContext context, int forcedLineIndex)
        {
            string dialogueId = GetDialogueId(player, npc);

            // 一時的にforcedLineIndexを保存
            _forcedLineIndexByDialogue[dialogueId] = forcedLineIndex;

            DialogueOptions options;

            if (_conversationIDs.ContainsKey(dialogueId))
            {
                string conversationId = _conversationIDs[dialogueId];
                options = GetDialogueOptionsByConversationId(player, npc, context, conversationId);
            }
            else
            {
                options = GetDialogueOptionsByTopic(player, npc, context);
            }

            // 使用後は消去（明示的指定が1回限りなのか、継続するかは用途次第）
            _forcedLineIndexByDialogue.Remove(dialogueId);

            return options;
        }

        // Helper: original (topic-based) approach
        private DialogueOptions GetDialogueOptionsByTopic(ICharacter player, ICharacter npc, DialogueContext context)
        {
            // 1) Retrieve the NPC's line from the database, based on context.PreviousTopic
            string npcResponse = GenerateNpcDialogue(player, npc, context);

            // 2) Retrieve all possible DialogueChoices for that topic
            List<DialogueChoice> availableChoices = GenerateDialogueChoices(player, npc, context);

            return new DialogueOptions
            {
                Choices = availableChoices,
                NpcDialogueText = npcResponse,
                NpcEmotion = context.NpcCurrentEmotion
            };
        }

        // NEW: conversationId-based approach
        private DialogueOptions GetDialogueOptionsByConversationId(
            ICharacter player, ICharacter npc, DialogueContext context, string conversationId)
        {
            if (dialogueDatabase == null)
            {
                Debug.LogError("DialogueSystem: No DialogueDatabase assigned.");
                return new DialogueOptions
                {
                    Choices = new List<DialogueChoice>(),
                    NpcDialogueText = "(Missing DialogueDatabase)",
                    NpcEmotion = context.NpcCurrentEmotion
                };
            }

            // Find all entries that match this conversationId
            List<DialogueEntry> entries = dialogueDatabase.GetEntriesByConversationId(conversationId);
            if (entries == null || entries.Count == 0)
            {
                // If you have no data, return fallback
                return new DialogueOptions
                {
                    Choices = new List<DialogueChoice>(),
                    NpcDialogueText = $"(No entries for conversation '{conversationId}')",
                    NpcEmotion = context.NpcCurrentEmotion
                };
            }

            // For demonstration:
            //  - We pick one DialogueEntry at random 
            //    or based on some logic you define.
            int idx = UnityEngine.Random.Range(0, entries.Count);
            DialogueEntry chosenEntry = entries[idx];

            // 1) NPC line
            string npcLine = "(No lines in this entry)";
            if (chosenEntry.npcLines != null && chosenEntry.npcLines.Count > 0)
            {
                // Check if there's a forced line index
                int forcedIndex = -1;
                string dialogueId = GetDialogueId(player, npc);
                if (_forcedLineIndexByDialogue.TryGetValue(dialogueId, out forcedIndex))
                {
                    // If forcedIndex is valid, use it
                    if (forcedIndex >= 0 && forcedIndex < chosenEntry.npcLines.Count)
                    {
                        npcLine = chosenEntry.npcLines[forcedIndex];
                    }
                    else
                    {
                        // Fallback: forced index out of range, pick random
                        int lineIdx = UnityEngine.Random.Range(0, chosenEntry.npcLines.Count);
                        npcLine = chosenEntry.npcLines[lineIdx];
                    }
                }
                else
                {
                    // No forced index: pick random
                    int lineIdx = UnityEngine.Random.Range(0, chosenEntry.npcLines.Count);
                    npcLine = chosenEntry.npcLines[lineIdx];
                }
            }

            // 2) Filter the entry’s choices by requirement
            List<DialogueChoice> availableChoices = new List<DialogueChoice>();
            foreach (DialogueChoice choice in chosenEntry.choices)
            {
                if (IsOptionAvailable(choice, player, npc, context))
                {
                    availableChoices.Add(choice);
                }
            }

            // You could also chain multiple entries if you have a multi-step conversation.
            // For now, we just pick 1 entry → show its line + choices.

            return new DialogueOptions
            {
                Choices = availableChoices,
                NpcDialogueText = npcLine,
                NpcEmotion = context.NpcCurrentEmotion
            };
        }

        public void ProcessDialogueChoice(DialogueChoice choice, ICharacter player, ICharacter npc)
        {
            string dialogueId = GetDialogueId(player, npc);
            
            if (!_activeDialogues.ContainsKey(dialogueId))
            {
                Debug.LogError($"No active dialogue found between {player.CharacterName} and {npc.CharacterName}");
                return;
            }
            
            DialogueContext context = _activeDialogues[dialogueId];
            
            // Calculate success
            float successRate = CalculateSuccessRate(choice, player, npc, context);
            bool isSuccess = UnityEngine.Random.value <= successRate;
            
            // Apply relationship effects
            if (isSuccess)
            {
                ApplyRelationshipEffects(choice, player, npc);
                
                // Check for critical success
                bool isCriticalSuccess = successRate >= _criticalSuccessThreshold;
                if (isCriticalSuccess)
                {
                    ApplyCriticalSuccessEffects(player, npc, choice);
                }
            }
            else
            {
                // Check for critical failure
                bool isCriticalFailure = successRate <= _criticalFailureThreshold;
                if (isCriticalFailure)
                {
                    ApplyCriticalFailureEffects(player, npc, choice);
                }
            }

            if (!string.IsNullOrEmpty(choice.NextConversationId))
            {
                _conversationIDs[dialogueId] = choice.NextConversationId;
            }
            else
            {
                _conversationIDs.Remove(dialogueId);
            }

            // Record in memory
            _memoryManager.RecordNegativeImpression(player.CharacterId, npc.CharacterId, choice.Topic.ToString());
            
            // Update context
            context.PreviousTopic = choice.Topic;
            _activeDialogues[dialogueId] = context;
            
            // Fire event
            InteractionSystem.EventManager.TriggerEvent("OnDialogueOptionSelected", new Dictionary<string, object>
            {
                { "Player", player },
                { "NPC", npc },
                { "Choice", choice },
                { "Success", isSuccess }
            });
        }

        // -----------------------------------------------------------
        // 5) EndDialogue – same as before, but also clear conversationId.

        public void EndDialogue(ICharacter player, ICharacter npc)
        {
            string dialogueId = GetDialogueId(player, npc);
            if (_activeDialogues.ContainsKey(dialogueId))
            {
                DialogueContext context = _activeDialogues[dialogueId];
                _activeDialogues.Remove(dialogueId);

                // Also remove the conversationId if it was stored
                if (_conversationIDs.ContainsKey(dialogueId))
                {
                    _conversationIDs.Remove(dialogueId);
                }

                InteractionSystem.EventManager.TriggerEvent("OnDialogueEnded", new Dictionary<string, object>
                {
                    { "Player", player },
                    { "NPC", npc },
                    { "FinalTopic", context.PreviousTopic }
                });
            }
        }

        // -----------------------------------------------------------------------------------------
        // NEW Implementations That Access External Data

        private List<DialogueChoice> GenerateDialogueChoices(ICharacter player, ICharacter npc, DialogueContext context)
        {
            // Instead of pulling from a local "GetAllDialogueOptions()", fetch from the database
            // The 'topic' for the next set of choices is the context's previous topic
            DialogueTopic topic = context.PreviousTopic;

            if (dialogueDatabase == null)
            {
                Debug.LogError("DialogueSystem: No DialogueDatabase assigned.");
                return new List<DialogueChoice>();
            }

            // Potentially, you might vary the topic further. 
            // For example, if the NPC's emotional state is angry, 
            // you might shift from “Greeting” to “News,” etc.  
            // We'll keep it simple: we fetch choices for the same topic.

            List<DialogueChoice> allPotentialChoices = dialogueDatabase.GetChoicesMatchingContext(topic, context, player, npc);
            List<DialogueChoice> availableOptions = new List<DialogueChoice>();

            // Filter by requirements
            foreach (var option in allPotentialChoices)
            {
                if (IsOptionAvailable(option, player, npc, context))
                {
                    availableOptions.Add(option);
                }
            }

            // Sort by relevance using your existing logic
            availableOptions.Sort((a, b) =>
                CalculateOptionRelevance(b, player, npc, context).CompareTo(
                    CalculateOptionRelevance(a, player, npc, context)));

            // Return top 4 (or fewer)
            int maxOptions = Mathf.Min(4, availableOptions.Count);
            return availableOptions.GetRange(0, maxOptions);
        }

        private string GenerateNpcDialogue(ICharacter player, ICharacter npc, DialogueContext context)
        {
            if (dialogueDatabase == null)
            {
                Debug.LogError("DialogueSystem: No DialogueDatabase assigned.");
                return "(No DialogueDatabase)";
            }

            // The NPC line is tied to the context.PreviousTopic
            DialogueTopic topic = context.PreviousTopic;
            return dialogueDatabase.GetNpcLine(topic, context, player, npc);
        }

        // -----------------------------------------------------------------------------------------
        // The remainder of the code stays the same as your original:
        // - IsDialogueAvailable, 
        // - IsOptionAvailable, 
        // - MeetsRequirement, 
        // - CalculateOptionRelevance, 
        // - CalculateSuccessRate, 
        // - ApplyRelationshipEffects, 
        // - ApplyCriticalSuccessEffects, 
        // - ApplyCriticalFailureEffects, 
        // - GetCurrentTimeOfDay, 
        // - GetDialogueId, 
        // - etc.
        // 
        // You only replaced the parts where data was previously hard-coded.

        private bool IsDialogueAvailable(ICharacter player, ICharacter npc)
        {
            // Check if they're in the same location
            if (player.CharacterCurrentLocation != npc.CharacterCurrentLocation)
                return false;
            
            // Check NPC schedule availability
            TimeOfDay currentTime = GetCurrentTimeOfDay();
            if (npc.CharacterDailySchedule.DailyLocations[currentTime] != npc.CharacterCurrentLocation)
                return false;
            
            // Check relationship restrictions (e.g., certain NPCs may not talk to the player below a threshold)
            float relationshipLevel = _relationshipNetwork.GetRelationshipValue(player.CharacterId, npc.CharacterId, RelationshipParameter.Friendship);
            if (relationshipLevel < 0)
                return false;
            
            return true;
        }

        private List<DialogueChoice> GenerateDialogueChoices_(ICharacter player, ICharacter npc, DialogueContext context)
        {
            List<DialogueChoice> allOptions = GetAllDialogueOptions();
            List<DialogueChoice> availableOptions = new List<DialogueChoice>();
            
            foreach (var option in allOptions)
            {
                if (IsOptionAvailable(option, player, npc, context))
                {
                    availableOptions.Add(option);
                }
            }
            
            // Sort and limit options
            availableOptions.Sort((a, b) => CalculateOptionRelevance(b, player, npc, context)
                .CompareTo(CalculateOptionRelevance(a, player, npc, context)));
            
            // Return top options (limited to reasonable number)
            int maxOptions = Mathf.Min(4, availableOptions.Count);
            return availableOptions.GetRange(0, maxOptions);
        }

        private bool IsOptionAvailable(DialogueChoice option, ICharacter player, ICharacter npc, DialogueContext context)
        {
            if (option.Requirements == null || option.Requirements.Count == 0)
                return true;
            
            foreach (var requirement in option.Requirements)
            {
                if (!MeetsRequirement(requirement, player, npc, context))
                    return false;
            }
            
            return true;
        }

        private bool MeetsRequirement(RequirementCondition requirement, ICharacter player, ICharacter npc, DialogueContext context)
        {
            switch (requirement.Type)
            {
                case RequirementType.Skill:
                    return player.CharacterSkills.ContainsKey((SkillType)Enum.Parse(typeof(SkillType), requirement.Parameter)) && 
                           player.CharacterSkills[(SkillType)Enum.Parse(typeof(SkillType), requirement.Parameter)] >= requirement.MinValue;
                
                case RequirementType.Attribute:
                    return player.CharacterAttributes.ContainsKey((AttributeType)Enum.Parse(typeof(AttributeType), requirement.Parameter)) && 
                           player.CharacterAttributes[(AttributeType)Enum.Parse(typeof(AttributeType), requirement.Parameter)] >= requirement.MinValue;
                
                case RequirementType.RelationshipLevel:
                    return _relationshipNetwork.GetRelationshipValue(player.CharacterId, npc.CharacterId, 
                        (RelationshipParameter)Enum.Parse(typeof(RelationshipParameter), requirement.Parameter)) >= requirement.MinValue;
                
                case RequirementType.TimeOfDay:
                    return context.TimeOfDay.ToString() == requirement.Parameter;
                
                case RequirementType.Location:
                    return context.Location.ToString() == requirement.Parameter;
                
                case RequirementType.Event:
                    return _memoryManager.HasExperiencedEvent(npc.CharacterId, requirement.Parameter);
                
                default:
                    return false;
            }
        }

        private float CalculateOptionRelevance(DialogueChoice option, ICharacter player, ICharacter npc, DialogueContext context)
        {
            float relevance = 0f;
            
            // Topic continuity bonus
            if (option.Topic == context.PreviousTopic)
                relevance += 0.3f;
            
            // Emotion relevance
            if (IsTopicRelevantToEmotion(option.Topic, context.NpcCurrentEmotion))
                relevance += 0.2f;
            
            // Player skill relevance
            SkillType relevantSkill = GetRelevantSkill(option.Topic);
            if (player.CharacterSkills.ContainsKey(relevantSkill))
                relevance += player.CharacterSkills[relevantSkill] * 0.1f;
            
            // Relationship relevance
            foreach (var effect in option.PotentialEffects)
            {
                float currentValue = _relationshipNetwork.GetRelationshipValue(player.CharacterId, npc.CharacterId, effect.Key);
                
                // If this parameter is low and the effect is positive, it's more relevant
                if (currentValue < 3 && effect.Value > 0)
                    relevance += 0.2f;
                
                // If this parameter is already high and effect is positive, less relevant
                if (currentValue > 7 && effect.Value > 0)
                    relevance -= 0.1f;
            }
            
            return relevance;
        }

        private float CalculateSuccessRate(DialogueChoice choice, ICharacter player, ICharacter npc, DialogueContext context)
        {
            // Base success rate
            float baseSuccessRate = choice.SuccessRate;
            
            // Skill modifier
            float skillModifier = 0f;
            SkillType relevantSkill = GetRelevantSkill(choice.Topic);
            if (player.CharacterSkills.ContainsKey(relevantSkill))
            {
                skillModifier = (player.CharacterSkills[relevantSkill] - 5f) / 5f; // Normalize around 0
            }
            
            // Affinity modifier
            float affinityModifier = 0f;
            float friendship = _relationshipNetwork.GetRelationshipValue(player.CharacterId, npc.CharacterId, RelationshipParameter.Friendship);
            affinityModifier = (friendship - 5f) / 5f; // Normalize around 0
            
            // Situation modifier
            float situationModifier = 0f;
            
            // Location appropriateness
            if (IsLocationAppropriateForTopic(choice.Topic, context.Location))
                situationModifier += 0.2f;
            else
                situationModifier -= 0.2f;
            
            // Emotional state appropriateness
            if (IsTopicRelevantToEmotion(choice.Topic, context.NpcCurrentEmotion))
                situationModifier += 0.2f;
            else
                situationModifier -= 0.1f;
            
            // Calculate final success rate
            float finalSuccessRate = baseSuccessRate + 
                                     (skillModifier * _skillModifierWeight) + 
                                     (affinityModifier * _affinityModifierWeight) + 
                                     (situationModifier * _situationModifierWeight);
            
            // Clamp between 0.1 and 0.95
            return Mathf.Clamp(finalSuccessRate, 0.1f, 0.95f);
        }

        private void ApplyRelationshipEffects(DialogueChoice choice, ICharacter player, ICharacter npc)
        {
            foreach (var effect in choice.PotentialEffects)
            {
                float currentValue = _relationshipNetwork.GetRelationshipValue(player.CharacterId, npc.CharacterId, effect.Key);
                float newValue = Mathf.Clamp(currentValue + effect.Value, 0f, 10f);
                _relationshipNetwork.SetRelationshipValue(player.CharacterId, npc.CharacterId, effect.Key, newValue);
            }
        }

        private void ApplyCriticalSuccessEffects(ICharacter player, ICharacter npc, DialogueChoice choice)
        {
            // Double the relationship effects
            foreach (var effect in choice.PotentialEffects)
            {
                float currentValue = _relationshipNetwork.GetRelationshipValue(player.CharacterId, npc.CharacterId, effect.Key);
                float bonusEffect = effect.Value * 0.5f; // 50% bonus
                float newValue = Mathf.Clamp(currentValue + bonusEffect, 0f, 10f);
                _relationshipNetwork.SetRelationshipValue(player.CharacterId, npc.CharacterId, effect.Key, newValue);
            }
            
            // Unlock a preference
            PreferenceType randomPreference = GetRandomUnrevealedPreference(npc);
            RevealPreference(player, npc, randomPreference);
        }

        private void ApplyCriticalFailureEffects(ICharacter player, ICharacter npc, DialogueChoice choice)
        {
            // Apply negative effects to relationships
            foreach (RelationshipParameter param in Enum.GetValues(typeof(RelationshipParameter)))
            {
                float currentValue = _relationshipNetwork.GetRelationshipValue(player.CharacterId, npc.CharacterId, param);
                float newValue = Mathf.Clamp(currentValue - 0.5f, 0f, 10f); // Slight negative to all parameters
                _relationshipNetwork.SetRelationshipValue(player.CharacterId, npc.CharacterId, param, newValue);
            }
            
            // Record critical failure in memory
            _memoryManager.RecordNegativeImpression(player.CharacterId, npc.CharacterId, choice.Topic.ToString());
        }

        private string GenerateNpcDialogue_(ICharacter player, ICharacter npc, DialogueContext context)
        {
            // In a real implementation, this would probably be driven by dialogue data
            // For now, returning placeholder text
            switch (context.PreviousTopic)
            {
                case DialogueTopic.Greeting:
                    return $"Hello {player.CharacterName}, nice to see you today!";
                default:
                    return "What would you like to talk about?";
            }
        }

        private PreferenceType GetRandomUnrevealedPreference(ICharacter npc)
        {
            List<PreferenceType> unrevealed = new List<PreferenceType>();
            
            foreach (PreferenceType type in Enum.GetValues(typeof(PreferenceType)))
            {
                if (npc.CharacterPreferences.ContainsKey(type) && !IsPreferenceRevealed(npc, type))
                {
                    unrevealed.Add(type);
                }
            }
            
            if (unrevealed.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, unrevealed.Count);
                return unrevealed[randomIndex];
            }
            
            return PreferenceType.Food; // Default fallback
        }

        private void RevealPreference(ICharacter player, ICharacter npc, PreferenceType preferenceType)
        {
            GiftPreference preference = new GiftPreference
            {
                Type = preferenceType,
                ItemName = GetPreferredItemName(npc, preferenceType),
                PreferenceLevel = npc.CharacterPreferences[preferenceType],
                IsRevealed = true
            };
            
            // In a real implementation, this would be stored in a preference database
            // For now, just triggering an event
            InteractionSystem.EventManager.TriggerEvent("OnPreferenceDiscovered", new Dictionary<string, object>
            {
                { "Player", player },
                { "NPC", npc },
                { "Preference", preference }
            });
        }

        private string GetPreferredItemName(ICharacter npc, PreferenceType preferenceType)
        {
            // In a real implementation, this would be stored in character data
            // For now, just returning a placeholder
            return $"{npc.CharacterName}'s favorite {preferenceType.ToString().ToLower()}";
        }

        private bool IsPreferenceRevealed(ICharacter npc, PreferenceType type)
        {
            // In a real implementation, this would check a database
            // For now, just return a random value
            return UnityEngine.Random.value > 0.7f;
        }

        private bool IsLocationAppropriateForTopic(DialogueTopic topic, LocationType location)
        {
            switch (topic)
            {
                case DialogueTopic.Work:
                    return location == LocationType.Work;
                case DialogueTopic.Personal:
                case DialogueTopic.Relationships:
                    return location == LocationType.Home || location == LocationType.Restaurant || location == LocationType.Park;
                default:
                    return true;
            }
        }

        private bool IsTopicRelevantToEmotion(DialogueTopic topic, MentalState.EmotionalState emotion)
        {
            string emotionType = emotion.type.ToLower();
            
            if (emotionType.Contains("happy") || emotionType.Contains("joy"))
                return topic == DialogueTopic.Hobbies || topic == DialogueTopic.Personal;
            else if (emotionType.Contains("sad") || emotionType.Contains("sorrow"))
                return topic == DialogueTopic.Personal || topic == DialogueTopic.Relationships;
            else if (emotionType.Contains("angry") || emotionType.Contains("rage"))
                return topic == DialogueTopic.News || topic == DialogueTopic.Work;
            else
                return true;
        }

        private SkillType GetRelevantSkill(DialogueTopic topic)
        {
            switch (topic)
            {
                case DialogueTopic.Personal:
                case DialogueTopic.Relationships:
                    return SkillType.Empathy;
                case DialogueTopic.Work:
                    return SkillType.Persuasion;
                case DialogueTopic.Hobbies:
                    return SkillType.Charm;
                default:
                    return SkillType.Conversation;
            }
        }

        private TimeOfDay GetCurrentTimeOfDay()
        {
            // In a real implementation, this would check the game's time system
            // For now, just returning a placeholder
            return TimeOfDay.Afternoon;
        }

        private string GetDialogueId(ICharacter player, ICharacter npc)
        {
            return $"{player.CharacterId}_{npc.CharacterId}";
        }

        private List<DialogueChoice> GetAllDialogueOptions()
        {
            // In a real implementation, this would load from a data source
            // For now, just returning placeholder data
            return new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    ChoiceText = "How are you today?",
                    Topic = DialogueTopic.Greeting,
                    PotentialEffects = new Dictionary<RelationshipParameter, float>
                    {
                        { RelationshipParameter.Friendship, 0.1f }
                    },
                    Requirements = new List<RequirementCondition>(),
                    SuccessRate = 0.9f
                },
                new DialogueChoice
                {
                    ChoiceText = "Can you tell me about your work?",
                    Topic = DialogueTopic.Work,
                    PotentialEffects = new Dictionary<RelationshipParameter, float>
                    {
                        { RelationshipParameter.Friendship, 0.2f },
                        { RelationshipParameter.Respect, 0.3f }
                    },
                    Requirements = new List<RequirementCondition>
                    {
                        new RequirementCondition
                        {
                            Type = RequirementType.Skill,
                            Parameter = SkillType.Conversation.ToString(),
                            MinValue = 2f
                        }
                    },
                    SuccessRate = 0.7f
                },
                new DialogueChoice
                {
                    ChoiceText = "I've been thinking about you...",
                    Topic = DialogueTopic.Personal,
                    PotentialEffects = new Dictionary<RelationshipParameter, float>
                    {
                        { RelationshipParameter.Romance, 0.5f },
                        { RelationshipParameter.Comfort, 0.2f }
                    },
                    Requirements = new List<RequirementCondition>
                    {
                        new RequirementCondition
                        {
                            Type = RequirementType.RelationshipLevel,
                            Parameter = RelationshipParameter.Friendship.ToString(),
                            MinValue = 5f
                        }
                    },
                    SuccessRate = 0.5f
                }
            };
        }
    }

    public class MessageSystem : MonoBehaviour, IMessageSystem
    {
        [SerializeField] private float _baseResponseTime = 2f; // Hours
        [SerializeField] private float _relationshipResponseMultiplier = 0.5f;
        //[SerializeField] private int _maxThreadsPerCharacter = 20;
        
        private RelationshipNetwork _relationshipNetwork;
        private CharacterMemoryManager _memoryManager;
        
        private Dictionary<string, List<MessageThread>> _characterThreads = new Dictionary<string, List<MessageThread>>();

        private void Awake()
        {
            _relationshipNetwork = GetComponent<RelationshipNetwork>();
            _memoryManager = GetComponent<CharacterMemoryManager>();
        }

        public bool CanSendMessage(ICharacter recipient, MessageType type)
        {
            // Check if relationship level is high enough
            float requiredRelationship = GetRequiredRelationshipLevel(type);
            // Assume the player is the sender with ID "player"
            float actualRelationship = _relationshipNetwork.GetAverageRelationship("player", recipient.CharacterId);
            
            if (actualRelationship < requiredRelationship)
                return false;
            
            // Check cooldown period
            if (IsOnCooldown(recipient, type))
                return false;
            
            return true;
        }

        public void SendMessage(ICharacter sender, ICharacter recipient, MessageContent content)
        {
            string threadId = GetThreadId(sender, recipient);
            
            // Find or create thread
            MessageThread thread = FindThread(threadId);
            if (thread == null)
            {
                thread = CreateNewThread(sender, recipient);
            }
            
            // Add message to thread
            thread.Messages.Add(content);
            thread.LastActivity = DateTime.Now;
            thread.HasUnread = true;
            
            // Fire event
            InteractionSystem.EventManager.TriggerEvent("OnMessageSent", new Dictionary<string, object>
            {
                { "Sender", sender },
                { "Recipient", recipient },
                { "Content", content }
            });
        }

        public MessageResponse GetResponse(ICharacter sender, ICharacter recipient, MessageContent content)
        {
            // Calculate if the NPC will respond
            float relationshipLevel = _relationshipNetwork.GetAverageRelationship(sender.CharacterId, recipient.CharacterId);
            bool willRespond = UnityEngine.Random.value <= GetResponseProbability(relationshipLevel, content.Type);
            
            if (!willRespond)
            {
                return new MessageResponse
                {
                    WillRespond = false,
                    ResponseDelay = 0,
                    ResponseText = "",
                    ResponseEmotion = new MentalState.EmotionalState { type = "Neutral", currentValue = 0f, volatility = 0.5f, decayRate = 0.1f },
                    AcceptedInvitation = false
                };
            }
            
            // Calculate response delay
            float responseDelay = CalculateResponseDelay(relationshipLevel, recipient);
            
            // Generate response text
            string responseText = GenerateResponseText(sender, recipient, content);
            
            // Determine emotional state
            MentalState.EmotionalState emotion = DetermineResponseEmotion(recipient, content);
            
            // Handle invitations
            bool acceptedInvitation = false;
            if (content.Type == MessageType.Invitation && content.Invitation != null)
            {
                acceptedInvitation = DetermineInvitationResponse(sender, recipient, content.Invitation);
            }
            
            return new MessageResponse
            {
                WillRespond = true,
                ResponseDelay = responseDelay,
                ResponseText = responseText,
                ResponseEmotion = emotion,
                AcceptedInvitation = acceptedInvitation
            };
        }

        public List<MessageThread> GetActiveThreads(ICharacter character)
        {
            if (!_characterThreads.ContainsKey(character.CharacterId))
                return new List<MessageThread>();
            
            return _characterThreads[character.CharacterId]
                .OrderByDescending(t => t.LastActivity)
                .ToList();
        }

        private float GetRequiredRelationshipLevel(MessageType type)
        {
            switch (type)
            {
                case MessageType.Text:
                    return 2f;
                case MessageType.Invitation:
                    return 4f;
                case MessageType.Gift:
                    return 3f;
                case MessageType.Special:
                    return 5f;
                default:
                    return 1f;
            }
        }

        private bool IsOnCooldown(ICharacter recipient, MessageType type)
        {
            if (!_characterThreads.ContainsKey(recipient.CharacterId))
                return false;
            
            // Find the latest message of this type sent to the recipient
            DateTime lastMessageTime = DateTime.MinValue;
            
            foreach (var thread in _characterThreads[recipient.CharacterId])
            {
                foreach (var message in thread.Messages)
                {
                    if (message.Type == type && message.SendTime > lastMessageTime)
                    {
                        lastMessageTime = message.SendTime;
                    }
                }
            }
            
            // Calculate cooldown period based on message type
            TimeSpan cooldownPeriod = GetCooldownPeriod(type);
            
            return (DateTime.Now - lastMessageTime) < cooldownPeriod;
        }

        private TimeSpan GetCooldownPeriod(MessageType type)
        {
            switch (type)
            {
                case MessageType.Text:
                    return TimeSpan.FromHours(1);
                case MessageType.Invitation:
                    return TimeSpan.FromHours(12);
                case MessageType.Gift:
                    return TimeSpan.FromHours(24);
                case MessageType.Special:
                    return TimeSpan.FromHours(48);
                default:
                    return TimeSpan.FromHours(3);
            }
        }

        private string GetThreadId(ICharacter character1, ICharacter character2)
        {
            // Sort IDs to ensure consistent thread IDs regardless of who initiated
            string[] ids = new[] { character1.CharacterId, character2.CharacterId };
            Array.Sort(ids);
            return string.Join("_", ids);
        }

        private MessageThread FindThread(string threadId)
        {
            foreach (var threads in _characterThreads.Values)
            {
                foreach (var thread in threads)
                {
                    if (thread.ThreadId == threadId)
                    {
                        return thread;
                    }
                }
            }
            
            return null;
        }

        private MessageThread CreateNewThread(ICharacter character1, ICharacter character2)
        {
            string threadId = GetThreadId(character1, character2);
            
            MessageThread newThread = new MessageThread
            {
                ThreadId = threadId,
                Participant = character2, // From character1's perspective
                Messages = new List<MessageContent>(),
                LastActivity = DateTime.Now,
                HasUnread = false
            };
            
            // Add thread to both characters' lists
            if (!_characterThreads.ContainsKey(character1.CharacterId))
            {
                _characterThreads[character1.CharacterId] = new List<MessageThread>();
            }
            
            if (!_characterThreads.ContainsKey(character2.CharacterId))
            {
                _characterThreads[character2.CharacterId] = new List<MessageThread>();
            }
            
            _characterThreads[character1.CharacterId].Add(newThread);
            
            // Create a mirrored thread for character2
            MessageThread mirroredThread = new MessageThread
            {
                ThreadId = threadId,
                Participant = character1, // From character2's perspective
                Messages = new List<MessageContent>(),
                LastActivity = DateTime.Now,
                HasUnread = false
            };
            
            _characterThreads[character2.CharacterId].Add(mirroredThread);
            
            return newThread;
        }

        private float GetResponseProbability(float relationshipLevel, MessageType type)
        {
            // Base probability adjusted by relationship level
            float baseProbability = 0.5f + (relationshipLevel * 0.05f);
            
            // Adjust based on message type
            switch (type)
            {
                case MessageType.Text:
                    return baseProbability;
                case MessageType.Invitation:
                    return baseProbability * 0.8f; // Less likely to respond to invitations
                case MessageType.Gift:
                    return baseProbability * 1.2f; // More likely to respond to gifts
                case MessageType.Special:
                    return baseProbability * 1.5f; // Most likely to respond to special messages
                default:
                    return baseProbability;
            }
        }

        private float CalculateResponseDelay(float relationshipLevel, ICharacter character)
        {
            // Base delay in hours, reduced by relationship level
            float delay = _baseResponseTime - (relationshipLevel * _relationshipResponseMultiplier);
            
            // Make sure delay is at least 0.1 hours (6 minutes)
            delay = Mathf.Max(0.1f, delay);
            
            // Add random variation (±20%)
            float variation = UnityEngine.Random.Range(-0.2f, 0.2f) * delay;
            delay += variation;
            
            return delay;
        }

        private string GenerateResponseText(ICharacter sender, ICharacter recipient, MessageContent content)
        {
            // In a real implementation, this would use dialogue data or an NLP system
            // For now, just return placeholder text
            switch (content.Type)
            {
                case MessageType.Text:
                    return $"Thanks for messaging me, {sender.CharacterName}!";
                case MessageType.Invitation:
                    bool accepts = DetermineInvitationResponse(sender, recipient, content.Invitation);
                    return accepts 
                        ? $"I'd love to go to {content.Invitation.EventName} with you!" 
                        : "Sorry, I'm busy at that time.";
                case MessageType.Gift:
                    return "Thank you for the gift!";
                case MessageType.Special:
                    return "This is interesting...";
                default:
                    return "...";
            }
        }

        private MentalState.EmotionalState DetermineResponseEmotion(ICharacter character, MessageContent content)
        {
            // In a real implementation, this would be more sophisticated
            // For now, just return a simple emotion based on message type
            MentalState.EmotionalState emotion = new MentalState.EmotionalState();
            
            switch (content.Type)
            {
                case MessageType.Text:
                    emotion.type = "Neutral";
                    emotion.currentValue = 0f;
                    break;
                case MessageType.Invitation:
                    emotion.type = "Happy";
                    emotion.currentValue = 50f;
                    break;
                case MessageType.Gift:
                    emotion.type = "Excited";
                    emotion.currentValue = 75f;
                    break;
                case MessageType.Special:
                    emotion.type = "Surprised";
                    emotion.currentValue = 40f;
                    break;
                default:
                    emotion.type = "Neutral";
                    emotion.currentValue = 0f;
                    break;
            }
            
            emotion.volatility = 0.5f;
            emotion.decayRate = 0.1f;
            
            return emotion;
        }

        private bool DetermineInvitationResponse(ICharacter sender, ICharacter recipient, EventInvitation invitation)
        {
            // Check relationship level
            // Get average relationship level directly
            float relationshipLevel = _relationshipNetwork.GetAverageRelationship(sender.CharacterId, recipient.CharacterId);
            
            // Base acceptance chance based on relationship
            float acceptanceChance = 0.3f + (relationshipLevel * 0.07f);
            
            // Adjust based on location appropriateness
            switch (invitation.Location)
            {
                case LocationType.Restaurant:
                case LocationType.Entertainment:
                    acceptanceChance += 0.1f;
                    break;
                case LocationType.Work:
                    acceptanceChance -= 0.1f;
                    break;
            }
            
            // Character availability check (simplified)
            // In a real implementation, this would check the character's schedule
            bool isAvailable = UnityEngine.Random.value > 0.2f;
            if (!isAvailable)
                return false;
            
            return UnityEngine.Random.value <= acceptanceChance;
        }
    }

    public class GiftSystem : MonoBehaviour, IGiftSystem
    {
        [SerializeField] private float _baseEffectMultiplier = 1.0f;
        [SerializeField] private float _birthdayMultiplier = 2.0f;
        [SerializeField] private float _repetitionPenaltyBase = 0.2f;
        
        private RelationshipNetwork _relationshipNetwork;
        private CharacterMemoryManager _memoryManager;
        
        private Dictionary<string, Dictionary<string, int>> _giftHistory = new Dictionary<string, Dictionary<string, int>>();
        private Dictionary<string, Dictionary<PreferenceType, GiftPreference>> _revealedPreferences = 
            new Dictionary<string, Dictionary<PreferenceType, GiftPreference>>();

        private void Awake()
        {
            _relationshipNetwork = GetComponent<RelationshipNetwork>();
            _memoryManager = GetComponent<CharacterMemoryManager>();
        }

        public List<GiftItem> GetAvailableGifts(ICharacter player)
        {
            // In a real implementation, this would check player's inventory, store availability, etc.
            // For now, just return placeholder data
            return new List<GiftItem>
            {
                new GiftItem
                {
                    Name = "Chocolate Box",
                    Description = "A box of assorted chocolates.",
                    Category = GiftCategory.DailyNecessity,
                    Cost = 10,
                    BaseEffectValue = 2,
                    PreferenceMultipliers = new Dictionary<PreferenceType, float>
                    {
                        { PreferenceType.Food, 1.5f }
                    }
                },
                new GiftItem
                {
                    Name = "Book of Poetry",
                    Description = "A collection of classic poems.",
                    Category = GiftCategory.HobbyRelated,
                    Cost = 25,
                    BaseEffectValue = 4,
                    PreferenceMultipliers = new Dictionary<PreferenceType, float>
                    {
                        { PreferenceType.Literature, 2.0f }
                    }
                },
                new GiftItem
                {
                    Name = "Luxury Watch",
                    Description = "An elegant timepiece.",
                    Category = GiftCategory.Luxury,
                    Cost = 100,
                    BaseEffectValue = 8,
                    PreferenceMultipliers = new Dictionary<PreferenceType, float>
                    {
                        { PreferenceType.Fashion, 1.8f }
                    }
                }
            };
        }

        public float CalculateGiftEffect(GiftItem gift, ICharacter recipient)
        {
            // Base effect
            float effect = gift.BaseEffectValue * _baseEffectMultiplier;
            
            // Preference multipliers
            foreach (var prefMult in gift.PreferenceMultipliers)
            {
                if (recipient.CharacterPreferences.ContainsKey(prefMult.Key))
                {
                    float prefValue = recipient.CharacterPreferences[prefMult.Key];
                    effect *= 1 + ((prefValue / 10f) * (prefMult.Value - 1));
                }
            }
            
            // Special timing bonus (e.g., birthday)
            if (IsSpecialDay(recipient))
            {
                effect *= _birthdayMultiplier;
            }
            
            // Repetition penalty
            int timesGiven = GetTimesGiftGiven(recipient.CharacterId, gift.Name);
            if (timesGiven > 0)
            {
                float repetitionPenalty = 1f - (_repetitionPenaltyBase * timesGiven);
                repetitionPenalty = Mathf.Max(0.2f, repetitionPenalty); // At least 20% effect
                effect *= repetitionPenalty;
            }
            
            return effect;
        }

        public void ProcessGiftGiving(ICharacter player, ICharacter recipient, GiftItem gift)
        {
            // Calculate effect
            float effect = CalculateGiftEffect(gift, recipient);
            
            // Apply relationship changes
            ApplyGiftEffect(player, recipient, gift, effect);
            
            // Record gift history
            RecordGiftGiven(player.CharacterId, recipient.CharacterId, gift.Name);
            
            // Check for preference discovery
            CheckPreferenceDiscovery(player, recipient, gift);
            
            // Fire event
            InteractionSystem.EventManager.TriggerEvent("OnGiftGiven", new Dictionary<string, object>
            {
                { "Player", player },
                { "Recipient", recipient },
                { "Gift", gift },
                { "Effect", effect }
            });
        }

        public List<GiftPreference> GetRevealedPreferences(ICharacter character)
        {
            if (!_revealedPreferences.ContainsKey(character.CharacterId))
                return new List<GiftPreference>();
            
            return _revealedPreferences[character.CharacterId].Values.ToList();
        }

        private void ApplyGiftEffect(ICharacter player, ICharacter recipient, GiftItem gift, float effect)
        {
            // Different gift categories affect different relationship parameters
            Dictionary<RelationshipParameter, float> effectDistribution = GetEffectDistribution(gift.Category);
            
            // Apply effects to relationship parameters
            foreach (var paramEffect in effectDistribution)
            {
                float currentValue = _relationshipNetwork.GetRelationshipValue(player.CharacterId, recipient.CharacterId, paramEffect.Key);
                float change = effect * paramEffect.Value;
                float newValue = Mathf.Clamp(currentValue + change, 0f, 10f);
                
                _relationshipNetwork.SetRelationshipValue(player.CharacterId, recipient.CharacterId, paramEffect.Key, newValue);
            }
        }

        private Dictionary<RelationshipParameter, float> GetEffectDistribution(GiftCategory category)
        {
            switch (category)
            {
                case GiftCategory.DailyNecessity:
                    return new Dictionary<RelationshipParameter, float>
                    {
                        { RelationshipParameter.Friendship, 0.7f },
                        { RelationshipParameter.Comfort, 0.3f }
                    };
                case GiftCategory.HobbyRelated:
                    return new Dictionary<RelationshipParameter, float>
                    {
                        { RelationshipParameter.Friendship, 0.5f },
                        { RelationshipParameter.Respect, 0.5f }
                    };
                case GiftCategory.Luxury:
                    return new Dictionary<RelationshipParameter, float>
                    {
                        { RelationshipParameter.Romance, 0.6f },
                        { RelationshipParameter.Respect, 0.4f }
                    };
                case GiftCategory.Special:
                    return new Dictionary<RelationshipParameter, float>
                    {
                        { RelationshipParameter.Friendship, 0.3f },
                        { RelationshipParameter.Romance, 0.3f },
                        { RelationshipParameter.Trust, 0.2f },
                        { RelationshipParameter.Respect, 0.2f }
                    };
                default:
                    return new Dictionary<RelationshipParameter, float>
                    {
                        { RelationshipParameter.Friendship, 1.0f }
                    };
            }
        }

        private bool IsSpecialDay(ICharacter character)
        {
            // In a real implementation, this would check for birthdays, anniversaries, etc.
            // For now, just return a random value
            return UnityEngine.Random.value > 0.95f;
        }

        private int GetTimesGiftGiven(string characterId, string giftName)
        {
            if (!_giftHistory.ContainsKey(characterId) || !_giftHistory[characterId].ContainsKey(giftName))
                return 0;
            
            return _giftHistory[characterId][giftName];
        }

        private void RecordGiftGiven(string giverId, string receiverId, string giftName)
        {
            // Record in receiver's history
            if (!_giftHistory.ContainsKey(receiverId))
            {
                _giftHistory[receiverId] = new Dictionary<string, int>();
            }
            
            if (!_giftHistory[receiverId].ContainsKey(giftName))
            {
                _giftHistory[receiverId][giftName] = 0;
            }
            
            _giftHistory[receiverId][giftName]++;
            
            // Record in memory system
            _memoryManager.RecordGiftReceived(giverId, receiverId, giftName);
        }

        private void CheckPreferenceDiscovery(ICharacter player, ICharacter recipient, GiftItem gift)
        {
            foreach (var prefMult in gift.PreferenceMultipliers)
            {
                if (recipient.CharacterPreferences.ContainsKey(prefMult.Key))
                {
                    float discoveryChance = 0.3f;
                    
                    // Higher chance if the preference is strong
                    float prefValue = recipient.CharacterPreferences[prefMult.Key];
                    if (prefValue > 7f)
                    {
                        discoveryChance += 0.3f;
                    }
                    
                    // Higher chance if the gift is particularly relevant to this preference
                    if (prefMult.Value > 1.5f)
                    {
                        discoveryChance += 0.2f;
                    }
                    
                    if (UnityEngine.Random.value <= discoveryChance)
                    {
                        RevealPreference(player, recipient, prefMult.Key);
                    }
                }
            }
        }

        private void RevealPreference(ICharacter player, ICharacter recipient, PreferenceType preferenceType)
        {
            // Initialize dictionaries if needed
            if (!_revealedPreferences.ContainsKey(recipient.CharacterId))
            {
                _revealedPreferences[recipient.CharacterId] = new Dictionary<PreferenceType, GiftPreference>();
            }
            
            // Check if already revealed
            if (_revealedPreferences[recipient.CharacterId].ContainsKey(preferenceType))
                return;
            
            // Create and store preference info
            GiftPreference preference = new GiftPreference
            {
                Type = preferenceType,
                ItemName = GetPreferredItemName(recipient, preferenceType),
                PreferenceLevel = recipient.CharacterPreferences[preferenceType],
                IsRevealed = true
            };
            
            _revealedPreferences[recipient.CharacterId][preferenceType] = preference;
            
            // Fire event
            InteractionSystem.EventManager.TriggerEvent("OnPreferenceDiscovered", new Dictionary<string, object>
            {
                { "Player", player },
                { "NPC", recipient },
                { "Preference", preference }
            });
        }

        private string GetPreferredItemName(ICharacter character, PreferenceType preferenceType)
        {
            // In a real implementation, this would be stored in character data
            // For now, just returning a placeholder
            return $"{character.CharacterName}'s favorite {preferenceType.ToString().ToLower()}";
        }
    }

    public class InteractionSystem : MonoBehaviour
    {
        [SerializeField] private DialogueSystem _dialogueSystem;
        [SerializeField] private MessageSystem _messageSystem;
        [SerializeField] private GiftSystem _giftSystem;
        
        // Public API methods
        public IDialogueSystem DialogueSystem => _dialogueSystem;
        public IMessageSystem MessageSystem => _messageSystem;
        public IGiftSystem GiftSystem => _giftSystem;
        
        private void Awake()
        {
            if (_dialogueSystem == null)
                _dialogueSystem = GetComponent<DialogueSystem>();
            
            if (_messageSystem == null)
                _messageSystem = GetComponent<MessageSystem>();
            
            if (_giftSystem == null)
                _giftSystem = GetComponent<GiftSystem>();
            
            if (_dialogueSystem == null || _messageSystem == null || _giftSystem == null)
            {
                Debug.LogError("InteractionSystem: One or more required systems are missing!");
            }
        }
        
        // Helper methods to simplify common operations
        public void StartConversation(ICharacter player, ICharacter npc)
        {
            _dialogueSystem.InitiateDialogue(player, npc);
        }
        
        public bool SendMessage(ICharacter player, ICharacter npc, string text, MessageType type = MessageType.Text)
        {
            if (!_messageSystem.CanSendMessage(npc, type))
                return false;
            
            MessageContent content = new MessageContent
            {
                Text = text,
                Type = type,
                SendTime = DateTime.Now,
                AttachedGifts = new List<GiftItem>(),
                Invitation = null
            };
            
            _messageSystem.SendMessage(player, npc, content);
            return true;
        }
        
        public bool SendInvitation(ICharacter player, ICharacter npc, EventInvitation invitation)
        {
            if (!_messageSystem.CanSendMessage(npc, MessageType.Invitation))
                return false;
            
            MessageContent content = new MessageContent
            {
                Text = $"Would you like to join me for {invitation.EventName}?",
                Type = MessageType.Invitation,
                SendTime = DateTime.Now,
                AttachedGifts = new List<GiftItem>(),
                Invitation = invitation
            };
            
            _messageSystem.SendMessage(player, npc, content);
            return true;
        }
        
        public bool GiveGift(ICharacter player, ICharacter npc, GiftItem gift)
        {
            if (!_messageSystem.CanSendMessage(npc, MessageType.Gift))
                return false;
            
            _giftSystem.ProcessGiftGiving(player, npc, gift);
            
            // Also send a message about the gift
            MessageContent content = new MessageContent
            {
                Text = $"I got you a gift: {gift.Name}",
                Type = MessageType.Gift,
                SendTime = DateTime.Now,
                AttachedGifts = new List<GiftItem> { gift },
                Invitation = null
            };
            
            _messageSystem.SendMessage(player, npc, content);
            return true;
        }
        
        // Event system helper
        public static class EventManager
        {
            private static Dictionary<string, Action<Dictionary<string, object>>> _eventHandlers = 
                new Dictionary<string, Action<Dictionary<string, object>>>();
            
            public static void AddListener(string eventName, Action<Dictionary<string, object>> handler)
            {
                if (!_eventHandlers.ContainsKey(eventName))
                {
                    _eventHandlers[eventName] = null;
                }
                
                _eventHandlers[eventName] += handler;
            }
            
            public static void RemoveListener(string eventName, Action<Dictionary<string, object>> handler)
            {
                if (_eventHandlers.ContainsKey(eventName))
                {
                    _eventHandlers[eventName] -= handler;
                }
            }
            
            public static void TriggerEvent(string eventName, Dictionary<string, object> parameters)
            {
                if (_eventHandlers.ContainsKey(eventName))
                {
                    _eventHandlers[eventName]?.Invoke(parameters);
                }
            }
        }
    }

    #endregion
}