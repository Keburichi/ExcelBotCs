using ExcelBotCs.Models.Database.Events;

namespace ExcelBotCs.Services.API.Interfaces;

public interface IEventTemplateService
{
    Task<List<EventTemplate>> GetAsync();
    Task<EventTemplate?> GetAsync(string id);
    Task CreateAsync(EventTemplate entity);
    Task UpdateAsync(string id, EventTemplate updatedEntity);
    Task DeleteAsync(string id);
}
