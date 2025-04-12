using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;
using System.Linq;

namespace InformationManagementUI
{
    /// <summary>
    /// View controller for the calendar component
    /// </summary>
    public class CalendarViewController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset _dayCellTemplate;
        [SerializeField] private VisualTreeAsset _eventItemTemplate;
        [SerializeField] private VisualTreeAsset _weekViewHourTemplate;
        
        private CalendarViewModel _viewModel;
        private VisualElement _rootElement;
        private VisualElement _monthViewPanel;
        private VisualElement _weekViewPanel;
        private VisualElement _dayDetailPanel;
        private VisualElement _eventDetailPopup;
        private Button _monthViewButton;
        private Button _weekViewButton;
        private Button _previousButton;
        private Button _nextButton;
        private Button _todayButton;
        private Label _monthYearLabel;
        private ListView _eventsList;
        
        // Cache of month view day cells
        private List<VisualElement> _dayElements = new List<VisualElement>();
        
        // Cache of week view hour slots
        private Dictionary<int, List<VisualElement>> _weekDaySlots = new Dictionary<int, List<VisualElement>>();
        
        // Week day names
        private readonly string[] _weekDayNames = new string[] 
        { 
            "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" 
        };
        
        // Week day short names
        private readonly string[] _weekDayShortNames = new string[] 
        { 
            "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" 
        };
        
