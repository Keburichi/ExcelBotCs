using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services.API.Interfaces;
using DbEventSignup = ExcelBotCs.Models.Database.EventSignup;

namespace ExcelBotCs.Services.API;

public class EventService : BaseEntityService<Event, IEventRepository>, IEventService
{
    // Tolerance for DateTime comparisons (accounts for MongoDB storage precision)
    private const double DateTimeToleranceSeconds = 1.0;

    private readonly IICalService _iCalService;

    public EventService(IEventRepository eventRepository, IICalService iCalService) : base(eventRepository)
    {
        _iCalService = iCalService;
    }

    public override async Task<List<Event>> GetAsync()
    {
        // Default: exclude archived events
        return await GetAsync(false);
    }

    public async Task<List<Event>> GetAsync(bool includeArchived)
    {
        var events = await Repository.GetAsync();

        if (events is null)
            return new List<Event>();

        // Filter by archive status unless includeArchived is true
        if (!includeArchived)
            events = events.Where(e => !e.IsArchived).ToList();

        // Return events as-is without expanding recurring events
        // The frontend will display recurrence information in the card
        return events.OrderBy(e => e.StartDate).ToList();
    }

    public async Task<List<Event>> GetArchivedAsync(ArchiveSearchParams? searchParams = null)
    {
        var events = await Repository.GetAsync();

        if (events is null)
            return new List<Event>();

        // Only return archived events
        var archivedEvents = events.Where(e => e.IsArchived);

        if (searchParams != null)
        {
            // Filter by search text (event name)
            if (!string.IsNullOrWhiteSpace(searchParams.SearchText))
            {
                var searchLower = searchParams.SearchText.ToLowerInvariant();
                archivedEvents = archivedEvents.Where(e =>
                    e.Name.ToLowerInvariant().Contains(searchLower));
            }

            // Filter by date range (based on event start date)
            if (searchParams.StartDate.HasValue)
                archivedEvents = archivedEvents.Where(e => e.StartDate >= searchParams.StartDate.Value);

            if (searchParams.EndDate.HasValue)
                archivedEvents = archivedEvents.Where(e => e.StartDate <= searchParams.EndDate.Value);

            // Filter by event type
            if (searchParams.EventType.HasValue)
                archivedEvents = archivedEvents.Where(e => e.Type == searchParams.EventType.Value);
        }

        return archivedEvents.OrderByDescending(e => e.ArchivedDate ?? e.EndDate).ToList();
    }

    public async Task<(bool Success, string? ErrorMessage)> ArchiveAsync(string eventId, string archivedByUserId)
    {
        var existingEvent = await Repository.GetAsync(eventId);
        if (existingEvent == null)
            return (false, "Event not found");

        if (existingEvent.IsArchived)
            return (false, "Event is already archived");

        if (!existingEvent.CanBeArchived)
            return (false, "Event cannot be archived. All occurrences must be Completed or Cancelled.");

        existingEvent.IsArchived = true;
        existingEvent.ArchivedDate = DateTime.UtcNow;
        existingEvent.ArchivedByUserId = archivedByUserId;

        await Repository.UpdateAsync(eventId, existingEvent);
        return (true, null);
    }

