namespace ExcelBotCs.Models.DTO.Events;

public class EventGroupRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public List<EventParticipantDto> Participants { get; set; }
}