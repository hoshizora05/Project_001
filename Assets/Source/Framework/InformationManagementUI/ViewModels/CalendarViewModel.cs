using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using System.Collections;

namespace InformationManagementUI
{
    /// <summary>
    /// ViewModel for the calendar view component
    /// </summary>
    public class CalendarViewModel
    {
        private readonly ICalendarDataProvider _dataProvider;
        
        // Observable properties
        private readonly ReactiveProperty<DateTime> _currentDate = new ReactiveProperty<DateTime>();
        private readonly ReactiveProperty<DateTime> _selectedDate = new ReactiveProperty<DateTime>();
        private readonly ReactiveProperty<CalendarViewMode> _viewMode = new ReactiveProperty<CalendarViewMode>(CalendarViewMode.Month);
        private readonly ReactiveCollection<CalendarEvent> _events = new ReactiveCollection<CalendarEvent>();
        private readonly ReactiveCollection<SpecialDate> _specialDates = new ReactiveCollection<SpecialDate>();
        
        // Filtered event collections
        private readonly ReactiveCollection<CalendarEvent> _selectedDateEvents = new ReactiveCollection<CalendarEvent>();
        private readonly ReactiveProperty<CalendarEvent> _selectedEvent = new ReactiveProperty<CalendarEvent>();
        
        // Observable commands
        private readonly ReactiveCommand _nextMonthCommand = new ReactiveCommand();
        private readonly ReactiveCommand _previousMonthCommand = new ReactiveCommand();
        private readonly ReactiveCommand _todayCommand = new ReactiveCommand();
        private readonly ReactiveCommand<CalendarViewMode> _changeViewModeCommand = new ReactiveCommand<CalendarViewMode>();
        private readonly ReactiveCommand<CalendarEvent> _selectEventCommand = new ReactiveCommand<CalendarEvent>();
        private readonly ReactiveCommand<DateTime> _selectDateCommand = new ReactiveCommand<DateTime>();
        
        // Public properties
        public IReactiveProperty<DateTime> CurrentDate => _currentDate;
        public IReactiveProperty<DateTime> SelectedDate => _selectedDate;
        public IReactiveProperty<CalendarViewMode> ViewMode => _viewMode;
        public IReadOnlyReactiveCollection<CalendarEvent> Events => _events;
        public IReadOnlyReactiveCollection<SpecialDate> SpecialDates => _specialDates;
        public IReadOnlyReactiveCollection<CalendarEvent> SelectedDateEvents => _selectedDateEvents;
        public IReactiveProperty<CalendarEvent> SelectedEvent => _selectedEvent;
        
        // Commands
        public IReactiveCommand<Unit> NextMonthCommand => _nextMonthCommand;
        public IReactiveCommand<Unit> PreviousMonthCommand => _previousMonthCommand;
        public IReactiveCommand<Unit> TodayCommand => _todayCommand;
        public IReactiveCommand<CalendarViewMode> ChangeViewModeCommand => _changeViewModeCommand;
        public IReactiveCommand<CalendarEvent> SelectEventCommand => _selectEventCommand;
        public IReactiveCommand<DateTime> SelectDateCommand => _selectDateCommand;
        
        // Computed properties
        private readonly ReactiveProperty<DateTime> _displayMonth = new ReactiveProperty<DateTime>();
        private readonly ReactiveProperty<int> _daysInMonth = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _firstDayOfMonth = new ReactiveProperty<int>();
        
        public IReactiveProperty<DateTime> DisplayMonth => _displayMonth;
        public IReactiveProperty<int> DaysInMonth => _daysInMonth;
        public IReactiveProperty<int> FirstDayOfMonth => _firstDayOfMonth;
        
        public CalendarViewModel(ICalendarDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            
            // Initialize with today's date
            _currentDate.Value = DateTime.Today;
            _selectedDate.Value = DateTime.Today;
            
            // Setup command handlers
            SetupCommands();
            
            // Setup data listeners
            SetupDataListeners();
            
            // Initial data load
            RefreshData();
            
            // Setup computed properties
            SetupComputedProperties();
        }
        
