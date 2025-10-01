using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Database;

public class EventParticipant
{
    public string DiscordUserId { get; set; }
    public Role Role { get; set; }
}