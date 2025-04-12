using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using CharacterSystem;
using SocialActivity;

/// <summary>
/// Extension methods for GameDate to provide formatted date information
/// </summary>
public static class GameDateExtensions
{
    public static string GetDayOfWeekName(this GameDate date)
    {
        var timeManager = SocialActivitySystem.Instance.TimeSystem as SocialActivity.TimeManager;
        if (timeManager != null)
        {
            string[] dayNames = { "日曜", "月曜", "火曜", "水曜", "木曜", "金曜", "土曜" };

            int dayIndex = timeManager.GetDayOfWeek() switch
            {
                DayOfWeek.Sunday => 0,
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                _ => -1
            };
            return (dayIndex >= 0 && dayIndex < dayNames.Length) ? dayNames[dayIndex] : "Unknown";
        }

        return "Unknown";
    }

    public static string GetTimeOfDayName(this GameDate date)
    {
        return date.TimeOfDay switch
        {
            SocialActivity.TimeOfDay.EarlyMorning => "早朝",
            SocialActivity.TimeOfDay.Morning => "朝",
            SocialActivity.TimeOfDay.Afternoon => "昼",
            SocialActivity.TimeOfDay.Evening => "夕方",
            SocialActivity.TimeOfDay.Night => "夜",
            SocialActivity.TimeOfDay.LateNight => "深夜",
            _ => "不明"
        };
    }

    public static SocialActivity.TimeOfDay GetTimeOfDay(this GameDate date)
    {
        return date.TimeOfDay;
    }
}