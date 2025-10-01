namespace ExcelBotCs.Models.Database;

public class MemberRole : BaseEntity
{
    public ulong DiscordId { get; set; }
    public string Name { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsMember { get; set; }
}