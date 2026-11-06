using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Tests.Utils;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The §5.7 reminder copy. The expected strings are the spec's own lines, including its leading <c>&lt;</c> before
///     each guess line, so the test fails if the copy drifts.
/// </summary>
public class LotteryReminderComposerTests
{
	private static readonly LotteryReminderTarget Tuesday = new("2026-11-10",
		TestInstants.UtcTime("2026-11-10T19:00:00"), "Tuesday", "20:00", new DateOnly(2026, 11, 10));

	[Fact]
	public void Compose_UsesTheExactSpecCopy_GroupsByCountDescending()
	{
		var audience = new LotteryReminderAudience([
			new LotteryReminderRecipient(33, 1),
			new LotteryReminderRecipient(11, 2),
			new LotteryReminderRecipient(22, 2)
		]);

		var text = LotteryReminderComposer.Compose(audience, Tuesday);

		text.ShouldBe(
			"## Use your lottery guesses before Tuesday 20:00\n" +
			"<2 guesses remaining: <@11>, <@22>\n" +
			"<1 guess remaining: <@33>\n" +
			"Only members who opted in are pinged. The draw runs Tuesday at 20:00.");
	}

	[Fact]
	public void Compose_OrdersMembersInsideAGroupByDiscordId()
	{
		var audience = new LotteryReminderAudience([
			new LotteryReminderRecipient(300, 1),
			new LotteryReminderRecipient(100, 1),
			new LotteryReminderRecipient(200, 1)
		]);

		var text = LotteryReminderComposer.Compose(audience, Tuesday);

		text.ShouldContain("<1 guess remaining: <@100>, <@200>, <@300>");
	}

	[Fact]
	public void Compose_UsesTheRoundsDayAndTime_SoARescheduledRoundReadsCorrectly()
	{
		var rescheduled = new LotteryReminderTarget("2026-11-11",
			TestInstants.UtcTime("2026-11-11T19:30:00"), "Wednesday", "20:30", new DateOnly(2026, 11, 11));
		var audience = new LotteryReminderAudience([new LotteryReminderRecipient(11, 1)]);

		var text = LotteryReminderComposer.Compose(audience, rescheduled);

		text.ShouldStartWith("## Use your lottery guesses before Wednesday 20:30");
		text.ShouldEndWith("Only members who opted in are pinged. The draw runs Wednesday at 20:30.");
	}

	[Fact]
	public void Compose_WithNoRecipients_StillRendersTheCopyWithoutGroupLines()
	{
		var text = LotteryReminderComposer.Compose(LotteryReminderAudience.Empty, Tuesday);

		text.ShouldBe(
			"## Use your lottery guesses before Tuesday 20:00\n" +
			"Only members who opted in are pinged. The draw runs Tuesday at 20:00.");
	}

	[Fact]
	public void Compose_NeverCarriesTheAnnouncementReconciliationMarkers()
	{
		// A reminder must never be adopted as an announcement post (#111 matches those two markers).
		var audience = new LotteryReminderAudience([new LotteryReminderRecipient(11, 1)]);

		var text = LotteryReminderComposer.Compose(audience, Tuesday);

		text.ShouldNotContain("Check the pinned posts for an explanation on commands.");
		text.ShouldNotContain("Full history: ");
		text.ShouldNotContain("/lottery/history");
	}

	[Fact]
	public void Compose_StaysWellUnderTheDiscordLimit()
	{
		var recipients = Enumerable.Range(1, 200)
			.Select(index => new LotteryReminderRecipient((ulong)index, 1))
			.ToList();

		var text = LotteryReminderComposer.Compose(new LotteryReminderAudience(recipients), Tuesday);

		text.Length.ShouldBeLessThan(LotteryReminderComposer.MaxMessageLength);
	}
}
