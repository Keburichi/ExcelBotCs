using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Models.DTO.Events;

public class UpdateOccurrenceStatusRequest
{
    public OccurrenceStatus Status { get; set; }
}