        // Month names
        private readonly string[] _monthNames = new string[]
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };
        
        // CompositeDisposable for cleanup
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        // Constants
        private const string MONTH_VIEW_PANEL_NAME = "month-view-panel";
        private const string WEEK_VIEW_PANEL_NAME = "week-view-panel";
        private const string DAY_DETAIL_PANEL_NAME = "day-detail-panel";
        private const string EVENT_DETAIL_POPUP_NAME = "event-detail-popup";
        private const string MONTH_VIEW_BUTTON_NAME = "month-view-button";
        private const string WEEK_VIEW_BUTTON_NAME = "week-view-button";
        private const string PREVIOUS_BUTTON_NAME = "previous-button";
        private const string NEXT_BUTTON_NAME = "next-button";
        private const string TODAY_BUTTON_NAME = "today-button";
        private const string MONTH_YEAR_LABEL_NAME = "month-year-label";
        private const string DAYS_CONTAINER_NAME = "days-container";
        private const string WEEK_CONTAINER_NAME = "week-container";
        private const string EVENTS_LIST_NAME = "events-list";
        private const string DAY_CELL_NAME = "day-cell";
        private const string DAY_NUMBER_NAME = "day-number";
        private const string EVENT_MARKERS_NAME = "event-markers";
        private const string SPECIAL_DATE_INDICATOR_NAME = "special-date-indicator";
        private const string DAY_COLUMN_PREFIX = "day-column-";
        private const string HOUR_SLOTS_PREFIX = "hour-slots-";
        private const string WEEK_DAY_HEADER_PREFIX = "week-day-header-";
        private const string CLOSE_POPUP_BUTTON_NAME = "close-popup-button";
        private const string EVENT_TITLE_NAME = "event-title";
        private const string EVENT_TIME_NAME = "event-time";
        private const string EVENT_DESCRIPTION_NAME = "event-description";
        private const string EVENT_COMPLETE_BUTTON_NAME = "event-complete-button";
        private const string DAY_CELL_TEMPLATE_NAME = "DayCellTemplate";
        private const string EVENT_ITEM_TEMPLATE_NAME = "EventItemTemplate";
        private const string WEEK_HOUR_TEMPLATE_NAME = "WeekHourTemplate";
        
        // UI State
        private CalendarEvent _currentlyDisplayedEvent;
        
        /// <summary>
        /// Initializes the view controller with a view model
        /// </summary>
        /// <param name="viewModel">The view model to initialize with</param>
        public void Initialize(CalendarViewModel viewModel)
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
        /// Shows the calendar view
        /// </summary>
        public void Show()
        {
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.Flex;
                
                // Refresh the UI when showing to make sure it's up to date
                RefreshMonthView();
                RefreshWeekView();
                UpdateEventsList();
            }
        }
        
        /// <summary>
        /// Hides the calendar view
        /// </summary>
        public void Hide()
        {
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.None;
                
                // Close event detail popup if open
                if (_eventDetailPopup != null)
                {
                    _eventDetailPopup.style.display = DisplayStyle.None;
                }
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
            _monthViewPanel = _rootElement.Q<VisualElement>(MONTH_VIEW_PANEL_NAME);
            _weekViewPanel = _rootElement.Q<VisualElement>(WEEK_VIEW_PANEL_NAME);
            _dayDetailPanel = _rootElement.Q<VisualElement>(DAY_DETAIL_PANEL_NAME);
            _eventDetailPopup = _rootElement.Q<VisualElement>(EVENT_DETAIL_POPUP_NAME);
            
            // Get navigation controls
            _monthViewButton = _rootElement.Q<Button>(MONTH_VIEW_BUTTON_NAME);
            _weekViewButton = _rootElement.Q<Button>(WEEK_VIEW_BUTTON_NAME);
            _previousButton = _rootElement.Q<Button>(PREVIOUS_BUTTON_NAME);
            _nextButton = _rootElement.Q<Button>(NEXT_BUTTON_NAME);
            _todayButton = _rootElement.Q<Button>(TODAY_BUTTON_NAME);
            _monthYearLabel = _rootElement.Q<Label>(MONTH_YEAR_LABEL_NAME);
            
            // Get event list
            _eventsList = _rootElement.Q<ListView>(EVENTS_LIST_NAME);
            
            // Set up the month view
            SetupMonthView();
            
            // Set up the week view
            SetupWeekView();
            
            // Set up event detail popup
            SetupEventDetailPopup();
            
            // Set up event list
            SetupEventsList();
            
            // Set up button handlers
            SetupButtonHandlers();
        }
        
        /// <summary>
        /// Sets up the month view calendar grid
        /// </summary>
        private void SetupMonthView()
        {
            var daysContainer = _monthViewPanel.Q<VisualElement>(DAYS_CONTAINER_NAME);
            daysContainer.Clear();
            _dayElements.Clear();
            
            // Add day name headers
            for (int i = 0; i < 7; i++)
            {
                var dayNameLabel = new Label(_weekDayShortNames[i]);
                dayNameLabel.AddToClassList("day-name");
                daysContainer.Add(dayNameLabel);
            }
            
            // Create day cells (maximum 42 for a 6-week month view)
            for (int i = 0; i < 42; i++)
            {
                var dayCell = _dayCellTemplate.Instantiate();
                dayCell.name = $"{DAY_CELL_NAME}-{i}";
                dayCell.AddToClassList("day-cell");
                
                // Store reference to the day cell
                _dayElements.Add(dayCell);
                
                // Set up click handler for the day cell
                int index = i; // Capture for lambda
                dayCell.RegisterCallback<ClickEvent>(evt => OnDayCellClicked(index));
                
                daysContainer.Add(dayCell);
            }
        }
        
        /// <summary>
        /// Sets up the week view with day columns and hour slots
        /// </summary>
        private void SetupWeekView()
        {
            var weekContainer = _weekViewPanel.Q<VisualElement>(WEEK_CONTAINER_NAME);
            weekContainer.Clear();
            _weekDaySlots.Clear();
            
            // Create time labels column
            var timeLabelsColumn = new VisualElement();
            timeLabelsColumn.AddToClassList("time-labels-column");
            
            // Add a header for spacing alignment
            var timeHeader = new Label("Time");
            timeHeader.AddToClassList("time-header");
            timeLabelsColumn.Add(timeHeader);
            
            // Add hour labels
            for (int hour = 0; hour < 24; hour++)
            {
                string hourText = hour.ToString("00") + ":00";
                var hourLabel = new Label(hourText);
                hourLabel.AddToClassList("hour-label");
                timeLabelsColumn.Add(hourLabel);
            }
            
            weekContainer.Add(timeLabelsColumn);
            
            // Create day columns
            for (int day = 0; day < 7; day++)
            {
                // Create day column
                var dayColumn = new VisualElement();
                dayColumn.name = $"{DAY_COLUMN_PREFIX}{day}";
                dayColumn.AddToClassList("day-column");
                
                // Add day header
                var dayHeader = new Label(_weekDayNames[day]);
                dayHeader.name = $"{WEEK_DAY_HEADER_PREFIX}{day}";
                dayHeader.AddToClassList("day-header");
                
                // Store day header in the day column for later reference
                dayColumn.Add(dayHeader);
                
                // Create hour slots container
                var hourSlotsContainer = new VisualElement();
                hourSlotsContainer.name = $"{HOUR_SLOTS_PREFIX}{day}";
                hourSlotsContainer.AddToClassList("hour-slots-container");
                
                // Store slots for this day
                _weekDaySlots[day] = new List<VisualElement>();
                
                // Add hour slots
                for (int hour = 0; hour < 24; hour++)
                {
                    var hourSlot = _weekViewHourTemplate.Instantiate()[0];
                    hourSlot.name = $"hour-slot-{day}-{hour}";
                    hourSlot.AddToClassList("hour-slot");
                    
                    // Store reference to hour slot
                    _weekDaySlots[day].Add(hourSlot);
                    
                    // Set up click handler for the hour slot
                    int dayIndex = day; // Capture for lambda
                    int hourIndex = hour;
                    hourSlot.RegisterCallback<ClickEvent>(evt => OnHourSlotClicked(dayIndex, hourIndex));
                    
                    hourSlotsContainer.Add(hourSlot);
                }
                
                dayColumn.Add(hourSlotsContainer);
                weekContainer.Add(dayColumn);
            }
        }
        
        /// <summary>
        /// Sets up the event detail popup
        /// </summary>
        private void SetupEventDetailPopup()
        {
            if (_eventDetailPopup == null) return;
            
            // Initially hide the popup
            _eventDetailPopup.style.display = DisplayStyle.None;
            
            // Set up close button
            var closeButton = _eventDetailPopup.Q<Button>(CLOSE_POPUP_BUTTON_NAME);
            if (closeButton != null)
            {
                closeButton.clicked += () => 
                {
                    _eventDetailPopup.style.display = DisplayStyle.None;
                    _currentlyDisplayedEvent = null;
                };
            }
            
            // Set up event complete button
            var completeButton = _eventDetailPopup.Q<Button>(EVENT_COMPLETE_BUTTON_NAME);
            if (completeButton != null)
            {
                completeButton.clicked += () => 
                {
                    if (_currentlyDisplayedEvent != null)
                    {
                        _viewModel.MarkEventAsCompleted(_currentlyDisplayedEvent.EventID);
                        _eventDetailPopup.style.display = DisplayStyle.None;
                        _currentlyDisplayedEvent = null;
                    }
                };
            }
        }
        
        /// <summary>
        /// Sets up the events list for the day detail panel
        /// </summary>
        private void SetupEventsList()
        {
            if (_eventsList == null) return;
            
            _eventsList.makeItem = () => _eventItemTemplate.Instantiate()[0];
            _eventsList.bindItem = (element, index) => 
            {
                if (index < 0 || index >= _viewModel.SelectedDateEvents.Count) return;
                
                var eventItem = _viewModel.SelectedDateEvents[index];
                
                var titleLabel = element.Q<Label>(EVENT_TITLE_NAME);
                if (titleLabel != null)
                {
                    titleLabel.text = eventItem.Title;
                }
                
                var timeLabel = element.Q<Label>(EVENT_TIME_NAME);
                if (timeLabel != null)
                {
                    if (eventItem.IsAllDay())
                    {
                        timeLabel.text = "All Day";
                    }
                    else
                    {
                        timeLabel.text = $"{eventItem.StartTime:HH:mm} - {eventItem.EndTime:HH:mm}";
                    }
                }
                
                // Add click handler for event item
                element.RegisterCallback<ClickEvent>(evt => ShowEventDetails(eventItem));
                
                // Apply styling based on event type and priority
                ApplyEventItemStyling(element, eventItem);
                
                // Highlight if completed
                if (eventItem.IsCompleted)
                {
                    element.AddToClassList("completed-event");
                }
                else
                {
                    element.RemoveFromClassList("completed-event");
                }
            };
        }
        
        /// <summary>
        /// Sets up the button handlers
        /// </summary>
        private void SetupButtonHandlers()
        {
            if (_monthViewButton != null)
            {
                _monthViewButton.clicked += () => _viewModel.ChangeViewMode(CalendarViewMode.Month);
            }
            
            if (_weekViewButton != null)
            {
                _weekViewButton.clicked += () => _viewModel.ChangeViewMode(CalendarViewMode.Week);
            }
            
            if (_previousButton != null)
            {
                _previousButton.clicked += () => 
                {
                    if (_viewModel.ViewMode.Value == CalendarViewMode.Month)
                    {
                        _viewModel.MoveToPreviousMonth();
                    }
                    else
                    {
                        // Move to previous week
                        var date = _viewModel.SelectedDate.Value.AddDays(-7);
                        _viewModel.SetSelectedDate(date);
                    }
                };
            }
            
            if (_nextButton != null)
            {
                _nextButton.clicked += () => 
                {
                    if (_viewModel.ViewMode.Value == CalendarViewMode.Month)
                    {
                        _viewModel.MoveToNextMonth();
                    }
                    else
                    {
                        // Move to next week
                        var date = _viewModel.SelectedDate.Value.AddDays(7);
                        _viewModel.SetSelectedDate(date);
                    }
                };
            }
            
            if (_todayButton != null)
            {
                _todayButton.clicked += () => _viewModel.MoveToToday();
            }
        }
        
        /// <summary>
        /// Binds the view model to the UI
        /// </summary>
        private void BindViewModel()
        {
            if (_viewModel == null) return;
            
            // Bind view mode changes
            _viewModel.ViewMode
                .Subscribe(mode => UpdateViewMode(mode))
                .AddTo(_disposables);
            
            // Bind selected date changes
            _viewModel.SelectedDate
                .Subscribe(_ => 
                {
                    RefreshMonthView();
                    RefreshWeekView();
                    UpdateMonthYearLabel();
                    UpdateEventsList();
                })
                .AddTo(_disposables);
            
            // Bind display month changes
            _viewModel.DisplayMonth
                .Subscribe(_ => 
                {
                    RefreshMonthView();
                    UpdateMonthYearLabel();
                })
                .AddTo(_disposables);
            
            // Bind events collection changes
            _viewModel.Events.ObserveCountChanged()
                .Subscribe(_ => 
                {
                    RefreshMonthView();
                    RefreshWeekView();
                })
                .AddTo(_disposables);
            
            // Bind selected date events collection changes
            _viewModel.SelectedDateEvents.ObserveCountChanged()
                .Subscribe(_ => UpdateEventsList())
                .AddTo(_disposables);
            
            // Bind special dates collection changes
            _viewModel.SpecialDates.ObserveCountChanged()
                .Subscribe(_ => 
                {
                    RefreshMonthView();
                    RefreshWeekView();
                })
                .AddTo(_disposables);
            
            // Initial UI update
            UpdateViewMode(_viewModel.ViewMode.Value);
            RefreshMonthView();
            RefreshWeekView();
            UpdateMonthYearLabel();
            UpdateEventsList();
        }
        
        /// <summary>
        /// Updates the UI based on the selected view mode
        /// </summary>
        /// <param name="mode">The view mode to display</param>
        private void UpdateViewMode(CalendarViewMode mode)
        {
            if (_monthViewPanel == null || _weekViewPanel == null) return;
            
            if (mode == CalendarViewMode.Month)
            {
                _monthViewPanel.style.display = DisplayStyle.Flex;
                _weekViewPanel.style.display = DisplayStyle.None;
                _monthViewButton.AddToClassList("active-view-button");
                _weekViewButton.RemoveFromClassList("active-view-button");
                
                RefreshMonthView();
            }
            else
            {
                _monthViewPanel.style.display = DisplayStyle.None;
                _weekViewPanel.style.display = DisplayStyle.Flex;
                _monthViewButton.RemoveFromClassList("active-view-button");
                _weekViewButton.AddToClassList("active-view-button");
                
                RefreshWeekView();
            }
            
            UpdateMonthYearLabel();
        }
        
        /// <summary>
        /// Updates the month/year label based on the current view mode and selected date
        /// </summary>
        private void UpdateMonthYearLabel()
        {
            if (_monthYearLabel == null || _viewModel == null) return;
            
            if (_viewModel.ViewMode.Value == CalendarViewMode.Month)
            {
                var month = _viewModel.DisplayMonth.Value.Month;
                var year = _viewModel.DisplayMonth.Value.Year;
                
                _monthYearLabel.text = $"{_monthNames[month - 1]} {year}";
            }
            else
            {
                var weekStart = GetWeekStartDate(_viewModel.SelectedDate.Value);
                var weekEnd = weekStart.AddDays(6);
                
                if (weekStart.Month == weekEnd.Month)
                {
                    _monthYearLabel.text = $"{_monthNames[weekStart.Month - 1]} {weekStart.Year}";
                }
                else if (weekStart.Year == weekEnd.Year)
                {
                    _monthYearLabel.text = $"{_monthNames[weekStart.Month - 1]} - {_monthNames[weekEnd.Month - 1]} {weekStart.Year}";
                }
                else
                {
                    _monthYearLabel.text = $"{_monthNames[weekStart.Month - 1]} {weekStart.Year} - {_monthNames[weekEnd.Month - 1]} {weekEnd.Year}";
                }
            }
        }
        
        /// <summary>
        /// Refreshes the month view based on the current display month
        /// </summary>
        private void RefreshMonthView()
        {
            if (_viewModel == null || _dayElements.Count == 0) return;
            
            int daysInMonth = _viewModel.DaysInMonth.Value;
            int firstDayOfMonth = _viewModel.FirstDayOfMonth.Value;
            DateTime displayMonth = _viewModel.DisplayMonth.Value;
            
            // Get last days of previous month to show in the first row
            DateTime previousMonth = displayMonth.AddMonths(-1);
            int daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
            
            // Update each day cell
            for (int i = 0; i < _dayElements.Count; i++)
            {
                var dayElement = _dayElements[i];
                var dayNumber = dayElement.Q<Label>(DAY_NUMBER_NAME);
                var eventMarkers = dayElement.Q<VisualElement>(EVENT_MARKERS_NAME);
                var specialDateIndicator = dayElement.Q<VisualElement>(SPECIAL_DATE_INDICATOR_NAME);
                
                // Clear event markers
                eventMarkers.Clear();
                
                // Determine which date this cell represents
                DateTime cellDate;
                bool isCurrentMonth = false;
                
                if (i < firstDayOfMonth)
                {
                    // Previous month days
                    int dayFromPreviousMonth = daysInPreviousMonth - (firstDayOfMonth - i - 1);
                    cellDate = new DateTime(previousMonth.Year, previousMonth.Month, dayFromPreviousMonth);
                    
                    dayElement.AddToClassList("other-month");
                    dayElement.RemoveFromClassList("current-month");
                }
                else if (i < firstDayOfMonth + daysInMonth)
                {
                    // Current month days
                    int day = i - firstDayOfMonth + 1;
                    cellDate = new DateTime(displayMonth.Year, displayMonth.Month, day);
                    isCurrentMonth = true;
                    
                    dayElement.AddToClassList("current-month");
                    dayElement.RemoveFromClassList("other-month");
                }
                else
                {
                    // Next month days
                    int dayFromNextMonth = i - (firstDayOfMonth + daysInMonth) + 1;
                    DateTime nextMonth = displayMonth.AddMonths(1);
                    cellDate = new DateTime(nextMonth.Year, nextMonth.Month, dayFromNextMonth);
                    
                    dayElement.AddToClassList("other-month");
                    dayElement.RemoveFromClassList("current-month");
                }
                
                // Set day number
                if (dayNumber != null)
                {
                    dayNumber.text = cellDate.Day.ToString();
                }
                
                // Apply today style if this is the current date
                if (_viewModel.IsCurrentDate(cellDate))
                {
                    dayElement.AddToClassList("today");
                }
                else
                {
                    dayElement.RemoveFromClassList("today");
                }
                
                // Apply selected style if this is the selected date
                if (_viewModel.IsSelectedDate(cellDate))
                {
                    dayElement.AddToClassList("selected");
                }
                else
                {
                    dayElement.RemoveFromClassList("selected");
                }
                
                // Add event markers if there are events on this date
                if (isCurrentMonth) // Only show markers for current month
                {
                    var events = _viewModel.GetEventsForDay(cellDate);
                    eventMarkers.style.display = events.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                    
                    // Show up to 3 event markers, with a count indicator if there are more
                    int markersToShow = Mathf.Min(events.Count, 3);
                    
                    for (int j = 0; j < markersToShow; j++)
                    {
                        var marker = new VisualElement();
                        marker.AddToClassList("event-marker");
                        
                        // Apply event-specific styling to the marker
                        ApplyEventMarkerStyling(marker, events[j]);
                        
                        eventMarkers.Add(marker);
                    }
                    
                    if (events.Count > 3)
                    {
                        var moreIndicator = new Label($"+{events.Count - 3}");
                        moreIndicator.AddToClassList("more-events-indicator");
                        eventMarkers.Add(moreIndicator);
                    }
                }
                else
                {
                    eventMarkers.style.display = DisplayStyle.None;
                }
                
                // Show special date indicator if applicable
                if (specialDateIndicator != null)
                {
                    var specialDates = _viewModel.GetSpecialDatesForDay(cellDate);
                    specialDateIndicator.style.display = specialDates.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                    
                    // Apply styling based on the first special date
                    if (specialDates.Count > 0)
                    {
                        var theme = specialDates[0].Theme;
                        if (theme != null)
                        {
                            specialDateIndicator.style.backgroundColor = theme.PrimaryColor;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Refreshes the week view based on the current selected date
        /// </summary>
        private void RefreshWeekView()
        {
            if (_viewModel == null || _weekDaySlots.Count == 0) return;
            
            // Get the first day of the week (Sunday) for the selected date
            DateTime weekStart = GetWeekStartDate(_viewModel.SelectedDate.Value);
            
            // Update day headers with dates
            for (int day = 0; day < 7; day++)
            {
                DateTime currentDate = weekStart.AddDays(day);
                
                // Find the day header
                var dayHeader = _rootElement.Q<Label>($"{WEEK_DAY_HEADER_PREFIX}{day}");
                if (dayHeader != null)
                {
                    // Update header text with day name and date
                    dayHeader.text = $"{_weekDayNames[day]} {currentDate.Day}";
                    
                    // Apply today style if applicable
                    if (_viewModel.IsCurrentDate(currentDate))
                    {
                        dayHeader.AddToClassList("today-header");
                    }
                    else
                    {
                        dayHeader.RemoveFromClassList("today-header");
                    }
                    
                    // Apply selected style if applicable
                    if (_viewModel.IsSelectedDate(currentDate))
                    {
                        dayHeader.AddToClassList("selected-header");
                    }
                    else
                    {
                        dayHeader.RemoveFromClassList("selected-header");
                    }
                }
                
                // Clear existing event blocks from all hour slots
                List<VisualElement> daySlots = _weekDaySlots[day];
                foreach (var slot in daySlots)
                {
                    // Find and remove all event blocks in this slot
                    slot.Query(className: "event-block").ForEach(element => element.RemoveFromHierarchy());
                }
                
                // Add events for this day
                var events = _viewModel.GetEventsForDay(currentDate);
                foreach (var evt in events)
                {
                    // Skip all-day events for now (could add these to a special row at the top)
                    if (evt.IsAllDay()) continue;
                    
                    // Get start and end hours
                    int startHour = evt.StartTime.Hour;
                    int endHour = evt.EndTime.Hour;
                    if (evt.EndTime.Minute > 0) endHour++; // If end time has minutes, include the next hour
                    
                    // Make sure the event is at least 1 hour long for visibility
                    endHour = Mathf.Max(endHour, startHour + 1);
                    
                    // Clamp to day boundaries (0-23)
                    startHour = Mathf.Clamp(startHour, 0, 23);
                    endHour = Mathf.Clamp(endHour, startHour + 1, 24);
                    
                    // Create the event block
                    var eventBlock = new VisualElement();
                    eventBlock.AddToClassList("event-block");
                    
                    // Apply event-specific styling
                    ApplyEventBlockStyling(eventBlock, evt);
                    
                    // Add the event title
                    var titleLabel = new Label(evt.Title);
                    titleLabel.AddToClassList("event-block-title");
                    eventBlock.Add(titleLabel);
                    
                    // Add the event time
                    var timeLabel = new Label($"{evt.StartTime:HH:mm} - {evt.EndTime:HH:mm}");
                    timeLabel.AddToClassList("event-block-time");
                    eventBlock.Add(timeLabel);
                    
                    // Add the event to the first hour slot and set its height to span multiple hours
                    if (startHour < daySlots.Count)
                    {
                        var startSlot = daySlots[startHour];
                        startSlot.Add(eventBlock);
                        
                        // Calculate height based on duration
                        float heightPercent = (endHour - startHour) * 100f;
                        eventBlock.style.height = new StyleLength(new Length(heightPercent, LengthUnit.Percent));
                        
                        // Position based on minutes within the first hour
                        float topPercent = (evt.StartTime.Minute / 60f) * 100f;
                        eventBlock.style.top = new StyleLength(new Length(topPercent, LengthUnit.Percent));
                        
                        // Add click handler for event block
                        eventBlock.RegisterCallback<ClickEvent>(e => 
                        {
                            e.StopPropagation();
                            ShowEventDetails(evt);
                        });
                    }
                }
            }
        }
        
        /// <summary>
        /// Updates the events list for the selected date
        /// </summary>
        private void UpdateEventsList()
        {
            if (_eventsList == null || _viewModel == null) return;
            
            _eventsList.itemsSource = _viewModel.SelectedDateEvents.ToList();
            _eventsList.Rebuild();
            
            // Update the day detail panel header to show the selected date
            var dayDetailHeader = _dayDetailPanel?.Q<Label>("day-detail-header");
            if (dayDetailHeader != null)
            {
                dayDetailHeader.text = $"{_viewModel.SelectedDate.Value:d MMMM yyyy}";
            }
        }
        
        /// <summary>
        /// Applies styling to an event item based on its type and priority
        /// </summary>
        /// <param name="element">The UI element representing the event</param>
        /// <param name="calendarEvent">The calendar event data</param>
        private void ApplyEventItemStyling(VisualElement element, CalendarEvent calendarEvent)
        {
            // Remove existing type and priority classes
            foreach (EventType type in Enum.GetValues(typeof(EventType)))
            {
                element.RemoveFromClassList($"event-type-{type.ToString().ToLower()}");
            }
            
            foreach (EventPriority priority in Enum.GetValues(typeof(EventPriority)))
            {
                element.RemoveFromClassList($"event-priority-{priority.ToString().ToLower()}");
            }
            
            // Add appropriate classes
            element.AddToClassList($"event-type-{calendarEvent.Type.ToString().ToLower()}");
            element.AddToClassList($"event-priority-{calendarEvent.Priority.ToString().ToLower()}");
            
            // Add specific visual indicators based on priority
            var priorityIndicator = element.Q<VisualElement>("priority-indicator");
            if (priorityIndicator != null)
            {
                switch (calendarEvent.Priority)
                {
                    case EventPriority.Low:
                        priorityIndicator.style.backgroundColor = new Color(0.3f, 0.8f, 0.3f); // Green
                        break;
                    case EventPriority.Medium:
                        priorityIndicator.style.backgroundColor = new Color(0.9f, 0.9f, 0.2f); // Yellow
                        break;
                    case EventPriority.High:
                        priorityIndicator.style.backgroundColor = new Color(0.9f, 0.5f, 0.1f); // Orange
                        break;
                    case EventPriority.Urgent:
                        priorityIndicator.style.backgroundColor = new Color(0.9f, 0.2f, 0.2f); // Red
                        break;
                }
            }
        }
        
        /// <summary>
        /// Applies styling to an event marker based on the event type and priority
        /// </summary>
        /// <param name="marker">The marker element</param>
        /// <param name="calendarEvent">The calendar event</param>
        private void ApplyEventMarkerStyling(VisualElement marker, CalendarEvent calendarEvent)
        {
            // Set color based on event type
            switch (calendarEvent.Type)
            {
                case EventType.Personal:
                    marker.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f); // Blue
                    break;
                case EventType.Social:
                    marker.style.backgroundColor = new Color(0.8f, 0.4f, 0.8f); // Purple
                    break;
                case EventType.Work:
                    marker.style.backgroundColor = new Color(0.6f, 0.8f, 0.2f); // Green
                    break;
                case EventType.Story:
                    marker.style.backgroundColor = new Color(0.8f, 0.6f, 0.2f); // Gold
                    break;
                case EventType.Deadline:
                    marker.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f); // Red
                    break;
                default:
                    marker.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f); // Gray
                    break;
            }
            
            // Add border for high priority events
            if (calendarEvent.Priority == EventPriority.High || calendarEvent.Priority == EventPriority.Urgent)
            {
                marker.style.borderTopWidth = new StyleFloat(1f);
                marker.style.borderTopColor = marker.style.borderRightColor = marker.style.borderBottomColor = marker.style.borderLeftColor = new StyleColor(new Color(0.9f, 0.2f, 0.2f)); // Red border
            }
            
            // Add strikethrough or opacity for completed events
            if (calendarEvent.IsCompleted)
            {
                marker.style.opacity = new StyleFloat(0.5f);
            }
        }
        
        /// <summary>
        /// Applies styling to a week view event block
        /// </summary>
        /// <param name="eventBlock">The event block element</param>
        /// <param name="calendarEvent">The calendar event</param>
        private void ApplyEventBlockStyling(VisualElement eventBlock, CalendarEvent calendarEvent)
        {
            // Set color based on event type
            switch (calendarEvent.Type)
            {
                case EventType.Personal:
                    eventBlock.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 0.8f); // Blue
                    break;
                case EventType.Social:
                    eventBlock.style.backgroundColor = new Color(0.8f, 0.4f, 0.8f, 0.8f); // Purple
                    break;
                case EventType.Work:
                    eventBlock.style.backgroundColor = new Color(0.6f, 0.8f, 0.2f, 0.8f); // Green
                    break;
                case EventType.Story:
                    eventBlock.style.backgroundColor = new Color(0.8f, 0.6f, 0.2f, 0.8f); // Gold
                    break;
                case EventType.Deadline:
                    eventBlock.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f); // Red
                    break;
                default:
                    eventBlock.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Gray
                    break;
            }
            
            // Set border for priority
            switch (calendarEvent.Priority)
            {
                case EventPriority.Low:
                    eventBlock.style.borderTopWidth = new StyleFloat(1f);
                    eventBlock.style.borderTopColor = eventBlock.style.borderRightColor = eventBlock.style.borderBottomColor = eventBlock.style.borderLeftColor = new StyleColor(new Color(0.3f, 0.8f, 0.3f)); // Green
                    break;
                case EventPriority.Medium:
                    eventBlock.style.borderTopWidth = new StyleFloat(1f);
                    eventBlock.style.borderTopColor = eventBlock.style.borderRightColor = eventBlock.style.borderBottomColor = eventBlock.style.borderLeftColor = new StyleColor(new Color(0.9f, 0.9f, 0.2f)); // Yellow
                    break;
                case EventPriority.High:
                    eventBlock.style.borderTopWidth = new StyleFloat(2f);
                    eventBlock.style.borderTopColor = eventBlock.style.borderRightColor = eventBlock.style.borderBottomColor = eventBlock.style.borderLeftColor = new StyleColor(new Color(0.9f, 0.5f, 0.1f)); // Orange
                    break;
                case EventPriority.Urgent:
                    eventBlock.style.borderTopWidth = new StyleFloat(2f);
                    eventBlock.style.borderTopColor = eventBlock.style.borderRightColor = eventBlock.style.borderBottomColor = eventBlock.style.borderLeftColor = new StyleColor(new Color(0.9f, 0.2f, 0.2f)); // Red
                    break;
            }
            
            // Add styling for completed events
            if (calendarEvent.IsCompleted)
            {
                eventBlock.style.opacity = new StyleFloat(0.5f);
                
                var strikethrough = new VisualElement();
                strikethrough.AddToClassList("strikethrough");
                eventBlock.Add(strikethrough);
            }
        }
        
        /// <summary>
        /// Shows the details for a specific event
        /// </summary>
        /// <param name="calendarEvent">The calendar event to show details for</param>
        private void ShowEventDetails(CalendarEvent calendarEvent)
        {
            if (_eventDetailPopup == null || calendarEvent == null) return;
            
            _currentlyDisplayedEvent = calendarEvent;
            
            // Set event title
            var titleLabel = _eventDetailPopup.Q<Label>(EVENT_TITLE_NAME);
            if (titleLabel != null)
            {
                titleLabel.text = calendarEvent.Title;
            }
            
            // Set event time
            var timeLabel = _eventDetailPopup.Q<Label>(EVENT_TIME_NAME);
            if (timeLabel != null)
            {
                if (calendarEvent.IsAllDay())
                {
                    timeLabel.text = $"All Day: {calendarEvent.StartTime:d MMMM}";
                    if (calendarEvent.GetDuration().Days > 1)
                    {
                        timeLabel.text += $" - {calendarEvent.EndTime:d MMMM}";
                    }
                }
                else
                {
                    timeLabel.text = $"{calendarEvent.StartTime:d MMMM, HH:mm} - {calendarEvent.EndTime:d MMMM, HH:mm}";
                }
            }
            
            // Set event description
            var descriptionLabel = _eventDetailPopup.Q<Label>(EVENT_DESCRIPTION_NAME);
            if (descriptionLabel != null)
            {
                descriptionLabel.text = calendarEvent.Description;
            }
            
            // Handle event complete button visibility
            var completeButton = _eventDetailPopup.Q<Button>(EVENT_COMPLETE_BUTTON_NAME);
            if (completeButton != null)
            {
                completeButton.style.display = calendarEvent.IsCompleted ? DisplayStyle.None : DisplayStyle.Flex;
            }
            
            // Show the popup
            _eventDetailPopup.style.display = DisplayStyle.Flex;
            
            // Apply popup styling based on event type
            var typeIndicator = _eventDetailPopup.Q<VisualElement>("event-type-indicator");
            if (typeIndicator != null)
            {
                // Clear existing classes
                foreach (EventType type in Enum.GetValues(typeof(EventType)))
                {
                    typeIndicator.RemoveFromClassList($"type-{type.ToString().ToLower()}");
                }
                
                // Add appropriate class
                typeIndicator.AddToClassList($"type-{calendarEvent.Type.ToString().ToLower()}");
                
                // Set color based on event type
                switch (calendarEvent.Type)
                {
                    case EventType.Personal:
                        typeIndicator.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f); // Blue
                        break;
                    case EventType.Social:
                        typeIndicator.style.backgroundColor = new Color(0.8f, 0.4f, 0.8f); // Purple
                        break;
                    case EventType.Work:
                        typeIndicator.style.backgroundColor = new Color(0.6f, 0.8f, 0.2f); // Green
                        break;
                    case EventType.Story:
                        typeIndicator.style.backgroundColor = new Color(0.8f, 0.6f, 0.2f); // Gold
                        break;
                    case EventType.Deadline:
                        typeIndicator.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f); // Red
                        break;
                    default:
                        typeIndicator.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f); // Gray
                        break;
                }
            }
            
            // Show priority indicator
            var priorityIndicator = _eventDetailPopup.Q<VisualElement>("priority-indicator");
            if (priorityIndicator != null)
            {
                var priorityLabel = priorityIndicator.Q<Label>("priority-label");
                if (priorityLabel != null)
                {
                    priorityLabel.text = calendarEvent.Priority.ToString();
                }
                
                switch (calendarEvent.Priority)
                {
                    case EventPriority.Low:
                        priorityIndicator.style.backgroundColor = new Color(0.3f, 0.8f, 0.3f, 0.2f); // Green
                        break;
                    case EventPriority.Medium:
                        priorityIndicator.style.backgroundColor = new Color(0.9f, 0.9f, 0.2f, 0.2f); // Yellow
                        break;
                    case EventPriority.High:
                        priorityIndicator.style.backgroundColor = new Color(0.9f, 0.5f, 0.1f, 0.2f); // Orange
                        break;
                    case EventPriority.Urgent:
                        priorityIndicator.style.backgroundColor = new Color(0.9f, 0.2f, 0.2f, 0.2f); // Red
                        break;
                }
            }
            
            // Show related characters if any
            var relatedCharactersContainer = _eventDetailPopup.Q<VisualElement>("related-characters-container");
            if (relatedCharactersContainer != null)
            {
                relatedCharactersContainer.Clear();
                relatedCharactersContainer.style.display = 
                    calendarEvent.RelatedCharacterIDs.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                
                var headerLabel = new Label("Related Characters:");
                headerLabel.AddToClassList("related-characters-header");
                relatedCharactersContainer.Add(headerLabel);
                
                foreach (var characterId in calendarEvent.RelatedCharacterIDs)
                {
                    var characterLabel = new Label(characterId);
                    characterLabel.AddToClassList("related-character-item");
                    relatedCharactersContainer.Add(characterLabel);
                }
            }
        }
        
        /// <summary>
        /// Handles a day cell being clicked in the month view
        /// </summary>
        /// <param name="index">The index of the day cell</param>
        private void OnDayCellClicked(int index)
        {
            if (_viewModel == null || _dayElements.Count <= index) return;
            
            int daysInMonth = _viewModel.DaysInMonth.Value;
            int firstDayOfMonth = _viewModel.FirstDayOfMonth.Value;
            DateTime displayMonth = _viewModel.DisplayMonth.Value;
            
            // Determine which date was clicked
            DateTime selectedDate;
            
            if (index < firstDayOfMonth)
            {
                // Previous month day
                DateTime previousMonth = displayMonth.AddMonths(-1);
                int daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
                int dayFromPreviousMonth = daysInPreviousMonth - (firstDayOfMonth - index - 1);
                selectedDate = new DateTime(previousMonth.Year, previousMonth.Month, dayFromPreviousMonth);
            }
            else if (index < firstDayOfMonth + daysInMonth)
            {
                // Current month day
                int day = index - firstDayOfMonth + 1;
                selectedDate = new DateTime(displayMonth.Year, displayMonth.Month, day);
            }
            else
            {
                // Next month day
                DateTime nextMonth = displayMonth.AddMonths(1);
                int dayFromNextMonth = index - (firstDayOfMonth + daysInMonth) + 1;
                selectedDate = new DateTime(nextMonth.Year, nextMonth.Month, dayFromNextMonth);
            }
            
            // Update the selected date in the view model
            _viewModel.SetSelectedDate(selectedDate);
        }
        
        /// <summary>
        /// Handles an hour slot being clicked in the week view
        /// </summary>
        /// <param name="dayIndex">The day index (0-6)</param>
        /// <param name="hourIndex">The hour index (0-23)</param>
        private void OnHourSlotClicked(int dayIndex, int hourIndex)
        {
            if (_viewModel == null) return;
            
            // Get the first day of the week (Sunday) for the selected date
            DateTime weekStart = GetWeekStartDate(_viewModel.SelectedDate.Value);
            
            // Determine which date and time was clicked
            DateTime selectedDateTime = weekStart.AddDays(dayIndex).AddHours(hourIndex);
            
            // Update the selected date in the view model
            _viewModel.SetSelectedDate(selectedDateTime.Date);
            
            // TODO: Could open event creation dialog focused on this time slot
        }
        
        /// <summary>
        /// Gets the start date of the week containing the specified date
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>The first day (Sunday) of the week</returns>
        private DateTime GetWeekStartDate(DateTime date)
        {
            // Calculate days to subtract to get to Sunday
            int diff = (int)date.DayOfWeek;
            return date.Date.AddDays(-diff);
        }
    }
}