# Save and Load System - User Guide

## Overview

The Save and Load System provides a comprehensive framework for saving and loading game data in Unity. This system integrates with various game subsystems and manages save files with features like encryption, compression, backup management, and thumbnail generation.

## Table of Contents

1. [Setting Up the System](#setting-up-the-system)
2. [Basic Usage](#basic-usage)
3. [Working with Save Slots](#working-with-save-slots)
4. [Making Objects Saveable](#making-objects-saveable)
5. [Autosaves](#autosaves)
6. [Save File Management](#save-file-management)
7. [Integration with Game Systems](#integration-with-game-systems)
8. [Advanced Features](#advanced-features)
9. [Troubleshooting](#troubleshooting)

## Setting Up the System

### 1. Installation

Add the `SaveLoadManager.cs` script to your Unity project. This single file contains all the necessary code for the save system.

### 2. Scene Setup

1. Create an empty GameObject in your initial scene (preferably a scene that persists throughout the game)
2. Name it "SaveLoadManager"
3. Add the `SaveLoadManager` component to this GameObject
4. Configure the component settings:
   - **Default Save File Name**: Base name for save files (default: "save")
   - **Save File Extension**: File extension for saves (default: ".sav")
   - **Use Encryption**: Whether to encrypt save files
   - **Encryption Key**: Secret key for encryption (change this for security)
   - **Use Compression**: Whether to compress save files to save space
   - **Create Backups**: Whether to create backup copies of save files
   - **Max Backup Count**: Number of backups to keep per save slot
   - **Auto Save On Application Quit**: Whether to save automatically when the game closes
   - **Verbose Logging**: Enable for detailed logging during development
   - **Max Save Slots**: Maximum number of save slots available
   - **Autosave Slot Name**: Name identifier for autosave slots

### 3. Accessing the Manager

The `SaveLoadManager` is implemented as a singleton, so you can access it from anywhere in your code:

```csharp
SaveLoadManager saveManager = SaveLoadManager.Instance;
```

## Basic Usage

### Creating a New Save

To start a new game and create a fresh save file:

```csharp
// Create a new save with player name
SaveData newSave = SaveLoadManager.Instance.CreateNewSave("PlayerName");

// Now you can start playing your game...

// When ready to save to disk (e.g., at a checkpoint):
await SaveLoadManager.Instance.SaveGameAsync(0); // Save to slot 0
```

### Saving the Game

To save the current game state to a specific slot:

```csharp
// Save to slot 1 with thumbnail
bool success = await SaveLoadManager.Instance.SaveGameAsync(1, includeThumbnail: true);

if (success) {
    Debug.Log("Game saved successfully!");
} else {
    Debug.LogError("Failed to save game!");
}
```

### Loading the Game

To load a game from a specific slot:

```csharp
// Load from slot 1
bool success = await SaveLoadManager.Instance.LoadGameAsync(1);

if (success) {
    Debug.Log("Game loaded successfully!");
    // Update your game state/UI based on the loaded data
} else {
    Debug.LogError("Failed to load game!");
}
```

## Working with Save Slots

### Understanding Save Slots

The system uses numbered slots (0 to `_maxSaveSlots-1`) to store different save files. This allows players to maintain multiple saves.

### Checking if a Save Exists

Before loading, you might want to check if a save exists in a slot:

```csharp
bool saveExists = SaveLoadManager.Instance.DoesSaveExist(2); // Check slot 2

if (saveExists) {
    // Show "Continue" or "Load" button
} else {
    // Hide or disable the button
}
```

### Getting All Save Metadata

To display a list of saves for the player to choose from:

```csharp
Dictionary<int, SaveMetadata> allSaves = SaveLoadManager.Instance.GetAllSaveMetadata();

foreach (var pair in allSaves) {
    int slotNumber = pair.Key;
    SaveMetadata metadata = pair.Value;
    
    // Display in UI
    // e.g., "Slot {slotNumber}: {metadata.playerName} - {metadata.lastSaveDate}"
    // You can also display the thumbnail using metadata.thumbnailData
}
```

### Deleting a Save

To delete a save from a specific slot:

```csharp
bool deleted = SaveLoadManager.Instance.DeleteSave(2); // Delete slot 2

if (deleted) {
    Debug.Log("Save deleted successfully!");
} else {
    Debug.LogError("Failed to delete save!");
}
```

## Making Objects Saveable

### Implementing the ISaveable Interface

Any object that needs its state saved should implement the `ISaveable` interface:

```csharp
public class InventoryManager : MonoBehaviour, ISaveable
{
    private List<ItemData> playerItems = new List<ItemData>();
    
    // Game-specific methods for managing inventory...
    
    // ISaveable implementation
    public async Task<object> GetSaveDataAsync()
    {
        // Return the data that needs to be saved
        return new Dictionary<string, object>
        {
            { "playerItems", playerItems }
        };
    }
    
    public async Task LoadSaveDataAsync(object saveData)
    {
        // Cast data back to the expected type
        var data = saveData as Dictionary<string, object>;
        
        if (data != null && data.ContainsKey("playerItems"))
        {
            // Restore the saved state
            playerItems = (List<ItemData>)data["playerItems"];
        }
    }
}
```

### Registering Saveables

After implementing `ISaveable`, register your objects with the SaveLoadManager:

```csharp
// In your InventoryManager component
void Start()
{
    // Register this object for saving/loading
    SaveLoadManager.Instance.RegisterSaveable(this, "playerInventory");
}

void OnDestroy()
{
    // Unregister when destroyed
    SaveLoadManager.Instance.UnregisterSaveable("playerInventory");
}
```

The string identifier ("playerInventory" in this example) must be unique across all registered saveables.

## Autosaves

### Creating an Autosave

To automatically save the game at key points (after completing an objective, entering a new area, etc.):

```csharp
public async void OnMissionComplete()
{
    // Award rewards, update progress, etc.

    // Then autosave
    bool success = await SaveLoadManager.Instance.AutosaveAsync();
    
    if (success) {
        ShowAutosaveIcon(); // Show a UI indicator
    }
}
```

### Loading the Latest Autosave

```csharp
// Get the autosave slot
int autosaveSlot = SaveLoadManager.Instance.GetAllSaveMetadata()
    .Where(pair => pair.Value.isAutosave)
    .Select(pair => pair.Key)
    .FirstOrDefault(-1);

if (autosaveSlot != -1) {
    await SaveLoadManager.Instance.LoadGameAsync(autosaveSlot);
}
```

## Save File Management

### Save File Location

Save files are stored in `Application.persistentDataPath/Saves/` with backups in a `Backups` subfolder. Each save slot gets its own file.

### Save File Structure

Each save file contains:
- **Metadata**: Player name, creation date, last save date, version, play time, thumbnail
- **Saveable Object Data**: Data from all registered ISaveable objects
- **System Data**: Data from major game systems like:
  - Player Progression
  - Relationship Network
  - Character System
  - Resource Management
  - and more...

## Integration with Game Systems

The SaveLoadManager automatically integrates with various game systems:

### Supported Systems

- **Player Progression System**: Character stats, skills, abilities
- **Relationship Network**: Character relationships and social connections
- **Character System**: NPC states and character-specific data
- **NPC Lifecycle System**: Daily routines and schedules
- **Resource Management System**: Inventory and resource states
- **Life Resource System**: Time-based resources management
- **Social Activity System**: Social interactions and activities
- **Progression and Event System**: Quest states and event progress

No additional code is needed to integrate with these systems if they follow the expected API conventions. The SaveLoadManager will automatically collect and restore data from these systems during save/load operations.

## Advanced Features

### Save Thumbnails

By default, the system captures a screenshot when saving (if `includeThumbnail` is true):

```csharp
// Save with thumbnail
await SaveLoadManager.Instance.SaveGameAsync(0, includeThumbnail: true);
```

The thumbnail is stored as a base64-encoded string and can be converted back to a texture for display in load menus.

### Encryption and Compression

Save files can be encrypted and compressed to protect them from tampering and reduce file size:

- **Encryption**: Toggle with `_useEncryption` (uses XOR encryption with the provided key)
- **Compression**: Toggle with `_useCompression` (uses GZip compression)

Both options are enabled by default but can be disabled for easier debugging or performance reasons.

### Backup System

The backup system creates copies of save files before overwriting them:

- **Backups**: Toggle with `_createBackups`
- **Maximum Backups**: Control with `_maxBackupCount`

If a save file becomes corrupted, the system automatically tries to load from the most recent backup.

## Troubleshooting

### Common Issues

1. **Save Failed**: 
   - Check if the save directory is writable
   - Ensure your saveables don't throw exceptions in GetSaveDataAsync()
   - Verify that serializable objects are properly marked [Serializable]

2. **Load Failed**:
   - Confirm the save file exists with DoesSaveExist()
   - Check if the save file is from a compatible game version
   - Verify that saveables properly handle loading in LoadSaveDataAsync()

3. **Missing Data After Load**:
   - Ensure objects are registered before saving
   - Check that saveables use the same ID for saving and loading
   - Verify that serializable classes handle null values gracefully

### Debugging

Enable `_verboseLogging` in the SaveLoadManager Inspector to see detailed log messages during save/load operations.

---

This guide covers the essential aspects of using the Save and Load System. For specific implementation details or customizations, refer to the comment documentation in the `SaveLoadManager.cs` file.