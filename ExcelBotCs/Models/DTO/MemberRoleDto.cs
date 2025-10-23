namespace ExcelBotCs.Models.DTO;

public class MemberRoleDto : BaseDto
{
    public string DiscordId { get; set; }
    public string Name { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsMember { get; set; }
}