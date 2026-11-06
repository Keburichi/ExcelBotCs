using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The admin page's read model and audited mutations (#114). It is an orchestration layer: the round engine owns
///     the state machine, the settings service owns precedence and the audit row, and the pot ledger owns the balance.
///     This class only joins them into the shapes the page shows and enforces the admin-specific rules (who may act,
///     which transitions are legal, what a prize assignment does to the winner's history).
/// </summary>
public class LotteryAdminService : ILotteryAdminService
{
	/// <summary>Ledger rows per page when the request names no limit.</summary>
	public const int DefaultLedgerPageSize = 20;

	/// <summary>The largest ledger page the endpoint serves, so one request cannot pull the whole ledger.</summary>
	public const int MaxLedgerPageSize = 100;

	/// <summary>Payout rows per page when the request names no limit.</summary>
	public const int DefaultPayoutPageSize = 25;

	/// <summary>The largest payouts page the endpoint serves.</summary>
	public const int MaxPayoutPageSize = 100;

	private readonly ILotteryPotLedgerRepository _ledger;
	private readonly ILogger<LotteryAdminService> _logger;
	private readonly ILotteryPotLedgerService _pot;
	private readonly IMemberService _members;
	private readonly ILotteryAnnouncementPreviewer _previewer;
	private readonly ILotteryPrizePoolRepository _prizes;
	private readonly ILotteryReminderService _reminders;
	private readonly ILotteryReminderOptinRepository _optIns;
	private readonly ILotteryRoundRunner _runner;
	private readonly ILotterySettingsHistoryRepository _settingsHistory;
	private readonly ILotterySettingsService _settings;
	private readonly ILotteryWinnerRepository _winners;

	public LotteryAdminService(ILotteryRoundRunner runner, ILotterySettingsService settings,
		ILotterySettingsHistoryRepository settingsHistory, ILotteryPrizePoolRepository prizes,
		ILotteryWinnerRepository winners, ILotteryReminderOptinRepository optIns,
		ILotteryReminderService reminders, ILotteryPotLedgerService pot, ILotteryPotLedgerRepository ledger,
		IMemberService members, ILotteryAnnouncementPreviewer previewer, ILogger<LotteryAdminService> logger)
	{
		_runner = runner;
		_settings = settings;
		_settingsHistory = settingsHistory;
		_prizes = prizes;
		_winners = winners;
		_optIns = optIns;
		_reminders = reminders;
		_members = members;
		_previewer = previewer;
		_pot = pot;
		_ledger = ledger;
		_logger = logger;
	}

	public async Task<LotteryAdminStatusResult> GetStatusAsync(CancellationToken cancellationToken = default)
	{
		var settings = await _settings.GetEffectiveAsync(cancellationToken);
		var snapshot = await _runner.GetStatusAsync(cancellationToken);

		var pendingPayouts = await _winners.CountByPayoutStatusAsync(LotteryPayoutStatus.Pending);
		var poolPrizes = (await _prizes.GetInPoolAsync()).Count;
		var optedIn = (await _optIns.GetEnabledAsync()).Count;
		var audience = await _reminders.GetAudienceAsync(cancellationToken);

		return new LotteryAdminStatusResult(settings.Status, settings, snapshot, pendingPayouts, poolPrizes,
			optedIn, audience.Recipients.Count, await GetLastSettingsChangeAsync(cancellationToken));
	}

	public Task<LotteryEffectiveSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
	{
		return _settings.GetEffectiveAsync(cancellationToken);
	}

	public Task<LotterySettingsSaveResult> SaveSettingsAsync(LotterySettingsUpdate update, bool resetTemplates,
		ulong? actorDiscordId, CancellationToken cancellationToken = default)
	{
		var request = resetTemplates ? LotterySettingsValidator.ResetTemplates(update) : update;
		return _settings.SaveAsync(request, actorDiscordId, cancellationToken);
	}

