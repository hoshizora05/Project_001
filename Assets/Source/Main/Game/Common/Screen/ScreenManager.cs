using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central manager that handles screen instantiation and fades.
/// </summary>
public class ScreenManager : MonoBehaviour
{
    public enum ScreenType
    {
        Conversation,
        Config,
        // Add more screens if needed, e.g., Title, Menu, etc.
    }

    [Header("Screen Prefabs")]
    [SerializeField] private List<ScreenInfo> screenInfoList = new List<ScreenInfo>();

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Where to spawn UI screens (Canvas, etc.)")]
    [SerializeField] private Transform screenParent;

    // Holds the instances we've created so far
    private Dictionary<ScreenType, BaseScreen> screenInstances = new Dictionary<ScreenType, BaseScreen>();
    private BaseScreen currentScreen = null;
    private Coroutine transitionRoutine = null;

    /// <summary>
    /// Show the given screen, fading out the current one (if any),
    /// then fading in the requested screen type.
    /// </summary>
    public void ShowScreen(ScreenType screenType)
    {
        // If we're already transitioning, stop it (to prevent overlap).
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        // Start a new routine that does fade-out then fade-in
        transitionRoutine = StartCoroutine(TransitionScreens(screenType));
    }

    private IEnumerator TransitionScreens(ScreenType screenType)
    {
        // 1) If there's a current screen, fade it out
        if (currentScreen != null)
        {
            yield return FadeOut(currentScreen.gameObject);
            currentScreen.gameObject.SetActive(false);
            currentScreen = null;
        }

        // 2) Check if we've already created an instance of the requested screen
        if (!screenInstances.TryGetValue(screenType, out BaseScreen nextScreen))
        {
            // If not, find its prefab in screenInfoList
            ScreenInfo info = screenInfoList.FirstOrDefault(si => si.screenType == screenType);
            if (info == null)
            {
                Debug.LogError("No prefab found for screen type: " + screenType);
                yield break;
            }

            // Instantiate that prefab
            nextScreen = Instantiate(info.prefab, screenParent);
            // Optionally rename it so itÅfs clear in the hierarchy
            nextScreen.gameObject.name = screenType.ToString() + "Screen";

            // Initialize alpha to 0 if you like
            var cg = nextScreen.GetComponent<CanvasGroup>();
            if (cg == null) cg = nextScreen.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // Store this instance
            screenInstances[screenType] = nextScreen;
        }

        // 3) Fade in the new screen
        nextScreen.gameObject.SetActive(true);
        yield return FadeIn(nextScreen.gameObject);

        currentScreen = nextScreen;
        transitionRoutine = null;
    }

    /// <summary>
    /// Hide the current screen (if any) by fading out, then set it inactive.
    /// </summary>
    public void HideCurrentScreen()
    {
        if (currentScreen == null) return;
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
        StartCoroutine(HideScreenRoutine(currentScreen));
    }

    private IEnumerator HideScreenRoutine(BaseScreen screenToHide)
    {
        yield return FadeOut(screenToHide.gameObject);
        screenToHide.gameObject.SetActive(false);
        if (currentScreen == screenToHide)
        {
            currentScreen = null;
        }
    }

    // ---------------- Fade Helper Methods ----------------

    private IEnumerator FadeOut(GameObject target)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();

        float startAlpha = cg.alpha;
        float timeElapsed = 0f;
        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / fadeDuration);
            cg.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        cg.alpha = 0f;

        // Optionally disable interaction
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    private IEnumerator FadeIn(GameObject target)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();

        cg.interactable = false;
        cg.blocksRaycasts = false;

        float startAlpha = cg.alpha;
        float timeElapsed = 0f;
        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / fadeDuration);
            cg.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }
        cg.alpha = 1f;

        // re-enable interaction
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }
}
