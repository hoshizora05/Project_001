using System;
using System.Collections.Generic;
using UnityEngine;

namespace InformationManagementUI
{
    /// <summary>
    /// Data structure for calendar data
    /// </summary>
    [CreateAssetMenu(fileName = "CalendarData", menuName = "Project_001/Information UI/Calendar Data")]
    public class CalendarData : ScriptableObject, ICalendarDataProvider
    {
        [SerializeField] private DateTime _currentDate = DateTime.Now;
        [SerializeField] private List<CalendarEvent> _events = new List<CalendarEvent>();
        [SerializeField] private List<SpecialDate> _specialDates = new List<SpecialDate>();
        
        public DateTime CurrentDate => _currentDate;
        public IReadOnlyList<CalendarEvent> Events => _events;
        public IReadOnlyList<SpecialDate> SpecialDates => _specialDates;
        
        public event Action<DateTime> OnCurrentDateChanged;
        public event Action<CalendarEvent> OnEventAdded;
        public event Action<CalendarEvent> OnEventUpdated;
        public event Action<string> OnEventRemoved;
        public event Action OnCalendarDataReset;
        
        public void SetCurrentDate(DateTime newDate)
        {
            _currentDate = newDate;
            OnCurrentDateChanged?.Invoke(_currentDate);
        }
        
        public void AddEvent(CalendarEvent newEvent)
        {
            // Check if event already exists
            int existingIndex = _events.FindIndex(e => e.EventID == newEvent.EventID);
            
            if (existingIndex >= 0)
            {
                // Update existing event
                _events[existingIndex] = newEvent;
                OnEventUpdated?.Invoke(newEvent);
            }
            else
            {
                // Add new event
                _events.Add(newEvent);
                OnEventAdded?.Invoke(newEvent);
            }
        }
        
        public void UpdateEvent(CalendarEvent updatedEvent)
        {
            int index = _events.FindIndex(e => e.EventID == updatedEvent.EventID);
            if (index >= 0)
            {
                _events[index] = updatedEvent;
                OnEventUpdated?.Invoke(updatedEvent);
            }
        }
        
        public void RemoveEvent(string eventId)
        {
            int index = _events.FindIndex(e => e.EventID == eventId);
            if (index >= 0)
            {
                _events.RemoveAt(index);
                OnEventRemoved?.Invoke(eventId);
            }
        }
        
        public void AddSpecialDate(SpecialDate specialDate)
        {
            _specialDates.Add(specialDate);
        }
        
        public void RemoveSpecialDate(DateTime date, SpecialDateType type)
        {
            _specialDates.RemoveAll(sd => sd.Date.Date == date.Date && sd.Type == type);
        }
        
        public List<CalendarEvent> GetEventsForDay(DateTime date)
        {
            return _events.FindAll(e => 
                (e.StartTime.Date <= date.Date && e.EndTime.Date >= date.Date) || 
                e.StartTime.Date == date.Date
            );
        }
        
        public List<SpecialDate> GetSpecialDatesForDay(DateTime date)
        {
            return _specialDates.FindAll(sd => sd.Date.Date == date.Date);
        }
        
        public void ResetCalendarData()
        {
            _events.Clear();
            _specialDates.Clear();
            OnCalendarDataReset?.Invoke();
        }
    }
    
    /// <summary>
    /// Interface for calendar data providers
    /// </summary>
    public interface ICalendarDataProvider
    {
        DateTime CurrentDate { get; }
        IReadOnlyList<CalendarEvent> Events { get; }
        IReadOnlyList<SpecialDate> SpecialDates { get; }
        
        event Action<DateTime> OnCurrentDateChanged;
        event Action<CalendarEvent> OnEventAdded;
        event Action<CalendarEvent> OnEventUpdated;
        event Action<string> OnEventRemoved;
        event Action OnCalendarDataReset;
        
        void SetCurrentDate(DateTime newDate);
        void AddEvent(CalendarEvent newEvent);
        void UpdateEvent(CalendarEvent updatedEvent);
        void RemoveEvent(string eventId);
        void AddSpecialDate(SpecialDate specialDate);
        void RemoveSpecialDate(DateTime date, SpecialDateType type);
        List<CalendarEvent> GetEventsForDay(DateTime date);
        List<SpecialDate> GetSpecialDatesForDay(DateTime date);
        void ResetCalendarData();
    }
    
