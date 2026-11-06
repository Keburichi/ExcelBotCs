using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The announcement seam (spec §5.6 and §5.10). The runner owns the state machine and calls this publisher after
///     the result, entries, winners and ledger are persisted. The real implementation
///     (<see cref="LotteryAnnouncementPublisher" />) renders the editable templates, pings the configured role, posts
///     the last five and the history link, prefixes a dry run and reconciles an already-posted announcement.
/// </summary>
public interface ILotteryAnnouncementPublisher
{
	/// <summary>
	///     Posts the announcement for a drawn round and returns the message id, so the round stores
	///     <c>AnnouncementMessageId</c>. Throwing marks the announcement failed and leaves the round <c>drawn</c> with
	///     a retry scheduled — it never triggers a second draw. A dry run
	///     (<see cref="LotteryAnnouncement.DryRun" />) posts the same text to the log channel with a
	///     <c>[DRY RUN]</c> prefix and mentions suppressed, and returns no id.
	/// </summary>
	Task<LotteryAnnouncementPost> PublishAsync(LotteryAnnouncement announcement,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Looks for a post this round already made before a crash (the <c>PostingStartedAt</c> reconciliation of
	///     §5.10). Returning the id adopts it and completes the round without a duplicate post; a dry-run message is
	///     never adopted.
	/// </summary>
	Task<string?> FindExistingPostAsync(LotteryRound round, CancellationToken cancellationToken = default);

	/// <summary>
	///     Annotates the announcement of a voided round in the lottery channel: the round key, the reason and the admin
	///     page link, so nobody pays out a retracted result.
	/// </summary>
	Task AnnotateVoidedAsync(LotteryRound round, string reason, CancellationToken cancellationToken = default);
}
