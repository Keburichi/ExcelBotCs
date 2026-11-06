using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

/// <summary>One settings change, kept for the admin audit trail.</summary>
public class LotterySettingsHistory : BaseEntity
{
	public string SettingsKey { get; set; } = LotterySettings.SingletonKey;
	public int Version { get; set; }
	public ulong? ActorDiscordId { get; set; }
	public DateTime ChangedAt { get; set; }

	/// <summary>The changed fields and their previous values, keyed by property name.</summary>
	public Dictionary<string, object?> OldValues { get; set; } = new();

	/// <summary>The changed fields and their new values, keyed by property name.</summary>
	public Dictionary<string, object?> NewValues { get; set; } = new();
}
