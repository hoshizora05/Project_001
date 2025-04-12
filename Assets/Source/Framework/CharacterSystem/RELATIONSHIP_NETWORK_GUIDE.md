# Relationship Network System - User Guide

## Overview

The Relationship Network System manages dynamic relationships between NPCs and the player, simulating information flow and reputation effects throughout your game world. This system enables your characters to form connections, share information, develop reputations, and create social groups that evolve organically over time.

## Key Features

- **Dynamic Relationships**: Relationships that evolve based on interactions and time
- **Reputation System**: Character reputations that spread through the social network
- **Information Propagation**: Realistic information sharing with accuracy degradation
- **Rumor System**: Rumors that spread, mutate, and affect reputations
- **Group Dynamics**: Social groups with internal hierarchies and cohesion

## Quick Start Guide

Here's how to get the Relationship Network System working in your game:

1. **Setup the System**

   Add the `RelationshipNetwork` component to a GameObject in your scene. For convenience, you can use:

   ```csharp
   // Get reference to the system
   var relationshipSystem = RelationshipNetwork.Instance;
   
   // Initialize the system
   relationshipSystem.Initialize();
   ```

2. **Add Characters to the Network**

   Before characters can interact, add them to the network:

   ```csharp
   // Use the API for cleaner access
   var networkAPI = RelationshipNetworkAPI.Instance;
   
   // Add player
   networkAPI.AddEntity("player_1", "Player", transform.position, 2.0f);
   
   // Add NPCs
   networkAPI.AddEntity("villager_1", "NPC", new Vector3(10, 0, 5), 1.0f);
   networkAPI.AddEntity("merchant_2", "NPC", new Vector3(-5, 0, 8), 1.5f);
   ```

3. **Create and Update Relationships**

   When characters interact, update their relationship:

   ```csharp
   // Positive interaction (values from -100 to 100)
   networkAPI.UpdateRelationship("player_1", "villager_1", "helped_in_quest", 15.0f);
   
   // Negative interaction
   networkAPI.UpdateRelationship("villager_1", "merchant_2", "argument", -10.0f);
   ```

4. **Query Relationships**

   Check relationship status between characters:

   ```csharp
   // Get relationship data
   RelationshipData relationship = networkAPI.GetRelationshipStatus("player_1", "villager_1");
   
   if (relationship != null)
   {
       // Check relationship strength (-100 to 100)
       float strength = relationship.strength;
       
       // Check trust level (0 to 100)
       float trust = relationship.trust;
       
       // Check familiarity (0 to 100)
       float familiarity = relationship.familiarity;
       
       // Check relationship type
       string type = relationship.type;  // "friend", "enemy", "acquaintance", etc.
   }
   ```

5. **Create Groups**

   Form social groups with members:

   ```csharp
   // Create a group
   networkAPI.CreateGroup("village_council", "Village governance", 70.0f);
   
   // Add members to the group
   networkAPI.ManageGroupMembership("village_council", "villager_1", "add");
   networkAPI.ManageGroupMembership("village_council", "merchant_2", "add");
   
   // Promote a member to leadership
   networkAPI.ManageGroupMembership("village_council", "villager_1", "promote");
   ```

6. **Manage Reputation**

   Check a character's reputation:

   ```csharp
   // Get global reputation
   ReputationData repData = networkAPI.GetReputation("villager_1");
   float globalRep = repData.globalReputation;
   
   // Get reputation from specific perspective
   ReputationData playerView = networkAPI.GetReputation("villager_1", "player_1");
   ```

7. **Spread Information**

   Create and share information between characters:

   ```csharp
   // Create information (id, type, content, sensitivity, originator)
   var infoContent = new Dictionary<string, object>
   {
       { "location", "forest_cave" },
       { "details", "Contains hidden treasure" }
   };
   
   networkAPI.CreateInformationUnit(
       "secret_location_1", 
       "location_info",
       infoContent,
       0.8f,  // High sensitivity (0-1)
       "player_1"
   );
   
   // Share information
   networkAPI.ShareInformation(
       "player_1",            // sender
       "villager_1",          // receiver
       "secret_location_1",   // info id
       "direct_conversation"  // channel type
   );
   
   // Check if character knows information
   InformationKnowledge knowledge = networkAPI.CheckKnowledgeState("villager_1", "secret_location_1");
   if (knowledge != null)
   {
       float accuracy = knowledge.accuracy;  // How accurate is their knowledge
       float detail = knowledge.detail;      // How detailed is their knowledge
   }
   ```

8. **Create and Spread Rumors**

   Generate rumors that affect reputations:

   ```csharp
   // Create a rumor (originator, subject, content, truth value 0-100)
   networkAPI.CreateAndPropagateRumor(
       "merchant_2",         // who started it
       "villager_1",         // who it's about
       "has_hidden_wealth",  // what the rumor claims
       30.0f                 // 30% truth value
   );
   ```

