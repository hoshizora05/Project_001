using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.UIElements;

namespace InformationManagementUI
{
    /// <summary>
    /// Main controller class for the Information Management UI system.
    /// Manages the overall UI state and coordinates between different UI components.
    /// </summary>
    public class InformationManagementUISystem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIDocument _mainUIDocument;
        [SerializeField] private PanelSettings _panelSettings;
        
        [Header("UI Components")]
        [SerializeField] private CalendarViewController _calendarViewController;
        [SerializeField] private RelationshipDiagramViewController _relationshipViewController;
        [SerializeField] private StatusManagementViewController _statusViewController;
        
        [Header("Data Sources")]
        [SerializeField] private ScriptableObject _calendarDataSource;
        [SerializeField] private ScriptableObject _relationshipDataSource;
        [SerializeField] private ScriptableObject _characterStatusDataSource;
        
        // UI Root elements
        private VisualElement _rootElement;
        private VisualElement _mainContainer;
        private VisualElement _navigationBar;
        
        // State tracking
        private ReactiveProperty<UIViewType> _currentView = new ReactiveProperty<UIViewType>(UIViewType.Calendar);
        private ReactiveProperty<bool> _isUIVisible = new ReactiveProperty<bool>(false);
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        // Component ViewModels
        private CalendarViewModel _calendarViewModel;
        private RelationshipDiagramViewModel _relationshipViewModel;
        private StatusManagementViewModel _statusViewModel;
        
