using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;

namespace InformationManagementUI
{
    /// <summary>
    /// View controller for the status management component
    /// </summary>
    public class StatusManagementViewController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset _statBarTemplate;
        [SerializeField] private VisualTreeAsset _skillItemTemplate;
        [SerializeField] private VisualTreeAsset _conditionItemTemplate;
        [SerializeField] private VisualTreeAsset _inventoryItemTemplate;
        [SerializeField] private VisualTreeAsset _goalItemTemplate;
        
        private StatusManagementViewModel _viewModel;
        private VisualElement _rootElement;
        private VisualElement _tabContainer;
        private VisualElement _characterSelectionPanel;
        private List<VisualElement> _tabPanels = new List<VisualElement>();
        private DropdownField _characterDropdown;
        private List<RadioButton> _tabButtons = new List<RadioButton>();
        private Button _toggleComparisonButton;
        private DropdownField _comparisonCharacterDropdown;
        private DropdownField _comparisonDateDropdown;
        
        // Tab panels
        private VisualElement _characterInfoTab;
        private VisualElement _statsTab;
        private VisualElement _conditionTab;
        private VisualElement _inventoryTab;
        private VisualElement _socialTab;
        private VisualElement _goalsTab;
        
        // Content containers
        private VisualElement _portraitSection;
        private VisualElement _basicInfoSection;
        private VisualElement _biographySection;
        private VisualElement _statBarsSection;
        private VisualElement _skillsGridSection;
        private VisualElement _comparisonGraphSection;
        private VisualElement _mentalStateSection;
        private VisualElement _physicalStateSection;
        private VisualElement _itemGridView;
        private VisualElement _equipmentSlots;
        private VisualElement _resourceCounters;
        private VisualElement _reputationChart;
        private VisualElement _achievementsSection;
        private VisualElement _titlesSection;
        private VisualElement _activeGoalsList;
        private VisualElement _progressTrackers;
        private VisualElement _completedGoalsArchive;
        
        // CompositeDisposable for cleanup
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        // Constants
        private const string TAB_CONTAINER_NAME = "tab-container";
        private const string CHARACTER_SELECTION_PANEL_NAME = "character-selection-panel";
        private const string CHARACTER_DROPDOWN_NAME = "character-dropdown";
        private const string TOGGLE_COMPARISON_BUTTON_NAME = "toggle-comparison-button";
        private const string COMPARISON_CHARACTER_DROPDOWN_NAME = "comparison-character-dropdown";
        private const string COMPARISON_DATE_DROPDOWN_NAME = "comparison-date-dropdown";
        private const string COMPARISON_PANEL_NAME = "comparison-panel";
        
        private const string CHARACTER_INFO_TAB_NAME = "character-info-tab";
        private const string STATS_TAB_NAME = "stats-tab";
        private const string CONDITION_TAB_NAME = "condition-tab";
        private const string INVENTORY_TAB_NAME = "inventory-tab";
        private const string SOCIAL_TAB_NAME = "social-tab";
        private const string GOALS_TAB_NAME = "goals-tab";
        
        private const string PORTRAIT_SECTION_NAME = "portrait-section";
        private const string BASIC_INFO_SECTION_NAME = "basic-info-section";
        private const string BIOGRAPHY_SECTION_NAME = "biography-section";
        private const string STAT_BARS_SECTION_NAME = "stat-bars-section";
        private const string SKILLS_GRID_SECTION_NAME = "skills-grid-section";
        private const string COMPARISON_GRAPH_SECTION_NAME = "comparison-graph-section";
        private const string MENTAL_STATE_SECTION_NAME = "mental-state-section";
        private const string PHYSICAL_STATE_SECTION_NAME = "physical-state-section";
        private const string ITEM_GRID_VIEW_NAME = "item-grid-view";
        private const string EQUIPMENT_SLOTS_NAME = "equipment-slots";
        private const string RESOURCE_COUNTERS_NAME = "resource-counters";
        private const string REPUTATION_CHART_NAME = "reputation-chart";
        private const string ACHIEVEMENTS_SECTION_NAME = "achievements-section";
        private const string TITLES_SECTION_NAME = "titles-section";
        private const string ACTIVE_GOALS_LIST_NAME = "active-goals-list";
        private const string PROGRESS_TRACKERS_NAME = "progress-trackers";
        private const string COMPLETED_GOALS_ARCHIVE_NAME = "completed-goals-archive";
        
        /// <summary>
        /// Initializes the view controller with a view model
        /// </summary>
        /// <param name="viewModel">The view model to initialize with</param>
        public void Initialize(StatusManagementViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            
            // Get the root element (assuming this component is on the same GameObject as the UIDocument)
            var document = GetComponent<UIDocument>();
            if (document != null)
            {
                _rootElement = document.rootVisualElement;
                if (_rootElement != null)
                {
                    SetupUI();
                    BindViewModel();
                }
            }
        }
        
