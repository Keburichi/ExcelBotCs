namespace ExcelBotCs.Database.DTO;

public class Event : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string DiscordMessage { get; set; }
    public string? PictureUrl { get; set; }
    public Member? Author { get; set; }
    public string? Organizer => Author?.PlayerName;
    public DateTime StartDate { get; set; }
    public int Duration { get; set; }
}