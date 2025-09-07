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
	[Route("retrieve/{id}.ics")]
	public async Task<IActionResult> GetEvent(string id)
	{
		try
		{
			var discordId = ulong.Parse(id);
			var joinedEvents = await _eventDetails.Where(e => e.Participants.Any(p => p.DiscordId == discordId)).ToListAsync();

			var ics =
				"BEGIN:VCALENDAR\n" +
				"VERSION:2.0\n" +
				"PRODID:Excelsior Events\n" +
				"CALSCALE:GREGORIAN\n" +
				$"URL:{_rootUrl}event/retrieve/{id}\n" +
				"BEGIN:VTIMEZONE\n" +
				"TZID:Etc/UTC\n" +
				"END:VTIMEZONE\n" +
				"REFRESH-INTERVAL;VALUE=DURATION:PT6H\n" +
				"X-PUBLISHED-TTL:PT6H\n" +
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
				$"DTSTART:{e.StartTime:yyyyMMddTHHmm00}\n" +
				$"DTEND:{e.EndTime:yyyyMMddTHHmm00}\n" +
				$"SUMMARY:{e.Name}\n" +
				"LOCATION:Final Fantasy XIV Online\n" +
				"END:VEVENT\n";
		}
	}
}