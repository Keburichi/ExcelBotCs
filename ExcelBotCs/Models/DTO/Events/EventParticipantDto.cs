using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.DTO.Events;

public class EventParticipantDto
{
    public string DiscordUserId { get; set; }
    public Role? Role { get; set; }
    public DateTime SelectionDate { get; set; }
}