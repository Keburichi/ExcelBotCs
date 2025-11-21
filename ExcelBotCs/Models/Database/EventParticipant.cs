using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Database;

/// <summary>
///     Represents a selected participant for an event occurrence (embedded subdocument)
/// </summary>
public class EventParticipant
{
    public string DiscordUserId { get; set; }
    public Role Role { get; set; }
    public DateTime SelectionDate { get; set; } = DateTime.UtcNow;
}