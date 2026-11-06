namespace ExcelBotCs.Modules.Lottery;

/// <summary>
///     The possible values of <see cref="LotteryRound.Status" />. Enum-like values are stored as plain strings so the
///     database, the JSON API and the spec use the same vocabulary.
/// </summary>
public static class LotteryRoundStatus
{
	public const string Scheduled = "scheduled";
	public const string Drawing = "drawing";
	public const string Drawn = "drawn";
	public const string Announced = "announced";
	public const string Completed = "completed";
	public const string Skipped = "skipped";
	public const string Failed = "failed";
	public const string Voided = "voided";

	public static readonly IReadOnlyList<string> All =
	[
		Scheduled, Drawing, Drawn, Announced, Completed, Skipped, Failed, Voided
	];

	/// <summary>
	///     A round in one of these statuses is unfinished: the next scheduled round must not run until an admin retries
	///     or voids it (spec §5.10). <see cref="Failed" /> is included even though it never retries on its own.
	/// </summary>
	public static bool BlocksTheSchedule(string status)
	{
		return status is Drawing or Drawn or Announced or Failed;
	}

	/// <summary>The statuses the scheduler resumes by itself: a claimed draw, a pending announcement and its cleanup.</summary>
	public static bool CanResumeAutomatically(string status)
	{
		return status is Drawing or Drawn or Announced;
	}

	/// <summary>
	///     Guessing is refused once a run owns the guess set: filling entries, announcing, or blocking on a failure.
	///     Accepting a guess then would either race the draw or be lost by the cleanup flush.
	/// </summary>
	public static bool BlocksGuessing(string status)
	{
		return BlocksTheSchedule(status);
	}
}

/// <summary>The possible values of <see cref="LotteryRound.FailedStep" />.</summary>
public static class LotteryFailedStep
{
	public const string Draw = "draw";
	public const string Announcement = "announcement";
	public const string Cleanup = "cleanup";

	public static readonly IReadOnlyList<string> All = [Draw, Announcement, Cleanup];
}
