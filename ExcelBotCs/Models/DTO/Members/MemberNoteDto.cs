namespace ExcelBotCs.Models.DTO.Members;

public class NoteResponse : BaseDto
{
    public string Note { get; set; }
    public string Author { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
}