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

        if (calendarEvent == null)
            throw new InvalidOperationException("No events found in iCal string");

        var occurrences = new List<Occurrence>();

        // If no end has been defined for the recurrence, we need to apply some tricks. We only create one occurrence initially
        // and add the next one once the event has concluded or been cancelled.
        // This way we are adding new occurrences as the event progresses and don't have to worry about the end date calculation
        if (calendarEvent.RecurrenceRules.FirstOrDefault() != null &&
            calendarEvent.RecurrenceRules.FirstOrDefault()?.Until == null &&
            calendarEvent.RecurrenceRules.FirstOrDefault()?.Count == null)
        {
            var searchStart = new CalDateTime(DateTime.UtcNow.Year - 100, 1, 1);
            var searchEnd = new CalDateTime(DateTime.UtcNow.AddDays(1));
            occurrences = GetOccurrences(calendarEvent, searchStart.AsUtc, searchEnd.AsUtc);
        }
        else
        {
            // Get all occurrences (limit to reasonable timeframe, e.g., next 5 years)
            var searchStart = new CalDateTime(DateTime.UtcNow.Year - 100, 1, 1);
            var searchEnd = new CalDateTime(DateTime.UtcNow.AddYears(5));
            occurrences = GetOccurrences(calendarEvent, searchStart.AsUtc, searchEnd.AsUtc);
        }

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

    public List<Occurrence> GetOccurrences(string iCalString, DateTime rangeStart, DateTime rangeEnd)
    {
        var calendar = Calendar.Load(iCalString);
        var calendarEvent = calendar.Events.FirstOrDefault();

        if (calendarEvent == null)
            throw new InvalidOperationException("No events found in iCal string");

        return GetOccurrences(calendarEvent, rangeStart, rangeEnd);
    }

    public List<Occurrence> GetOccurrences(Event fcEvent, DateTime rangeStart, DateTime rangeEnd)
    {
        return GetOccurrences(fcEvent.ICalString, rangeStart, rangeEnd);
    }

    public List<Occurrence> GetOccurrences(CalendarEvent calendarEvent, DateTime rangeStart, DateTime rangeEnd)
    {
        return calendarEvent.GetOccurrences(new CalDateTime(rangeStart))
            .TakeWhile(o => o.Period.StartTime.AsUtc <= rangeEnd).ToList();
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
                    Occurrences = recurringEvent.Occurrences ?? new List<EventOccurrence>(),
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

    public List<EventOccurrence> CreateOccurrences(string iCalString, DateTime rangeStart, DateTime rangeEnd)
    {
        if (string.IsNullOrEmpty(iCalString))
            // Non-recurring event - return single occurrence
            return new List<EventOccurrence>
            {
                new()
                {
                    OccurrenceDate = rangeStart,
                    Status = OccurrenceStatus.Scheduled
                }
            };

        try
        {
            var calendar = Calendar.Load(iCalString);
            if (calendar == null || calendar.Events == null || !calendar.Events.Any())
                // Fallback: single occurrence
                return new List<EventOccurrence>
                {
                    new()
                    {
                        OccurrenceDate = rangeStart,
                        Status = OccurrenceStatus.Scheduled
                    }
                };

            var calendarEvent = calendar.Events.FirstOrDefault();
            if (calendarEvent == null)
                return new List<EventOccurrence>
                {
                    new()
                    {
                        OccurrenceDate = rangeStart,
                        Status = OccurrenceStatus.Scheduled
                    }
                };

            var rangeStartCal = new CalDateTime(rangeStart);
            var rangeEndCal = new CalDateTime(rangeEnd);
            var occurrences = calendarEvent.GetOccurrences(rangeStartCal)
                .Where(o => o?.Period?.StartTime != null && o.Period.StartTime.AsUtc <= rangeEndCal.AsUtc)
                .ToList();

            if (!occurrences.Any())
                // Fallback: single occurrence
                return new List<EventOccurrence>
                {
                    new()
                    {
                        OccurrenceDate = rangeStart,
                        Status = OccurrenceStatus.Scheduled
                    }
                };

            // Map to EventOccurrence objects
            return occurrences.Select(o => new EventOccurrence
            {
                OccurrenceDate = o.Period.StartTime.AsUtc,
                Status = OccurrenceStatus.Scheduled
            }).ToList();
        }
        catch
        {
            // If parsing fails, return single occurrence
            return new List<EventOccurrence>
            {
                new()
                {
                    OccurrenceDate = rangeStart,
                    Status = OccurrenceStatus.Scheduled
                }
            };
        }
    }

    public List<DateTime> GetOccurrenceDates(string iCalString, DateTime rangeStart, DateTime rangeEnd)
    {
        var occurrences = CreateOccurrences(iCalString, rangeStart, rangeEnd);
        return occurrences.Select(o => o.OccurrenceDate).ToList();
    }

    public bool IsRecurringEvent(string iCalString)
    {
        if (string.IsNullOrWhiteSpace(iCalString))
            return false;

        var calendar = Calendar.Load(iCalString);
        var calendarEvent = calendar?.Events.FirstOrDefault();

        return calendarEvent?.RecurrenceRules != null && calendarEvent.RecurrenceRules.Any();
    }

    public bool IsRecurrenceEnding(string iCalString)
    {
        if (string.IsNullOrWhiteSpace(iCalString))
            return false;

        var calendar = Calendar.Load(iCalString);
        var calendarEvent = calendar?.Events.FirstOrDefault();

        return calendarEvent?.RecurrenceRules.FirstOrDefault() != null &&
               calendarEvent.RecurrenceRules.FirstOrDefault()?.Until == null &&
               calendarEvent.RecurrenceRules.FirstOrDefault()?.Count == null;
    }
}