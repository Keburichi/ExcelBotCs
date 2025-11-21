using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IICalService _iCalService;

    public EventService(IEventRepository eventRepository, IICalService iCalService)
    {
        _eventRepository = eventRepository;
        _iCalService = iCalService;
    }

    public async Task<List<Event>> GetAsync()
    {
        var events = await _eventRepository.GetAsync();

        // Return events as-is without expanding recurring events
        // The frontend will display recurrence information in the card
        return events.OrderBy(e => e.StartDate).ToList();
    }

    public async Task<Event> GetAsync(string id)
    {
        return await _eventRepository.GetAsync(id);
    }

    public async Task CreateAsync(Event entity)
    {
        // Update StartDate and EndDate from iCal string if provided
        if (!string.IsNullOrEmpty(entity.ICalString))
            _iCalService.UpdateEventDatesFromICalString(entity);
        else
            // For non-recurring events, calculate EndDate
            entity.EndDate = entity.StartDate.AddMinutes(entity.Duration);

        // Create occurrences from iCal string or single occurrence for non-recurring events
        var rangeStart = entity.StartDate;
        // For recurring events, always look ahead 1 year from start date, not end date
        // This ensures we generate enough occurrences even if EndDate calculation fails
        var rangeEnd = string.IsNullOrEmpty(entity.ICalString)
            ? entity.EndDate
            : entity.StartDate.AddYears(1);

        entity.Occurrences = _iCalService.CreateOccurrences(entity.ICalString, rangeStart, rangeEnd);

        await _eventRepository.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, Event updatedEntity)
    {
        // Get existing event to check if iCal changed
        var existingEvent = await _eventRepository.GetAsync(id);

        // Update StartDate and EndDate from iCal string if provided
        if (!string.IsNullOrEmpty(updatedEntity.ICalString))
            _iCalService.UpdateEventDatesFromICalString(updatedEntity);
        else
            // For non-recurring events, calculate EndDate
            updatedEntity.EndDate = updatedEntity.StartDate.AddMinutes(updatedEntity.Duration);

        // Check if iCal string has changed or if occurrences need regeneration
        var iCalChanged = existingEvent?.ICalString != updatedEntity.ICalString;
        var datesChanged = existingEvent?.StartDate != updatedEntity.StartDate;

        if (iCalChanged || datesChanged || updatedEntity.Occurrences == null || !updatedEntity.Occurrences.Any())
        {
            // Regenerate occurrences
            var rangeStart = updatedEntity.StartDate;
            // For recurring events, look ahead 1 year from start date
            var rangeEnd = string.IsNullOrEmpty(updatedEntity.ICalString)
                ? updatedEntity.EndDate
                : updatedEntity.StartDate.AddYears(1);
            var newOccurrences = _iCalService.CreateOccurrences(updatedEntity.ICalString, rangeStart, rangeEnd);

            // Preserve existing signups/participants/status by matching OccurrenceDate
            if (existingEvent?.Occurrences != null)
                foreach (var newOccurrence in newOccurrences)
                {
                    // Try to find matching existing occurrence (within 1 minute tolerance for floating point date comparisons)
                    var existingOccurrence = existingEvent.Occurrences
                        .FirstOrDefault(o =>
                            Math.Abs((o.OccurrenceDate - newOccurrence.OccurrenceDate).TotalMinutes) < 1);

                    if (existingOccurrence != null)
                    {
                        // Preserve data from existing occurrence
                        newOccurrence.Id = existingOccurrence.Id;
                        newOccurrence.Status = existingOccurrence.Status;
                        newOccurrence.DiscordMessageId = existingOccurrence.DiscordMessageId;
                        newOccurrence.Signups = existingOccurrence.Signups ?? new List<EventSignup>();
                        newOccurrence.Participants = existingOccurrence.Participants ?? new List<EventParticipant>();
                    }
                }

            updatedEntity.Occurrences = newOccurrences;
        }

        await _eventRepository.UpdateAsync(id, updatedEntity);
    }

    public async Task DeleteAsync(string id)
    {
        await _eventRepository.DeleteAsync(id);
    }
}