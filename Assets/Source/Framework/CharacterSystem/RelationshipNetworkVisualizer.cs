using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CharacterSystem
{
    /// <summary>
    /// Visualizer for the relationship network
    /// Used for debugging and editor tools
    /// </summary>
    public class RelationshipNetworkVisualizer : MonoBehaviour
    {
        [SerializeField] private RelationshipNetwork networkSystem;
        
        [Header("Visualization Settings")]
        [SerializeField] private bool visualizeInGame = false;
        [SerializeField] private bool visualizeEntities = true;
        [SerializeField] private bool visualizeRelationships = true;
        [SerializeField] private bool visualizeGroups = true;
        [SerializeField] private float nodeSize = 0.5f;
        
        [Header("Colors")]
        [SerializeField] private Color positiveRelationshipColor = Color.green;
        [SerializeField] private Color negativeRelationshipColor = Color.red;
        [SerializeField] private Color neutralRelationshipColor = Color.yellow;
        [SerializeField] private Color playerColor = Color.blue;
        [SerializeField] private Color npcColor = Color.cyan;
        [SerializeField] private Color groupColor = Color.magenta;
        
        private Dictionary<string, Vector3> entityPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, Color> entityColors = new Dictionary<string, Color>();
        
        private RelationshipGraph cachedGraph;
        
        private void Start()
        {
            if (networkSystem == null)
            {
                networkSystem = RelationshipNetwork.Instance;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!visualizeInGame && !Application.isEditor)
            {
                return;
            }
            
            // In editor mode, we need to ensure we have a reference to the system
            if (networkSystem == null)
            {
                networkSystem = FindFirstObjectByType<RelationshipNetwork>();
                if (networkSystem == null)
                {
                    return;
                }
            }
            
            // Get a reference to the relationship graph using reflection
            // This is not ideal but allows for visualization without exposing internals
            if (cachedGraph == null)
            {
                var fieldInfo = typeof(RelationshipNetwork).GetField("_relationshipGraph", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (fieldInfo != null)
                {
                    cachedGraph = fieldInfo.GetValue(networkSystem) as RelationshipGraph;
                }
                else
                {
                    Debug.LogError("Failed to access relationship graph through reflection");
                    return;
                }
            }
            
            if (cachedGraph == null)
            {
                return;
            }
            
            // Calculate positions for visualization (simple circle layout)
            CalculateVisualizationPositions();
            
            // Draw entities
            if (visualizeEntities)
            {
                DrawEntities();
            }
            
            // Draw relationships
            if (visualizeRelationships)
            {
                DrawRelationships();
            }
            
            // Draw groups
            if (visualizeGroups)
            {
                DrawGroups();
            }
        }
        
        /// <summary>
        /// Calculates positions for entities in a circle layout
        /// </summary>
        private void CalculateVisualizationPositions()
        {
            float radius = 5f * Mathf.Sqrt(cachedGraph.entities.Count);
            float angleStep = 2f * Mathf.PI / cachedGraph.entities.Count;
            
            for (int i = 0; i < cachedGraph.entities.Count; i++)
            {
                var entity = cachedGraph.entities[i];
                float angle = i * angleStep;
                
                // Calculate position in a circle
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                
                // Add some variation to avoid overlaps
                position += new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    0,
                    UnityEngine.Random.Range(-0.5f, 0.5f)
                );
                
                // Store the calculated position
                entityPositions[entity.id] = position;
                
                // Assign color based on entity type
                if (entity.type == "Player")
                {
                    entityColors[entity.id] = playerColor;
                }
                else
                {
                    entityColors[entity.id] = npcColor;
                }
            }
        }
        
        /// <summary>
        /// Draws entities as spheres
        /// </summary>
        private void DrawEntities()
        {
            foreach (var entity in cachedGraph.entities)
            {
                if (entityPositions.ContainsKey(entity.id))
                {
                    // Draw sphere at entity position
                    Gizmos.color = entityColors[entity.id];
                    Gizmos.DrawSphere(entityPositions[entity.id], nodeSize * (0.5f + entity.influence * 0.5f));
                    
                    // Draw label
                    #if UNITY_EDITOR
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(entityPositions[entity.id] + Vector3.up * nodeSize * 1.5f, entity.id);
                    #endif
                }
            }
        }
        
        /// <summary>
        /// Draws relationships as lines between entities
        /// </summary>
        private void DrawRelationships()
        {
            foreach (var relationship in cachedGraph.relationships)
            {
                // Skip if we don't have positions for both entities
                if (!entityPositions.ContainsKey(relationship.sourceId) || 
                    !entityPositions.ContainsKey(relationship.targetId))
                {
                    continue;
                }
                
                Vector3 sourcePos = entityPositions[relationship.sourceId];
                Vector3 targetPos = entityPositions[relationship.targetId];
                
                // Determine color based on relationship strength
                Color relationshipColor;
                if (relationship.strength > 20f)
                {
                    relationshipColor = Color.Lerp(neutralRelationshipColor, positiveRelationshipColor, 
                                                 (relationship.strength - 20f) / 80f);
                }
                else if (relationship.strength < -20f)
                {
                    relationshipColor = Color.Lerp(neutralRelationshipColor, negativeRelationshipColor, 
                                                 (-relationship.strength - 20f) / 80f);
                }
                else
                {
                    relationshipColor = neutralRelationshipColor;
                }
                
                // Draw line between entities
                Gizmos.color = relationshipColor;
                Gizmos.DrawLine(sourcePos, targetPos);
                
                // Draw relationship type label at the midpoint
                #if UNITY_EDITOR
                Vector3 midpoint = (sourcePos + targetPos) * 0.5f + Vector3.up * 0.2f;
                UnityEditor.Handles.color = relationshipColor;
                UnityEditor.Handles.Label(midpoint, relationship.type);
                
                // Draw strength value
                Vector3 strengthPos = midpoint + Vector3.up * 0.3f;
                UnityEditor.Handles.Label(strengthPos, relationship.strength.ToString("F1"));
                #endif
            }
        }
        
        /// <summary>
        /// Draws groups as larger transparent spheres containing members
        /// </summary>
        private void DrawGroups()
        {
            foreach (var group in cachedGraph.groups)
            {
                // Skip empty groups
                if (group.members.Count == 0)
                {
                    continue;
                }
                
                // Calculate group center as average of member positions
                Vector3 groupCenter = Vector3.zero;
                int validMembers = 0;
                
                foreach (var memberId in group.members)
                {
                    if (entityPositions.ContainsKey(memberId))
                    {
                        groupCenter += entityPositions[memberId];
                        validMembers++;
                    }
                }
                
                if (validMembers == 0)
                {
                    continue;
                }
                
                groupCenter /= validMembers;
                
                // Calculate group radius to encompass all members
                float radius = 1f;
                foreach (var memberId in group.members)
                {
                    if (entityPositions.ContainsKey(memberId))
                    {
                        float distance = Vector3.Distance(groupCenter, entityPositions[memberId]);
                        radius = Mathf.Max(radius, distance + nodeSize);
                    }
                }
                
                // Draw transparent sphere for the group
                Gizmos.color = new Color(groupColor.r, groupColor.g, groupColor.b, 0.3f);
                Gizmos.DrawSphere(groupCenter, radius);
                
                // Draw group label
                #if UNITY_EDITOR
                UnityEditor.Handles.color = groupColor;
                UnityEditor.Handles.Label(groupCenter + Vector3.up * radius, 
                                         $"{group.id} ({group.purpose}) - Cohesion: {group.cohesion:F1}");
                #endif
                
                // Draw connections from group center to members
                Gizmos.color = new Color(groupColor.r, groupColor.g, groupColor.b, 0.5f);
                foreach (var memberId in group.members)
                {
                    if (entityPositions.ContainsKey(memberId))
                    {
                        Gizmos.DrawLine(groupCenter, entityPositions[memberId]);
                    }
                }
            }
        }
        
        /// <summary>
        /// Forces a refresh of the visualization
        /// </summary>
        public void RefreshVisualization()
        {
            cachedGraph = null;
            entityPositions.Clear();
            entityColors.Clear();
        }
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Custom editor for the relationship network visualizer
    /// </summary>
    [UnityEditor.CustomEditor(typeof(RelationshipNetworkVisualizer))]
    public class RelationshipNetworkVisualizerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            RelationshipNetworkVisualizer visualizer = (RelationshipNetworkVisualizer)target;
            
            if (GUILayout.Button("Refresh Visualization"))
            {
                visualizer.RefreshVisualization();
                UnityEditor.SceneView.RepaintAll();
            }
        }
    }
    #endif
}