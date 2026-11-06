using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The announcement seam (spec §5.6 and §5.10). The runner owns the state machine and calls this publisher after
///     the result, entries, winners and ledger are persisted. #111 replaces the minimal implementation with the real
///     post (templates, role ping, last five, history link, dry-run prefix, posting reconciliation).
/// </summary>
public interface ILotteryAnnouncementPublisher
{
	/// <summary>
	///     Posts the announcement for a drawn round. Returning a message id lets the round store it; the minimal
	///     implementation returns <c>null</c> until #111 adds a Discord API that returns the message. Throwing marks
	///     the announcement failed and leaves the round <c>drawn</c> with a retry scheduled — it never triggers a
	///     second draw.
	/// </summary>
	Task<LotteryAnnouncementPost> PublishAsync(LotteryAnnouncement announcement,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Looks for a post this round already made before a crash (the <c>PostingStartedAt</c> reconciliation of
	///     §5.10). Returning the id adopts it and completes the round without a duplicate post. The minimal
	///     implementation returns <c>null</c>; the real search belongs to #111.
	/// </summary>
	Task<string?> FindExistingPostAsync(LotteryRound round, CancellationToken cancellationToken = default);

	/// <summary>Annotates (or retracts) the post of a voided round. Copy and API belong to #111.</summary>
	Task AnnotateVoidedAsync(LotteryRound round, string reason, CancellationToken cancellationToken = default);
}
