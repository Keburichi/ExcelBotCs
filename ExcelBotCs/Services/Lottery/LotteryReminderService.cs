using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The reminder module (spec §5.7): the member's opt-in, the audience for the upcoming draw, the once-per-week
///     scheduled post, the officer's manual reminder and the admin test send.
///     <para>
///         Opt-in: one row per Discord id in <c>lottery_reminder_optins</c>, default off, kept through leave/rejoin
///         (membership is evaluated when the audience is built, never stored on the opt-in). The web record is the
///         source of truth; the slash command and the opt-out button write the same record.
///     </para>
///     <para>
///         Idempotency: at most one scheduled post per round. The service remembers the round key it posted in memory
///         (so two ticks in the same week cannot double-post), and before posting it looks for an existing bot reminder
///         since the reminder slot (so a restart inside the window adopts the earlier post instead of repeating it).
///         The delivery window is bounded: it opens at the configured reminder instant, stays open for at most
///         <see cref="CatchUpWindow" />, and always closes at the draw.
///     </para>
///     <para>
///         Delivery is best effort: <see cref="MaxAttempts" /> posts over <see cref="RetryDelays" />, one needs-admin
///         alert when they are exhausted, and no path throws into the scheduler (spec §5.10).
///     </para>
///     <para>
///         Automation gates: the scheduled tick honours the startup verdict exactly like the round engine — a
///         still-running or degraded startup check posts nothing, because a degraded start must disable the draw and
///         the reminder together (criterion 19). The officer's manual reminder and the admin test send are escape
///         hatches and ignore the verdict, just as the manual Run now ignores the kill switch.
///     </para>
/// </summary>
public class LotteryReminderService : ILotteryReminderService
{
	/// <summary>Attempts at a scheduled post before the failure is alerted instead of retried.</summary>
	public const int MaxAttempts = 3;

	/// <summary>The waits between the three attempts: 0, 5 and 15 minutes after the reminder instant.</summary>
	public static readonly IReadOnlyList<TimeSpan> RetryDelays =
	[
		TimeSpan.FromMinutes(5),
		TimeSpan.FromMinutes(10)
	];

	/// <summary>
	///     The longest a scheduled reminder may be posted after its configured instant. The window always also closes
	///     at the draw, so a restart cannot send "use your guesses" for a round that already ran.
	/// </summary>
	public static readonly TimeSpan CatchUpWindow = TimeSpan.FromHours(24);

	/// <summary>How many recent lottery-channel messages the restart check searches.</summary>
	public const int RecentMessageLimit = 50;

	private readonly ILotteryAlertService _alerts;
	private readonly IExtraLotteryGuessRepository _extraGuesses;
	private readonly ILotteryGuessRepository _guesses;
	private readonly ILogger<LotteryReminderService> _logger;
	private readonly IMemberService _members;
	private readonly IDiscordMessageService _messages;
	private readonly ILotteryReminderOptinRepository _optIns;
	private readonly ILotteryRoundRepository _rounds;
	private readonly ILotteryScheduleService _schedules;
	private readonly ILotterySettingsService _settings;
	private readonly LotteryStartupCheckService? _startupCheck;
	private readonly TimeProvider _time;

	private string? _exhaustedRoundKey;
	private PendingAttempt? _pending;
	private string? _sentRoundKey;

	public LotteryReminderService(ILotterySettingsService settings, ILotteryScheduleService schedules,
		ILotteryReminderOptinRepository optIns, ILotteryRoundRepository rounds, ILotteryGuessRepository guesses,
		IExtraLotteryGuessRepository extraGuesses, IMemberService members, IDiscordMessageService messages,
		ILotteryAlertService alerts, TimeProvider time, ILogger<LotteryReminderService> logger,
		LotteryStartupCheckService? startupCheck = null)
	{
		_settings = settings;
		_schedules = schedules;
		_optIns = optIns;
		_rounds = rounds;
		_guesses = guesses;
		_extraGuesses = extraGuesses;
		_members = members;
		_messages = messages;
		_alerts = alerts;
		_time = time;
		_logger = logger;
		_startupCheck = startupCheck;
	}

	#region Opt-in

	public async Task<LotteryReminderPreference> GetPreferenceAsync(ulong discordId,
		CancellationToken cancellationToken = default)
	{
		var optIn = await _optIns.GetByDiscordIdAsync(discordId);
		return new LotteryReminderPreference(optIn?.Enabled ?? false);
	}

