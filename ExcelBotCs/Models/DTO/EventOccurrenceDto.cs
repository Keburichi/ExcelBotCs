using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO;

public class EventOccurrenceDto
{
    public string Id { get; set; }
    public DateTime OccurrenceDate { get; set; }
    public OccurrenceStatus Status { get; set; }
    public string? DiscordMessageId { get; set; }
    public List<EventSignupDto> Signups { get; set; } = new();
    public List<EventParticipantDto> Participants { get; set; } = new();
}