        private void SetupCommands()
        {
            _nextMonthCommand.Subscribe(_ => MoveToNextMonth()).AddTo(CompositeDisposable);
            _previousMonthCommand.Subscribe(_ => MoveToPreviousMonth()).AddTo(CompositeDisposable);
            _todayCommand.Subscribe(_ => MoveToToday()).AddTo(CompositeDisposable);
            _changeViewModeCommand.Subscribe(mode => ChangeViewMode(mode)).AddTo(CompositeDisposable);
            _selectEventCommand.Subscribe(evt => SelectEvent(evt)).AddTo(CompositeDisposable);
            _selectDateCommand.Subscribe(date => SetSelectedDate(date)).AddTo(CompositeDisposable);
        }
        
        private void SetupDataListeners()
        {
            if (_dataProvider != null)
            {
                // Subscribe to data provider events
                _dataProvider.OnCurrentDateChanged += date => _currentDate.Value = date;
                _dataProvider.OnEventAdded += evt => RefreshEvents();
                _dataProvider.OnEventUpdated += evt => RefreshEvents();
                _dataProvider.OnEventRemoved += id => RefreshEvents();
                _dataProvider.OnCalendarDataReset += RefreshData;
            }
            
            // When selected date changes, update selected date events
            _selectedDate.Subscribe(_ => RefreshSelectedDateEvents()).AddTo(CompositeDisposable);
        }
        
        private void SetupComputedProperties()
        {
            // Update display month when selected date changes
            _selectedDate.Subscribe(date => 
            {
                _displayMonth.Value = new DateTime(date.Year, date.Month, 1);
            }).AddTo(CompositeDisposable);
            
            // Update days in month and first day of month when display month changes
            _displayMonth.Subscribe(date => 
            {
                _daysInMonth.Value = DateTime.DaysInMonth(date.Year, date.Month);
                _firstDayOfMonth.Value = (int)new DateTime(date.Year, date.Month, 1).DayOfWeek;
            }).AddTo(CompositeDisposable);
        }
        
        // CompositeDisposable for cleanup
        private CompositeDisposable CompositeDisposable { get; } = new CompositeDisposable();
        
        /// <summary>
        /// Refreshes all calendar data from the data provider
        /// </summary>
        public void RefreshData()
        {
            if (_dataProvider == null) return;
            
            _currentDate.Value = _dataProvider.CurrentDate;
            RefreshEvents();
            RefreshSpecialDates();
            
            // If selected date hasn't been set, initialize it to current date
            if (_selectedDate.Value == default)
            {
                _selectedDate.Value = _currentDate.Value;
            }
            
            RefreshSelectedDateEvents();
        }
        
        /// <summary>
        /// Refreshes the events collection from the data provider
        /// </summary>
        private void RefreshEvents()
        {
            if (_dataProvider == null) return;
            
            _events.Clear();
            foreach (var evt in _dataProvider.Events)
            {
                _events.Add(evt);
            }
            
            RefreshSelectedDateEvents();
        }
        
        /// <summary>
        /// Refreshes the special dates collection from the data provider
        /// </summary>
        private void RefreshSpecialDates()
        {
            if (_dataProvider == null) return;
            
            _specialDates.Clear();
            foreach (var specialDate in _dataProvider.SpecialDates)
            {
                _specialDates.Add(specialDate);
            }
        }
        
        /// <summary>
        /// Refreshes the selected date events collection based on the current selected date
        /// </summary>
        private void RefreshSelectedDateEvents()
        {
            if (_dataProvider == null) return;
            
            _selectedDateEvents.Clear();
            
            var events = _dataProvider.GetEventsForDay(_selectedDate.Value);
            foreach (var evt in events)
            {
                _selectedDateEvents.Add(evt);
            }
        }
        
        /// <summary>
        /// Moves the display to the next month
        /// </summary>
        public void MoveToNextMonth()
        {
            var newDate = _displayMonth.Value.AddMonths(1);
            _displayMonth.Value = newDate;
            _selectedDate.Value = new DateTime(newDate.Year, newDate.Month, 1);
        }
        
        /// <summary>
        /// Moves the display to the previous month
        /// </summary>
        public void MoveToPreviousMonth()
        {
            var newDate = _displayMonth.Value.AddMonths(-1);
            _displayMonth.Value = newDate;
            _selectedDate.Value = new DateTime(newDate.Year, newDate.Month, 1);
        }
        
