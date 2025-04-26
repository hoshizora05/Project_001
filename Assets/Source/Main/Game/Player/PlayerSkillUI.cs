using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerProgression;
using PlayerProgression.Data;

namespace PlayerProgression.UI
{
    /// <summary>
    /// Runtime UI that lists all player skills grouped by category.
    /// Each skill row shows skill name, level, and an experience progress bar
    /// ( currentExp / nextLevelThreshold ).
    /// </summary>
    public class PlayerSkillUI : MonoBehaviour
    {
        [Header("Required References")]
        [Tooltip("Root transform that receives dynamically created category headers and skill rows.")]
        [SerializeField] private RectTransform _listContainer;

        [Tooltip("Prefab for a category header. Must contain a Text component (category name) and a VerticalLayoutGroup for the skills.")]
        [SerializeField] private GameObject _categoryHeaderPrefab;

        [Tooltip("Prefab for a single skill row (name + level text + slider).")]
        [SerializeField] private GameObject _skillRowPrefab;

        [Tooltip("Optional: Event bus used by the progression system.  When set, the UI will refresh immediately when a progression event that changes skills is published.")]
        [SerializeField] private EventBusReference _eventBus;

        /// <summary> Helper cache that lets us update only the slider/value of existing rows. </summary>
        private readonly Dictionary<string, SkillRow> _skillRows = new();

        /// <summary> Mapping: categoryId -> Vertical layout that owns the rows. </summary>
        private readonly Dictionary<string, RectTransform> _categoryContainers = new();

        private PlayerProgressionManager _progression => PlayerProgressionManager.Instance;
        private PlayerProgressionConfig _config => _progression ? (PlayerProgressionConfig)typeof(PlayerProgressionManager)
            .GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(_progression) : null; // crude but avoids adding a public getter

        private void Awake()
        {
            BuildCategoryHierarchy();
            RefreshAll();
        }

        private void OnEnable()
        {
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

        private void OnProgressionEvent(ProgressionEvent evt)
        {
            switch (evt.type)
            {
                case ProgressionEvent.ProgressionEventType.SkillExperience:
                case ProgressionEvent.ProgressionEventType.CompleteAction:
                    // A skill level/experience probably changed.
                    RefreshAll();
                    break;
            }
        }

        private void Update()
        {
            // Cheap periodic refresh (every 0.2 s like other UI) – could be replaced with event‑only updates.
            _timeSinceRefresh += Time.unscaledDeltaTime;
            if (_timeSinceRefresh >= _refreshInterval)
            {
                _timeSinceRefresh = 0f;
                RefreshAll();
            }
        }

        private const float _refreshInterval = 0.2f;
        private float _timeSinceRefresh;

        #region Build UI
        private void BuildCategoryHierarchy()
        {
            if (_config == null) return;
            foreach (var category in _config.skillCategories)
            {
                // Instantiate category header prefab
                var catGO = Instantiate(_categoryHeaderPrefab, _listContainer);
                catGO.name = $"Category_{category.categoryName}";

                // Expect a child Text component for the title.
                var titleText = catGO.GetComponentInChildren<Text>();
                if (titleText != null) titleText.text = category.categoryName;

                // Expect a VerticalLayoutGroup target for skill rows.  By convention we'll use a child called "Content" if present.
                RectTransform contentHolder = null;
                var trans = catGO.transform.Find("Content");
                if (trans != null) contentHolder = trans as RectTransform;
                if (contentHolder == null) contentHolder = catGO.GetComponent<RectTransform>();

                _categoryContainers[category.categoryId] = contentHolder;

                // Create row for each skill inside category
                foreach (var skill in category.skills)
                {
                    CreateSkillRow(category.categoryId, skill.skillId, skill.skillName);
                }
            }
        }

        private void CreateSkillRow(string categoryId, string skillId, string skillName)
        {
            if (!_categoryContainers.TryGetValue(categoryId, out var parent)) return;

            var rowGO = Instantiate(_skillRowPrefab, parent);
            rowGO.name = $"Skill_{skillName}";

            var texts = rowGO.GetComponentsInChildren<Text>();
            Text nameText = null;
            Text levelText = null;
            foreach (var t in texts)
            {
                if (t.gameObject.name.ToLower().Contains("name")) nameText = t;
                else if (t.gameObject.name.ToLower().Contains("level")) levelText = t;
            }

            var slider = rowGO.GetComponentInChildren<Slider>();
            if (nameText != null) nameText.text = skillName;

            _skillRows[skillId] = new SkillRow
            {
                LevelText = levelText,
                ExpSlider = slider
            };
        }
        #endregion

        #region Refresh Logic
        private void RefreshAll()
        {
            foreach (var pair in _skillRows)
            {
                var skillId = pair.Key;
                var row = pair.Value;

                float level = _progression.GetSkillLevel(skillId);
                float exp = _progression.GetSkillExperience(skillId);
                float threshold = GetNextThreshold(skillId);

                if (row.LevelText != null)
                {
                    row.LevelText.text = $"Lv. {level}";
                }

                if (row.ExpSlider != null)
                {
                    row.ExpSlider.value = threshold > 0 ? exp / threshold : 0f;
                }
            }
        }

        private float GetNextThreshold(string skillId)
        {
            // Access private data through config (since runtime system does not expose).  We'll approximate by reading config initial + multiplier.
            if (_config == null) return 1f;
            foreach (var cat in _config.skillCategories)
            {
                foreach (var s in cat.skills)
                {
                    if (s.skillId == skillId)
                    {
                        // initialLevelThreshold * (multiplier ^ currentLevel)
                        float lvl = _progression.GetSkillLevel(skillId);
                        float thresh = s.initialLevelThreshold;
                        for (int i = 0; i < lvl; i++)
                        {
                            thresh *= s.levelThresholdMultiplier;
                        }
                        return thresh;
                    }
                }
            }
            return 1f;
        }
        #endregion

        private class SkillRow
        {
            public Text LevelText;
            public Slider ExpSlider;
        }
    }
}
