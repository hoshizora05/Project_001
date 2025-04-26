using System;
using UnityEngine;

/// <summary>
/// Data structure holding information about a screen prefab and its type.
/// Used by the ScreenManager to instantiate the correct prefab for a given screen type.
/// </summary>
[Serializable]
public class ScreenInfo
{
    /// <summary>
    /// The type of screen this prefab represents
    /// </summary>
    public ScreenManager.ScreenType screenType;

    /// <summary>
    /// The prefab that will be instantiated for this screen type
    /// </summary>
    public BaseScreen prefab;

    /// <summary>
    /// Optional settings for this screen type
    /// </summary>
    [Header("Optional Settings")]
    public bool keepInMemory = true;

    /// <summary>
    /// Whether this screen should be preloaded at initialization
    /// </summary>
    public bool preloadOnStart = false;

    /// <summary>
    /// Default transition type for this screen (if different from global default)
    /// </summary>
    public ScreenManager.TransitionType customTransition = ScreenManager.TransitionType.Fade;

    /// <summary>
    /// Whether to use custom transition settings
    /// </summary>
    public bool useCustomTransition = false;

    /// <summary>
    /// Whether this screen should block inputs to screens beneath it
    /// </summary>
    public bool blockRaycasts = true;
}