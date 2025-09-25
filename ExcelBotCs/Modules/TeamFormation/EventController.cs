using ExcelBotCs.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Text;

namespace ExcelBotCs.Modules.TeamFormation;

[ApiController]
[Route("event")]
public class EventController : ControllerBase
{
	private readonly Repository<EventDetails> _eventDetails;
	private readonly string _rootUrl;

	public EventController(Database database)
	{
		_eventDetails = database.GetCollection<EventDetails>("event_details");
		_rootUrl = Utils.GetEnvVar("EVENT_ENDPOINT_URL", nameof(TeamFormationInteraction));
	}

	[HttpGet]
	[Route("calendar/{id}")]
	public IActionResult GetPage(string id)
	{
		var html = $"<!DOCTYPE html><html><body><p>(I promise I'll make this page prettier later! -Zahrymm)</p><a href=\"webcal://{_rootUrl}event/retrieve/{id}.ics\">Subscribe to an auto-updating calendar</a></body></html>";
		return Content(html, "text/html");
	}

	[HttpGet]
	[Route("retrieve/{id}.ics")]
	public async Task<IActionResult> GetEvent(string id)
	{
		try
		{
			var discordId = ulong.Parse(id);
			var joinedEvents = await _eventDetails
				.Where(e => e.EndTime > DateTime.UtcNow && e.Participants.Any(p => p.DiscordId == discordId))
				.ToListAsync();

			var ics =
				"BEGIN:VCALENDAR\n" +
				"VERSION:2.0\n" +
				"PRODID:Excelsior Events\n" +
				"CALSCALE:GREGORIAN\n" +
				$"URL:https://{_rootUrl}event/retrieve/{id}.ics\n" +
				"METHOD:PUBLISH\n" +
				"REFRESH-INTERVAL;VALUE=DURATION:PT6H\n" +
				"X-PUBLISHED-TTL:PT6H\n" +
				"X-WR-CALNAME:Excelsior Events\n" +
				"NAME:Excelsior Events\n" +
				"BEGIN:VTIMEZONE\n" +
				"TZID:Etc/UTC\n" +
				"END:VTIMEZONE\n" +
				string.Join("", joinedEvents.Select(GenerateEvent)) +
				"END:VCALENDAR";

			var bytes = Encoding.UTF8.GetBytes(ics);
			return File(bytes, "text/calendar");
		}
		catch
		{
			return BadRequest("Event is malformed");
		}

		string GenerateEvent(EventDetails e)
		{
			return
				"BEGIN:VEVENT\n" +
				$"UID:{e.StartTime.Ticks}-{e.Name}\n" +
				$"DTSTAMP:{e.DateCreated:yyyyMMddTHHmm00}Z\n" +
				$"DTSTART:{e.StartTime:yyyyMMddTHHmm00}Z\n" +
				$"DTEND:{e.EndTime:yyyyMMddTHHmm00}Z\n" +
				$"SUMMARY:{e.Name}\n" +
				"LOCATION:Final Fantasy XIV Online\n" +
				"STATUS:CONFIRMED\n" +
				"END:VEVENT\n";
		}
	}
}