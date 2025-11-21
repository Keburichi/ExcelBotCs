using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.DTO;

public class EventSignupDto
{
    public string DiscordUserId { get; set; }
    public List<Role> Roles { get; set; }
    public DateTime SignupDate { get; set; }
}