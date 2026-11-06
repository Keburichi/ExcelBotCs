using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The round state machine (spec §5.10) as a pure decision: given the clock, the schedule and the rounds that are
///     currently in the database, it returns what the scheduler should do on this tick. Keeping the decision out of
///     the runner makes the whole transition table unit-testable without Mongo, a real Tuesday or a clock.
/// </summary>
public static class LotteryRoundPlanner
{
	/// <summary>
	///     How many missed dates catch-up will record as <c>skipped</c>. A weekly schedule needs a year of downtime to
	///     reach this, and a longer gap is not useful history; the run itself still catches up the most recent date.
	/// </summary>
	public const int MaxCatchUpSkips = 52;

	public static LotteryTickPlan Plan(
		LotterySchedule schedule,
		DateTimeOffset now,
		LotteryTickState state,
		bool automationEnabled,
		bool pauseAutomation,
		bool dryRun)
	{
		// The deployment kill switch is the final override, then the admin pause. Neither writes anything.
		if (!automationEnabled)
			return LotteryTickPlan.Of(LotteryTickAction.Disabled, dryRun,
				"Lottery automation is disabled (Lottery:Automation:Enabled=false).");

		if (pauseAutomation)
			return LotteryTickPlan.Of(LotteryTickAction.Paused, dryRun,
				"Lottery automation is paused by the stored settings.");

		// An unfinished round owns the scheduler: retry or resume it before considering the next slot.
		if (state.BlockingRound is { } pending)
		{
			if (!LotteryRoundStatus.CanResumeAutomatically(pending.Status))
				return LotteryTickPlan.Of(LotteryTickAction.Blocked, dryRun,
					$"Round {pending.RoundKey} failed at the {pending.FailedStep ?? "unknown"} step after " +
					$"{pending.Attempts} attempt(s); it blocks the schedule until an admin retries or voids it.",
					pending);

			if (pending.NextRetryAt is { } nextRetryAt && nextRetryAt > now.UtcDateTime)
				return LotteryTickPlan.Of(LotteryTickAction.WaitingForRetry, dryRun,
					$"Round {pending.RoundKey} is waiting for its retry at {nextRetryAt:O}.", pending);

			return pending.Status switch
			{
				LotteryRoundStatus.Drawing => LotteryTickPlan.Of(LotteryTickAction.ResumeDraw, dryRun,
					$"Resuming the draw of round {pending.RoundKey}.", pending),
				LotteryRoundStatus.Drawn => LotteryTickPlan.Of(LotteryTickAction.ResumeAnnouncement, dryRun,
					$"Resuming the announcement of round {pending.RoundKey}.", pending),
				_ => LotteryTickPlan.Of(LotteryTickAction.ResumeCleanup, dryRun,
					$"Resuming the cleanup of round {pending.RoundKey}.", pending)
			};
		}

		// A round that was scheduled for an arbitrary instant (a reschedule) runs when that instant is due, even though
		// its round key is the date it originally belonged to.
		if (state.ScheduledDueRound is { } scheduled)
			return LotteryTickPlan.Of(LotteryTickAction.Run, dryRun,
				$"Round {scheduled.RoundKey} is due (scheduled for {scheduled.ScheduledAt:O}).",
				round: scheduled, slot: SlotOf(schedule, scheduled));

		var dueSlot = schedule.MostRecentDueSlot(now);

		// A dry run persists no round row, so the previewed date would look like a missed slot and be drawn live after
		// the promotion. The marker the dry-run tick writes makes that slot count as satisfied instead (§12 step 7:
		// "the first live run is the next scheduled round").
		if (state.LastDryRunRoundKey is { } dryRunKey && string.Equals(dryRunKey, dueSlot.RoundKey,
			    StringComparison.Ordinal))
			return LotteryTickPlan.Of(LotteryTickAction.Nothing, dryRun,
				$"The due round {dueSlot.RoundKey} was a dry-run week; it is not replayed live. " +
				"The next live run is the next scheduled round.");

		// The due date already has a round in any state: it ran, it was skipped, it was voided, or it is scheduled for
		// later. Nothing to do.
		if (state.DueRound is not null || state.LatestRound is null)
			return state.DueRound is not null
				? LotteryTickPlan.Of(LotteryTickAction.Nothing, dryRun,
					$"The due round {dueSlot.RoundKey} is already {state.DueRound.Status}.")
				: PlanFirstRound(schedule, now, dueSlot, dryRun);

		// An older round is the most recent thing the scheduler knows about. Everything between it and the due slot was
		// missed: record those dates as skipped and run only the most recent one once.
		if (LotteryRoundKey.Compare(state.LatestRound.RoundKey, dueSlot.RoundKey) >= 0)
			return LotteryTickPlan.Of(LotteryTickAction.Nothing, dryRun,
				$"Nothing due: the newest round is {state.LatestRound.RoundKey} and the due slot is {dueSlot.RoundKey}.");

		var skipped = schedule.SlotsBefore(dueSlot, MaxCatchUpSkips)
			.Where(slot => LotteryRoundKey.Compare(slot.RoundKey, state.LatestRound.RoundKey) > 0)
			.OrderBy(slot => slot.RoundKey, StringComparer.Ordinal)
			.ToList();

		return LotteryTickPlan.Of(LotteryTickAction.Run, dryRun,
			$"Catching up the most recent missed round {dueSlot.RoundKey}; " +
			$"{skipped.Count} older missed date(s) will be marked skipped.",
			slot: dueSlot, slotsToSkip: skipped);
	}

