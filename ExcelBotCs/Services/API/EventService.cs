using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using DbEventSignup = ExcelBotCs.Models.Database.Events.EventSignup;

namespace ExcelBotCs.Services.API;

public class EventService : IEventService
{
    private const double DateTimeToleranceSeconds = 1.0;

    private readonly IEventRepository _eventRepository;
    private readonly IICalService _iCalService;
    private readonly IDiscordMessageService _discordMessageService;

    public EventService(IEventRepository eventRepository, IICalService iCalService,
        IDiscordMessageService discordMessageService)
    {
        _eventRepository = eventRepository;
        _iCalService = iCalService;
        _discordMessageService = discordMessageService;
    }

    public async Task<Event> GetAsync(string id)
    {
        return await _eventRepository.GetAsync(id);
    }

    public async Task<List<Event>> GetAsync()
    {
        return await GetAsync(false);
    }

    public async Task<List<Event>> GetAsync(bool includeArchived)
    {
        var events = await _eventRepository.GetAsync();

        if (events is null)
            return new List<Event>();

        if (!includeArchived)
            events = events.Where(e => !e.IsArchived).ToList();

        return events.OrderBy(e => e.StartDate).ToList();
    }

    public async Task<List<Event>> GetArchivedAsync(ArchiveSearchParams? searchParams = null)
    {
        var events = await _eventRepository.GetAsync();

        if (events is null)
            return new List<Event>();

        var archivedEvents = events.Where(e => e.IsArchived);

        if (searchParams != null)
        {
            if (!string.IsNullOrWhiteSpace(searchParams.SearchText))
            {
                var searchLower = searchParams.SearchText.ToLowerInvariant();
                archivedEvents = archivedEvents.Where(e =>
                    e.Name.ToLowerInvariant().Contains(searchLower));
            }

            if (searchParams.StartDate.HasValue)
                archivedEvents = archivedEvents.Where(e => e.StartDate >= searchParams.StartDate.Value);

            if (searchParams.EndDate.HasValue)
                archivedEvents = archivedEvents.Where(e => e.StartDate <= searchParams.EndDate.Value);

            if (searchParams.EventType.HasValue)
                archivedEvents = archivedEvents.Where(e => e.Type == searchParams.EventType.Value);
        }

        return archivedEvents.OrderByDescending(e => e.ArchivedDate ?? e.EndDate).ToList();
    }

    public async Task HandleSignupAsync(string eventId, Role role, ulong discordUserId)
    {
        var fcEvent = await _eventRepository.GetAsync(eventId);
        if (fcEvent == null) return;

        var existing = fcEvent.Signups.FirstOrDefault(x => x.DiscordUserId == discordUserId.ToString());
        if (existing != null)
        {
            if (existing.Roles.Contains(role))
                existing.Roles.Remove(role);
            else
                existing.Roles.Add(role);
        }
        else
        {
            fcEvent.Signups.Add(new DbEventSignup
            {
                DiscordUserId = discordUserId.ToString(),
                Roles = new List<Role> { role },
                SignupDate = DateTime.UtcNow
            });
        }

        await UpdateAsync(fcEvent.Id, fcEvent);
    }

    public async Task<(bool Success, string? ErrorMessage)> ArchiveAsync(string eventId, string archivedByUserId)
    {
        var existingEvent = await _eventRepository.GetAsync(eventId);
        if (existingEvent == null)
            return (false, "Event not found");

        if (existingEvent.IsArchived)
            return (false, "Event is already archived");

        if (!existingEvent.CanBeArchived)
            return (false, "Event cannot be archived. All occurrences must be Completed or Cancelled.");

        existingEvent.IsArchived = true;
        existingEvent.ArchivedDate = DateTime.UtcNow;
        existingEvent.ArchivedByUserId = archivedByUserId;

        await _eventRepository.UpdateAsync(eventId, existingEvent);

        if (!existingEvent.DiscordMessageId.IsNullOrEmpty())
            await _discordMessageService.UpdateSignupMessage(existingEvent);

        return (true, null);
    }

