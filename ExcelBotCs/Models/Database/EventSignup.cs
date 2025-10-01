using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Database;

public class EventSignup
{
    public string DiscordUserId { get; set; }
    public List<Role> Roles { get; set; }
}