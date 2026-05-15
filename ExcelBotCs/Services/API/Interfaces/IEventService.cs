using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IEventService
{
    Task<List<Event>> GetAsync();
    Task<Event> GetAsync(string id);
    Task CreateAsync(Event entity);
    Task UpdateAsync(string id, Event updatedEntity);
    Task DeleteAsync(string id);

    Task<List<Event>> GetAsync(bool includeArchived);
    Task<PagedResult<Event>> GetPagedAsync(int page, int pageSize);
    Task<PagedResult<Event>> GetArchivedPagedAsync(int page, int pageSize, ArchiveSearchParams? searchParams = null);
    Task<List<Event>> GetArchivedAsync(ArchiveSearchParams? searchParams = null);
    Task<(bool Success, string? ErrorMessage)> ArchiveAsync(string eventId, string archivedByUserId);
    Task<bool> TryAutoArchiveAsync(string eventId, string archivedByUserId);
    Task<(bool Success, string? ErrorMessage)> RestoreAsync(string eventId);
    Task<(Event? Event, string? ErrorMessage)> ExtendEventAsync(string eventId, int count);
    Task AppendNextOccurrencesAsync(string eventId, int count = 1);
    Task HandleSignupAsync(string eventId, Role role, ulong discordUserId);
    Task HandleSignupAsync(string eventId, string slug, ulong discordUserId);
}
