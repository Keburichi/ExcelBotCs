using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Database;

public class EventParticipant
{
    public ulong DiscordUserId { get; set; }
    public Role Role { get; set; }
}