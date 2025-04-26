using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Public API for the Relationship Network System
    /// Provides a clean interface for other systems to interact with relationships
    /// </summary>
    public class RelationshipNetworkAPI : MonoBehaviour
    {
        private static RelationshipNetworkAPI _instance;
        public static RelationshipNetworkAPI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RelationshipNetworkAPI>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RelationshipNetworkAPI");
                        _instance = go.AddComponent<RelationshipNetworkAPI>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private RelationshipNetwork networkSystem;
        [SerializeField] private bool logApiCalls = false;

        private void Awake()
        {
            if (networkSystem == null)
            {
                networkSystem = RelationshipNetwork.Instance;
            }
        }

        #region Public API Methods

        /// <summary>
        /// Gets the relationship status between two entities
        /// </summary>
        /// <param name="entityId1">First entity ID</param>
        /// <param name="entityId2">Second entity ID</param>
        /// <returns>Relationship data or null if no relationship exists</returns>
        public RelationshipData GetRelationshipStatus(string entityId1, string entityId2)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Getting relationship between {entityId1} and {entityId2}");
            }
            
            return networkSystem.GetRelationshipStatus(entityId1, entityId2);
        }

        /// <summary>
        /// Updates the relationship between two entities
        /// </summary>
        /// <param name="sourceId">Source entity ID</param>
        /// <param name="targetId">Target entity ID</param>
        /// <param name="interactionType">Type of interaction</param>
        /// <param name="intensity">Intensity of interaction (-100 to 100)</param>
        /// <returns>Result of the relationship change</returns>
        public RelationshipChangeResult UpdateRelationship(string sourceId, string targetId, string interactionType, float intensity)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Updating relationship between {sourceId} and {targetId} - {interactionType} ({intensity})");
            }
            
            return networkSystem.UpdateRelationship(sourceId, targetId, interactionType, intensity);
        }

        /// <summary>
        /// Gets an entity's reputation from a specific perspective
        /// </summary>
        /// <param name="entityId">Entity ID</param>
        /// <param name="perspective">Perspective entity ID or group ID (null for global)</param>
        /// <returns>Reputation data</returns>
        public ReputationData GetReputation(string entityId, string perspective = null)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Getting reputation for {entityId}" + 
                         (perspective != null ? $" from perspective of {perspective}" : ""));
            }
            
            return networkSystem.GetReputation(entityId, perspective);
        }

        /// <summary>
        /// Creates and propagates a rumor about an entity
        /// </summary>
        /// <param name="originatorId">Entity starting the rumor</param>
        /// <param name="subjectId">Entity the rumor is about</param>
        /// <param name="contentType">Type of rumor content</param>
        /// <param name="truthValue">Truth value (0-100)</param>
        /// <returns>Created rumor data</returns>
        public RumorData CreateAndPropagateRumor(string originatorId, string subjectId, string contentType, float truthValue)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Creating rumor from {originatorId} about {subjectId} - Truth: {truthValue}%");
            }
            
            return networkSystem.CreateAndPropagateRumor(originatorId, subjectId, contentType, truthValue);
        }

        /// <summary>
        /// Shares information between two entities
        /// </summary>
        /// <param name="senderId">Sender entity ID</param>
        /// <param name="receiverId">Receiver entity ID</param>
        /// <param name="informationId">Information ID</param>
        /// <param name="channelType">Communication channel type</param>
        /// <returns>True if information was shared successfully</returns>
        public bool ShareInformation(string senderId, string receiverId, string informationId, string channelType)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Sharing information {informationId} from {senderId} to {receiverId} via {channelType}");
            }
            
            return networkSystem.ShareInformation(senderId, receiverId, informationId, channelType);
        }

        /// <summary>
        /// Checks if an entity knows specific information and to what degree
        /// </summary>
        /// <param name="entityId">Entity ID</param>
        /// <param name="informationId">Information ID</param>
        /// <returns>Knowledge data or null if entity doesn't know</returns>
        public InformationKnowledge CheckKnowledgeState(string entityId, string informationId)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Checking knowledge state of {entityId} for information {informationId}");
            }
            
            return networkSystem.CheckKnowledgeState(entityId, informationId);
        }

        /// <summary>
        /// Manages group membership (add, remove, change role)
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="entityId">Entity ID</param>
        /// <param name="action">Action (add, remove, promote, demote)</param>
        /// <returns>True if action was successful</returns>
        public bool ManageGroupMembership(string groupId, string entityId, string action)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Managing group membership - {action} {entityId} to/from {groupId}");
            }
            
            return networkSystem.ManageGroupMembership(groupId, entityId, action);
        }

        /// <summary>
        /// Adds a new entity to the relationship network
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <param name="type">Entity type</param>
        /// <param name="position">Position in the network</param>
        /// <param name="influence">Initial influence</param>
        public void AddEntity(string id, string type, Vector3 position, float influence = 1f)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Adding entity {id} of type {type}");
            }
            
            networkSystem.AddEntity(id, type, position, influence);
        }

        /// <summary>
        /// Creates a new group in the relationship network
        /// </summary>
        /// <param name="id">Group ID</param>
        /// <param name="purpose">Group purpose</param>
        /// <param name="cohesion">Initial cohesion (0-100)</param>
        /// <returns>Created group data</returns>
        public GroupData CreateGroup(string id, string purpose, float cohesion = 50f)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Creating group {id} with purpose {purpose}");
            }
            
            return networkSystem.CreateGroup(id, purpose, cohesion);
        }

        /// <summary>
        /// Creates a new information unit in the network
        /// </summary>
        /// <param name="id">Information ID</param>
        /// <param name="type">Information type</param>
        /// <param name="content">Information content</param>
        /// <param name="sensitivity">Sensitivity level (0-1)</param>
        /// <param name="originatorId">Entity that created the information</param>
        /// <returns>Created information unit</returns>
        public InformationUnit CreateInformationUnit(string id, string type, Dictionary<string, object> content, 
                                                   float sensitivity, string originatorId)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Creating information unit {id} of type {type} from {originatorId}");
            }
            
            return networkSystem.CreateInformationUnit(id, type, content, sensitivity, originatorId);
        }

        /// <summary>
        /// Creates a communication channel between entities
        /// </summary>
        /// <param name="type">Channel type</param>
        /// <param name="participatingEntities">Participating entity IDs</param>
        /// <param name="bandwidth">Bandwidth (0-1)</param>
        /// <param name="noiseLevel">Noise level (0-1)</param>
        /// <returns>Created communication channel</returns>
        public CommunicationChannel CreateCommunicationChannel(string type, List<string> participatingEntities, 
                                                              float bandwidth, float noiseLevel)
        {
            if (logApiCalls)
            {
                Debug.Log($"[RelationshipAPI] Creating communication channel of type {type}");
            }
            
            return networkSystem.CreateCommunicationChannel(type, participatingEntities, bandwidth, noiseLevel);
        }

        /// <summary>
        /// Gets all entities in the relationship network
        /// </summary>
        /// <returns>List of all entities</returns>
        public List<EntityData> GetAllEntities()
        {
            // Get all entities from the network system
            return networkSystem.GetAllEntities();
        }

        #endregion
    }

    /// <summary>
    /// Unity event handler for relationship network events
    /// </summary>
    [Serializable]
    public class RelationshipNetworkUIEvents : MonoBehaviour
    {
        [Header("Unity Events")]
        public UnityEngine.Events.UnityEvent<string, string, float> onRelationshipChanged;
        public UnityEngine.Events.UnityEvent<string, float> onReputationChanged;
        public UnityEngine.Events.UnityEvent<string, string, string> onInformationShared;
        public UnityEngine.Events.UnityEvent<string, string, string> onRumorCreated;
        public UnityEngine.Events.UnityEvent<string, string, string> onGroupMembershipChanged;
        
        private void Start()
        {
            // Subscribe to events from the central event bus
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            UnsubscribeFromEvents();
        }
        
        private void SubscribeToEvents()
        {
            var eventBus = CharacterEventBus.Instance;
            
            eventBus.Subscribe("relationship_change", HandleRelationshipChangeEvent);
            eventBus.Subscribe("reputation_update", HandleReputationUpdateEvent);
            eventBus.Subscribe("information_share", HandleInformationShareEvent);
            eventBus.Subscribe("rumor_created", HandleRumorEvent);
            eventBus.Subscribe("group_change", HandleGroupMembershipEvent);
        }
        
        private void UnsubscribeFromEvents()
        {
            var eventBus = CharacterEventBus.Instance;
            
            eventBus.Unsubscribe("relationship_change", HandleRelationshipChangeEvent);
            eventBus.Unsubscribe("reputation_update", HandleReputationUpdateEvent);
            eventBus.Unsubscribe("information_share", HandleInformationShareEvent);
            eventBus.Unsubscribe("rumor_created", HandleRumorEvent);
            eventBus.Unsubscribe("group_change", HandleGroupMembershipEvent);
        }
        
        private void HandleRelationshipChangeEvent(PsychologyEvent evt)
        {
            if (evt is RelationshipChangeEvent relationshipEvent)
            {
                onRelationshipChanged?.Invoke(
                    relationshipEvent.sourceEntityId, 
                    relationshipEvent.targetEntityId, 
                    relationshipEvent.newStrength);
            }
        }
        
        private void HandleReputationUpdateEvent(PsychologyEvent evt)
        {
            if (evt is ReputationUpdateEvent reputationEvent)
            {
                onReputationChanged?.Invoke(
                    reputationEvent.entityId, 
                    reputationEvent.newValue);
            }
        }
        
        private void HandleInformationShareEvent(PsychologyEvent evt)
        {
            if (evt is InformationShareEvent infoEvent)
            {
                onInformationShared?.Invoke(
                    infoEvent.senderId, 
                    infoEvent.receiverId, 
                    infoEvent.informationType);
            }
        }
        
        private void HandleRumorEvent(PsychologyEvent evt)
        {
            if (evt is RumorEvent rumorEvent)
            {
                onRumorCreated?.Invoke(
                    rumorEvent.originatorId, 
                    rumorEvent.subjectId, 
                    rumorEvent.contentType);
            }
        }
        
        private void HandleGroupMembershipEvent(PsychologyEvent evt)
        {
            if (evt is GroupMembershipEvent groupEvent)
            {
                onGroupMembershipChanged?.Invoke(
                    groupEvent.groupId, 
                    groupEvent.entityId, 
                    groupEvent.action);
            }
        }
    }
}