        /// <summary>
        /// Moves the display to today's date
        /// </summary>
        public void MoveToToday()
        {
            _selectedDate.Value = DateTime.Today;
        }
        
        /// <summary>
        /// Changes the current view mode (month/week)
        /// </summary>
        /// <param name="mode">The new view mode</param>
        public void ChangeViewMode(CalendarViewMode mode)
        {
            _viewMode.Value = mode;
        }
        
        /// <summary>
        /// Sets the selected event
        /// </summary>
        /// <param name="evt">The event to select</param>
        public void SelectEvent(CalendarEvent evt)
        {
            _selectedEvent.Value = evt;
        }
        
        /// <summary>
        /// Sets the selected date
        /// </summary>
        /// <param name="date">The date to select</param>
        public void SetSelectedDate(DateTime date)
        {
            _selectedDate.Value = date;
        }
        
        /// <summary>
        /// Gets events for a specific date
        /// </summary>
        /// <param name="date">The date to get events for</param>
        /// <returns>List of events on that date</returns>
        public List<CalendarEvent> GetEventsForDay(DateTime date)
        {
            return _dataProvider?.GetEventsForDay(date) ?? new List<CalendarEvent>();
        }
        
        /// <summary>
        /// Gets special dates for a specific date
        /// </summary>
        /// <param name="date">The date to get special dates for</param>
        /// <returns>List of special dates on that date</returns>
        public List<SpecialDate> GetSpecialDatesForDay(DateTime date)
        {
            return _dataProvider?.GetSpecialDatesForDay(date) ?? new List<SpecialDate>();
        }
        
        /// <summary>
        /// Determines if a date is the current date
        /// </summary>
        /// <param name="date">The date to check</param>
        /// <returns>True if the date is today</returns>
        public bool IsCurrentDate(DateTime date)
        {
            return date.Date == _currentDate.Value.Date;
        }
        
        /// <summary>
        /// Determines if a date is the selected date
        /// </summary>
        /// <param name="date">The date to check</param>
        /// <returns>True if the date is selected</returns>
        public bool IsSelectedDate(DateTime date)
        {
            return date.Date == _selectedDate.Value.Date;
        }
        
        /// <summary>
        /// Adds a new event to the calendar
        /// </summary>
        /// <param name="newEvent">The new event to add</param>
        public void AddEvent(CalendarEvent newEvent)
        {
            _dataProvider?.AddEvent(newEvent);
        }
        
        /// <summary>
        /// Updates an existing event
        /// </summary>
        /// <param name="updatedEvent">The updated event</param>
        public void UpdateEvent(CalendarEvent updatedEvent)
        {
            _dataProvider?.UpdateEvent(updatedEvent);
        }
        
        /// <summary>
        /// Removes an event from the calendar
        /// </summary>
        /// <param name="eventId">The ID of the event to remove</param>
        public void RemoveEvent(string eventId)
        {
            _dataProvider?.RemoveEvent(eventId);
        }
        
        /// <summary>
        /// Marks an event as completed
        /// </summary>
        /// <param name="eventId">The ID of the event to mark as completed</param>
        public void MarkEventAsCompleted(string eventId)
        {
            if (_dataProvider == null) return;
            
            var events = _dataProvider.Events;
            foreach (var evt in events)
            {
                if (evt.EventID == eventId)
                {
                    // Create a completed version of the event
                    var completedEvent = new CalendarEvent(
                        evt.EventID,
                        evt.Title,
                        evt.Description,
                        evt.StartTime,
                        evt.EndTime,
                        evt.Type,
                        evt.Priority,
                        true,
                        new List<string>(evt.RelatedCharacterIDs)
                    );
                    
                    _dataProvider.UpdateEvent(completedEvent);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Adds a special date to the calendar
        /// </summary>
        /// <param name="specialDate">The special date to add</param>
        public void AddSpecialDate(SpecialDate specialDate)
        {
            _dataProvider?.AddSpecialDate(specialDate);
        }
        
        /// <summary>
        /// Cleans up resources when the view model is no longer needed
        /// </summary>
        public void Dispose()
        {
            CompositeDisposable.Dispose();
        }
    }
    
    /// <summary>
    /// Enum representing the calendar view mode
    /// </summary>
    public enum CalendarViewMode
    {
        Month,
        Week
    }
}