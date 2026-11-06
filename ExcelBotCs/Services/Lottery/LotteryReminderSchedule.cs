using System.Globalization;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The reminder's instant for a draw: the most recent configured reminder day/time at or before the draw, and the
///     bounded window in which that reminder may still be posted. Pure, so the window edges and the DST-safe timezone
///     arithmetic can be tested without a clock (spec §5.7).
/// </summary>
public static class LotteryReminderSchedule
{
	/// <summary>The draw the reminder is about, with the day and time the post advertises read off its actual instant.</summary>
	public static LotteryReminderTarget TargetFor(LotterySchedule runSchedule, LotteryScheduleSlot slot)
	{
		return TargetFor(runSchedule, slot.RoundKey, slot.ScheduledAtUtc, slot.LocalDate);
	}

	/// <summary>A round that may have been rescheduled: its stored instant wins over the configured weekday.</summary>
	public static LotteryReminderTarget TargetFor(LotterySchedule runSchedule, LotteryRound round)
	{
		var scheduledAt = DateTime.SpecifyKind(round.ScheduledAt, DateTimeKind.Utc);
		return TargetFor(runSchedule, round.RoundKey, scheduledAt,
			runSchedule.LocalDate(new DateTimeOffset(scheduledAt, TimeSpan.Zero)));
	}

	private static LotteryReminderTarget TargetFor(LotterySchedule runSchedule, string roundKey,
		DateTime scheduledAtUtc, DateOnly localDate)
	{
		var scheduledAt = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);
		var local = TimeZoneInfo.ConvertTime(new DateTimeOffset(scheduledAt, TimeSpan.Zero), runSchedule.Timezone);

		return new LotteryReminderTarget(roundKey, scheduledAt, local.DayOfWeek.ToString(),
			local.ToString("HH:mm", CultureInfo.InvariantCulture), localDate);
	}

	/// <summary>
	///     The window the reminder may be posted in: it opens at the configured reminder instant, is capped by
	///     <paramref name="catchUpWindow" />, and always closes at the draw so a restart cannot remind members about a
	///     round that already ran.
	/// </summary>
	public static LotteryReminderWindow WindowFor(LotterySchedule reminderSchedule, LotteryReminderTarget target,
		TimeSpan catchUpWindow)
	{
		var targetInstant = new DateTimeOffset(target.ScheduledAtUtc, TimeSpan.Zero);
		var reminderSlot = reminderSchedule.MostRecentDueSlot(targetInstant);

		var startsAt = reminderSlot.ScheduledAtUtc;
		var endsAt = startsAt + catchUpWindow;
		if (target.ScheduledAtUtc < endsAt) endsAt = target.ScheduledAtUtc;

		return new LotteryReminderWindow(startsAt, endsAt);
	}
}

/// <summary>When the reminder for a draw may be posted; empty when the configured reminder is at or after the draw.</summary>
public sealed record LotteryReminderWindow(DateTime StartsAtUtc, DateTime EndsAtUtc)
{
	public bool IsEmpty => EndsAtUtc <= StartsAtUtc;

	public bool IsOpen(DateTime nowUtc)
	{
		return nowUtc >= StartsAtUtc && nowUtc < EndsAtUtc;
	}
}
