# Progression and Event System

A powerful and flexible event management system for Unity games that handles storyline progression, character interactions, and event sequencing.

## Features

- **Integrated Event System**: Manage all game events through a centralized system
- **Event Types**: Support for normal, special, seasonal, and crisis events
- **Event Conditions**: Trigger events based on time, relationships, state, location, or compound conditions
- **Event Stages**: Multi-stage event sequences with branching paths
- **Event Choices**: Player-driven decision making with success rates and effects
- **Effect Management**: Apply and track event outcomes on game state
- **UI Integration**: Flexible callback system for event display
- **Event Dependencies**: Chain events together in sequences
- **Event Blocking**: Control which events can coexist
- **Cooldown & Expiration**: Time-based constraints for event triggering

## Documentation

For detailed usage instructions and examples, please refer to:

- [Usage Guide](./USAGE_GUIDE.md): Complete guide with examples and best practices
- [API Reference](./API_REFERENCE.md): Technical documentation of classes and methods
- [Integration Guide](./INTEGRATION_GUIDE.md): Step-by-step integration instructions

## Quick Start

1. Add the `ProgressionAndEventSystem` component to a GameObject in your scene
2. Implement the `ICharacter` interface for your player character
3. Create event definitions using the `GameEvent` class
4. Register events with the system
5. Connect your UI using the callback system

```csharp
// Example: Creating and registering a simple event
GameEvent welcomeEvent = new GameEvent
{
    Id = "welcome_001",
    Title = "Welcome to Town",
    Description = "You arrive at the town gates.",
    Type = EventType.Normal,
    // Add conditions, stages, choices, etc.
};

// Get reference to the system
ProgressionAndEventSystem eventSystem = FindObjectOfType<ProgressionAndEventSystem>();

// Register the event
eventSystem.RegisterEvent(welcomeEvent);
```

## Support

For questions, issues, or feature requests, please contact the development team.