using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

/// <summary>
///     The round store. Every state transition is a conditional atomic update — the filter names the statuses the
///     transition is valid from — so a second scheduler, a retried step or two racing claims can never draw twice.
///     All <c>DateTime</c> parameters are UTC.
/// </summary>
public interface ILotteryRoundRepository : IBaseRepository<LotteryRound>
{
	Task<LotteryRound?> GetByRoundKeyAsync(string roundKey);

	Task<List<LotteryRound>> GetByStatusAsync(string status);

	/// <summary>The most recently scheduled rounds, newest first.</summary>
	Task<List<LotteryRound>> GetLatestAsync(int limit);

	/// <summary>The newest round that is unfinished (drawing, drawn, announced or failed), if any.</summary>
	Task<LotteryRound?> GetNewestBlockingAsync();

	/// <summary>The newest <c>scheduled</c> round whose scheduled instant has passed, if any (a rescheduled round).</summary>
	Task<LotteryRound?> GetNewestScheduledDueAsync(DateTime nowUtc);

	/// <summary>
	///     The soonest <c>scheduled</c> round whose scheduled instant is still in the future, if any. Rounds are created
	///     lazily, so this is normally <c>null</c>; a hit is a round the admin rescheduled, and the reminder follows its
	///     new instant instead of the configured weekday (§5.7).
	/// </summary>
	Task<LotteryRound?> GetNextScheduledAsync(DateTime nowUtc);

	/// <summary>
	///     Creates the round for a slot as <c>scheduled</c> if it does not exist. The unique <c>RoundKey</c> index
	///     settles two schedulers racing to create it; both callers get the same document.
	/// </summary>
	Task<LotteryRound> EnsureScheduledAsync(string roundKey, DateTime scheduledAtUtc);

	/// <summary>
	///     Claims the round for drawing: moves it to <c>drawing</c> and records the owner and claim instant. The claim
	///     is conditional on the round being claimable — <c>scheduled</c>, an interrupted <c>drawing</c>, or (with
	///     <paramref name="fromFailed" />) a <c>failed</c> draw an admin retried — and on its retry time having come.
	/// </summary>
	/// <param name="ignoreRetry">
	///     An explicit admin retry acts immediately instead of waiting for the recorded retry instant.
	/// </param>
	/// <returns>The round as it was before the claim, or <c>null</c> when it is already running or already run.</returns>
	Task<LotteryRound?> TryClaimForDrawAsync(string roundKey, string claimOwner, DateTime nowUtc, bool fromFailed,
		bool ignoreRetry = false);

	/// <summary>
	///     Stores the drawn number and the pot at the draw. Conditional on the round not having a winning number yet, so
	///     a crash-and-retry re-uses the first draw instead of drawing again.
	/// </summary>
	Task<bool> TrySetWinningNumberAsync(string roundKey, int winningNumber, DateTime runAtUtc, long potBefore);

	/// <summary>Moves <c>drawing</c> to <c>drawn</c>: the result, entries, winners and ledger are all persisted.</summary>
	Task<bool> TryMarkDrawnAsync(string roundKey, long potAfter, DateTime retryUntilUtc);

	/// <summary>Moves <c>drawn</c> to <c>announced</c> with the posted message id (or <c>null</c> until #111 stores it).</summary>
	Task<bool> TryMarkAnnouncedAsync(string roundKey, string? announcementMessageId);

	/// <summary>Moves <c>announced</c> to <c>completed</c>; live guesses have been flushed.</summary>
	Task<bool> TryMarkCompletedAsync(string roundKey);

	/// <summary>Writes the posting reconciliation marker before the Discord send (§5.10).</summary>
	Task<bool> TryStartPostingAsync(string roundKey, DateTime nowUtc);

	/// <summary>
	///     Records a failed attempt: the failed step, the status to stay in (<c>drawing</c> or <c>drawn</c>), the attempt
	///     count and the next retry (or <c>failed</c> with no retry when the policy is exhausted).
	/// </summary>
	Task<bool> RegisterFailureAsync(string roundKey, string failedStep, string status, int attempts, string error,
		DateTime? nextRetryAtUtc, DateTime? retryUntilUtc);

	/// <summary>Reopens a terminal <c>failed</c> round for an admin retry of the given step.</summary>
	Task<bool> TryReopenFailedAsync(string roundKey, string failedStep, string newStatus, DateTime? retryUntilUtc);

	/// <summary>Marks a scheduled round as skipped: no draw, no pot change, guesses carry.</summary>
	Task<bool> TryMarkSkippedAsync(string roundKey);

	/// <summary>Voids an unfinished or completed round and annotates it with the reason, actor and time.</summary>
	Task<bool> TryMarkVoidedAsync(string roundKey, string reason, ulong? actorDiscordId, DateTime nowUtc);

	/// <summary>
	///     Re-opens a voided round for a fresh draw of the same date (spec §5.1 "Redo"): back to <c>scheduled</c>, the
	///     voided result cleared and <c>Attempt</c> incremented so the new run's ledger keys are attempt-scoped. The void
	///     reason, actor and time are preserved for the audit trail.
	/// </summary>
	Task<bool> TryReopenVoidedAsync(string roundKey, DateTime nowUtc);

	/// <summary>Moves a single scheduled round to a new date; the round key follows it. Guesses carry.</summary>
	Task<bool> TryRescheduleAsync(string currentRoundKey, string newRoundKey, DateTime scheduledAtUtc);

	/// <summary>
	///     Imports an already drawn round from the <c>lottery_results</c> archive: creates it as
	///     <see cref="LotteryRoundStatus.Completed" /> when missing, and on re-run only refreshes the run
	///     instant, the winning number and the status. Pot fields, the announcement message id and the claim
	///     metadata are never touched, so the backfill cannot clobber a live round.
	/// </summary>
	/// <returns><c>true</c> when a round was created, <c>false</c> when an existing round was refreshed.</returns>
	Task<bool> ImportCompletedRoundAsync(string roundKey, DateTime runAtUtc, int winningNumber);
}
