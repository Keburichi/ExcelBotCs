using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The sample data behind a preview and the save-time length check (spec §5.6, §5.9 Messages tab). The values are
///     the worked example from #101 — a 25,000,000 Gil win with a prize item and a 21,000,000 Gil no-win — so the
///     rendered preview and the character counts match the accepted copy. The ids are 18 digits and the base URL is
///     realistic, which makes the length a fair upper bound for a production post.
/// </summary>
public static class LotteryAnnouncementSamples
{
	public const long WinPrizePot = 25_000_000;
	public const long NoWinPot = 21_000_000;
	public const long BasePot = 1_000_000;

	/// <summary>The deployment used by previews and template measurements.</summary>
	public static LotteryAnnouncementDeployment Deployment { get; } = new(
		123456789012345678UL,
		123456789012345678UL,
		123456789012345678UL,
		123456789012345678UL,
		"https://excelbot.example.com",
		"Tuesday",
		"20:00");

	/// <summary>The win from the worked example: one winner, one bonus item.</summary>
	public static LotteryWinner SampleWinner => SampleWinnerWith("Chaotic Mount");

	/// <summary>The worked example's winner with the given bonus items (the preview uses the real pool).</summary>
	public static LotteryWinner SampleWinnerWith(params string[] bonusItems)
	{
		return Winner("2026-11-10", 42, 7_001UL, "Aurora", 25_000_000, bonusItems);
	}

	/// <summary>The five most recent winners of the worked example, newest first.</summary>
	public static IReadOnlyList<LotteryWinner> HistoricalWinners { get; } =
	[
		Winner("2026-07-21", 56, null, "Siegward", 80_000_000, ["Chaotic Mount"]),
		Winner("2026-06-17", 99, null, "Elara", 6_700_000, []),
		Winner("2026-05-26", 94, null, "Elara", 1_100_000, []),
		Winner("2026-05-12", 90, null, "Skor", 17_050_000, []),
		Winner("2026-05-05", 24, null, "Oriel", 24_100_000, [])
	];

	/// <summary>A win preview request; the current pot is the reset pot unless the caller knows the live value.</summary>
	public static LotteryAnnouncementRenderRequest WinRequest(string? template = null, long? prizePot = null,
		long? currentPot = null, IReadOnlyList<string>? poolPrizes = null,
		IReadOnlyList<LotteryWinner>? winners = null, IReadOnlyList<LotteryWinner>? lastFive = null)
	{
		var sampleWinners = winners ?? [SampleWinner];

		return new LotteryAnnouncementRenderRequest(
			template ?? LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			42,
			prizePot ?? WinPrizePot,
			currentPot ?? BasePot,
			sampleWinners,
			lastFive ?? [.. sampleWinners, .. HistoricalWinners.Take(Math.Max(0, 5 - sampleWinners.Count))],
			poolPrizes ?? [],
			Deployment);
	}

	/// <summary>A no-win preview request; the current pot is the grown pot unless the caller knows the live value.</summary>
	public static LotteryAnnouncementRenderRequest NoWinRequest(string? template = null, long? currentPot = null,
		IReadOnlyList<string>? poolPrizes = null, int winningNumber = 1)
	{
		return new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			template ?? LotterySettingsDefaults.NoWinTemplate,
			winningNumber,
			0,
			currentPot ?? NoWinPot,
			[],
			HistoricalWinners,
			poolPrizes ?? [],
			Deployment);
	}

	/// <summary>The assembled message for one template, used by the save-time length check.</summary>
	public static LotteryAssembledMessage Compose(string template, bool isWin)
	{
		var request = isWin ? WinRequest(template) : NoWinRequest(template);
		return LotteryAnnouncementComposer.Compose(request);
	}

	private static LotteryWinner Winner(string roundKey, int guess, ulong? discordId, string displayName, long gilPrize,
		string[] bonusItems)
	{
		return new LotteryWinner
		{
			RoundKey = roundKey,
			DiscordId = discordId,
			DisplayName = displayName,
			Guess = guess,
			GilPrize = gilPrize,
			BonusItems = bonusItems,
			PayoutStatus = LotteryPayoutStatus.Paid
		};
	}
}
