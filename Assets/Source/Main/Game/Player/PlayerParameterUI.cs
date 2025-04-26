using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerProgression;
using PlayerProgression.Data;

namespace PlayerProgression.UI
{
    /// <summary>
    /// Runtime UI that lists all player stats defined in <see cref="PlayerProgressionConfig.initialStats"/>.
    /// It instantiates a row for each stat and keeps the displayed values in‑sync with
    /// <see cref="PlayerProgressionManager"/> by polling at a small interval *and* listening to
    /// <see cref="ProgressionEvent"/> updates that come through the shared <see cref="EventBusReference"/>.
    /// </summary>
    public class PlayerParameterUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerProgressionManager _progressionManager;
        [SerializeField] private EventBusReference _eventBus;
        [SerializeField] private PlayerProgressionConfig _config;

        [Header("UI")]
        [Tooltip("Parent under which StatRowUIPrefabs will be spawned (typically a Vertical Layout Group).")]
        [SerializeField] private RectTransform _listContainer;
        [Tooltip("Prefab containing a StatRowUI component.")]
        [SerializeField] private StatRowUI _statRowPrefab;

        [Header("Refresh Settings")]
        [Tooltip("How often (in seconds) to poll the progression manager for updated values.\n" +
                 "This is a fallback for changes that are not surfaced via ProgressionEvent.")]
        [SerializeField, Min(0.05f)] private float _pollInterval = 0.2f;

        private readonly Dictionary<string, StatRowUI> _rows = new Dictionary<string, StatRowUI>();
        private float _pollTimer;

        #region Unity
        private void Awake()
        {
            if (_progressionManager == null)
                _progressionManager = PlayerProgressionManager.Instance;
            //if (_config == null && _progressionManager != null)
            //    _config = _progressionManager.GetComponent<PlayerProgressionManager>() ? _progressionManager.GetComponent<PlayerProgressionManager>().GetComponent<PlayerProgressionManager>().GetComponent<PlayerProgressionManager>().config : null;
            if (_config == null)
                Debug.LogError("[PlayerParameterUI] PlayerProgressionConfig reference missing – UI will not populate.");
        }

        private void OnEnable()
        {
            BuildStatRows();
            if (_eventBus != null)
            {
                _eventBus.Subscribe<ProgressionEvent>(OnProgressionEvent);
            }
        }

        private void OnDisable()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<ProgressionEvent>(OnProgressionEvent);
            }
        }

        private void Update()
        {
            _pollTimer += Time.deltaTime;
            if (_pollTimer >= _pollInterval)
            {
                _pollTimer = 0f;
                RefreshAllRows();
            }
        }
        #endregion

        #region Event Handling
        private void OnProgressionEvent(ProgressionEvent evt)
        {
            if (evt.type != ProgressionEvent.ProgressionEventType.StatChange)
                return;
            if (evt.parameters == null || !evt.parameters.TryGetValue("statId", out var statIdObj))
                return;

            string statId = statIdObj as string;
            if (string.IsNullOrEmpty(statId))
                return;

            RefreshRow(statId);
        }
        #endregion

        #region UI Helpers
        private void BuildStatRows()
        {
            if (_config == null || _statRowPrefab == null || _listContainer == null)
                return;

            foreach (Transform child in _listContainer)
            {
                Destroy(child.gameObject);
            }
            _rows.Clear();

            foreach (var stat in _config.initialStats)
            {
                var row = Instantiate(_statRowPrefab, _listContainer);
                _rows[stat.statId] = row;
                row.SetName(stat.statName);
                UpdateRowValues(stat.statId, stat.baseValue, stat.maxValue);
            }
        }

        private void RefreshAllRows()
        {
            foreach (var kvp in _rows)
            {
                RefreshRow(kvp.Key);
            }
        }

        private void RefreshRow(string statId)
        {
            if (_progressionManager == null || !_rows.ContainsKey(statId))
                return;

            StatValue value = _progressionManager.GetStatValue(statId);
            UpdateRowValues(statId, value.CurrentValue, value.MaxValue);
        }

        private void UpdateRowValues(string statId, float current, float max)
        {
            if (_rows.TryGetValue(statId, out var row))
            {
                row.SetValue(current, max);
            }
        }
        #endregion
    }

    /// <summary>
    /// Simple reusable row for displaying a single stat (name + bar + numeric value).
    /// Attach this component to the prefab referenced by <see cref="PlayerParameterUI._statRowPrefab"/>.
    /// </summary>
    public class StatRowUI : MonoBehaviour
    {
        [SerializeField] private Text _statNameText;
        [SerializeField] private Slider _valueSlider;
        [SerializeField] private Text _valueLabel;

        public void SetName(string displayName)
        {
            if (_statNameText != null)
                _statNameText.text = displayName;
        }

        public void SetValue(float current, float max)
        {
            if (_valueSlider != null)
            {
                _valueSlider.maxValue = max;
                _valueSlider.value = current;
            }

            if (_valueLabel != null)
            {
                _valueLabel.text = $"{current:0}/{max:0}";
            }
        }
    }
}