    public async Task<bool> TryAutoArchiveAsync(string eventId, string archivedByUserId)
    {
        var existingEvent = await Repository.GetAsync(eventId);
        if (existingEvent == null || existingEvent.IsArchived)
            return false;

        if (!existingEvent.CanBeArchived)
            return false;

        existingEvent.IsArchived = true;
        existingEvent.ArchivedDate = DateTime.UtcNow;
        existingEvent.ArchivedByUserId = archivedByUserId;

        await Repository.UpdateAsync(eventId, existingEvent);
        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> RestoreAsync(string eventId)
    {
        var existingEvent = await Repository.GetAsync(eventId);
        if (existingEvent == null)
            return (false, "Event not found");

        if (!existingEvent.IsArchived)
            return (false, "Event is not archived");

        existingEvent.IsArchived = false;
        existingEvent.ArchivedDate = null;
        existingEvent.ArchivedByUserId = null;

        await Repository.UpdateAsync(eventId, existingEvent);
        return (true, null);
    }

    public async Task<(Event? Event, string? ErrorMessage)> ExtendEventAsync(string eventId, int count)
    {
        if (count <= 0)
            return (null, "Count must be greater than 0");

        var existingEvent = await Repository.GetAsync(eventId);
        if (existingEvent == null)
            return (null, "Event not found");

        // Check if event is recurring
        if (string.IsNullOrEmpty(existingEvent.ICalString) ||
            !_iCalService.IsRecurringEvent(existingEvent.ICalString))
            return (null, "Cannot extend a non-recurring event");

        existingEvent.Occurrences ??= new List<EventOccurrence>();

        // For bounded recurring events (with COUNT), extend the COUNT first
        existingEvent.ICalString = _iCalService.ExtendRecurrenceCount(existingEvent.ICalString, count);

        // Find the latest occurrence date
        var latestDate = existingEvent.Occurrences.Any()
            ? existingEvent.Occurrences.Max(o => o.OccurrenceDate)
            : existingEvent.StartDate;

        // Generate next occurrences starting after the latest one
        var newOccurrences = _iCalService.GetNextOccurrences(
            existingEvent.ICalString,
            latestDate.AddSeconds(1),
            count);

        if (newOccurrences.Count == 0)
            return (null, "Could not generate new occurrences. The recurrence pattern may have ended.");

        existingEvent.Occurrences.AddRange(newOccurrences);
        await Repository.UpdateAsync(eventId, existingEvent);

        return (existingEvent, null);
    }

    public override async Task CreateAsync(Event entity)
    {
        // Create occurrences from iCal string or single occurrence for non-recurring events
        var rangeStart = entity.StartDate;

        // For recurring events, always look ahead 1 year from start date, not end date
        // This ensures we generate enough occurrences even if EndDate calculation fails
        var rangeEnd = string.IsNullOrEmpty(entity.ICalString)
            ? entity.EndDate
            : entity.StartDate.AddYears(1);

        entity.Occurrences = _iCalService.CreateOccurrences(entity.ICalString, rangeStart, rangeEnd);

        await Repository.CreateAsync(entity);
    }

    public override async Task UpdateAsync(string id, Event updatedEntity)
    {
        // Get existing event to check if iCal changed
        var existingEvent = await Repository.GetAsync(id);

        // Check if regeneration is actually needed
        var needsRegeneration = ShouldRegenerateOccurrences(existingEvent, updatedEntity);

        if (needsRegeneration)
        {
            // Regenerate occurrences
            var rangeStart = updatedEntity.StartDate;
            // For recurring events, look ahead 1 year from start date
            var rangeEnd = string.IsNullOrEmpty(updatedEntity.ICalString)
                ? updatedEntity.EndDate
                : updatedEntity.StartDate.AddYears(1);
            var newOccurrences = _iCalService.CreateOccurrences(updatedEntity.ICalString, rangeStart, rangeEnd);

            // Preserve existing signups/participants/status using O(1) dictionary lookup
            PreserveExistingOccurrenceData(existingEvent?.Occurrences, newOccurrences);

            updatedEntity.Occurrences = newOccurrences;
        }

        await Repository.UpdateAsync(id, updatedEntity);
    }

    /// <summary>
    ///     Appends the next N occurrences for an infinite recurring event.
    ///     Use this instead of full regeneration when an occurrence completes.
    /// </summary>
    public async Task AppendNextOccurrencesAsync(string eventId, int count = 1)
    {
        var existingEvent = await Repository.GetAsync(eventId);
        if (existingEvent == null)
            return;

        // Only works for recurring events without an end
        if (string.IsNullOrEmpty(existingEvent.ICalString) ||
            !_iCalService.IsRecurrenceEnding(existingEvent.ICalString))
            return;

        existingEvent.Occurrences ??= new List<EventOccurrence>();

        // Find the latest occurrence date
        var latestDate = existingEvent.Occurrences.Any()
            ? existingEvent.Occurrences.Max(o => o.OccurrenceDate)
            : existingEvent.StartDate;

        // Generate next occurrences starting after the latest one
        var newOccurrences = _iCalService.GetNextOccurrences(
            existingEvent.ICalString,
            latestDate.AddSeconds(1), // Start just after the latest
            count);

        existingEvent.Occurrences.AddRange(newOccurrences);
        await Repository.UpdateAsync(eventId, existingEvent);
    }

    /// <summary>
    ///     Determines if occurrences need to be regenerated based on meaningful changes.
    ///     Uses tolerance-based comparison to avoid false positives from datetime precision issues.
    /// </summary>
    private bool ShouldRegenerateOccurrences(Event? existingEvent, Event updatedEntity)
    {
        // Always regenerate if no occurrences exist
        if (updatedEntity.Occurrences == null || !updatedEntity.Occurrences.Any())
            return true;

        // Check if iCal string meaningfully changed (treat null and empty as equivalent)
        var existingIcal = existingEvent?.ICalString ?? "";
        var updatedIcal = updatedEntity.ICalString ?? "";
        if (existingIcal != updatedIcal)
            return true;

        // Check if start date changed (with tolerance for MongoDB precision)
        if (existingEvent != null &&
            !AreDatesEqual(existingEvent.StartDate, updatedEntity.StartDate))
            return true;

        return false;
    }

    /// <summary>
    ///     Compares two DateTimes with tolerance for storage precision differences.
    /// </summary>
    private static bool AreDatesEqual(DateTime date1, DateTime date2)
    {
        return Math.Abs((date1 - date2).TotalSeconds) < DateTimeToleranceSeconds;
    }

    /// <summary>
    ///     Preserves signups, participants, status, and other data from existing occurrences
    ///     using O(1) dictionary lookup instead of O(n) linear search.
    /// </summary>
    private static void PreserveExistingOccurrenceData(
        List<EventOccurrence>? existingOccurrences,
        List<EventOccurrence> newOccurrences)
    {
        if (existingOccurrences == null || !existingOccurrences.Any())
            return;

        // Build dictionary keyed by occurrence date rounded to the minute for fast lookup
        var existingByDate = existingOccurrences.ToDictionary(
            o => RoundToMinute(o.OccurrenceDate),
            o => o);

        foreach (var newOccurrence in newOccurrences)
        {
            var roundedDate = RoundToMinute(newOccurrence.OccurrenceDate);

            if (existingByDate.TryGetValue(roundedDate, out var existingOccurrence))
            {
                // Preserve data from existing occurrence
                newOccurrence.Id = existingOccurrence.Id;
                newOccurrence.Status = existingOccurrence.Status;
                newOccurrence.DiscordMessageId = existingOccurrence.DiscordMessageId;
                newOccurrence.Signups = existingOccurrence.Signups ?? new List<DbEventSignup>();
                newOccurrence.Participants = existingOccurrence.Participants ?? new List<EventParticipant>();
            }
        }
    }

    /// <summary>
    ///     Rounds a DateTime to the nearest minute for consistent dictionary key matching.
    /// </summary>
    private static DateTime RoundToMinute(DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
    }
}
