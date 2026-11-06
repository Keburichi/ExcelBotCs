using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The scheduler's decision table (spec §5.1 and §5.10), pure and driven by a fake clock: which round is due,
///     which statuses block, what catch-up does with several missed dates, and what an admin failure blocks.
/// </summary>
public class LotteryRoundPlannerTests
{
	private static readonly LotterySchedule Schedule = LotterySchedule.Create("Tuesday", "20:00", "Europe/Berlin");

	private static DateTimeOffset Utc(string instant)
	{
		return DateTimeOffset.Parse(instant + "Z", System.Globalization.CultureInfo.InvariantCulture);
	}

	private static LotteryTickState State(
		LotteryRound? blocking = null,
		LotteryRound? scheduledDue = null,
		LotteryRound? due = null,
		LotteryRound? latest = null)
	{
		return new LotteryTickState(blocking, scheduledDue, due, latest);
	}

	private static LotteryRound Round(string roundKey, string status, DateTime? scheduledAt = null,
		DateTime? nextRetryAt = null, string? failedStep = null, int attempts = 0)
	{
		return new LotteryRound
		{
			RoundKey = roundKey,
			Status = status,
			ScheduledAt = scheduledAt ?? Utc(roundKey + "T19:00:00").UtcDateTime,
			NextRetryAt = nextRetryAt,
			FailedStep = failedStep,
			Attempts = attempts
		};
	}

	private static LotteryTickPlan Plan(DateTimeOffset now, LotteryTickState state, bool enabled = true,
		bool paused = false, bool dryRun = false)
	{
		return LotteryRoundPlanner.Plan(Schedule, now, state, enabled, paused, dryRun);
	}

	[Fact]
	public void Plan_Disabled_DoesNothing()
	{
		var plan = Plan(Utc("2026-11-10T19:30:00"), State(), enabled: false);

		plan.Action.ShouldBe(LotteryTickAction.Disabled);
		plan.Round.ShouldBeNull();
		plan.Slot.ShouldBeNull();
	}

	[Fact]
	public void Plan_Paused_DoesNothing()
	{
		var plan = Plan(Utc("2026-11-10T19:30:00"), State(), paused: true);

		plan.Action.ShouldBe(LotteryTickAction.Paused);
	}

	[Fact]
	public void Plan_DueSlotWithoutARound_RunsIt()
	{
		var latest = Round("2026-11-03", LotteryRoundStatus.Completed);
		var now = Utc("2026-11-10T19:00:00");

		var plan = Plan(now, State(latest: latest));

		plan.Action.ShouldBe(LotteryTickAction.Run);
		plan.Slot.ShouldNotBeNull();
		plan.Slot!.RoundKey.ShouldBe("2026-11-10");
		plan.Slot.ScheduledAtUtc.ShouldBe(Utc("2026-11-10T19:00:00").UtcDateTime);
		plan.SlotsToSkip.ShouldBeEmpty();
	}

	[Fact]
	public void Plan_BeforeTheRunTime_DoesNotRunTodaysSlotYet()
	{
		var latest = Round("2026-11-03", LotteryRoundStatus.Completed);
		var due = Round("2026-11-03", LotteryRoundStatus.Completed);
		var now = Utc("2026-11-10T18:59:00");

		var plan = Plan(now, State(due: due, latest: latest));

		plan.Action.ShouldBe(LotteryTickAction.Nothing);
	}

	[Fact]
	public void Plan_DueRoundAlreadyCompleted_DoesNothing()
	{
		var due = Round("2026-11-10", LotteryRoundStatus.Completed);
		var plan = Plan(Utc("2026-11-10T19:05:00"), State(due: due, latest: due));

		plan.Action.ShouldBe(LotteryTickAction.Nothing);
	}

	[Fact]
	public void Plan_ThreeMissedWeeks_SkipsTheOlderDatesAndRunsTheMostRecentOnce()
	{
		var latest = Round("2026-10-13", LotteryRoundStatus.Completed);
		var now = Utc("2026-11-05T12:00:00"); // Thursday: the most recent slot is 3 November.

		var plan = Plan(now, State(latest: latest));

		plan.Action.ShouldBe(LotteryTickAction.Run);
		plan.Slot!.RoundKey.ShouldBe("2026-11-03");
		// The two dates between the latest round and the due slot are recorded as skipped, oldest first.
		plan.SlotsToSkip.Select(slot => slot.RoundKey).ShouldBe(["2026-10-20", "2026-10-27"]);
	}

