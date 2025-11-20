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

        await _eventRepository.CreateAsync(entity);
    }

    public async Task UpdateAsync(string id, Event updatedEntity)
    {
        // Update StartDate and EndDate from iCal string if provided
        if (!string.IsNullOrEmpty(updatedEntity.ICalString))
            _iCalService.UpdateEventDatesFromICalString(updatedEntity);
        else
            // For non-recurring events, calculate EndDate
            updatedEntity.EndDate = updatedEntity.StartDate.AddMinutes(updatedEntity.Duration);

        await _eventRepository.UpdateAsync(id, updatedEntity);
    }

    public async Task DeleteAsync(string id)
    {
        await _eventRepository.DeleteAsync(id);
    }
}