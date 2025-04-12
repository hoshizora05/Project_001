# Information Management UI System

## Overview

The Information Management UI system is a comprehensive framework for displaying and managing various types of in-game information through a unified interface. It follows the MVVM (Model-View-ViewModel) architecture pattern and provides an intuitive way to visualize and interact with:

- Calendar and event scheduling
- Character relationships and social networks
- Character status and attributes

## Key Features

- **Calendar Management**: Track events, appointments, deadlines, and special dates
- **Relationship Visualization**: View and manage character relationships and social networks
- **Status Monitoring**: Track character statistics, skills, conditions, and inventories
- **Unified UI System**: Seamlessly switch between different information views
- **Reactive Design**: UI automatically updates when data changes using UniRx
- **Modular Architecture**: Easily extend with new information views

## Setup Instructions

### 1. Scene Setup

1. Create a new GameObject in your scene and name it "InformationManagementUI"
2. Add the `InformationManagementUISystem` component to this GameObject
3. Create UI Documents for each view component (Calendar, Relationship, Status)
4. Assign the UI Documents to their respective view controller components

### 2. Data Configuration

1. Create instances of the required ScriptableObject data providers:
   - `CalendarData` for calendar information
   - `RelationshipGraphData` for relationship networks
   - `CharacterStatusData` for character statuses
2. Configure the data providers with initial data if needed
3. Assign these data providers to the `InformationManagementUISystem` component

### 3. UI Configuration

1. Set up the UI Document with the expected container and navigation elements:
   - Add a main container with the ID `main-container`
   - Add a navigation bar with the ID `nav-bar`
   - Add navigation buttons with IDs `calendar-button`, `relationship-button`, `status-button`
   - Add a toggle button with the ID `toggle-ui-button`
2. Configure each view's specific UI components according to their requirements

## Usage Guide

### Opening the UI

The Information Management UI can be opened programmatically:

```csharp
// Reference to the UI system
private InformationManagementUISystem _uiSystem;

// Show the UI with a specific view
_uiSystem.ShowUI(UIViewType.Calendar);
// OR
_uiSystem.ShowUI(UIViewType.RelationshipDiagram);
// OR
_uiSystem.ShowUI(UIViewType.StatusManagement);
```

### Calendar View

The Calendar View allows you to:

1. **Navigate by Month/Week**: Switch between month and week views
2. **View and Manage Events**: See events displayed on calendar days
3. **Create and Edit Events**: Add new events or edit existing ones
4. **Track Special Dates**: Visualize holidays, birthdays, and other important dates

#### Calendar Event Types

Events can be categorized into different types:
- Personal
- Social
- Work
- Story
- Deadline
- Custom

#### Event Priorities

Events can have different priority levels:
- Low
- Medium
- High
- Urgent

#### Special Dates

Special dates can be categorized as:
- Holiday
- Birthday
- Anniversary
- Seasonal Event
- Game Event
- Custom

#### Common Actions

```csharp
// Show the calendar for a specific date
_uiSystem.ShowCalendarForDate(DateTime.Now);

// Using the data provider directly
var calendarData = GetComponent<ICalendarDataProvider>();

// Add a new event
var newEvent = new CalendarEvent(
    "event1",
    "Meeting with NPC",
    "Discuss quest details with the village elder",
    DateTime.Now.AddDays(2).AddHours(14),
    DateTime.Now.AddDays(2).AddHours(15),
    EventType.Story,
    EventPriority.Medium
);
calendarData.AddEvent(newEvent);

// Mark an event as completed
calendarData.UpdateEvent(newEvent.MarkAsCompleted());
```

### Relationship Diagram View

The Relationship Diagram View allows you to:

1. **Visualize Character Networks**: See character connections in a graph layout
2. **Inspect Relationships**: View detailed information about character relationships
3. **Manage Groups**: Organize characters into logical groups
4. **Focus on Characters**: Center the view on specific characters of interest

#### Relationship Types

Relationships can be categorized into different types:
- Friend
- Family
- Romantic
- Colleague
- Ally
- Rival
- Enemy
- Acquaintance
- Business
- Custom

#### Relationship Attributes

Relationships can have various attributes:
- Trust
- Respect
- Affection
- Loyalty
- Dependence
- Influence
- Conflict
- Envy
- History
- Secret
- Debt
- Custom

#### Common Actions

```csharp
// Show relationships centered on a specific character
_uiSystem.ShowRelationshipsForCharacter("character1");

// Using the data provider directly
var relationshipData = GetComponent<IRelationshipDataProvider>();

// Add a new character
var newCharacter = new CharacterNode(
    "character2",
    "Village Elder",
    CharacterType.NPC,
    new Vector2(100, 150)
);
relationshipData.AddCharacter(newCharacter);

// Create a relationship between characters
var relationship = new RelationshipEdge(
    "character1",
    "character2",
    RelationshipType.Mentor,
    0.8f,
    new List<RelationshipAttribute> {
        new RelationshipAttribute(RelationshipAttributeType.Respect, 0.9f),
        new RelationshipAttribute(RelationshipAttributeType.Trust, 0.7f)
    }
);
relationshipData.AddOrUpdateRelationship(relationship);
```

