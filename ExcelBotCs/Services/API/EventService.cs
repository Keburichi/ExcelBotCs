using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.API;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;

    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<List<Event>> GetAsync()
    {
        return await _eventRepository.GetAsync();
    }

    public async Task<Event> GetAsync(string id)
    {
        return  await _eventRepository.GetAsync(id);
    }

    public async Task CreateAsync(Event entity)
    {
        await _eventRepository.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, Event updatedEntity)
    {
        await _eventRepository.UpdateAsync(id, updatedEntity);
    }

    public async Task DeleteAsync(string id)
    {
        await _eventRepository.DeleteAsync(id);
    }
}