## Core Systems Explained

### 1. Relationship Graph

The relationship graph tracks connections between entities (characters):

```
Entity A -----> Entity B
  |              |
  v              v
Entity C <----- Entity D
```

Each connection (edge) has properties:
- **Strength**: Overall relationship quality (-100 to 100)
- **Trust**: How much they trust each other (0 to 100)
- **Familiarity**: How well they know each other (0 to 100)
- **Type**: Relationship type (friend, enemy, family, etc.)
- **History**: Record of past interactions

### 2. Reputation System

Characters build reputations based on:
- Direct interactions with others
- Information shared about them
- Rumors (which may be false)

Reputation has multiple aspects:
- **Global Reputation**: Overall standing in the world
- **Trait-Based Reputation**: Specific qualities (honesty, generosity, etc.)
- **Group-Specific Reputation**: Standing within specific communities
- **Perspective-Based Reputation**: How specific characters view them

### 3. Information Propagation

Information spreads through the network based on:
- Relationship strength and trust
- Information sensitivity
- Communication channel properties

As information travels:
- **Accuracy degrades** based on trust and noise
- **Detail degrades** as it passes through multiple people
- Some entities may never receive certain information

### 4. Group Dynamics

Social groups provide:
- Reputation effects within communities
- Group-based decision making
- Hierarchies and leadership structures
- Group cohesion that affects information spread

## Advanced Usage

### Using Events

The system uses an event-driven approach. Subscribe to events to respond to changes:

```csharp
// Add a RelationshipNetworkUIEvents component to listen for events
var eventHandler = gameObject.AddComponent<RelationshipNetworkUIEvents>();

// Hook up event handlers
eventHandler.onRelationshipChanged.AddListener((sourceId, targetId, strength) => {
    Debug.Log($"Relationship between {sourceId} and {targetId} changed to {strength}");
});

eventHandler.onReputationChanged.AddListener((entityId, newValue) => {
    Debug.Log($"{entityId}'s reputation changed to {newValue}");
});
```

### Custom Visualization

The system includes a visualizer for debugging:

1. Add the `RelationshipNetworkVisualizer` component to a GameObject
2. Use the Inspector to customize visualization settings
3. View relationships as colored lines in the Scene view

### Editor Tools

Use the built-in editor window for testing and debugging:

1. Open the editor window via `Window > Character System > Relationship Network Inspector`
2. View and modify relationships, create groups, spread information, and more
3. Use this tool to set up initial relationships or test scenarios

## Integration Examples

### Quest System Integration

```csharp
// When player completes a quest for an NPC
void OnQuestCompleted(string questId, string npcId)
{
    // Improve relationship based on quest importance
    float questImportance = questManager.GetQuestImportance(questId);
    float relationshipBoost = questImportance * 10f;  // Scale appropriately
    
    networkAPI.UpdateRelationship("player_1", npcId, "completed_quest", relationshipBoost);
    
    // Spread information about player's helpfulness
    Dictionary<string, object> content = new Dictionary<string, object>
    {
        { "event", "quest_completion" },
        { "quest", questId },
        { "helper", "player_1" }
    };
    
    string infoId = $"quest_completion_{questId}";
    
    networkAPI.CreateInformationUnit(infoId, "reputation_event", content, 0.3f, npcId);
    
    // Character might tell others about the player's help
    SpreadQuestCompletion(npcId, infoId);
}

// NPC tells friends about the player's help
void SpreadQuestCompletion(string npcId, string infoId)
{
    // Get NPCs with good relationships to share with
    var relationships = GetRelationshipsAboveThreshold(npcId, 30f);
    
    foreach (var friendId in relationships)
    {
        networkAPI.ShareInformation(npcId, friendId, infoId, "casual_conversation");
    }
}
```

### Dialogue System Integration

```csharp
// Check relationship before dialogue options
void SetupDialogueOptions(string npcId)
{
    RelationshipData relationship = networkAPI.GetRelationshipStatus("player_1", npcId);
    
    // Unlock dialogue options based on relationship
    if (relationship.strength > 50)
    {
        dialogueSystem.UnlockOption("ask_personal_question");
    }
    
    if (relationship.trust > 70)
    {
        dialogueSystem.UnlockOption("ask_for_secret");
    }
    
    // Check if NPC knows certain information
    InformationKnowledge knowledge = networkAPI.CheckKnowledgeState(npcId, "secret_plot");
    if (knowledge != null && knowledge.accuracy > 0.7f)
    {
        dialogueSystem.UnlockOption("ask_about_plot");
    }
}
```

### Faction System Integration

