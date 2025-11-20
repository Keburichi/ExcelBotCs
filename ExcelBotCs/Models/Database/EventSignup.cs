using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Database;

public class EventUserSignup
{
    public string DiscordUserId { get; set; }
    public List<Role> Roles { get; set; }

    /// <summary>
    ///     For IndependentSignups type: which occurrence this signup is for
    ///     Null for SingleEvent and LockedGroup types
    /// </summary>
    public DateTime? OccurrenceDate { get; set; }
}