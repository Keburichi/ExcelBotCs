namespace ExcelBotCs.Database.DTO;

public class MemberNote : BaseEntity
{
    public string Note { get; set; }
    public Member Author { get; set; }
}