	public async Task<LotteryReminderPreference> SetPreferenceAsync(ulong discordId, bool enabled,
		CancellationToken cancellationToken = default)
	{
		// Idempotent by construction: the repository upserts the member's single document, and there is deliberately no
		// FC membership check — the opt-out button must keep working after the caller left the FC.
		await _optIns.SetEnabledAsync(discordId, enabled);
		return new LotteryReminderPreference(enabled);
	}

	#endregion

	#region Audience

	public async Task<LotteryReminderAudience> GetAudienceAsync(CancellationToken cancellationToken = default)
	{
		return (await BuildAudienceAsync(cancellationToken)).Audience;
	}

	private async Task<(LotteryReminderAudience Audience, bool AnyOptIns)> BuildAudienceAsync(
		CancellationToken cancellationToken)
	{
		var optedIn = await _optIns.GetEnabledAsync();
		if (optedIn.Count == 0) return (LotteryReminderAudience.Empty, false);

		// Current FC members only: a member who left is still opted in (the record survives), but is not pinged until
		// they rejoin. An opted-in id with no member row (a deleted account) is skipped.
		var currentMemberIds = (await _members.GetFcMembers())
			.Select(member => ParseDiscordId(member.DiscordId))
			.Where(id => id is not null)
			.Select(id => id!.Value)
			.ToHashSet();

		var usedGuesses = await CountByDiscordIdAsync(_guesses.GetAsync(), guess => guess.DiscordId);
		var awardedGuesses = await CountByDiscordIdAsync(_extraGuesses.GetAsync(), award => award.DiscordId);

		var recipients = optedIn
			.Select(optIn => optIn.DiscordId)
			.Where(currentMemberIds.Contains)
			.Select(id => new LotteryReminderRecipient(id,
				LotteryGuessAllowance.Remaining(usedGuesses.GetValueOrDefault(id),
					awardedGuesses.GetValueOrDefault(id))))
			.Where(recipient => recipient.RemainingGuesses > 0)
			.OrderByDescending(recipient => recipient.RemainingGuesses)
			.ThenBy(recipient => recipient.DiscordId)
			.ToList();

		return (new LotteryReminderAudience(recipients), true);
	}

	#endregion

	#region Scheduled tick

