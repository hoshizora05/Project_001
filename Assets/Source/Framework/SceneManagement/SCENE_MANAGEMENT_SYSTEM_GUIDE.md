# Scene Management Framework User Guide

## Introduction

This document provides a comprehensive guide to using the Scene Management Framework for Unity. This framework offers a robust and flexible way to manage scene transitions in your Unity projects with built-in support for smooth transitions, type-safe parameter passing, and scene history management.

## Table of Contents

1. [System Overview](#system-overview)
2. [Getting Started](#getting-started)
3. [Creating Custom Scenes](#creating-custom-scenes)
4. [Scene Navigation](#scene-navigation)
5. [Working with Scene Parameters](#working-with-scene-parameters)
6. [Transition Effects](#transition-effects)
7. [Loading Screen](#loading-screen)
8. [Integration with Unity Scenes](#integration-with-unity-scenes)
9. [Best Practices](#best-practices)
10. [Troubleshooting](#troubleshooting)

## System Overview

The Scene Management Framework consists of several components that work together to provide a seamless scene management experience:

- **SceneManager**: Central controller managing scene transitions, history, and state
- **Scene**: Base class for all scenes in your application
- **SceneFactory**: Responsible for creating and managing scene instances
- **Transition Effects**: Customizable visual effects during scene transitions
- **Loading Screen**: Visual indicator during asynchronous operations
- **Scene Utility**: Helper functions for integrating with Unity's built-in scene system

## Getting Started

### Setting Up the System

1. **Add the Initializer**:
   
   Add the `SceneManagerInitializer` component to a GameObject in your initial scene:

   ```csharp
   // Create a GameObject to hold the initializer
   GameObject initializerObj = new GameObject("SceneManagerInitializer");
   initializerObj.AddComponent<SceneManagerInitializer>();
   ```

2. **Configure Loading Screen**:

   Create a loading screen prefab with the `LoadingScreen` component attached. Assign it to the `_loadingScreenPrefab` field in the `SceneManagerInitializer` component.

3. **Register Scene Prefabs**:

   In the Inspector, add entries to the `_scenePrefabs` list in the `SceneManagerInitializer` component. Each entry should include a scene name and a prefab with the scene component attached.

4. **Set Initial Scene**:

   Specify the name of the initial scene in the `_initialSceneName` field of the `SceneManagerInitializer` component.

### Basic Setup in Code

```csharp
// Custom initialization
SceneManager sceneManager = SceneManager.Instance;

// Register a scene prefab
GameObject myScenePrefab = Resources.Load<GameObject>("Prefabs/MyScene");
sceneManager.RegisterScene<MyScene>(myScenePrefab);

// Show the first scene
await sceneManager.ShowScene<MyScene>();
```

## Creating Custom Scenes

### Simple Scene Without Parameters

```csharp
using UnityEngine;
using SceneManagement;

public class MenuScene : Scene
{
    [SerializeField] private Button _startButton;
    
    protected override void OnInitialize()
    {
        // Initialize the scene
        _startButton.onClick.AddListener(OnStartButtonClicked);
    }
    
    private async void OnStartButtonClicked()
    {
        // Navigate to another scene
        await SceneManager.Instance.ShowScene<GameScene>();
    }
    
    protected override async Task OnShow()
    {
        // Code that runs when scene becomes visible
        // Animations, sound effects, etc.
        await base.OnShow();
    }
    
    protected override async Task OnHide()
    {
        // Code that runs when scene is hidden
        await base.OnHide();
    }
    
    protected override async Task OnFinalize()
    {
        // Cleanup code
        _startButton.onClick.RemoveAllListeners();
        await base.OnFinalize();
    }
}
```

### Scene With Parameters

```csharp
// Define the parameters class
public class LevelSceneParams
{
    public int LevelNumber { get; set; }
    public float Difficulty { get; set; }
    public bool IsTutorial { get; set; }
}

// Create the scene class
public class LevelScene : Scene<LevelSceneParams>
{
    [SerializeField] private Text _levelText;
    
    protected override void OnInitialize()
    {
        // Access the parameters
        _levelText.text = $"Level {Parameters.LevelNumber}";
        
        // Apply difficulty settings
        if (Parameters.IsTutorial)
        {
            // Set up tutorial elements
        }
    }
}
```

## Scene Navigation

### Showing a Scene

```csharp
// Show a scene without parameters
await SceneManager.Instance.ShowScene<MenuScene>();

// Show a scene with parameters
await SceneManager.Instance.ShowScene<LevelScene, LevelSceneParams>(
    new LevelSceneParams
    {
        LevelNumber = 5,
        Difficulty = 0.8f,
        IsTutorial = false
    }
);

// Show a scene without adding to history (won't be available via GoBack())
await SceneManager.Instance.ShowScene<SettingsScene>(addToHistory: false);
```

### Going Back to Previous Scene

```csharp
// Go back to the previous scene
bool success = await SceneManager.Instance.GoBack();

// Check if the operation was successful
if (!success)
{
    // No history or navigation failed
    Debug.Log("Could not go back to previous scene");
}
```

### Clearing History

```csharp
// Clear the scene history
SceneManager.Instance.ClearHistory();
```

## Working with Scene Parameters

### Defining Parameter Classes

```csharp
// Simple parameters
public class CharacterSelectionParams
{
    public string[] AvailableCharacters { get; set; }
    public int DefaultSelectionIndex { get; set; }
}

// Parameters with methods
public class GameplayParams
{
    public string LevelId { get; set; }
    public Dictionary<string, float> GameSettings { get; set; }
    
    public GameplayParams()
    {
        // Default values
        GameSettings = new Dictionary<string, float>();
    }
    
    public void ApplyDifficulty(float difficulty)
    {
        GameSettings["EnemyStrength"] = 1.0f + (difficulty * 0.5f);
        GameSettings["ResourceMultiplier"] = 1.0f - (difficulty * 0.2f);
    }
}
```

### Accessing Parameters in Scenes

```csharp
public class GameScene : Scene<GameplayParams>
{
    protected override void OnInitialize()
    {
        // Access parameters
        string levelId = Parameters.LevelId;
        
        // Access settings
        float enemyStrength = Parameters.GameSettings["EnemyStrength"];
        
        // Configure the scene based on parameters
        LoadLevel(levelId);
        SetEnemyDifficulty(enemyStrength);
    }
}
```

## Transition Effects

### Using the Default Fade Effect

The framework includes a built-in fade transition effect that works with a CanvasGroup component:

```csharp
// Create a canvas group for transitions
CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

// Create a fade transition effect
ITransitionEffect fadeEffect = new FadeTransitionEffect(canvasGroup, fadeDuration: 0.5f);

// Set it as the current transition effect
SceneManager.Instance.SetTransitionEffect(fadeEffect);
```

### Creating Custom Transition Effects

You can create custom transition effects by implementing the `ITransitionEffect` interface:

```csharp
public class SlideTransitionEffect : ITransitionEffect
{
    private readonly RectTransform _panel;
    private readonly float _duration;
    
    public SlideTransitionEffect(RectTransform panel, float duration = 0.5f)
    {
        _panel = panel;
        _duration = duration;
    }
    
    public async Task PlayEnteringEffect()
    {
        // Slide in from right to left
        Vector2 startPos = new Vector2(Screen.width, 0);
        Vector2 endPos = Vector2.zero;
        
        await AnimatePosition(startPos, endPos);
    }
    
    public async Task PlayExitingEffect()
    {
        // Slide out from left to right
        Vector2 startPos = Vector2.zero;
        Vector2 endPos = new Vector2(-Screen.width, 0);
        
        await AnimatePosition(startPos, endPos);
    }
    
    private async Task AnimatePosition(Vector2 startPos, Vector2 endPos)
    {
        float elapsedTime = 0;
        _panel.anchoredPosition = startPos;
        
        while (elapsedTime < _duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _duration);
            _panel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            await Task.Yield();
        }
        
        _panel.anchoredPosition = endPos;
    }
}
```

## Loading Screen

### Creating a Loading Screen

Create a prefab with the following components:

1. Canvas with a CanvasGroup component
2. Loading Screen component attached to the root GameObject
3. Optional progress bar (Image with fill amount)
4. Optional progress text

```csharp
// LoadingScreen component configuration
[SerializeField] private CanvasGroup _canvasGroup;
[SerializeField] private Image _progressBar;
[SerializeField] private Text _progressText;
```

### Customizing the Loading Screen

You can customize the loading screen in your own implementation:

```csharp
public class CustomLoadingScreen : LoadingScreen
{
    [SerializeField] private ParticleSystem _loadingParticles;
    
    // Override the Show method to add custom effects
    public override async Task Show()
    {
        _loadingParticles.Play();
        await base.Show();
    }
    
    // Override the Hide method to add custom effects
    public override async Task Hide()
    {
        await base.Hide();
        _loadingParticles.Stop();
    }
    
    // Override the UpdateProgress method to add custom behavior
    public override void UpdateProgress(float progress)
    {
        base.UpdateProgress(progress);
        
        // Custom progress visualization
        _loadingParticles.emissionRate = progress * 100;
    }
}
```

## Integration with Unity Scenes

### Loading Unity Scenes

The framework provides utilities for loading Unity scenes alongside the managed scenes:

```csharp
// Load a Unity scene
await SceneUtility.LoadUnitySceneAsync("MainLevel");

// Load a Unity scene with progress updates
await SceneUtility.LoadUnitySceneAsync("MainLevel", LoadSceneMode.Additive, 
    progress => Debug.Log($"Loading progress: {progress * 100}%"));

// Load a Unity scene with the loading screen
await SceneUtility.LoadUnitySceneWithLoadingScreenAsync("MainLevel");
```

### Unloading Unity Scenes

```csharp
// Unload a Unity scene
await SceneUtility.UnloadSceneAsync("MainLevel");
```

## Best Practices

### Scene Organization

1. **Create a Scene Hierarchy**:
   - Group scenes by functionality (e.g., Menu, Gameplay, Settings)
   - Use consistent naming conventions

2. **Scene Prefab Structure**:
   - Keep scene prefabs lightweight
   - Put resource-intensive elements in separate prefabs that can be instantiated on demand

### Performance Optimization

1. **Lazy Loading**:
   - Load heavy resources in the OnShow method instead of OnInitialize
   - Unload resources in OnHide or OnFinalize

2. **Scene Pooling**:
   - For frequently used scenes, consider keeping them hidden instead of destroying them

### Clean Architecture

1. **Separation of Concerns**:
   - Keep scene classes focused on presentation logic
   - Move business logic to separate service classes that can be injected

2. **Scene Communication**:
   - Use parameters for direct scene-to-scene communication
   - Consider using events for more decoupled communication

## Troubleshooting

### Common Issues

1. **Scene Not Showing**:
   - Ensure the scene prefab is registered with the SceneManager
   - Check for errors during initialization or OnShow

2. **Transition Effects Not Working**:
   - Verify the CanvasGroup is properly set up
   - Check that the transition effect is correctly assigned

3. **Parameters Not Being Passed**:
   - Ensure the parameter class is properly defined (must be a reference type)
   - Check that the parameter object is being correctly instantiated

### Debugging Tips

1. **Scene State Visualization**:
   - Create a debug UI that shows the current scene and history stack
   - Log scene lifecycle events (OnInitialize, OnShow, etc.)

2. **Performance Monitoring**:
   - Use the Unity Profiler to identify bottlenecks in scene transitions
   - Monitor memory usage during scene loading/unloading

---

## Conclusion

This Scene Management Framework provides a robust foundation for managing scenes in your Unity project. By leveraging the framework's features, you can create smooth, professional scene transitions while maintaining clean, modular code.

For more advanced usage or custom extensions, you can extend the base classes or implement the provided interfaces to tailor the framework to your specific needs.