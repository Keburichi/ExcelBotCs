using ExcelBotCs.Models.Database;

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

    /// <summary>
    ///     Expands a recurring event into individual occurrences within a date range
    /// </summary>
    List<Event> ExpandRecurringEvent(Event recurringEvent, DateTime rangeStart, DateTime rangeEnd);

    /// <summary>
    ///     Updates an Event object with calculated StartDate and EndDate from its iCal string
    /// </summary>
    void UpdateEventDatesFromICalString(Event eventData);
}