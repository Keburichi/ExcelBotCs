using System.Globalization;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     Reads the winner timeline for the member history page (#113, spec §5.8). It is a read-only view over data other
///     modules own: the rows come from <c>lottery_winners</c>, the pot from the pot ledger, and the avatar from the
///     member record when the winner is still a member. No write path lives here — the opening balance is only ever
///     established by the draw, never by a page read.
/// </summary>
public class LotteryHistoryService : ILotteryHistoryService
{
	/// <summary>Winners per page when the request names no limit.</summary>
	public const int DefaultLimit = 10;

	/// <summary>A page must show something; a smaller or missing limit is raised to this.</summary>
	public const int MinLimit = 1;

	/// <summary>The largest page the endpoint serves, so one request cannot pull the whole history.</summary>
	public const int MaxLimit = 50;

	private const string RoundKeyFormat = "yyyy-MM-dd";
	private const string CardDateFormat = "dd.MM.yy";

	private readonly IMemberRepository _members;
	private readonly ILotteryPotLedgerService _pot;
	private readonly ILotterySettingsService _settings;
	private readonly ILotteryWinnerRepository _winners;

	public LotteryHistoryService(ILotteryWinnerRepository winners, IMemberRepository members,
		ILotteryPotLedgerService pot, ILotterySettingsService settings)
	{
		_winners = winners;
		_members = members;
		_pot = pot;
		_settings = settings;
	}

	public async Task<ILotteryHistoryResult> GetPageAsync(string? cursor, int? limit,
		CancellationToken cancellationToken = default)
	{
		LotteryWinnerPageAnchor? anchor = null;
		if (!string.IsNullOrWhiteSpace(cursor))
		{
			if (!LotteryHistoryCursor.TryDecode(cursor, out var decoded))
				return new LotteryHistoryInvalidCursorResult(
					"That page of winners is no longer valid. Reload the page to start from the newest draw.");

			anchor = decoded;
		}

		var pageSize = Math.Clamp(limit ?? DefaultLimit, MinLimit, MaxLimit);

		// One extra row tells us whether another page exists without a second count query.
		var winners = await _winners.GetHistoryPageAsync(anchor, pageSize + 1);
		var hasMore = winners.Count > pageSize;
		var page = hasMore ? winners.Take(pageSize).ToList() : winners;

		var counts = await _winners.GetHistoryCountsAsync();
		var settings = await _settings.GetEffectiveAsync(cancellationToken);
		var pot = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
		var avatars = await ResolveAvatarsAsync(page);

		var items = page.Select(w => new LotteryHistoryWinnerRow(
			w.Id,
			w.RoundKey,
			FormatDate(w.RoundKey),
			w.DisplayName,
			w.DiscordId,
			w.DiscordId is { } discordId ? avatars.GetValueOrDefault(discordId) : null,
			w.Guess,
			w.GilPrize,
			w.BonusItems)).ToList();

		var nextCursor = hasMore && page.Count > 0
			? LotteryHistoryCursor.Encode(new LotteryWinnerPageAnchor(page[^1].RoundKey, page[^1].DateCreated,
				page[^1].Id))
			: null;

		return new LotteryHistoryPageResult(items, nextCursor,
			new LotteryHistoryTotalsResult(counts.Winners, counts.Draws, pot, counts.NewestRoundKey));
	}

	/// <summary>The card date: <c>2026-01-01</c> is shown as <c>01.01.26</c>. An unexpected key is shown verbatim.</summary>
	private static string FormatDate(string roundKey)
	{
		return DateTime.TryParseExact(roundKey, RoundKeyFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
			out var date)
			? date.ToString(CardDateFormat, CultureInfo.InvariantCulture)
			: roundKey;
	}

	/// <summary>
	///     Looks up the Discord avatar of the page's winners that still have a member row. A historical row has no
	///     Discord id and a departed member has no row, so both fall back to the initials the card renders.
	/// </summary>
	private async Task<Dictionary<ulong, string>> ResolveAvatarsAsync(IReadOnlyList<LotteryWinner> winners)
	{
		var avatars = new Dictionary<ulong, string>();
		var discordIds = winners
			.Where(w => w.DiscordId is not null)
			.Select(w => w.DiscordId!.Value)
			.Distinct()
			.ToList();

		foreach (var discordId in discordIds)
		{
			var member = await _members.GetByDiscordId(discordId.ToString(CultureInfo.InvariantCulture));
			if (member is not null && !string.IsNullOrWhiteSpace(member.DiscordAvatar))
				avatars[discordId] = member.DiscordAvatar;
		}

		return avatars;
	}
}
