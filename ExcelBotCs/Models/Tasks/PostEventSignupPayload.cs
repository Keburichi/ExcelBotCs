using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Tasks;

public class PostEventSignupPayload
{
    public string EventId { get; set; } = string.Empty;
    public Role Role { get; set; }
    public ulong DiscordUserId { get; set; }
}
