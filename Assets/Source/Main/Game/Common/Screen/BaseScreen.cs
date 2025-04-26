using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

/// <summary>
/// Base class for all UI screens managed by the ScreenManager.
/// Provides lifecycle methods and common functionality for screens.
/// </summary>
public class BaseScreen : MonoBehaviour
{
    #region Properties and Fields
    /// <summary>
    /// Type identifier for this screen instance
    /// </summary>
    public ScreenManager.ScreenType ScreenType { get; set; }

    /// <summary>
    /// Reference to the screen manager (for convenience)
    /// </summary>
    protected ScreenManager ScreenManager => ScreenManager.Instance;

    /// <summary>
    /// Canvas group component for transitions
    /// </summary>
    protected CanvasGroup CanvasGroup { get; private set; }

    /// <summary>
    /// Flag indicating if this screen has been initialized
    /// </summary>
    protected bool IsInitialized { get; private set; }

    /// <summary>
    /// Flag indicating if this screen is currently active/visible
    /// </summary>
    public bool IsActive => gameObject.activeSelf && CanvasGroup.alpha > 0;

    /// <summary>
    /// Data that might be passed to this screen
    /// </summary>
    protected object ScreenData { get; private set; }
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        // Ensure we have a CanvasGroup
        CanvasGroup = GetComponent<CanvasGroup>();
        if (CanvasGroup == null)
        {
            CanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    protected virtual void OnEnable()
    {
        // Hook up UI element events if needed
    }

    protected virtual void OnDisable()
    {
        // Unhook UI element events if needed
    }
    #endregion

    #region Screen Lifecycle Methods
    /// <summary>
    /// Initialize the screen. Called only once when the screen is instantiated.
    /// Used for one-time setup operations.
    /// </summary>
    public virtual void Initialize()
    {
        if (IsInitialized) return;

        // Cache references to UI elements
        // Set up event listeners

        IsInitialized = true;
    }

    /// <summary>
    /// Called before the screen becomes visible. Used to prepare the screen state.
    /// </summary>
    public virtual void OnPrepare()
    {
        // Prepare screen state before it becomes visible
        // Reset UI elements, load initial data, etc.
    }

    /// <summary>
    /// Called when the screen becomes fully visible after transitions.
    /// Used to start animations, sounds, or other post-show effects.
    /// </summary>
    public virtual void OnShow()
    {
        // Screen is now fully visible
        // Start animations, play sounds, etc.
    }

    /// <summary>
    /// Called when the screen is about to be hidden.
    /// Used to clean up temporary state or prepare for hiding.
    /// </summary>
    public virtual void OnHide()
    {
        // Screen is about to be hidden
        // Pause animations, cancel operations, etc.
    }

    /// <summary>
    /// Called when the screen is being destroyed.
    /// Used for final cleanup before the object is destroyed.
    /// </summary>
    public virtual void OnFinalize()
    {
        // Final cleanup when screen is being destroyed
        // Unsubscribe from events, release resources, etc.
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Set data for this screen
    /// </summary>
    /// <param name="data">Data to pass to the screen</param>
    public virtual void SetData(object data)
    {
        ScreenData = data;
    }

    /// <summary>
    /// Close this screen
    /// </summary>
    /// <param name="navigateBack">Whether to navigate to the previous screen</param>
    public virtual async Task CloseScreen(bool navigateBack = true)
    {
        if (navigateBack)
        {
            await ScreenManager.GoBackAsync();
        }
        else
        {
            await ScreenManager.HideCurrentScreenAsync();
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Find a child component by name
    /// </summary>
    protected T GetChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            return child.GetComponent<T>();
        }
        return null;
    }

    /// <summary>
    /// Set up a simple button click handler
    /// </summary>
    protected void SetupButton(string buttonName, System.Action onClickAction)
    {
        Button button = GetChildComponent<Button>(buttonName);
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickAction?.Invoke());
        }
    }

    /// <summary>
    /// Navigate to another screen
    /// </summary>
    protected async Task NavigateToScreen(ScreenManager.ScreenType screenType, bool addToHistory = true)
    {
        await ScreenManager.ShowScreenAsync(screenType, addToHistory);
    }

    /// <summary>
    /// Add a fade animation to a UI element
    /// </summary>
    protected async Task FadeElement(Graphic element, float fromAlpha, float toAlpha, float duration)
    {
        if (element == null) return;

        Color startColor = element.color;
        Color endColor = startColor;
        startColor.a = fromAlpha;
        endColor.a = toAlpha;

        element.color = startColor;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            element.color = Color.Lerp(startColor, endColor, t);
            await Task.Yield();
        }

        element.color = endColor;
    }

    /// <summary>
    /// Run a simple animation on a RectTransform
    /// </summary>
    protected async Task AnimateRectTransform(RectTransform rect, Vector2 fromPosition, Vector2 toPosition, float duration)
    {
        if (rect == null) return;

        rect.anchoredPosition = fromPosition;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            rect.anchoredPosition = Vector2.Lerp(fromPosition, toPosition, t);
            await Task.Yield();
        }

        rect.anchoredPosition = toPosition;
    }
    #endregion
}