	public async Task<LotteryPotAdjustmentResult> AdjustPotAsync(long amount, string? direction, string? reason,
		string? idempotencyKey, ulong actorDiscordId, CancellationToken cancellationToken = default)
	{
		var errors = new List<LotterySettingsValidationError>();

		if (amount <= 0)
			errors.Add(new LotterySettingsValidationError("Amount",
				"must be a positive whole number of Gil."));

		var signed = LotteryPotAdjustmentDirection.Signed(amount, direction);
		if (!LotteryPotAdjustmentDirection.IsKnown(direction))
			errors.Add(new LotterySettingsValidationError("Direction",
				$"must be '{LotteryPotAdjustmentDirection.Increase}' or '{LotteryPotAdjustmentDirection.Decrease}'."));

		var trimmedReason = reason?.Trim();
		if (string.IsNullOrEmpty(trimmedReason))
			errors.Add(new LotterySettingsValidationError("Reason",
				"is required for the audit trail."));
		else if (trimmedReason.Length > 200)
			errors.Add(new LotterySettingsValidationError("Reason",
				"must be 200 characters or fewer."));

		if (errors.Count > 0) return LotteryPotAdjustmentResult.Rejected(amount, errors);

		var settings = await _settings.GetEffectiveAsync(cancellationToken);
		var key = ManualKey(idempotencyKey);

		var existing = await _pot.FindAsync(key, cancellationToken);
		if (existing is not null)
		{
			// A retried submit of the same form: the stored entry is the answer, and the pot does not move again.
			var current = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
			return LotteryPotAdjustmentResult.Applied(current, amount, existing.Amount, existing, true);
		}

		var balance = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
		var applied = LotteryPotMath.ApplyCorrection(balance, signed!.Value, settings.BasePot);

		var entry = await _pot.AppendAsync(null, LotteryPotLedgerType.Correction, applied, balance + applied,
			trimmedReason!, actorDiscordId, key, cancellationToken);

		return LotteryPotAdjustmentResult.Applied(entry.BalanceAfter, amount, applied, entry, false);
	}

	/// <summary>
	///     Namespaces a form-submitted idempotency key so it can never collide with a round-derived ledger key; a form
	///     without a key gets a fresh one, which makes this request the one that wrote the entry.
	/// </summary>
	private static string ManualKey(string? idempotencyKey)
	{
		var value = idempotencyKey?.Trim();
		return value is null or ""
			? $"manual:{Guid.NewGuid():N}"
			: $"manual:{value}";
	}

	public async Task<LotteryLedgerPageResult> GetLedgerAsync(string? cursor, int? limit,
		CancellationToken cancellationToken = default)
	{
		LotteryLedgerPageAnchor? anchor = null;
		if (!string.IsNullOrWhiteSpace(cursor))
		{
			if (!LotteryLedgerCursor.TryDecode(cursor, out var decoded))
				return new LotteryLedgerPageResult([], null,
					"That page of the ledger is no longer valid. Reload the page to start from the newest entry.");

			anchor = decoded;
		}

		var pageSize = Math.Clamp(limit ?? DefaultLedgerPageSize, 1, MaxLedgerPageSize);

		// One extra row tells us whether another page exists without a second count query.
		var rows = await _ledger.GetPageAsync(anchor, pageSize + 1);
		var hasMore = rows.Count > pageSize;
		var page = hasMore ? rows.Take(pageSize).ToList() : rows;

		var nextCursor = hasMore && page.Count > 0
			? LotteryLedgerCursor.Encode(new LotteryLedgerPageAnchor(page[^1].DateCreated, page[^1].Id))
			: null;

		return new LotteryLedgerPageResult(page, nextCursor);
	}

