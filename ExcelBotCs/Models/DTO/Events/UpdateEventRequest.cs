using ExcelBotCs.Models.Database.Events;

namespace ExcelBotCs.Models.DTO.Events;

public class UpdateEventRequest : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public EventType Type { get; set; } = EventType.Other;
    public DateTime StartDate { get; set; } // First occurrence start date

    public int Duration { get; set; } // Duration in minutes

    // iCal source of truth
    public string ICalString { get; set; }

    // Signup configuration
    public SignupType SignupType { get; set; } = SignupType.SingleEvent;

    public string? PictureUrl { get; set; }
    public string? FightId { get; set; }
    public string? Organizer { get; set; }
    public int MaxNumberOfParticipants { get; set; }
}