	[Fact]
	public void Plan_NoRoundsAtAllMidWeek_WaitsForTheNextSlot()
	{
		var now = Utc("2026-11-05T12:00:00");

		var plan = Plan(now, State());

		plan.Action.ShouldBe(LotteryTickAction.Nothing);
	}

	[Fact]
	public void Plan_NoRoundsAtAllLaterOnTheScheduledDate_RunsImmediately()
	{
		var now = Utc("2026-11-10T20:30:00");

		var plan = Plan(now, State());

		plan.Action.ShouldBe(LotteryTickAction.Run);
		plan.Slot!.RoundKey.ShouldBe("2026-11-10");
	}

	[Fact]
	public void Plan_RescheduledScheduledRoundThatIsDue_RunsThatRound()
	{
		// A round moved from 3 November to 5 November: its key is still 3 November but it runs on the 5th.
		var rescheduled = Round("2026-11-03", LotteryRoundStatus.Scheduled, Utc("2026-11-05T18:00:00").UtcDateTime);
		var now = Utc("2026-11-05T18:30:00");

		var plan = Plan(now, State(scheduledDue: rescheduled, latest: rescheduled));

		plan.Action.ShouldBe(LotteryTickAction.Run);
		plan.Round.ShouldBe(rescheduled);
		plan.Slot!.RoundKey.ShouldBe("2026-11-03");
		plan.Slot.ScheduledAtUtc.ShouldBe(Utc("2026-11-05T18:00:00").UtcDateTime);
	}

	[Fact]
	public void Plan_FutureRescheduledRoundExists_DoesNothingForTheOlderDueSlot()
	{
		// The 3 November round was moved to 12 November, so the due slot (3 November) must not run again.
		var future = Round("2026-11-12", LotteryRoundStatus.Scheduled, Utc("2026-11-12T19:00:00").UtcDateTime);
		var now = Utc("2026-11-05T12:00:00");

		var plan = Plan(now, State(latest: future));

		plan.Action.ShouldBe(LotteryTickAction.Nothing);
	}

	[Fact]
	public void Plan_BlockingDrawingRoundWithDueRetry_ResumesTheDraw()
	{
		var pending = Round("2026-11-10", LotteryRoundStatus.Drawing, nextRetryAt: Utc("2026-11-10T19:00:30").UtcDateTime,
			failedStep: LotteryFailedStep.Draw, attempts: 1);

		var plan = Plan(Utc("2026-11-10T19:01:00"), State(blocking: pending, latest: pending));

		plan.Action.ShouldBe(LotteryTickAction.ResumeDraw);
		plan.Round.ShouldBe(pending);
	}

	[Fact]
	public void Plan_BlockingDrawingRoundWithAFutureRetry_Waits()
	{
		var pending = Round("2026-11-10", LotteryRoundStatus.Drawing, nextRetryAt: Utc("2026-11-10T19:05:00").UtcDateTime,
			failedStep: LotteryFailedStep.Draw, attempts: 1);

		var plan = Plan(Utc("2026-11-10T19:01:00"), State(blocking: pending, latest: pending));

		plan.Action.ShouldBe(LotteryTickAction.WaitingForRetry);
	}

	[Fact]
	public void Plan_BlockingDrawnRound_ResumesTheAnnouncement()
	{
		var pending = Round("2026-11-10", LotteryRoundStatus.Drawn, nextRetryAt: Utc("2026-11-10T19:00:30").UtcDateTime,
			failedStep: LotteryFailedStep.Announcement, attempts: 1);

		var plan = Plan(Utc("2026-11-10T19:02:00"), State(blocking: pending, latest: pending));

		plan.Action.ShouldBe(LotteryTickAction.ResumeAnnouncement);
	}

	[Fact]
	public void Plan_BlockingAnnouncedRound_ResumesTheCleanup()
	{
		var pending = Round("2026-11-10", LotteryRoundStatus.Announced);

		var plan = Plan(Utc("2026-11-10T19:02:00"), State(blocking: pending, latest: pending));

		plan.Action.ShouldBe(LotteryTickAction.ResumeCleanup);
	}

