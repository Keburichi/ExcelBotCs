using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The assembled announcement (spec §5.6, criteria 9 and 20): one plain message with the configured role ping,
///     the events/riddles mentions, the configured deadline, the pot summary, the five newest winners and the history
///     link. The two full snapshots are the accepted copy from #101, so a change to the generated body is a deliberate
///     change to the post and not an accident.
/// </summary>
public class LotteryAnnouncementComposerTests
{
	private const ulong RoleId = 111_222_333_444_555_666;
	private const ulong EventsId = 222_333_444_555_666_777;
	private const ulong RiddlesId = 333_444_555_666_777_888;
	private const ulong EmojiId = 444_555_666_777_888_999;
	private const string WebBase = "https://lottery.test";

	private static readonly LotteryAnnouncementDeployment Deployment =
		new(RoleId, EventsId, RiddlesId, EmojiId, WebBase, "Tuesday", "20:00");

	private static readonly LotteryWinner Aurora = Winner("2026-11-10", 42, 7001, "Aurora", 25_000_000,
		"Chaotic Mount");

	private static readonly LotteryWinner Siegward = Winner("2026-07-21", 56, null, "Siegward", 80_000_000,
		"Chaotic Mount");

	private static readonly LotteryWinner ElaraJune = Winner("2026-06-17", 99, null, "Elara", 6_700_000);
	private static readonly LotteryWinner ElaraMay = Winner("2026-05-26", 94, null, "Elara", 1_100_000);
	private static readonly LotteryWinner Skor = Winner("2026-05-12", 90, null, "Skor", 17_050_000);
	private static readonly LotteryWinner Oriel = Winner("2026-05-05", 24, null, "Oriel", 24_100_000);

	private const string GeneratedBody = """
		Make sure to look out for new posts in <#222333444555666777> to earn extra lottery entries and if you wanna increase the jackpot even more, go and solve the riddle in <#333444555666777888>!

		You can make the guesses for this week now! Remember the rules are:
		1) Number is a natural number between 1-99 (1 and 99 included)
		2) For every FC event you participate in, you get an extra guess to your one weekly guess!

		Check the pinned posts for an explanation on commands.
		Submit your guesses by Tuesday 20:00, when the draw runs.
		""";

	private static LotteryWinner Winner(string roundKey, int guess, ulong? discordId, string displayName, long gilPrize,
		params string[] bonusItems)
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

