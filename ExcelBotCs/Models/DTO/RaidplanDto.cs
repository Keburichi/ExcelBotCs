namespace ExcelBotCs.Models.DTO;

public class RaidplanDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public MemberDto Author { get; set; }
}