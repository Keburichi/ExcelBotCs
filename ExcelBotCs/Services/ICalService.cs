using ExcelBotCs.Models.Database;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace ExcelBotCs.Services;

public class ICalService : IICalService
{
    public string GenerateICalString(Event eventData)
    {
        var calendar = new Calendar();
        var calendarEvent = new CalendarEvent
        {
            Summary = eventData.Name,
            Description = eventData.Description,
            Start = new CalDateTime(eventData.StartDate),
            End = new CalDateTime(eventData.StartDate.AddMinutes(eventData.Duration)),
            Uid = eventData.Id ?? Guid.NewGuid().ToString()
        };

        calendar.Events.Add(calendarEvent);

        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(calendar);
    }

    public (DateTime firstOccurrence, DateTime lastOccurrence) GetOccurrenceDateRange(string iCalString,
        int durationMinutes)
    {
        var calendar = Calendar.Load(iCalString);
        var calendarEvent = calendar.Events.FirstOrDefault();

        if (calendarEvent == null) throw new InvalidOperationException("No events found in iCal string");

        // Get all occurrences (limit to reasonable timeframe, e.g., next 5 years)
        var searchStart = new CalDateTime(DateTime.MinValue);
        var searchEnd = new CalDateTime(DateTime.UtcNow.AddYears(5));
        var occurrences = calendarEvent.GetOccurrences(searchStart)
            .Where(o => o.Period.StartTime.AsUtc <= searchEnd.AsUtc).ToList();

        if (!occurrences.Any())
        {
            // Non-recurring event
            var start = calendarEvent.Start.AsUtc;
            var end = start.AddMinutes(durationMinutes);
            return (start, end);
        }

        var firstOccurrence = occurrences.First().Period.StartTime.AsUtc;
        var lastOccurrenceStart = occurrences.Last().Period.StartTime.AsUtc;
        var lastOccurrence = lastOccurrenceStart.AddMinutes(durationMinutes);

        return (firstOccurrence, lastOccurrence);
    }

    public List<Event> ExpandRecurringEvent(Event recurringEvent, DateTime rangeStart, DateTime rangeEnd)
    {
        if (string.IsNullOrEmpty(recurringEvent.ICalString)) return new List<Event> { recurringEvent };

        try
        {
            var calendar = Calendar.Load(recurringEvent.ICalString);
            if (calendar == null || calendar.Events == null || !calendar.Events.Any())
                return new List<Event> { recurringEvent };

            var calendarEvent = calendar.Events.FirstOrDefault();
            if (calendarEvent == null) return new List<Event> { recurringEvent };

            var rangeStartCal = new CalDateTime(rangeStart);
            var rangeEndCal = new CalDateTime(rangeEnd);
            var occurrences = calendarEvent.GetOccurrences(rangeStartCal)
                .Where(o => o?.Period?.StartTime != null && o.Period.StartTime.AsUtc <= rangeEndCal.AsUtc)
                .ToList();

            if (!occurrences.Any()) return new List<Event> { recurringEvent };

            var expandedEvents = new List<Event>();

            foreach (var occurrence in occurrences)
            {
                var occurrenceEvent = new Event
                {
                    Id = recurringEvent.Id,
                    Name = recurringEvent.Name ?? "",
                    Description = recurringEvent.Description ?? "",
                    Type = recurringEvent.Type,
                    StartDate = occurrence.Period.StartTime.AsUtc,
                    EndDate = occurrence.Period.EndTime?.AsUtc ??
                              occurrence.Period.StartTime.AsUtc.AddMinutes(recurringEvent.Duration),
                    Duration = recurringEvent.Duration,
                    ICalString = recurringEvent.ICalString ?? "",
                    DiscordMessageId = recurringEvent.DiscordMessageId ?? "",
                    PictureUrl = recurringEvent.PictureUrl,
                    FightId = recurringEvent.FightId,
                    Participants = recurringEvent.Participants ?? new List<EventParticipant>(),
                    Signups = recurringEvent.Signups ?? new List<EventUserSignup>(),
                    AuthorId = recurringEvent.AuthorId,
                    Organizer = recurringEvent.Organizer ?? "",
                    MaxNumberOfParticipants = recurringEvent.MaxNumberOfParticipants
                };

                expandedEvents.Add(occurrenceEvent);
            }

            return expandedEvents;
        }
        catch
        {
            // If parsing fails, return the original event
            return new List<Event> { recurringEvent };
        }
    }

    public void UpdateEventDatesFromICalString(Event eventData)
    {
        if (string.IsNullOrEmpty(eventData.ICalString))
        {
            // For non-recurring events, set EndDate based on StartDate + Duration
            eventData.EndDate = eventData.StartDate.AddMinutes(eventData.Duration);
            return;
        }

        try
        {
            var (firstOccurrence, lastOccurrence) = GetOccurrenceDateRange(eventData.ICalString, eventData.Duration);
            eventData.StartDate = firstOccurrence;
            eventData.EndDate = lastOccurrence;
        }
        catch
        {
            // Fallback if parsing fails
            eventData.EndDate = eventData.StartDate.AddMinutes(eventData.Duration);
        }
    }
}