        // Constants
        private const string MAIN_CONTAINER_NAME = "main-container";
        private const string NAV_BAR_NAME = "navigation-bar";
        private const string CALENDAR_BTN_NAME = "calendar-button";
        private const string RELATIONSHIP_BTN_NAME = "relationship-button";
        private const string STATUS_BTN_NAME = "status-button";
        private const string TOGGLE_UI_BTN_NAME = "toggle-ui-button";
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeViewModels();
        }
        
        private void OnEnable()
        {
            if (_mainUIDocument != null)
            {
                _rootElement = _mainUIDocument.rootVisualElement;
                if (_rootElement != null)
                {
                    SetupUIReferences();
                    BindUIEvents();
                    SetupReactiveProperties();
                }
            }
        }
        
        private void Start()
        {
            // Initialize with calendar view as default
            ShowView(UIViewType.Calendar);
            
            // Initially hide the UI if configured that way
            if (!_isUIVisible.Value)
            {
                SetUIVisibility(false);
            }
        }
        
        private void OnDisable()
        {
            // Dispose all subscriptions
            _disposables.Clear();
            
            // Unregister UI events
            UnbindUIEvents();
        }
        
        private void OnDestroy()
        {
            _disposables.Dispose();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeViewModels()
        {
            // Initialize ViewModels with their respective data sources
            _calendarViewModel = new CalendarViewModel(_calendarDataSource as ICalendarDataProvider);
            _relationshipViewModel = new RelationshipDiagramViewModel(_relationshipDataSource as IRelationshipDataProvider);
            _statusViewModel = new StatusManagementViewModel(_characterStatusDataSource as ICharacterStatusDataProvider);
            
            // Connect ViewModels to View Controllers
            if (_calendarViewController != null)
                _calendarViewController.Initialize(_calendarViewModel);
                
            if (_relationshipViewController != null)
                _relationshipViewController.Initialize(_relationshipViewModel);
                
            if (_statusViewController != null)
                _statusViewController.Initialize(_statusViewModel);
        }
        
        private void SetupUIReferences()
        {
            _mainContainer = _rootElement.Q<VisualElement>(MAIN_CONTAINER_NAME);
            _navigationBar = _rootElement.Q<VisualElement>(NAV_BAR_NAME);
        }
        
        private void BindUIEvents()
        {
            // Navigation button events
            Button calendarButton = _rootElement.Q<Button>(CALENDAR_BTN_NAME);
            Button relationshipButton = _rootElement.Q<Button>(RELATIONSHIP_BTN_NAME);
            Button statusButton = _rootElement.Q<Button>(STATUS_BTN_NAME);
            Button toggleUIButton = _rootElement.Q<Button>(TOGGLE_UI_BTN_NAME);
            
            if (calendarButton != null)
                calendarButton.clicked += () => ShowView(UIViewType.Calendar);
                
            if (relationshipButton != null)
                relationshipButton.clicked += () => ShowView(UIViewType.RelationshipDiagram);
                
            if (statusButton != null)
                statusButton.clicked += () => ShowView(UIViewType.StatusManagement);
                
            if (toggleUIButton != null)
                toggleUIButton.clicked += ToggleUIVisibility;
        }
        
        private void UnbindUIEvents()
        {
            Button calendarButton = _rootElement.Q<Button>(CALENDAR_BTN_NAME);
            Button relationshipButton = _rootElement.Q<Button>(RELATIONSHIP_BTN_NAME);
            Button statusButton = _rootElement.Q<Button>(STATUS_BTN_NAME);
            Button toggleUIButton = _rootElement.Q<Button>(TOGGLE_UI_BTN_NAME);
            
            if (calendarButton != null)
                calendarButton.clicked -= () => ShowView(UIViewType.Calendar);
                
            if (relationshipButton != null)
                relationshipButton.clicked -= () => ShowView(UIViewType.RelationshipDiagram);
                
            if (statusButton != null)
                statusButton.clicked -= () => ShowView(UIViewType.StatusManagement);
                
            if (toggleUIButton != null)
                toggleUIButton.clicked -= ToggleUIVisibility;
        }
        
        private void SetupReactiveProperties()
        {
            // Subscribe to view change events
            _currentView
                .Subscribe(viewType => UpdateActiveView(viewType))
                .AddTo(_disposables);
                
            // Subscribe to visibility change events
            _isUIVisible
                .Subscribe(isVisible => SetUIVisibility(isVisible))
                .AddTo(_disposables);
        }
        
        #endregion
        
        #region UI Control Methods
        
        /// <summary>
        /// Shows the specified UI view and hides others
        /// </summary>
        /// <param name="viewType">The type of view to display</param>
        public void ShowView(UIViewType viewType)
        {
            _currentView.Value = viewType;
        }
        
        /// <summary>
        /// Updates the active view based on the current view type
        /// </summary>
        /// <param name="viewType">The current view type</param>
        private void UpdateActiveView(UIViewType viewType)
        {
            // Hide all views first
            if (_calendarViewController != null)
                _calendarViewController.Hide();
                
            if (_relationshipViewController != null)
                _relationshipViewController.Hide();
                
            if (_statusViewController != null)
                _statusViewController.Hide();
            
            // Then show the selected view
            switch (viewType)
            {
                case UIViewType.Calendar:
                    if (_calendarViewController != null)
                        _calendarViewController.Show();
                    break;
                
                case UIViewType.RelationshipDiagram:
                    if (_relationshipViewController != null)
                        _relationshipViewController.Show();
                    break;
                
                case UIViewType.StatusManagement:
                    if (_statusViewController != null)
                        _statusViewController.Show();
                    break;
            }
            
            // Update navigation highlighting
            UpdateNavigationHighlight(viewType);
        }
        
        /// <summary>
        /// Updates the navigation bar button highlighting based on the active view
        /// </summary>
        /// <param name="viewType">The current view type</param>
        private void UpdateNavigationHighlight(UIViewType viewType)
        {
            if (_navigationBar == null) return;
            
            // Remove highlight class from all buttons
            foreach (var button in _navigationBar.Children())
            {
                button.RemoveFromClassList("active-nav-button");
            }
            
            // Add highlight to the active button
            string activeButtonName = string.Empty;
            switch (viewType)
            {
                case UIViewType.Calendar:
                    activeButtonName = CALENDAR_BTN_NAME;
                    break;
                case UIViewType.RelationshipDiagram:
                    activeButtonName = RELATIONSHIP_BTN_NAME;
                    break;
                case UIViewType.StatusManagement:
                    activeButtonName = STATUS_BTN_NAME;
                    break;
            }
            
            if (!string.IsNullOrEmpty(activeButtonName))
            {
                Button activeButton = _navigationBar.Q<Button>(activeButtonName);
                if (activeButton != null)
                {
                    activeButton.AddToClassList("active-nav-button");
                }
            }
        }
        
        /// <summary>
        /// Toggles the visibility of the entire UI
        /// </summary>
        public void ToggleUIVisibility()
        {
            _isUIVisible.Value = !_isUIVisible.Value;
        }
        
        /// <summary>
        /// Sets the visibility of the entire UI
        /// </summary>
        /// <param name="isVisible">Whether the UI should be visible</param>
        private void SetUIVisibility(bool isVisible)
        {
            if (_mainContainer != null)
            {
                _mainContainer.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// Shows the UI with the specified view
        /// </summary>
        /// <param name="viewType">The view to display</param>
        public void ShowUI(UIViewType viewType)
        {
            _isUIVisible.Value = true;
            ShowView(viewType);
        }
        
        /// <summary>
        /// Hides the UI
        /// </summary>
        public void HideUI()
        {
            _isUIVisible.Value = false;
        }
        
        #endregion
        
        #region Public API Methods
        
        /// <summary>
        /// Opens the calendar to a specific date
        /// </summary>
        /// <param name="date">The date to display</param>
        public void ShowCalendarForDate(DateTime date)
        {
            ShowUI(UIViewType.Calendar);
            _calendarViewModel.SetSelectedDate(date);
        }
        
        /// <summary>
        /// Opens the relationship diagram focused on a specific character
        /// </summary>
        /// <param name="characterId">ID of the character to focus on</param>
        public void ShowRelationshipsForCharacter(string characterId)
        {
            ShowUI(UIViewType.RelationshipDiagram);
            _relationshipViewModel.SetFocusCharacter(characterId);
        }
        
        /// <summary>
        /// Opens the status management screen for a specific character
        /// </summary>
        /// <param name="characterId">ID of the character to display</param>
        public void ShowStatusForCharacter(string characterId)
        {
            ShowUI(UIViewType.StatusManagement);
            _statusViewModel.SetSelectedCharacter(characterId);
        }
        
        /// <summary>
        /// Refreshes all UI data from their respective data sources
        /// </summary>
        public void RefreshAllData()
        {
            _calendarViewModel.RefreshData();
            _relationshipViewModel.RefreshData();
            _statusViewModel.RefreshData();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Enum representing the different view types in the Information Management UI
    /// </summary>
    public enum UIViewType
    {
        Calendar,
        RelationshipDiagram,
        StatusManagement
    }
}