```csharp
// When player helps a faction, update relationships with all members
void OnFactionActionCompleted(string factionId, float reputationChange)
{
    // Get all faction members
    List<string> factionMembers = GetFactionMembers(factionId);
    
    // Update relationship with each member
    foreach (string memberId in factionMembers)
    {
        float relationshipChange = reputationChange * 0.5f;
        networkAPI.UpdateRelationship("player_1", memberId, "faction_assistance", relationshipChange);
    }
    
    // Create rumor about player's alliance
    if (reputationChange > 20)
    {
        string opposingFaction = GetOpposingFaction(factionId);
        if (!string.IsNullOrEmpty(opposingFaction))
        {
            // Opposing faction spreads negative rumor
            string rumorSource = GetRandomFactionMember(opposingFaction);
            networkAPI.CreateAndPropagateRumor(
                rumorSource,
                "player_1",
                "supports_enemy_faction",
                85.0f  // Mostly true
            );
        }
    }
}
```

## Performance Considerations

- The system automatically performs periodic updates at an adjustable frequency
- For large numbers of entities (1000+), consider:
  - Reducing update frequency
  - Limiting relationship updates to entities near the player
  - Using LOD (Level of Detail) for distant relationships

## Customization

### Adding Custom Relationship Types

Define new relationship types in your game logic:

```csharp
// When characters become family members
public void EstablishFamilyRelationship(string character1Id, string character2Id)
{
    // Get existing relationship
    RelationshipData relationship = networkAPI.GetRelationshipStatus(character1Id, character2Id);
    
    if (relationship != null)
    {
        // Use the Edit API to update relationship type
        RelationshipChangeResult result = networkAPI.UpdateRelationship(
            character1Id, 
            character2Id, 
            "became_family", 
            50.0f
        );
        
        // Update custom attributes via reflection
        Type type = relationship.GetType();
        var fieldInfo = type.GetField("type");
        if (fieldInfo != null)
        {
            fieldInfo.SetValue(relationship, "family");
        }
    }
}
```

### Custom Reputation Traits

Add new reputation traits based on your game's needs:

```csharp
// After player actions that demonstrate courage
public void UpdateCourageReputation(string characterId, float courageValue)
{
    // Get character's reputation
    ReputationData repData = networkAPI.GetReputation(characterId);
    
    // Update or add courage trait via reflection
    Type type = repData.GetType();
    var fieldInfo = type.GetField("reputationScores");
    
    if (fieldInfo != null)
    {
        var scores = fieldInfo.GetValue(repData) as Dictionary<string, ReputationScore>;
        
        if (scores != null)
        {
            if (scores.ContainsKey("courage"))
            {
                var score = scores["courage"];
                score.value = Mathf.Clamp(score.value + courageValue, -100f, 100f);
                scores["courage"] = score;
            }
            else
            {
                // Create new reputation score
                var newScore = new ReputationScore
                {
                    value = courageValue,
                    confidence = 40f,
                    sources = new List<string> { "observer" }
                };
                
                scores["courage"] = newScore;
            }
        }
    }
}
```

## Common Challenges and Solutions

### Challenge: Relationships Changing Too Quickly

**Solution**: Adjust interaction intensity values and natural decay rate.

```csharp
// Use smaller values for routine interactions
networkAPI.UpdateRelationship("npc_1", "npc_2", "small_talk", 2.0f);  // Small positive change

// Reserve larger values for significant events
networkAPI.UpdateRelationship("player_1", "npc_1", "saved_life", 50.0f);  // Major positive change
```

### Challenge: Information Spreads Too Quickly

**Solution**: Adjust communication channels and trust factors.

```csharp
// Create a more restrictive communication channel
List<string> participants = new List<string> { "npc_1", "npc_2" };
networkAPI.CreateCommunicationChannel(
    "private_conversation",
    participants,
    0.9f,  // High bandwidth
    0.1f   // Low noise
);

// Make sensitive information harder to spread
Dictionary<string, object> content = new Dictionary<string, object>();
content.Add("secret", "very important information");

networkAPI.CreateInformationUnit(
    "top_secret",
    "sensitive_info",
    content,
    0.9f,  // Very sensitive (0-1)
    "npc_1"
);
```

### Challenge: Debugging Complex Networks

**Solution**: Use the visualization tools and event logging.

```csharp
// Enable debug mode in the RelationshipNetwork component via Inspector

// Add a custom logger for relationship events
CharacterEventBus.Instance.Subscribe("relationship_change", (evt) => {
    if (evt is RelationshipChangeEvent relEvt)
    {
        Debug.Log($"Relationship change: {relEvt.sourceEntityId} -> {relEvt.targetEntityId}: " +
                  $"{relEvt.previousStrength} to {relEvt.newStrength} via {relEvt.interactionType}");
    }
});
```

## Conclusion

The Relationship Network System provides a robust foundation for creating dynamic social interactions in your game. By using this system, your characters will form believable relationships, share information naturally, and develop reputations that affect gameplay.

Remember that the system is designed to be customized to your specific game needs. Experiment with different interaction types, reputation traits, and group dynamics to create the exact social simulation your game requires.

For more technical details, see the API documentation or inspect the source code.