### Status Management View

The Status Management View allows you to:

1. **View Character Stats**: See a character's basic attributes and statistics
2. **Track Skills and Progress**: Monitor skill levels and development
3. **Monitor Conditions**: View physical and mental states
4. **Review Inventory**: Browse character possessions and resources
5. **Track Goals**: View character objectives and their progress

#### Character Stats

Characters can have various stat types:
- Strength
- Dexterity
- Constitution
- Intelligence
- Wisdom
- Charisma
- Luck
- Custom

#### Emotional States

Characters can have various emotional states:
- Joy
- Sadness
- Anger
- Fear
- Disgust
- Surprise
- Trust
- Anticipation
- Love
- Jealousy
- Pride
- Shame
- Guilt
- Custom

#### Physical Conditions

Characters can have various physical conditions:
- Healthy
- Injured
- Sick
- Exhausted
- Hungry
- Thirsty
- Intoxicated
- Custom

#### Common Actions

```csharp
// Show status for a specific character
_uiSystem.ShowStatusForCharacter("character1");

// Using the data provider directly
var statusData = GetComponent<ICharacterStatusDataProvider>();

// Update a character's stat
statusData.UpdateStat("character1", StatType.Strength, 0.75f);

// Update a character's skill
statusData.UpdateSkill("character1", "swordsmanship", 0.8f, 450f, 1000f);

// Update a character's mental state
var mentalState = new MentalState(
    new Dictionary<EmotionType, float> {
        { EmotionType.Joy, 0.7f },
        { EmotionType.Anger, 0.2f }
    },
    0.3f, // stress
    0.8f, // happiness
    0.9f, // motivation
    new List<MoodEffect>()
);
statusData.UpdateMentalState("character1", mentalState);
```

## Integration with Other Systems

### Character System Integration

The Information Management UI system can integrate with your character system:

```csharp
// When a character's stats change
public void OnCharacterStatsChanged(Character character)
{
    // Update the status data
    _statusDataProvider.UpdateStat(character.ID, StatType.Strength, character.Strength);
    // Additional stat updates...
    
    // Record a snapshot of the character's current status
    _statusDataProvider.RecordStatusSnapshot(character.ID);
}
```

### Event System Integration

```csharp
// When game events occur that should appear on the calendar
public void OnQuestAssigned(Quest quest)
{
    // Add the quest deadline to the calendar
    var deadlineEvent = new CalendarEvent(
        $"quest_{quest.ID}_deadline",
        $"Quest Deadline: {quest.Title}",
        quest.Description,
        quest.DeadlineDate,
        quest.DeadlineDate.AddHours(1),
        EventType.Deadline,
        EventPriority.High
    );
    _calendarDataProvider.AddEvent(deadlineEvent);
}
```

### Relationship System Integration

```csharp
// When character relationships change
public void OnCharacterRelationshipChanged(Character character1, Character character2, float relationshipValue)
{
    // Update the relationship in the diagram
    var relationshipType = DetermineRelationshipType(relationshipValue);
    var relationship = new RelationshipEdge(
        character1.ID,
        character2.ID,
        relationshipType,
        Mathf.Abs(relationshipValue),
        GenerateRelationshipAttributes(character1, character2)
    );
    _relationshipDataProvider.AddOrUpdateRelationship(relationship);
}
```

## Customization

### Adding Custom View Types

1. Add a new enum value to the `UIViewType` enum
2. Create a new view controller class that handles the UI for your view
3. Add the necessary references and initialization in the `InformationManagementUISystem`
4. Create a new data provider for your view's data if needed
5. Add navigation buttons and UI elements for the new view

### Styling the UI

The UI uses USS (Unity Style Sheets) for styling. You can customize the appearance by:

1. Modifying the USS files for each view
2. Adding custom class names for styled elements
3. Creating theme variants for different visual styles

## Troubleshooting

### Common Issues

1. **UI not appearing**:
   - Check that the UI Document reference is set correctly
   - Verify that the Panel Settings are configured properly
   - Ensure the UI Document has a root visual element

2. **Data not updating**:
   - Confirm the data providers are properly initialized
   - Check that event subscriptions are set up correctly
   - Verify that data change notifications are being triggered

3. **Navigation not working**:
   - Ensure navigation buttons have the correct IDs
   - Verify that button click handlers are registered
   - Check for errors in the button callback functions

### Debugging Tips

- Use the Unity UI Builder to inspect and debug the UI hierarchy
- Check the Console for any errors related to UI initialization
- Use ReactiveProperty's built-in debugging features to trace data flow

## API Reference

See the code documentation for complete API details:

- `InformationManagementUISystem`: Main controller for the UI system
- `CalendarViewController`: Handles the calendar view UI
- `RelationshipDiagramViewController`: Handles the relationship diagram view UI
- `StatusManagementViewController`: Handles the character status view UI
- Data providers:
  - `ICalendarDataProvider`: Interface for calendar data
  - `IRelationshipDataProvider`: Interface for relationship data
  - `ICharacterStatusDataProvider`: Interface for character status data