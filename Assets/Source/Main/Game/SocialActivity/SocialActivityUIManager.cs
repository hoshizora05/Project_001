using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SocialActivity.UI
{
    /// <summary>
    /// UI‑layer controller that visualises the current <see cref="SocialActivitySystem"/> state.
    /// 
    /// ‑ Locations currently available/open
    /// ‑ Activities available at the player’s location
    /// ‑ Current in‑game time / date
    /// ‑ NPCs present at the player’s location
    /// ‑ Player schedule (future <see cref="ScheduledActivity"/> instances)
    /// 
    /// The script expects the assigned <see cref="UIDocument"/> to contain the following
    /// VisualElement hierarchy (names are looked up via <c>Q&lt;T&gt;(name)</c>):
    /// 
    /// root
    /// ├── time‑label              : <see cref="Label"/> – shows current time & date
    /// ├── location‑list           : <see cref="ListView"/> – available locations
    /// ├── activity‑list           : <see cref="ListView"/> – available activities at current location
    /// ├── npc‑list                : <see cref="ListView"/> – NPCs present at current location
    /// └── schedule‑list           : <see cref="ListView"/> – upcoming scheduled activities for the player
    /// 
    /// All lists are filled with simple string items; replace <see cref="Generate*Item"/> if richer
    /// UI is required.
    /// </summary>
    [AddComponentMenu("SocialActivity/UI/Social Activity UI Manager")]
    public sealed class SocialActivityUIManager : MonoBehaviour
    {
        #region Inspector
        [Header("References")]
        [SerializeField] private SocialActivitySystem _socialSystem;
        [Tooltip("UIDocument containing the VisualTreeTemplate for the Social‑Activity HUD.")]
        [SerializeField] private UIDocument _uiDocument;
        [Tooltip("Player character that drives context‑sensitive lists.")]
        [SerializeField] private MonoBehaviour _playerBehaviour; // must implement ICharacter
        #endregion

        #region Private fields
        private ICharacter _player;
        private Label _timeLabel;
        private ListView _locationList;
        private ListView _activityList;
        private ListView _npcList;
        private ListView _scheduleList;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (_socialSystem == null) _socialSystem = SocialActivitySystem.Instance;
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
            if (_playerBehaviour != null) _player = _playerBehaviour as ICharacter;

            InitialiseUIReferences();
            HookEvents();
        }

        private void Start()
        {
            RefreshAll();
        }

        private void OnDestroy()
        {
            UnhookEvents();
        }
        #endregion

        #region Initialisation helpers
        private void InitialiseUIReferences()
        {
            if (_uiDocument == null)
            {
                Debug.LogError("[SocialActivityUIManager] No UIDocument assigned.");
                enabled = false;
                return;
            }

            VisualElement root = _uiDocument.rootVisualElement;
            _timeLabel = root.Q<Label>("time-label");
            _locationList = root.Q<ListView>("location-list");
            _activityList = root.Q<ListView>("activity-list");
            _npcList = root.Q<ListView>("npc-list");
            _scheduleList = root.Q<ListView>("schedule-list");

            ConfigureList(_locationList);
            ConfigureList(_activityList);
            ConfigureList(_npcList);
            ConfigureList(_scheduleList);
        }

        private static void ConfigureList(ListView list)
        {
            if (list == null) return;
            list.makeItem = () => new Label();
            list.bindItem = (ve, i) => ((Label)ve).text = list.itemsSource[i] as string;
            list.fixedItemHeight = 20f;
        }
        #endregion

        #region Event wiring
        private void HookEvents()
        {
            if (_socialSystem == null) return;
            _socialSystem.OnTimeChanged += HandleTimeChanged;
            _socialSystem.OnLocationChanged += HandleLocationChanged;
            _socialSystem.OnActivityStarted += HandleActivityListInvalidated;
            _socialSystem.OnActivityCompleted += HandleActivityListInvalidated;
            _socialSystem.OnScheduleCreated += HandleScheduleChanged;
        }

        private void UnhookEvents()
        {
            if (_socialSystem == null) return;
            _socialSystem.OnTimeChanged -= HandleTimeChanged;
            _socialSystem.OnLocationChanged -= HandleLocationChanged;
            _socialSystem.OnActivityStarted -= HandleActivityListInvalidated;
            _socialSystem.OnActivityCompleted -= HandleActivityListInvalidated;
            _socialSystem.OnScheduleCreated -= HandleScheduleChanged;
        }
        #endregion

        #region Event handlers
        private void HandleTimeChanged(object sender, TimeChangedEventArgs e) => RefreshTime();

        private void HandleLocationChanged(object sender, LocationChangedEventArgs e)
        {
            if (e.Character != _player) return;
            RefreshLocations();
            RefreshActivities();
            RefreshNpcs();
        }

        private void HandleActivityListInvalidated(object sender, EventArgs e)
        {
            RefreshActivities();
        }

        private void HandleScheduleChanged(object sender, ScheduleCreatedEventArgs e)
        {
            if (e.Organizer != _player) return;
            RefreshSchedule();
        }
        #endregion

        #region Refresh helpers
        private void RefreshAll()
        {
            RefreshTime();
            RefreshLocations();
            RefreshActivities();
            RefreshNpcs();
            RefreshSchedule();
        }

        private void RefreshTime()
        {
            if (_timeLabel == null) return;
            ITimeSystem time = _socialSystem?.TimeSystem;
            if (time == null) return;

            GameDate date = (time as TimeManager)?.GetCurrentGameDate() ?? default;
            string timeText = $"{date}: {time.GetCurrentTime()}";
            _timeLabel.text = timeText;
        }

        private void RefreshLocations()
        {
            if (_locationList == null) return;
            ILocationSystem locSys = _socialSystem?.LocationSystem;
            if (locSys == null) return;

            List<GameLocation> locs = locSys.GetAvailableLocations(_socialSystem.TimeSystem.GetCurrentTime());
            _locationList.itemsSource = locs.Select(l => l.Name).ToList();
            _locationList.Rebuild();
        }

        private void RefreshActivities()
        {
            if (_activityList == null || _player == null) return;
            ILocationSystem locSys = _socialSystem?.LocationSystem;
            IActivitySystem actSys = _socialSystem?.ActivitySystem;
            ITimeSystem time = _socialSystem?.TimeSystem;
            if (locSys == null || actSys == null || time == null) return;

            LocationManager locationManager = locSys as LocationManager;
            GameLocation currentLoc = locationManager.GetCharacterLocation(_player);
            if (currentLoc == null)
            {
                _activityList.itemsSource = new List<string>();
                _activityList.Rebuild();
                return;
            }

            List<Activity> acts = actSys.GetAvailableActivities(_player, currentLoc, time.GetCurrentTime());
            _activityList.itemsSource = acts.Select(a => a.Name).ToList();
            _activityList.Rebuild();
        }

        private void RefreshNpcs()
        {
            if (_npcList == null || _player == null) return;
            ILocationSystem locSys = _socialSystem?.LocationSystem;
            if (locSys == null) return;
            LocationManager locationManager = locSys as LocationManager;
            GameLocation currentLoc = locationManager.GetCharacterLocation(_player);
            if (currentLoc == null)
            {
                _npcList.itemsSource = new List<string>();
                _npcList.Rebuild();
                return;
            }

            List<ICharacter> npcs = locSys.GetNpcsAtLocation(currentLoc, _socialSystem.TimeSystem.GetCurrentTime());
            _npcList.itemsSource = npcs.Select(c => c.Name).ToList();
            _npcList.Rebuild();
        }

        private void RefreshSchedule()
        {
            if (_scheduleList == null || _player == null) return;
            _scheduleList.itemsSource = _player.Schedule
                .OrderBy(sa => sa.ScheduledDate)
                .Select(sa => $"{sa.ScheduledDate}: {sa.ActivityToPerform?.Name ?? "(unknown)"}")
                .ToList();
            _scheduleList.Rebuild();
        }
        #endregion
    }
}
