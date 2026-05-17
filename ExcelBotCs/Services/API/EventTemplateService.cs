using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class EventTemplateService : IEventTemplateService
{
    private readonly IEventTemplateRepository _repository;

    public EventTemplateService(IEventTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<EventTemplate>> GetAsync()
    {
        var templates = await _repository.GetAsync();
        return templates ?? new List<EventTemplate>();
    }

    public async Task<EventTemplate?> GetAsync(string id)
    {
        return await _repository.GetAsync(id);
    }

    public async Task CreateAsync(EventTemplate entity)
    {
        await _repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, EventTemplate updatedEntity)
    {
        await _repository.UpdateAsync(id, updatedEntity);
    }

    public async Task DeleteAsync(string id)
    {
        await _repository.DeleteAsync(id);
    }
}
