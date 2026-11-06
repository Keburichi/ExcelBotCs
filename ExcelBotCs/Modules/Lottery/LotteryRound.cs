using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

public class LotteryRound : BaseEntity
{
	/// <summary>
	///     The round's scheduled run date in the run timezone (Europe/Berlin by default), formatted
	///     <c>YYYY-MM-DD</c>. Unique and used as the foreign key by every other lottery collection.
	/// </summary>
	public string RoundKey { get; set; } = string.Empty;

	public DateTime ScheduledAt { get; set; }
	public DateTime? RunAt { get; set; }
	public string Status { get; set; } = LotteryRoundStatus.Scheduled;

	/// <summary>One of <see cref="LotteryFailedStep" />; set when a run fails.</summary>
	public string? FailedStep { get; set; }

	/// <summary>Consecutive failures of the current step; reset when the step succeeds or an admin reopens the round.</summary>
	public int Attempts { get; set; }

	public DateTime? NextRetryAt { get; set; }
	public string? LastError { get; set; }

	/// <summary>
	///     The end of the announcement retry window (the draw instant plus one hour, per §5.10). An admin retry
	///     re-arms it; the scheduler marks the round failed when it passes without a successful post.
	/// </summary>
	public DateTime? RetryUntil { get; set; }

	public int? WinningNumber { get; set; }
	public long? PotBefore { get; set; }
	public long? PotAfter { get; set; }
	public string? AnnouncementMessageId { get; set; }

	/// <summary>Set while the announcement is being posted; the reconciliation marker.</summary>
	public DateTime? PostingStartedAt { get; set; }

	public DateTime? ClaimedAt { get; set; }
	public string? ClaimOwner { get; set; }

	/// <summary>Why the round was voided, kept next to the actor and the time for the admin audit view.</summary>
	public string? VoidReason { get; set; }

	public DateTime? VoidedAt { get; set; }
	public ulong? VoidedByDiscordId { get; set; }
}
