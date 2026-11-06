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
}

/// <summary>The possible values of <see cref="LotteryRound.FailedStep" />.</summary>
public static class LotteryFailedStep
{
	public const string Draw = "draw";
	public const string Announcement = "announcement";
	public const string Cleanup = "cleanup";

	public static readonly IReadOnlyList<string> All = [Draw, Announcement, Cleanup];
}
