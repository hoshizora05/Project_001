# InteractionSystem Guide

## Overview
The InteractionSystem provides a comprehensive framework for creating meaningful character interactions in your game. This system enables characters to engage in dialogue, exchange messages, and give gifts, with each interaction affecting relationships dynamically.

## Core Components

The InteractionSystem consists of three main subsystems:

1. **DialogueSystem** - Real-time conversations between characters
2. **MessageSystem** - Asynchronous communication (texts, invitations)
3. **GiftSystem** - Gift exchanges and their effects on relationships

## Getting Started

### Basic Setup

```csharp
// Get reference to the InteractionSystem
InteractionSystem interactionSystem = CharacterManager.Instance.GetInteractionSystem();

// Reference to characters
CharacterBase player = CharacterManager.Instance.GetPlayerCharacter();
CharacterBase npc = CharacterManager.Instance.GetCharacterById("npc_001");
```

### Starting a Dialogue

```csharp
// Start a basic conversation
DialogueContext context = new DialogueContext
{
    Location = "Cafe",
    TimeOfDay = TimeOfDay.Morning,
    IsBusy = false
};

DialogueSession session = interactionSystem.StartConversation(player, npc, context);

// Choose a dialogue option
DialogueOption option = session.GetAvailableOptions()[0];
DialogueResult result = session.SelectDialogueOption(option);

// Check conversation outcome
if (result.Success)
{
    // Handle successful dialogue
    float relationshipChange = result.RelationshipImpact;
    Debug.Log($"Dialogue successful! Relationship changed by {relationshipChange}");
    
    if (result.IsCriticalSuccess)
    {
        Debug.Log("Critical success! Bonus relationship points and preference discovered.");
    }
}
```

### Sending Messages

```csharp
// Send a basic text message
MessageContent message = new MessageContent
{
    Text = "Would you like to meet up later?",
    Type = MessageType.Text
};

MessageResult result = interactionSystem.SendMessage(player, npc, message);

// Check if message will receive a response
if (result.WillRespond)
{
    Debug.Log($"Message sent! Expect a response in approximately {result.EstimatedResponseTime} seconds");
}
else
{
    Debug.Log("Message sent, but they might not respond due to your current relationship status");
}

// Send an invitation
EventInvitation invitation = new EventInvitation
{
    EventType = EventType.Dinner,
    Location = "Italian Restaurant",
    Time = System.DateTime.Now.AddHours(2)
};

interactionSystem.SendInvitation(player, npc, invitation);
```

### Giving Gifts

```csharp
// Create a gift item
GiftItem gift = new GiftItem
{
    ItemId = "flower_bouquet",
    Category = GiftCategory.Romantic,
    Value = 25.0f
};

// Give the gift
GiftResult result = interactionSystem.GiveGift(player, npc, gift);

Debug.Log($"Gift reaction: {result.Reaction}");
Debug.Log($"Relationship change: {result.RelationshipImpact}");

// Check if you discovered a preference
if (result.PreferenceDiscovered)
{
    Debug.Log($"You discovered that {npc.Name} {(result.IsPositivePreference ? "loves" : "dislikes")} {result.PreferenceCategory} gifts!");
}
```

## Advanced Usage

### Dialogue with Requirements

```csharp
// Define dialogue options with requirements
DialogueOption persuasion = new DialogueOption
{
    Text = "You should really reconsider your position.",
    RequiredSkill = SkillType.Persuasion,
    RequiredSkillLevel = 3,
    RequiredRelationshipLevel = RelationshipLevel.Friendly
};

// Check if the option would succeed before attempting
float successChance = interactionSystem.CalculateSuccessChance(player, npc, persuasion);
Debug.Log($"Chance of success: {successChance * 100}%");

// Attempt the dialogue option
DialogueResult result = session.SelectDialogueOption(persuasion);
```

### Advanced Messaging

```csharp
// Check relationship status before sending a sensitive message
RelationshipData relationship = RelationshipNetwork.Instance.GetRelationship(player.Id, npc.Id);

if (relationship.Trust >= 70f)
{
    // Send a message with a special relationship requirement
    MessageContent sensitiveMessage = new MessageContent
    {
        Text = "I need to tell you something important...",
        Type = MessageType.Special,
        RequiredRelationshipLevel = RelationshipLevel.Close
    };
    
    interactionSystem.SendMessage(player, npc, sensitiveMessage);
}
```

