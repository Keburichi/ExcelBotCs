using ExcelBotCs.Models.Database;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace ExcelBotCs.Services;

public interface IICalService
{
    /// <summary>
    ///     Generates an iCal string from event data
    /// </summary>
    string GenerateICalString(Event eventData);

    /// <summary>
    ///     Parses an iCal string and extracts the first and last occurrence dates
    /// </summary>
    (DateTime firstOccurrence, DateTime lastOccurrence) GetOccurrenceDateRange(string iCalString, int durationMinutes);

    List<Occurrence> GetOccurrences(string iCalString, DateTime rangeStart, DateTime rangeEnd);
    List<Occurrence> GetOccurrences(Event fcEvent, DateTime rangeStart, DateTime rangeEnd);
    List<Occurrence> GetOccurrences(CalendarEvent calendarEvent, DateTime rangeStart, DateTime rangeEnd);

    /// <summary>
    ///     Expands a recurring event into individual occurrences within a date range
    /// </summary>
    List<Event> ExpandRecurringEvent(Event recurringEvent, DateTime rangeStart, DateTime rangeEnd);

    /// <summary>
    ///     Updates an Event object with calculated StartDate and EndDate from its iCal string
    /// </summary>
    void UpdateEventDatesFromICalString(Event eventData);

    /// <summary>
    ///     Creates EventOccurrence objects from an iCal string within a date range
    /// </summary>
    List<EventOccurrence> CreateOccurrences(string iCalString, DateTime rangeStart, DateTime rangeEnd);

    /// <summary>
    ///     Extracts occurrence dates from an iCal string within a date range
    /// </summary>
    List<DateTime> GetOccurrenceDates(string iCalString, DateTime rangeStart, DateTime rangeEnd);

    bool IsRecurringEvent(string iCalString);
    bool IsRecurrenceEnding(string iCalString);
}