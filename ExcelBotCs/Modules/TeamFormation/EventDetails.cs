using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.TeamFormation;

public class EventDetails : BaseEntity
{
	public string Name { get; set; }
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public List<EventMemberDetails> Participants { get; set; } = [];
}
