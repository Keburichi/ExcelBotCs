using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The lottery round engine: one state machine for the scheduled run, the manual run and the admin recovery
///     actions (spec §5.1, §5.3, §5.10). Every operation is idempotent per <c>RoundKey</c> and returns a typed result,
///     so the scheduler and the future admin HTTP layer (#114) share exactly one code path.
/// </summary>
public interface ILotteryRoundRunner
{
	/// <summary>
	///     One scheduler tick. It resumes an unfinished round first, then runs the most recent due slot (catch-up),
	///     recording older missed dates as skipped. It never throws: a failure is recorded on the round and returned.
	///     Honours <c>Enabled</c>, <c>PauseAutomation</c> and <c>DryRun</c>.
	/// </summary>
	Task<LotteryTickResult> TickAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     The admin "Run now" escape hatch, on the live path with a real actor. It runs the most recent due round — the
	///     draw that should have run — when that round has not run yet, so a missed or never-scheduled draw can be
	///     started by hand. A voided due round is re-opened and drawn again (spec §5.1 "Redo"), with its ledger keys
	///     scoped to the new attempt; an already completed or skipped due round returns "already run"; a pending or
	///     failed round returns "blocked" (retry or void first). It is not affected by the kill switch or the dry-run
	///     flag.
	/// </summary>
	Task<LotteryRunResult> RunNowAsync(ulong actorDiscordId, CancellationToken cancellationToken = default);

	/// <summary>
	///     Renders and posts a dry-run preview of what the next draw would look like. It persists nothing: no round, no
	///     entries, no winners, no ledger, no flush and no pot change (spec §13 criterion 20).
	/// </summary>
	Task<LotteryRunResult> PreviewAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Resumes the newest unfinished round: an interrupted draw, a pending announcement, a missed cleanup, or a
	///     terminal failure an admin re-arms. It never draws a second number for a round that already has one.
	/// </summary>
	Task<LotteryRunResult> RetryAsync(ulong? actorDiscordId, CancellationToken cancellationToken = default);

	/// <summary>
	///     Skips a round: no draw, no pot change, guesses carry and the weekly allowance is not reset. Skips the due
	///     slot when it has not run yet, otherwise the next slot.
	/// </summary>
	Task<LotteryRunResult> SkipAsync(ulong actorDiscordId, string? reason = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Voids a drawn/announced/completed/failed round: appends audited reversal corrections for the round's ledger
	///     entries, annotates the round and the post, and never silently re-draws the date.
	/// </summary>
	Task<LotteryRunResult> VoidAsync(string roundKey, ulong actorDiscordId, string reason,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Moves one scheduled round to a new instant; the round key follows the local date and the guesses stay.
	///     <paramref name="roundKey" /> defaults to the round that would run next.
	/// </summary>
	Task<LotteryRunResult> RescheduleAsync(DateTime newScheduledAtUtc, ulong actorDiscordId,
		string? roundKey = null, CancellationToken cancellationToken = default);

	/// <summary>The admin/health read model: automation state, pot, last round, blocking round and the next run.</summary>
	Task<LotteryStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     The current pot derived from the ledger — the newest <c>BalanceAfter</c>, or the base pot when the ledger is
	///     empty. It never writes: the opening balance is a manual seed (spec §6 "no auto set").
	/// </summary>
	Task<long> GetPotAsync(CancellationToken cancellationToken = default);
}
