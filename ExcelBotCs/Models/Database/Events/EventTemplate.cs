namespace ExcelBotCs.Models.Database.Events;

public class EventTemplate : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public EventType Type { get; set; } = EventType.Other;
    public DayOfWeek DayOfWeek { get; set; }
    public int TimeOfDayMinutes { get; set; }
    public int Duration { get; set; }
    public string Organizer { get; set; }
    public int MaxNumberOfParticipants { get; set; }
    public List<SignupButtonConfig>? SignupButtonConfigs { get; set; }
}
