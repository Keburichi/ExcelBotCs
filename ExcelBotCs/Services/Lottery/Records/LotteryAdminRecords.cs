using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Records;

/// <summary>
///     One audit row of <c>lottery_settings_history</c> as the admin page shows it: who changed which fields at
///     which version. <see cref="Fields" /> is the changed property names, so the overview can say "latest: RunTime".
/// </summary>
public sealed record LotterySettingsChange(int Version, DateTime ChangedAtUtc, ulong? ActorDiscordId,
	IReadOnlyList<string> Fields);

/// <summary>
///     The admin Overview read model (#114): the round engine's snapshot plus the three needs-attention counts, the
///     effective settings and the newest settings change. Everything the page shows comes from here so the page never
///     has to stitch several endpoints together.
/// </summary>
public sealed record LotteryAdminStatusResult(
	LotteryAutomationStatus Automation,
	LotteryEffectiveSettings Settings,
	LotteryStatusSnapshot Snapshot,
	int PendingPayouts,
	int PoolPrizes,
	int OptedIn,
	int EligibleReminders,
	LotterySettingsChange? LastSettingsChange);

/// <summary>
///     The two directions an admin pot adjustment may take (spec §8: <c>post /pot {amount, reason, direction}</c>).
///     The API sends a positive amount plus a direction instead of a signed number so a minus sign cannot be lost in a
///     form field and silently invert the correction.
/// </summary>
public static class LotteryPotAdjustmentDirection
{
	public const string Increase = "increase";
	public const string Decrease = "decrease";

	public static readonly IReadOnlyList<string> All = [Increase, Decrease];

	public static bool IsKnown(string? direction)
	{
		return direction?.Trim().ToLowerInvariant() is Increase or Decrease;
	}

	/// <summary>The signed amount for a direction, or <c>null</c> when the direction is not one of the two.</summary>
	public static long? Signed(long amount, string? direction)
	{
		if (amount <= 0) return null;

		return direction?.Trim().ToLowerInvariant() switch
		{
			Increase => amount,
			Decrease => -amount,
			_ => null
		};
	}
}

/// <summary>
///     The result of an admin pot adjustment: rejected field by field, or applied (possibly clamped by the 1,000,000
///     Gil floor) with the ledger entry that records it. <see cref="Replayed" /> is true when the request repeated an
///     idempotency key, in which case the stored entry is returned and nothing moved a second time.
/// </summary>
public sealed record LotteryPotAdjustmentResult(
	bool Success,
	bool Replayed,
	long Balance,
	long RequestedAmount,
	long AppliedAmount,
	LotteryPotLedgerEntry? Entry,
	IReadOnlyList<LotterySettingsValidationError> Errors)
{
	public static LotteryPotAdjustmentResult Rejected(long requestedAmount,
		IReadOnlyList<LotterySettingsValidationError> errors)
	{
		return new LotteryPotAdjustmentResult(false, false, 0, requestedAmount, 0, null, errors);
	}

	public static LotteryPotAdjustmentResult Applied(long balance, long requestedAmount, long appliedAmount,
		LotteryPotLedgerEntry entry, bool replayed)
	{
		return new LotteryPotAdjustmentResult(true, replayed, balance, requestedAmount, appliedAmount, entry, []);
	}
}

/// <summary>
///     One page of the admin ledger, newest first. <see cref="CursorError" /> is set when the caller sent a cursor this
///     endpoint never issued, which the controller answers with a typed 400 rather than silently starting over.
/// </summary>
public sealed record LotteryLedgerPageResult(
	IReadOnlyList<LotteryPotLedgerEntry> Items,
	string? NextCursor,
	string? CursorError = null)
{
	public bool InvalidCursor => CursorError is not null;
}

/// <summary>
///     One winner as the admin payouts table shows it (spec §5.9 "Pot &amp; payouts"). <see cref="Items" /> is the
///     winner's <c>BonusItems</c> — the same list the public history card shows — so an assigned pool prize appears in
///     both places from one write.
/// </summary>
public sealed record LotteryPayoutRow(
	string Id,
	string RoundKey,
	ulong? DiscordId,
	string DisplayName,
	int Guess,
	long GilPrize,
	IReadOnlyList<string> Items,
	string PayoutStatus,
	ulong? PayoutActorDiscordId,
	DateTime? PayoutAt,
	string? PayoutNote);

/// <summary>
///     The payout table page. <see cref="PendingCount" /> is the collection-wide pending number the Overview's
///     needs-attention card shows, not the page count. <see cref="StatusError" /> is a typed 400 for an unknown filter.
/// </summary>
public sealed record LotteryPayoutPageResult(
	IReadOnlyList<LotteryPayoutRow> Items,
	string? NextCursor,
	int PendingCount,
	string? StatusError = null,
	string? CursorError = null)
{
	public bool InvalidStatus => StatusError is not null;

	public bool InvalidCursor => CursorError is not null;
}

/// <summary>The codes the payout update answers with, so the page can react without parsing prose.</summary>
public static class LotteryPayoutUpdateCode
{
	/// <summary>The winner moved to the requested state; actor, time and note were recorded.</summary>
	public const string Updated = "updated";

	/// <summary>The winner is already in the requested state (a second click); nothing changed.</summary>
	public const string Unchanged = "unchanged";

	/// <summary>No winner with that id exists under that round key.</summary>
	public const string NotFound = "not-found";

