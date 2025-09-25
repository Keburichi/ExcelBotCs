using System.Text;
using ExcelBotCs.Database.DTO;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : BaseCrudController<Event>
{
    private readonly EventService _eventService;
    private readonly ICurrentMemberAccessor _currentMemberAccessor;
    private readonly string _rootUrl;

    public EventsController(ILogger<EventsController> logger, EventService eventService,
        ICurrentMemberAccessor currentMemberAccessor) : base(logger, eventService)
    {
        _eventService = eventService;
        _currentMemberAccessor = currentMemberAccessor;
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