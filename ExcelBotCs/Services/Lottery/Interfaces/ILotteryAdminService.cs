using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The admin API's domain seam (#114, spec §5.9 and §8). It composes the read models and the audited mutations
///     the <c>/lottery/admin</c> page drives, so the HTTP layer only maps results to DTOs and dispositions to status
///     codes. Nothing here re-implements a money, prize or state-machine rule: the pot floor is
///     <see cref="LotteryPotMath.ApplyCorrection" />, a round action is <see cref="ILotteryRoundRunner" />, a settings
///     save is <see cref="ILotterySettingsService" /> and the announcement is <see cref="ILotteryAnnouncementPreviewer" />.
/// </summary>
public interface ILotteryAdminService
{
	/// <summary>The Overview: status pill, pot, counts, latest/blocking round, next run and last settings change.</summary>
	Task<LotteryAdminStatusResult> GetStatusAsync(CancellationToken cancellationToken = default);

	/// <summary>The settings form: the effective admin-editable set plus the read-only flags it displays.</summary>
	Task<LotteryEffectiveSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Validates and stores a settings change through <see cref="ILotterySettingsService" />, so the version bump,
	///     the <c>lottery_settings_history</c> row and the log-channel notice are the ones §5.9 asks for. A rejected
	///     change writes nothing. <paramref name="resetTemplates" /> restores the §5.6 default result copy first.
	/// </summary>
	Task<LotterySettingsSaveResult> SaveSettingsAsync(LotterySettingsUpdate update, bool resetTemplates,
		ulong? actorDiscordId, CancellationToken cancellationToken = default);

	/// <summary>
	///     Applies a signed correction, clamped by <see cref="LotteryPotMath.ApplyCorrection" /> so the pot never drops
	///     below the base pot (1,000,000 Gil). The correction is always ledgered, even when it applies zero; a repeated
	///     idempotency key replays the stored entry instead of moving the pot twice.
	/// </summary>
	Task<LotteryPotAdjustmentResult> AdjustPotAsync(long amount, string? direction, string? reason,
		string? idempotencyKey, ulong actorDiscordId, CancellationToken cancellationToken = default);

	/// <summary>One keyset page of the ledger, newest first (spec §5.4, §8).</summary>
	Task<LotteryLedgerPageResult> GetLedgerAsync(string? cursor, int? limit,
		CancellationToken cancellationToken = default);

	/// <summary>One keyset page of the payouts table, newest round first, optionally filtered by payout status.</summary>
	Task<LotteryPayoutPageResult> GetPayoutsAsync(string? payoutStatus, string? cursor, int? limit,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Moves a winner's payout to <c>paid</c> or <c>forfeited</c> (and a paid row back to forfeited), recording the
	///     actor, the time and the note. A forfeit returns the winner's assigned pool prizes to the pool and removes
	///     them from the winner's history items. Terminal states are not silently reversed: an illegal transition is a
	///     typed rejection (spec §5.9 "no silent redo").
	/// </summary>
	Task<LotteryPayoutUpdateResult> UpdatePayoutAsync(string roundKey, string winnerId, string? payoutStatus,
		string? note, ulong actorDiscordId, CancellationToken cancellationToken = default);

	/// <summary>The prize pool: every item, in-pool first, with the counts the pot summary shows (spec §5.5).</summary>
	Task<LotteryPrizePoolResult> GetPrizesAsync(CancellationToken cancellationToken = default);

	/// <summary>Adds an indivisible non-Gil item to the pool, recording who added it; names must be unique in the pool.</summary>
	Task<LotteryPrizeResult> AddPrizeAsync(string? name, ulong actorDiscordId,
		CancellationToken cancellationToken = default);

	/// <summary>Removes an item that is still in the pool. An awarded item is refused: it returns only on a forfeit.</summary>
	Task<LotteryPrizeResult> RemovePrizeAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>
	///     Links a pool item to one winner and appends its name to that winner's <c>BonusItems</c>, which is the same
	///     list the public history card reads — one write, both surfaces (§5.5, §13 criterion 8).
	/// </summary>
	Task<LotteryPrizeAssignmentResult> AssignPrizeAsync(string prizeId, string? roundKey, string? winnerId,
		ulong actorDiscordId, CancellationToken cancellationToken = default);

	/// <summary>The opt-in list with names and eligibility, plus the raw and eligible counts (spec §5.7, §5.9).</summary>
	Task<LotteryOptInsResult> GetOptInsAsync(CancellationToken cancellationToken = default);

	/// <summary>Removes one member's opt-in; their guesses and any other member state are untouched.</summary>
	Task<LotteryOptInRemovalResult> RemoveOptInAsync(ulong discordId, CancellationToken cancellationToken = default);

	/// <summary>Sends the reminder copy to the requesting admin only — never the channel, never the audience (§5.9).</summary>
	Task<LotteryReminderOutcome> SendTestReminderAsync(ulong requestingAdminDiscordId,
		CancellationToken cancellationToken = default);

	/// <summary>The assembled announcement for one result variant, with the visible character count (§5.6, §8).</summary>
	Task<LotteryAnnouncementPreview> RenderPreviewAsync(string? variant,
		CancellationToken cancellationToken = default);
}