	public async Task<LotteryReminderOutcome> TickAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			return await TickCoreAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			return new LotteryReminderOutcome(LotteryReminderDisposition.OutsideWindow,
				"The lottery reminder tick was cancelled.");
		}
		catch (Exception exception)
		{
			// The scheduler loop must never see a reminder failure (spec §5.10). The next tick retries.
			_logger.LogError(exception, "The lottery reminder tick failed; the next tick will retry");
			return new LotteryReminderOutcome(LotteryReminderDisposition.Failed,
				$"The lottery reminder tick failed: {exception.Message}");
		}
	}

	private async Task<LotteryReminderOutcome> TickCoreAsync(CancellationToken cancellationToken)
	{
		// The startup verdict gates every automated post, the reminder included (spec §5.10, criterion 19). The
		// startup check owns the single alert; logging again here would enqueue one line per tick through DiscordLogger.
		if (_startupCheck is { Result: null })
			return new LotteryReminderOutcome(LotteryReminderDisposition.StartupPending,
				"The lottery startup check has not finished yet; no reminder was posted on this tick.");

		if (_startupCheck?.Result is { IsDegraded: true })
			return new LotteryReminderOutcome(LotteryReminderDisposition.StartupDegraded,
				"Lottery automation starts degraded; no reminder is posted until the configuration is fixed.");

		var settings = await _settings.GetEffectiveAsync(cancellationToken);

		if (!settings.AutomationEnabled)
			return new LotteryReminderOutcome(LotteryReminderDisposition.AutomationDisabled,
				"Lottery automation is disabled, so no reminder was posted.");

		if (settings.PauseAutomation)
			return new LotteryReminderOutcome(LotteryReminderDisposition.Paused,
				"Lottery automation is paused, so no reminder was posted.");

		if (!settings.RemindersEnabled)
			return new LotteryReminderOutcome(LotteryReminderDisposition.RemindersDisabled,
				"Lottery reminders are turned off in the settings.");

		var resolved = await ResolveTargetAsync(cancellationToken);
		if (resolved.Target is not { } target)
			return new LotteryReminderOutcome(resolved.SilentDisposition!, resolved.Message!);

		if (_sentRoundKey == target.RoundKey)
			return new LotteryReminderOutcome(LotteryReminderDisposition.AlreadySent,
				"This week's lottery reminder has already been posted.", target.RoundKey);

		if (_exhaustedRoundKey == target.RoundKey)
			return new LotteryReminderOutcome(LotteryReminderDisposition.Failed,
				"The lottery reminder for this round already exhausted its attempts; the admins were alerted.",
				target.RoundKey);

		var reminderSchedule = LotterySchedule.Create(settings.ReminderDay, settings.ReminderTime,
			settings.ReminderTimezone);
		var window = LotteryReminderSchedule.WindowFor(reminderSchedule, target, CatchUpWindow);
		var now = _time.GetUtcNow().UtcDateTime;

		if (!window.IsOpen(now))
			return new LotteryReminderOutcome(LotteryReminderDisposition.OutsideWindow,
				"It is not time for the lottery reminder yet, or the draw has passed.", target.RoundKey);

		// A restart inside the window must adopt the earlier post rather than ping the audience twice.
		if (await AlreadyPostedAsync(window, cancellationToken))
		{
			_sentRoundKey = target.RoundKey;
			_pending = null;
			return new LotteryReminderOutcome(LotteryReminderDisposition.AlreadySent,
				"This week's lottery reminder was already posted.", target.RoundKey);
		}

		if (_pending is { } pending && pending.RoundKey == target.RoundKey && now < pending.NextRetryAtUtc)
			return new LotteryReminderOutcome(LotteryReminderDisposition.WaitingForRetry,
				$"The lottery reminder is waiting for its retry at {pending.NextRetryAtUtc:O}.", target.RoundKey);

		var (audience, anyOptIns) = await BuildAudienceAsync(cancellationToken);
		if (audience.IsEmpty)
			return anyOptIns
				? new LotteryReminderOutcome(LotteryReminderDisposition.NobodyEligible,
					"Nobody has unused lottery guesses right now, so no reminder was posted.")
				: new LotteryReminderOutcome(LotteryReminderDisposition.NobodyOptedIn,
					"Nobody has lottery reminders turned on, so no reminder was posted.");

		var text = LotteryReminderComposer.Compose(audience, target);

		try
		{
			var posted = await _messages.PostLotteryReminderAsync(text, audience.DiscordIds);
			if (posted is null)
				throw new InvalidOperationException(
					"The lottery reminder could not be posted: the lottery channel is unreachable.");

			_sentRoundKey = target.RoundKey;
			_pending = null;

			_logger.LogInformation("Lottery reminder for round {RoundKey} posted to {Recipients} member(s)",
				target.RoundKey, audience.Recipients.Count);

			return new LotteryReminderOutcome(LotteryReminderDisposition.Sent,
				"The lottery reminder was posted to the lottery channel.", target.RoundKey,
				audience.Recipients.Count);
		}
		catch (Exception exception)
		{
			return await RegisterFailureAsync(target, exception, now, cancellationToken);
		}
	}

	/// <summary>
	///     The next draw: a round the admin rescheduled wins, otherwise the next configured slot. A slot whose round is
	///     already skipped, voided, completed or running gets no reminder (spec §5.7).
	/// </summary>
	private async Task<ResolvedTarget> ResolveTargetAsync(CancellationToken cancellationToken)
	{
		var schedule = await _schedules.GetScheduleAsync(cancellationToken);
		var now = _time.GetUtcNow();

		var rescheduled = await _rounds.GetNextScheduledAsync(now.UtcDateTime);
		if (rescheduled is not null)
			return ResolvedTarget.Of(LotteryReminderSchedule.TargetFor(schedule, rescheduled));

		var slot = schedule.NextSlotAfter(now);
		var existing = await _rounds.GetByRoundKeyAsync(slot.RoundKey);

		if (existing is not null)
		{
			if (existing.Status != LotteryRoundStatus.Scheduled)
				return ResolvedTarget.Silent(LotteryReminderDisposition.Skipped,
					$"Round {existing.RoundKey} is {existing.Status}, so no reminder was posted.");

			// A round moved within the same date keeps its key but not its time.
			return ResolvedTarget.Of(LotteryReminderSchedule.TargetFor(schedule, existing));
		}

		return ResolvedTarget.Of(LotteryReminderSchedule.TargetFor(schedule, slot));
	}

	private async Task<bool> AlreadyPostedAsync(LotteryReminderWindow window, CancellationToken cancellationToken)
	{
		if (window.IsEmpty) return false;

		try
		{
			var messages = await _messages.GetRecentLotteryChannelMessagesAsync(RecentMessageLimit) ?? [];

			return messages.Any(message => message.IsBot
			                               && message.Content.Contains(LotteryReminderComposer.HeadingPrefix,
				                               StringComparison.Ordinal)
			                               && message.CreatedAt.UtcDateTime >= window.StartsAtUtc
			                               && message.CreatedAt.UtcDateTime < window.EndsAtUtc);
		}
		catch (Exception exception)
		{
			// The channel search is a duplicate guard, not the delivery itself: if it fails, post (the round is still
			// better reminded once than missed entirely) and let the post's own failure handling deal with the rest.
			_logger.LogWarning(exception,
				"The reminder channel search failed; posting without the restart duplicate check");
			return false;
		}
	}

	private async Task<LotteryReminderOutcome> RegisterFailureAsync(LotteryReminderTarget target,
		Exception exception, DateTime nowUtc, CancellationToken cancellationToken)
	{
		var attempts = (_pending is { } pending && pending.RoundKey == target.RoundKey ? pending.Attempts : 0) + 1;
		var error = exception.Message;

		if (attempts >= MaxAttempts)
		{
			_pending = null;
			_exhaustedRoundKey = target.RoundKey;

			_logger.LogError(exception,
				"The lottery reminder for round {RoundKey} failed after {Attempts} attempt(s); the admins are alerted",
				target.RoundKey, attempts);

			await PublishFailureAlertAsync(target, exception, attempts, cancellationToken);

			return new LotteryReminderOutcome(LotteryReminderDisposition.Failed,
				$"The lottery reminder could not be posted after {attempts} attempts; the admins were alerted.",
				target.RoundKey);
		}

		var delay = RetryDelays[attempts - 1];
		_pending = new PendingAttempt(target.RoundKey, attempts, nowUtc + delay);

		_logger.LogError(exception,
			"The lottery reminder for round {RoundKey} failed (attempt {Attempts} of {MaxAttempts}); the next retry " +
			"is at {NextRetryAt}", target.RoundKey, attempts, MaxAttempts, _pending.NextRetryAtUtc);

		return new LotteryReminderOutcome(LotteryReminderDisposition.Retrying,
			$"The lottery reminder could not be posted (attempt {attempts} of {MaxAttempts}); it will be retried " +
			$"at {_pending.NextRetryAtUtc:O}.", target.RoundKey);
	}

	#endregion

	#region Manual reminder and test send

	/// <summary>
	///     The officer's manual reminder: the same audience and copy, posted now, with no window, toggle or
	///     already-posted check — an explicit request always posts. Nobody eligible means nothing is posted and the
	///     outcome explains why (spec §5.7).
	/// </summary>
	public async Task<LotteryReminderOutcome> RemindNowAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var resolved = await ResolveTargetAsync(cancellationToken);
			if (resolved.Target is not { } target)
				return new LotteryReminderOutcome(resolved.SilentDisposition!, resolved.Message!);

			var (audience, anyOptIns) = await BuildAudienceAsync(cancellationToken);
			if (audience.IsEmpty) return EmptyAudienceOutcome(anyOptIns, target.RoundKey);

			var text = LotteryReminderComposer.Compose(audience, target);

			try
			{
				var posted = await _messages.PostLotteryReminderAsync(text, audience.DiscordIds);
				if (posted is null)
					throw new InvalidOperationException(
						"The lottery reminder could not be posted: the lottery channel is unreachable.");

				_sentRoundKey = target.RoundKey;
				_pending = null;

				_logger.LogInformation(
					"The manual lottery reminder for round {RoundKey} was posted to {Recipients} member(s)",
					target.RoundKey, audience.Recipients.Count);

				return new LotteryReminderOutcome(LotteryReminderDisposition.Sent,
					"The lottery reminder was posted to the lottery channel.", target.RoundKey,
					audience.Recipients.Count);
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "The manual lottery reminder for round {RoundKey} could not be posted",
					target.RoundKey);

				await PublishFailureAlertAsync(target, exception, 1, cancellationToken);

				return new LotteryReminderOutcome(LotteryReminderDisposition.Failed,
					"The lottery reminder could not be posted; the admins were alerted.", target.RoundKey);
			}
		}
		catch (OperationCanceledException)
		{
			return new LotteryReminderOutcome(LotteryReminderDisposition.Failed,
				"The manual lottery reminder was cancelled.");
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "The manual lottery reminder failed");
			return new LotteryReminderOutcome(LotteryReminderDisposition.Failed,
				$"The lottery reminder could not be built: {exception.Message}");
		}
	}

	/// <summary>
	///     The admin page's "Send test reminder" (#114): the current copy, delivered to the requesting admin only. The
	///     window and the toggles are ignored — an admin explicitly asked for it — and nothing is posted to the channel.
	/// </summary>
	public async Task<LotteryReminderOutcome> SendTestAsync(ulong requestingAdminDiscordId,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var resolved = await ResolveTargetAsync(cancellationToken);
			if (resolved.Target is not { } target)
				return new LotteryReminderOutcome(resolved.SilentDisposition!, resolved.Message!);

			var (audience, anyOptIns) = await BuildAudienceAsync(cancellationToken);
			if (audience.IsEmpty) return EmptyAudienceOutcome(anyOptIns, target.RoundKey);

			var text = LotteryReminderComposer.Compose(audience, target);
			bool delivered;

			try
			{
				delivered = await _messages.SendLotteryReminderDirectMessageAsync(requestingAdminDiscordId, text);
			}
			catch (Exception exception)
			{
				// A DM to an admin can fail for reasons the channel post never does (privacy settings, no shared
				// guild). The admin sees the failure in the page; no channel alert is raised for a test send.
				_logger.LogWarning(exception,
					"The test lottery reminder could not be sent to admin {AdminDiscordId}",
					requestingAdminDiscordId);
				delivered = false;
			}

			return delivered
				? new LotteryReminderOutcome(LotteryReminderDisposition.Sent,
					"The test reminder was sent to you.", target.RoundKey, audience.Recipients.Count)
				: new LotteryReminderOutcome(LotteryReminderDisposition.Failed,
					"The test reminder could not be sent to you.", target.RoundKey);
		}
		catch (OperationCanceledException)
		{
			return new LotteryReminderOutcome(LotteryReminderDisposition.Failed,
				"The test reminder was cancelled.");
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "The test lottery reminder failed");
			return new LotteryReminderOutcome(LotteryReminderDisposition.Failed,
				$"The test reminder could not be built: {exception.Message}");
		}
	}

	#endregion

	private LotteryReminderOutcome EmptyAudienceOutcome(bool anyOptIns, string roundKey)
	{
		return anyOptIns
			? new LotteryReminderOutcome(LotteryReminderDisposition.NobodyEligible,
				"Nobody has unused lottery guesses right now, so no reminder was posted.", roundKey)
			: new LotteryReminderOutcome(LotteryReminderDisposition.NobodyOptedIn,
				"Nobody has lottery reminders turned on, so no reminder was posted.", roundKey);
	}

	/// <summary>One needs-admin alert for a failed reminder; never throws, whatever the alert channel does.</summary>
	private async Task PublishFailureAlertAsync(LotteryReminderTarget target, Exception exception, int attempts,
		CancellationToken cancellationToken)
	{
		try
		{
			await _alerts.PublishAsync(new LotteryAlert(
				Title: $"The lottery reminder for round {target.RoundKey} could not be posted",
				RoundKey: target.RoundKey,
				FailedStep: null,
				Error: exception.Message,
				Attempts: attempts,
				NextRetryAt: null,
				NeedsAdmin: true), cancellationToken);
		}
		catch (Exception alertException)
		{
			_logger.LogWarning(alertException,
				"The lottery reminder failure alert for round {RoundKey} could not be published", target.RoundKey);
		}
	}

	private static ulong? ParseDiscordId(string? discordId)
	{
		return ulong.TryParse(discordId, out var parsed) && parsed != 0 ? parsed : null;
	}

	private static async Task<Dictionary<ulong, int>> CountByDiscordIdAsync<T>(Task<List<T>> rows,
		Func<T, ulong> discordIdOf)
	{
		return (await rows)
			.GroupBy(discordIdOf)
			.ToDictionary(group => group.Key, group => group.Count());
	}

	/// <summary>The resolved next draw, or why there is none (a skipped round, no schedule).</summary>
	private sealed record ResolvedTarget(
		LotteryReminderTarget? Target,
		string? SilentDisposition,
		string? Message)
	{
		public static ResolvedTarget Of(LotteryReminderTarget target)
		{
			return new ResolvedTarget(target, null, null);
		}

		public static ResolvedTarget Silent(string disposition, string message)
		{
			return new ResolvedTarget(null, disposition, message);
		}
	}

	/// <summary>An in-memory retry slot; the reminder has no round document to record attempts on.</summary>
	private sealed record PendingAttempt(string RoundKey, int Attempts, DateTime NextRetryAtUtc);
}
