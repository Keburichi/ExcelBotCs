using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Services.Lottery.Records;

/// <summary>
///     Everything the announcement needs about a round: the round itself (winning number, pot, key), the winner rows
///     with their shares, and whether this is a dry-run preview. #111 turns this into the real post with templates,
///     the role ping, the last five and the history link.
/// </summary>
public sealed record LotteryAnnouncement(
	LotteryRound Round,
	IReadOnlyList<LotteryWinner> Winners,
	long PotBefore,
	long PotAfter,
	bool DryRun);

/// <summary>
///     The published announcement. <see cref="MessageId" /> stays <c>null</c> until #111 adds a Discord post method
///     that returns the message; the round still records <c>announced</c> so the state machine is complete.
/// </summary>
public sealed record LotteryAnnouncementPost(string? MessageId);

/// <summary>The possible values of <see cref="LotteryRunResult.Disposition" />; the admin API maps these to responses.</summary>
public static class LotteryRunDisposition
{
	/// <summary>The round ran to <c>completed</c> (drawn, announced and cleaned up).</summary>
	public const string Ran = "ran";

	/// <summary>The draw is saved but the announcement is still pending (it will be retried).</summary>
	public const string Drawn = "drawn";

	/// <summary>The round is claimed by another attempt or is still being retried; nothing was drawn again.</summary>
	public const string AlreadyRunning = "already-running";

	/// <summary>This round already ran, was skipped or was voided.</summary>
	public const string AlreadyRun = "already-run";

	/// <summary>A pending/failed round blocks the action; retry or void it first.</summary>
	public const string Blocked = "blocked";

	/// <summary>The round was marked skipped; guesses carry and the pot did not move.</summary>
	public const string Skipped = "skipped";

	/// <summary>The round was voided and its ledger entries were reversed.</summary>
	public const string Voided = "voided";

	/// <summary>The round was moved to a new date; its round key followed.</summary>
	public const string Rescheduled = "rescheduled";

	/// <summary>No round came due (or the first run is not due yet).</summary>
	public const string NothingDue = "nothing-due";

	/// <summary>Automation is disabled by deployment config.</summary>
	public const string Disabled = "disabled";

	/// <summary>Automation is paused by the stored settings.</summary>
	public const string Paused = "paused";

	/// <summary>A dry run preview was posted; nothing was persisted.</summary>
	public const string DryRun = "preview";

	/// <summary>A step failed but a retry is scheduled; the round is still in flight.</summary>
	public const string Retrying = "retrying";

	/// <summary>The action failed; the round records the failed step, attempts and next retry.</summary>
	public const string Failed = "failed";

	/// <summary>The round or another target does not exist.</summary>
	public const string NotFound = "not-found";

	/// <summary>The action is not allowed in the round's current state.</summary>
	public const string NotAllowed = "not-allowed";

	/// <summary>Whether the call achieved what it was asked to do.</summary>
	public static bool IsSuccess(string disposition)
	{
		return disposition is Ran or Drawn or Skipped or Voided or Rescheduled or DryRun or NothingDue;
	}
}

/// <summary>The typed outcome of a run/retry/skip/void/reschedule action, ready for the admin HTTP layer (#114).</summary>
public sealed record LotteryRunResult(
	string Disposition,
	string Message,
	LotteryRound? Round = null,
	long? Pot = null)
{
	public bool Success => LotteryRunDisposition.IsSuccess(Disposition);

	public string? RoundKey => Round?.RoundKey;
}

/// <summary>The typed outcome of one scheduler tick.</summary>
public sealed record LotteryTickResult(string Action, string Message, LotteryRound? Round = null, long? Pot = null);

/// <summary>
///     The read model the admin page (#114) and the health check (#115) use: automation state, current pot, the last
///     round, the blocking round and the next scheduled run.
/// </summary>
public sealed record LotteryStatusSnapshot(
	string AutomationStatus,
	bool SchedulerEnabled,
	long Pot,
	LotteryRound? LatestRound,
	LotteryRound? PendingRound,
	DateTime? NextRunAtUtc,
	string? NextRoundKey,
	string? LastError)
{
	/// <summary>Degraded while an unfinished round exists; the health check reports it without failing the host.</summary>
	public bool IsDegraded => PendingRound is not null;
}