### Strategic Gift Giving

```csharp
// Check character preferences before giving a gift
CharacterPreferences preferences = interactionSystem.GetKnownPreferences(npc.Id);

// Choose an appropriate gift based on known preferences
GiftCategory bestCategory = GiftCategory.General;

foreach (var preference in preferences)
{
    if (preference.Value > 1.5f) // Strong preference
    {
        bestCategory = preference.Category;
        break;
    }
}

// Give a gift in their preferred category
GiftItem idealGift = new GiftItem
{
    ItemId = "preferred_item",
    Category = bestCategory,
    Value = 50.0f,
    IsSpecialOccasion = npc.IsBirthday
};

interactionSystem.GiveGift(player, npc, idealGift);
```

## Integration with Other Systems

### Using with RelationshipNetwork

```csharp
// Get the relationship before and after an interaction
RelationshipData beforeRelationship = RelationshipNetwork.Instance.GetRelationship(player.Id, npc.Id);

DialogueSession session = interactionSystem.StartConversation(player, npc);
session.SelectDialogueOption(session.GetAvailableOptions()[0]);

RelationshipData afterRelationship = RelationshipNetwork.Instance.GetRelationship(player.Id, npc.Id);

// Compare the changes
float trustChange = afterRelationship.Trust - beforeRelationship.Trust;
float intimacyChange = afterRelationship.Intimacy - beforeRelationship.Intimacy;

Debug.Log($"Trust changed by: {trustChange}, Intimacy changed by: {intimacyChange}");
```

### Using with CharacterMemoryManager

```csharp
// Access interaction history from the memory system
List<InteractionMemory> pastInteractions = CharacterMemoryManager.Instance
    .GetMemories(npc.Id)
    .OfType<InteractionMemory>()
    .ToList();

// Make decisions based on past interactions
bool hasArgued = pastInteractions.Any(m => m.Type == MemoryType.Argument && m.Age.TotalDays < 7);
bool hasGivenGift = pastInteractions.Any(m => m.Type == MemoryType.Gift && m.Age.TotalDays < 3);

if (hasArgued && !hasGivenGift)
{
    // Maybe they need a gift to smooth things over
    GiftItem apologyGift = new GiftItem { ItemId = "chocolate_box", Category = GiftCategory.Food };
    interactionSystem.GiveGift(player, npc, apologyGift);
}
```

## Event System

The InteractionSystem uses events to notify other systems of important changes:

```csharp
// Subscribe to dialogue events
InteractionSystem.OnDialogueStarted += HandleDialogueStarted;
InteractionSystem.OnDialogueEnded += HandleDialogueEnded;

// Subscribe to message events
InteractionSystem.OnMessageSent += HandleMessageSent;
InteractionSystem.OnMessageReceived += HandleMessageReceived;

// Subscribe to gift events
InteractionSystem.OnGiftGiven += HandleGiftGiven;
InteractionSystem.OnPreferenceDiscovered += HandlePreferenceDiscovered;

// Event handler example
private void HandlePreferenceDiscovered(CharacterBase character, GiftCategory category, bool isPositive)
{
    string reaction = isPositive ? "loves" : "dislikes";
    Debug.Log($"Discovered that {character.Name} {reaction} {category} gifts!");
    
    // Update UI or quest system
    UpdateQuestSystem(character, category, isPositive);
}
```

## Common Challenges and Solutions

### Challenge: Characters Not Responding to Messages

**Solution:** Check these potential issues:
- Relationship level may be too low (try improving it first)
- Character might be busy (check IsBusy status before sending)
- You may have reached the message frequency limit (check cooldown)

```csharp
// Check if character is available for messaging
if (!npc.IsBusy && interactionSystem.GetMessageCooldown(npc.Id) <= 0)
{
    // Safe to send message
    interactionSystem.SendMessage(player, npc, message);
}
```

### Challenge: Dialogue Options Not Appearing

**Solution:** Options might have requirements you don't meet:
- Check required skills and levels
- Verify relationship requirements
- Consider time/location constraints