    public async Task<bool> TryAutoArchiveAsync(string eventId, string archivedByUserId)
    {
        var existingEvent = await _eventRepository.GetAsync(eventId);
        if (existingEvent == null || existingEvent.IsArchived)
            return false;

        if (!existingEvent.CanBeArchived)
            return false;

        existingEvent.IsArchived = true;
        existingEvent.ArchivedDate = DateTime.UtcNow;
        existingEvent.ArchivedByUserId = archivedByUserId;

        await _eventRepository.UpdateAsync(eventId, existingEvent);

        if (!existingEvent.DiscordMessageId.IsNullOrEmpty())
            await _discordMessageService.UpdateSignupMessage(existingEvent);

        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> RestoreAsync(string eventId)
    {
        var existingEvent = await _eventRepository.GetAsync(eventId);
        if (existingEvent == null)
            return (false, "Event not found");

        if (!existingEvent.IsArchived)
            return (false, "Event is not archived");

        existingEvent.IsArchived = false;
        existingEvent.ArchivedDate = null;
        existingEvent.ArchivedByUserId = null;

        await _eventRepository.UpdateAsync(eventId, existingEvent);
        return (true, null);
    }

    public async Task<(Event? Event, string? ErrorMessage)> ExtendEventAsync(string eventId, int count)
    {
        if (count <= 0)
            return (null, "Count must be greater than 0");

        var existingEvent = await _eventRepository.GetAsync(eventId);
        if (existingEvent == null)
            return (null, "Event not found");

        if (string.IsNullOrEmpty(existingEvent.ICalString) ||
            !_iCalService.IsRecurringEvent(existingEvent.ICalString))
            return (null, "Cannot extend a non-recurring event");

        existingEvent.Occurrences ??= new List<EventOccurrence>();

        existingEvent.ICalString = _iCalService.ExtendRecurrenceCount(existingEvent.ICalString, count);

        var latestDate = existingEvent.Occurrences.Any()
            ? existingEvent.Occurrences.Max(o => o.OccurrenceDate)
            : existingEvent.StartDate;

        var newOccurrences = _iCalService.GetNextOccurrences(
            existingEvent.ICalString,
            latestDate.AddSeconds(1),
            count);

        if (newOccurrences.Count == 0)
            return (null, "Could not generate new occurrences. The recurrence pattern may have ended.");

        existingEvent.Occurrences.AddRange(newOccurrences);
        await _eventRepository.UpdateAsync(eventId, existingEvent);

        return (existingEvent, null);
    }

    public async Task CreateAsync(Event entity)
    {
        var rangeStart = entity.StartDate;

        var rangeEnd = string.IsNullOrEmpty(entity.ICalString)
            ? entity.EndDate
            : entity.StartDate.AddYears(1);

        entity.Occurrences = _iCalService.CreateOccurrences(entity.ICalString, rangeStart, rangeEnd);

        await _eventRepository.CreateAsync(entity);

        var message = await _discordMessageService.PostEventSignupAsync(entity);

        if (message != null)
        {
            entity.DiscordMessageId = message.Id.ToString();
            await _eventRepository.UpdateAsync(entity.Id, entity);
        }
    }

    public async Task UpdateAsync(string id, Event updatedEntity)
    {
        var existingEvent = await _eventRepository.GetAsync(id);

        var needsRegeneration = ShouldRegenerateOccurrences(existingEvent, updatedEntity);

        if (needsRegeneration)
        {
            var rangeStart = updatedEntity.StartDate;
            var rangeEnd = string.IsNullOrEmpty(updatedEntity.ICalString)
                ? updatedEntity.EndDate
                : updatedEntity.StartDate.AddYears(1);
            var newOccurrences = _iCalService.CreateOccurrences(updatedEntity.ICalString, rangeStart, rangeEnd);

            PreserveExistingOccurrenceData(existingEvent?.Occurrences, newOccurrences);

            updatedEntity.Occurrences = newOccurrences;
        }

        await _eventRepository.UpdateAsync(id, updatedEntity);

        if (!updatedEntity.DiscordMessageId.IsNullOrEmpty())
            await _discordMessageService.UpdateSignupMessage(updatedEntity);
    }

    public async Task DeleteAsync(string id)
    {
        var existingEvent = await _eventRepository.GetAsync(id);

        if (existingEvent != null && !existingEvent.DiscordMessageId.IsNullOrEmpty())
            await _discordMessageService.DeleteEventMessageAsync(existingEvent.DiscordMessageId);

        await _eventRepository.DeleteAsync(id);
    }

    public async Task AppendNextOccurrencesAsync(string eventId, int count = 1)
    {
        var existingEvent = await _eventRepository.GetAsync(eventId);
        if (existingEvent == null)
            return;

        if (string.IsNullOrEmpty(existingEvent.ICalString) ||
            !_iCalService.IsRecurrenceEnding(existingEvent.ICalString))
            return;

        existingEvent.Occurrences ??= new List<EventOccurrence>();

        var latestDate = existingEvent.Occurrences.Any()
            ? existingEvent.Occurrences.Max(o => o.OccurrenceDate)
            : existingEvent.StartDate;

        var newOccurrences = _iCalService.GetNextOccurrences(
            existingEvent.ICalString,
            latestDate.AddSeconds(1),
            count);

        existingEvent.Occurrences.AddRange(newOccurrences);
        await _eventRepository.UpdateAsync(eventId, existingEvent);
    }

    private bool ShouldRegenerateOccurrences(Event? existingEvent, Event updatedEntity)
    {
        if (updatedEntity.Occurrences == null || !updatedEntity.Occurrences.Any())
            return true;

        var existingIcal = existingEvent?.ICalString ?? "";
        var updatedIcal = updatedEntity.ICalString ?? "";
        if (existingIcal != updatedIcal)
            return true;

        if (existingEvent != null &&
            !AreDatesEqual(existingEvent.StartDate, updatedEntity.StartDate))
            return true;

        return false;
    }

    private static bool AreDatesEqual(DateTime date1, DateTime date2)
    {
        return Math.Abs((date1 - date2).TotalSeconds) < DateTimeToleranceSeconds;
    }

    private static void PreserveExistingOccurrenceData(
        List<EventOccurrence>? existingOccurrences,
        List<EventOccurrence> newOccurrences)
    {
        if (existingOccurrences == null || !existingOccurrences.Any())
            return;

        var existingByDate = existingOccurrences.ToDictionary(
            o => RoundToMinute(o.OccurrenceDate),
            o => o);

        foreach (var newOccurrence in newOccurrences)
        {
            var roundedDate = RoundToMinute(newOccurrence.OccurrenceDate);

            if (existingByDate.TryGetValue(roundedDate, out var existingOccurrence))
            {
                newOccurrence.Id = existingOccurrence.Id;
                newOccurrence.Status = existingOccurrence.Status;
            }
        }
    }

    private static DateTime RoundToMinute(DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
    }
}
