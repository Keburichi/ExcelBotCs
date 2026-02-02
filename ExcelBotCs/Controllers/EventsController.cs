using System.Text;
using ExcelBotCs.Attributes;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Extensions;
using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Mvc;
using DbEventSignup = ExcelBotCs.Models.Database.EventSignup;

namespace ExcelBotCs.Controllers;

[ApiController]
[MemberAuth]
[Route("api/[controller]")]
public class EventsController : AuthorizedController, IBaseCrudController<EventDto>
{
    private readonly ICurrentMemberAccessor _currentMemberAccessor;
    private readonly IDiscordMessageService _discordMessageService;
    private readonly IEventService _eventService;
    private readonly IICalService _iiCalService;
    private readonly string _rootUrl;

    public EventsController(ILogger<EventsController> logger, IEventService eventService,
        ICurrentMemberAccessor currentMemberAccessor, IDiscordMessageService discordMessageService,
        IICalService iiCalService) : base(logger)
    {
        _eventService = eventService;
        _currentMemberAccessor = currentMemberAccessor;
        _discordMessageService = discordMessageService;
        _iiCalService = iiCalService;
        _rootUrl = Utils.GetEnvVar("EVENT_ENDPOINT_URL", nameof(TeamFormationInteraction));
    }

    [HttpGet]
    public async Task<ActionResult<List<EventDto>>> GetEntities([FromQuery] bool archived = false)
    {
        return await GetEntitiesInternal(archived);
    }

    // Explicit interface implementation to satisfy IBaseCrudController<EventDto>
    async Task<ActionResult<List<EventDto>>> IBaseCrudController<EventDto>.GetEntities()
    {
        return await GetEntitiesInternal(false);
    }

    private async Task<ActionResult<List<EventDto>>> GetEntitiesInternal(bool archived)
    {
        var entities = await _eventService.GetAsync(archived);

        if (entities is null)
            return new List<EventDto>();

        var dtos = entities.Select(EventMapper.ToDto).ToList();

        return dtos;
    }

    [HttpGet("archived")]
    [AdminAuth]
    public async Task<ActionResult<List<EventDto>>> GetArchivedEvents(
        [FromQuery] ArchiveSearchParams? searchParams = null)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        if (!user.IsAdmin.GetValueOrDefault())
            return Forbid();

        var entities = await _eventService.GetArchivedAsync(searchParams);
        var dtos = entities.Select(EventMapper.ToDto).ToList();

