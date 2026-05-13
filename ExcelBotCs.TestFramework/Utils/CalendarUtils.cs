using ExcelBotCs.Models.Database.Events;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace ExcelBotCs.TestFramework.Utils;

public static class CalendarUtils
{
    public static string CreateICalString(DateTime startDate, FrequencyType frequencyType, int recurrenceCount)
    {
        var calendar = new Calendar();
        var calendarEvent = new CalendarEvent();
        calendarEvent.DtStart = new CalDateTime(startDate);
        calendarEvent.RecurrenceRules = new List<RecurrencePattern>
        {
            new(frequencyType)
            {
                Count = recurrenceCount
            }
        };

        calendar.Events.Add(calendarEvent);

        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(calendar);
    }

    public static string CreateICalString(Event fcEvent, FrequencyType frequencyType, int recurrenceCount)
    {
        return CreateICalString(fcEvent.StartDate, frequencyType, recurrenceCount);
    }
}