	public async Task<LotteryPayoutPageResult> GetPayoutsAsync(string? payoutStatus, string? cursor, int? limit,
		CancellationToken cancellationToken = default)
	{
		string? status = null;
		if (!string.IsNullOrWhiteSpace(payoutStatus))
		{
			status = payoutStatus.Trim().ToLowerInvariant();
			if (!LotteryPayoutStatus.All.Contains(status))
				return new LotteryPayoutPageResult([], null, 0,
					$"'{payoutStatus}' is not a payout status. Use one of: {string.Join(", ", LotteryPayoutStatus.All)}.");
		}

		LotteryWinnerPageAnchor? anchor = null;
		if (!string.IsNullOrWhiteSpace(cursor))
		{
			// The payouts table and the member timeline order winners identically, so they share one cursor shape.
			if (!LotteryHistoryCursor.TryDecode(cursor, out var decoded))
				return new LotteryPayoutPageResult([], null, 0, null,
					"That page of payouts is no longer valid. Reload the page to start from the newest draw.");

			anchor = decoded;
		}

		var pageSize = Math.Clamp(limit ?? DefaultPayoutPageSize, 1, MaxPayoutPageSize);
		var winners = await _winners.GetPayoutPageAsync(status, anchor, pageSize + 1);
		var hasMore = winners.Count > pageSize;
		var page = hasMore ? winners.Take(pageSize).ToList() : winners;

		var nextCursor = hasMore && page.Count > 0
			? LotteryHistoryCursor.Encode(new LotteryWinnerPageAnchor(page[^1].RoundKey, page[^1].DateCreated,
				page[^1].Id))
			: null;

		return new LotteryPayoutPageResult(page.Select(ToPayoutRow).ToList(), nextCursor,
			await _winners.CountByPayoutStatusAsync(LotteryPayoutStatus.Pending));
	}

	public async Task<LotteryPayoutUpdateResult> UpdatePayoutAsync(string roundKey, string winnerId,
		string? payoutStatus, string? note, ulong actorDiscordId, CancellationToken cancellationToken = default)
	{
		var status = payoutStatus?.Trim().ToLowerInvariant();
		if (status is null || !LotteryPayoutStatus.All.Contains(status))
			return LotteryPayoutUpdateResult.Rejected(LotteryPayoutUpdateCode.InvalidStatus,
				$"'{payoutStatus}' is not a payout status. Use one of: {string.Join(", ", LotteryPayoutStatus.All)}.");

		// A path id is caller input: Mongo would throw on a malformed ObjectId, so an id that cannot be one is just a
		// missing winner (typed 404), never a raw exception.
		if (!IsDocumentId(winnerId))
			return LotteryPayoutUpdateResult.Rejected(LotteryPayoutUpdateCode.NotFound,
				$"No winner {winnerId} exists in round {roundKey}.");

		var winner = await _winners.GetAsync(winnerId);
		if (winner is null || !string.Equals(winner.RoundKey, roundKey, StringComparison.Ordinal))
			return LotteryPayoutUpdateResult.Rejected(LotteryPayoutUpdateCode.NotFound,
				$"No winner {winnerId} exists in round {roundKey}.");

		if (winner.PayoutStatus == status)
			return LotteryPayoutUpdateResult.Applied(LotteryPayoutUpdateCode.Unchanged,
				$"{winner.DisplayName} is already {status}.", ToPayoutRow(winner), []);

		if (!IsAllowedPayoutTransition(winner.PayoutStatus, status))
			return LotteryPayoutUpdateResult.Rejected(LotteryPayoutUpdateCode.InvalidTransition,
				$"A {winner.PayoutStatus} payout cannot become {status}. A forfeited payout is final; re-award the " +
				"prize instead of undoing it.");

		var returned = status == LotteryPayoutStatus.Forfeited
			? await ReturnAssignedPrizesAsync(winner, cancellationToken)
			: [];

		winner.PayoutStatus = status;
		winner.PayoutActorDiscordId = actorDiscordId;
		winner.PayoutAt = DateTime.UtcNow;
		winner.PayoutNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
		await _winners.UpdateAsync(winner.Id, winner);

		var message = status == LotteryPayoutStatus.Paid
			? $"{winner.DisplayName}'s {winner.GilPrize:N0} Gil payout was marked paid."
			: $"{winner.DisplayName}'s payout was forfeited." +
			  (returned.Count == 0 ? "" : $" Returned to the pool: {string.Join(", ", returned.Select(p => p.Name))}.");

		return LotteryPayoutUpdateResult.Applied(LotteryPayoutUpdateCode.Updated, message, ToPayoutRow(winner),
			returned);
	}

