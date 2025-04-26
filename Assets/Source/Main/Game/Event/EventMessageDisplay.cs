using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ProgressionAndEventSystem;

/// <summary>
/// MessageDisplay — 完全版 (TouchManager 対応)
/// ------------------------------------------------------------
/// * GameEventManager / EventUIManager 連携
/// * タイプライタ (1 文字ずつ表示) + スキップ
/// * TouchManager のタップでメッセージ送り
/// * 表示速度倍率変更 & ウィンドウ透過率調整
/// ------------------------------------------------------------
/// </summary>
[AddComponentMenu("Game/UI/MessageDisplay")]
public sealed class EventMessageDisplay: MonoBehaviour
{
    #region ===== Serialized Refs =====
    [Header("Event System Managers")]
    [SerializeField] private GameEventManager _eventManager;
    [SerializeField] private EventUIManager _eventUIManager;

    [Header("Touch Manager")]
    [SerializeField] private TouchManager _touchManager;

    [Header("Message Window")]
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private CanvasGroup _windowGroup;

    [Header("Typewriter Settings")]
    [SerializeField] private float _baseCharInterval = 0.03f;
    [SerializeField] private bool _allowSkip = true;

    [Header("Log UI")]
    [SerializeField] private GameObject _logPanel;
    [SerializeField] private TMP_Text _logText;

    #endregion

    #region ===== Internal State =====
    private ICharacter _player;
    private GameEvent _currentEvent;
    private readonly Queue<string> _pendingLines = new();
    private readonly List<string> _logLines = new();

    private Coroutine _typingCo;
    private bool _isTyping;
    private float _speedMul = 1f;
    #endregion

    #region ===== Unity =====
    private void Awake()
    {
        //if (_eventManager == null) _eventManager = FindFirstObjectByType<GameEventManager>();
        //if (_eventUIManager == null) _eventUIManager = FindFirstObjectByType<EventUIManager>();
        //if (_touchManager == null) _touchManager = TouchManager.Instance ?? FindFirstObjectByType<TouchManager>();

        //if (_eventManager == null || _eventUIManager == null || _touchManager == null || _messageText == null || _windowGroup == null)
        //{
        //    Debug.LogError("[MessageDisplay] 必要な参照が不足しています。");
        //    enabled = false;
        //    return;
        //}

        SetEvent();

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        ResetEvent();
    }

    private void OnEnable()
    {
    }
    private void OnDisable()
    {
        ResetEvent();
    }

    #endregion

    #region ===== Public API =====

    public void Initialized(GameEventManager gameEventManager , EventUIManager eventUIManager )
    {
        _eventManager = gameEventManager;
        _eventUIManager = eventUIManager;
        SetEvent();
    }
    public void StartEvent(string eventId, ICharacter player)
    {
        if (string.IsNullOrEmpty(eventId) || player == null) return;
        _player = player;
        _eventManager.TriggerEvent(eventId, player);
    }

    public void SetTypingSpeedMultiplier(float mul) => _speedMul = Mathf.Max(0.1f, mul);
    public void SetWindowAlpha(float a) => _windowGroup.alpha = Mathf.Clamp01(a);

    public void ToggleLog()
    {
        if (_logPanel == null) return;
        bool newState = !_logPanel.activeSelf;
        _logPanel.SetActive(newState);
        if (newState) RefreshLogText();
    }

    #endregion

    #region ===== Event Flow Handlers =====
    private void OnEventTriggered(GameEvent ev, ICharacter player)
    {
        _currentEvent = ev;
        gameObject.SetActive(true);
        _eventUIManager.DisplayEventStart(ev);
        QueueLine(ev.Description);
    }

    private void OnChoicesRequired(GameEvent ev, List<EventChoice> choices, ICharacter player)
    {
        if (ev != _currentEvent) return;
        _eventUIManager.ShowEventChoices(choices);
    }

    private void OnProgressUpdated(GameEvent ev, float progress, string desc, ICharacter player)
    {
        if (ev != _currentEvent) return;
        _eventUIManager.DisplayEventProgress(progress, desc);
        if (!string.IsNullOrEmpty(desc)) QueueLine(desc);
    }

    private void OnEventCompleted(GameEvent ev, EventResult res, ICharacter player)
    {
        if (ev != _currentEvent) return;
        _eventUIManager.DisplayEventResult(res);
        QueueLine(res.Success ? "イベント成功" : "イベント失敗");
        _currentEvent = null;
        _player = null;
    }

    private void OnChoiceSelected(object payload)
    {
        if (_currentEvent == null || _player == null) return;
        if (payload is EventChoice choice)
            _eventManager.MakeChoice(_currentEvent, choice, _player);
    }
    #endregion

    #region ===== Touch Handling =====
    private void HandleTouchEnded(TouchInfo info)
    {
        // 2 本指タップでログトグル、それ以外は送り/スキップ
        if (Input.touchSupported && Application.isMobilePlatform && Input.touchCount >= 2)
        {
            ToggleLog();
            return;
        }

        AdvanceOrSkip();
    }
    #endregion

    #region ===== Typing Logic =====
    private void QueueLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        _pendingLines.Enqueue(line.Trim());
        if (!_isTyping) StartNext();
    }

    private void StartNext()
    {
        if (_pendingLines.Count == 0) { _isTyping = false; return; }
        string text = _pendingLines.Dequeue();
        if (_typingCo != null) StopCoroutine(_typingCo);
        _typingCo = StartCoroutine(TypeRoutine(text));
    }

    private IEnumerator TypeRoutine(string full)
    {
        _isTyping = true;
        _messageText.text = string.Empty;
        float delay = _baseCharInterval / _speedMul;
        foreach (char c in full)
        {
            _messageText.text += c;
            yield return new WaitForSeconds(delay);
        }
        _isTyping = false;
        AppendToLog(full);
        StartNext();
    }

    private void AdvanceOrSkip()
    {
        if (_allowSkip && _isTyping)
        {
            if (_typingCo != null) StopCoroutine(_typingCo);
            _isTyping = false;
            StartNext();
        }
        else if (!_isTyping)
        {
            StartNext();
        }
    }
    #endregion

    #region ===== Log Logic =====
    private void AppendToLog(string line)
    {
        _logLines.Add(line);
        if (_logPanel != null && _logPanel.activeSelf) RefreshLogText();
    }

    private void RefreshLogText()
    {
        if (_logText == null) return;
        _logText.text = string.Join("\n", _logLines);
    }
    #endregion

    private void ResetEvent()
    {
        if (_eventManager != null)
        {
            _eventManager.OnEventTriggered -= OnEventTriggered;
            _eventManager.OnChoicesRequired -= OnChoicesRequired;
            _eventManager.OnProgressUpdated -= OnProgressUpdated;
            _eventManager.OnEventCompleted -= OnEventCompleted;
        }
        if (_touchManager != null)
            _touchManager.OnTouchEnded -= HandleTouchEnded;
    }
    private void SetEvent()
    {
        _eventManager.OnEventTriggered += OnEventTriggered;
        _eventManager.OnChoicesRequired += OnChoicesRequired;
        _eventManager.OnProgressUpdated += OnProgressUpdated;
        _eventManager.OnEventCompleted += OnEventCompleted;

        _eventUIManager.RegisterEventUICallback(EventUICallbackType.ChoicesShown, OnChoiceSelected);

        _touchManager.OnTouchEnded += HandleTouchEnded;
    }
}
