using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Database;

public class EventSignup
{
    public ulong DiscordUserId { get; set; }
    public List<Role> Roles { get; set; }
}