	/// <summary>
	///     The payout state machine of spec §5.9: a pending payout is settled either way, a paid one can still be
	///     forfeited when the winner leaves before receiving the item, and a forfeit is final. Nothing goes back to
	///     pending, so a mistaken click never re-opens a settled payout silently.
	/// </summary>
	/// <summary>Whether a path/body id can be a Mongo document id; anything else is a typed not-found, not a throw.</summary>
	private static bool IsDocumentId(string? id)
	{
		return id is { Length: 24 } && id.All(Uri.IsHexDigit);
	}

	private static bool IsAllowedPayoutTransition(string from, string to)
	{
		return (from, to) switch
		{
			(LotteryPayoutStatus.Pending, LotteryPayoutStatus.Paid) => true,
			(LotteryPayoutStatus.Pending, LotteryPayoutStatus.Forfeited) => true,
			(LotteryPayoutStatus.Paid, LotteryPayoutStatus.Forfeited) => true,
			_ => false
		};
	}

	/// <summary>
	///     Hands the winner's assigned pool items back: each prize returns to <c>in-pool</c> with its award fields
	///     cleared, and its name leaves the winner's <c>BonusItems</c> so the public history stops claiming it. Items a
	///     historical winner carries without a pool row are left alone — there is nothing to return.
	/// </summary>
	private async Task<IReadOnlyList<LotteryPrize>> ReturnAssignedPrizesAsync(LotteryWinner winner,
		CancellationToken cancellationToken)
	{
		if (winner.DiscordId is not { } discordId) return [];

		var prizes = await _prizes.GetByAwardedRoundKeyAsync(winner.RoundKey);
		var owned = prizes
			.Where(prize => prize.Status == LotteryPrizePoolStatus.Awarded
			                && prize.AwardedToDiscordId == discordId)
			.ToList();

		if (owned.Count == 0) return [];

		foreach (var prize in owned)
		{
			prize.Status = LotteryPrizePoolStatus.InPool;
			prize.AwardedToDiscordId = null;
			prize.AwardedToName = null;
			prize.AwardedRoundKey = null;
			await _prizes.UpdateAsync(prize.Id, prize);
		}

		var returnedNames = owned.Select(prize => prize.Name).ToHashSet(StringComparer.Ordinal);
		winner.BonusItems = winner.BonusItems.Where(item => !returnedNames.Contains(item)).ToArray();

		return owned;
	}

	public async Task<LotteryPrizePoolResult> GetPrizesAsync(CancellationToken cancellationToken = default)
	{
		var prizes = await _prizes.GetAsync();

		// In-pool first in the order they were added (the order an admin works through them), then the awarded items
		// newest round first, so the pool card reads as a queue followed by an archive.
		var ordered = prizes
			.OrderBy(prize => prize.Status == LotteryPrizePoolStatus.InPool ? 0 : 1)
			.ThenBy(prize => prize.AwardedRoundKey ?? string.Empty, StringComparer.Ordinal)
			.ThenBy(prize => prize.AddedAt)
			.ThenBy(prize => prize.Id, StringComparer.Ordinal)
			.ToList();

		return new LotteryPrizePoolResult(ordered,
			ordered.Count(prize => prize.Status == LotteryPrizePoolStatus.InPool),
			ordered.Count(prize => prize.Status == LotteryPrizePoolStatus.Awarded));
	}

