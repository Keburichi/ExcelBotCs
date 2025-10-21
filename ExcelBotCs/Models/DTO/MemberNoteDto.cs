namespace ExcelBotCs.Models.DTO;

public class MemberNoteDto : BaseDto
{
    public string Note { get; set; }
    public MemberDto Author { get; set; }
}