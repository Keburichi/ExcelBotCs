using System.Text;
using ExcelBotCs.Attributes;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers.Events;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Models.DTO.Events;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/[controller]")]
public class EventsController : AuthorizedController, IEventsController
{
    private readonly ICurrentMemberAccessor _currentMemberAccessor;
    private readonly IDiscordMessageService _discordMessageService;
    private readonly IEventService _eventService;
    private readonly IICalService _iiCalService;
    private readonly IDiscordMessageCreator _discordMessageCreator;
    private readonly string _rootUrl;

    public EventsController(ILogger<EventsController> logger, IEventService eventService,
        ICurrentMemberAccessor currentMemberAccessor, IDiscordMessageService discordMessageService,
        IICalService iiCalService, IDiscordMessageCreator discordMessageCreator) : base(logger)
    {
        _eventService = eventService;
        _currentMemberAccessor = currentMemberAccessor;
        _discordMessageService = discordMessageService;
        _iiCalService = iiCalService;
        _discordMessageCreator = discordMessageCreator;
        _rootUrl = Utils.GetEnvVar("EVENT_ENDPOINT_URL", nameof(TeamFormationInteraction));
    }

    #region CRUD Operations

    [HttpGet]
    public async Task<ActionResult<PagedResult<EventResponse>>> GetEvents(int page = 1, int pageSize = 50)
    {
        var pagedResult = await _eventService.GetPagedAsync(page, pageSize);
        return Ok(pagedResult.ToPagedEventResponse());
    }

    [HttpGet("{eventId:length(24)}")]
    public async Task<ActionResult<EventResponse>> GetEvent(string eventId)
    {
        var fcEvent = await _eventService.GetAsync(eventId);

        if (fcEvent == null)
            return NotFound();

        return fcEvent.ToEventResponse();
    }

