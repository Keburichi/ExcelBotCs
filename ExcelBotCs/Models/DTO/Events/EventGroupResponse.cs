namespace ExcelBotCs.Models.DTO.Events;

public class EventGroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<EventParticipantDto> Participants { get; set; }
}