```csharp
// Debug available dialogue options and their requirements
foreach (var option in session.GetAllOptions()) // Not just available ones
{
    bool available = session.IsOptionAvailable(option);
    Debug.Log($"Option: {option.Text} - Available: {available}");
    
    if (!available)
    {
        if (option.RequiredSkillLevel > 0)
        {
            int playerSkill = player.GetSkillLevel(option.RequiredSkill);
            Debug.Log($"  Skill requirement: {option.RequiredSkill} level {option.RequiredSkillLevel} (you have {playerSkill})");
        }
        
        if (option.RequiredRelationshipLevel > RelationshipLevel.Stranger)
        {
            RelationshipLevel current = RelationshipNetwork.Instance.GetRelationshipLevel(player.Id, npc.Id);
            Debug.Log($"  Relationship requirement: {option.RequiredRelationshipLevel} (currently {current})");
        }
    }
}
```

### Challenge: Gifts Not Having Expected Impact

**Solution:** Multiple factors affect gift impact:
- Check if you've given similar gifts recently (repetition penalty)
- Verify character preferences (they might dislike the category)
- Consider timing (birthday gifts have more impact)

```csharp
// Get gift history
List<GiftMemory> giftHistory = CharacterMemoryManager.Instance
    .GetMemories(npc.Id)
    .OfType<GiftMemory>()
    .OrderByDescending(m => m.Timestamp)
    .ToList();

// Check for repetition
bool isRepetitive = giftHistory.Any(g => g.ItemId == "flower_bouquet" && g.Age.TotalDays < 14);

if (isRepetitive)
{
    Debug.Log("This gift might not be effective because you've given something similar recently.");
}
```

## API Reference

### InteractionSystem

| Method | Description |
|--------|-------------|
| `StartConversation(CharacterBase initiator, CharacterBase target, DialogueContext context = null)` | Begins a dialogue session between two characters |
| `SendMessage(CharacterBase sender, CharacterBase recipient, MessageContent message)` | Sends a message from one character to another |
| `SendInvitation(CharacterBase sender, CharacterBase recipient, EventInvitation invitation)` | Sends an event invitation |
| `GiveGift(CharacterBase giver, CharacterBase recipient, GiftItem gift)` | Gives a gift to a character |
| `CalculateSuccessChance(CharacterBase initiator, CharacterBase target, DialogueOption option)` | Calculates probability of dialogue success |
| `GetKnownPreferences(string characterId)` | Retrieves discovered character preferences |
| `GetMessageCooldown(string characterId)` | Checks remaining cooldown time before sending another message |

### DialogueSession

| Method | Description |
|--------|-------------|
| `GetAvailableOptions()` | Gets dialogue options currently available |
| `GetAllOptions()` | Gets all dialogue options, even unavailable ones |
| `IsOptionAvailable(DialogueOption option)` | Checks if a specific option is available |
| `SelectDialogueOption(DialogueOption option)` | Chooses a dialogue option and returns the result |
| `EndDialogue()` | Terminates the dialogue session |

### Events

| Event | Description |
|-------|-------------|
| `OnDialogueStarted` | Fired when a dialogue begins |
| `OnDialogueEnded` | Fired when a dialogue ends |
| `OnMessageSent` | Fired when a message is sent |
| `OnMessageReceived` | Fired when a message is received |
| `OnGiftGiven` | Fired when a gift is given |
| `OnPreferenceDiscovered` | Fired when a character preference is discovered |

## Performance Considerations

For optimal performance:
- Limit the number of active dialogue sessions (end conversations when complete)
- Use message cooldowns to prevent spam
- Consider batching relationship updates for frequently interacting characters

```csharp
// Performance optimization for bulk interactions
interactionSystem.BeginBatch();

// Perform multiple interactions
interactionSystem.SendMessage(player, npc1, message1);
interactionSystem.SendMessage(player, npc2, message2);
interactionSystem.GiveGift(player, npc3, gift);

// Apply all relationship changes at once
interactionSystem.EndBatch();
```

## Conclusion

The InteractionSystem provides a robust framework for creating deep and meaningful character interactions in your game. By leveraging dialogue, messaging, and gift mechanics, you can create complex relationship dynamics that evolve based on player choices and character preferences.

For more information about character relationships, refer to the RELATIONSHIP_NETWORK_GUIDE.md document.