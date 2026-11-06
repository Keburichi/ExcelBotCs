namespace ExcelBotCs.Services.Lottery.Records;

/// <summary>
///     One winner row of the member history timeline (spec §5.8). The date is already formatted as <c>DD.MM.YY</c>
///     for the card, the Discord id is present only when the row recorded one (historical rows have none), and
///     <see cref="AvatarUrl" /> is the member's avatar when the winner is still a known member. Payout status is
///     deliberately absent: it is admin-only and the member timeline must not leak it.
/// </summary>
public sealed record LotteryHistoryWinnerRow(
	string Id,
	string RoundKey,
	string Date,
	string DisplayName,
	ulong? DiscordId,
	string? AvatarUrl,
	int Guess,
	long GilPrize,
	IReadOnlyList<string> PrizeItems);

/// <summary>The summary numbers the history page shows above the timeline.</summary>
/// <param name="Winners">Winner rows recorded, not draws — a shared draw has two rows.</param>
/// <param name="Draws">Distinct rounds that recorded at least one winner.</param>
public sealed record LotteryHistoryTotalsResult(int Winners, int Draws, long CurrentPot, string? LastDrawRoundKey);

/// <summary>The outcome of a history read: a page, or a cursor this endpoint did not issue.</summary>
public interface ILotteryHistoryResult;

public sealed record LotteryHistoryPageResult(
	IReadOnlyList<LotteryHistoryWinnerRow> Items,
	string? NextCursor,
	LotteryHistoryTotalsResult Totals) : ILotteryHistoryResult;

public sealed record LotteryHistoryInvalidCursorResult(string Message) : ILotteryHistoryResult;
