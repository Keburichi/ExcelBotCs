using ExcelBotCs.Data;

namespace ExcelBotCs.Modules.TeamFormation;

public class EventDetails : DatabaseObject
{
	public string Name { get; set; }
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public List<EventMemberDetails> Participants { get; set; } = [];
}