    [HttpPost]
    [AdminAuth]
    public async Task<ActionResult<EventResponse>> CreateEvent(CreateEventRequest createEvent)
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is null)
            return BadRequest("Member not found for the current user");

        var newFcEvent = createEvent.ToFcEvent();

        newFcEvent.AuthorId = member.Id;
        newFcEvent.Organizer = member.PlayerName;

        await _eventService.CreateAsync(newFcEvent);
        return CreatedAtAction(nameof(CreateEvent), new { id = newFcEvent.Id }, newFcEvent);
    }

    [HttpPut("{id:length(24)}")]
    [AdminAuth]
    public async Task<ActionResult<EventResponse>> UpdateEvent(string id, [FromBody] UpdateEventRequest updateEvent)
    {
        var existingEvent = await _eventService.GetAsync(id);
        if (existingEvent is null)
            return NotFound();

        existingEvent.ApplyUpdate(updateEvent);

        await _eventService.UpdateAsync(id, existingEvent);

        return Ok(existingEvent.ToEventResponse());
    }

    [HttpDelete("{eventId:length(24)}")]
    [AdminAuth]
    public async Task<ActionResult> DeleteEvent(string eventId)
    {
        var fcEvent = await _eventService.GetAsync(eventId);

        if (fcEvent is null)
            return NotFound();

        await _eventService.DeleteAsync(eventId);
        return NoContent();
    }

    #endregion

    #region Archive Operations

    [HttpGet("archived")]
    [AdminAuth]
    public async Task<ActionResult<PagedResult<EventResponse>>> GetArchivedEvents(int page = 1, int pageSize = 20,
        [FromQuery] ArchiveSearchParams? searchParams = null)
    {
        var pagedResult = await _eventService.GetArchivedPagedAsync(page, pageSize, searchParams);
        return Ok(pagedResult.ToPagedEventResponse());
    }

    [HttpPost("{id:length(24)}/archive")]
    [AdminAuth]
    public async Task<ActionResult> ArchiveEvent(string id)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        var (success, errorMessage) = await _eventService.ArchiveAsync(id, user.DiscordId);
        return success ? Ok() : BadRequest(errorMessage);
    }

    [HttpPost("{id:length(24)}/restore")]
    [AdminAuth]
    public async Task<ActionResult> RestoreEvent(string id)
    {
        var (success, errorMessage) = await _eventService.RestoreAsync(id);
        return success ? Ok() : BadRequest(errorMessage);
    }

    #endregion

    #region Signups

    [HttpPost]
    [Route("{eventId}/signup")]
    public async Task<ActionResult> SignupForEvent(string eventId, [FromBody] EventSignupDto signupRequest)
    {
        var fcEvent = await _eventService.GetAsync(eventId);

        if (fcEvent is null)
            return NotFound();

        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is null)
            return BadRequest("Member not found for the current user");

        // Check if the member is already signed up for this occurrence
        var existing = fcEvent.Signups.FirstOrDefault(x => x.DiscordUserId == member.DiscordId);
        if (existing != null)
            // Update roles for existing signup
            existing.Roles = signupRequest.Roles;
        else
            fcEvent.Signups.Add(new EventSignup
            {
                DiscordUserId = member.DiscordId,
                Roles = signupRequest.Roles,
                SignupDate = DateTime.UtcNow
            });

        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

        return Ok();
    }

    [HttpDelete]
    [Route("{eventId}/signup")]
    public async Task<ActionResult> CancelSignup(string eventId)
    {
        var fcEvent = await _eventService.GetAsync(eventId);

        if (fcEvent is null)
            return NotFound();

        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is null)
            return BadRequest("Member not found for the current user");

        var existing = fcEvent.Signups.FirstOrDefault(x => x.DiscordUserId == member.DiscordId);
        if (existing == null)
            return Ok();

        fcEvent.Signups.Remove(existing);
        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

        return Ok();
    }

    [HttpPost]
    [Route("{eventId}/participants")]
    [AdminAuth]
    public async Task<ActionResult> SelectParticipants(string eventId, [FromBody] List<EventGroupRequest> eventGroups)
    {
        var fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent is null)
            return NotFound("Event not found");

        // Set selection date for all participants
        foreach (var group in eventGroups)
        {
            foreach (var participant in group.Participants)
            {
                participant.SelectionDate = DateTime.UtcNow;
            }
        }

        fcEvent.Groups = eventGroups.ToEventGroups();

        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

        // Post the roster to Discord
        await _discordMessageService.PostInUpcomingRosterChannelAsync(
            await _discordMessageCreator.CreateUpcomingRosterMessage(fcEvent));

        return Ok();
    }

    [HttpDelete]
    [Route("{eventId}/participants/{userId}")]
    [AdminAuth]
    public async Task<ActionResult> RemoveParticipant(string eventId, string participantId)
    {
        var fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent is null)
            return NotFound("Event not found");

        EventParticipant participant = null;
        foreach (var eventParticipant in fcEvent.Groups.Select(fcEventGroup =>
                     fcEventGroup.Participants.FirstOrDefault(eventParticipant =>
                         eventParticipant.DiscordUserId == participantId)))
            participant = eventParticipant;

        // var participant = fcEvent.Participants.FirstOrDefault(p => p.DiscordUserId == userId);
        if (participant == null)
            return Ok();

        fcEvent.Groups.ForEach(x => x.Participants.Remove(participant));
        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

        return Ok();
    }

    #endregion

    #region Occurrence

    [HttpPatch]
    [Route("{eventId}/occurrences/{occurrenceId}/status")]
    [AdminAuth]
    public async Task<ActionResult> UpdateOccurenceStatus(string eventId, string occurrenceId,
        [FromBody] UpdateOccurrenceStatusRequest statusUpdate)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        var fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent is null)
            return NotFound("Event not found");

        var occurrence = fcEvent.Occurrences?.FirstOrDefault(o => o.Id == occurrenceId);
        if (occurrence == null)
            return NotFound("Occurrence not found");

        // Validation: Can't complete future occurrences
        if (statusUpdate.Status == OccurrenceStatus.Completed && occurrence.OccurrenceDate > DateTime.UtcNow)
            return BadRequest("Cannot mark future occurrence as completed");

        occurrence.Status = statusUpdate.Status;

        // If the event is reoccurring and has no end date, create a new occurrence for the next occurrence date
        if (_iiCalService.IsRecurringEvent(fcEvent.ICalString) && !_iiCalService.IsRecurrenceEnding(fcEvent.ICalString))
        {
            // Check if there already is an occurrence for this date
            var occurences = _iiCalService.GetOccurrences(fcEvent, occurrence.OccurrenceDate.ToUniversalTime(),
                occurrence.OccurrenceDate.AddMonths(1).ToUniversalTime()).OrderBy(x => x.Period.StartTime).ToList();

            // Filter out the current occurrence
            occurences.RemoveAll(x => x.Period.StartTime.AsUtc == occurrence.OccurrenceDate.ToUniversalTime());
        }

        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

        // Auto-archive if all occurrences are now completed or cancelled
        if (statusUpdate.Status == OccurrenceStatus.Completed || statusUpdate.Status == OccurrenceStatus.Cancelled)
            await _eventService.TryAutoArchiveAsync(fcEvent.Id, user.DiscordId);

        return Ok();
    }

    [HttpDelete]
    [Route("{eventId}/occurrences/{occurrenceId}")]
    [AdminAuth]
    public async Task<ActionResult> CancelOccurence(string eventId, string occurrenceId)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        var fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent is null)
            return NotFound("Event not found");

        var occurrence = fcEvent.Occurrences?.FirstOrDefault(o => o.Id == occurrenceId);
        if (occurrence == null)
            return NotFound("Occurrence not found");

        // Mark as cancelled rather than deleting to preserve history
        occurrence.Status = OccurrenceStatus.Cancelled;

        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

        // Auto-archive if all occurrences are now completed or cancelled
        await _eventService.TryAutoArchiveAsync(fcEvent.Id, user.DiscordId);

        return Ok();
    }

    #endregion

    #region event calendar

    [HttpGet]
    [Route("retrieve/{userId}.ics")]
    [AllowAnonymous]
    public async Task<ActionResult> GetEventIcal(string userId)
    {
        try
        {
            // The id here is a Discord user id. Build a calendar of events the user signed up for or participates in.
            var allEvents = await _eventService.GetAsync();
            if (allEvents is null || !allEvents.Any()) return NotFound();

            var userEvents = new List<Event>();

            foreach (var fcEvent in allEvents)
            foreach (var fcEventGroup in fcEvent.Groups)
                if (fcEventGroup.Participants.Any(x => x.DiscordUserId == userId))
                    userEvents.Add(fcEvent);

            // It's okay to return an empty but valid calendar if there are no events
            var calendar = new Calendar
            {
                Name = "Excelsior Events Calendar",
                Method = CalendarMethods.Publish,
                ProductId = "-//Excelsior//Excelsior Events Calendar//EN",
                Version = "2.0",
                Scale = CalendarScales.Gregorian
            };

            // Best-effort absolute URL to this ICS
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            calendar.AddProperty(new CalendarProperty("URL", $"{baseUrl}/api/Events/retrieve/{userId}.ics"));

            foreach (var fcEvent in userEvents)
            {
                var calEvent = CreateCalendarEvent(fcEvent);

                // Propagate recurrence rules if present in stored ICal string
                if (!string.IsNullOrWhiteSpace(fcEvent.ICalString))
                    try
                    {
                        var srcCal = Calendar.Load(fcEvent.ICalString);
                        var srcEvt = srcCal.Events.FirstOrDefault();
                        if (srcEvt?.RecurrenceRules != null && srcEvt.RecurrenceRules.Any())
                            foreach (var rrule in srcEvt.RecurrenceRules)
                                calEvent.RecurrenceRules.Add(rrule);
                    }
                    catch
                    {
                        // Ignore malformed recurrence; still provide a valid single event
                    }

                calendar.Events.Add(calEvent);
            }

            var serializer = new CalendarSerializer();
            var ics = serializer.SerializeToString(calendar);
            var file = Encoding.UTF8.GetBytes(ics);
            return File(file, "text/calendar; charset=utf-8", $"{userId}.ics");
        }
        catch (Exception e)
        {
            return BadRequest("Event is malformed");
        }
    }

    private CalendarEvent CreateCalendarEvent(Event fcEvent)
    {
        var startUtc = DateTime.SpecifyKind(fcEvent.StartDate, DateTimeKind.Utc);
        var endUtc = startUtc.AddMinutes(fcEvent.Duration);

        return new CalendarEvent
        {
            Location = "Final Fantasy XIV Online",
            Status = EventStatus.Confirmed,
            Summary = fcEvent.Name,
            Description = fcEvent.Description,
            Uid = string.IsNullOrWhiteSpace(fcEvent.Id) ? Guid.NewGuid().ToString() : fcEvent.Id,
            DtStamp = new CalDateTime(DateTime.UtcNow, TimeZoneInfo.Utc.Id),
            Start = new CalDateTime(startUtc, TimeZoneInfo.Utc.Id),
            End = new CalDateTime(endUtc, TimeZoneInfo.Utc.Id)
        };
    }

    #endregion


    [HttpPost("{id:length(24)}/extend")]
    [AdminAuth]
    public async Task<ActionResult<EventResponse>> ExtendEvent(string id, [FromBody] ExtendEventRequest request)
    {
        var (updatedEvent, errorMessage) = await _eventService.ExtendEventAsync(id, request.Count);

        if (updatedEvent == null)
            return BadRequest(errorMessage);

        return Ok(updatedEvent.ToEventResponse());
    }
}