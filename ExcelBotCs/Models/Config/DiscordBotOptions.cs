using System.ComponentModel.DataAnnotations;
using ExcelBotCs.Attributes;

namespace ExcelBotCs.Models.Config;

[OptionsSection("DiscordBot")]
public class DiscordBotOptions : IValidatableObject
{
    /// <summary>
    /// When false the bot never connects to the Discord gateway, never registers slash commands and never
    /// mirrors console output into the log channel. Parallel checkouts (git worktrees) run with this off so
    /// only one process owns the bot; exactly one checkout per token may leave it enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// When false the bot may still connect and handle interactions, but it does not re-register global
    /// slash commands on ready. Registration replaces the global command set and deletes commands this
    /// process does not know about, so it must belong to a single designated process.
    /// </summary>
    public bool RegisterCommands { get; set; } = true;

    // Deliberately not [Required]: the token must only be present when the bot is enabled. That
    // conditional rule lives in Validate so the generic options validation in ConfigExtensions can run
    // unchanged (a botless checkout does not need a token at all).
    public string Token { get; set; } = string.Empty;

    [Required] public ulong GuildId { get; set; }

    [Required] public ulong LotteryChannel { get; set; }

    [Required] public ulong AnnouncementChannel { get; set; }

    [Required] public ulong EventsChannel { get; set; }

    [Required] public ulong UpcomingRosterChannel { get; set; }

    [Required] public ulong HallOfClearsChannel { get; set; }

    [Required] public ulong LogChannel { get; set; }

    public List<ulong> AdminRoleIds { get; set; } = [];
    public List<ulong> MemberRoleIds { get; set; } = [];
    public List<ulong> FriendOfFcRoleIds { get; set; } = [];

    public TimeSpan DisconnectHealthThreshold { get; set; } = TimeSpan.FromMinutes(5);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Enabled && string.IsNullOrWhiteSpace(Token))
            yield return new ValidationResult(
                "DiscordBot:Token is required when DiscordBot:Enabled is true.", [nameof(Token)]);
    }
}
