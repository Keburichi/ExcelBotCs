using ExcelBotCs.Services.Lottery;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The schedule arithmetic the scheduler depends on: when the next run is, which run slot is currently due,
///     which round key a slot has, and how both Europe/Berlin DST transitions behave. Everything here is pure, so
///     no test needs to wait for a real Tuesday.
/// </summary>
public class LotteryScheduleTests
{
	private static LotterySchedule TuesdayAt20()
	{
		return LotterySchedule.Create("Tuesday", "20:00", "Europe/Berlin");
	}

	private static DateTimeOffset Utc(string instant)
	{
		return DateTimeOffset.Parse(instant + "Z", System.Globalization.CultureInfo.InvariantCulture);
	}

	private static DateTime UtcTime(string instant)
	{
		return Utc(instant).UtcDateTime;
	}

	[Fact]
	public void NextSlotAfter_BeforeTheRunTimeOnTheRunDay_IsTodaysSlot()
	{
		var schedule = TuesdayAt20();

		var slot = schedule.NextSlotAfter(Utc("2026-11-10T18:59:00"));

		slot.ScheduledAtUtc.ShouldBe(UtcTime("2026-11-10T19:00:00"));
		slot.RoundKey.ShouldBe("2026-11-10");
		slot.LocalDate.ShouldBe(new DateOnly(2026, 11, 10));
	}

	[Fact]
	public void NextSlotAfter_AtTheRunInstant_IsNextWeeksSlot()
	{
		var schedule = TuesdayAt20();

		var slot = schedule.NextSlotAfter(Utc("2026-11-10T19:00:00"));

		slot.ScheduledAtUtc.ShouldBe(UtcTime("2026-11-17T19:00:00"));
		slot.RoundKey.ShouldBe("2026-11-17");
	}

	[Fact]
	public void NextSlotAfter_AfterTheRunTime_IsNextWeeksSlot()
	{
		var schedule = TuesdayAt20();

		var slot = schedule.NextSlotAfter(Utc("2026-11-10T21:30:00"));

		slot.RoundKey.ShouldBe("2026-11-17");
	}

	[Fact]
	public void MostRecentDueSlot_BeforeTheRunTimeOnTheRunDay_IsLastWeeksSlot()
	{
		var schedule = TuesdayAt20();

		var slot = schedule.MostRecentDueSlot(Utc("2026-11-10T18:59:00"));

		slot.ScheduledAtUtc.ShouldBe(UtcTime("2026-11-03T19:00:00"));
		slot.RoundKey.ShouldBe("2026-11-03");
	}

	[Fact]
	public void MostRecentDueSlot_AtTheRunInstant_IsTodaysSlot()
	{
		var schedule = TuesdayAt20();

		var slot = schedule.MostRecentDueSlot(Utc("2026-11-10T19:00:00"));

		slot.ScheduledAtUtc.ShouldBe(UtcTime("2026-11-10T19:00:00"));
		slot.RoundKey.ShouldBe("2026-11-10");
	}

	[Fact]
	public void RoundKeyFor_IsTheLocalDateNotTheUtcDate()
	{
		var schedule = LotterySchedule.Create("Tuesday", "23:30", "Europe/Berlin");

		// 22:30 UTC on 10 November is 23:30 in Berlin, still the 10th locally.
		schedule.RoundKeyFor(Utc("2026-11-10T22:30:00")).ShouldBe("2026-11-10");

		// 23:30 UTC is 00:30 on the 11th in Berlin, so it belongs to the next local date.
		schedule.RoundKeyFor(Utc("2026-11-10T23:30:00")).ShouldBe("2026-11-11");
	}

	[Fact]
	public void NextSlotAfter_AfterTheSpringForward_KeepsTheLocalRunTime()
	{
		var schedule = TuesdayAt20();
		var now = Utc("2026-03-29T12:00:00"); // The transition Sunday, after 02:00 local.

		var slot = schedule.NextSlotAfter(now);

		// CEST (UTC+2): 20:00 local is 18:00 UTC, one hour earlier than the winter slot.
		slot.ScheduledAtUtc.ShouldBe(UtcTime("2026-03-31T18:00:00"));
		slot.RoundKey.ShouldBe("2026-03-31");
	}

