using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Database.Events;

/// <summary>
///     Represents a selected participant for an event
/// </summary>
public class EventParticipant
{
    public string DiscordUserId { get; set; }
    public Role Role { get; set; }
    public DateTime SelectionDate { get; set; } = DateTime.UtcNow;
}