	public async Task<LotteryPrizeResult> AddPrizeAsync(string? name, ulong actorDiscordId,
		CancellationToken cancellationToken = default)
	{
		var trimmed = name?.Trim();
		if (string.IsNullOrEmpty(trimmed))
			return LotteryPrizeResult.Rejected(LotteryPrizeCode.Invalid, "A prize needs a name.", [
				new LotterySettingsValidationError(nameof(Modules.Lottery.LotteryPrize.Name), "is required.")
			]);

		if (trimmed.Length > 100)
			return LotteryPrizeResult.Rejected(LotteryPrizeCode.Invalid,
				"A prize name must be 100 characters or fewer.", [
					new LotterySettingsValidationError(nameof(Modules.Lottery.LotteryPrize.Name),
						"must be 100 characters or fewer.")
				]);

		// Only in-pool names are unique: the same prize can be won again years later, and a historical awarded row
		// must not block restocking it.
		var inPool = await _prizes.GetInPoolAsync();
		if (inPool.Any(prize => string.Equals(prize.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
			return LotteryPrizeResult.Rejected(LotteryPrizeCode.Duplicate,
				$"'{trimmed}' is already in the prize pool.");

		var prize = new LotteryPrize
		{
			Name = trimmed,
			Status = LotteryPrizePoolStatus.InPool,
			AddedByDiscordId = actorDiscordId == 0 ? null : actorDiscordId,
			AddedAt = DateTime.UtcNow
		};

		await _prizes.CreateAsync(prize);
		_logger.LogInformation("Lottery prize '{Name}' added to the pool by {Actor}", prize.Name, actorDiscordId);

		return LotteryPrizeResult.Applied(LotteryPrizeCode.Created, $"'{prize.Name}' was added to the prize pool.",
			prize);
	}

	public async Task<LotteryPrizeResult> RemovePrizeAsync(string id, CancellationToken cancellationToken = default)
	{
		if (!IsDocumentId(id)) return LotteryPrizeResult.Rejected(LotteryPrizeCode.NotFound, $"Prize {id} does not exist.");

		var prize = await _prizes.GetAsync(id);
		if (prize is null)
			return LotteryPrizeResult.Rejected(LotteryPrizeCode.NotFound, $"Prize {id} does not exist.");

		if (prize.Status == LotteryPrizePoolStatus.Awarded)
			return LotteryPrizeResult.Rejected(LotteryPrizeCode.Awarded,
				$"'{prize.Name}' is awarded to {prize.AwardedToName ?? "a winner"}; forfeit that payout to return it to " +
				"the pool.");

		await _prizes.DeleteAsync(prize.Id);
		_logger.LogInformation("Lottery prize '{Name}' ({Id}) removed from the pool", prize.Name, prize.Id);

		return LotteryPrizeResult.Applied(LotteryPrizeCode.Removed, $"'{prize.Name}' was removed from the prize pool.",
			prize);
	}

	public async Task<LotteryPrizeAssignmentResult> AssignPrizeAsync(string prizeId, string? roundKey,
		string? winnerId, ulong actorDiscordId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(roundKey) || string.IsNullOrWhiteSpace(winnerId))
			return LotteryPrizeAssignmentResult.Rejected(LotteryPrizeCode.Invalid,
				"A prize needs both a round key and a winner.");

		if (!IsDocumentId(prizeId))
			return LotteryPrizeAssignmentResult.Rejected(LotteryPrizeCode.NotFound,
				$"Prize {prizeId} does not exist.");

		var prize = await _prizes.GetAsync(prizeId);
		if (prize is null)
			return LotteryPrizeAssignmentResult.Rejected(LotteryPrizeCode.NotFound,
				$"Prize {prizeId} does not exist.");

		if (prize.Status != LotteryPrizePoolStatus.InPool)
			return LotteryPrizeAssignmentResult.Rejected(LotteryPrizeCode.Awarded,
				$"'{prize.Name}' is already awarded to {prize.AwardedToName ?? "a winner"}. An item goes to one winner.");

		// A path id is caller input: Mongo would throw on a malformed ObjectId, so an id that cannot be one is just a
		// missing winner (typed 404), never a raw exception.
		var winner = IsDocumentId(winnerId) ? await _winners.GetAsync(winnerId) : null;
		if (winner is null || !string.Equals(winner.RoundKey, roundKey, StringComparison.Ordinal))
			return LotteryPrizeAssignmentResult.Rejected(LotteryPrizeCode.NotFound,
				$"No winner {winnerId} exists in round {roundKey}.");

		// A historical row has only a display name, so there is no Discord id to link an item to.
		if (winner.DiscordId is null)
			return LotteryPrizeAssignmentResult.Rejected(LotteryPrizeCode.Invalid,
				$"{winner.DisplayName} was recorded without a Discord id, so a prize cannot be linked to them.");

		if (winner.PayoutStatus == LotteryPayoutStatus.Forfeited)
			return LotteryPrizeAssignmentResult.Rejected(LotteryPrizeCode.InvalidTransition,
				$"{winner.DisplayName}'s payout was forfeited; assign the item to another winner.");

		prize.Status = LotteryPrizePoolStatus.Awarded;
		prize.AwardedToDiscordId = winner.DiscordId;
		prize.AwardedToName = winner.DisplayName;
		prize.AwardedRoundKey = winner.RoundKey;
		await _prizes.UpdateAsync(prize.Id, prize);

		if (!winner.BonusItems.Contains(prize.Name, StringComparer.Ordinal))
		{
			winner.BonusItems = [..winner.BonusItems, prize.Name];
			await _winners.UpdateAsync(winner.Id, winner);
		}

		_logger.LogInformation("Lottery prize '{Name}' assigned to {Winner} ({RoundKey}) by {Actor}", prize.Name,
			winner.DisplayName, winner.RoundKey, actorDiscordId);

		return LotteryPrizeAssignmentResult.Applied(prize, ToPayoutRow(winner));
	}

	public async Task<LotteryOptInsResult> GetOptInsAsync(CancellationToken cancellationToken = default)
	{
		var optedIn = await _optIns.GetEnabledAsync();
		var audience = await _reminders.GetAudienceAsync(cancellationToken);
		var eligibleIds = audience.Recipients.Select(recipient => recipient.DiscordId).ToHashSet();

		var members = await _members.GetAsync();
		var byDiscordId = members
			.Where(member => ulong.TryParse(member.DiscordId, out _))
			.ToDictionary(member => ulong.Parse(member.DiscordId), member => member);

		var rows = optedIn
			.Select(optIn =>
			{
				var member = byDiscordId.GetValueOrDefault(optIn.DiscordId);
				return new LotteryOptInRow(optIn.DiscordId, member?.DiscordName,
					member?.IsMember.GetValueOrDefault() == true, eligibleIds.Contains(optIn.DiscordId),
					optIn.DateCreated);
			})
			.OrderByDescending(row => row.IsEligible)
			.ThenBy(row => row.DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
			.ThenBy(row => row.DiscordId)
			.ToList();

		return new LotteryOptInsResult(rows, optedIn.Count, audience.Recipients.Count);
	}

	public async Task<LotteryOptInRemovalResult> RemoveOptInAsync(ulong discordId,
		CancellationToken cancellationToken = default)
	{
		var optIn = await _optIns.GetByDiscordIdAsync(discordId);
		if (optIn is null) return LotteryOptInRemovalResult.NotFound(discordId);

		await _optIns.DeleteAsync(optIn.Id);
		_logger.LogInformation("Lottery reminder opt-in {DiscordId} was removed by an admin", discordId);

		return LotteryOptInRemovalResult.Removed(discordId);
	}

	public Task<LotteryReminderOutcome> SendTestReminderAsync(ulong requestingAdminDiscordId,
		CancellationToken cancellationToken = default)
	{
		return _reminders.SendTestAsync(requestingAdminDiscordId, cancellationToken);
	}

	public Task<LotteryAnnouncementPreview> RenderPreviewAsync(string? variant,
		CancellationToken cancellationToken = default)
	{
		// The page opens the Messages tab on the no-win copy; every other value must be one the composer knows, which
		// the previewer rejects with the typed message the controller turns into a 400.
		var resolved = string.IsNullOrWhiteSpace(variant) ? LotteryAnnouncementVariant.NoWin : variant.Trim();
		return _previewer.RenderAsync(resolved, cancellationToken);
	}

	private static LotteryPayoutRow ToPayoutRow(LotteryWinner winner)
	{
		return new LotteryPayoutRow(winner.Id, winner.RoundKey, winner.DiscordId, winner.DisplayName, winner.Guess,
			winner.GilPrize, winner.BonusItems, winner.PayoutStatus, winner.PayoutActorDiscordId, winner.PayoutAt,
			winner.PayoutNote);
	}

	private async Task<LotterySettingsChange?> GetLastSettingsChangeAsync(CancellationToken cancellationToken)
	{
		var rows = await _settingsHistory.GetBySettingsKeyAsync(LotterySettings.SingletonKey);
		var newest = rows.LastOrDefault();
		if (newest is null) return null;

		return new LotterySettingsChange(newest.Version, newest.ChangedAt, newest.ActorDiscordId,
			newest.NewValues.Keys.OrderBy(field => field, StringComparer.Ordinal).ToList());
	}
}
