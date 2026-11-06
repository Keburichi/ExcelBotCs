using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

public class LotteryRound : BaseEntity
{
	/// <summary>
	///     The round's scheduled run date in Europe/Berlin, formatted <c>YYYY-MM-DD</c>. Unique and used as the
	///     foreign key by every other lottery collection.
	/// </summary>
	public string RoundKey { get; set; } = string.Empty;

	public DateTime ScheduledAt { get; set; }
	public DateTime? RunAt { get; set; }
	public string Status { get; set; } = LotteryRoundStatus.Scheduled;

	/// <summary>One of <see cref="LotteryFailedStep" />; set when a run fails.</summary>
	public string? FailedStep { get; set; }

	public int Attempts { get; set; }
	public DateTime? NextRetryAt { get; set; }
	public string? LastError { get; set; }
	public int? WinningNumber { get; set; }
	public long? PotBefore { get; set; }
	public long? PotAfter { get; set; }
	public string? AnnouncementMessageId { get; set; }

	/// <summary>Set while the announcement is being posted; the reconciliation marker.</summary>
	public DateTime? PostingStartedAt { get; set; }

	public DateTime? ClaimedAt { get; set; }
	public string? ClaimOwner { get; set; }
}
