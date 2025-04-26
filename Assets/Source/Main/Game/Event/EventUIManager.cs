using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProgressionAndEventSystem; // Namespace from the provided core system

/// <summary>
/// EventUIManager is a concrete implementation of <see cref="IEventUIManager"/>.
/// It presents event start information, dialogue/choices, progress, and results
/// through a simple, modular canvas‑based UI.  
/// The class follows an MVVM‑friendly structure: public methods act as the ViewModel,
/// and the serialized reference fields represent the View.  
/// External systems (e.g. EventManager) simply call the interface methods, keeping
/// UI concerns fully decoupled from game logic.
/// </summary>
[AddComponentMenu("Game/UI/Event UI Manager")]
public sealed class EventUIManager : MonoBehaviour, IEventUIManager
{
    #region Serialized View References
    [Header("Root Panels")]
    [Tooltip("CanvasGroup that fades in/out while an event is active.")]
    [SerializeField] private CanvasGroup _rootGroup;

    [Header("Event Start View")]
    [SerializeField] private GameObject _eventStartPanel;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("Dialogue View")]
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private TMP_Text _dialogueText;

    [Header("Choices View")]
    [SerializeField] private GameObject _choicesPanel;
    [SerializeField] private Transform _choicesContainer;
    [SerializeField] private Button _choiceButtonPrefab;

    [Header("Progress View")]
    [SerializeField] private GameObject _progressPanel;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TMP_Text _progressDescriptionText;

    [Header("Result View")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TMP_Text _resultHeaderText;
    [SerializeField] private TMP_Text _resultBodyText;

    [Header("Animation Settings")]
    [Tooltip("Seconds for fade‑in/out animations.")]
    [SerializeField] private float _fadeDuration = 0.25f;
    #endregion

    #region Private State
    private readonly Dictionary<EventUICallbackType, Action<object>> _callbacks = new();
    private GameEvent _currentEvent;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Ensure the root canvas group is hidden on startup.
        SetCanvasGroupVisible(false, immediate: true);
    }
    #endregion

    #region Public API – IEventUIManager
    public void DisplayEventStart(GameEvent gameEvent)
    {
        _currentEvent = gameEvent;

        // Activate root & specific view panels
        SetCanvasGroupVisible(true);
        _eventStartPanel.SetActive(true);
        _dialoguePanel.SetActive(false);
        _choicesPanel.SetActive(false);
        _progressPanel.SetActive(false);
        _resultPanel.SetActive(false);

        // Populate text
        _titleText.text = gameEvent.Title;
        _descriptionText.text = gameEvent.Description;

        // Notify listeners
        InvokeCallback(EventUICallbackType.EventStart, gameEvent);
    }

    public void ShowEventChoices(List<EventChoice> choices)
    {
        if (choices == null) return;

        _eventStartPanel.SetActive(false);
        _dialoguePanel.SetActive(false);
        _choicesPanel.SetActive(true);

        // Clear previous buttons
        foreach (Transform child in _choicesContainer)
        {
            Destroy(child.gameObject);
        }

        // Instantiate buttons for each choice
        foreach (var choice in choices)
        {
            Button btn = Instantiate(_choiceButtonPrefab, _choicesContainer);
            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label) label.text = choice.Text;

            // Capture local variable for closure
            var capturedChoice = choice;
            btn.onClick.AddListener(() => OnChoiceButtonClicked(capturedChoice));
        }

        InvokeCallback(EventUICallbackType.ChoicesShown, choices);
    }

    public void DisplayEventProgress(float progress, string stageDescription)
    {
        _choicesPanel.SetActive(false);
        _progressPanel.SetActive(true);

        _progressBar.value = Mathf.Clamp01(progress);
        _progressDescriptionText.text = stageDescription;

        var payload = new Dictionary<string, object>
        {
            { "progress", progress },
            { "description", stageDescription }
        };
        InvokeCallback(EventUICallbackType.ProgressUpdated, payload);
    }

    public void DisplayEventResult(EventResult result)
    {
        _progressPanel.SetActive(false);
        _resultPanel.SetActive(true);

        _resultHeaderText.text = result.Success ? "Success" : "Failure";
        _resultBodyText.text = BuildResultBody(result);

        InvokeCallback(EventUICallbackType.ResultShown, result);

        // Auto‑close after a delay (optional)
        _ = CloseRoutine(3f);
    }

    public void RegisterEventUICallback(EventUICallbackType type, Action<object> callback)
    {
        if (callback == null) return;
        _callbacks[type] = callback;
    }
    #endregion

    #region Private Helpers
    private void OnChoiceButtonClicked(EventChoice choice)
    {
        // Forward the selected choice to any subscribed system via the callback dictionary.
        if (_callbacks.TryGetValue(EventUICallbackType.ChoicesShown, out var cb))
        {
            cb?.Invoke(choice);
        }
    }

    private void InvokeCallback(EventUICallbackType type, object data)
    {
        if (_callbacks.TryGetValue(type, out var cb))
        {
            cb?.Invoke(data);
        }
    }

    private static string BuildResultBody(EventResult result)
    {
        if (result == null) return string.Empty;

        System.Text.StringBuilder sb = new();
        sb.AppendLine($"Completed Stage: {result.CompletedStageId}");

        if (result.ChoicesMade != null && result.ChoicesMade.Count > 0)
        {
            sb.AppendLine("Choices Made:");
            foreach (var c in result.ChoicesMade)
            {
                sb.AppendLine($" • {c}");
            }
        }

        if (result.AppliedEffects != null && result.AppliedEffects.Count > 0)
        {
            sb.AppendLine("Effects:");
            foreach (var kv in result.AppliedEffects)
            {
                sb.AppendLine($" • {kv.Key}: {kv.Value:+0.##;-0.##;0}");
            }
        }

        if (result.NewlyUnlockedContent != null && result.NewlyUnlockedContent.Count > 0)
        {
            sb.AppendLine("Unlocked Content:");
            foreach (var uc in result.NewlyUnlockedContent)
            {
                sb.AppendLine($" • {uc.ContentType}: {uc.Description}");
            }
        }

        return sb.ToString();
    }

    private void SetCanvasGroupVisible(bool visible, bool immediate = false)
    {
        if (_rootGroup == null) return;

        _rootGroup.interactable = visible;
        _rootGroup.blocksRaycasts = visible;

        if (immediate || Mathf.Approximately(_fadeDuration, 0f))
        {
            _rootGroup.alpha = visible ? 1f : 0f;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(_rootGroup, visible ? 1f : 0f, _fadeDuration));
        }
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration)
    {
        float startAlpha = group.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, target, elapsed / duration);
            yield return null;
        }
        group.alpha = target;
    }

    private System.Collections.IEnumerator CloseRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetCanvasGroupVisible(false);
    }
    #endregion
}
