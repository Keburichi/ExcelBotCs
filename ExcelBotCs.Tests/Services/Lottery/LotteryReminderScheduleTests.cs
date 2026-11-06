using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Tests.Utils;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The reminder window arithmetic (spec §5.7): it opens at the configured reminder instant, is capped by the
///     catch-up window, and always closes at the draw. All instants are checked against the Europe/Berlin schedule.
/// </summary>
public class LotteryReminderScheduleTests
{
	private static readonly LotterySchedule RunSchedule =
		LotterySchedule.Create("Tuesday", "20:00", "Europe/Berlin");

	private static readonly LotterySchedule DefaultReminder =
		LotterySchedule.Create("Monday", "18:00", "Europe/Berlin");

	private static readonly TimeSpan CatchUp = TimeSpan.FromHours(24);

	[Fact]
	public void TargetFor_ReadsTheRoundsLocalDayAndTime()
	{
		var slot = RunSchedule.SlotForDate(new DateOnly(2026, 11, 10));

		var target = LotteryReminderSchedule.TargetFor(RunSchedule, slot);

		target.RoundKey.ShouldBe("2026-11-10");
		target.RunDay.ShouldBe("Tuesday");
		target.RunTime.ShouldBe("20:00");
		target.LocalDate.ShouldBe(new DateOnly(2026, 11, 10));
	}

	[Fact]
	public void TargetFor_ARescheduledRoundUsesItsNewDayAndTime()
	{
		var round = new LotteryRound
		{
			RoundKey = "2026-11-11",
			// Wednesday 20:30 Europe/Berlin (CET, +1).
			ScheduledAt = TestInstants.UtcTime("2026-11-11T19:30:00")
		};

		var target = LotteryReminderSchedule.TargetFor(RunSchedule, round);

		target.RunDay.ShouldBe("Wednesday");
		target.RunTime.ShouldBe("20:30");
		target.RoundKey.ShouldBe("2026-11-11");
	}

	[Fact]
	public void WindowFor_OpensAtTheConfiguredReminderInstant_AndCapsAtTheCatchUpWindow()
	{
		var target = LotteryReminderSchedule.TargetFor(RunSchedule,
			RunSchedule.SlotForDate(new DateOnly(2026, 11, 10)));

		var window = LotteryReminderSchedule.WindowFor(DefaultReminder, target, CatchUp);

		// Monday 18:00 Europe/Berlin is 17:00 UTC; 24 hours later is Tuesday 17:00 UTC, before the 19:00 UTC draw.
		window.StartsAtUtc.ShouldBe(TestInstants.UtcTime("2026-11-09T17:00:00"));
		window.EndsAtUtc.ShouldBe(TestInstants.UtcTime("2026-11-10T17:00:00"));
	}

	[Fact]
	public void WindowFor_AlwaysClosesAtTheDraw()
	{
		var target = LotteryReminderSchedule.TargetFor(RunSchedule,
			RunSchedule.SlotForDate(new DateOnly(2026, 11, 10)));
		var lateReminder = LotterySchedule.Create("Tuesday", "19:50", "Europe/Berlin");

		var window = LotteryReminderSchedule.WindowFor(lateReminder, target, CatchUp);

		window.StartsAtUtc.ShouldBe(TestInstants.UtcTime("2026-11-10T18:50:00"));
		window.EndsAtUtc.ShouldBe(TestInstants.UtcTime("2026-11-10T19:00:00"));
	}

	[Fact]
	public void WindowFor_IsEmptyWhenTheReminderIsConfiguredAtOrAfterTheDraw()
	{
		var target = LotteryReminderSchedule.TargetFor(RunSchedule,
			RunSchedule.SlotForDate(new DateOnly(2026, 11, 10)));
		var tooLate = LotterySchedule.Create("Tuesday", "20:00", "Europe/Berlin");

		var window = LotteryReminderSchedule.WindowFor(tooLate, target, CatchUp);

		window.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public void IsOpen_IncludesTheStartAndExcludesTheEnd()
	{
		var window = new LotteryReminderWindow(TestInstants.UtcTime("2026-11-09T17:00:00"),
			TestInstants.UtcTime("2026-11-10T17:00:00"));

		window.IsOpen(TestInstants.UtcTime("2026-11-09T16:59:59")).ShouldBeFalse();
		window.IsOpen(TestInstants.UtcTime("2026-11-09T17:00:00")).ShouldBeTrue();
		window.IsOpen(TestInstants.UtcTime("2026-11-10T16:59:59")).ShouldBeTrue();
		window.IsOpen(TestInstants.UtcTime("2026-11-10T17:00:00")).ShouldBeFalse();
	}
}
