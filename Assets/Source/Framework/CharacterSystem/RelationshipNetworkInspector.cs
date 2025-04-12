using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace CharacterSystem
{
    /// <summary>
    /// Editor window for inspecting and modifying the relationship network
    /// </summary>
    public class RelationshipNetworkInspector : EditorWindow
    {
        private enum TabType
        {
            Entities,
            Relationships,
            Groups,
            Information,
            Rumors
        }
        
        private TabType activeTab = TabType.Entities;
        private Vector2 scrollPosition;
        private RelationshipNetwork networkSystem;
        
        // Filter settings
        private string entityFilterText = "";
        private string relationshipFilterText = "";
        private string groupFilterText = "";
        private string informationFilterText = "";
        
        // Edit settings
        private string sourceEntityId = "";
        private string targetEntityId = "";
        private string interactionType = "friendly_chat";
        private float interactionIntensity = 10f;
        
        private string newEntityId = "";
        private string newEntityType = "NPC";
        private float newEntityInfluence = 1f;
        
        private string newGroupId = "";
        private string newGroupPurpose = "";
        private float newGroupCohesion = 50f;
        
        private string rumorOriginatorId = "";
        private string rumorSubjectId = "";
        private string rumorContent = "";
        private float rumorTruthValue = 50f;
        
        private string infoId = "";
        private string infoType = "";
        private float infoSensitivity = 0.5f;
        private string infoOriginatorId = "";
        
        [MenuItem("Window/Character System/Relationship Network Inspector")]
        public static void ShowWindow()
        {
            GetWindow<RelationshipNetworkInspector>("Relationship Network");
        }
        
        private void OnEnable()
        {
            // Find the network system reference
            networkSystem = FindFirstObjectByType<RelationshipNetwork>();
        }
        
        private void OnGUI()
        {
            // Check if we have a reference to the network system
            if (networkSystem == null)
            {
                networkSystem = FindFirstObjectByType<RelationshipNetwork>();
                if (networkSystem == null)
                {
                    EditorGUILayout.HelpBox("No RelationshipNetwork found in the scene!", UnityEditor.MessageType.Error);
                    if (GUILayout.Button("Create Relationship Network"))
                    {
                        GameObject go = new GameObject("RelationshipNetwork");
                        networkSystem = go.AddComponent<RelationshipNetwork>();
                    }
                    return;
                }
            }
            
            // Draw tabs
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(activeTab == TabType.Entities, "Entities", EditorStyles.toolbarButton))
                activeTab = TabType.Entities;
                
            if (GUILayout.Toggle(activeTab == TabType.Relationships, "Relationships", EditorStyles.toolbarButton))
                activeTab = TabType.Relationships;
                
            if (GUILayout.Toggle(activeTab == TabType.Groups, "Groups", EditorStyles.toolbarButton))
                activeTab = TabType.Groups;
                
            if (GUILayout.Toggle(activeTab == TabType.Information, "Information", EditorStyles.toolbarButton))
                activeTab = TabType.Information;
                
            if (GUILayout.Toggle(activeTab == TabType.Rumors, "Rumors", EditorStyles.toolbarButton))
                activeTab = TabType.Rumors;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Start the scroll view
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Draw the active tab content
            switch (activeTab)
            {
                case TabType.Entities:
                    DrawEntitiesTab();
                    break;
                case TabType.Relationships:
                    DrawRelationshipsTab();
                    break;
                case TabType.Groups:
                    DrawGroupsTab();
                    break;
                case TabType.Information:
                    DrawInformationTab();
                    break;
                case TabType.Rumors:
                    DrawRumorsTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        #region Tab Drawing Methods
        
        private void DrawEntitiesTab()
        {
            EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
            
            // Filter
            entityFilterText = EditorGUILayout.TextField("Filter", entityFilterText);
            
            // Add new entity section
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Add New Entity", EditorStyles.boldLabel);
            
            newEntityId = EditorGUILayout.TextField("Entity ID", newEntityId);
            newEntityType = EditorGUILayout.TextField("Entity Type", newEntityType);
            newEntityInfluence = EditorGUILayout.Slider("Influence", newEntityInfluence, 0.1f, 5f);
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newEntityId));
            if (GUILayout.Button("Add Entity"))
            {
                // Call the NetworkSystem via reflection to add the entity
                Type type = networkSystem.GetType();
                var method = type.GetMethod("AddEntity");
                
                if (method != null)
                {
                    method.Invoke(networkSystem, new object[] { 
                        newEntityId, 
                        newEntityType,
                        Vector3.zero,
                        newEntityInfluence
                    });
                    
                    // Clear the fields
                    newEntityId = "";
                }
                else
                {
                    Debug.LogError("Failed to find AddEntity method");
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            // Entity list
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Entity List", EditorStyles.boldLabel);
            
            // Access entities through reflection as they're private
            var fieldInfo = typeof(RelationshipNetwork).GetField("_relationshipGraph", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (fieldInfo != null)
            {
                var graph = fieldInfo.GetValue(networkSystem) as RelationshipGraph;
                
                if (graph != null && graph.entities != null)
                {
                    // Filter entities
                    var filteredEntities = graph.entities;
                    if (!string.IsNullOrEmpty(entityFilterText))
                    {
                        filteredEntities = graph.entities.Where(e => 
                            e.id.ToLower().Contains(entityFilterText.ToLower()) || 
                            e.type.ToLower().Contains(entityFilterText.ToLower())).ToList();
                    }
                    
                    // Display each entity
                    foreach (var entity in filteredEntities)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        EditorGUILayout.LabelField($"ID: {entity.id}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Type: {entity.type}");
                        EditorGUILayout.LabelField($"Influence: {entity.influence}");
                        
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space();
                    }
                }
            }
        }
        
        private void DrawRelationshipsTab()
        {
            EditorGUILayout.LabelField("Relationships", EditorStyles.boldLabel);
            
            // Filter
            relationshipFilterText = EditorGUILayout.TextField("Filter", relationshipFilterText);
            
            // Access the relationship graph through reflection
            var fieldInfo = typeof(RelationshipNetwork).GetField("_relationshipGraph", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (fieldInfo == null)
                return;
                
            var graph = fieldInfo.GetValue(networkSystem) as RelationshipGraph;
            if (graph == null || graph.relationships == null)
                return;
            
            // Update relationship section
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Update Relationship", EditorStyles.boldLabel);
            
            // Entity selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Source Entity");
            if (EditorGUILayout.DropdownButton(new GUIContent(sourceEntityId), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var entity in graph.entities)
                {
                    string id = entity.id;
                    menu.AddItem(new GUIContent(id), sourceEntityId == id, () => {
                        sourceEntityId = id;
                    });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Target Entity");
            if (EditorGUILayout.DropdownButton(new GUIContent(targetEntityId), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var entity in graph.entities)
                {
                    if (entity.id != sourceEntityId)
                    {
                        string id = entity.id;
                        menu.AddItem(new GUIContent(id), targetEntityId == id, () => {
                            targetEntityId = id;
                        });
                    }
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            // Interaction type dropdown
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Interaction Type");
            if (EditorGUILayout.DropdownButton(new GUIContent(interactionType), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                string[] interactionTypes = {
                    "friendly_chat", "deep_conversation", "argument", "fight",
                    "assistance", "betrayal", "gift", "insult", "compliment", "criticism"
                };
                
                foreach (var type in interactionTypes)
                {
                    menu.AddItem(new GUIContent(type), interactionType == type, () => {
                        interactionType = type;
                    });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            // Intensity slider
            interactionIntensity = EditorGUILayout.Slider("Intensity", interactionIntensity, -100f, 100f);
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(sourceEntityId) || string.IsNullOrEmpty(targetEntityId));
            if (GUILayout.Button("Update Relationship"))
            {
                var method = typeof(RelationshipNetwork).GetMethod("UpdateRelationship");
                
                if (method != null)
                {
                    method.Invoke(networkSystem, new object[] { 
                        sourceEntityId, 
                        targetEntityId,
                        interactionType,
                        interactionIntensity
                    });
                }
                else
                {
                    Debug.LogError("Failed to find UpdateRelationship method");
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            // Relationship list
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Relationship List", EditorStyles.boldLabel);
            
            // Filter relationships
            var filteredRelationships = graph.relationships;
            if (!string.IsNullOrEmpty(relationshipFilterText))
            {
                filteredRelationships = graph.relationships.Where(r => 
                    r.sourceId.ToLower().Contains(relationshipFilterText.ToLower()) || 
                    r.targetId.ToLower().Contains(relationshipFilterText.ToLower()) ||
                    r.type.ToLower().Contains(relationshipFilterText.ToLower())).ToList();
            }
            
            // Display each relationship
            foreach (var relationship in filteredRelationships)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField($"{relationship.sourceId} → {relationship.targetId}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Type: {relationship.type}");
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Strength:", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{relationship.strength}", GUILayout.Width(50));
                Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(20), GUILayout.ExpandWidth(true));
                DrawStrengthBar(rect, relationship.strength);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField($"Trust: {relationship.trust}");
                EditorGUILayout.LabelField($"Familiarity: {relationship.familiarity}");
                
                if (relationship.history.Count > 0)
                {
                    EditorGUILayout.LabelField("Recent History:", EditorStyles.boldLabel);
                    
                    // Show at most 3 most recent events
                    foreach (var entry in relationship.history.OrderByDescending(h => h.timestamp).Take(3))
                    {
                        string timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                            .AddMilliseconds(entry.timestamp)
                            .ToLocalTime()
                            .ToString("yyyy-MM-dd HH:mm:ss");
                            
                        EditorGUILayout.LabelField($"{timestamp}: {entry.eventEntry} (Impact: {entry.impact})");
                    }
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
        
        private void DrawGroupsTab()
        {
            EditorGUILayout.LabelField("Groups", EditorStyles.boldLabel);
            
            // Filter
            groupFilterText = EditorGUILayout.TextField("Filter", groupFilterText);
            
            // Access the relationship graph through reflection
            var fieldInfo = typeof(RelationshipNetwork).GetField("_relationshipGraph", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (fieldInfo == null)
                return;
                
            var graph = fieldInfo.GetValue(networkSystem) as RelationshipGraph;
            if (graph == null)
                return;
            
            // Create new group section
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Create New Group", EditorStyles.boldLabel);
            
            newGroupId = EditorGUILayout.TextField("Group ID", newGroupId);
            newGroupPurpose = EditorGUILayout.TextField("Purpose", newGroupPurpose);
            newGroupCohesion = EditorGUILayout.Slider("Initial Cohesion", newGroupCohesion, 0f, 100f);
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newGroupId) || string.IsNullOrEmpty(newGroupPurpose));
            if (GUILayout.Button("Create Group"))
            {
                var method = typeof(RelationshipNetwork).GetMethod("CreateGroup");
                
                if (method != null)
                {
                    method.Invoke(networkSystem, new object[] { 
                        newGroupId, 
                        newGroupPurpose,
                        newGroupCohesion
                    });
                    
                    // Clear the fields
                    newGroupId = "";
                    newGroupPurpose = "";
                }
                else
                {
                    Debug.LogError("Failed to find CreateGroup method");
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            // Manage group membership section
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Manage Group Membership", EditorStyles.boldLabel);
            
            string selectedGroupId = "";
            string selectedEntityId = "";
            string selectedAction = "add";
            
            // Group selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Group");
            if (EditorGUILayout.DropdownButton(new GUIContent(selectedGroupId), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var group in graph.groups)
                {
                    string id = group.id;
                    menu.AddItem(new GUIContent(id), selectedGroupId == id, () => {
                        selectedGroupId = id;
                    });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            // Entity selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Entity");
            if (EditorGUILayout.DropdownButton(new GUIContent(selectedEntityId), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var entity in graph.entities)
                {
                    string id = entity.id;
                    menu.AddItem(new GUIContent(id), selectedEntityId == id, () => {
                        selectedEntityId = id;
                    });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            // Action selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Action");
            if (EditorGUILayout.DropdownButton(new GUIContent(selectedAction), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                string[] actions = { "add", "remove", "promote", "demote" };
                
                foreach (var action in actions)
                {
                    menu.AddItem(new GUIContent(action), selectedAction == action, () => {
                        selectedAction = action;
                    });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(selectedGroupId) || string.IsNullOrEmpty(selectedEntityId));
            if (GUILayout.Button("Apply Change"))
            {
                var method = typeof(RelationshipNetwork).GetMethod("ManageGroupMembership");
                
                if (method != null)
                {
                    method.Invoke(networkSystem, new object[] { 
                        selectedGroupId, 
                        selectedEntityId,
                        selectedAction
                    });
                }
                else
                {
                    Debug.LogError("Failed to find ManageGroupMembership method");
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            // Group list
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Group List", EditorStyles.boldLabel);
            
            if (graph.groups != null)
            {
                // Filter groups
                var filteredGroups = graph.groups;
                if (!string.IsNullOrEmpty(groupFilterText))
                {
                    filteredGroups = graph.groups.Where(g => 
                        g.id.ToLower().Contains(groupFilterText.ToLower()) || 
                        g.purpose.ToLower().Contains(groupFilterText.ToLower())).ToList();
                }
                
                // Display each group
                foreach (var group in filteredGroups)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField($"ID: {group.id}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Purpose: {group.purpose}");
                    EditorGUILayout.LabelField($"Cohesion: {group.cohesion}");
                    EditorGUILayout.LabelField($"Members: {group.members.Count}");
                    
                    EditorGUI.indentLevel++;
                    foreach (var memberId in group.members)
                    {
                        EditorGUILayout.LabelField(memberId);
                    }
                    EditorGUI.indentLevel--;
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
        }
        
        private void DrawInformationTab()
        {
            EditorGUILayout.LabelField("Information Units", EditorStyles.boldLabel);
            
            // Filter
            informationFilterText = EditorGUILayout.TextField("Filter", informationFilterText);
            
            // Access the information model through reflection
            var fieldInfo = typeof(RelationshipNetwork).GetField("_informationModel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (fieldInfo == null)
                return;
                
            var infoModel = fieldInfo.GetValue(networkSystem) as InformationPropagationModel;
            if (infoModel == null)
                return;
            
            // Create new information section
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Create New Information", EditorStyles.boldLabel);
            
            infoId = EditorGUILayout.TextField("Information ID", infoId);
            infoType = EditorGUILayout.TextField("Information Type", infoType);
            infoSensitivity = EditorGUILayout.Slider("Sensitivity", infoSensitivity, 0f, 1f);
            
            // Entity selection for originator
            var graphField = typeof(RelationshipNetwork).GetField("_relationshipGraph", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            var graph = graphField != null ? graphField.GetValue(networkSystem) as RelationshipGraph : null;
            
            if (graph != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Originator");
                if (EditorGUILayout.DropdownButton(new GUIContent(infoOriginatorId), FocusType.Keyboard))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (var entity in graph.entities)
                    {
                        string id = entity.id;
                        menu.AddItem(new GUIContent(id), infoOriginatorId == id, () => {
                            infoOriginatorId = id;
                        });
                    }
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(infoId) || 
                                        string.IsNullOrEmpty(infoType) || 
                                        string.IsNullOrEmpty(infoOriginatorId));
            if (GUILayout.Button("Create Information"))
            {
                var method = typeof(RelationshipNetwork).GetMethod("CreateInformationUnit");
                
                if (method != null)
                {
                    method.Invoke(networkSystem, new object[] { 
                        infoId, 
                        infoType,
                        new Dictionary<string, object>(),
                        infoSensitivity,
                        infoOriginatorId
                    });
                    
                    // Clear the fields
                    infoId = "";
                    infoType = "";
                }
                else
                {
                    Debug.LogError("Failed to find CreateInformationUnit method");
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            // Share information section
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Share Information", EditorStyles.boldLabel);
            
            string selectedSenderId = "";
            string selectedReceiverId = "";
            string selectedInfoId = "";
            string selectedChannelType = "direct_conversation";
            
            // Sender selection
            if (graph != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Sender");
                if (EditorGUILayout.DropdownButton(new GUIContent(selectedSenderId), FocusType.Keyboard))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (var entity in graph.entities)
                    {
                        string id = entity.id;
                        menu.AddItem(new GUIContent(id), selectedSenderId == id, () => {
                            selectedSenderId = id;
                        });
                    }
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();
                
                // Receiver selection
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Receiver");
                if (EditorGUILayout.DropdownButton(new GUIContent(selectedReceiverId), FocusType.Keyboard))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (var entity in graph.entities)
                    {
                        if (entity.id != selectedSenderId)
                        {
                            string id = entity.id;
                            menu.AddItem(new GUIContent(id), selectedReceiverId == id, () => {
                                selectedReceiverId = id;
                            });
                        }
                    }
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // Information selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Information");
            if (EditorGUILayout.DropdownButton(new GUIContent(selectedInfoId), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var info in infoModel.informationUnits)
                {
                    string id = info.id;
                    menu.AddItem(new GUIContent($"{id} ({info.type})"), selectedInfoId == id, () => {
                        selectedInfoId = id;
                    });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            // Channel type selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Channel Type");
            if (EditorGUILayout.DropdownButton(new GUIContent(selectedChannelType), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                string[] channelTypes = { "direct_conversation", "group_meeting", "media", "gossip", "formal_announcement" };
                
                foreach (var type in channelTypes)
                {
                    menu.AddItem(new GUIContent(type), selectedChannelType == type, () => {
                        selectedChannelType = type;
                    });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(selectedSenderId) || 
                                        string.IsNullOrEmpty(selectedReceiverId) || 
                                        string.IsNullOrEmpty(selectedInfoId));
            if (GUILayout.Button("Share Information"))
            {
                var method = typeof(RelationshipNetwork).GetMethod("ShareInformation");
                
                if (method != null)
                {
                    method.Invoke(networkSystem, new object[] { 
                        selectedSenderId, 
                        selectedReceiverId,
                        selectedInfoId,
                        selectedChannelType
                    });
                }
                else
                {
                    Debug.LogError("Failed to find ShareInformation method");
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            // Information list
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Information List", EditorStyles.boldLabel);
            
            if (infoModel.informationUnits != null)
            {
                // Filter information units
                var filteredInfoUnits = infoModel.informationUnits;
                if (!string.IsNullOrEmpty(informationFilterText))
                {
                    filteredInfoUnits = infoModel.informationUnits.Where(i => 
                        i.id.ToLower().Contains(informationFilterText.ToLower()) || 
                        i.type.ToLower().Contains(informationFilterText.ToLower()) ||
                        i.originatorId.ToLower().Contains(informationFilterText.ToLower())).ToList();
                }
                
                // Display each information unit
                foreach (var info in filteredInfoUnits)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField($"ID: {info.id}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Type: {info.type}");
                    EditorGUILayout.LabelField($"Originator: {info.originatorId}");
                    EditorGUILayout.LabelField($"Sensitivity: {info.sensitivity}");
                    EditorGUILayout.LabelField($"Known by: {info.knownBy.Count} entities");
                    
                    if (info.knownBy.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var knower in info.knownBy)
                        {
                            EditorGUILayout.LabelField($"{knower.Key} - Accuracy: {knower.Value.accuracy:F2}, Detail: {knower.Value.detail:F2}");
                        }
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
        }
        
        private void DrawRumorsTab()
        {
            EditorGUILayout.LabelField("Rumors", EditorStyles.boldLabel);
            
            // Access the reputation system through reflection
            var fieldInfo = typeof(RelationshipNetwork).GetField("_reputationSystem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (fieldInfo == null)
                return;
                
            var repSystem = fieldInfo.GetValue(networkSystem) as ReputationSystem;
            if (repSystem == null)
                return;
            
            // Create new rumor section
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Create New Rumor", EditorStyles.boldLabel);
            
            // Entity selection for originator and subject
            var graphField = typeof(RelationshipNetwork).GetField("_relationshipGraph", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            var graph = graphField != null ? graphField.GetValue(networkSystem) as RelationshipGraph : null;
            
            if (graph != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Originator");
                if (EditorGUILayout.DropdownButton(new GUIContent(rumorOriginatorId), FocusType.Keyboard))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (var entity in graph.entities)
                    {
                        string id = entity.id;
                        menu.AddItem(new GUIContent(id), rumorOriginatorId == id, () => {
                            rumorOriginatorId = id;
                        });
                    }
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Subject");
                if (EditorGUILayout.DropdownButton(new GUIContent(rumorSubjectId), FocusType.Keyboard))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (var entity in graph.entities)
                    {
                        if (entity.id != rumorOriginatorId)
                        {
                            string id = entity.id;
                            menu.AddItem(new GUIContent(id), rumorSubjectId == id, () => {
                                rumorSubjectId = id;
                            });
                        }
                    }
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // Content selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Content");
            if (EditorGUILayout.DropdownButton(new GUIContent(rumorContent), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                string[] contentTypes = { 
                    "is_dishonest", "is_aggressive", "is_generous", "has_hidden_wealth", 
                    "has_secret_relationship", "is_planning_something", "betrayed_someone" 
                };
                
                foreach (var type in contentTypes)
                {
                    menu.AddItem(new GUIContent(type), rumorContent == type, () => {
                        rumorContent = type;
                    });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            // Truth value
            rumorTruthValue = EditorGUILayout.Slider("Truth Value", rumorTruthValue, 0f, 100f);
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(rumorOriginatorId) || 
                                        string.IsNullOrEmpty(rumorSubjectId) || 
                                        string.IsNullOrEmpty(rumorContent));
            if (GUILayout.Button("Create Rumor"))
            {
                var method = typeof(RelationshipNetwork).GetMethod("CreateAndPropagateRumor");
                
                if (method != null)
                {
                    method.Invoke(networkSystem, new object[] { 
                        rumorOriginatorId, 
                        rumorSubjectId,
                        rumorContent,
                        rumorTruthValue
                    });
                }
                else
                {
                    Debug.LogError("Failed to find CreateAndPropagateRumor method");
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            // Rumor list
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active Rumors", EditorStyles.boldLabel);
            
            if (repSystem.rumors != null)
            {
                // Display each rumor
                foreach (var rumor in repSystem.rumors.OrderByDescending(r => r.currentRelevance))
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField($"Subject: {rumor.subject}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Content: {rumor.content}");
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Relevance:", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{rumor.currentRelevance:F1}", GUILayout.Width(50));
                    Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(20), GUILayout.ExpandWidth(true));
                    DrawRelevanceBar(rect, rumor.currentRelevance);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.LabelField($"Truth Value: {rumor.truth:F1}%");
                    EditorGUILayout.LabelField($"Known by: {rumor.knownBy.Count} entities");
                    
                    if (rumor.knownBy.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var knower in rumor.knownBy)
                        {
                            EditorGUILayout.LabelField(knower);
                        }
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
            
            // Reputation summary
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reputation Summary", EditorStyles.boldLabel);
            
            if (repSystem.entities != null)
            {
                foreach (var entity in repSystem.entities)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField($"Entity: {entity.id}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Global Reputation: {entity.globalReputation:F1}");
                    
                    if (entity.reputationScores.Count > 0)
                    {
                        EditorGUILayout.LabelField("Reputation Traits:", EditorStyles.boldLabel);
                        
                        EditorGUI.indentLevel++;
                        foreach (var trait in entity.reputationScores)
                        {
                            EditorGUILayout.LabelField($"{trait.Key}: {trait.Value.value:F1} (Confidence: {trait.Value.confidence:F1}%)");
                        }
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Draws a bar representing relationship strength
        /// </summary>
        private void DrawStrengthBar(Rect rect, float strength)
        {
            // Draw background
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f));
            
            // Calculate value rect
            float normalizedValue = (strength + 100) / 200f; // Convert -100 to 100 to 0 to 1
            Rect valueRect = new Rect(
                rect.x + rect.width * 0.5f * (1 - normalizedValue), // Start from middle for negative values
                rect.y,
                rect.width * normalizedValue * 0.5f, // Half width for positive, half for negative
                rect.height
            );
            
            if (strength < 0)
            {
                // For negative values, adjust the rect
                valueRect = new Rect(
                    rect.x + rect.width * 0.5f * normalizedValue,
                    rect.y,
                    rect.width * 0.5f * (0.5f - normalizedValue) * 2,
                    rect.height
                );
            }
            
            // Determine color based on value
            Color barColor;
            if (strength > 20f)
            {
                barColor = Color.Lerp(Color.yellow, Color.green, (strength - 20f) / 80f);
            }
            else if (strength < -20f)
            {
                barColor = Color.Lerp(Color.yellow, Color.red, (-strength - 20f) / 80f);
            }
            else
            {
                barColor = Color.yellow;
            }
            
            // Draw value rect
            EditorGUI.DrawRect(valueRect, barColor);
            
            // Draw line in the middle
            Rect midLineRect = new Rect(rect.x + rect.width * 0.5f - 1, rect.y, 2, rect.height);
            EditorGUI.DrawRect(midLineRect, Color.white);
        }
        
        /// <summary>
        /// Draws a bar representing rumor relevance
        /// </summary>
        private void DrawRelevanceBar(Rect rect, float relevance)
        {
            // Draw background
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f));
            
            // Calculate value rect
            float normalizedValue = relevance / 100f;
            Rect valueRect = new Rect(
                rect.x,
                rect.y,
                rect.width * normalizedValue,
                rect.height
            );
            
            // Determine color based on value
            Color barColor = Color.Lerp(Color.red, Color.green, normalizedValue);
            
            // Draw value rect
            EditorGUI.DrawRect(valueRect, barColor);
        }
        
        #endregion
    }
}
#endif