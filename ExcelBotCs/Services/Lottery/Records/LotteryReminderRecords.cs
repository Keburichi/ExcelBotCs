namespace ExcelBotCs.Services.Lottery.Records;

/// <summary>
///     A member's stored reminder choice. Default off: an absent document is <c>Enabled == false</c>, so a member who
///     never opened the toggle is never pinged (spec §5.7).
/// </summary>
public sealed record LotteryReminderPreference(bool Enabled);

/// <summary>One member the reminder would ping, and how many guesses they have left for the upcoming draw.</summary>
public sealed record LotteryReminderRecipient(ulong DiscordId, int RemainingGuesses);

/// <summary>One "N guesses remaining" line of the reminder: a count and the members who share it.</summary>
public sealed record LotteryReminderGroup(int RemainingGuesses, IReadOnlyList<ulong> DiscordIds);

/// <summary>
///     The reminder audience for the upcoming round: current FC members who opted in and still have a guess. Grouped by
///     remaining count when rendered, newest count first (spec §5.7). An empty audience is how the sender knows to stay
///     silent instead of posting an empty ping.
/// </summary>
public sealed record LotteryReminderAudience(IReadOnlyList<LotteryReminderRecipient> Recipients)
{
	public static LotteryReminderAudience Empty => new([]);

	public bool IsEmpty => Recipients.Count == 0;

	/// <summary>Every recipient id, in the deterministic order the post mentions them.</summary>
	public IReadOnlyList<ulong> DiscordIds => Recipients.Select(recipient => recipient.DiscordId).ToList();

	/// <summary>
	///     The post's group lines: counts descending (the §5.7 copy shows "2" before "1"), and the members inside one
	///     group ordered by Discord id so the same audience always renders the same message.
	/// </summary>
	public IReadOnlyList<LotteryReminderGroup> Groups => Recipients
		.GroupBy(recipient => recipient.RemainingGuesses)
		.OrderByDescending(group => group.Key)
		.Select(group => new LotteryReminderGroup(group.Key,
			group.Select(recipient => recipient.DiscordId).OrderBy(id => id).ToList()))
		.ToList();
}

/// <summary>
///     The draw the reminder is about. <see cref="RunDay" /> and <see cref="RunTime" /> are read off the round's actual
///     scheduled instant, so a rescheduled round advertises its new day and time (§5.7).
/// </summary>
public sealed record LotteryReminderTarget(
	string RoundKey,
	DateTime ScheduledAtUtc,
	string RunDay,
	string RunTime,
	DateOnly LocalDate);

/// <summary>The possible values of <see cref="LotteryReminderOutcome.Disposition" />, ready for the command, the API and the admin page.</summary>
public static class LotteryReminderDisposition
{
	/// <summary>The reminder was posted (or, for a test send, delivered to the requesting admin).</summary>
	public const string Sent = "sent";

	/// <summary>The reminder for this round was already posted; nothing was sent again.</summary>
	public const string AlreadySent = "already-sent";

	/// <summary>Nobody has reminders turned on.</summary>
	public const string NobodyOptedIn = "nobody-opted-in";

	/// <summary>Members opted in but none of them is an eligible current member with a guess left.</summary>
	public const string NobodyEligible = "nobody-eligible";

	/// <summary>The admin reminder toggle is off.</summary>
	public const string RemindersDisabled = "reminders-disabled";

	/// <summary>The deployment master switch is off.</summary>
	public const string AutomationDisabled = "automation-disabled";

	/// <summary>The admin paused automation.</summary>
	public const string Paused = "paused";

	/// <summary>The deployment is in its dry-run week, which previews the draw and posts no live side effects.</summary>
	public const string DryRun = "dry-run";

	/// <summary>The startup check has not finished yet, so nothing is scheduled or reminded until it does.</summary>
	public const string StartupPending = "startup-pending";

	/// <summary>The startup check found bad or missing configuration; the scheduler is off until it is fixed.</summary>
	public const string StartupDegraded = "startup-degraded";

	/// <summary>There is no upcoming draw to remind about.</summary>
	public const string NoUpcomingRound = "no-upcoming-round";

	/// <summary>The upcoming round was skipped, voided, completed or already ran, so it gets no reminder.</summary>
	public const string Skipped = "skipped";

	/// <summary>Now is outside the reminder's delivery window (too early, or past the draw).</summary>
	public const string OutsideWindow = "outside-window";

	/// <summary>A failed attempt is waiting for its scheduled retry.</summary>
	public const string WaitingForRetry = "waiting-for-retry";

	/// <summary>The post failed but a retry is scheduled.</summary>
	public const string Retrying = "retrying";

	/// <summary>The post failed and the attempts are exhausted (or a manual send failed); the admins were alerted.</summary>
	public const string Failed = "failed";
}

/// <summary>The typed outcome of one reminder tick or send, so the command and the admin page can answer without parsing text.</summary>
public sealed record LotteryReminderOutcome(
	string Disposition,
	string Message,
	string? RoundKey = null,
	int Recipients = 0)
{
	public bool Posted => Disposition == LotteryReminderDisposition.Sent;

	public bool HasAudience => Recipients > 0;
}
