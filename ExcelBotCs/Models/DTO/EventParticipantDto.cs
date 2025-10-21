using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.DTO;

public class EventParticipantDto
{
    public string DiscordUserId { get; set; }
    public Role Role { get; set; }
}