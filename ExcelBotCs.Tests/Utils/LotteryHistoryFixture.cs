using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Tests.Utils;

/// <summary>One row of the manual cutover seed: the shape <c>db.lottery_winners.insertOne</c> receives.</summary>
public sealed record LotteryHistorySeedRow(string RoundKey, string DisplayName, int Guess, long GilPrize,
	string[] BonusItems);

/// <summary>
///     The 35 historical winners transcribed from <c>docs/lottery/manual-seed.md</c> (spec §6 "Manual seed shapes",
///     §12 step 3). It is the acceptance fixture for the history page, the last-five announcement rows and the cutover:
///     the numbers are the operator's source of truth, so the expected totals, the newest rows and the shared draw are
///     literals here rather than being recomputed from the rows under test.
/// </summary>
public static class LotteryHistoryFixture
{
	/// <summary>Winner rows in the seed, not draws: rows 25 and 26 share one draw.</summary>
	public const int WinnerCount = 35;

	/// <summary>Distinct <c>RoundKey</c> values in the seed.</summary>
	public const int DrawCount = 34;

	/// <summary>The draw every history summary calls "last draw".</summary>
	public const string NewestRoundKey = "2026-07-21";

	/// <summary>The two rows that share round <c>2026-01-01</c> (the shared draw of §5.8).</summary>
	public const string SharedDrawRoundKey = "2026-01-01";

	/// <summary>The seed in document order, exactly as the manual seed table lists it.</summary>
	public static IReadOnlyList<LotteryHistorySeedRow> Rows { get; } =
	[
		new("2024-09-15", "Skor", 44, 1_500_000, []),
		new("2024-11-10", "Rhytar", 15, 7_000_000, []),
		new("2024-11-17", "Kei", 32, 1_000_000, []),
		new("2025-03-17", "Bonedaddy", 64, 21_000_000, []),
		new("2025-05-12", "Ta'veren", 28, 18_000_000, []),
		new("2025-05-18", "Mari", 82, 9_000_000, []),
		new("2025-06-09", "Aurora", 17, 7_500_000, []),
		new("2025-06-16", "Mari", 71, 5_000_000, []),
		new("2025-06-23", "Leon", 80, 3_000_000, []),
		new("2025-07-22", "H'molosi", 71, 26_000_000, []),
		new("2025-07-29", "H'molosi", 71, 1_000_000, []),
		new("2025-08-05", "Oriel", 26, 1_000_000, []),
		new("2025-08-12", "Nazha", 99, 8_000_000, []),
		new("2025-08-19", "Mina", 53, 1_000_000, []),
		new("2025-09-09", "Shivero", 55, 8_000_000, ["Yan"]),
		new("2025-09-16", "Keburichi", 21, 1_000_000, ["Figmental Weapon Coffer"]),
		new("2025-09-23", "Udoku", 61, 1_000_000, []),
		new("2025-09-30", "H'molosi", 43, 1_000_000, []),
		new("2025-11-04", "Amara", 35, 30_000_000, []),
		new("2025-11-18", "Aria", 35, 2_000_000, []),
		new("2025-11-25", "Mari", 30, 2_000_000, []),
		new("2025-12-02", "Siegward", 68, 5_000_000, []),
		new("2025-12-09", "Ta'veren", 82, 6_000_000, []),
		new("2025-12-23", "Mina", 56, 18_080_000, []),
		new("2026-01-01", "Karya", 66, 2_500_000, []),
		new("2026-01-01", "Uma", 66, 2_500_000, []),
		new("2026-02-10", "Ta'veren", 83, 14_300_000, ["Fuax Mount"]),
		new("2026-03-17", "Keburichi", 92, 33_000_000, ["Cosmic Mount"]),
		new("2026-03-24", "Oriana", 18, 1_000_000, []),
		new("2026-03-31", "Eva", 68, 2_450_000, []),
		new("2026-05-05", "Oriel", 24, 24_100_000, []),
		new("2026-05-12", "Skor", 90, 17_050_000, []),
		new("2026-05-26", "Elara", 94, 1_100_000, []),
		new("2026-06-17", "Elara", 99, 6_700_000, []),
		new("2026-07-21", "Siegward", 56, 80_000_000, ["Chaotic Mount"])
	];

	/// <summary>
	///     The newest five rows of the seed, newest first: what the announcement's "last five" block must show after the
	///     cutover (spec §5.6, criterion 9). Written out from the table, not derived from <see cref="Rows" />.
	/// </summary>
	public static IReadOnlyList<LotteryHistorySeedRow> LastFive { get; } =
	[
		new("2026-07-21", "Siegward", 56, 80_000_000, ["Chaotic Mount"]),
		new("2026-06-17", "Elara", 99, 6_700_000, []),
		new("2026-05-26", "Elara", 94, 1_100_000, []),
		new("2026-05-12", "Skor", 90, 17_050_000, []),
		new("2026-05-05", "Oriel", 24, 24_100_000, [])
	];

	/// <summary>
	///     Inserts the whole seed the way the operator does at cutover: one document per row, no Discord id, paid and the
	///     historical note. The repository stamps <c>DateCreated</c> in insertion order, which is the tie-breaker the
	///     history page uses inside a shared draw.
	/// </summary>
	public static async Task SeedAsync(ILotteryWinnerRepository repository)
	{
		foreach (var row in Rows) await repository.CreateAsync(ToWinner(row));
	}

	/// <summary>The winner document of one seed row.</summary>
	public static LotteryWinner ToWinner(LotteryHistorySeedRow row)
	{
		return new LotteryWinner
		{
			RoundKey = row.RoundKey,
			DiscordId = null,
			DisplayName = row.DisplayName,
			Guess = row.Guess,
			GilPrize = row.GilPrize,
			BonusItems = row.BonusItems,
			PayoutStatus = LotteryPayoutStatus.Paid,
			PayoutActorDiscordId = null,
			PayoutAt = null,
			PayoutNote = "historical record"
		};
	}
}
