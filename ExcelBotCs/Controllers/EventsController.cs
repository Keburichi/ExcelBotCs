using System.Text;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services;
using ExcelBotCs.Services.Discord.Interfaces;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : BaseCrudController<Event>
{
    private readonly EventService _eventService;
    private readonly ICurrentMemberAccessor _currentMemberAccessor;
    private readonly IDiscordMessageService _discordMessageService;
    private readonly string _rootUrl;

    public EventsController(ILogger<EventsController> logger, EventService eventService,
        ICurrentMemberAccessor currentMemberAccessor, IDiscordMessageService discordMessageService) : base(logger,
        eventService)
    {
        _eventService = eventService;
        _currentMemberAccessor = currentMemberAccessor;
        _discordMessageService = discordMessageService;
        _rootUrl = Utils.GetEnvVar("EVENT_ENDPOINT_URL", nameof(TeamFormationInteraction));
    }

    protected override async Task<ActionResult<Event>?> OnBeforePostAsync(Event entity)
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is null)
            return BadRequest("Member not found for the current user");

        entity.Author = member;
        return null;
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
            {
                eventSignup.Roles.Remove(signup.Role);
            }
            else
            {
                eventSignup.Roles.Add(signup.Role);
            }
        }
        else
        {
            fcEvent.Signups.Add(new EventUserSignup()
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
    [Authorize(Roles = "Admin")]
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
    [Route("retrieve/{id:int}.ics")]
    public async Task<IActionResult> GetEventIcal(string id)
    {
        try
        {
            var userEvents = new List<Event>();

            var calendar = new Calendar();
            calendar.Name = "Excelsior Events Calendar";
            calendar.Method = CalendarMethods.Publish;
            calendar.ProductId = "-//Excelsior//Excelsior Events Calendar//EN";
            calendar.Version = "2.0";
            calendar.Scale = CalendarScales.Gregorian;
            calendar.AddProperty(new CalendarProperty("URL", $"https://{_rootUrl}event/retrieve/{id}.ics"));

            foreach (var userEvent in userEvents)
            {
                calendar.Events.Add(CreateCalendarEvent(userEvent));
            }

            var calendarSerializer = new CalendarSerializer(calendar);
            calendarSerializer.SerializeToString();

            var file = Encoding.UTF8.GetBytes(calendarSerializer.SerializeToString());
            return File(file, "text/calendar", $"{id}.ics");
        }
        catch (Exception e)
        {
            return BadRequest("Event is malformed");
        }
    }

    private CalendarEvent CreateCalendarEvent(Event fcEvent)
    {
        return new CalendarEvent
        {
            Location = "Final Fantasy XIV Online",
            Status = EventStatus.Confirmed,
            Summary = fcEvent.Name,
            Description = fcEvent.Description,
            Start = new CalDateTime(fcEvent.StartDate, TimeZoneInfo.Utc.Id),
            Duration = Duration.FromMinutes(fcEvent.Duration)
        };
    }
}