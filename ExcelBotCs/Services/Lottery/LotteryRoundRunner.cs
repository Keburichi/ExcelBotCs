using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The round engine (spec §5.1, §5.3, §5.10). It owns the state machine, the pot and the winner writes, and it is
///     the single code path for the scheduled run, the manual run and the admin recovery actions.
///     <para>
///         Ordering guarantees: the winning number is stored before anything else so a retry never draws twice; the
///         guess set is frozen into <c>lottery_entries</c> before the number is chosen; the result, entries, winners and
///         ledger are persisted before the announcement; and the live guesses are cleared only once the announcement
///         was posted, immediately before the round becomes <c>completed</c>. There are no multi-document
///         transactions, so every write is either conditional on the status it transitions from or idempotent by a
///         deterministic key (round key, ledger idempotency key, natural winner/entry keys).
///     </para>
/// </summary>
public sealed class LotteryRoundRunner : ILotteryRoundRunner
{
	private const ulong NoActor = 0;

	private readonly ILotteryAnnouncementPublisher _announcements;
	private readonly IExtraLotteryGuessRepository _extraGuesses;
	private readonly ILotteryEntryRepository _entries;
	private readonly ILotteryGuessRepository _guesses;
	private readonly IMemberService _members;
	private readonly ILotteryNumberGenerator _numbers;
	private readonly ILotteryPotLedgerService _pot;
	private readonly ILotteryRoundRepository _rounds;
	private readonly ILotterySettingsService _settingsService;
	private readonly ILogger<LotteryRoundRunner> _logger;
	private readonly LotteryStartupCheckService? _startupCheck;
	private readonly TimeProvider _time;
	private readonly ILotteryWinnerRepository _winners;

	private string? _lastPreviewRoundKey;

	public LotteryRoundRunner(
		ILotterySettingsService settingsService,
		ILotteryRoundRepository rounds,
		ILotteryEntryRepository entries,
		ILotteryWinnerRepository winners,
		ILotteryPotLedgerService pot,
		ILotteryGuessRepository guesses,
		IExtraLotteryGuessRepository extraGuesses,
		IMemberService members,
		ILotteryNumberGenerator numbers,
		ILotteryAnnouncementPublisher announcements,
		TimeProvider time,
		ILogger<LotteryRoundRunner> logger,
		LotteryStartupCheckService? startupCheck = null)
	{
		_settingsService = settingsService;
		_rounds = rounds;
		_entries = entries;
		_winners = winners;
		_pot = pot;
		_guesses = guesses;
		_extraGuesses = extraGuesses;
		_members = members;
		_numbers = numbers;
		_announcements = announcements;
		_time = time;
		_logger = logger;
		_startupCheck = startupCheck;

		// Informational only: there is a single replica, and the claim is settled by the conditional status update.
		ClaimOwner = $"runner-{Environment.MachineName}-{Guid.NewGuid():N}";
	}

	/// <summary>The claim label written on a round this instance claimed.</summary>
	public string ClaimOwner { get; }

