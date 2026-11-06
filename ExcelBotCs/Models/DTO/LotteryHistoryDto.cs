namespace ExcelBotCs.Models.DTO;

/// <summary>
///     One winner card of the member history timeline (spec §5.8). <see cref="Date" /> is the card date
///     (<c>DD.MM.YY</c>), <see cref="DiscordId" /> is a string because a Discord snowflake exceeds the range a
///     JavaScript number represents exactly, and <see cref="AvatarUrl" /> is present only when the winner is still a
///     member. Payout status is admin-only and intentionally absent.
/// </summary>
public sealed record LotteryHistoryItem(
    string Id,
    string RoundKey,
    string Date,
    string DisplayName,
    string? DiscordId,
    string? AvatarUrl,
    int Guess,
    long GilPrize,
    IReadOnlyList<string> PrizeItems);

/// <summary>The summary numbers above the timeline: rows recorded (a shared draw counts twice), distinct draws, the
/// current pot and the newest round.</summary>
public sealed record LotteryHistoryTotals(int Winners, int Draws, long CurrentPot, string? LastDrawRoundKey);

/// <summary>One page of the winner timeline. <see cref="NextCursor" /> is absent on the last page.</summary>
public sealed record LotteryHistoryPage(
    IReadOnlyList<LotteryHistoryItem> Items,
    string? NextCursor,
    LotteryHistoryTotals Totals);

/// <summary>A typed, user-facing failure; the member UI shows <see cref="Message" />.</summary>
public sealed record LotteryHistoryError(string Error, string Message);
