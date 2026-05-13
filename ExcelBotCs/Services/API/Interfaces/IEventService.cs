using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IEventService : IBaseEntityService<Event>
{
    /// <summary>
    ///     Gets events filtered by archive status
    /// </summary>
    /// <param name="includeArchived">If true, includes archived events; if false, excludes them (default: false)</param>
    Task<List<Event>> GetAsync(bool includeArchived);

    /// <summary>
    ///     Gets archived events with optional search/filter parameters
    /// </summary>
    Task<List<Event>> GetArchivedAsync(ArchiveSearchParams? searchParams = null);

    /// <summary>
    ///     Archives an event. The event must have all occurrences Completed or Cancelled.
    /// </summary>
    /// <param name="eventId">The event ID to archive</param>
    /// <param name="archivedByUserId">The Discord user ID of the admin archiving the event</param>
    /// <returns>True if archived successfully, false if event doesn't exist or cannot be archived</returns>
    Task<(bool Success, string? ErrorMessage)> ArchiveAsync(string eventId, string archivedByUserId);

    /// <summary>
    ///     Automatically archives an event if all occurrences are Completed or Cancelled.
    ///     Called after occurrence status changes.
    /// </summary>
    /// <param name="eventId">The event ID to check</param>
    /// <param name="archivedByUserId">The Discord user ID of the user triggering the archive</param>
    /// <returns>True if the event was archived, false otherwise</returns>
    Task<bool> TryAutoArchiveAsync(string eventId, string archivedByUserId);

    /// <summary>
    ///     Restores an archived event back to active status
    /// </summary>
    /// <param name="eventId">The event ID to restore</param>
    /// <returns>True if restored successfully, false if event doesn't exist or isn't archived</returns>
    Task<(bool Success, string? ErrorMessage)> RestoreAsync(string eventId);

    /// <summary>
    ///     Extends a recurring event by adding more occurrences
    /// </summary>
    /// <param name="eventId">The event ID to extend</param>
    /// <param name="count">Number of occurrences to add</param>
    /// <returns>The updated event with new occurrences</returns>
    Task<(Event? Event, string? ErrorMessage)> ExtendEventAsync(string eventId, int count);

    /// <summary>
    ///     Appends the next N occurrences for an infinite recurring event.
    ///     Use this instead of full regeneration when an occurrence completes.
    /// </summary>
    Task AppendNextOccurrencesAsync(string eventId, int count = 1);

    /// <summary>
    ///     Handles a signup button interaction for an event.
    ///     Toggles the given role for the user on the next upcoming scheduled occurrence.
    /// </summary>
    Task HandleSignupAsync(string eventId, Role role, ulong discordUserId);
}