	public async Task<LotteryTickResult> TickAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			return await TickCoreAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			// Host shutdown; not an error, and not something to alert on.
			return new LotteryTickResult(LotteryTickAction.Nothing, "The lottery tick was cancelled.");
		}
		catch (Exception exception)
		{
			// A scheduler tick must never take the host down. Whatever step failed is recorded on the round by the
			// step-specific handlers; anything that escaped them is retried on the next tick.
			_logger.LogError(exception, "The lottery scheduler tick failed; the next tick will retry");
			return new LotteryTickResult(LotteryTickAction.Nothing,
				$"The lottery scheduler tick failed: {exception.Message}");
		}
	}

	public async Task<LotteryRunResult> RunNowAsync(ulong actorDiscordId,
		CancellationToken cancellationToken = default)
	{
		var schedule = await _settingsService.GetEffectiveAsync(cancellationToken);
		var lotterySchedule = LotterySchedule.CreateFrom(schedule);
		var now = Now();

		var pending = await _rounds.GetNewestBlockingAsync();
		if (pending is not null)
			return new LotteryRunResult(LotteryRunDisposition.Blocked,
				$"Round {pending.RoundKey} is still {pending.Status}; retry or void it before starting a new round.",
				pending);

		var target = await ResolveDueTargetAsync(lotterySchedule, now);
		var existing = await _rounds.GetByRoundKeyAsync(target.RoundKey);
		if (existing is not null && existing.Status != LotteryRoundStatus.Scheduled)
			return AlreadyClaimed(existing);

		return await RunRoundAsync(target, actorDiscordId, false, cancellationToken);
	}

	public async Task<LotteryRunResult> PreviewAsync(CancellationToken cancellationToken = default)
	{
		var settings = await _settingsService.GetEffectiveAsync(cancellationToken);
		var schedule = LotterySchedule.CreateFrom(settings);
		var target = await ResolveUpcomingTargetAsync(schedule, Now());

		return await PreviewForSlotAsync(target, settings, cancellationToken);
	}

	public async Task<LotteryRunResult> RetryAsync(ulong? actorDiscordId,
		CancellationToken cancellationToken = default)
	{
		var pending = await _rounds.GetNewestBlockingAsync();
		if (pending is null)
			return new LotteryRunResult(LotteryRunDisposition.NothingDue, "There is no unfinished round to resume.");

		var schedule = LotterySchedule.CreateFrom(await _settingsService.GetEffectiveAsync(cancellationToken));

		switch (pending.Status)
		{
			case LotteryRoundStatus.Failed:
				return await RetryFailedAsync(pending, actorDiscordId, schedule, cancellationToken);

			case LotteryRoundStatus.Drawing:
				// An admin retry acts now instead of waiting for the recorded backoff.
				return await RunRoundAsync(LotteryRoundPlanner.SlotOf(schedule, pending), actorDiscordId, false,
					cancellationToken, ignoreRetry: true);

			case LotteryRoundStatus.Drawn:
				return await AnnounceAndCompleteAsync(pending.RoundKey, false, actorDiscordId, cancellationToken);

			case LotteryRoundStatus.Announced:
				return await CompleteAsync(pending.RoundKey, cancellationToken);

			default:
				return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
					$"Round {pending.RoundKey} is {pending.Status} and cannot be resumed.", pending);
		}
	}

	public async Task<LotteryRunResult> SkipAsync(ulong actorDiscordId, string? reason = null,
		CancellationToken cancellationToken = default)
	{
		var settings = await _settingsService.GetEffectiveAsync(cancellationToken);
		var schedule = LotterySchedule.CreateFrom(settings);
		var now = Now();

		var pending = await _rounds.GetNewestBlockingAsync();
		if (pending is not null)
			return new LotteryRunResult(LotteryRunDisposition.Blocked,
				$"Round {pending.RoundKey} is still {pending.Status}; retry or void it before skipping a week.",
				pending);

		var slot = await ResolveUpcomingTargetAsync(schedule, now);
		var round = await _rounds.EnsureScheduledAsync(slot.RoundKey, slot.ScheduledAtUtc);
		if (round.Status != LotteryRoundStatus.Scheduled)
			return new LotteryRunResult(LotteryRunDisposition.AlreadyRun,
				$"Round {slot.RoundKey} is already {round.Status} and cannot be skipped.", round);

		if (!await _rounds.TryMarkSkippedAsync(slot.RoundKey))
			return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
				$"Round {slot.RoundKey} changed state before it could be skipped.", round);

		_logger.LogInformation("Lottery round {RoundKey} skipped by {Actor}: {Reason}", slot.RoundKey,
			Actor(actorDiscordId), reason ?? "no reason given");

		var pot = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
		return new LotteryRunResult(LotteryRunDisposition.Skipped,
			$"Round {slot.RoundKey} was skipped. Guesses carry over, the weekly allowance is unchanged and the pot " +
			$"stays at {pot:N0} Gil.",
			await _rounds.GetByRoundKeyAsync(slot.RoundKey), pot);
	}

	public async Task<LotteryRunResult> VoidAsync(string roundKey, ulong actorDiscordId, string reason,
		CancellationToken cancellationToken = default)
	{
		var round = await _rounds.GetByRoundKeyAsync(roundKey);
		if (round is null)
			return new LotteryRunResult(LotteryRunDisposition.NotFound, $"Round {roundKey} does not exist.");

		if (round.Status is not (LotteryRoundStatus.Drawn or LotteryRoundStatus.Announced
		    or LotteryRoundStatus.Completed or LotteryRoundStatus.Failed))
			return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
				$"Round {roundKey} is {round.Status} and cannot be voided.", round);

		var settings = await _settingsService.GetEffectiveAsync(cancellationToken);
		var reversed = await ReverseLedgerAsync(round, reason, actorDiscordId, settings, cancellationToken);
		var now = Now().UtcDateTime;
		if (!await _rounds.TryMarkVoidedAsync(roundKey, reason, actorDiscordId, now))
			return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
				$"Round {roundKey} changed state before it could be voided.",
				await _rounds.GetByRoundKeyAsync(roundKey));

		try
		{
			await _announcements.AnnotateVoidedAsync(round, reason, cancellationToken);
		}
		catch (Exception exception)
		{
			// The ledger reversal and the round annotation are the record of truth; a Discord failure must not undo
			// the void. #111 owns the copy and the retraction behaviour.
			_logger.LogWarning(exception, "Round {RoundKey} was voided but its post could not be annotated", roundKey);
		}

		var pot = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
		_logger.LogInformation("Lottery round {RoundKey} voided by {Actor}: {Reason} ({Reversed} ledger entries " +
		                       "reversed; pot now {Pot})", roundKey, Actor(actorDiscordId), reason, reversed, pot);

		return new LotteryRunResult(LotteryRunDisposition.Voided,
			$"Round {roundKey} was voided: {reason}. {reversed} ledger entr{(reversed == 1 ? "y was" : "ies were")} " +
			$"reversed and the pot is {pot:N0} Gil.",
			await _rounds.GetByRoundKeyAsync(roundKey), pot);
	}

	public async Task<LotteryRunResult> RescheduleAsync(DateTime newScheduledAtUtc, ulong actorDiscordId,
		string? roundKey = null, CancellationToken cancellationToken = default)
	{
		var schedule = LotterySchedule.CreateFrom(await _settingsService.GetEffectiveAsync(cancellationToken));
		var now = Now();

		string currentRoundKey;
		if (roundKey is null)
		{
			currentRoundKey = (await ResolveUpcomingTargetAsync(schedule, now)).RoundKey;
		}
		else
		{
			if (!LotteryRoundKey.TryParse(roundKey, out _))
				return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
					$"'{roundKey}' is not a round key in YYYY-MM-DD format.");
			currentRoundKey = roundKey;
		}

		var existing = await _rounds.GetByRoundKeyAsync(currentRoundKey);
		if (existing is null)
		{
			// Future rounds are created lazily, so the admin can move a round that has no document yet.
			if (!LotteryRoundKey.TryParse(currentRoundKey, out var localDate))
				return new LotteryRunResult(LotteryRunDisposition.NotFound,
					$"Round {currentRoundKey} does not exist.");

			existing = await _rounds.EnsureScheduledAsync(currentRoundKey, schedule.SlotForDate(localDate).ScheduledAtUtc);
		}

		if (existing.Status != LotteryRoundStatus.Scheduled)
			return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
				$"Round {currentRoundKey} is {existing.Status}; only a scheduled round can be rescheduled.", existing);

		var newInstant = DateTime.SpecifyKind(newScheduledAtUtc, DateTimeKind.Utc);
		var newRoundKey = schedule.RoundKeyFor(new DateTimeOffset(newInstant, TimeSpan.Zero));
		if (newRoundKey == currentRoundKey && newInstant == existing.ScheduledAt)
			return new LotteryRunResult(LotteryRunDisposition.Rescheduled,
				$"Round {currentRoundKey} is already scheduled for {newInstant:O}.", existing);

		if (!await _rounds.TryRescheduleAsync(currentRoundKey, newRoundKey, newInstant))
			return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
				$"Round {currentRoundKey} could not be moved to {newRoundKey}: the date may already have a round.",
				existing);

		_logger.LogInformation("Lottery round {RoundKey} rescheduled to {NewRoundKey} at {ScheduledAt} by {Actor}",
			currentRoundKey, newRoundKey, newInstant, Actor(actorDiscordId));

		return new LotteryRunResult(LotteryRunDisposition.Rescheduled,
			$"Round {currentRoundKey} was moved to {newRoundKey} at {newInstant:O}. Guesses carry over.",
			await _rounds.GetByRoundKeyAsync(newRoundKey));
	}

	public async Task<LotteryStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default)
	{
		var settings = await _settingsService.GetEffectiveAsync(cancellationToken);
		var schedule = LotterySchedule.CreateFrom(settings);
		var now = Now();

		var pending = await _rounds.GetNewestBlockingAsync();
		var latest = (await _rounds.GetLatestAsync(1)).FirstOrDefault();
		var pot = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
		var nextSlot = schedule.NextSlotAfter(now);

		return new LotteryStatusSnapshot(settings.Status.Kind, settings.Status.SchedulerEnabled, pot, latest, pending,
			nextSlot.ScheduledAtUtc, nextSlot.RoundKey, pending?.LastError);
	}

	public async Task<long> GetPotAsync(CancellationToken cancellationToken = default)
	{
		var settings = await _settingsService.GetEffectiveAsync(cancellationToken);
		return await _pot.EnsureBalanceAsync(settings.BasePot, cancellationToken);
	}

	#region Tick

	private async Task<LotteryTickResult> TickCoreAsync(CancellationToken cancellationToken)
	{
		if (_startupCheck is { Result: null })
			return new LotteryTickResult(LotteryTickAction.Nothing,
				"The lottery startup check has not finished yet; nothing is scheduled on this tick.");

		if (_startupCheck?.Result is { IsDegraded: true })
			return new LotteryTickResult(LotteryTickAction.Nothing,
				"Lottery automation starts degraded; nothing is scheduled until the configuration is fixed.");

		var settings = await _settingsService.GetEffectiveAsync(cancellationToken);
		if (!settings.AutomationEnabled)
			return new LotteryTickResult(LotteryTickAction.Disabled,
				"Lottery automation is disabled (Lottery:Automation:Enabled=false).");

		if (settings.PauseAutomation)
			return new LotteryTickResult(LotteryTickAction.Paused,
				"Lottery automation is paused by the stored settings.");

		var schedule = LotterySchedule.CreateFrom(settings);
		var now = Now();
		var state = await ReadStateAsync(schedule, now, cancellationToken);
		var plan = LotteryRoundPlanner.Plan(schedule, now, state, true, false, settings.DryRun);

		_logger.LogDebug("Lottery tick: {Action} — {Message}", plan.Action, plan.Message);

		switch (plan.Action)
		{
			case LotteryTickAction.Run when plan.DryRun:
				return await PreviewTickAsync(plan, settings, cancellationToken);

			case LotteryTickAction.Run:
				if (plan.Slot is null)
					return new LotteryTickResult(LotteryTickAction.Nothing, "The run plan has no slot to run.");

				await SkipMissedSlotsAsync(plan, cancellationToken);
				var run = await RunRoundAsync(plan.Slot, null, false, cancellationToken);
				return new LotteryTickResult(run.Disposition, run.Message, run.Round, run.Pot);

			case LotteryTickAction.ResumeDraw when plan.Round is not null:
				var resumed = await RunRoundAsync(LotteryRoundPlanner.SlotOf(schedule, plan.Round), null, false,
					cancellationToken);
				return new LotteryTickResult(resumed.Disposition, resumed.Message, resumed.Round, resumed.Pot);

			case LotteryTickAction.ResumeAnnouncement when plan.Round is not null:
				var announced = await AnnounceAndCompleteAsync(plan.Round.RoundKey, true, null, cancellationToken);
				return new LotteryTickResult(announced.Disposition, announced.Message, announced.Round,
					announced.Pot);

			case LotteryTickAction.ResumeCleanup when plan.Round is not null:
				var completed = await CompleteAsync(plan.Round.RoundKey, cancellationToken);
				return new LotteryTickResult(completed.Disposition, completed.Message, completed.Round,
					completed.Pot);

			default:
				return new LotteryTickResult(plan.Action, plan.Message, plan.Round);
		}
	}

	private async Task<LotteryTickState> ReadStateAsync(LotterySchedule schedule, DateTimeOffset now,
		CancellationToken cancellationToken)
	{
		var blocking = await _rounds.GetNewestBlockingAsync();
		var scheduledDue = await _rounds.GetNewestScheduledDueAsync(now.UtcDateTime);
		var dueSlot = schedule.MostRecentDueSlot(now);
		var dueRound = await _rounds.GetByRoundKeyAsync(dueSlot.RoundKey);
		var latest = (await _rounds.GetLatestAsync(1)).FirstOrDefault();

		return new LotteryTickState(blocking, scheduledDue, dueRound, latest);
	}

	private async Task SkipMissedSlotsAsync(LotteryTickPlan plan, CancellationToken cancellationToken)
	{
		foreach (var slot in plan.Skips)
		{
			await _rounds.EnsureScheduledAsync(slot.RoundKey, slot.ScheduledAtUtc);
			if (await _rounds.TryMarkSkippedAsync(slot.RoundKey))
				_logger.LogInformation(
					"Lottery round {RoundKey} was missed while the bot was down; it is marked skipped and the pot does " +
					"not grow for it", slot.RoundKey);
		}
	}

	private async Task<LotteryTickResult> PreviewTickAsync(LotteryTickPlan plan,
		LotteryEffectiveSettings settings, CancellationToken cancellationToken)
	{
		if (plan.Slot is null)
			return new LotteryTickResult(LotteryTickAction.Nothing, "The dry-run plan has no slot to preview.");

		var preview = await PreviewForSlotAsync(plan.Slot, settings, cancellationToken);
		return new LotteryTickResult(LotteryRunDisposition.DryRun, preview.Message, preview.Round, preview.Pot);
	}

	#endregion

	#region Run

	private async Task<LotteryRunResult> RunRoundAsync(LotteryScheduleSlot slot, ulong? actorDiscordId,
		bool fromFailed, CancellationToken cancellationToken, bool ignoreRetry = false)
	{
		var now = Now();
		await _rounds.EnsureScheduledAsync(slot.RoundKey, slot.ScheduledAtUtc);

		var claimed = await _rounds.TryClaimForDrawAsync(slot.RoundKey, ClaimOwner, now.UtcDateTime, fromFailed,
			ignoreRetry);
		if (claimed is null)
		{
			// The unique RoundKey plus the conditional status update is the entire double-draw guarantee.
			var current = await _rounds.GetByRoundKeyAsync(slot.RoundKey);
			return current is null
				? new LotteryRunResult(LotteryRunDisposition.NotFound, $"Round {slot.RoundKey} does not exist.")
				: AlreadyClaimed(current);
		}

		_logger.LogInformation("Lottery round {RoundKey} claimed for drawing by {Actor} ({ClaimOwner})", slot.RoundKey,
			Actor(actorDiscordId), ClaimOwner);

		LotteryEffectiveSettings settings;
		LotteryRound drawn;
		try
		{
			settings = await _settingsService.GetEffectiveAsync(cancellationToken);
			var round = await _rounds.GetByRoundKeyAsync(slot.RoundKey) ??
			            throw new InvalidOperationException($"Round {slot.RoundKey} vanished after its claim.");
			drawn = await DrawAsync(round, settings, cancellationToken);
		}
		catch (Exception exception)
		{
			return await RegisterDrawFailureAsync(slot.RoundKey, exception, cancellationToken);
		}

		return await AnnounceAndCompleteAsync(drawn.RoundKey, true, actorDiscordId, cancellationToken);
	}

	private async Task<LotteryRound> DrawAsync(LotteryRound round, LotteryEffectiveSettings settings,
		CancellationToken cancellationToken)
	{
		var roundKey = round.RoundKey;
		var now = Now();

		// Freeze the guess set first: the draw reads lottery_entries, never the live guesses again, so a resumed
		// attempt and the winner calculation both work on exactly the same set.
		if (round.WinningNumber is null) await CopyEntriesAsync(roundKey, cancellationToken);

		int winningNumber;
		long potBefore;

		if (round.WinningNumber is { } storedNumber && round.PotBefore is { } storedPot)
		{
			// A previous attempt already drew. Never draw again, and use the pot that attempt froze for the shares.
			winningNumber = storedNumber;
			potBefore = storedPot;
		}
		else
		{
			potBefore = await ResolvePotBeforeAsync(roundKey, settings, cancellationToken);
			winningNumber = round.WinningNumber ?? _numbers.DrawWinningNumber();

			var stored = await _rounds.TrySetWinningNumberAsync(roundKey, winningNumber, now.UtcDateTime, potBefore);
			if (!stored)
			{
				var fresh = await _rounds.GetByRoundKeyAsync(roundKey) ??
				            throw new InvalidOperationException($"Round {roundKey} vanished while storing its number.");
				winningNumber = fresh.WinningNumber ?? winningNumber;
				potBefore = fresh.PotBefore ?? potBefore;
			}

			_logger.LogInformation("Lottery round {RoundKey} drew {WinningNumber} (pot at the draw: {PotBefore})",
				roundKey, winningNumber, potBefore);
		}

		var entries = await _entries.GetByRoundKeyAsync(roundKey);
		var winningEntries = entries
			.Where(entry => entry.Number == winningNumber)
			.OrderBy(entry => entry.DateCreated)
			.ThenBy(entry => entry.Id, StringComparer.Ordinal)
			.ToList();

		long potAfter;

		if (winningEntries.Count == 0)
		{
			var balance = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
			potAfter = LotteryPotMath.AfterNoWinner(balance, settings.WeeklyIncrement);
			await _pot.AppendAsync(roundKey, LotteryPotLedgerType.WeeklyIncrement, settings.WeeklyIncrement, potAfter,
				$"No winner in round {roundKey}; the pot grows by the weekly increment.", null,
				LotteryPotLedgerKeys.WeeklyIncrement(roundKey), cancellationToken);

			_logger.LogInformation("Lottery round {RoundKey} had no winner; the pot grows from {PotBefore} to {PotAfter}",
				roundKey, balance, potAfter);
		}
		else
		{
			potAfter = await WriteWinnersAsync(roundKey, winningNumber, winningEntries, potBefore, settings,
				cancellationToken);
		}

		var retryUntil = now.UtcDateTime + LotteryRetryPolicy.AnnouncementWindow;
		await _rounds.TryMarkDrawnAsync(roundKey, potAfter, retryUntil);

		return await _rounds.GetByRoundKeyAsync(roundKey) ??
		       throw new InvalidOperationException($"Round {roundKey} vanished after it was drawn.");
	}

	private async Task<long> WriteWinnersAsync(string roundKey, int winningNumber,
		IReadOnlyList<LotteryEntry> winningEntries, long potBefore, LotteryEffectiveSettings settings,
		CancellationToken cancellationToken)
	{
		var shares = LotteryPotMath.Split(potBefore, winningEntries.Count);
		var members = await _members.GetByDiscordIds(winningEntries.Select(entry => entry.DiscordId).Distinct());

		for (var index = 0; index < winningEntries.Count; index++)
		{
			var entry = winningEntries[index];
			var winner = new LotteryWinner
			{
				RoundKey = roundKey,
				DiscordId = entry.DiscordId,
				DisplayName = DisplayNameOf(entry.DiscordId, members),
				Guess = winningNumber,
				GilPrize = shares[index],
				BonusItems = [],
				PayoutStatus = LotteryPayoutStatus.Pending
			};

			if (await _winners.AddIfMissingAsync(winner))
				_logger.LogInformation(
					"Lottery round {RoundKey} winner {DiscordId} takes {Share} Gil of the {Pot} Gil pot",
					roundKey, entry.DiscordId, shares[index], potBefore);
		}

		// The payout debits the pot at the draw; the reset then returns it to the base. The balances are taken from an
		// existing entry when a previous attempt already wrote it, so a retry cannot move the pot twice.
		var payout = await _pot.FindAsync(LotteryPotLedgerKeys.Payout(roundKey), cancellationToken);
		var payoutBalanceAfter = payout?.BalanceAfter
		                         ?? await _pot.GetBalanceAsync(settings.BasePot, cancellationToken) - potBefore;

		await _pot.AppendAsync(roundKey, LotteryPotLedgerType.Payout, -potBefore, payoutBalanceAfter,
			$"Prize payout for round {roundKey}: {winningEntries.Count} winner(s) share the {potBefore} Gil pot.",
			null, LotteryPotLedgerKeys.Payout(roundKey), cancellationToken);

		var reset = await _pot.FindAsync(LotteryPotLedgerKeys.Reset(roundKey), cancellationToken);
		var potAfter = reset?.BalanceAfter ?? payoutBalanceAfter + settings.BasePot;

		await _pot.AppendAsync(roundKey, LotteryPotLedgerType.Reset, settings.BasePot, potAfter,
			$"The pot resets to the base after round {roundKey} was won.", null,
			LotteryPotLedgerKeys.Reset(roundKey), cancellationToken);

		return potAfter;
	}

	private async Task CopyEntriesAsync(string roundKey, CancellationToken cancellationToken)
	{
		var guesses = await _guesses.GetAsync();
		foreach (var guess in guesses)
			await _entries.AddIfMissingAsync(new LotteryEntry
			{
				RoundKey = roundKey,
				DiscordId = guess.DiscordId,
				Number = guess.Number,
				// Keep the instant the member guessed: the rounding remainder goes to the earliest winning entry.
				DateCreated = guess.DateCreated
			});
	}

	private async Task<long> ResolvePotBeforeAsync(string roundKey, LotteryEffectiveSettings settings,
		CancellationToken cancellationToken)
	{
		// A resumed draw must reuse the pot its first attempt froze, even when that attempt got as far as the ledger.
		var payout = await _pot.FindAsync(LotteryPotLedgerKeys.Payout(roundKey), cancellationToken);
		if (payout is not null) return -payout.Amount;

		var increment = await _pot.FindAsync(LotteryPotLedgerKeys.WeeklyIncrement(roundKey), cancellationToken);
		if (increment is not null) return increment.BalanceAfter - increment.Amount;

		return await _pot.EnsureBalanceAsync(settings.BasePot, cancellationToken);
	}

	private async Task<LotteryRunResult> RegisterDrawFailureAsync(string roundKey, Exception exception,
		CancellationToken cancellationToken)
	{
		var round = await _rounds.GetByRoundKeyAsync(roundKey);
		var attempts = (round?.Attempts ?? 0) + 1;
		var delay = LotteryRetryPolicy.NextDrawDelay(attempts);
		var now = Now().UtcDateTime;
		var error = Describe(exception);

		if (delay is null)
		{
			await _rounds.RegisterFailureAsync(roundKey, LotteryFailedStep.Draw, LotteryRoundStatus.Failed, attempts,
				error, null, round?.RetryUntil);

			_logger.LogError(exception,
				"Lottery round {RoundKey} failed at the draw step after {Attempts} attempt(s); the round is terminal " +
				"and blocks the schedule until an admin retries or voids it", roundKey, attempts);

			return new LotteryRunResult(LotteryRunDisposition.Failed,
				$"Round {roundKey} failed at the draw step after {attempts} attempt(s): {error}",
				await _rounds.GetByRoundKeyAsync(roundKey));
		}

		var nextRetryAt = now + delay.Value;
		await _rounds.RegisterFailureAsync(roundKey, LotteryFailedStep.Draw, LotteryRoundStatus.Drawing, attempts,
			error, nextRetryAt, round?.RetryUntil);

		_logger.LogError(exception,
			"Lottery round {RoundKey} failed at the draw step (attempt {Attempts}); next retry at {NextRetryAt}",
			roundKey, attempts, nextRetryAt);

		return new LotteryRunResult(LotteryRunDisposition.Retrying,
			$"Round {roundKey} failed at the draw step; retrying at {nextRetryAt:O}.", round);
	}

	#endregion

	#region Announce and complete

	private async Task<LotteryRunResult> AnnounceAndCompleteAsync(string roundKey, bool enforceWindow,
		ulong? actorDiscordId, CancellationToken cancellationToken)
	{
		var round = await _rounds.GetByRoundKeyAsync(roundKey);
		if (round is null)
			return new LotteryRunResult(LotteryRunDisposition.NotFound, $"Round {roundKey} does not exist.");

		if (round.Status == LotteryRoundStatus.Completed)
			return new LotteryRunResult(LotteryRunDisposition.Ran, $"Round {roundKey} is already completed.", round,
				round.PotAfter);

		if (round.Status == LotteryRoundStatus.Announced)
			return await CompleteAsync(roundKey, cancellationToken);

		if (round.Status != LotteryRoundStatus.Drawn)
			return new LotteryRunResult(LotteryRunDisposition.AlreadyRun,
				$"Round {roundKey} is {round.Status} and cannot be announced.", round);

		try
		{
			if (round.PostingStartedAt is not null)
			{
				// A previous attempt started posting. Search the channel before posting again, so a crash between the
				// Discord send and the state write cannot duplicate the announcement (§5.10).
				var existing = await _announcements.FindExistingPostAsync(round, cancellationToken);
				if (existing is not null)
				{
					_logger.LogInformation(
						"Round {RoundKey} already has announcement {MessageId}; adopting it instead of posting again",
						roundKey, existing);
					await _rounds.TryMarkAnnouncedAsync(roundKey, existing);
					return await CompleteAsync(roundKey, cancellationToken);
				}
			}
			else
			{
				await _rounds.TryStartPostingAsync(roundKey, Now().UtcDateTime);
			}

			var winners = await _winners.GetByRoundKeyAsync(roundKey);
			var announcement = new LotteryAnnouncement(round, winners, round.PotBefore ?? 0, round.PotAfter ?? 0,
				false);

			var post = await _announcements.PublishAsync(announcement, cancellationToken);
			await _rounds.TryMarkAnnouncedAsync(roundKey, post.MessageId);

			_logger.LogInformation("Lottery round {RoundKey} announced by {Actor}", roundKey, Actor(actorDiscordId));
			return await CompleteAsync(roundKey, cancellationToken);
		}
		catch (Exception exception)
		{
			return await RegisterAnnouncementFailureAsync(round, exception, enforceWindow, cancellationToken);
		}
	}

	private async Task<LotteryRunResult> RegisterAnnouncementFailureAsync(LotteryRound round, Exception exception,
		bool enforceWindow, CancellationToken cancellationToken)
	{
		var attempts = round.Attempts + 1;
		var now = Now().UtcDateTime;
		var retryUntil = round.RetryUntil ?? (round.RunAt ?? now) + LotteryRetryPolicy.AnnouncementWindow;
		var error = Describe(exception);
		var windowOpen = LotteryRetryPolicy.IsAnnouncementWindowOpen(retryUntil, now);

		if (enforceWindow && !windowOpen)
		{
			await _rounds.RegisterFailureAsync(round.RoundKey, LotteryFailedStep.Announcement,
				LotteryRoundStatus.Failed, attempts, error, null, retryUntil);

			_logger.LogError(exception,
				"Lottery round {RoundKey} could not be announced within the {Window} window after {Attempts} " +
				"attempt(s); the round failed at the announcement step, was not re-drawn, and needs an admin",
				round.RoundKey, LotteryRetryPolicy.AnnouncementWindow, attempts);

			return new LotteryRunResult(LotteryRunDisposition.Failed,
				$"Round {round.RoundKey} could not be announced: {error}", round, round.PotAfter);
		}

		var nextRetryAt = now + LotteryRetryPolicy.NextAnnouncementDelay(attempts);
		await _rounds.RegisterFailureAsync(round.RoundKey, LotteryFailedStep.Announcement, LotteryRoundStatus.Drawn,
			attempts, error, nextRetryAt, retryUntil);

		_logger.LogError(exception,
			"Lottery round {RoundKey} failed at the announcement step (attempt {Attempts}); next retry at {NextRetryAt}",
			round.RoundKey, attempts, nextRetryAt);

		return new LotteryRunResult(LotteryRunDisposition.Drawn,
			$"Round {round.RoundKey} is drawn; the announcement failed and will be retried at {nextRetryAt:O}. " +
			"The result will not be drawn again.", round, round.PotAfter);
	}

	private async Task<LotteryRunResult> CompleteAsync(string roundKey, CancellationToken cancellationToken)
	{
		var round = await _rounds.GetByRoundKeyAsync(roundKey);
		if (round is null)
			return new LotteryRunResult(LotteryRunDisposition.NotFound, $"Round {roundKey} does not exist.");

		if (round.Status == LotteryRoundStatus.Completed)
			return new LotteryRunResult(LotteryRunDisposition.Ran, $"Round {roundKey} is already completed.", round,
				round.PotAfter);

		if (round.Status != LotteryRoundStatus.Announced)
			return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
				$"Round {roundKey} is {round.Status} and cannot be completed.", round);

		var settings = await _settingsService.GetEffectiveAsync(cancellationToken);

		try
		{
			// Flush first, mark completed last: a crash in between repeats the flush (harmless) instead of leaving old
			// guesses behind for the next round.
			await _guesses.DeleteAllAsync();
			await _extraGuesses.DeleteAllAsync();

			if (!await _rounds.TryMarkCompletedAsync(roundKey))
				return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
					$"Round {roundKey} changed state before it could be completed.",
					await _rounds.GetByRoundKeyAsync(roundKey));

			var pot = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
			_logger.LogInformation("Lottery round {RoundKey} completed; live guesses were cleared", roundKey);

			return new LotteryRunResult(LotteryRunDisposition.Ran, $"Round {roundKey} is completed.", 
				await _rounds.GetByRoundKeyAsync(roundKey), pot);
		}
		catch (Exception exception)
		{
			return await RegisterCleanupFailureAsync(round, exception, cancellationToken);
		}
	}

	private async Task<LotteryRunResult> RegisterCleanupFailureAsync(LotteryRound round, Exception exception,
		CancellationToken cancellationToken)
	{
		var attempts = round.Attempts + 1;
		var delay = LotteryRetryPolicy.NextDrawDelay(attempts);
		var now = Now().UtcDateTime;
		var error = Describe(exception);

		if (delay is null)
		{
			await _rounds.RegisterFailureAsync(round.RoundKey, LotteryFailedStep.Cleanup, LotteryRoundStatus.Failed,
				attempts, error, null, round.RetryUntil);

			_logger.LogError(exception,
				"Lottery round {RoundKey} failed at the cleanup step after {Attempts} attempt(s); the round is " +
				"terminal and needs an admin", round.RoundKey, attempts);

			return new LotteryRunResult(LotteryRunDisposition.Failed,
				$"Round {round.RoundKey} failed at the cleanup step: {error}",
				await _rounds.GetByRoundKeyAsync(round.RoundKey), round.PotAfter);
		}

		var nextRetryAt = now + delay.Value;
		await _rounds.RegisterFailureAsync(round.RoundKey, LotteryFailedStep.Cleanup, LotteryRoundStatus.Announced,
			attempts, error, nextRetryAt, round.RetryUntil);

		_logger.LogError(exception,
			"Lottery round {RoundKey} failed at the cleanup step (attempt {Attempts}); next retry at {NextRetryAt}",
			round.RoundKey, attempts, nextRetryAt);

		return new LotteryRunResult(LotteryRunDisposition.Retrying,
			$"Round {round.RoundKey} is announced; the cleanup failed and will be retried at {nextRetryAt:O}.",
			round, round.PotAfter);
	}

	#endregion

	#region Retry, preview, void

	private async Task<LotteryRunResult> RetryFailedAsync(LotteryRound failed, ulong? actorDiscordId,
		LotterySchedule schedule, CancellationToken cancellationToken)
	{
		switch (failed.FailedStep)
		{
			case LotteryFailedStep.Draw:
				if (!await _rounds.TryReopenFailedAsync(failed.RoundKey, LotteryFailedStep.Draw,
					    LotteryRoundStatus.Drawing, failed.RetryUntil))
					return new LotteryRunResult(LotteryRunDisposition.AlreadyRunning,
						$"Round {failed.RoundKey} could not be reopened for a draw retry.",
						await _rounds.GetByRoundKeyAsync(failed.RoundKey));

				return await RunRoundAsync(LotteryRoundPlanner.SlotOf(schedule, failed), actorDiscordId, true,
					cancellationToken, ignoreRetry: true);

			case LotteryFailedStep.Announcement:
				var retryUntil = Now().UtcDateTime + LotteryRetryPolicy.AnnouncementWindow;
				if (!await _rounds.TryReopenFailedAsync(failed.RoundKey, LotteryFailedStep.Announcement,
					    LotteryRoundStatus.Drawn, retryUntil))
					return new LotteryRunResult(LotteryRunDisposition.AlreadyRunning,
						$"Round {failed.RoundKey} could not be reopened for an announcement retry.",
						await _rounds.GetByRoundKeyAsync(failed.RoundKey));

				// An explicit admin retry re-arms the one-hour window instead of failing again immediately.
				return await AnnounceAndCompleteAsync(failed.RoundKey, false, actorDiscordId, cancellationToken);

			case LotteryFailedStep.Cleanup:
				if (!await _rounds.TryReopenFailedAsync(failed.RoundKey, LotteryFailedStep.Cleanup,
					    LotteryRoundStatus.Announced, failed.RetryUntil))
					return new LotteryRunResult(LotteryRunDisposition.AlreadyRunning,
						$"Round {failed.RoundKey} could not be reopened for a cleanup retry.",
						await _rounds.GetByRoundKeyAsync(failed.RoundKey));

				return await CompleteAsync(failed.RoundKey, cancellationToken);

			default:
				return new LotteryRunResult(LotteryRunDisposition.NotAllowed,
					$"Round {failed.RoundKey} failed at an unknown step '{failed.FailedStep}'.", failed);
		}
	}

	private async Task<LotteryRunResult> PreviewForSlotAsync(LotteryScheduleSlot slot,
		LotteryEffectiveSettings settings, CancellationToken cancellationToken)
	{
		if (_lastPreviewRoundKey == slot.RoundKey)
			return new LotteryRunResult(LotteryRunDisposition.DryRun,
				$"The dry run for round {slot.RoundKey} was already previewed; nothing is persisted.");

		var winningNumber = _numbers.DrawWinningNumber();
		var guesses = await _guesses.GetAsync();
		var winningEntries = guesses
			.Where(guess => guess.Number == winningNumber)
			.OrderBy(guess => guess.DateCreated)
			.ThenBy(guess => guess.Id, StringComparer.Ordinal)
			.ToList();

		var potBefore = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
		var potAfter = winningEntries.Count == 0
			? LotteryPotMath.AfterNoWinner(potBefore, settings.WeeklyIncrement)
			: settings.BasePot;

		var shares = winningEntries.Count == 0 ? [] : LotteryPotMath.Split(potBefore, winningEntries.Count);
		var members = await _members.GetByDiscordIds(winningEntries.Select(entry => entry.DiscordId).Distinct());
		var winners = winningEntries.Select((entry, index) => new LotteryWinner
		{
			RoundKey = slot.RoundKey,
			DiscordId = entry.DiscordId,
			DisplayName = DisplayNameOf(entry.DiscordId, members),
			Guess = winningNumber,
			GilPrize = shares[index],
			BonusItems = [],
			PayoutStatus = LotteryPayoutStatus.Pending
		}).ToList();

		var preview = new LotteryRound
		{
			RoundKey = slot.RoundKey,
			ScheduledAt = slot.ScheduledAtUtc,
			Status = LotteryRoundStatus.Scheduled,
			RunAt = Now().UtcDateTime,
			WinningNumber = winningNumber,
			PotBefore = potBefore,
			PotAfter = potAfter
		};

		await _announcements.PublishAsync(
			new LotteryAnnouncement(preview, winners, potBefore, potAfter, true), cancellationToken);

		// Only remember the preview once it was published: a failed preview is retried on the next tick.
		_lastPreviewRoundKey = slot.RoundKey;

		_logger.LogInformation(
			"Dry run preview for round {RoundKey}: winning number {WinningNumber}, {WinnerCount} winner(s), pot " +
			"afterwards {PotAfter}; nothing was persisted", slot.RoundKey, winningNumber, winners.Count, potAfter);

		return new LotteryRunResult(LotteryRunDisposition.DryRun,
			$"Dry run for round {slot.RoundKey}: the winning number would be {winningNumber}, " +
			$"{(winners.Count == 0 ? "nobody would win" : $"{winners.Count} winner(s)")} and the pot would be " +
			$"{potAfter:N0} Gil. Nothing was saved.", preview, potAfter);
	}

	private async Task<int> ReverseLedgerAsync(LotteryRound round, string reason, ulong actorDiscordId,
		LotteryEffectiveSettings settings, CancellationToken cancellationToken)
	{
		// Only the round's own movements are reversed; a correction is an admin decision and is never undone silently.
		var entries = (await _pot.GetByRoundKeyAsync(round.RoundKey, cancellationToken))
			.Where(entry => entry.Type is LotteryPotLedgerType.Payout or LotteryPotLedgerType.Reset
				or LotteryPotLedgerType.WeeklyIncrement)
			.OrderBy(entry => entry.DateCreated)
			.ThenBy(entry => entry.Id, StringComparer.Ordinal)
			.ToList();

		var balance = await _pot.GetBalanceAsync(settings.BasePot, cancellationToken);
		var reversed = 0;

		foreach (var entry in entries)
		{
			var key = LotteryPotLedgerKeys.VoidReversal(round.RoundKey, entry.Type);
			if (await _pot.FindAsync(key, cancellationToken) is not null) continue;

			var requested = -entry.Amount;
			var applied = LotteryPotMath.ApplyCorrection(balance, requested, settings.BasePot);
			balance += applied;

			var note = applied == requested
				? $"Void of round {round.RoundKey} ({entry.Type}): {reason}"
				: $"Void of round {round.RoundKey} ({entry.Type}): {reason} (clamped at the base pot)";

			await _pot.AppendAsync(round.RoundKey, LotteryPotLedgerType.Correction, applied, balance, note,
				actorDiscordId, key, cancellationToken);

			reversed++;
		}

		return reversed;
	}

	#endregion

	#region Helpers

	/// <summary>
	///     The round the manual actions operate on for "the current period": the due slot while its date is empty or
	///     still scheduled, otherwise the next slot. Skipping, moving and previewing naturally target the upcoming run.
	/// </summary>
	private async Task<LotteryScheduleSlot> ResolveUpcomingTargetAsync(LotterySchedule schedule, DateTimeOffset now)
	{
		var dueSlot = schedule.MostRecentDueSlot(now);
		var dueRound = await _rounds.GetByRoundKeyAsync(dueSlot.RoundKey);

		return dueRound is null || dueRound.Status == LotteryRoundStatus.Scheduled
			? dueSlot
			: schedule.NextSlotAfter(now);
	}

	/// <summary>
	///     The round the manual "Run now" operates on: the most recent due slot. That is the draw that should have run,
	///     so a run that was missed, failed or never scheduled can be started by hand. A due round that already ran,
	///     was skipped or was voided makes the manual run report "already run" instead of drawing a second round for a
	///     different date (§5.1 "one round per date").
	/// </summary>
	private Task<LotteryScheduleSlot> ResolveDueTargetAsync(LotterySchedule schedule, DateTimeOffset now)
	{
		return Task.FromResult(schedule.MostRecentDueSlot(now));
	}

	private DateTimeOffset Now()
	{
		return _time.GetUtcNow();
	}

	private static LotteryRunResult AlreadyClaimed(LotteryRound round)
	{
		return round.Status switch
		{
			LotteryRoundStatus.Drawing => new LotteryRunResult(LotteryRunDisposition.AlreadyRunning,
				$"Round {round.RoundKey} is already running; it will not be drawn twice.", round),
			LotteryRoundStatus.Failed => new LotteryRunResult(LotteryRunDisposition.Blocked,
				$"Round {round.RoundKey} failed at the {round.FailedStep} step; retry or void it.", round),
			_ => new LotteryRunResult(LotteryRunDisposition.AlreadyRun,
				$"Round {round.RoundKey} is already {round.Status}.", round)
		};
	}

	private static string Actor(ulong? actorDiscordId)
	{
		return actorDiscordId is null or NoActor ? "the system" : $"<@{actorDiscordId}>";
	}

	private static string DisplayNameOf(ulong discordId, IEnumerable<Member> members)
	{
		var member = members.FirstOrDefault(candidate => candidate.DiscordId == discordId.ToString());
		return string.IsNullOrWhiteSpace(member?.DiscordName) ? $"Unknown User ({discordId})" : member.DiscordName;
	}

	private static string Describe(Exception exception)
	{
		return exception is AggregateException { InnerException: { } inner }
			? inner.Message
			: exception.Message;
	}

	#endregion
}
