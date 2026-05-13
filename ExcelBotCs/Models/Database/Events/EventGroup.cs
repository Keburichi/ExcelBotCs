namespace ExcelBotCs.Models.Database.Events;

public class EventGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<EventParticipant> Participants { get; set; }
}