        return dtos;
    }

    [HttpPost("{id:length(24)}/archive")]
    [AdminAuth]
    public async Task<IActionResult> ArchiveEvent(string id)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        if (!user.IsAdmin.GetValueOrDefault())
            return Forbid();

        var (success, errorMessage) = await _eventService.ArchiveAsync(id, user.DiscordId);

        if (!success)
            return BadRequest(errorMessage);

        return Ok();
    }

    [HttpPost("{id:length(24)}/restore")]
    [AdminAuth]
    public async Task<IActionResult> RestoreEvent(string id)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        if (!user.IsAdmin.GetValueOrDefault())
            return Forbid();

        var (success, errorMessage) = await _eventService.RestoreAsync(id);

        if (!success)
            return BadRequest(errorMessage);

        return Ok();
    }

    [HttpPost("{id:length(24)}/extend")]
    [AdminAuth]
    public async Task<ActionResult<EventDto>> ExtendEvent(string id, [FromBody] ExtendEventRequest request)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        if (!user.IsAdmin.GetValueOrDefault())
            return Forbid();

        var (updatedEvent, errorMessage) = await _eventService.ExtendEventAsync(id, request.Count);

        if (updatedEvent == null)
            return BadRequest(errorMessage);

        return Ok(EventMapper.ToDto(updatedEvent));
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<EventDto>> GetEntity(string id)
    {
        var entity = await _eventService.GetAsync(id);

        if (entity is null)
            return NotFound();

        return EventMapper.ToDto(entity);
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> CreateEntity(EventDto entity)
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is null)
            return BadRequest("Member not found for the current user");

        entity.AuthorId = member.Id;
        entity.Organizer = member.PlayerName;

        await _eventService.CreateAsync(EventMapper.ToEntity(entity));
        return CreatedAtAction(nameof(CreateEntity), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult<EventDto>> UpdateEntity(string id, EventDto updatedEntity)
    {
        Logger.LogInformation("Updating entity with id: {id}", id);

        await _eventService.UpdateAsync(id, EventMapper.ToEntity(updatedEntity));

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<ActionResult<EventDto>> DeleteEntity(string id)
    {
        var entity = await _eventService.GetAsync(id);

        if (entity is null)
            return NotFound();

        await _eventService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost]
    [Route("{id}/signup")]
    public async Task<IActionResult> SignupForEvent(string id, [FromBody] EventSignupDto signup)
    {
        var fcEvent = await _eventService.GetAsync(id);

        if (fcEvent is null)
            return NotFound();

        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is null)
            return BadRequest("Member not found for the current user");

        // Determine target occurrence: prefer next upcoming scheduled occurrence, fall back to the first; create if none exists
        var occurrence = fcEvent.Occurrences
                             ?.Where(o => o.Status == OccurrenceStatus.Scheduled && o.OccurrenceDate >= DateTime.UtcNow)
                             .OrderBy(o => o.OccurrenceDate)
                             .FirstOrDefault()
                         ?? fcEvent.Occurrences?.FirstOrDefault();

        if (occurrence == null)
        {
            occurrence = new EventOccurrence
            {
                OccurrenceDate = fcEvent.StartDate,
                Status = OccurrenceStatus.Scheduled
            };
            fcEvent.Occurrences ??= new List<EventOccurrence>();
            fcEvent.Occurrences.Add(occurrence);
        }

        occurrence.Signups ??= new List<DbEventSignup>();

        // Check if the member is already signed up for this occurrence
        var existing = occurrence.Signups.FirstOrDefault(x => x.DiscordUserId == member.DiscordId);
        if (existing != null)
            // Update roles for existing signup
            existing.Roles = signup.Roles;
        else
            occurrence.Signups.Add(new DbEventSignup
            {
                DiscordUserId = member.DiscordId,
                Roles = signup.Roles,
                SignupDate = DateTime.UtcNow
            });

        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

        return Ok();
    }

    [HttpPost]
    [Route("{eventId}/occurrences/{occurrenceId}/signup")]
    public async Task<IActionResult> SignupForOccurrence(string eventId, string occurrenceId,
        [FromBody] EventSignupDto signup)
    {
        var fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent is null)
            return NotFound("Event not found");

        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is null)
            return BadRequest("Member not found for the current user");

        var occurrence = fcEvent.Occurrences?.FirstOrDefault(o => o.Id == occurrenceId);
        if (occurrence == null)
            return NotFound("Occurrence not found");

        // Check if occurrence is available for signup
        if (occurrence.Status != OccurrenceStatus.Scheduled)
            return BadRequest("Cannot sign up for non-scheduled occurrence");

        if (occurrence.OccurrenceDate < DateTime.UtcNow)
            return BadRequest("Cannot sign up for past occurrence");

        // For LockedGroup, propagate signup to ALL occurrences
        if (fcEvent.SignupType == SignupType.LockedGroup)
        {
            foreach (var occ in fcEvent.Occurrences ?? new List<EventOccurrence>())
            {
                occ.Signups ??= new List<DbEventSignup>();

                var existing = occ.Signups.FirstOrDefault(x => x.DiscordUserId == member.DiscordId);
                if (existing != null)
                    existing.Roles = signup.Roles;
                else
                    occ.Signups.Add(new DbEventSignup
                    {
                        DiscordUserId = member.DiscordId,
                        Roles = signup.Roles,
                        SignupDate = DateTime.UtcNow
                    });
            }
        }
        else
        {
            // For other types, only update the specified occurrence
            occurrence.Signups ??= new List<DbEventSignup>();

            var existing = occurrence.Signups.FirstOrDefault(x => x.DiscordUserId == member.DiscordId);
            if (existing != null)
                existing.Roles = signup.Roles;
            else
                occurrence.Signups.Add(new DbEventSignup
                {
                    DiscordUserId = member.DiscordId,
                    Roles = signup.Roles,
                    SignupDate = DateTime.UtcNow
                });
        }

        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

        return Ok();
    }

    [HttpDelete]
    [Route("{eventId}/occurrences/{occurrenceId}/signup")]
    public async Task<IActionResult> CancelSignupForOccurrence(string eventId, string occurrenceId)
    {
        var fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent is null)
            return NotFound("Event not found");

        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is null)
            return BadRequest("Member not found for the current user");

        var occurrence = fcEvent.Occurrences?.FirstOrDefault(o => o.Id == occurrenceId);
        if (occurrence == null)
            return NotFound("Occurrence not found");

        // For LockedGroup, remove signup from ALL occurrences
        if (fcEvent.SignupType == SignupType.LockedGroup)
        {
            foreach (var occ in fcEvent.Occurrences ?? new List<EventOccurrence>())
            {
                occ.Signups ??= new List<DbEventSignup>();
                var signup = occ.Signups.FirstOrDefault(s => s.DiscordUserId == member.DiscordId);
                if (signup != null)
                    occ.Signups.Remove(signup);
            }
        }
        else
        {
            occurrence.Signups ??= new List<DbEventSignup>();
            var signup = occurrence.Signups.FirstOrDefault(s => s.DiscordUserId == member.DiscordId);
            if (signup != null)
                occurrence.Signups.Remove(signup);
        }

        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

        return Ok();
    }

    [HttpPost]
    [Route("{eventId}/occurrences/{occurrenceId}/participants")]
    [AdminAuth]
    public async Task<IActionResult> SelectParticipants(string eventId, string occurrenceId,
        [FromBody] List<EventParticipant> participants)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        if (!user.IsAdmin.GetValueOrDefault())
            return Forbid();

        var fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent is null)
            return NotFound("Event not found");

        var occurrence = fcEvent.Occurrences?.FirstOrDefault(o => o.Id == occurrenceId);
        if (occurrence == null)
            return NotFound("Occurrence not found");

        // Set selection date for all participants
        foreach (var participant in participants) participant.SelectionDate = DateTime.UtcNow;

        // For LockedGroup, copy participants to ALL occurrences
        if (fcEvent.SignupType == SignupType.LockedGroup)
            foreach (var occ in fcEvent.Occurrences ?? new List<EventOccurrence>())
                occ.Participants = participants.Select(p => new EventParticipant
                {
                    DiscordUserId = p.DiscordUserId,
                    Role = p.Role,
                    SelectionDate = p.SelectionDate
                }).ToList();
        else
            // For other types, only update the specified occurrence
            occurrence.Participants = participants;

        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

        // Post the roster to Discord
        await _discordMessageService.PostInUpcomingRosterChannelAsync(fcEvent.CreateUpcomingRosterMessage());

        return Ok();
    }

    [HttpDelete]
    [Route("{eventId}/occurrences/{occurrenceId}/participants/{userId}")]
    [AdminAuth]
    public async Task<IActionResult> RemoveParticipant(string eventId, string occurrenceId, string userId)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        if (!user.IsAdmin.GetValueOrDefault())
            return Forbid();

        var fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent is null)
            return NotFound("Event not found");

        var occurrence = fcEvent.Occurrences?.FirstOrDefault(o => o.Id == occurrenceId);
        if (occurrence == null)
            return NotFound("Occurrence not found");

        occurrence.Participants ??= new List<EventParticipant>();

        var participant = occurrence.Participants.FirstOrDefault(p => p.DiscordUserId == userId);
        if (participant != null)
        {
            occurrence.Participants.Remove(participant);
            await _eventService.UpdateAsync(fcEvent.Id, fcEvent);
        }

        return Ok();
    }

    [HttpPatch]
    [Route("{eventId}/occurrences/{occurrenceId}/status")]
    [AdminAuth]
    public async Task<IActionResult> UpdateOccurrenceStatus(string eventId, string occurrenceId,
        [FromBody] OccurrenceStatusUpdateDto statusUpdate)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        if (!user.IsAdmin.GetValueOrDefault())
            return Forbid();

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

            if (!occurences.Any())
                Console.WriteLine("test");
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
    public async Task<IActionResult> CancelOccurrence(string eventId, string occurrenceId)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        if (!user.IsAdmin.GetValueOrDefault())
            return Forbid();

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

    [HttpPost]
    [Route("{id:length(24)}/plan")]
    [AdminAuth]
    public async Task<IActionResult> PlanEvent(string id, Event eventDto)
    {
        var user = await _currentMemberAccessor.GetCurrentAsync();
        if (user is null)
            return BadRequest("User not found for the current user");

        var fcEvent = await _eventService.GetAsync(id);
        if (fcEvent is null)
            return NotFound();

        if (!user.IsAdmin.GetValueOrDefault())
            return Forbid();

        // Save the list of participants and post the message to the upcoming roster channel
        await _eventService.UpdateAsync(fcEvent.Id, eventDto);
        await _discordMessageService.PostInUpcomingRosterChannelAsync(eventDto.CreateUpcomingRosterMessage());

        return Ok();
    }

    [HttpGet]
    [Route("retrieve/{id}.ics")]
    public async Task<IActionResult> GetEventIcal(string id)
    {
        try
        {
            // The id here is a Discord user id. Build a calendar of events the user signed up for or participates in.
            var allEvents = await _eventService.GetAsync();
            if (allEvents is null || !allEvents.Any()) return NotFound();

            var userEvents = allEvents.Where(ev =>
                (ev.Occurrences != null &&
                 ev.Occurrences.Any(oc => oc.Signups != null && oc.Signups.Any(s => s.DiscordUserId == id))) ||
                (ev.Occurrences != null && ev.Occurrences.Any(oc =>
                    oc.Participants != null && oc.Participants.Any(p => p.DiscordUserId == id)))
            ).ToList();

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
            calendar.AddProperty(new CalendarProperty("URL", $"{baseUrl}/api/Events/retrieve/{id}.ics"));

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
            return File(file, "text/calendar; charset=utf-8", $"{id}.ics");
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
}