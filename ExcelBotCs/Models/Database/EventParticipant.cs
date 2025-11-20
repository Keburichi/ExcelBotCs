using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Database;

public class EventParticipant
{
    public string DiscordUserId { get; set; }
    public Role Role { get; set; }

    /// <summary>
    ///     For IndependentSignups type: which occurrence this participant is assigned to
    ///     Null for SingleEvent and LockedGroup types (same participants for all occurrences)
    /// </summary>
    public DateTime? OccurrenceDate { get; set; }
}