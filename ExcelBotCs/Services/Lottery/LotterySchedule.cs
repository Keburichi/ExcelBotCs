using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     One scheduled run: the instant it is due in UTC, the round key it creates and its local scheduled date.
/// </summary>
public sealed record LotteryScheduleSlot(DateTime ScheduledAtUtc, string RoundKey, DateOnly LocalDate);

/// <summary>
///     The lottery schedule arithmetic (spec §5.1): the run is a <em>local</em> wall-clock time in the configured
///     timezone, the round key is that local date, and DST is resolved by the timezone rather than by adding a fixed
///     offset. This type is pure and takes every instant as an argument, so it can be unit-tested without a clock.
///     <para>
///         DST rules: a run time that falls into the spring-forward gap moves forward by the size of the gap (so
///         02:30 becomes 03:30), and a run time that happens twice on the fall-back day uses the first occurrence.
///         The default 20:00 Tuesday run never lands in either window, but the admin can configure any time.
///     </para>
/// </summary>
public sealed class LotterySchedule
{
	/// <summary>
	///     A defensive bound for <see cref="SlotsBefore" />: a weekly schedule needs 520 slots for ten years, so this
	///     only exists to keep a misconfigured schedule from looping forever.
	/// </summary>
	public const int MaxSlotsPerQuery = 1_000;

	private LotterySchedule(DayOfWeek runDay, TimeOnly runTime, TimeZoneInfo timezone, string timezoneId)
	{
		RunDay = runDay;
		RunTime = runTime;
		Timezone = timezone;
		TimezoneId = timezoneId;
	}

	public DayOfWeek RunDay { get; }
	public TimeOnly RunTime { get; }
	public TimeZoneInfo Timezone { get; }
	public string TimezoneId { get; }

	/// <summary>The weekly period of the schedule.</summary>
	public TimeSpan Period => TimeSpan.FromDays(7);

	/// <summary>Builds the schedule from already-validated settings.</summary>
	public static LotterySchedule CreateFrom(LotteryEffectiveSettings settings)
	{
		return Create(settings.RunDay, settings.RunTime, settings.RunTimezone);
	}

	/// <summary>
	///     Builds the schedule. The stored values are validated by <see cref="LotterySettingsValidator" /> before they
	///     reach here; the exceptions are for a caller that skips that gate, not for normal operation.
	/// </summary>
	public static LotterySchedule Create(string runDay, string runTime, string timezoneId)
	{
		var day = LotterySettingsValidator.ValidateDay(runDay);
		if (!day.IsValid) throw new ArgumentException(day.Error, nameof(runDay));

		var time = LotterySettingsValidator.ValidateTime(runTime);
		if (!time.IsValid) throw new ArgumentException(time.Error, nameof(runTime));

		var timezone = LotterySettingsValidator.ValidateTimezone(timezoneId);
		if (!timezone.IsValid) throw new ArgumentException(timezone.Error, nameof(timezoneId));

		return new LotterySchedule(
			Enum.Parse<DayOfWeek>(day.Normalized),
			TimeOnly.ParseExact(time.Normalized, "HH:mm", System.Globalization.CultureInfo.InvariantCulture),
			TimeZoneInfo.FindSystemTimeZoneById(timezone.Normalized),
			timezone.Normalized);
	}

	/// <summary>The round key for an arbitrary instant: its date in the configured run timezone.</summary>
	public string RoundKeyFor(DateTimeOffset instant)
	{
		return LotteryRoundKey.FromLocalDate(LocalDate(instant));
	}

	/// <summary>The local date of an instant in the run timezone.</summary>
	public DateOnly LocalDate(DateTimeOffset instant)
	{
		var local = TimeZoneInfo.ConvertTime(instant.ToUniversalTime(), Timezone);
		return DateOnly.FromDateTime(local.DateTime);
	}

	/// <summary>The slot on a specific local date, whether or not it is the configured weekday.</summary>
	public LotteryScheduleSlot SlotForDate(DateOnly localDate)
	{
		var local = localDate.ToDateTime(RunTime);

		// The run is a wall-clock time: resolve it through the timezone so CET/CEST are respected.
		if (Timezone.IsInvalidTime(local))
		{
			// The clock jumps forward over this wall time. Move past the gap by the size of the jump.
			var before = Timezone.GetUtcOffset(local.AddHours(-1));
			var after = Timezone.GetUtcOffset(local.AddHours(1));
			var shift = after - before;
			if (shift <= TimeSpan.Zero) shift = TimeSpan.FromHours(1);
			local = local.Add(shift);
		}

		var offset = Timezone.IsAmbiguousTime(local)
			// Ambiguous means the wall time happens twice; the first occurrence is the larger (DST) offset.
			? Timezone.GetAmbiguousTimeOffsets(local).Max()
			: Timezone.GetUtcOffset(local);

		var scheduledAtUtc = new DateTimeOffset(local, offset).ToUniversalTime().UtcDateTime;

		return new LotteryScheduleSlot(scheduledAtUtc, LotteryRoundKey.FromLocalDate(localDate), localDate);
	}

	/// <summary>The first slot strictly after <paramref name="now" />.</summary>
	public LotteryScheduleSlot NextSlotAfter(DateTimeOffset now)
	{
		var localDate = LocalDate(now);
		for (var offset = 0; offset <= 7; offset++)
		{
			var candidateDate = localDate.AddDays(offset);
			if (candidateDate.DayOfWeek != RunDay) continue;

			var candidate = SlotForDate(candidateDate);
			if (candidate.ScheduledAtUtc > now.UtcDateTime) return candidate;
		}

		// Unreachable: within eight consecutive dates every weekday occurs exactly once.
		throw new InvalidOperationException($"The {RunDay} schedule has no slot within a week of {localDate}.");
	}

	/// <summary>
	///     The most recent slot at or before <paramref name="now" />: the run that is due. When the run instant has just
	///     passed this is today's slot, which is what makes the scheduler run at the configured time.
	/// </summary>
	public LotteryScheduleSlot MostRecentDueSlot(DateTimeOffset now)
	{
		var localDate = LocalDate(now);
		for (var offset = 0; offset <= 7; offset++)
		{
			var candidateDate = localDate.AddDays(-offset);
			if (candidateDate.DayOfWeek != RunDay) continue;

			var candidate = SlotForDate(candidateDate);
			if (candidate.ScheduledAtUtc <= now.UtcDateTime) return candidate;
		}

		// Unreachable: the previous week's slot is always at or before now.
		throw new InvalidOperationException($"The {RunDay} schedule has no due slot within a week of {localDate}.");
	}

	/// <summary>
	///     The weekly slots before <paramref name="slot" />, newest first. Used by the catch-up selection to find the
	///     dates that must be recorded as <c>skipped</c> when several runs were missed.
	/// </summary>
	public IReadOnlyList<LotteryScheduleSlot> SlotsBefore(LotteryScheduleSlot slot, int count)
	{
		if (count <= 0) return [];

		var bounded = Math.Min(count, MaxSlotsPerQuery);
		var slots = new List<LotteryScheduleSlot>(bounded);
		for (var index = 1; index <= bounded; index++)
			slots.Add(SlotForDate(slot.LocalDate.AddDays(-7 * index)));

		return slots;
	}

	/// <summary>
	///     Whether an instant is on the same local date as the slot: the "the bot started later on the scheduled date"
	///     catch-up rule (§5.1).
	/// </summary>
	public bool IsOnScheduledDate(LotteryScheduleSlot slot, DateTimeOffset now)
	{
		return RoundKeyFor(now) == slot.RoundKey;
	}
}
