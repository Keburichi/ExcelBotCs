namespace ExcelBotCs.Models.DTO;

public class FcMemberDto : BaseDto
{
    public string Name { get; set; }
    public string CharacterId { get; set; }
    public string Title { get; set; }
    public DateTime LastSynchronisation { get; set; }
    public string FcRank { get; set; }
    public string Avatar { get; set; }
    public string Bio { get; set; }
}