        /// <summary>
        /// Shows the status management view
        /// </summary>
        public void Show()
        {
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.Flex;
                
                // Refresh all tabs when showing
                RefreshAllTabs();
            }
        }
        
        /// <summary>
        /// Hides the status management view
        /// </summary>
        public void Hide()
        {
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.None;
            }
        }
        
        private void OnDestroy()
        {
            _disposables.Dispose();
        }
        
        /// <summary>
        /// Sets up the UI elements and event handlers
        /// </summary>
        private void SetupUI()
        {
            // Get main UI containers
            _tabContainer = _rootElement.Q<VisualElement>(TAB_CONTAINER_NAME);
            _characterSelectionPanel = _rootElement.Q<VisualElement>(CHARACTER_SELECTION_PANEL_NAME);
            
            // Get tab panels
            _characterInfoTab = _rootElement.Q<VisualElement>(CHARACTER_INFO_TAB_NAME);
            _statsTab = _rootElement.Q<VisualElement>(STATS_TAB_NAME);
            _conditionTab = _rootElement.Q<VisualElement>(CONDITION_TAB_NAME);
            _inventoryTab = _rootElement.Q<VisualElement>(INVENTORY_TAB_NAME);
            _socialTab = _rootElement.Q<VisualElement>(SOCIAL_TAB_NAME);
            _goalsTab = _rootElement.Q<VisualElement>(GOALS_TAB_NAME);
            
            // Add tabs to list for easy access
            _tabPanels.Add(_characterInfoTab);
            _tabPanels.Add(_statsTab);
            _tabPanels.Add(_conditionTab);
            _tabPanels.Add(_inventoryTab);
            _tabPanels.Add(_socialTab);
            _tabPanels.Add(_goalsTab);
            
            // Get content containers
            SetupContentContainers();
            
            // Get character dropdown
            _characterDropdown = _characterSelectionPanel.Q<DropdownField>(CHARACTER_DROPDOWN_NAME);
            
            // Get comparison controls
            _toggleComparisonButton = _characterSelectionPanel.Q<Button>(TOGGLE_COMPARISON_BUTTON_NAME);
            _comparisonCharacterDropdown = _characterSelectionPanel.Q<DropdownField>(COMPARISON_CHARACTER_DROPDOWN_NAME);
            _comparisonDateDropdown = _characterSelectionPanel.Q<DropdownField>(COMPARISON_DATE_DROPDOWN_NAME);
            
            // Get tab buttons
            GetTabButtons();
            
            // Set up character dropdown
            SetupCharacterDropdown();
            
            // Set up tab buttons
            SetupTabButtons();
            
            // Set up comparison controls
            SetupComparisonControls();
            
            // Initial tab selection
            ShowTab(0);
        }
        
        /// <summary>
        /// Gets references to all content containers
        /// </summary>
        private void SetupContentContainers()
        {
            // Character info tab containers
            _portraitSection = _characterInfoTab?.Q<VisualElement>(PORTRAIT_SECTION_NAME);
            _basicInfoSection = _characterInfoTab?.Q<VisualElement>(BASIC_INFO_SECTION_NAME);
            _biographySection = _characterInfoTab?.Q<VisualElement>(BIOGRAPHY_SECTION_NAME);
            
            // Stats tab containers
            _statBarsSection = _statsTab?.Q<VisualElement>(STAT_BARS_SECTION_NAME);
            _skillsGridSection = _statsTab?.Q<VisualElement>(SKILLS_GRID_SECTION_NAME);
            _comparisonGraphSection = _statsTab?.Q<VisualElement>(COMPARISON_GRAPH_SECTION_NAME);
            
            // Condition tab containers
            _mentalStateSection = _conditionTab?.Q<VisualElement>(MENTAL_STATE_SECTION_NAME);
            _physicalStateSection = _conditionTab?.Q<VisualElement>(PHYSICAL_STATE_SECTION_NAME);
            
            // Inventory tab containers
            _itemGridView = _inventoryTab?.Q<VisualElement>(ITEM_GRID_VIEW_NAME);
            _equipmentSlots = _inventoryTab?.Q<VisualElement>(EQUIPMENT_SLOTS_NAME);
            _resourceCounters = _inventoryTab?.Q<VisualElement>(RESOURCE_COUNTERS_NAME);
            
            // Social tab containers
            _reputationChart = _socialTab?.Q<VisualElement>(REPUTATION_CHART_NAME);
            _achievementsSection = _socialTab?.Q<VisualElement>(ACHIEVEMENTS_SECTION_NAME);
            _titlesSection = _socialTab?.Q<VisualElement>(TITLES_SECTION_NAME);
            
            // Goals tab containers
            _activeGoalsList = _goalsTab?.Q<VisualElement>(ACTIVE_GOALS_LIST_NAME);
            _progressTrackers = _goalsTab?.Q<VisualElement>(PROGRESS_TRACKERS_NAME);
            _completedGoalsArchive = _goalsTab?.Q<VisualElement>(COMPLETED_GOALS_ARCHIVE_NAME);
        }
        
        /// <summary>
        /// Gets references to all tab buttons
        /// </summary>
        private void GetTabButtons()
        {
            // Clear existing list
            _tabButtons.Clear();
            
            // Find tab buttons (usually RadioButtons in the tab container)
            var radioButtons = _tabContainer.Query<RadioButton>().ToList();
            foreach (var button in radioButtons)
            {
                _tabButtons.Add(button);
            }
        }
        
        /// <summary>
        /// Sets up the character dropdown
        /// </summary>
        private void SetupCharacterDropdown()
        {
            if (_characterDropdown == null) return;
            
            // Set up change handler
            _characterDropdown.RegisterValueChangedCallback(evt => 
            {
                if (_viewModel != null && !string.IsNullOrEmpty(evt.newValue))
                {
                    _viewModel.SetSelectedCharacter(evt.newValue);
                }
            });
        }
        
        /// <summary>
        /// Sets up the tab buttons
        /// </summary>
        private void SetupTabButtons()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                int index = i; // Capture for lambda
                _tabButtons[i].RegisterValueChangedCallback(evt => 
                {
                    if (evt.newValue)
                    {
                        ShowTab(index);
                    }
                });
            }
        }
        
        /// <summary>
        /// Sets up the comparison controls
        /// </summary>
        private void SetupComparisonControls()
        {
            if (_toggleComparisonButton != null)
            {
                _toggleComparisonButton.clicked += () => 
                {
                    if (_viewModel != null)
                    {
                        _viewModel.ToggleComparisonMode();
                    }
                };
            }
            
            if (_comparisonCharacterDropdown != null)
            {
                _comparisonCharacterDropdown.RegisterValueChangedCallback(evt => 
                {
                    if (_viewModel != null && !string.IsNullOrEmpty(evt.newValue))
                    {
                        _viewModel.SelectComparisonCharacter(evt.newValue);
                    }
                });
            }
            
            if (_comparisonDateDropdown != null)
            {
                _comparisonDateDropdown.RegisterValueChangedCallback(evt => 
                {
                    if (_viewModel != null && !string.IsNullOrEmpty(evt.newValue))
                    {
                        // Parse date from dropdown value (format: "yyyy-MM-dd")
                        if (DateTime.TryParse(evt.newValue, out DateTime date))
                        {
                            _viewModel.SelectHistoricalDate(date);
                        }
                    }
                });
            }
            
            // Initially hide comparison dropdowns
            if (_comparisonCharacterDropdown != null)
            {
                _comparisonCharacterDropdown.style.display = DisplayStyle.None;
            }
            
            if (_comparisonDateDropdown != null)
            {
                _comparisonDateDropdown.style.display = DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// Binds the view model to the UI
        /// </summary>
        private void BindViewModel()
        {
            if (_viewModel == null) return;
            
            // Bind character collection changes
            _viewModel.Characters.ObserveCountChanged()
                .Subscribe(_ => UpdateCharacterDropdown())
                .AddTo(_disposables);
            
            // Bind selected character changes
            _viewModel.SelectedCharacter
                .Subscribe(_ => RefreshAllTabs())
                .AddTo(_disposables);
            
            // Bind selected tab changes
            _viewModel.SelectedTabIndex
                .Subscribe(index => ShowTab(index))
                .AddTo(_disposables);
            
            // Bind comparison mode changes
            _viewModel.ComparisonModeActive
                .Subscribe(active => UpdateComparisonMode(active))
                .AddTo(_disposables);
            
            // Bind comparison character changes
            _viewModel.ComparisonCharacterId
                .Subscribe(id => 
                {
                    if (_comparisonCharacterDropdown != null && 
                        !string.IsNullOrEmpty(id) && 
                        _comparisonCharacterDropdown.value != id)
                    {
                        _comparisonCharacterDropdown.value = id;
                    }
                    
                    // Refresh comparison graphs
                    RefreshComparisonViews();
                })
                .AddTo(_disposables);
            
            // Bind historical comparison date changes
            _viewModel.HistoricalComparisonDate
                .Subscribe(date => 
                {
                    if (_comparisonDateDropdown != null && 
                        date != default && 
                        _comparisonDateDropdown.value != date.ToString("yyyy-MM-dd"))
                    {
                        _comparisonDateDropdown.value = date.ToString("yyyy-MM-dd");
                    }
                    
                    // Refresh comparison graphs
                    RefreshComparisonViews();
                })
                .AddTo(_disposables);
            
            // Initial UI update
            UpdateCharacterDropdown();
            RefreshAllTabs();
        }
        
        /// <summary>
        /// Updates the character dropdown with current characters
        /// </summary>
        private void UpdateCharacterDropdown()
        {
            if (_characterDropdown == null || _viewModel == null) return;
            
            // Build character choices
            List<string> choices = new List<string>();
            Dictionary<string, string> displayNames = new Dictionary<string, string>();
            
            foreach (var character in _viewModel.Characters)
            {
                choices.Add(character.CharacterID);
                displayNames[character.CharacterID] = character.BasicInfo.Name;
            }
            
            // Save current selection
            string currentSelection = _characterDropdown.value;
            
            // Update dropdown
            _characterDropdown.choices = choices;
            
            // Set custom name formatter
            _characterDropdown.formatSelectedValueCallback = id => 
                displayNames.TryGetValue(id, out string name) ? name : id;
            _characterDropdown.formatListItemCallback = id => 
                displayNames.TryGetValue(id, out string name) ? name : id;
            
            // Restore selection if possible
            if (!string.IsNullOrEmpty(currentSelection) && choices.Contains(currentSelection))
            {
                _characterDropdown.value = currentSelection;
            }
            else if (choices.Count > 0)
            {
                _characterDropdown.value = choices[0];
                if (_viewModel != null)
                {
                    _viewModel.SetSelectedCharacter(choices[0]);
                }
            }
            
            // Also update comparison character dropdown
            UpdateComparisonCharacterDropdown();
        }
        
        /// <summary>
        /// Updates the comparison character dropdown with characters other than the selected one
        /// </summary>
        private void UpdateComparisonCharacterDropdown()
        {
            if (_comparisonCharacterDropdown == null || _viewModel == null) return;
            
            // Build character choices (excluding selected character)
            List<string> choices = new List<string>();
            Dictionary<string, string> displayNames = new Dictionary<string, string>();
            
            foreach (var character in _viewModel.Characters)
            {
                if (character.CharacterID != _viewModel.SelectedCharacterId.Value)
                {
                    choices.Add(character.CharacterID);
                    displayNames[character.CharacterID] = character.BasicInfo.Name;
                }
            }
            
            // Save current selection
            string currentSelection = _comparisonCharacterDropdown.value;
            
            // Update dropdown
            _comparisonCharacterDropdown.choices = choices;
            
            // Set custom name formatter
            _comparisonCharacterDropdown.formatSelectedValueCallback = id => 
                displayNames.TryGetValue(id, out string name) ? name : id;
            _comparisonCharacterDropdown.formatListItemCallback = id => 
                displayNames.TryGetValue(id, out string name) ? name : id;
            
            // Restore selection if possible
            if (!string.IsNullOrEmpty(currentSelection) && choices.Contains(currentSelection))
            {
                _comparisonCharacterDropdown.value = currentSelection;
            }
            else if (choices.Count > 0)
            {
                _comparisonCharacterDropdown.value = choices[0];
            }
        }
        
        /// <summary>
        /// Updates the historical date dropdown with available snapshot dates
        /// </summary>
        private void UpdateHistoricalDateDropdown()
        {
            if (_comparisonDateDropdown == null || _viewModel == null) return;
            
            // Get available snapshot dates
            var dates = _viewModel.GetHistoricalSnapshotDates();
            
            // Format dates for dropdown
            List<string> choices = new List<string>();
            foreach (var date in dates)
            {
                choices.Add(date.ToString("yyyy-MM-dd"));
            }
            
            // Save current selection
            string currentSelection = _comparisonDateDropdown.value;
            
            // Update dropdown
            _comparisonDateDropdown.choices = choices;
            
            // Set custom name formatter
            _comparisonDateDropdown.formatSelectedValueCallback = dateStr => 
                DateTime.TryParse(dateStr, out DateTime date) ? date.ToString("d MMMM yyyy") : dateStr;
            _comparisonDateDropdown.formatListItemCallback = dateStr => 
                DateTime.TryParse(dateStr, out DateTime date) ? date.ToString("d MMMM yyyy") : dateStr;
            
            // Restore selection if possible
            if (!string.IsNullOrEmpty(currentSelection) && choices.Contains(currentSelection))
            {
                _comparisonDateDropdown.value = currentSelection;
            }
            else if (choices.Count > 0)
            {
                _comparisonDateDropdown.value = choices[0];
                
                // If comparison mode is active and no character is selected, select this date
                if (_viewModel.ComparisonModeActive.Value && 
                    string.IsNullOrEmpty(_viewModel.ComparisonCharacterId.Value) &&
                    DateTime.TryParse(choices[0], out DateTime date))
                {
                    _viewModel.SelectHistoricalDate(date);
                }
            }
        }
        
        /// <summary>
        /// Shows the specified tab
        /// </summary>
        /// <param name="tabIndex">The index of the tab to show</param>
        private void ShowTab(int tabIndex)
        {
            // Update tab buttons
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (i == tabIndex)
                {
                    _tabButtons[i].AddToClassList("active-tab");
                    _tabButtons[i].SetValueWithoutNotify(true);
                }
                else
                {
                    _tabButtons[i].RemoveFromClassList("active-tab");
                    _tabButtons[i].SetValueWithoutNotify(false);
                }
            }
            
            // Show selected tab panel, hide others
            for (int i = 0; i < _tabPanels.Count; i++)
            {
                if (i == tabIndex && _tabPanels[i] != null)
                {
                    _tabPanels[i].style.display = DisplayStyle.Flex;
                }
                else if (_tabPanels[i] != null)
                {
                    _tabPanels[i].style.display = DisplayStyle.None;
                }
            }
            
            // Update view model
            if (_viewModel != null && _viewModel.SelectedTabIndex.Value != tabIndex)
            {
                _viewModel.SelectedTabIndex.Value = tabIndex;
            }
            
            // Refresh the visible tab
            RefreshTab(tabIndex);
        }
        
        /// <summary>
        /// Refreshes all tabs
        /// </summary>
        private void RefreshAllTabs()
        {
            for (int i = 0; i < _tabPanels.Count; i++)
            {
                RefreshTab(i);
            }
        }
        
        /// <summary>
        /// Refreshes a specific tab
        /// </summary>
        /// <param name="tabIndex">The index of the tab to refresh</param>
        private void RefreshTab(int tabIndex)
        {
            if (_viewModel == null || _viewModel.SelectedCharacter.Value == null) return;
            
            switch (tabIndex)
            {
                case 0: // Character Info
                    RefreshCharacterInfoTab();
                    break;
                case 1: // Stats
                    RefreshStatsTab();
                    break;
                case 2: // Condition
                    RefreshConditionTab();
                    break;
                case 3: // Inventory
                    RefreshInventoryTab();
                    break;
                case 4: // Social
                    RefreshSocialTab();
                    break;
                case 5: // Goals
                    RefreshGoalsTab();
                    break;
            }
        }
        
        /// <summary>
        /// Updates the comparison mode UI
        /// </summary>
        /// <param name="active">Whether comparison mode is active</param>
        private void UpdateComparisonMode(bool active)
        {
            // Update comparison panel visibility
            var comparisonPanel = _rootElement.Q<VisualElement>(COMPARISON_PANEL_NAME);
            if (comparisonPanel != null)
            {
                comparisonPanel.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            // Update toggle button styling
            if (_toggleComparisonButton != null)
            {
                if (active)
                {
                    _toggleComparisonButton.AddToClassList("active-button");
                    _toggleComparisonButton.text = "Disable Comparison";
                }
                else
                {
                    _toggleComparisonButton.RemoveFromClassList("active-button");
                    _toggleComparisonButton.text = "Enable Comparison";
                }
            }
            
            // Update dropdown visibility
            if (_comparisonCharacterDropdown != null)
            {
                _comparisonCharacterDropdown.style.display = 
                    (active && string.IsNullOrEmpty(_viewModel.HistoricalComparisonDate.Value.ToString())) 
                    ? DisplayStyle.Flex 
                    : DisplayStyle.None;
            }
            
            if (_comparisonDateDropdown != null)
            {
                _comparisonDateDropdown.style.display = 
                    (active && string.IsNullOrEmpty(_viewModel.ComparisonCharacterId.Value)) 
                    ? DisplayStyle.Flex 
                    : DisplayStyle.None;
                
                // Update historical dates if showing this dropdown
                if (_comparisonDateDropdown.style.display == DisplayStyle.Flex)
                {
                    UpdateHistoricalDateDropdown();
                }
            }
            
            // Refresh comparison views
            RefreshComparisonViews();
        }
        
        /// <summary>
        /// Refreshes views that display comparison data
        /// </summary>
        private void RefreshComparisonViews()
        {
            // Refresh the stats tab which contains comparison graphs
            RefreshStatsTab();
            
            // Also refresh condition tab which may show comparison
            RefreshConditionTab();
        }
        
        /// <summary>
        /// Refreshes the character info tab
        /// </summary>
        private void RefreshCharacterInfoTab()
        {
            var character = _viewModel.SelectedCharacter.Value;
            if (character == null || _characterInfoTab == null) return;
            
            // Portrait section
            if (_portraitSection != null)
            {
                var portraitImage = _portraitSection.Q<VisualElement>("character-portrait");
                if (portraitImage != null && character.BasicInfo.Portrait != null)
                {
                    var backgroundImage = new StyleBackground(character.BasicInfo.Portrait);
                    portraitImage.style.backgroundImage = backgroundImage;
                }
            }
            
            // Basic info section
            if (_basicInfoSection != null)
            {
                var nameLabel = _basicInfoSection.Q<Label>("character-name");
                if (nameLabel != null)
                {
                    nameLabel.text = character.BasicInfo.Name;
                }
                
                var ageLabel = _basicInfoSection.Q<Label>("character-age");
                if (ageLabel != null)
                {
                    ageLabel.text = $"Age: {character.BasicInfo.Age}";
                }
                
                var occupationLabel = _basicInfoSection.Q<Label>("character-occupation");
                if (occupationLabel != null)
                {
                    occupationLabel.text = $"Occupation: {character.BasicInfo.Occupation}";
                }
            }
            
            // Biography section
            if (_biographySection != null)
            {
                var biographyText = _biographySection.Q<Label>("biography-text");
                if (biographyText != null)
                {
                    biographyText.text = character.BasicInfo.Background;
                }
            }
        }
        
        /// <summary>
        /// Refreshes the stats tab
        /// </summary>
        private void RefreshStatsTab()
        {
            var character = _viewModel.SelectedCharacter.Value;
            if (character == null || _statsTab == null) return;
            
            // Stats bars section
            if (_statBarsSection != null)
            {
                _statBarsSection.Clear();
                
                // Add stat bars for each stat
                foreach (var statPair in character.Stats)
                {
                    AddStatBar(statPair.Key, statPair.Value);
                }
            }
            
            // Skills grid section
            if (_skillsGridSection != null)
            {
                _skillsGridSection.Clear();
                
                // Add skill items for each skill
                foreach (var skill in character.Skills)
                {
                    AddSkillItem(skill);
                }
            }
            
            // Comparison graph section
            if (_comparisonGraphSection != null)
            {
                _comparisonGraphSection.style.display = _viewModel.ComparisonModeActive.Value ? 
                    DisplayStyle.Flex : DisplayStyle.None;
                
                if (_viewModel.ComparisonModeActive.Value)
                {
                    RefreshComparisonGraph();
                }
            }
        }
        
        /// <summary>
        /// Refreshes the condition tab
        /// </summary>
        private void RefreshConditionTab()
        {
            var character = _viewModel.SelectedCharacter.Value;
            if (character == null || _conditionTab == null) return;
            
            // Mental state section
            if (_mentalStateSection != null && character.MentalState != null)
            {
                // Update stress gauge
                var stressGauge = _mentalStateSection.Q<VisualElement>("stress-gauge");
                if (stressGauge != null)
                {
                    var stressFill = stressGauge.Q<VisualElement>("stress-fill");
                    if (stressFill != null)
                    {
                        stressFill.style.width = new Length(character.MentalState.Stress * 100, LengthUnit.Percent);
                    }
                    
                    var stressLabel = stressGauge.Q<Label>("stress-value");
                    if (stressLabel != null)
                    {
                        stressLabel.text = $"{Mathf.Round(character.MentalState.Stress * 100)}%";
                    }
                }
                
                // Update happiness gauge
                var happinessGauge = _mentalStateSection.Q<VisualElement>("happiness-gauge");
                if (happinessGauge != null)
                {
                    var happinessFill = happinessGauge.Q<VisualElement>("happiness-fill");
                    if (happinessFill != null)
                    {
                        happinessFill.style.width = new Length(character.MentalState.Happiness * 100, LengthUnit.Percent);
                    }
                    
                    var happinessLabel = happinessGauge.Q<Label>("happiness-value");
                    if (happinessLabel != null)
                    {
                        happinessLabel.text = $"{Mathf.Round(character.MentalState.Happiness * 100)}%";
                    }
                }
                
                // Update motivation gauge
                var motivationGauge = _mentalStateSection.Q<VisualElement>("motivation-gauge");
                if (motivationGauge != null)
                {
                    var motivationFill = motivationGauge.Q<VisualElement>("motivation-fill");
                    if (motivationFill != null)
                    {
                        motivationFill.style.width = new Length(character.MentalState.Motivation * 100, LengthUnit.Percent);
                    }
                    
                    var motivationLabel = motivationGauge.Q<Label>("motivation-value");
                    if (motivationLabel != null)
                    {
                        motivationLabel.text = $"{Mathf.Round(character.MentalState.Motivation * 100)}%";
                    }
                }
                
                // Update emotion chart
                var emotionChart = _mentalStateSection.Q<VisualElement>("emotion-chart");
                if (emotionChart != null)
                {
                    emotionChart.Clear();
                    
                    // Add entries for each emotion
                    foreach (var emotionPair in character.MentalState.Emotions)
                    {
                        var emotionBar = new VisualElement();
                        emotionBar.AddToClassList("emotion-bar");
                        
                        var emotionLabel = new Label(emotionPair.Key.ToString());
                        emotionLabel.AddToClassList("emotion-label");
                        emotionBar.Add(emotionLabel);
                        
                        var emotionValue = new VisualElement();
                        emotionValue.AddToClassList("emotion-value");
                        
                        var emotionFill = new VisualElement();
                        emotionFill.AddToClassList("emotion-fill");
                        emotionFill.style.width = new Length(emotionPair.Value * 100, LengthUnit.Percent);
                        emotionValue.Add(emotionFill);
                        emotionBar.Add(emotionValue);
                        
                        var valueLabel = new Label($"{Mathf.Round(emotionPair.Value * 100)}%");
                        valueLabel.AddToClassList("emotion-value-label");
                        emotionBar.Add(valueLabel);
                        
                        emotionChart.Add(emotionBar);
                    }
                }
                
                // Update mood effects list
                var moodEffectsList = _mentalStateSection.Q<VisualElement>("mood-effects-list");
                if (moodEffectsList != null)
                {
                    moodEffectsList.Clear();
                    
                    foreach (var effect in character.MentalState.ActiveEffects)
                    {
                        var effectItem = _conditionItemTemplate.Instantiate()[0];
                        
                        var nameLabel = effectItem.Q<Label>("condition-name");
                        if (nameLabel != null)
                        {
                            nameLabel.text = effect.Name;
                        }
                        
                        var descriptionLabel = effectItem.Q<Label>("condition-description");
                        if (descriptionLabel != null)
                        {
                            descriptionLabel.text = effect.Description;
                        }
                        
                        var severityIndicator = effectItem.Q<VisualElement>("severity-indicator");
                        if (severityIndicator != null)
                        {
                            severityIndicator.style.backgroundColor = new Color(0.3f, 0.6f, 0.9f, effect.Intensity);
                        }
                        
                        var durationLabel = effectItem.Q<Label>("duration-label");
                        if (durationLabel != null)
                        {
                            TimeSpan remaining = effect.GetRemainingTime(DateTime.Now);
                            durationLabel.text = remaining > TimeSpan.Zero ? 
                                $"Expires in {FormatTimeSpan(remaining)}" : 
                                "Expired";
                        }
                        
                        moodEffectsList.Add(effectItem);
                    }
                }
            }
            
            // Physical state section
            if (_physicalStateSection != null && character.PhysicalCondition != null)
            {
                // Update stamina gauge
                var staminaGauge = _physicalStateSection.Q<VisualElement>("stamina-gauge");
                if (staminaGauge != null)
                {
                    var staminaFill = staminaGauge.Q<VisualElement>("stamina-fill");
                    if (staminaFill != null)
                    {
                        staminaFill.style.width = new Length(character.PhysicalCondition.Stamina * 100, LengthUnit.Percent);
                    }
                    
                    var staminaLabel = staminaGauge.Q<Label>("stamina-value");
                    if (staminaLabel != null)
                    {
                        staminaLabel.text = $"{Mathf.Round(character.PhysicalCondition.Stamina * 100)}%";
                    }
                }
                
                // Update energy gauge
                var energyGauge = _physicalStateSection.Q<VisualElement>("energy-gauge");
                if (energyGauge != null)
                {
                    var energyFill = energyGauge.Q<VisualElement>("energy-fill");
                    if (energyFill != null)
                    {
                        energyFill.style.width = new Length(character.PhysicalCondition.Energy * 100, LengthUnit.Percent);
                    }
                    
                    var energyLabel = energyGauge.Q<Label>("energy-value");
                    if (energyLabel != null)
                    {
                        energyLabel.text = $"{Mathf.Round(character.PhysicalCondition.Energy * 100)}%";
                    }
                }
                
                // Update health gauge
                var healthGauge = _physicalStateSection.Q<VisualElement>("health-gauge");
                if (healthGauge != null)
                {
                    var healthFill = healthGauge.Q<VisualElement>("health-fill");
                    if (healthFill != null)
                    {
                        healthFill.style.width = new Length(character.PhysicalCondition.Health * 100, LengthUnit.Percent);
                    }
                    
                    var healthLabel = healthGauge.Q<Label>("health-value");
                    if (healthLabel != null)
                    {
                        healthLabel.text = $"{Mathf.Round(character.PhysicalCondition.Health * 100)}%";
                    }
                }
                
                // Update conditions list
                var conditionsList = _physicalStateSection.Q<VisualElement>("conditions-list");
                if (conditionsList != null)
                {
                    conditionsList.Clear();
                    
                    foreach (var condition in character.PhysicalCondition.ActiveConditions)
                    {
                        var conditionItem = _conditionItemTemplate.Instantiate()[0];
                        
                        var nameLabel = conditionItem.Q<Label>("condition-name");
                        if (nameLabel != null)
                        {
                            nameLabel.text = condition.Name;
                        }
                        
                        var descriptionLabel = conditionItem.Q<Label>("condition-description");
                        if (descriptionLabel != null)
                        {
                            descriptionLabel.text = condition.Description;
                        }
                        
                        var severityIndicator = conditionItem.Q<VisualElement>("severity-indicator");
                        if (severityIndicator != null)
                        {
                            // Color based on condition type
                            Color conditionColor;
                            switch (condition.Type)
                            {
                                case ConditionType.Injured:
                                    conditionColor = new Color(0.9f, 0.2f, 0.2f); // Red
                                    break;
                                case ConditionType.Sick:
                                    conditionColor = new Color(0.9f, 0.6f, 0.2f); // Orange
                                    break;
                                case ConditionType.Exhausted:
                                    conditionColor = new Color(0.6f, 0.3f, 0.9f); // Purple
                                    break;
                                case ConditionType.Hungry:
                                    conditionColor = new Color(0.8f, 0.8f, 0.2f); // Yellow
                                    break;
                                case ConditionType.Thirsty:
                                    conditionColor = new Color(0.2f, 0.6f, 0.9f); // Blue
                                    break;
                                case ConditionType.Intoxicated:
                                    conditionColor = new Color(0.2f, 0.9f, 0.6f); // Teal
                                    break;
                                default:
                                    conditionColor = new Color(0.7f, 0.7f, 0.7f); // Gray
                                    break;
                            }
                            
                            severityIndicator.style.backgroundColor = new Color(
                                conditionColor.r, 
                                conditionColor.g, 
                                conditionColor.b, 
                                condition.Severity
                            );
                        }
                        
                        var durationLabel = conditionItem.Q<Label>("duration-label");
                        if (durationLabel != null)
                        {
                            if (condition.IsPermanent())
                            {
                                durationLabel.text = "Permanent";
                            }
                            else
                            {
                                TimeSpan remaining = condition.GetRemainingTime(DateTime.Now);
                                durationLabel.text = remaining > TimeSpan.Zero ? 
                                    $"Expires in {FormatTimeSpan(remaining)}" : 
                                    "Expired";
                            }
                        }
                        
                        conditionsList.Add(conditionItem);
                    }
                }
            }
        }
        
        /// <summary>
        /// Refreshes the inventory tab
        /// </summary>
        private void RefreshInventoryTab()
        {
            var character = _viewModel.SelectedCharacter.Value;
            if (character == null || _inventoryTab == null) return;
            
            // Item grid view
            if (_itemGridView != null && character.Inventory != null)
            {
                _itemGridView.Clear();
                
                foreach (var item in character.Inventory.Items)
                {
                    var itemElement = _inventoryItemTemplate.Instantiate()[0];
                    
                    var iconImage = itemElement.Q<VisualElement>("item-icon");
                    if (iconImage != null && item.Icon != null)
                    {
                        var backgroundImage = new StyleBackground(item.Icon);
                        iconImage.style.backgroundImage = backgroundImage;
                    }
                    
                    var nameLabel = itemElement.Q<Label>("item-name");
                    if (nameLabel != null)
                    {
                        nameLabel.text = item.Name;
                    }
                    
                    var quantityLabel = itemElement.Q<Label>("item-quantity");
                    if (quantityLabel != null)
                    {
                        quantityLabel.text = item.Quantity > 1 ? item.Quantity.ToString() : "";
                    }
                    
                    var typeIndicator = itemElement.Q<VisualElement>("item-type-indicator");
                    if (typeIndicator != null)
                    {
                        // Apply styling based on item type
                        ApplyItemTypeStyle(typeIndicator, item.Type);
                    }
                    
                    // Add tooltip to show item description on hover
                    var tooltipContainer = itemElement.Q<VisualElement>("tooltip-container");
                    if (tooltipContainer != null)
                    {
                        var descriptionLabel = tooltipContainer.Q<Label>("item-description");
                        if (descriptionLabel != null)
                        {
                            descriptionLabel.text = item.Description;
                        }
                        
                        var valueLabel = tooltipContainer.Q<Label>("item-value");
                        if (valueLabel != null)
                        {
                            valueLabel.text = $"Value: {item.Value:F1}";
                        }
                    }
                    
                    // Setup hover behavior
                    itemElement.RegisterCallback<MouseEnterEvent>(evt => 
                    {
                        if (tooltipContainer != null)
                        {
                            tooltipContainer.style.display = DisplayStyle.Flex;
                        }
                    });
                    
                    itemElement.RegisterCallback<MouseLeaveEvent>(evt => 
                    {
                        if (tooltipContainer != null)
                        {
                            tooltipContainer.style.display = DisplayStyle.None;
                        }
                    });
                    
                    _itemGridView.Add(itemElement);
                }
            }
            
            // Equipment slots
            if (_equipmentSlots != null && character.Inventory != null)
            {
                // Update each equipment slot
                foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
                {
                    if (slot == EquipSlot.Custom) continue;
                    
                    var slotElement = _equipmentSlots.Q<VisualElement>($"slot-{slot.ToString().ToLower()}");
                    if (slotElement != null)
                    {
                        var item = character.Inventory.GetEquippedItem(slot);
                        
                        // Update slot with item info or show empty
                        var iconElement = slotElement.Q<VisualElement>("slot-icon");
                        if (iconElement != null)
                        {
                            if (item != null && item.Icon != null)
                            {
                                var backgroundImage = new StyleBackground(item.Icon);
                                iconElement.style.backgroundImage = backgroundImage;
                                iconElement.style.opacity = 1;
                            }
                            else
                            {
                                iconElement.style.backgroundImage = new StyleBackground(StyleKeyword.None);
                                iconElement.style.opacity = 0.3f;
                            }
                        }
                        
                        var nameLabel = slotElement.Q<Label>("slot-item-name");
                        if (nameLabel != null)
                        {
                            nameLabel.text = item != null ? item.Name : "Empty";
                        }
                    }
                }
            }
            
            // Resource counters
            if (_resourceCounters != null && character.Inventory != null)
            {
                _resourceCounters.Clear();
                
                foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
                {
                    if (resourceType == ResourceType.Custom) continue;
                    
                    float amount = character.Inventory.GetResourceAmount(resourceType);
                    
                    // Skip if zero
                    if (Mathf.Approximately(amount, 0)) continue;
                    
                    var resourceElement = new VisualElement();
                    resourceElement.AddToClassList("resource-counter");
                    
                    var iconElement = new VisualElement();
                    iconElement.AddToClassList("resource-icon");
                    iconElement.AddToClassList($"resource-{resourceType.ToString().ToLower()}");
                    resourceElement.Add(iconElement);
                    
                    var nameLabel = new Label(resourceType.ToString());
                    nameLabel.AddToClassList("resource-name");
                    resourceElement.Add(nameLabel);
                    
                    var valueLabel = new Label(amount.ToString("F1"));
                    valueLabel.AddToClassList("resource-value");
                    resourceElement.Add(valueLabel);
                    
                    _resourceCounters.Add(resourceElement);
                }
            }
        }
        
        /// <summary>
        /// Refreshes the social tab
        /// </summary>
        private void RefreshSocialTab()
        {
            var character = _viewModel.SelectedCharacter.Value;
            if (character == null || _socialTab == null) return;
            
            // Reputation chart
            if (_reputationChart != null && character.SocialStatus != null)
            {
                _reputationChart.Clear();
                
                foreach (var reputationPair in character.SocialStatus.Reputation)
                {
                    var reputationBar = new VisualElement();
                    reputationBar.AddToClassList("reputation-bar");
                    
                    var groupLabel = new Label(reputationPair.Key);
                    groupLabel.AddToClassList("group-label");
                    reputationBar.Add(groupLabel);
                    
                    var reputationValue = new VisualElement();
                    reputationValue.AddToClassList("reputation-value");
                    
                    var reputationFill = new VisualElement();
                    reputationFill.AddToClassList("reputation-fill");
                    reputationFill.style.width = new Length(reputationPair.Value * 100, LengthUnit.Percent);
                    reputationValue.Add(reputationFill);
                    reputationBar.Add(reputationValue);
                    
                    var valueLabel = new Label($"{Mathf.Round(reputationPair.Value * 100)}%");
                    valueLabel.AddToClassList("reputation-value-label");
                    reputationBar.Add(valueLabel);
                    
                    _reputationChart.Add(reputationBar);
                }
                
                // Add overall fame bar
                var fameBar = new VisualElement();
                fameBar.AddToClassList("reputation-bar");
                fameBar.AddToClassList("fame-bar");
                
                var fameLabel = new Label("Overall Fame");
                fameLabel.AddToClassList("group-label");
                fameBar.Add(fameLabel);
                
                var fameValue = new VisualElement();
                fameValue.AddToClassList("reputation-value");
                
                var fameFill = new VisualElement();
                fameFill.AddToClassList("reputation-fill");
                fameFill.AddToClassList("fame-fill");
                fameFill.style.width = new Length(character.SocialStatus.OverallFame * 100, LengthUnit.Percent);
                fameValue.Add(fameFill);
                fameBar.Add(fameValue);
                
                var fameValueLabel = new Label($"{Mathf.Round(character.SocialStatus.OverallFame * 100)}%");
                fameValueLabel.AddToClassList("reputation-value-label");
                fameBar.Add(fameValueLabel);
                
                _reputationChart.Add(fameBar);
            }
            
            // Achievements section
            if (_achievementsSection != null && character.SocialStatus != null)
            {
                var achievementsList = _achievementsSection.Q<VisualElement>("achievements-list");
                if (achievementsList != null)
                {
                    achievementsList.Clear();
                    
                    foreach (var achievement in character.SocialStatus.Achievements)
                    {
                        var achievementItem = new VisualElement();
                        achievementItem.AddToClassList("achievement-item");
                        
                        if (!achievement.IsUnlocked)
                        {
                            achievementItem.AddToClassList("locked-achievement");
                        }
                        
                        var nameLabel = new Label(achievement.Name);
                        nameLabel.AddToClassList("achievement-name");
                        achievementItem.Add(nameLabel);
                        
                        var descriptionLabel = new Label(achievement.Description);
                        descriptionLabel.AddToClassList("achievement-description");
                        achievementItem.Add(descriptionLabel);
                        
                        if (achievement.IsUnlocked)
                        {
                            var unlockDateLabel = new Label($"Unlocked: {achievement.UnlockDate:d MMM yyyy}");
                            unlockDateLabel.AddToClassList("unlock-date");
                            achievementItem.Add(unlockDateLabel);
                        }
                        
                        var prestigeLabel = new Label($"Prestige: {achievement.PrestigeValue:F1}");
                        prestigeLabel.AddToClassList("prestige-value");
                        achievementItem.Add(prestigeLabel);
                        
                        achievementsList.Add(achievementItem);
                    }
                }
            }
            
            // Titles section
            if (_titlesSection != null && character.SocialStatus != null)
            {
                var titlesList = _titlesSection.Q<VisualElement>("titles-list");
                if (titlesList != null)
                {
                    titlesList.Clear();
                    
                    foreach (var title in character.SocialStatus.Titles)
                    {
                        var titleItem = new VisualElement();
                        titleItem.AddToClassList("title-item");
                        
                        var titleLabel = new Label(title);
                        titleItem.Add(titleLabel);
                        
                        titlesList.Add(titleItem);
                    }
                }
            }
        }
        
        /// <summary>
        /// Refreshes the goals tab
        /// </summary>
        private void RefreshGoalsTab()
        {
            var character = _viewModel.SelectedCharacter.Value;
            if (character == null || _goalsTab == null) return;
            
            // Active goals list
            if (_activeGoalsList != null)
            {
                _activeGoalsList.Clear();
                
                foreach (var goal in character.GetActiveGoals())
                {
                    AddGoalItem(_activeGoalsList, goal);
                }
            }
            
            // Progress trackers
            if (_progressTrackers != null)
            {
                // This could show overall progress across all goals or other metrics
                // For now, leave empty
            }
            
            // Completed goals archive
            if (_completedGoalsArchive != null)
            {
                _completedGoalsArchive.Clear();
                
                foreach (var goal in character.GetCompletedGoals())
                {
                    AddGoalItem(_completedGoalsArchive, goal, true);
                }
            }
        }
        
        /// <summary>
        /// Adds a stat bar to the stats bars section
        /// </summary>
        /// <param name="statType">The type of stat</param>
        /// <param name="value">The stat value</param>
        private void AddStatBar(StatType statType, float value)
        {
            if (_statBarsSection == null || _statBarTemplate == null) return;
            
            var statBar = _statBarTemplate.Instantiate()[0];
            
            // Set stat name
            var nameLabel = statBar.Q<Label>("stat-name");
            if (nameLabel != null)
            {
                nameLabel.text = statType.ToString();
            }
            
            // Set stat value
            var valueLabel = statBar.Q<Label>("stat-value");
            if (valueLabel != null)
            {
                valueLabel.text = $"{Mathf.Round(value * 100)}";
            }
            
            // Set stat fill
            var statFill = statBar.Q<VisualElement>("stat-fill");
            if (statFill != null)
            {
                statFill.style.width = new Length(value * 100, LengthUnit.Percent);
            }
            
            // Apply color based on stat type
            ApplyStatTypeColor(statBar, statType);
            
            // Add comparison data if in comparison mode
            if (_viewModel.ComparisonModeActive.Value)
            {
                var comparisonData = _viewModel.GetComparisonData();
                if (comparisonData != null && comparisonData.StatDifferences.TryGetValue(statType, out float difference))
                {
                    // Add comparison indicator
                    var comparisonIndicator = statBar.Q<VisualElement>("comparison-indicator");
                    if (comparisonIndicator != null)
                    {
                        comparisonIndicator.style.display = DisplayStyle.Flex;
                        
                        // Position indicator based on difference
                        if (difference > 0)
                        {
                            comparisonIndicator.AddToClassList("positive-difference");
                            comparisonIndicator.RemoveFromClassList("negative-difference");
                        }
                        else if (difference < 0)
                        {
                            comparisonIndicator.AddToClassList("negative-difference");
                            comparisonIndicator.RemoveFromClassList("positive-difference");
                        }
                        else
                        {
                            comparisonIndicator.RemoveFromClassList("positive-difference");
                            comparisonIndicator.RemoveFromClassList("negative-difference");
                        }
                        
                        // Show difference value
                        var differenceLabel = comparisonIndicator.Q<Label>("difference-value");
                        if (differenceLabel != null)
                        {
                            differenceLabel.text = difference > 0 ? 
                                $"+{Mathf.Round(difference * 100)}" : 
                                $"{Mathf.Round(difference * 100)}";
                        }
                    }
                }
            }
            
            _statBarsSection.Add(statBar);
        }
        
        /// <summary>
        /// Adds a skill item to the skills grid section
        /// </summary>
        /// <param name="skill">The skill data</param>
        private void AddSkillItem(Skill skill)
        {
            if (_skillsGridSection == null || _skillItemTemplate == null) return;
            
            var skillItem = _skillItemTemplate.Instantiate()[0];
            
            // Set skill name
            var nameLabel = skillItem.Q<Label>("skill-name");
            if (nameLabel != null)
            {
                nameLabel.text = skill.Name;
            }
            
            // Set skill level
            var levelLabel = skillItem.Q<Label>("skill-level");
            if (levelLabel != null)
            {
                levelLabel.text = $"Level: {Mathf.Floor(skill.Level * 100)}";
            }
            
            // Set experience progress
            var experienceProgress = skillItem.Q<VisualElement>("experience-progress");
            if (experienceProgress != null)
            {
                var experienceFill = experienceProgress.Q<VisualElement>("experience-fill");
                if (experienceFill != null)
                {
                    experienceFill.style.width = new Length(skill.GetProgressToNextLevel() * 100, LengthUnit.Percent);
                }
            }
            
            // Add comparison data if in comparison mode
            if (_viewModel.ComparisonModeActive.Value)
            {
                var comparisonData = _viewModel.GetComparisonData();
                if (comparisonData != null && comparisonData.SkillDifferences.TryGetValue(skill.Name, out float difference))
                {
                    // Add comparison indicator
                    var comparisonIndicator = skillItem.Q<VisualElement>("comparison-indicator");
                    if (comparisonIndicator != null)
                    {
                        comparisonIndicator.style.display = DisplayStyle.Flex;
                        
                        // Position indicator based on difference
                        if (difference > 0)
                        {
                            comparisonIndicator.AddToClassList("positive-difference");
                            comparisonIndicator.RemoveFromClassList("negative-difference");
                        }
                        else if (difference < 0)
                        {
                            comparisonIndicator.AddToClassList("negative-difference");
                            comparisonIndicator.RemoveFromClassList("positive-difference");
                        }
                        else
                        {
                            comparisonIndicator.RemoveFromClassList("positive-difference");
                            comparisonIndicator.RemoveFromClassList("negative-difference");
                        }
                        
                        // Show difference value
                        var differenceLabel = comparisonIndicator.Q<Label>("difference-value");
                        if (differenceLabel != null)
                        {
                            differenceLabel.text = difference > 0 ? 
                                $"+{Mathf.Round(difference * 100)}" : 
                                $"{Mathf.Round(difference * 100)}";
                        }
                    }
                }
            }
            
            _skillsGridSection.Add(skillItem);
        }
        
        /// <summary>
        /// Adds a goal item to the specified container
        /// </summary>
        /// <param name="container">The container to add the goal item to</param>
        /// <param name="goal">The goal data</param>
        /// <param name="isCompleted">Whether the goal is completed</param>
        private void AddGoalItem(VisualElement container, Goal goal, bool isCompleted = false)
        {
            if (container == null || _goalItemTemplate == null) return;
            
            var goalItem = _goalItemTemplate.Instantiate()[0];
            
            // Set goal title
            var titleLabel = goalItem.Q<Label>("goal-title");
            if (titleLabel != null)
            {
                titleLabel.text = goal.Description;
            }
            
            // Set progress
            var progressBar = goalItem.Q<VisualElement>("goal-progress");
            if (progressBar != null)
            {
                var progressFill = progressBar.Q<VisualElement>("progress-fill");
                if (progressFill != null)
                {
                    progressFill.style.width = new Length(goal.Progress * 100, LengthUnit.Percent);
                }
                
                var progressLabel = progressBar.Q<Label>("progress-value");
                if (progressLabel != null)
                {
                    progressLabel.text = $"{Mathf.Round(goal.Progress * 100)}%";
                }
            }
            
            // Set deadline
            var deadlineLabel = goalItem.Q<Label>("goal-deadline");
            if (deadlineLabel != null)
            {
                if (goal.Deadline != default)
                {
                    if (goal.IsOverdue(DateTime.Now))
                    {
                        deadlineLabel.text = $"Overdue: {goal.Deadline:d MMM yyyy}";
                        deadlineLabel.AddToClassList("overdue");
                    }
                    else
                    {
                        deadlineLabel.text = $"Deadline: {goal.Deadline:d MMM yyyy} ({FormatTimeSpan(goal.GetTimeRemaining(DateTime.Now))} remaining)";
                    }
                }
                else
                {
                    deadlineLabel.text = "No deadline";
                }
            }
            
            // Show subgoals if any
            var subgoalsList = goalItem.Q<VisualElement>("subgoals-list");
            if (subgoalsList != null && goal.SubGoals.Count > 0)
            {
                subgoalsList.Clear();
                
                foreach (var subgoal in goal.SubGoals)
                {
                    var subgoalItem = new VisualElement();
                    subgoalItem.AddToClassList("subgoal-item");
                    
                    if (subgoal.IsCompleted)
                    {
                        subgoalItem.AddToClassList("completed-subgoal");
                    }
                    
                    var checkbox = new VisualElement();
                    checkbox.AddToClassList("subgoal-checkbox");
                    if (subgoal.IsCompleted)
                    {
                        checkbox.AddToClassList("checked");
                    }
                    subgoalItem.Add(checkbox);
                    
                    var subgoalLabel = new Label(subgoal.Description);
                    subgoalItem.Add(subgoalLabel);
                    
                    subgoalsList.Add(subgoalItem);
                }
            }
            
            // Add progress controls for active goals
            if (!isCompleted)
            {
                var controlsSection = goalItem.Q<VisualElement>("goal-controls");
                if (controlsSection != null)
                {
                    controlsSection.style.display = DisplayStyle.Flex;
                    
                    var decreaseButton = controlsSection.Q<Button>("decrease-progress");
                    var increaseButton = controlsSection.Q<Button>("increase-progress");
                    
                    if (decreaseButton != null)
                    {
                        decreaseButton.clicked += () => 
                        {
                            float newProgress = Mathf.Max(0, goal.Progress - 0.1f);
                            _viewModel.UpdateGoalProgress(goal.GoalID, newProgress);
                        };
                    }
                    
                    if (increaseButton != null)
                    {
                        increaseButton.clicked += () => 
                        {
                            float newProgress = Mathf.Min(1, goal.Progress + 0.1f);
                            _viewModel.UpdateGoalProgress(goal.GoalID, newProgress);
                        };
                    }
                }
            }
            else
            {
                // For completed goals, show completion status
                var statusLabel = goalItem.Q<Label>("goal-status");
                if (statusLabel != null)
                {
                    statusLabel.text = "Completed";
                    statusLabel.AddToClassList("completed-status");
                }
            }
            
            container.Add(goalItem);
        }
        
        /// <summary>
        /// Refreshes the comparison graph
        /// </summary>
        private void RefreshComparisonGraph()
        {
            if (_comparisonGraphSection == null || !_viewModel.ComparisonModeActive.Value) return;
            
            var comparisonData = _viewModel.GetComparisonData();
            if (comparisonData == null) return;
            
            _comparisonGraphSection.Clear();
            
            // Add comparison title
            var titleLabel = new Label($"Comparison with {comparisonData.ComparisonName}");
            titleLabel.AddToClassList("comparison-title");
            _comparisonGraphSection.Add(titleLabel);
            
            // Create radar chart (or similar visualization)
            var chartContainer = new VisualElement();
            chartContainer.AddToClassList("comparison-chart");
            _comparisonGraphSection.Add(chartContainer);
            
            // Create stats comparison table
            var statsTable = new VisualElement();
            statsTable.AddToClassList("comparison-table");
            
            // Add header row
            var headerRow = new VisualElement();
            headerRow.AddToClassList("table-row");
            headerRow.AddToClassList("header-row");
            
            headerRow.Add(new Label("Stat"));
            headerRow.Add(new Label("Current"));
            headerRow.Add(new Label("Compared"));
            headerRow.Add(new Label("Diff"));
            
            statsTable.Add(headerRow);
            
            // Add rows for each stat
            var character = _viewModel.SelectedCharacter.Value;
            if (character == null) return;
            
            foreach (var statPair in character.Stats)
            {
                float currentValue = statPair.Value;
                float comparisonValue = 0;
                float difference = 0;
                
                if (comparisonData.StatDifferences.TryGetValue(statPair.Key, out float diff))
                {
                    if (comparisonData.ComparisonType == ComparisonType.Character)
                    {
                        // For character comparison, current - diff = comparison value
                        comparisonValue = currentValue - diff;
                        difference = diff;
                    }
                    else
                    {
                        // For historical comparison, current - diff = historical value
                        comparisonValue = currentValue - diff;
                        difference = diff;
                    }
                }
                
                var row = new VisualElement();
                row.AddToClassList("table-row");
                
                row.Add(new Label(statPair.Key.ToString()));
                row.Add(new Label($"{Mathf.Round(currentValue * 100)}"));
                row.Add(new Label($"{Mathf.Round(comparisonValue * 100)}"));
                
                var diffLabel = new Label(difference > 0 ? 
                    $"+{Mathf.Round(difference * 100)}" : 
                    $"{Mathf.Round(difference * 100)}");
                
                if (difference > 0)
                {
                    diffLabel.AddToClassList("positive-diff");
                }
                else if (difference < 0)
                {
                    diffLabel.AddToClassList("negative-diff");
                }
                
                row.Add(diffLabel);
                
                statsTable.Add(row);
            }
            
            _comparisonGraphSection.Add(statsTable);
            
            // Create snapshot record button if comparing with historical data
            if (comparisonData.ComparisonType == ComparisonType.Historical)
            {
                var snapshotButton = new Button { text = "Record Current Status" };
                snapshotButton.AddToClassList("snapshot-button");
                snapshotButton.clicked += () => _viewModel.RecordStatusSnapshot();
                
                _comparisonGraphSection.Add(snapshotButton);
            }
        }
        
        /// <summary>
        /// Applies color styling based on stat type
        /// </summary>
        /// <param name="element">The element to style</param>
        /// <param name="statType">The stat type</param>
        private void ApplyStatTypeColor(VisualElement element, StatType statType)
        {
            var fillElement = element.Q<VisualElement>("stat-fill");
            if (fillElement == null) return;
            
            switch (statType)
            {
                case StatType.Strength:
                    fillElement.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f); // Red
                    break;
                case StatType.Dexterity:
                    fillElement.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f); // Green
                    break;
                case StatType.Constitution:
                    fillElement.style.backgroundColor = new Color(0.8f, 0.6f, 0.2f); // Orange
                    break;
                case StatType.Intelligence:
                    fillElement.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f); // Blue
                    break;
                case StatType.Wisdom:
                    fillElement.style.backgroundColor = new Color(0.6f, 0.4f, 0.8f); // Purple
                    break;
                case StatType.Charisma:
                    fillElement.style.backgroundColor = new Color(0.8f, 0.3f, 0.6f); // Pink
                    break;
                case StatType.Luck:
                    fillElement.style.backgroundColor = new Color(0.3f, 0.8f, 0.6f); // Teal
                    break;
                default:
                    fillElement.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f); // Gray
                    break;
            }
        }
        
        /// <summary>
        /// Applies styling based on item type
        /// </summary>
        /// <param name="element">The element to style</param>
        /// <param name="itemType">The item type</param>
        private void ApplyItemTypeStyle(VisualElement element, ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Equipment:
                    element.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f); // Gray
                    break;
                case ItemType.Weapon:
                    element.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f); // Red
                    break;
                case ItemType.Apparel:
                    element.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f); // Blue
                    break;
                case ItemType.Consumable:
                    element.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f); // Green
                    break;
                case ItemType.Crafting:
                    element.style.backgroundColor = new Color(0.8f, 0.6f, 0.2f); // Orange
                    break;
                case ItemType.Quest:
                    element.style.backgroundColor = new Color(0.8f, 0.3f, 0.8f); // Purple
                    break;
                case ItemType.Valuable:
                    element.style.backgroundColor = new Color(0.8f, 0.8f, 0.2f); // Yellow
                    break;
                default:
                    element.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f); // Gray
                    break;
            }
        }
        
        /// <summary>
        /// Formats a TimeSpan into a human-readable string
        /// </summary>
        /// <param name="timeSpan">The TimeSpan to format</param>
        /// <returns>A human-readable string</returns>
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
            }
            else
            {
                return $"{timeSpan.Seconds}s";
            }
        }
    }
}