	[Fact]
	public void Plan_FailedRound_IsBlockedAndNeverAutomaticallyRetried()
	{
		var failed = Round("2026-11-10", LotteryRoundStatus.Failed, failedStep: LotteryFailedStep.Announcement,
			attempts: 7);
		var now = Utc("2026-11-17T19:00:00");

		var plan = Plan(now, State(blocking: failed, latest: failed));

		plan.Action.ShouldBe(LotteryTickAction.Blocked);
		plan.Round.ShouldBe(failed);

		// Even when the next scheduled slot is due, the failed round blocks it.
		plan.Slot.ShouldBeNull();
	}

	[Fact]
	public void Plan_PendingRoundBlocksTheDueSlot()
	{
		var pending = Round("2026-11-03", LotteryRoundStatus.Drawn, nextRetryAt: Utc("2026-11-04T00:00:00").UtcDateTime,
			failedStep: LotteryFailedStep.Announcement, attempts: 2);
		var now = Utc("2026-11-10T19:00:00");

		var plan = Plan(now, State(blocking: pending, latest: pending));

		plan.Action.ShouldBe(LotteryTickAction.ResumeAnnouncement);
		plan.Round.ShouldBe(pending);
	}

	[Fact]
	public void Plan_DryRun_PlansTheRunForAPreview()
	{
		var latest = Round("2026-11-03", LotteryRoundStatus.Completed);

		var plan = Plan(Utc("2026-11-10T19:00:00"), State(latest: latest), dryRun: true);

		plan.Action.ShouldBe(LotteryTickAction.Run);
		plan.DryRun.ShouldBeTrue();
	}

	[Fact]
	public void Plan_DueSlotThatWasADryRunWeek_IsNotRunLiveAfterPromotion()
	{
		// Spec §12 step 7: a previewed week is not replayed once the deployment goes live.
		var latest = Round("2026-11-03", LotteryRoundStatus.Completed);
		var now = Utc("2026-11-10T19:30:00");

		var plan = LotteryRoundPlanner.Plan(Schedule, now,
			new LotteryTickState(null, null, null, latest, LastDryRunRoundKey: "2026-11-10"), true, false, false);

		plan.Action.ShouldBe(LotteryTickAction.Nothing);
		plan.Slot.ShouldBeNull();
	}

	[Fact]
	public void Plan_TheSlotAfterADryRunWeek_StillRunsLive()
	{
		var latest = Round("2026-11-03", LotteryRoundStatus.Completed);
		var now = Utc("2026-11-17T19:30:00");

		var plan = LotteryRoundPlanner.Plan(Schedule, now,
			new LotteryTickState(null, null, null, latest, LastDryRunRoundKey: "2026-11-10"), true, false, false);

		plan.Action.ShouldBe(LotteryTickAction.Run);
		plan.Slot.ShouldNotBeNull();
		plan.Slot!.RoundKey.ShouldBe("2026-11-17");
	}

	[Fact]
	public void Statuses_ClassifyBlockingResumableAndGuessWindows()
	{
		LotteryRoundStatus.BlocksTheSchedule(LotteryRoundStatus.Drawing).ShouldBeTrue();
		LotteryRoundStatus.BlocksTheSchedule(LotteryRoundStatus.Drawn).ShouldBeTrue();
		LotteryRoundStatus.BlocksTheSchedule(LotteryRoundStatus.Announced).ShouldBeTrue();
		LotteryRoundStatus.BlocksTheSchedule(LotteryRoundStatus.Failed).ShouldBeTrue();
		LotteryRoundStatus.BlocksTheSchedule(LotteryRoundStatus.Completed).ShouldBeFalse();
		LotteryRoundStatus.BlocksTheSchedule(LotteryRoundStatus.Skipped).ShouldBeFalse();
		LotteryRoundStatus.BlocksTheSchedule(LotteryRoundStatus.Voided).ShouldBeFalse();

		LotteryRoundStatus.CanResumeAutomatically(LotteryRoundStatus.Drawing).ShouldBeTrue();
		LotteryRoundStatus.CanResumeAutomatically(LotteryRoundStatus.Failed).ShouldBeFalse();

		LotteryRoundStatus.BlocksGuessing(LotteryRoundStatus.Drawing).ShouldBeTrue();
		LotteryRoundStatus.BlocksGuessing(LotteryRoundStatus.Drawn).ShouldBeTrue();
		LotteryRoundStatus.BlocksGuessing(LotteryRoundStatus.Announced).ShouldBeTrue();
		LotteryRoundStatus.BlocksGuessing(LotteryRoundStatus.Failed).ShouldBeTrue();
		LotteryRoundStatus.BlocksGuessing(LotteryRoundStatus.Completed).ShouldBeFalse();
	}
}
