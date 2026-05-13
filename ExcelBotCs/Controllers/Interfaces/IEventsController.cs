using ExcelBotCs.Models.DTO;
using ExcelBotCs.Models.DTO.Events;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers.Interfaces;

public interface IEventsController
{
    // CRUD operations
    public Task<ActionResult<PagedResult<EventResponse>>> GetEvents(int page = 1, int pageSize = 50);
    public Task<ActionResult<EventResponse>> GetEvent(string eventId);
    public Task<ActionResult<EventResponse>> CreateEvent(CreateEventRequest createEvent);
    public Task<ActionResult<EventResponse>> UpdateEvent(string id, UpdateEventRequest updateEvent);
    public Task<ActionResult> DeleteEvent(string eventId);

    // Archive operations
    public Task<ActionResult<PagedResult<EventResponse>>> GetArchivedEvents(int page = 1, int pageSize = 20,
        [FromQuery] ArchiveSearchParams? searchParams = null);

    public Task<ActionResult> ArchiveEvent(string eventId);
    public Task<ActionResult> RestoreEvent(string eventId);

    // Signups
    public Task<ActionResult> SignupForEvent(string eventId, EventSignupDto signupRequest);
    public Task<ActionResult> SelectParticipants(string eventId, List<EventGroupRequest> eventGroups);
    public Task<ActionResult> RemoveParticipant(string eventId, string participantId);

    // Occurrence operations
    public Task<ActionResult> UpdateOccurenceStatus(string eventId, string occurrenceId,
        UpdateOccurrenceStatusRequest occurrenceStatusRequest);

    public Task<ActionResult> CancelOccurence(string eventId, string occurrenceId);

    // event calendar
    public Task<ActionResult> GetEventIcal(string eventId);
}
