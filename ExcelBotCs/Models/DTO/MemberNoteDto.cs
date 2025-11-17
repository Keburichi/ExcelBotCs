namespace ExcelBotCs.Models.DTO;

public class MemberNoteDto : BaseDto
{
    public string Note { get; set; }
    public string Author { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime EditDate { get; set; }
}

public class AddNoteRequest
{
    public string Note { get; set; }
}

public class UpdateNoteRequest
{
    public string Note { get; set; }
}