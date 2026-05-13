using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO.Events;

public class EventOccurrenceDto
{
    public string Id { get; set; }
    public DateTime OccurrenceDate { get; set; }
    public OccurrenceStatus Status { get; set; }
}