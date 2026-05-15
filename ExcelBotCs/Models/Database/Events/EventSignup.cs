using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Models.Database.Events;

/// <summary>
///     Represents a user signup for an event occurrence (embedded subdocument)
/// </summary>
public class EventSignup
{
    public string DiscordUserId { get; set; }
    public List<Role> Roles { get; set; }
    public List<string>? SignupSlugs { get; set; }
    public DateTime SignupDate { get; set; } = DateTime.UtcNow;
}