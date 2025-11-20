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

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : AuthorizedController, IBaseCrudController<EventDto>
{
    private readonly ICurrentMemberAccessor _currentMemberAccessor;
    private readonly IDiscordMessageService _discordMessageService;
    private readonly IEventService _eventService;
    private readonly string _rootUrl;

    public EventsController(ILogger<EventsController> logger, IEventService eventService,
        ICurrentMemberAccessor currentMemberAccessor, IDiscordMessageService discordMessageService) : base(logger)
    {
        _eventService = eventService;
        _currentMemberAccessor = currentMemberAccessor;
        _discordMessageService = discordMessageService;
        _rootUrl = Utils.GetEnvVar("EVENT_ENDPOINT_URL", nameof(TeamFormationInteraction));
    }

    [HttpGet]
    public async Task<ActionResult<List<EventDto>>> GetEntities()
    {
        var entities = await _eventService.GetAsync();

        if (entities is null)
            return new List<EventDto>();

        var dtos = entities.Select(EventMapper.ToDto).ToList();

        return dtos;
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
    public async Task<IActionResult> SignupForEvent(string id, [FromBody] EventSignup signup)
    {
        var fcEvent = await _eventService.GetAsync(id);

        if (fcEvent is null)
            return NotFound();

        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is null)
            return BadRequest("Member not found for the current user");

        // Check if the member is already signed up for the event
        if (fcEvent.Signups.Any(x => x.DiscordUserId == member.DiscordId))
        {
            // Update the signup. That means we remove the role if its present or add it if its not
            var eventSignup = fcEvent.Signups.First(x => x.DiscordUserId == member.DiscordId);
            if (eventSignup.Roles.Contains(signup.Role))
                eventSignup.Roles.Remove(signup.Role);
            else
                eventSignup.Roles.Add(signup.Role);
        }
        else
        {
            fcEvent.Signups.Add(new EventUserSignup
            {
                DiscordUserId = member.DiscordId,
                Roles = [signup.Role]
            });
        }

        await _eventService.UpdateAsync(fcEvent.Id, fcEvent);

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
                (ev.Signups != null && ev.Signups.Any(s => s.DiscordUserId == id)) ||
                (ev.Participants != null && ev.Participants.Any(p => p.DiscordUserId == id))
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