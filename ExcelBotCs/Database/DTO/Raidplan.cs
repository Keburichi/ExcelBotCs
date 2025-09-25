namespace ExcelBotCs.Database.DTO;

public class Raidplan : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public Member Author { get; set; }
}