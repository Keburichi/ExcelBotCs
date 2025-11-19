using ExcelBotCs.Extensions;

namespace ExcelBotCs.Models.Database;

public class Event : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string DiscordMessageId { get; set; }
    public string? PictureUrl { get; set; }
    public EventType Type { get; set; } = EventType.Other;
    public string? FightId { get; set; }
    public List<EventParticipant> Participants { get; set; } = [];
    public List<EventUserSignup> Signups { get; set; } = [];
    public string? AuthorId { get; set; }
    public string? Organizer { get; set; }
    public DateTime StartDate { get; set; }
    public int Duration { get; set; }
    public int MaxNumberOfParticipants { get; set; }

    public bool AvailableForSignup
    {
        get
        {
            // A event is available for signup if no participants have signed up yet, and the event is in the future
            if (!Participants.IsNullOrEmpty())
                return false;

            if (StartDate < DateTime.UtcNow)
                return false;

            return true;
        }
    }
}