	/// <summary>
	///     A deployment with no rounds at all has no downtime to catch up. It runs the due slot only when the bot started
	///     later on the scheduled date (§5.1); otherwise the first run is the next slot.
	/// </summary>
	private static LotteryTickPlan PlanFirstRound(LotterySchedule schedule, DateTimeOffset now,
		LotteryScheduleSlot dueSlot, bool dryRun)
	{
		return schedule.IsOnScheduledDate(dueSlot, now)
			? LotteryTickPlan.Of(LotteryTickAction.Run, dryRun,
				$"The first round {dueSlot.RoundKey} is due today.", slot: dueSlot)
			: LotteryTickPlan.Of(LotteryTickAction.Nothing, dryRun,
				$"No rounds yet and the due slot {dueSlot.RoundKey} is not today; waiting for the next slot.");
	}

	/// <summary>The slot a round's document describes; used to resume a round whose date is not the current due slot.</summary>
	public static LotteryScheduleSlot SlotOf(LotterySchedule schedule, LotteryRound round)
	{
		var scheduledAt = DateTime.SpecifyKind(round.ScheduledAt, DateTimeKind.Utc);
		return new LotteryScheduleSlot(scheduledAt, round.RoundKey,
			schedule.LocalDate(new DateTimeOffset(scheduledAt, TimeSpan.Zero)));
	}
}

/// <summary>The data the planner needs about the rounds that exist; the runner gathers it with four point reads.</summary>
public sealed record LotteryTickState(
	LotteryRound? BlockingRound,
	LotteryRound? ScheduledDueRound,
	LotteryRound? DueRound,
	LotteryRound? LatestRound,
	string? LastDryRunRoundKey = null);

/// <summary>The possible values of <see cref="LotteryTickPlan.Action" />.</summary>
public static class LotteryTickAction
{
	public const string Run = "run";
	public const string ResumeDraw = "resume-draw";
	public const string ResumeAnnouncement = "resume-announcement";
	public const string ResumeCleanup = "resume-cleanup";
	public const string WaitingForRetry = "waiting-for-retry";
	public const string Blocked = "blocked";
	public const string Nothing = "nothing";
	public const string Disabled = "disabled";
	public const string Paused = "paused";
}

/// <summary>What the scheduler decided to do on one tick.</summary>
public sealed record LotteryTickPlan(
	string Action,
	bool DryRun,
	string Message,
	LotteryRound? Round = null,
	LotteryScheduleSlot? Slot = null,
	IReadOnlyList<LotteryScheduleSlot>? SlotsToSkip = null)
{
	public IReadOnlyList<LotteryScheduleSlot> Skips => SlotsToSkip ?? [];

	public static LotteryTickPlan Of(string action, bool dryRun, string message, LotteryRound? round = null,
		LotteryScheduleSlot? slot = null, IReadOnlyList<LotteryScheduleSlot>? slotsToSkip = null)
	{
		return new LotteryTickPlan(action, dryRun, message, round, slot, slotsToSkip ?? []);
	}
}
