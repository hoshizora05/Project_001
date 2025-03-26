using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Relationship Network System that manages relationships between NPCs and the player.
    /// </summary>
    public class RelationshipNetwork : MonoBehaviour, ICharacterSubsystem
    {
        private static RelationshipNetwork _instance;
        public static RelationshipNetwork Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RelationshipNetwork>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RelationshipNetwork");
                        _instance = go.AddComponent<RelationshipNetwork>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private float updateFrequency = 10f; // Seconds between autonomous relationship updates
        [SerializeField] private float naturalDecayRate = 0.05f; // How quickly relationships naturally decay
        [SerializeField] private bool debugMode = false;

        private RelationshipGraph _relationshipGraph = new RelationshipGraph();
        private ReputationSystem _reputationSystem = new ReputationSystem();
        private InformationPropagationModel _informationModel = new InformationPropagationModel();

        private float _timeSinceLastUpdate = 0f;

        #region ICharacterSubsystem Implementation
        
        public void Initialize()
        {
            Debug.Log("[RelationshipNetwork] Initializing system...");
            
            // Subscribe to relevant events
            CharacterEventBus.Instance.Subscribe("relationship_change", HandleRelationshipChangeEvent);
            CharacterEventBus.Instance.Subscribe("reputation_update", HandleReputationUpdateEvent);
            CharacterEventBus.Instance.Subscribe("information_share", HandleInformationShareEvent);
            CharacterEventBus.Instance.Subscribe("group_change", HandleGroupChangeEvent);
        }

        void ICharacterSubsystem.Update(float deltaTime)
        {
            _timeSinceLastUpdate += deltaTime;
            
            // Perform periodic updates based on frequency
            if (_timeSinceLastUpdate >= updateFrequency)
            {
                UpdateNetwork(deltaTime);
                _timeSinceLastUpdate = 0f;
            }
        }

        public void Reset()
        {
            _relationshipGraph = new RelationshipGraph();
            _reputationSystem = new ReputationSystem();
            _informationModel = new InformationPropagationModel();
            _timeSinceLastUpdate = 0f;
        }
        
        #endregion

        #region Public API Methods

        /// <summary>
        /// Retrieves the relationship status between two entities
        /// </summary>
        public RelationshipData GetRelationshipStatus(string entityId1, string entityId2)
        {
            // Find the relationship in the graph
            var relationship = _relationshipGraph.relationships.FirstOrDefault(
                r => (r.sourceId == entityId1 && r.targetId == entityId2) || 
                     (r.sourceId == entityId2 && r.targetId == entityId1));
                     
            if (relationship == null)
            {
                if (debugMode)
                    Debug.LogWarning($"[RelationshipNetwork] No relationship found between {entityId1} and {entityId2}");
                    
                return null;
            }
            
            // Ensure source is entityId1
            if (relationship.sourceId != entityId1)
            {
                // Create a reversed view of the relationship
                return new RelationshipData
                {
                    sourceId = entityId1,
                    targetId = entityId2,
                    type = relationship.type,
                    strength = relationship.strength,
                    trust = relationship.trust,
                    familiarity = relationship.familiarity,
                    history = relationship.history,
                    hiddenAttributes = relationship.hiddenAttributes
                };
            }
            
            return relationship;
        }

        /// <summary>
        /// Updates a relationship between source and target entities based on an interaction
        /// </summary>
        public RelationshipChangeResult UpdateRelationship(string sourceId, string targetId, string interactionType, float intensity)
        {
            RelationshipData relationship = _relationshipGraph.relationships.FirstOrDefault(
                r => r.sourceId == sourceId && r.targetId == targetId);

            // If relationship doesn't exist yet, create it
            if (relationship == null)
            {
                relationship = CreateRelationship(sourceId, targetId);
                _relationshipGraph.relationships.Add(relationship);
            }

            // Create relationship event record
            RelationshipHistoryEntry historyEntry = new RelationshipHistoryEntry
            {
                eventEntry = interactionType,
                impact = intensity,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            relationship.history.Add(historyEntry);

            // Apply interaction effects
            float previousStrength = relationship.strength;
            relationship.strength = Mathf.Clamp(relationship.strength + intensity, -100f, 100f);
            
            // Adjust trust and familiarity based on interaction
            relationship.trust = Mathf.Clamp(relationship.trust + (intensity * 0.5f), 0f, 100f);
            relationship.familiarity = Mathf.Clamp(relationship.familiarity + 5f, 0f, 100f);

            // Create and publish event
            PublishRelationshipChangeEvent(sourceId, targetId, previousStrength, relationship.strength, interactionType);
            
            // Check for potential group changes
            CheckGroupCohesionUpdate(sourceId, targetId, intensity);

            // Create result
            var result = new RelationshipChangeResult
            {
                sourceId = sourceId,
                targetId = targetId,
                previousStrength = previousStrength,
                newStrength = relationship.strength,
                changeAmount = relationship.strength - previousStrength,
                interactionType = interactionType
            };
            
            return result;
        }

        /// <summary>
        /// Gets an entity's reputation from a specific perspective
        /// </summary>
        public ReputationData GetReputation(string entityId, string perspective = null)
        {
            // Get the entity's reputation from the system
            ReputationData entityReputation = _reputationSystem.entities.FirstOrDefault(e => e.id == entityId);
            
            if (entityReputation == null)
            {
                if (debugMode)
                    Debug.LogWarning($"[RelationshipNetwork] No reputation data found for entity {entityId}");
                    
                return null;
            }
            
            // If no perspective specified, return global reputation
            if (string.IsNullOrEmpty(perspective))
            {
                return entityReputation;
            }
            
            // If perspective is a group, return group-specific reputation
            if (entityReputation.reputationPerGroup.ContainsKey(perspective))
            {
                // Create a view of the reputation specific to this perspective
                var groupReputation = new ReputationData
                {
                    id = entityId,
                    globalReputation = (float)entityReputation.reputationPerGroup[perspective]
                };
                
                // Copy only reputation scores known to this group
                foreach (var trait in entityReputation.reputationScores)
                {
                    if (trait.Value.sources.Contains(perspective))
                    {
                        groupReputation.reputationScores[trait.Key] = trait.Value;
                    }
                }
                
                return groupReputation;
            }
            
            // Otherwise, provide reputation from perspective of another entity
            RelationshipData relationship = GetRelationshipStatus(perspective, entityId);
            if (relationship != null)
            {
                float perspectiveModifier = relationship.strength / 100f;
                
                // Create a view of the reputation that's colored by the relationship
                var perspectiveReputation = new ReputationData
                {
                    id = entityId,
                    globalReputation = entityReputation.globalReputation * (1f + perspectiveModifier * 0.3f)
                };
                
                // Adjust reputation scores based on relationship
                foreach (var trait in entityReputation.reputationScores)
                {
                    if (trait.Value.sources.Contains(perspective))
                    {
                        var score = new ReputationScore
                        {
                            value = trait.Value.value * (1f + perspectiveModifier * 0.3f),
                            confidence = trait.Value.confidence,
                            sources = trait.Value.sources
                        };
                        
                        perspectiveReputation.reputationScores[trait.Key] = score;
                    }
                }
                
                return perspectiveReputation;
            }
            
            // Default fallback to global reputation
            return entityReputation;
        }

        /// <summary>
        /// Creates and propagates a rumor in the network
        /// </summary>
        public RumorData CreateAndPropagateRumor(string originatorId, string subjectId, string contentType, float truthValue)
        {
            // Create the rumor
            RumorData rumor = new RumorData
            {
                id = Guid.NewGuid().ToString(),
                subject = subjectId,
                content = contentType,
                truth = truthValue,
                spreadPattern = new Dictionary<string, object>
                {
                    { "initialSpreadRate", 0.7f },
                    { "mutationChance", 0.2f }
                },
                knownBy = new List<string> { originatorId },
                currentRelevance = 100f
            };
            
            // Add to the reputation system
            _reputationSystem.rumors.Add(rumor);
            
            // Initial propagation
            PropagateRumor(rumor, originatorId);
            
            // Publish event
            PublishRumorCreatedEvent(originatorId, rumor);
            
            return rumor;
        }

        /// <summary>
        /// Shares information between entities
        /// </summary>
        public bool ShareInformation(string senderId, string receiverId, string informationId, string channelType)
        {
            // Find the information unit
            InformationUnit info = _informationModel.informationUnits.FirstOrDefault(i => i.id == informationId);
            
            if (info == null)
            {
                if (debugMode)
                    Debug.LogWarning($"[RelationshipNetwork] Information with id {informationId} not found");
                    
                return false;
            }
            
            // Check if sender has the information
            if (!info.knownBy.ContainsKey(senderId))
            {
                if (debugMode)
                    Debug.LogWarning($"[RelationshipNetwork] Sender {senderId} doesn't know information {informationId}");
                    
                return false;
            }
            
            // Find the communication channel
            CommunicationChannel channel = _informationModel.communicationChannels.FirstOrDefault(
                c => c.type == channelType && 
                c.participatingEntities.Contains(senderId) && 
                c.participatingEntities.Contains(receiverId));
            
            // If no specific channel exists, create a temporary direct one
            if (channel == null)
            {
                channel = new CommunicationChannel
                {
                    type = "direct_conversation",
                    participatingEntities = new List<string> { senderId, receiverId },
                    bandwidth = 0.9f,
                    noiseLevel = 0.1f,
                    accessRestrictions = new Dictionary<string, object>()
                };
            }
            
            // Get relationship to determine trust and accuracy of transmission
            RelationshipData relationship = GetRelationshipStatus(senderId, receiverId);
            float trustFactor = 0.5f;
            
            if (relationship != null)
            {
                trustFactor = relationship.trust / 100f;
            }
            
            // Calculate information transmission factors
            float noiseFactor = channel.noiseLevel;
            float accuracyDegradation = noiseFactor * (1f - trustFactor);
            float detailDegradation = noiseFactor * 0.5f;
            
            // Get sender's knowledge of information
            var senderKnowledge = info.knownBy[senderId];
            
            // Create or update receiver's knowledge
            if (info.knownBy.ContainsKey(receiverId))
            {
                // Existing knowledge is updated
                var receiverKnowledge = info.knownBy[receiverId];
                
                // Only update if the source is more accurate or detailed
                if (senderKnowledge.accuracy > receiverKnowledge.accuracy)
                {
                    receiverKnowledge.accuracy = Mathf.Lerp(
                        receiverKnowledge.accuracy, 
                        senderKnowledge.accuracy * (1f - accuracyDegradation),
                        0.7f);
                }
                
                if (senderKnowledge.detail > receiverKnowledge.detail)
                {
                    receiverKnowledge.detail = Mathf.Lerp(
                        receiverKnowledge.detail,
                        senderKnowledge.detail * (1f - detailDegradation),
                        0.7f);
                }
                
                info.knownBy[receiverId] = receiverKnowledge;
            }
            else
            {
                // New knowledge
                info.knownBy[receiverId] = new InformationKnowledge
                {
                    accuracy = senderKnowledge.accuracy * (1f - accuracyDegradation),
                    detail = senderKnowledge.detail * (1f - detailDegradation),
                    acquired = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }
            
            // Publish information shared event
            PublishInformationSharedEvent(senderId, receiverId, info);
            
            // Update relationship - sharing information increases familiarity
            UpdateRelationship(senderId, receiverId, "information_sharing", 5f * info.sensitivity);
            
            return true;
        }

        /// <summary>
        /// Checks if an entity knows specific information and to what degree
        /// </summary>
        public InformationKnowledge CheckKnowledgeState(string entityId, string informationId)
        {
            // Find the information unit
            InformationUnit info = _informationModel.informationUnits.FirstOrDefault(i => i.id == informationId);
            
            if (info == null)
            {
                if (debugMode)
                    Debug.LogWarning($"[RelationshipNetwork] Information with id {informationId} not found");
                    
                return null;
            }
            
            // Check if entity knows about it
            if (info.knownBy.ContainsKey(entityId))
            {
                return info.knownBy[entityId];
            }
            
            return null;
        }

        /// <summary>
        /// Manages group membership (add, remove, change role)
        /// </summary>
        public bool ManageGroupMembership(string groupId, string entityId, string action)
        {
            // Find the group
            GroupData group = _relationshipGraph.groups.FirstOrDefault(g => g.id == groupId);
            
            if (group == null)
            {
                if (debugMode)
                    Debug.LogWarning($"[RelationshipNetwork] Group with id {groupId} not found");
                    
                return false;
            }
            
            bool result = false;
            
            switch (action.ToLower())
            {
                case "add":
                    if (!group.members.Contains(entityId))
                    {
                        group.members.Add(entityId);
                        result = true;
                        
                        // Create relationships with all group members
                        foreach (var memberId in group.members)
                        {
                            if (memberId != entityId)
                            {
                                // If no relationship exists, create an initial positive one
                                if (GetRelationshipStatus(entityId, memberId) == null)
                                {
                                    var relationship = CreateRelationship(entityId, memberId);
                                    relationship.type = "group_member";
                                    relationship.strength = 20f;
                                    relationship.trust = 30f;
                                    relationship.familiarity = 20f;
                                    
                                    _relationshipGraph.relationships.Add(relationship);
                                }
                            }
                        }
                    }
                    break;
                    
                case "remove":
                    if (group.members.Contains(entityId))
                    {
                        group.members.Remove(entityId);
                        result = true;
                        
                        // Update relationships with all group members
                        foreach (var memberId in group.members)
                        {
                            var relationship = GetRelationshipStatus(entityId, memberId);
                            if (relationship != null && relationship.type == "group_member")
                            {
                                // Weaken the relationship slightly
                                UpdateRelationship(entityId, memberId, "left_group", -10f);
                            }
                        }
                    }
                    break;
                    
                case "promote":
                    if (group.members.Contains(entityId) && group.hierarchies != null)
                    {
                        // Simplified hierarchy system - just record the promotion
                        if (!group.hierarchies.ContainsKey("leadership"))
                        {
                            group.hierarchies["leadership"] = new List<string>();
                        }
                        
                        if (!((List<string>)group.hierarchies["leadership"]).Contains(entityId))
                        {
                            ((List<string>)group.hierarchies["leadership"]).Add(entityId);
                            result = true;
                            
                            // Increase influence of the entity
                            var entity = _relationshipGraph.entities.FirstOrDefault(e => e.id == entityId);
                            if (entity != null)
                            {
                                entity.influence += 10f;
                            }
                        }
                    }
                    break;
                    
                case "demote":
                    if (group.members.Contains(entityId) && group.hierarchies != null &&
                        group.hierarchies.ContainsKey("leadership"))
                    {
                        var leaders = (List<string>)group.hierarchies["leadership"];
                        if (leaders.Contains(entityId))
                        {
                            leaders.Remove(entityId);
                            result = true;
                            
                            // Decrease influence of the entity
                            var entity = _relationshipGraph.entities.FirstOrDefault(e => e.id == entityId);
                            if (entity != null)
                            {
                                entity.influence -= 5f;
                            }
                        }
                    }
                    break;
            }
            
            if (result)
            {
                // Publish group membership change event
                PublishGroupMembershipEvent(groupId, entityId, action);
            }
            
            return result;
        }

        /// <summary>
        /// Adds a new entity to the relationship network
        /// </summary>
        public void AddEntity(string id, string type, Vector3 position, float influence = 1f)
        {
            // Check if entity already exists
            if (_relationshipGraph.entities.Any(e => e.id == id))
            {
                if (debugMode)
                    Debug.LogWarning($"[RelationshipNetwork] Entity with id {id} already exists");
                    
                return;
            }
            
            // Create and add entity
            var entity = new EntityData
            {
                id = id,
                type = type,
                position = position,
                influence = influence
            };
            
            _relationshipGraph.entities.Add(entity);
            
            // Initialize reputation for this entity
            var reputation = new ReputationData
            {
                id = id,
                reputationScores = new Dictionary<string, ReputationScore>(),
                globalReputation = 0f,
                reputationPerGroup = new Dictionary<string, object>()
            };
            
            _reputationSystem.entities.Add(reputation);
        }

        /// <summary>
        /// Creates a new group in the relationship network
        /// </summary>
        public GroupData CreateGroup(string id, string purpose, float cohesion = 50f)
        {
            // Check if group already exists
            if (_relationshipGraph.groups.Any(g => g.id == id))
            {
                if (debugMode)
                    Debug.LogWarning($"[RelationshipNetwork] Group with id {id} already exists");
                    
                return null;
            }
            
            // Create and add group
            var group = new GroupData
            {
                id = id,
                members = new List<string>(),
                cohesion = cohesion,
                purpose = purpose,
                hierarchies = new Dictionary<string, object>()
            };
            
            _relationshipGraph.groups.Add(group);
            
            return group;
        }

        /// <summary>
        /// Creates a new information unit in the network
        /// </summary>
        public InformationUnit CreateInformationUnit(string id, string type, Dictionary<string, object> content, 
                                                   float sensitivity, string originatorId)
        {
            // Check if information already exists
            if (_informationModel.informationUnits.Any(i => i.id == id))
            {
                if (debugMode)
                    Debug.LogWarning($"[RelationshipNetwork] Information with id {id} already exists");
                    
                return null;
            }
            
            // Create information
            var infoUnit = new InformationUnit
            {
                id = id,
                type = type,
                content = content,
                sensitivity = sensitivity,
                originatorId = originatorId,
                knownBy = new Dictionary<string, InformationKnowledge>(),
                spreadFactors = new InformationSpreadFactors
                {
                    interestLevel = UnityEngine.Random.Range(0.3f, 0.8f),
                    controversyLevel = sensitivity * 0.7f,
                    believability = UnityEngine.Random.Range(0.5f, 1f)
                }
            };
            
            // Add originator as first knower with perfect knowledge
            infoUnit.knownBy[originatorId] = new InformationKnowledge
            {
                accuracy = 1f,
                detail = 1f,
                acquired = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            _informationModel.informationUnits.Add(infoUnit);
            
            return infoUnit;
        }

        /// <summary>
        /// Creates a communication channel between entities
        /// </summary>
        public CommunicationChannel CreateCommunicationChannel(string type, List<string> participatingEntities, 
                                                              float bandwidth, float noiseLevel)
        {
            var channel = new CommunicationChannel
            {
                type = type,
                participatingEntities = participatingEntities,
                bandwidth = bandwidth,
                noiseLevel = noiseLevel,
                accessRestrictions = new Dictionary<string, object>()
            };
            
            _informationModel.communicationChannels.Add(channel);
            
            return channel;
        }
        
        #endregion

        #region Internal Helper Methods

        /// <summary>
        /// Performs periodic updates to the relationship network
        /// </summary>
        private void UpdateNetwork(float deltaTime)
        {
            if (debugMode)
                Debug.Log("[RelationshipNetwork] Updating network...");
                
            // Update relationships
            UpdateRelationships();
            
            // Update reputations
            UpdateReputations();
            
            // Update information relevance and propagation
            UpdateInformation();
            
            // Update group dynamics
            UpdateGroups();
        }

        /// <summary>
        /// Updates all relationships in the network
        /// </summary>
        private void UpdateRelationships()
        {
            // Natural relationship decay for unused relationships
            foreach (var relationship in _relationshipGraph.relationships)
            {
                // Skip if updated recently
                if (relationship.history.Count > 0 && 
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - relationship.history.Last().timestamp < 86400000) // 24 hours
                {
                    continue;
                }
                
                // Apply natural decay - relationships weaken over time if not maintained
                if (Mathf.Abs(relationship.strength) > 0)
                {
                    float decay = Mathf.Sign(relationship.strength) * -1 * naturalDecayRate;
                    relationship.strength = Mathf.Clamp(relationship.strength + decay, -100f, 100f);
                }
                
                // Familiarity also decays slightly
                if (relationship.familiarity > 0)
                {
                    relationship.familiarity = Mathf.Max(0, relationship.familiarity - (naturalDecayRate * 0.5f));
                }
            }
            
            // Simulated NPC-to-NPC interactions (simplified)
            if (UnityEngine.Random.value < 0.3f) // 30% chance each update
            {
                // Select random entities that aren't the player
                var npcs = _relationshipGraph.entities.Where(e => e.type != "Player").ToList();
                
                if (npcs.Count >= 2)
                {
                    int idx1 = UnityEngine.Random.Range(0, npcs.Count);
                    int idx2 = UnityEngine.Random.Range(0, npcs.Count);
                    
                    // Ensure different entities
                    while (idx1 == idx2 && npcs.Count > 1)
                    {
                        idx2 = UnityEngine.Random.Range(0, npcs.Count);
                    }
                    
                    if (idx1 != idx2)
                    {
                        string entity1 = npcs[idx1].id;
                        string entity2 = npcs[idx2].id;
                        
                        // Get existing relationship to determine interaction type
                        var relationship = GetRelationshipStatus(entity1, entity2);
                        
                        string[] positiveInteractions = { "friendly_chat", "assistance", "compliment" };
                        string[] negativeInteractions = { "argument", "criticism", "avoidance" };
                        string[] neutralInteractions = { "small_talk", "mundane_interaction" };
                        
                        string interactionType;
                        float intensity;
                        
                        if (relationship != null && relationship.strength > 30f)
                        {
                            interactionType = positiveInteractions[UnityEngine.Random.Range(0, positiveInteractions.Length)];
                            intensity = UnityEngine.Random.Range(2f, 8f);
                        }
                        else if (relationship != null && relationship.strength < -30f)
                        {
                            interactionType = negativeInteractions[UnityEngine.Random.Range(0, negativeInteractions.Length)];
                            intensity = UnityEngine.Random.Range(-8f, -2f);
                        }
                        else
                        {
                            interactionType = neutralInteractions[UnityEngine.Random.Range(0, neutralInteractions.Length)];
                            intensity = UnityEngine.Random.Range(-2f, 2f);
                        }
                        
                        // Apply interaction effect
                        UpdateRelationship(entity1, entity2, interactionType, intensity);
                        
                        if (debugMode)
                            Debug.Log($"[RelationshipNetwork] Simulated interaction: {entity1} and {entity2} - {interactionType}");
                    }
                }
            }
        }

        /// <summary>
        /// Updates reputations in the system
        /// </summary>
        private void UpdateReputations()
        {
            // Update rumor relevance
            foreach (var rumor in _reputationSystem.rumors)
            {
                // Rumors become less relevant over time
                rumor.currentRelevance = Mathf.Max(0, rumor.currentRelevance - (naturalDecayRate * 2f));
                
                // Propagate active rumors
                if (rumor.currentRelevance > 20f && UnityEngine.Random.value < 0.4f)
                {
                    if (rumor.knownBy.Count > 0)
                    {
                        string spreader = rumor.knownBy[UnityEngine.Random.Range(0, rumor.knownBy.Count)];
                        PropagateRumor(rumor, spreader);
                    }
                }
            }
            
            // Cleanup expired rumors
            _reputationSystem.rumors.RemoveAll(r => r.currentRelevance <= 0);
        }

        /// <summary>
        /// Updates information propagation in the network
        /// </summary>
        private void UpdateInformation()
        {
            // Select a random piece of information for potential propagation
            if (_informationModel.informationUnits.Count > 0 && UnityEngine.Random.value < 0.5f)
            {
                var infoUnit = _informationModel.informationUnits[UnityEngine.Random.Range(0, _informationModel.informationUnits.Count)];
                
                // Select a random knower to share it
                if (infoUnit.knownBy.Count > 0)
                {
                    string sharer = infoUnit.knownBy.Keys.ElementAt(UnityEngine.Random.Range(0, infoUnit.knownBy.Count));
                    
                    // Find potential recipient based on relationships
                    var relationships = _relationshipGraph.relationships.Where(r => 
                        r.sourceId == sharer && r.strength > 0 || 
                        r.targetId == sharer && r.strength > 0).ToList();
                    
                    if (relationships.Count > 0)
                    {
                        var relationship = relationships[UnityEngine.Random.Range(0, relationships.Count)];
                        string recipient = relationship.sourceId == sharer ? relationship.targetId : relationship.sourceId;
                        
                        // Chance to share based on sensitivity and trust
                        float trustFactor = relationship.trust / 100f;
                        float sharingChance = Mathf.Lerp(0.1f, 0.9f, trustFactor) * 
                                             (1f - (infoUnit.sensitivity * 0.5f));
                                             
                        if (UnityEngine.Random.value < sharingChance)
                        {
                            ShareInformation(sharer, recipient, infoUnit.id, "autonomous_sharing");
                            
                            if (debugMode)
                                Debug.Log($"[RelationshipNetwork] Autonomous information sharing: {sharer} shared {infoUnit.id} with {recipient}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates group dynamics in the network
        /// </summary>
        private void UpdateGroups()
        {
            foreach (var group in _relationshipGraph.groups)
            {
                // Skip empty groups
                if (group.members.Count == 0)
                    continue;
                    
                // Calculate average relationship strength between members
                float totalStrength = 0f;
                int relationshipCount = 0;
                
                for (int i = 0; i < group.members.Count; i++)
                {
                    for (int j = i + 1; j < group.members.Count; j++)
                    {
                        var relationship = GetRelationshipStatus(group.members[i], group.members[j]);
                        if (relationship != null)
                        {
                            totalStrength += relationship.strength;
                            relationshipCount++;
                        }
                    }
                }
                
                float averageStrength = relationshipCount > 0 ? totalStrength / relationshipCount : 0;
                
                // Update group cohesion based on average relationship strength
                group.cohesion = Mathf.Lerp(group.cohesion, Mathf.Max(0, averageStrength), 0.1f);
                
                // Very low cohesion might lead to group dissolution (not implemented here)
                if (group.cohesion < 10f && group.members.Count > 0)
                {
                    if (debugMode)
                        Debug.LogWarning($"[RelationshipNetwork] Group {group.id} has very low cohesion ({group.cohesion})");
                }
            }
        }

        /// <summary>
        /// Propagates a rumor from a source entity to others
        /// </summary>
        private void PropagateRumor(RumorData rumor, string sourceId)
        {
            // Get relationships of the source
            var relationships = _relationshipGraph.relationships
                .Where(r => (r.sourceId == sourceId || r.targetId == sourceId) && r.strength > -20f)
                .ToList();
                
            if (relationships.Count == 0)
                return;
                
            // Select a random relationship for propagation
            var relationship = relationships[UnityEngine.Random.Range(0, relationships.Count)];
            string targetId = relationship.sourceId == sourceId ? relationship.targetId : relationship.sourceId;
            
            // Skip if target already knows the rumor
            if (rumor.knownBy.Contains(targetId))
                return;
                
            // Calculate propagation chance based on relationship strength and trust
            float trustFactor = (relationship.trust + 50) / 150f; // Normalize to 0.33-1 range
            float spreadChance = (float)rumor.spreadPattern["initialSpreadRate"] * trustFactor;
            
            if (UnityEngine.Random.value < spreadChance)
            {
                // Rumor propagates successfully
                rumor.knownBy.Add(targetId);
                
                // Check if rumor mutates
                if (UnityEngine.Random.value < (float)rumor.spreadPattern["mutationChance"])
                {
                    // Create mutated version with reduced truth value
                    float newTruth = Mathf.Max(0, rumor.truth - UnityEngine.Random.Range(10f, 30f));
                    
                    RumorData mutatedRumor = new RumorData
                    {
                        id = Guid.NewGuid().ToString(),
                        subject = rumor.subject,
                        content = rumor.content + " (mutated)",
                        truth = newTruth,
                        spreadPattern = rumor.spreadPattern,
                        knownBy = new List<string> { targetId },
                        currentRelevance = rumor.currentRelevance * 0.8f
                    };
                    
                    _reputationSystem.rumors.Add(mutatedRumor);
                    
                    if (debugMode)
                        Debug.Log($"[RelationshipNetwork] Rumor mutated when spreading from {sourceId} to {targetId} - Truth: {newTruth}%");
                }
                else
                {
                    if (debugMode)
                        Debug.Log($"[RelationshipNetwork] Rumor spread from {sourceId} to {targetId}");
                }
                
                // Update reputation of subject based on rumor
                UpdateReputationBasedOnRumor(rumor);
            }
        }

        /// <summary>
        /// Updates reputation based on a propagating rumor
        /// </summary>
        private void UpdateReputationBasedOnRumor(RumorData rumor)
        {
            // Find the entity in the reputation system
            ReputationData entityReputation = _reputationSystem.entities.FirstOrDefault(e => e.id == rumor.subject);
            
            if (entityReputation == null)
                return;
                
            // Extract trait type from content (simplified)
            string traitType = "general";
            if (rumor.content.Contains("dishonest"))
                traitType = "honesty";
            else if (rumor.content.Contains("aggressive") || rumor.content.Contains("violent"))
                traitType = "peacefulness";
            else if (rumor.content.Contains("generous") || rumor.content.Contains("charitable"))
                traitType = "generosity";
                
            // Calculate impact based on rumor properties
            float impact = ((rumor.truth / 100f) * 2f - 1f) * 10f; // Range -10 to +10
            impact *= rumor.currentRelevance / 100f; // Scale by relevance
            
            // Apply to reputation
            if (entityReputation.reputationScores.ContainsKey(traitType))
            {
                var score = entityReputation.reputationScores[traitType];
                score.value = Mathf.Clamp(score.value + impact, -100f, 100f);
                
                // Add sources that don't already know about it
                foreach (var knower in rumor.knownBy)
                {
                    if (!score.sources.Contains(knower))
                    {
                        score.sources.Add(knower);
                    }
                }
                
                // Update confidence based on number of sources
                score.confidence = Mathf.Min(100f, 40f + (score.sources.Count * 5f));
                
                entityReputation.reputationScores[traitType] = score;
            }
            else
            {
                // Create new reputation score
                var score = new ReputationScore
                {
                    value = Mathf.Clamp(impact, -100f, 100f),
                    confidence = 40f, // Initial confidence
                    sources = new List<string>(rumor.knownBy)
                };
                
                entityReputation.reputationScores[traitType] = score;
            }
            
            // Update global reputation
            UpdateGlobalReputation(entityReputation);
        }

        /// <summary>
        /// Updates the global reputation score based on trait scores
        /// </summary>
        private void UpdateGlobalReputation(ReputationData entityReputation)
        {
            if (entityReputation.reputationScores.Count == 0)
            {
                entityReputation.globalReputation = 0f;
                return;
            }
            
            float totalWeightedScore = 0f;
            float totalWeight = 0f;
            
            foreach (var score in entityReputation.reputationScores)
            {
                float weight = score.Value.confidence / 100f;
                totalWeightedScore += score.Value.value * weight;
                totalWeight += weight;
            }
            
            entityReputation.globalReputation = totalWeight > 0 ? totalWeightedScore / totalWeight : 0f;
            
            // Update group-specific reputations
            foreach (var group in _relationshipGraph.groups)
            {
                // Calculate reputation for this group
                float groupReputation = 0f;
                int relevantTraits = 0;
                
                foreach (var score in entityReputation.reputationScores)
                {
                    // Only consider traits where at least one group member is a source
                    if (score.Value.sources.Any(s => group.members.Contains(s)))
                    {
                        groupReputation += score.Value.value;
                        relevantTraits++;
                    }
                }
                
                if (relevantTraits > 0)
                {
                    entityReputation.reputationPerGroup[group.id] = groupReputation / relevantTraits;
                }
            }
        }

        /// <summary>
        /// Creates a new relationship between two entities
        /// </summary>
        private RelationshipData CreateRelationship(string sourceId, string targetId)
        {
            return new RelationshipData
            {
                sourceId = sourceId,
                targetId = targetId,
                type = "acquaintance", // Default relationship type
                strength = 0f,         // Neutral
                trust = 20f,           // Low initial trust
                familiarity = 10f,     // Low initial familiarity
                history = new List<RelationshipHistoryEntry>(),
                hiddenAttributes = new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Checks and updates group cohesion after a relationship change
        /// </summary>
        private void CheckGroupCohesionUpdate(string entity1, string entity2, float change)
        {
            // Find groups that both entities are in
            var sharedGroups = _relationshipGraph.groups.Where(
                g => g.members.Contains(entity1) && g.members.Contains(entity2)).ToList();
                
            foreach (var group in sharedGroups)
            {
                // Major changes can affect overall group cohesion
                if (Mathf.Abs(change) > 20f)
                {
                    float cohesionChange = change * 0.1f;
                    group.cohesion = Mathf.Clamp(group.cohesion + cohesionChange, 0f, 100f);
                    
                    if (debugMode)
                    {
                        Debug.Log($"[RelationshipNetwork] Group {group.id} cohesion updated to {group.cohesion} " +
                                 $"due to relationship change between {entity1} and {entity2}");
                    }
                }
            }
        }
        
        #endregion

        #region Event Publishing

        /// <summary>
        /// Publishes a relationship change event to the event bus
        /// </summary>
        private void PublishRelationshipChangeEvent(string sourceId, string targetId, float oldStrength, float newStrength, string eventType)
        {
            var evt = new RelationshipChangeEvent
            {
                sourceEntityId = sourceId,
                targetEntityId = targetId,
                previousStrength = oldStrength,
                newStrength = newStrength,
                interactionType = eventType
            };
            
            CharacterEventBus.Instance.Publish(evt);
        }

        /// <summary>
        /// Publishes a reputation update event to the event bus
        /// </summary>
        private void PublishReputationUpdateEvent(string entityId, string traitType, float oldValue, float newValue)
        {
            var evt = new ReputationUpdateEvent
            {
                entityId = entityId,
                traitType = traitType,
                previousValue = oldValue,
                newValue = newValue
            };
            
            CharacterEventBus.Instance.Publish(evt);
        }

        /// <summary>
        /// Publishes an information shared event to the event bus
        /// </summary>
        private void PublishInformationSharedEvent(string senderId, string receiverId, InformationUnit information)
        {
            var evt = new InformationShareEvent
            {
                senderId = senderId,
                receiverId = receiverId,
                informationId = information.id,
                informationType = information.type
            };
            
            CharacterEventBus.Instance.Publish(evt);
        }

        /// <summary>
        /// Publishes a rumor created event to the event bus
        /// </summary>
        private void PublishRumorCreatedEvent(string originatorId, RumorData rumor)
        {
            var evt = new RumorEvent
            {
                originatorId = originatorId,
                rumorId = rumor.id,
                subjectId = rumor.subject,
                contentType = rumor.content
            };
            
            CharacterEventBus.Instance.Publish(evt);
        }

        /// <summary>
        /// Publishes a group membership change event to the event bus
        /// </summary>
        private void PublishGroupMembershipEvent(string groupId, string entityId, string action)
        {
            var evt = new GroupMembershipEvent
            {
                groupId = groupId,
                entityId = entityId,
                action = action
            };
            
            CharacterEventBus.Instance.Publish(evt);
        }

        /// <summary>
        /// Handles relationship change events from the event bus
        /// </summary>
        private void HandleRelationshipChangeEvent(PsychologyEvent evt)
        {
            if (evt is RelationshipChangeEvent relationshipEvent)
            {
                if (debugMode)
                {
                    Debug.Log($"[RelationshipNetwork] Received relationship change event between " +
                             $"{relationshipEvent.sourceEntityId} and {relationshipEvent.targetEntityId}");
                }
                
                // Additional logic can be added here if needed
            }
        }

        /// <summary>
        /// Handles reputation update events from the event bus
        /// </summary>
        private void HandleReputationUpdateEvent(PsychologyEvent evt)
        {
            if (evt is ReputationUpdateEvent reputationEvent)
            {
                if (debugMode)
                {
                    Debug.Log($"[RelationshipNetwork] Received reputation update event for " +
                             $"{reputationEvent.entityId} - Trait: {reputationEvent.traitType}");
                }
                
                // Additional logic can be added here if needed
            }
        }

        /// <summary>
        /// Handles information share events from the event bus
        /// </summary>
        private void HandleInformationShareEvent(PsychologyEvent evt)
        {
            if (evt is InformationShareEvent infoEvent)
            {
                if (debugMode)
                {
                    Debug.Log($"[RelationshipNetwork] Received information share event from " +
                             $"{infoEvent.senderId} to {infoEvent.receiverId}");
                }
                
                // Additional logic can be added here if needed
            }
        }

        /// <summary>
        /// Handles group change events from the event bus
        /// </summary>
        private void HandleGroupChangeEvent(PsychologyEvent evt)
        {
            if (evt is GroupMembershipEvent groupEvent)
            {
                if (debugMode)
                {
                    Debug.Log($"[RelationshipNetwork] Received group membership event for " +
                             $"{groupEvent.entityId} in group {groupEvent.groupId} - Action: {groupEvent.action}");
                }
                
                // Additional logic can be added here if needed
            }
        }
        
        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Core data structure for relationship graph
    /// </summary>
    [Serializable]
    public class RelationshipGraph
    {
        public List<EntityData> entities = new List<EntityData>();
        public List<RelationshipData> relationships = new List<RelationshipData>();
        public List<GroupData> groups = new List<GroupData>();
    }

    /// <summary>
    /// Data for an entity in the relationship network
    /// </summary>
    [Serializable]
    public class EntityData
    {
        public string id;
        public string type;
        public Vector3 position;
        public float influence = 1f;
    }

    /// <summary>
    /// Data for a relationship between two entities
    /// </summary>
    [Serializable]
    public class RelationshipData
    {
        public string sourceId;
        public string targetId;
        public string type;
        public float strength;
        public float trust;
        public float familiarity;
        public List<RelationshipHistoryEntry> history = new List<RelationshipHistoryEntry>();
        public Dictionary<string, object> hiddenAttributes = new Dictionary<string, object>();
    }

    /// <summary>
    /// Entry in relationship history
    /// </summary>
    [Serializable]
    public class RelationshipHistoryEntry
    {
        public string eventEntry;
        public float impact;
        public long timestamp;
    }

    /// <summary>
    /// Data for a group in the relationship network
    /// </summary>
    [Serializable]
    public class GroupData
    {
        public string id;
        public List<string> members = new List<string>();
        public float cohesion;
        public string purpose;
        public Dictionary<string, object> hierarchies = new Dictionary<string, object>();
    }

    /// <summary>
    /// Core data structure for reputation system
    /// </summary>
    [Serializable]
    public class ReputationSystem
    {
        public List<ReputationData> entities = new List<ReputationData>();
        public List<RumorData> rumors = new List<RumorData>();
    }

    /// <summary>
    /// Data for an entity's reputation
    /// </summary>
    [Serializable]
    public class ReputationData
    {
        public string id;
        public Dictionary<string, ReputationScore> reputationScores = new Dictionary<string, ReputationScore>();
        public float globalReputation;
        public Dictionary<string, object> reputationPerGroup = new Dictionary<string, object>();
    }

    /// <summary>
    /// Score for a specific reputation trait
    /// </summary>
    [Serializable]
    public class ReputationScore
    {
        public float value;
        public float confidence;
        public List<string> sources = new List<string>();
    }

    /// <summary>
    /// Data for a rumor in the reputation system
    /// </summary>
    [Serializable]
    public class RumorData
    {
        public string id;
        public string subject;
        public string content;
        public float truth;
        public Dictionary<string, object> spreadPattern = new Dictionary<string, object>();
        public List<string> knownBy = new List<string>();
        public float currentRelevance;
    }

    /// <summary>
    /// Core data structure for information propagation model
    /// </summary>
    [Serializable]
    public class InformationPropagationModel
    {
        public List<InformationUnit> informationUnits = new List<InformationUnit>();
        public List<CommunicationChannel> communicationChannels = new List<CommunicationChannel>();
    }

    /// <summary>
    /// Data for an information unit
    /// </summary>
    [Serializable]
    public class InformationUnit
    {
        public string id;
        public string type;
        public Dictionary<string, object> content = new Dictionary<string, object>();
        public float sensitivity;
        public string originatorId;
        public Dictionary<string, InformationKnowledge> knownBy = new Dictionary<string, InformationKnowledge>();
        public InformationSpreadFactors spreadFactors = new InformationSpreadFactors();
    }

    /// <summary>
    /// Knowledge state of an entity about information
    /// </summary>
    [Serializable]
    public class InformationKnowledge
    {
        public float accuracy;
        public float detail;
        public long acquired;
    }

    /// <summary>
    /// Factors affecting information spread
    /// </summary>
    [Serializable]
    public class InformationSpreadFactors
    {
        public float interestLevel;
        public float controversyLevel;
        public float believability;
    }

    /// <summary>
    /// Data for a communication channel
    /// </summary>
    [Serializable]
    public class CommunicationChannel
    {
        public string type;
        public List<string> participatingEntities = new List<string>();
        public float bandwidth;
        public float noiseLevel;
        public Dictionary<string, object> accessRestrictions = new Dictionary<string, object>();
    }

    #endregion

    #region Event Definitions

    /// <summary>
    /// Event for relationship changes
    /// </summary>
    [Serializable]
    public class RelationshipChangeEvent : PsychologyEvent
    {
        public string sourceEntityId;
        public string targetEntityId;
        public float previousStrength;
        public float newStrength;
        public string interactionType;
        
        public RelationshipChangeEvent()
        {
            eventType = "relationship_change";
        }
    }

    /// <summary>
    /// Event for reputation updates
    /// </summary>
    [Serializable]
    public class ReputationUpdateEvent : PsychologyEvent
    {
        public string entityId;
        public string traitType;
        public float previousValue;
        public float newValue;
        
        public ReputationUpdateEvent()
        {
            eventType = "reputation_update";
        }
    }

    /// <summary>
    /// Event for information sharing
    /// </summary>
    [Serializable]
    public class InformationShareEvent : PsychologyEvent
    {
        public string senderId;
        public string receiverId;
        public string informationId;
        public string informationType;
        
        public InformationShareEvent()
        {
            eventType = "information_share";
        }
    }

    /// <summary>
    /// Event for rumors
    /// </summary>
    [Serializable]
    public class RumorEvent : PsychologyEvent
    {
        public string originatorId;
        public string rumorId;
        public string subjectId;
        public string contentType;
        
        public RumorEvent()
        {
            eventType = "rumor_created";
        }
    }

    /// <summary>
    /// Event for group membership changes
    /// </summary>
    [Serializable]
    public class GroupMembershipEvent : PsychologyEvent
    {
        public string groupId;
        public string entityId;
        public string action;
        
        public GroupMembershipEvent()
        {
            eventType = "group_change";
        }
    }

    #endregion

    #region Result Classes

    /// <summary>
    /// Result of a relationship change operation
    /// </summary>
    [Serializable]
    public class RelationshipChangeResult
    {
        public string sourceId;
        public string targetId;
        public float previousStrength;
        public float newStrength;
        public float changeAmount;
        public string interactionType;
    }

    /// <summary>
    /// Result of a network query
    /// </summary>
    [Serializable]
    public class NetworkQueryResult
    {
        public string queryType;
        public Dictionary<string, object> data = new Dictionary<string, object>();
    }

    #endregion

    /// <summary>
    /// Interface for the Relationship System
    /// </summary>
    public interface IRelationshipSystem : ICharacterSubsystem
    {
        // Periodic updates to the relationship graph
        void UpdateNetwork(float deltaTime);
        
        // Handle specific relationship change events
        RelationshipChangeResult ProcessRelationshipEvent(PsychologyEvent evt);
        
        // Read-only query of the current network state
        NetworkQueryResult QueryNetwork(StateQuery queryParams);
    }
}