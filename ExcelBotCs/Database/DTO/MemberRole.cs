namespace ExcelBotCs.Database.DTO;

public class MemberRole : BaseEntity
{
    public string Name { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsMember { get; set; }
}