    /// <summary>
    /// Represents an event on the calendar
    /// </summary>
    [Serializable]
    public class CalendarEvent
    {
        [SerializeField] private string _eventID;
        [SerializeField] private string _title;
        [SerializeField] private string _description;
        [SerializeField] private DateTime _startTime;
        [SerializeField] private DateTime _endTime;
        [SerializeField] private EventType _type;
        [SerializeField] private EventPriority _priority;
        [SerializeField] private bool _isCompleted;
        [SerializeField] private List<string> _relatedCharacterIDs = new List<string>();
        
        public string EventID => _eventID;
        public string Title => _title;
        public string Description => _description;
        public DateTime StartTime => _startTime;
        public DateTime EndTime => _endTime;
        public EventType Type => _type;
        public EventPriority Priority => _priority;
        public bool IsCompleted => _isCompleted;
        public IReadOnlyList<string> RelatedCharacterIDs => _relatedCharacterIDs;
        
        public CalendarEvent(
            string eventID,
            string title,
            string description,
            DateTime startTime,
            DateTime endTime,
            EventType type,
            EventPriority priority,
            bool isCompleted = false,
            List<string> relatedCharacterIDs = null)
        {
            _eventID = eventID;
            _title = title;
            _description = description;
            _startTime = startTime;
            _endTime = endTime;
            _type = type;
            _priority = priority;
            _isCompleted = isCompleted;
            _relatedCharacterIDs = relatedCharacterIDs ?? new List<string>();
        }
        
        public void MarkAsCompleted()
        {
            _isCompleted = true;
        }
        
        public TimeSpan GetDuration()
        {
            return _endTime - _startTime;
        }
        
        public bool IsAllDay()
        {
            return _startTime.TimeOfDay.TotalSeconds == 0 && 
                   _endTime.TimeOfDay.TotalSeconds == 0 && 
                   (_endTime - _startTime).TotalDays >= 1;
        }
        
        public bool IsOngoing(DateTime currentTime)
        {
            return currentTime >= _startTime && currentTime <= _endTime;
        }
    }
    
    /// <summary>
    /// Represents a special date on the calendar (holidays, anniversaries, etc.)
    /// </summary>
    [Serializable]
    public class SpecialDate
    {
        [SerializeField] private DateTime _date;
        [SerializeField] private SpecialDateType _type;
        [SerializeField] private string _description;
        [SerializeField] private VisualTheme _theme;
        
        public DateTime Date => _date;
        public SpecialDateType Type => _type;
        public string Description => _description;
        public VisualTheme Theme => _theme;
        
        public SpecialDate(
            DateTime date,
            SpecialDateType type,
            string description,
            VisualTheme theme)
        {
            _date = date;
            _type = type;
            _description = description;
            _theme = theme;
        }
    }
    
    /// <summary>
    /// Enum representing the type of calendar event
    /// </summary>
    public enum EventType
    {
        Personal,
        Social,
        Work,
        Story,
        Deadline,
        Custom
    }
    
    /// <summary>
    /// Enum representing the priority of calendar event
    /// </summary>
    public enum EventPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }
    
    /// <summary>
    /// Enum representing the type of special date
    /// </summary>
    public enum SpecialDateType
    {
        Holiday,
        Birthday,
        Anniversary,
        SeasonalEvent,
        GameEvent,
        Custom
    }
    
    /// <summary>
    /// Visual theme for special dates
    /// </summary>
    [Serializable]
    public class VisualTheme
    {
        [SerializeField] private Color _primaryColor = Color.white;
        [SerializeField] private Color _secondaryColor = Color.gray;
        [SerializeField] private Sprite _icon;
        
        public Color PrimaryColor => _primaryColor;
        public Color SecondaryColor => _secondaryColor;
        public Sprite Icon => _icon;
        
        public VisualTheme(Color primaryColor, Color secondaryColor, Sprite icon)
        {
            _primaryColor = primaryColor;
            _secondaryColor = secondaryColor;
            _icon = icon;
        }
    }
}