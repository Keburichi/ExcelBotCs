using ExcelBotCs.Models.DTO;
using ExcelBotCs.Models.DTO.Events;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers.Interfaces;

public interface IEventsController
{
    // CRUD operations
    public Task<ActionResult<List<EventResponse>>> GetEvents(int limit = 50, int page = 1);
    public Task<ActionResult<EventResponse>> GetEvent(string eventId);
    public Task<ActionResult<EventResponse>> CreateEvent(CreateEventRequest createEvent);
    public Task<ActionResult<EventResponse>> UpdateEvent(UpdateEventRequest updateEvent);
    public Task<ActionResult> DeleteEvent(string eventId);

    // Archive operations
    public Task<ActionResult<List<EventResponse>>> GetArchivedEvents(int limit = 50, int page = 1,
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

    // planning and concluding events
    public Task<ActionResult> PlanEvent(string id, PlannedEventRequest planRequest);

    // event calendar
    public Task<ActionResult> GetEventIcal(string eventId);
}