	[Fact]
	public void Compose_NoWin_MatchesTheAcceptedCopy()
	{
		var message = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			1,
			0,
			21_000_000,
			[],
			[Siegward, ElaraJune, ElaraMay, Skor, Oriel],
			[],
			Deployment));

		var expected = $"""
			Hello <@&{RoleId}>,

			the winning number was 1! So no one won this week again <:kekDoggo:{EmojiId}>
			The jackpot goes up to 21,000,000 Gil!!

			{GeneratedBody}

			Current pot: 21,000,000 Gil

			Last 5 winners:
			Siegward | 56 - 80,000,000 Gil + Chaotic Mount (21.07.26)
			Elara | 99 - 6,700,000 Gil (17.06.26)
			Elara | 94 - 1,100,000 Gil (26.05.26)
			Skor | 90 - 17,050,000 Gil (12.05.26)
			Oriel | 24 - 24,100,000 Gil (05.05.26)

			Full history: {WebBase}/lottery/history
			""";

		message.Text.ShouldBe(expected);
	}

	[Fact]
	public void Compose_Win_MatchesTheAcceptedCopy()
	{
		var message = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			42,
			25_000_000,
			1_000_000,
			[Aurora],
			[Aurora, Siegward, ElaraJune, ElaraMay, Skor],
			[],
			Deployment));

		var expected = $"""
			Hello <@&{RoleId}>,

			the winning number was 42! Congratulations to <@7001>! 🎉
			They take the 25,000,000 Gil pot plus the Chaotic Mount.
			The jackpot resets to 1,000,000 Gil for next week.

			{GeneratedBody}

			Current pot: 1,000,000 Gil

			Last 5 winners:
			Aurora | 42 - 25,000,000 Gil + Chaotic Mount (10.11.26)
			Siegward | 56 - 80,000,000 Gil + Chaotic Mount (21.07.26)
			Elara | 99 - 6,700,000 Gil (17.06.26)
			Elara | 94 - 1,100,000 Gil (26.05.26)
			Skor | 90 - 17,050,000 Gil (12.05.26)

			Full history: {WebBase}/lottery/history
			""";

		message.Text.ShouldBe(expected);
	}

	[Fact]
	public void Compose_SharedDraw_SplitsThePotAndNamesEachBonusItemNextToItsWinner()
	{
		var bea = Winner("2026-11-10", 42, 7002, "Bea", 12_500_000);
		var cass = Winner("2026-11-10", 42, 7003, "Cass", 12_500_000, "Fuax Mount");

		var message = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			42,
			25_000_000,
			1_000_000,
			[Aurora, bea, cass],
			[Aurora, bea, cass, Siegward, ElaraJune],
			[],
			Deployment));

		message.Text.ShouldContain(
			"the winning number was 42! Congratulations to <@7001>, <@7002>, and <@7003>! 🎉");
		message.Text.ShouldContain("They split the 25,000,000 Gil pot");
		message.Text.ShouldContain("Bonus item: <@7001> — Chaotic Mount");
		message.Text.ShouldContain("Bonus item: <@7003> — Fuax Mount.");
		message.Text.ShouldNotContain("They take the 25,000,000 Gil pot");

		// A shared draw contributes one row per winner, and the headline is the only place they are mentioned.
		message.Text.ShouldContain("Aurora | 42 - 25,000,000 Gil + Chaotic Mount (10.11.26)");
		message.Text.ShouldContain("Bea | 42 - 12,500,000 Gil (10.11.26)");
		message.Text.ShouldContain("Cass | 42 - 12,500,000 Gil + Fuax Mount (10.11.26)");
	}

	[Fact]
	public void Compose_SharedDraw_LeavesACustomTemplateWithoutTheTakePhrasingIntact()
	{
		// The split wording is a verb transform on the default copy, not a rewrite of arbitrary admin text: a template
		// that words the prize line differently must be emitted exactly as written, even for a shared draw.
		const string custom = "the winning number was {number}! {winners} share the {prize} pot{bonusItems}.";
		var first = Winner("2026-11-10", 42, 7001, "Aurora", 8_333_334);
		var bea = Winner("2026-11-10", 42, 7002, "Bea", 8_333_333);
		var cass = Winner("2026-11-10", 42, 7003, "Cass", 8_333_333);

		var message = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			custom,
			LotterySettingsDefaults.NoWinTemplate,
			42,
			25_000_000,
			1_000_000,
			[first, bea, cass],
			[first, bea, cass],
			[],
			Deployment));

		message.Text.ShouldContain(
			"the winning number was 42! <@7001>, <@7002>, and <@7003> share the 25,000,000 Gil pot.");
		message.Text.ShouldNotContain("They split the ");
		message.Text.ShouldNotContain("They take the ");
	}

	[Fact]
	public void Compose_ListsTheLastFiveInTheOrderTheyWereGiven()
	{
		var message = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			1,
			0,
			21_000_000,
			[],
			[Siegward, ElaraJune, ElaraMay, Skor, Oriel],
			[],
			Deployment));

		var rows = message.Text.Split('\n')
			.Where(line => line.Contains(" | ", StringComparison.Ordinal))
			.ToList();

		rows.Count.ShouldBe(5);
		rows[0].ShouldStartWith("Siegward | 56");
		rows[1].ShouldStartWith("Elara | 99");
		rows[2].ShouldStartWith("Elara | 94");
		rows[3].ShouldStartWith("Skor | 90");
		rows[4].ShouldStartWith("Oriel | 24");
	}

	[Fact]
	public void Compose_WithoutAnyWinnersYet_StillReadsCleanly()
	{
		var message = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			7,
			0,
			1_000_000,
			[],
			[],
			[],
			Deployment));

		message.Text.ShouldContain("Last 5 winners:\nNo winners yet.\n");
	}

	[Fact]
	public void Compose_WithAnUnconfiguredDeployment_FallsBackToPlainText()
	{
		var message = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			1,
			0,
			1_000_000,
			[],
			[Siegward],
			[],
			LotteryAnnouncementDeployment.Empty));

		message.Text.ShouldStartWith("Hello everyone,\n");
		message.Text.ShouldContain("new posts in the events channel to earn");
		message.Text.ShouldContain("the riddle in the riddles channel!");
		message.Text.ShouldContain(":kekDoggo:");
		message.Text.ShouldNotContain("<:kekDoggo:");
		message.Text.ShouldContain("Full history: /lottery/history");
	}

	[Fact]
	public void Compose_WithPooledPrizes_ShowsThemNextToThePot()
	{
		var onePrize = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			1,
			0,
			21_000_000,
			[],
			[Siegward],
			["Chaotic Mount"],
			Deployment));

		onePrize.Text.ShouldContain("Current pot: 21,000,000 Gil + Chaotic Mount");

		var twoPrizes = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			1,
			0,
			21_000_000,
			[],
			[Siegward],
			["Chaotic Mount", "Fuax Mount"],
			Deployment));

		twoPrizes.Text.ShouldContain("Current pot: 21,000,000 Gil + 2 prizes");
	}

	[Fact]
	public void Compose_UsesTheConfiguredRunDayAndTimeForTheDeadline()
	{
		var message = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			1,
			0,
			1_000_000,
			[],
			[Siegward],
			[],
			Deployment with { RunDay = "Sunday", RunTime = "21:30" }));

		message.Text.ShouldContain("Submit your guesses by Sunday 21:30, when the draw runs.");
		message.Text.ShouldNotContain("end of Monday");
	}

	[Fact]
	public void Compose_AnAdminTemplateIsFilledWithTheSameData()
	{
		var message = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			"Winner {winners} took {prize} at {emoji}. Next: {nextPot} (was {pot}, number {number}).",
			"Nobody at {number}: {winners}.",
			42,
			25_000_000,
			1_000_000,
			[Aurora],
			[Aurora],
			[],
			Deployment));

		message.Text.ShouldContain("<@7001> took 25,000,000 Gil at <:kekDoggo:" + EmojiId + ">.");
		message.Text.ShouldContain("Next: 1,000,000 (was 1,000,000 Gil, number 42).");
	}

	[Fact]
	public void Compose_ReportsVisibleCharacterCountsWithinTheSpecBudget()
	{
		var noWin = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			1,
			0,
			21_000_000,
			[],
			[Siegward, ElaraJune, ElaraMay, Skor, Oriel],
			[],
			Deployment));

		var win = LotteryAnnouncementComposer.Compose(new LotteryAnnouncementRenderRequest(
			LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate,
			42,
			25_000_000,
			1_000_000,
			[Aurora],
			[Aurora, Siegward, ElaraJune, ElaraMay, Skor],
			[],
			Deployment));

		// Spec §5.6: ~928 visible characters for a no-win, ~1,003 for a win.
		noWin.VisibleLength.ShouldBeInRange(900, 950);
		win.VisibleLength.ShouldBeInRange(970, 1_020);

		// Raw markup (role, channel and emoji ids) stays under ~1,100, far below Discord's 2,000 limit.
		noWin.RawLength.ShouldBeLessThan(1_100);
		win.RawLength.ShouldBeLessThan(1_100);
		win.VisibleLength.ShouldBeLessThan(win.RawLength);
		win.VisibleLength.ShouldBeGreaterThan(noWin.VisibleLength);
	}
}