	[Fact]
	public void NextSlotAfter_AfterTheFallBack_KeepsTheLocalRunTime()
	{
		var schedule = TuesdayAt20();
		var now = Utc("2026-10-25T12:00:00"); // The fall-back Sunday.

		var slot = schedule.NextSlotAfter(now);

		// CET (UTC+1): 20:00 local is 19:00 UTC again.
		slot.ScheduledAtUtc.ShouldBe(UtcTime("2026-10-27T19:00:00"));
		slot.RoundKey.ShouldBe("2026-10-27");
	}

	[Fact]
	public void NextSlotAfter_AcrossTheSpringTransition_UsesTheOffsetInForceOnTheSlot()
	{
		var schedule = TuesdayAt20();

		var slot = schedule.NextSlotAfter(Utc("2026-03-24T20:00:00"));

		slot.ScheduledAtUtc.ShouldBe(UtcTime("2026-03-31T18:00:00"));
	}

	[Fact]
	public void SlotForDate_RunTimeInsideTheSpringForwardGap_MovesToTheFirstValidInstant()
	{
		var schedule = LotterySchedule.Create("Sunday", "02:30", "Europe/Berlin");

		var slot = schedule.SlotForDate(new DateOnly(2026, 3, 29));

		// 02:30 does not exist that morning; the run happens at 03:30 CEST = 01:30 UTC on the same local date.
		slot.ScheduledAtUtc.ShouldBe(UtcTime("2026-03-29T01:30:00"));
		slot.RoundKey.ShouldBe("2026-03-29");
	}

	[Fact]
	public void SlotForDate_RunTimeInsideTheFallBackHour_UsesTheFirstOccurrence()
	{
		var schedule = LotterySchedule.Create("Sunday", "02:30", "Europe/Berlin");

		var slot = schedule.SlotForDate(new DateOnly(2026, 10, 25));

		// 02:30 happens twice; the first occurrence is 00:30 UTC (CEST).
		slot.ScheduledAtUtc.ShouldBe(UtcTime("2026-10-25T00:30:00"));
		slot.RoundKey.ShouldBe("2026-10-25");
	}

	[Fact]
	public void SlotsBefore_ReturnsTheWeeklySlotsNewestFirst()
	{
		var schedule = TuesdayAt20();
		var due = schedule.MostRecentDueSlot(Utc("2026-11-10T19:00:00"));

		var slots = schedule.SlotsBefore(due, 3);

		slots.Select(slot => slot.RoundKey).ShouldBe(["2026-11-03", "2026-10-27", "2026-10-20"]);
	}

	[Fact]
	public void SlotsBefore_CrossesTheDstTransitionWithTheLocalDate()
	{
		var schedule = TuesdayAt20();
		var due = schedule.MostRecentDueSlot(Utc("2026-11-03T19:00:00"));

		var slots = schedule.SlotsBefore(due, 6);

		slots.Select(slot => slot.RoundKey)
			.ShouldBe(["2026-10-27", "2026-10-20", "2026-10-13", "2026-10-06", "2026-09-29", "2026-09-22"]);
		slots[0].ScheduledAtUtc.ShouldBe(UtcTime("2026-10-27T19:00:00"));
		slots[1].ScheduledAtUtc.ShouldBe(UtcTime("2026-10-20T18:00:00"));
	}

	[Fact]
	public void CreateFrom_UsesTheEffectiveSettings()
	{
		var settings = new ExcelBotCs.Services.Lottery.Records.LotteryEffectiveSettings("Wednesday", "19:15",
			"Europe/Berlin", "Monday", "18:00", "Europe/Berlin", false, false, "win", "no-win", 0, null, true, false,
			1_000_000, 1_000_000, []);

		var schedule = LotterySchedule.CreateFrom(settings);

		schedule.RunDay.ShouldBe(DayOfWeek.Wednesday);
		schedule.RunTime.ShouldBe(new TimeOnly(19, 15));
		schedule.TimezoneId.ShouldBe("Europe/Berlin");
		schedule.NextSlotAfter(Utc("2026-11-10T12:00:00")).RoundKey.ShouldBe("2026-11-11");
	}
}
