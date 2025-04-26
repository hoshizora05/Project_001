using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Central manager that handles UI screen instantiation, transitions, and animations.
/// Integrates with various systems for coordinated UI state management.
/// </summary>
public class ScreenManager : MonoBehaviour
{
    #region Enums and Types
    /// <summary>
    /// Types of screens available in the application
    /// </summary>
    public enum ScreenType
    {
        None,
        MainMenu,
        Conversation,
        Inventory,
        Character,
        Relationship,
        Calendar,
        Quest,
        Config,
        Loading,
        StatusManagement
    }

    /// <summary>
    /// Transition types for screen animations
    /// </summary>
    public enum TransitionType
    {
        Fade,
        SlideLeft,
        SlideRight,
        SlideUp,
        SlideDown,
        Zoom
    }
    #endregion

    #region Serialized Fields
    [Header("Screen Prefabs")]
    [SerializeField] private List<ScreenInfo> screenInfoList = new List<ScreenInfo>();

    [Header("Transition Settings")]
    [SerializeField] private TransitionType defaultTransition = TransitionType.Fade;
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("UI Hierarchy")]
    [SerializeField] private Transform screenParent;
    [SerializeField] private Transform popupParent;
    [SerializeField] private Canvas rootCanvas;

    [Header("Options")]
    [SerializeField] private bool cacheScreens = true;
    [SerializeField] private bool alwaysKeepLoadingScreen = true;
    [SerializeField] private bool pauseGameWhenOverlayActive = true;
    #endregion

    #region Private Variables
    // Screen instances and state tracking
    private Dictionary<ScreenType, BaseScreen> screenInstances = new Dictionary<ScreenType, BaseScreen>();
    private BaseScreen currentScreen = null;
    private Stack<ScreenType> screenHistory = new Stack<ScreenType>();
    private Coroutine transitionRoutine = null;
    private bool isTransitioning = false;

    // Optional overlays and popups
    private List<BaseScreen> activeOverlays = new List<BaseScreen>();
    private float previousTimeScale = 1f;

    // System integration
    private bool hasInitialized = false;
    #endregion

    #region Singleton Pattern
    private static ScreenManager _instance;

    public static ScreenManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ScreenManager>();

                if (_instance == null)
                {
                    GameObject managerObject = new GameObject("ScreenManager");
                    _instance = managerObject.AddComponent<ScreenManager>();
                    DontDestroyOnLoad(managerObject);
                }
            }

            return _instance;
        }
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure required components
        if (screenParent == null)
        {
            screenParent = transform;
        }

        if (popupParent == null)
        {
            GameObject popupObject = new GameObject("PopupContainer");
            popupObject.transform.SetParent(transform, false);
            popupParent = popupObject.transform;
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                Debug.LogWarning("ScreenManager: No root canvas found. Some features may not work correctly.");
            }
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        CleanupScreenInstances();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Initialize the screen manager and prepare essential screens
    /// </summary>
    public void Initialize()
    {
        if (hasInitialized) return;

        // Pre-instantiate loading screen if configured to always keep it
        if (alwaysKeepLoadingScreen)
        {
            GetScreenInstance(ScreenType.Loading, false);
        }

        hasInitialized = true;
    }

    /// <summary>
    /// Show the given screen with default transition settings
    /// </summary>
    /// <param name="screenType">The type of screen to show</param>
    /// <param name="addToHistory">Whether to add current screen to history for back navigation</param>
    /// <returns>Task representing the screen transition operation</returns>
    public async Task<BaseScreen> ShowScreenAsync(ScreenType screenType, bool addToHistory = true)
    {
        return await ShowScreenAsync(screenType, defaultTransition, transitionDuration, addToHistory);
    }

    /// <summary>
    /// Show the given screen with custom transition settings
    /// </summary>
    /// <param name="screenType">The type of screen to show</param>
    /// <param name="transitionType">The type of transition animation</param>
    /// <param name="duration">Duration of the transition in seconds</param>
    /// <param name="addToHistory">Whether to add current screen to history for back navigation</param>
    /// <returns>Task representing the screen transition operation</returns>
    public async Task<BaseScreen> ShowScreenAsync(ScreenType screenType, TransitionType transitionType, float duration, bool addToHistory = true)
    {
        // If we're already showing this screen, do nothing
        if (currentScreen != null && currentScreen.ScreenType == screenType && !isTransitioning)
        {
            return currentScreen;
        }

        // Prevent multiple transitions at once
        if (isTransitioning)
        {
            Debug.LogWarning("Screen transition already in progress. Request for " + screenType + " ignored.");
            return null;
        }

        isTransitioning = true;

        // Add current screen to history if needed
        if (currentScreen != null && addToHistory)
        {
            screenHistory.Push(currentScreen.ScreenType);
        }

        // Get or instantiate the requested screen
        BaseScreen nextScreen = GetScreenInstance(screenType, true);
        if (nextScreen == null)
        {
            isTransitioning = false;
            return null;
        }

        // Prepare the screen before showing it
        nextScreen.gameObject.SetActive(true);
        nextScreen.OnPrepare();

        // If we have a current screen, transition out
        if (currentScreen != null)
        {
            await TransitionScreensAsync(currentScreen, nextScreen, transitionType, duration);

            if (!cacheScreens && currentScreen.ScreenType != ScreenType.Loading)
            {
                currentScreen.OnFinalize();
                Destroy(currentScreen.gameObject);
                screenInstances.Remove(currentScreen.ScreenType);
            }
            else
            {
                currentScreen.gameObject.SetActive(false);
                currentScreen.OnHide();
            }
        }
        else
        {
            // Just fade in the new screen if there's no current screen
            await FadeInAsync(nextScreen.gameObject, duration);
        }

        // Update current screen reference
        currentScreen = nextScreen;
        currentScreen.OnShow();

        isTransitioning = false;
        return currentScreen;
    }

    /// <summary>
    /// Navigate back to the previous screen in the history
    /// </summary>
    /// <returns>True if back navigation succeeded, false if no history available</returns>
    public async Task<bool> GoBackAsync()
    {
        if (screenHistory.Count == 0)
        {
            return false;
        }

        ScreenType previousScreen = screenHistory.Pop();
        await ShowScreenAsync(previousScreen, false);
        return true;
    }

    /// <summary>
    /// Show an overlay on top of the current screen
    /// </summary>
    /// <param name="overlayPrefab">The overlay screen prefab to instantiate</param>
    /// <returns>The instantiated overlay BaseScreen component</returns>
    public async Task<BaseScreen> ShowOverlayAsync(BaseScreen overlayPrefab)
    {
        BaseScreen overlay = Instantiate(overlayPrefab, popupParent);
        activeOverlays.Add(overlay);

        if (pauseGameWhenOverlayActive && activeOverlays.Count == 1)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        overlay.OnPrepare();
        await FadeInAsync(overlay.gameObject, transitionDuration / 2f);
        overlay.OnShow();

        return overlay;
    }

    /// <summary>
    /// Hide and remove an overlay
    /// </summary>
    /// <param name="overlay">The overlay to hide</param>
    public async Task HideOverlayAsync(BaseScreen overlay)
    {
        if (!activeOverlays.Contains(overlay))
        {
            return;
        }

        await FadeOutAsync(overlay.gameObject, transitionDuration / 2f);

        activeOverlays.Remove(overlay);
        overlay.OnHide();
        overlay.OnFinalize();
        Destroy(overlay.gameObject);

        if (pauseGameWhenOverlayActive && activeOverlays.Count == 0)
        {
            Time.timeScale = previousTimeScale;
        }
    }

    /// <summary>
    /// Hide all active overlays
    /// </summary>
    public async Task HideAllOverlaysAsync()
    {
        List<BaseScreen> overlaysCopy = new List<BaseScreen>(activeOverlays);
        foreach (var overlay in overlaysCopy)
        {
            await HideOverlayAsync(overlay);
        }
    }

    /// <summary>
    /// Hide the current screen without showing another one
    /// </summary>
    public async Task HideCurrentScreenAsync()
    {
        if (currentScreen == null) return;

        await FadeOutAsync(currentScreen.gameObject, transitionDuration);

        currentScreen.gameObject.SetActive(false);
        currentScreen.OnHide();
        currentScreen = null;
    }

    /// <summary>
    /// Get the current active screen
    /// </summary>
    public BaseScreen GetCurrentScreen()
    {
        return currentScreen;
    }

    /// <summary>
    /// Get an instance of a screen without showing it
    /// </summary>
    /// <param name="screenType">The type of screen to get</param>
    /// <param name="createIfNeeded">Whether to create the screen if it doesn't exist</param>
    /// <returns>The BaseScreen instance or null if not found/created</returns>
    public BaseScreen GetScreenInstance(ScreenType screenType, bool createIfNeeded = true)
    {
        // Check if we already have an instance
        if (screenInstances.TryGetValue(screenType, out BaseScreen screenInstance))
        {
            return screenInstance;
        }

        // If we don't want to create it, return null
        if (!createIfNeeded)
        {
            return null;
        }

        // Find the corresponding prefab
        ScreenInfo screenInfo = screenInfoList.FirstOrDefault(si => si.screenType == screenType);
        if (screenInfo == null || screenInfo.prefab == null)
        {
            Debug.LogError($"No prefab found for screen type: {screenType}");
            return null;
        }

        // Instantiate and set up the screen
        BaseScreen newScreen = Instantiate(screenInfo.prefab, screenParent);
        newScreen.gameObject.name = $"{screenType}Screen";

        // Initialize the screen
        newScreen.ScreenType = screenType;
        newScreen.Initialize();

        // Ensure it has a CanvasGroup
        CanvasGroup canvasGroup = newScreen.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = newScreen.gameObject.AddComponent<CanvasGroup>();
        }

        // Set initial state
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        newScreen.gameObject.SetActive(false);

        // Store in our dictionary
        screenInstances[screenType] = newScreen;

        return newScreen;
    }

    /// <summary>
    /// Clear screen history
    /// </summary>
    public void ClearHistory()
    {
        screenHistory.Clear();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Handle the transition between two screens
    /// </summary>
    private async Task TransitionScreensAsync(BaseScreen fromScreen, BaseScreen toScreen, TransitionType transitionType, float duration)
    {
        switch (transitionType)
        {
            case TransitionType.Fade:
                await FadeTransitionAsync(fromScreen, toScreen, duration);
                break;
            case TransitionType.SlideLeft:
                await SlideTransitionAsync(fromScreen, toScreen, Vector2.left, duration);
                break;
            case TransitionType.SlideRight:
                await SlideTransitionAsync(fromScreen, toScreen, Vector2.right, duration);
                break;
            case TransitionType.SlideUp:
                await SlideTransitionAsync(fromScreen, toScreen, Vector2.up, duration);
                break;
            case TransitionType.SlideDown:
                await SlideTransitionAsync(fromScreen, toScreen, Vector2.down, duration);
                break;
            case TransitionType.Zoom:
                await ZoomTransitionAsync(fromScreen, toScreen, duration);
                break;
            default:
                await FadeTransitionAsync(fromScreen, toScreen, duration);
                break;
        }
    }

    private async Task FadeTransitionAsync(BaseScreen fromScreen, BaseScreen toScreen, float duration)
    {
        // Set up initial state
        CanvasGroup fromCG = fromScreen.GetComponent<CanvasGroup>();
        CanvasGroup toCG = toScreen.GetComponent<CanvasGroup>();

        fromCG.alpha = 1f;
        toCG.alpha = 0f;

        toScreen.gameObject.SetActive(true);
        toCG.interactable = false;
        toCG.blocksRaycasts = false;

        // Create tasks for both fade operations
        Task fadeOutTask = FadeOutAsync(fromScreen.gameObject, duration);
        Task fadeInTask = FadeInAsync(toScreen.gameObject, duration);

        // Wait for both to complete
        await Task.WhenAll(fadeOutTask, fadeInTask);
    }

    private async Task SlideTransitionAsync(BaseScreen fromScreen, BaseScreen toScreen, Vector2 direction, float duration)
    {
        // Sliding requires both screens to have RectTransform
        RectTransform fromRect = fromScreen.GetComponent<RectTransform>();
        RectTransform toRect = toScreen.GetComponent<RectTransform>();

        if (fromRect == null || toRect == null)
        {
            // Fall back to fade if RectTransforms aren't available
            await FadeTransitionAsync(fromScreen, toScreen, duration);
            return;
        }

        // Get screen dimensions from the root canvas
        float screenWidth = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>().rect.width : Screen.width;
        float screenHeight = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>().rect.height : Screen.height;

        // Calculate start and end positions
        Vector2 startPos = Vector2.zero;
        Vector2 offscreenPos = new Vector2(direction.x * screenWidth, direction.y * screenHeight);

        // Set up initial positions
        fromRect.anchoredPosition = startPos;
        toRect.anchoredPosition = -offscreenPos; // Start from opposite side

        // Show the incoming screen
        toScreen.gameObject.SetActive(true);

        // Get canvas groups
        CanvasGroup fromCG = fromScreen.GetComponent<CanvasGroup>();
        CanvasGroup toCG = toScreen.GetComponent<CanvasGroup>();

        toCG.alpha = 1f;
        toCG.interactable = false;
        toCG.blocksRaycasts = false;

        // Animate both screens simultaneously
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float curvedT = transitionCurve.Evaluate(t);

            fromRect.anchoredPosition = Vector2.Lerp(startPos, offscreenPos, curvedT);
            toRect.anchoredPosition = Vector2.Lerp(-offscreenPos, startPos, curvedT);

            await Task.Yield();
        }

        // Ensure final positions
        fromRect.anchoredPosition = offscreenPos;
        toRect.anchoredPosition = startPos;

        // Update interaction state
        toCG.interactable = true;
        toCG.blocksRaycasts = true;
    }

    private async Task ZoomTransitionAsync(BaseScreen fromScreen, BaseScreen toScreen, float duration)
    {
        RectTransform fromRect = fromScreen.GetComponent<RectTransform>();
        RectTransform toRect = toScreen.GetComponent<RectTransform>();

        if (fromRect == null || toRect == null)
        {
            // Fall back to fade if RectTransforms aren't available
            await FadeTransitionAsync(fromScreen, toScreen, duration);
            return;
        }

        // Set up initial state
        CanvasGroup fromCG = fromScreen.GetComponent<CanvasGroup>();
        CanvasGroup toCG = toScreen.GetComponent<CanvasGroup>();

        Vector3 normalScale = Vector3.one;
        Vector3 smallScale = Vector3.one * 0.8f;
        Vector3 largeScale = Vector3.one * 1.2f;

        // Initialize from screen
        fromRect.localScale = normalScale;
        fromCG.alpha = 1f;

        // Initialize to screen
        toScreen.gameObject.SetActive(true);
        toRect.localScale = smallScale;
        toCG.alpha = 0f;
        toCG.interactable = false;
        toCG.blocksRaycasts = false;

        // Perform the transition
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float curvedT = transitionCurve.Evaluate(t);

            // Current screen zooms out and fades out
            fromRect.localScale = Vector3.Lerp(normalScale, largeScale, curvedT);
            fromCG.alpha = Mathf.Lerp(1f, 0f, curvedT);

            // New screen zooms in and fades in
            toRect.localScale = Vector3.Lerp(smallScale, normalScale, curvedT);
            toCG.alpha = Mathf.Lerp(0f, 1f, curvedT);

            await Task.Yield();
        }

        // Ensure final state
        fromRect.localScale = largeScale;
        fromCG.alpha = 0f;

        toRect.localScale = normalScale;
        toCG.alpha = 1f;
        toCG.interactable = true;
        toCG.blocksRaycasts = true;
    }

    private async Task FadeOutAsync(GameObject target, float duration)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();

        float startAlpha = cg.alpha;
        float elapsedTime = 0f;

        cg.interactable = false;
        cg.blocksRaycasts = false;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float curvedT = transitionCurve.Evaluate(t);

            cg.alpha = Mathf.Lerp(startAlpha, 0f, curvedT);
            await Task.Yield();
        }

        cg.alpha = 0f;
    }

    private async Task FadeInAsync(GameObject target, float duration)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();

        float startAlpha = cg.alpha;
        float elapsedTime = 0f;

        cg.interactable = false;
        cg.blocksRaycasts = false;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float curvedT = transitionCurve.Evaluate(t);

            cg.alpha = Mathf.Lerp(startAlpha, 1f, curvedT);
            await Task.Yield();
        }

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private void CleanupScreenInstances()
    {
        foreach (var screenPair in screenInstances)
        {
            if (screenPair.Value != null)
            {
                screenPair.Value.OnFinalize();
                Destroy(screenPair.Value.gameObject);
            }
        }

        screenInstances.Clear();
        currentScreen = null;

        foreach (var overlay in activeOverlays)
        {
            if (overlay != null)
            {
                Destroy(overlay.gameObject);
            }
        }

        activeOverlays.Clear();
    }
    #endregion
}