	/// <summary>The requested status is not a payout state.</summary>
	public const string InvalidStatus = "invalid-status";

	/// <summary>The winner is in a terminal state the requested transition would undo.</summary>
	public const string InvalidTransition = "invalid-transition";
}

/// <summary>
///     The result of a payout update. <see cref="ReturnedPrizes" /> names the pool items a forfeit handed back, because
///     the action is not complete for the admin unless they see the item is available again.
/// </summary>
public sealed record LotteryPayoutUpdateResult(
	bool Success,
	string Code,
	string Message,
	LotteryPayoutRow? Row,
	IReadOnlyList<LotteryPrize> ReturnedPrizes)
{
	public static LotteryPayoutUpdateResult Rejected(string code, string message)
	{
		return new LotteryPayoutUpdateResult(false, code, message, null, []);
	}

	public static LotteryPayoutUpdateResult Applied(string code, string message, LotteryPayoutRow row,
		IReadOnlyList<LotteryPrize> returnedPrizes)
	{
		return new LotteryPayoutUpdateResult(true, code, message, row, returnedPrizes);
	}
}

/// <summary>The codes the prize pool mutations answer with.</summary>
public static class LotteryPrizeCode
{
	/// <summary>A new item was added to the pool.</summary>
	public const string Created = "created";

	/// <summary>An in-pool item was deleted.</summary>
	public const string Removed = "removed";

	/// <summary>An in-pool item was assigned to a winner.</summary>
	public const string Assigned = "assigned";

	/// <summary>No prize or winner with that id exists.</summary>
	public const string NotFound = "not-found";

	/// <summary>The request itself is invalid (empty name, missing winner, a winner without a Discord id).</summary>
	public const string Invalid = "invalid";

	/// <summary>An in-pool item with that name already exists.</summary>
	public const string Duplicate = "duplicate";

	/// <summary>The item is already awarded; only the forfeit path returns it to the pool.</summary>
	public const string Awarded = "awarded";

	/// <summary>The target winner's payout is forfeited, so the item cannot be linked to them.</summary>
	public const string InvalidTransition = "invalid-transition";
}

/// <summary>The admin prize pool: every item, and the two counts that drive the pot summary and the pool card.</summary>
public sealed record LotteryPrizePoolResult(
	IReadOnlyList<LotteryPrize> Items,
	int InPoolCount,
	int AwardedCount);

/// <summary>The outcome of adding or removing a pool item; <see cref="Errors" /> carries an add's field error.</summary>
public sealed record LotteryPrizeResult(
	bool Success,
	string Code,
	string Message,
	LotteryPrize? Prize,
	IReadOnlyList<LotterySettingsValidationError> Errors)
{
	public static LotteryPrizeResult Rejected(string code, string message,
		IReadOnlyList<LotterySettingsValidationError>? errors = null)
	{
		return new LotteryPrizeResult(false, code, message, null, errors ?? []);
	}

	public static LotteryPrizeResult Applied(string code, string message, LotteryPrize prize)
	{
		return new LotteryPrizeResult(true, code, message, prize, []);
	}
}

/// <summary>
///     The outcome of assigning a pool item to a winner. <see cref="Winner" /> is the winner after the write, so the
///     page can update its payout row without refetching the whole table.
/// </summary>
public sealed record LotteryPrizeAssignmentResult(
	bool Success,
	string Code,
	string Message,
	LotteryPrize? Prize,
	LotteryPayoutRow? Winner,
	IReadOnlyList<LotterySettingsValidationError> Errors)
{
	public static LotteryPrizeAssignmentResult Rejected(string code, string message,
		IReadOnlyList<LotterySettingsValidationError>? errors = null)
	{
		return new LotteryPrizeAssignmentResult(false, code, message, null, null, errors ?? []);
	}

	public static LotteryPrizeAssignmentResult Applied(LotteryPrize prize, LotteryPayoutRow winner)
	{
		return new LotteryPrizeAssignmentResult(true, LotteryPrizeCode.Assigned,
			$"'{prize.Name}' was assigned to {winner.DisplayName}.", prize, winner, []);
	}
}

/// <summary>
///     One member who has reminders turned on, for the Schedule &amp; reminders tab (spec §5.9). Opt-in is by Discord id
///     and survives leaving the FC, so <see cref="IsCurrentMember" /> and <see cref="IsEligible" /> say whether the
///     reminder would actually ping them: only a current member with an unused guess is eligible (§5.7).
/// </summary>
public sealed record LotteryOptInRow(
	ulong DiscordId,
	string? DisplayName,
	bool IsCurrentMember,
	bool IsEligible,
	DateTime OptedInAtUtc);

/// <summary>The opt-in list with the raw opted-in count and the eligible audience size.</summary>
public sealed record LotteryOptInsResult(
	IReadOnlyList<LotteryOptInRow> Items,
	int OptedIn,
	int Eligible);

/// <summary>The outcome of removing one opt-in: removed, or no such record.</summary>
public sealed record LotteryOptInRemovalResult(bool Success, string Message)
{
	public static LotteryOptInRemovalResult Removed(ulong discordId)
	{
		return new LotteryOptInRemovalResult(true, $"{discordId} will no longer receive lottery reminders.");
	}

	public static LotteryOptInRemovalResult NotFound(ulong discordId)
	{
		return new LotteryOptInRemovalResult(false, $"{discordId} does not have lottery reminders turned on.");
	}
}
