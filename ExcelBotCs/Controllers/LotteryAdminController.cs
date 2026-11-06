using System.Globalization;
using ExcelBotCs.Attributes;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Controllers;

/// <summary>
///     The admin API behind <c>/lottery/admin</c> (spec §5.9, §8, #114). Every route is <see cref="AdminAuthAttribute" />;
///     expected failures are typed results the page can show verbatim, never raw exceptions. The controller only maps
///     the domain results in <see cref="ILotteryAdminService" /> and <see cref="ILotteryRoundRunner" /> to DTOs and
///     status codes — a disposition keeps its meaning across HTTP so the page never parses prose.
/// </summary>
[ApiController]
[AdminAuth]
[Route("api/lottery/admin")]
public class LotteryAdminController : AuthorizedController
{
	private readonly ILotteryAdminService _admin;
	private readonly ILotteryRoundRunner _runner;
	private readonly ICurrentMemberAccessor _currentMemberAccessor;
	private readonly IOptions<DiscordBotOptions> _discordOptions;
	private readonly IOptions<WebBaseUrlOptions> _webOptions;

	public LotteryAdminController(ILogger<LotteryAdminController> logger, ILotteryAdminService admin,
		ILotteryRoundRunner runner, ICurrentMemberAccessor currentMemberAccessor,
		IOptions<DiscordBotOptions> discordOptions, IOptions<WebBaseUrlOptions> webOptions) : base(logger)
	{
		_admin = admin;
		_runner = runner;
		_currentMemberAccessor = currentMemberAccessor;
		_discordOptions = discordOptions;
		_webOptions = webOptions;
	}

	[HttpGet("status")]
	public async Task<IActionResult> GetStatus()
	{
		var status = await _admin.GetStatusAsync(HttpContext.RequestAborted);
		return Ok(ToStatusDto(status));
	}

	[HttpGet("settings")]
	public async Task<IActionResult> GetSettings()
	{
		var settings = await _admin.GetSettingsAsync(HttpContext.RequestAborted);
		return Ok(ToSettingsDto(settings));
	}

	[HttpPut("settings")]
	public async Task<IActionResult> PutSettings([FromBody] LotterySettingsSaveRequest request)
	{
		// The binder leaves a missing JSON property null on a record; the validator turns that into a per-field
		// message instead of a null reference, so a typo'd body is a typed 400 like any other invalid field.
		var update = new LotterySettingsUpdate(
			request.RunDay ?? string.Empty,
			request.RunTime ?? string.Empty,
			request.RunTimezone ?? string.Empty,
			request.ReminderDay ?? string.Empty,
			request.ReminderTime ?? string.Empty,
			request.ReminderTimezone ?? string.Empty,
			request.RemindersEnabled,
			request.PauseAutomation,
			request.WinTemplate ?? string.Empty,
			request.NoWinTemplate ?? string.Empty);

		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		var result = await _admin.SaveSettingsAsync(update, request.ResetTemplates, actor.Value,
			HttpContext.RequestAborted);

		if (!result.Success)
			return BadRequest(new LotterySettingsSaveResponse(false, false, null,
				result.Errors.Select(error => new LotteryFieldError(error.Field, error.Message)).ToList()));

		return Ok(new LotterySettingsSaveResponse(true, result.Changed, ToSettingsDto(result.Settings!), []));
	}

	[HttpPost("pot")]
	public async Task<IActionResult> AdjustPot([FromBody] PotAdjustmentRequest request)
	{
		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		var result = await _admin.AdjustPotAsync(request.Amount, request.Direction, request.Reason,
			request.IdempotencyKey, actor.Value, HttpContext.RequestAborted);

		if (!result.Success)
			return BadRequest(ToPotDto(result));

		return Ok(ToPotDto(result));
	}

	[HttpGet("ledger")]
	public async Task<IActionResult> GetLedger([FromQuery] string? cursor = null, [FromQuery] int? limit = null)
	{
		var result = await _admin.GetLedgerAsync(cursor, limit, HttpContext.RequestAborted);

		if (result.InvalidCursor)
			return BadRequest(new LotteryHistoryError("invalid_cursor", result.CursorError!));

		return Ok(new LotteryLedgerPage(result.Items.Select(ToLedgerEntryDto).ToList(), result.NextCursor));
	}

	[HttpGet("payouts")]
	public async Task<IActionResult> GetPayouts([FromQuery] string? status = null, [FromQuery] string? cursor = null,
		[FromQuery] int? limit = null)
	{
		var result = await _admin.GetPayoutsAsync(status, cursor, limit, HttpContext.RequestAborted);

		if (result.InvalidStatus)
			return BadRequest(new LotteryHistoryError("invalid_status", result.StatusError!));

		if (result.InvalidCursor)
			return BadRequest(new LotteryHistoryError("invalid_cursor", result.CursorError!));

		return Ok(new LotteryPayoutPage(result.Items.Select(ToPayoutDto).ToList(), result.NextCursor,
			result.PendingCount));
	}

	[HttpPatch("winners/{roundKey}/{winnerId}/payout")]
	public async Task<IActionResult> UpdatePayout(string roundKey, string winnerId,
		[FromBody] PayoutUpdateRequest request)
	{
		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		var result = await _admin.UpdatePayoutAsync(roundKey, winnerId, request.Status, request.Note, actor.Value,
			HttpContext.RequestAborted);

		var body = new PayoutUpdateResponse(result.Success, result.Code, result.Message,
			result.Row is null ? null : ToPayoutDto(result.Row),
			result.ReturnedPrizes.Select(prize => prize.Name).ToList());

		return StatusCode(ToStatusCode(result.Code), body);
	}

	[HttpGet("prizes")]
	public async Task<IActionResult> GetPrizes()
	{
		var pool = await _admin.GetPrizesAsync(HttpContext.RequestAborted);
		return Ok(new LotteryPrizePool(pool.Items.Select(ToPrizeDto).ToList(), pool.InPoolCount, pool.AwardedCount));
	}

	[HttpPost("prizes")]
	public async Task<IActionResult> AddPrize([FromBody] CreatePrizeRequest request)
	{
		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		var result = await _admin.AddPrizeAsync(request.Name, actor.Value, HttpContext.RequestAborted);

		var body = ToPrizeResponse(result);
		if (result.Success) return Created($"/api/lottery/admin/prizes/{result.Prize?.Id}", body);

		return StatusCode(ToStatusCode(result.Code), body);
	}

	[HttpDelete("prizes/{id}")]
	public async Task<IActionResult> RemovePrize(string id)
	{
		var result = await _admin.RemovePrizeAsync(id, HttpContext.RequestAborted);

		if (result.Success) return NoContent();

		return StatusCode(ToStatusCode(result.Code), ToPrizeResponse(result));
	}

	[HttpPost("prizes/{id}/assign")]
	public async Task<IActionResult> AssignPrize(string id, [FromBody] AssignPrizeRequest request)
	{
		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		var result = await _admin.AssignPrizeAsync(id, request.RoundKey, request.WinnerId, actor.Value,
			HttpContext.RequestAborted);

		var body = new PrizeResponse(
			result.Success,
			result.Code,
			result.Message,
			result.Prize is null ? null : ToPrizeDto(result.Prize),
			result.Winner is null ? null : ToPayoutDto(result.Winner),
			result.Errors.Select(error => new LotteryFieldError(error.Field, error.Message)).ToList());

		return StatusCode(ToStatusCode(result.Code), body);
	}

	[HttpGet("optins")]
	public async Task<IActionResult> GetOptIns()
	{
		var optIns = await _admin.GetOptInsAsync(HttpContext.RequestAborted);

		return Ok(new LotteryOptIns(
			optIns.Items.Select(row => new ReminderOptInDto(FormatId(row.DiscordId), row.DisplayName,
				row.IsCurrentMember, row.IsEligible, row.OptedInAtUtc)).ToList(),
			optIns.OptedIn,
			optIns.Eligible));
	}

	[HttpDelete("optins/{discordId}")]
	public async Task<IActionResult> RemoveOptIn(string discordId)
	{
		if (!ulong.TryParse(discordId, out var parsed))
			return BadRequest(new LotteryHistoryError("invalid_id", $"'{discordId}' is not a Discord id."));

		var result = await _admin.RemoveOptInAsync(parsed, HttpContext.RequestAborted);

		return result.Success
			? NoContent()
			: NotFound(new LotteryHistoryError("not-found", result.Message));
	}

	[HttpPost("reminders/test")]
	public async Task<IActionResult> SendTestReminder()
	{
		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		var outcome = await _admin.SendTestReminderAsync(actor.Value, HttpContext.RequestAborted);

		return Ok(new ReminderTestResponse(outcome.Posted, outcome.Disposition, outcome.Message, outcome.RoundKey,
			outcome.Recipients));
	}

	[HttpGet("preview")]
	public async Task<IActionResult> GetPreview([FromQuery] string? result = null)
	{
		var variant = string.IsNullOrWhiteSpace(result) ? LotteryAnnouncementVariant.NoWin : result.Trim();
		if (!LotteryAnnouncementVariant.IsKnown(variant))
			return BadRequest(new LotteryHistoryError("invalid_variant",
				$"'{result}' is not an announcement result. Use '{LotteryAnnouncementVariant.Win}' or " +
				$"'{LotteryAnnouncementVariant.NoWin}'."));

		var preview = await _admin.RenderPreviewAsync(variant, HttpContext.RequestAborted);
		return Ok(new LotteryAnnouncementPreviewDto(preview.Variant, preview.Text, preview.VisibleLength,
			preview.RawLength));
	}

	[HttpPost("run")]
	public async Task<IActionResult> Run()
	{
		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		return ToActionResult(await _runner.RunNowAsync(actor.Value, HttpContext.RequestAborted));
	}

	[HttpPost("skip")]
	public async Task<IActionResult> Skip([FromBody] ManualActionRequest? request)
	{
		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		return ToActionResult(await _runner.SkipAsync(actor.Value, request?.Reason,
			HttpContext.RequestAborted));
	}

	[HttpPost("retry")]
	public async Task<IActionResult> Retry()
	{
		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		return ToActionResult(await _runner.RetryAsync(actor.Value, HttpContext.RequestAborted));
	}

	[HttpPost("dry-run")]
	public async Task<IActionResult> DryRun()
	{
		return ToActionResult(await _runner.PreviewAsync(HttpContext.RequestAborted));
	}

	[HttpPost("void")]
	public async Task<IActionResult> Void([FromBody] ManualActionRequest? request)
	{
		var reason = request?.Reason?.Trim();
		if (string.IsNullOrEmpty(reason))
			// A missing reason is a malformed request, not a state conflict: the shared mapping answers it with a 400.
			return ToActionResult(new LotteryRunResult(LotteryRunDisposition.InvalidRequest,
				"A reason is required to void a draw: it goes on the round and in the audit trail."));

		var roundKey = request?.RoundKey?.Trim();
		if (string.IsNullOrEmpty(roundKey))
		{
			// "Void last draw": the newest round is the one the page means when no key is sent.
			var snapshot = await _runner.GetStatusAsync(HttpContext.RequestAborted);
			roundKey = snapshot.LatestRound?.RoundKey;
		}

		if (string.IsNullOrEmpty(roundKey))
			return ToActionResult(new LotteryRunResult(LotteryRunDisposition.NotFound,
				"There is no draw to void."));

		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		return ToActionResult(await _runner.VoidAsync(roundKey, actor.Value, reason, HttpContext.RequestAborted));
	}

	/// <summary>
	///     Moves one scheduled round to a new instant (spec §5.1 "Reschedule"). The round key follows the new local date
	///     and the guesses stay; the pot is untouched until the run. A target date that already has a round is a typed
	///     conflict, not a silent merge.
	/// </summary>
	[HttpPost("reschedule")]
	public async Task<IActionResult> Reschedule([FromBody] RescheduleRequest request)
	{
		var actor = await _currentMemberAccessor.TryGetActorIdAsync();
		if (actor is null) return this.MissingActor();

		if (request.ScheduledAtUtc is not { } scheduledAt)
			return ToActionResult(new LotteryRunResult(LotteryRunDisposition.InvalidRequest,
				"A new scheduled instant is required to move a round."));

		return ToActionResult(await _runner.RescheduleAsync(scheduledAt.UtcDateTime, actor.Value,
			request.RoundKey, HttpContext.RequestAborted));
	}

	/// <summary>
	///     The one status mapping for the typed prize and payout codes, so the five hand-written switches this replaces
	///     cannot drift apart: an unknown target is a 404, an invalid request a 400, a state conflict a 409.
	/// </summary>
	private static int ToStatusCode(string code)
	{
		return code switch
		{
			LotteryPrizeCode.NotFound or LotteryPayoutUpdateCode.NotFound => StatusCodes.Status404NotFound,
			LotteryPrizeCode.Invalid or LotteryPayoutUpdateCode.InvalidStatus => StatusCodes.Status400BadRequest,
			LotteryPrizeCode.Duplicate or LotteryPrizeCode.Awarded or LotteryPrizeCode.InvalidTransition
				or LotteryPayoutUpdateCode.InvalidTransition => StatusCodes.Status409Conflict,
			_ => StatusCodes.Status200OK
		};
	}

	/// <summary>
	///     Maps a typed disposition to HTTP without changing its meaning: a state conflict is a 409, a missing target a
	///     404, a hard failure a 500, everything else a 200. The body is always the same shape, so the page reads
	///     <c>Disposition</c> and never parses prose.
	/// </summary>
	private IActionResult ToActionResult(LotteryRunResult result)
	{
		var body = Action(result.Disposition, result.Success, result.Message, result.RoundKey, result.Pot);

		return result.Disposition switch
		{
			LotteryRunDisposition.NotFound => NotFound(body),
			LotteryRunDisposition.InvalidRequest => BadRequest(body),
			LotteryRunDisposition.Failed => StatusCode(StatusCodes.Status500InternalServerError, body),
			LotteryRunDisposition.Blocked or LotteryRunDisposition.AlreadyRunning or
				LotteryRunDisposition.AlreadyRun or LotteryRunDisposition.NotAllowed or
				LotteryRunDisposition.RescheduleConflict => Conflict(body),
			_ => Ok(body)
		};
	}

	private static ManualActionResponse Action(string disposition, bool success, string message,
		string? roundKey = null, long? pot = null)
	{
		return new ManualActionResponse(disposition, success, message, roundKey, pot);
	}

	#region Mapping

	private LotteryAdminStatus ToStatusDto(LotteryAdminStatusResult status)
	{
		var discord = _discordOptions.Value;

		return new LotteryAdminStatus(
			status.Automation.Kind,
			status.Automation.Label,
			status.Automation.SchedulerEnabled,
			status.Settings.AutomationEnabled,
			status.Settings.DryRun,
			status.Settings.PauseAutomation,
			status.Snapshot.IsDegraded,
			status.Snapshot.PendingRound is not null,
			status.Snapshot.Pot,
			status.PendingPayouts,
			status.PoolPrizes,
			status.OptedIn,
			status.EligibleReminders,
			status.Settings.RemindersEnabled,
			ToRoundDto(status.Snapshot.LatestRound),
			ToRoundDto(status.Snapshot.PendingRound),
			status.Snapshot.NextRunAtUtc,
			status.Snapshot.NextRoundKey,
			status.Snapshot.LastError,
			new LotteryAdminDeployment(
				FormatId(discord.LotteryChannel),
				FormatId(discord.LotteryMemberRoleId),
				FormatId(discord.LotteryCustomEmojiId),
				_webOptions.Value.BaseUrl),
			ToSettingsChangeDto(status.LastSettingsChange),
			status.Settings.InvalidFields);
	}

	private static PotAdjustmentResponse ToPotDto(LotteryPotAdjustmentResult result)
	{
		return new PotAdjustmentResponse(
			result.Success,
			result.Replayed,
			result.Balance,
			result.RequestedAmount,
			result.AppliedAmount,
			result.Entry is null ? null : ToLedgerEntryDto(result.Entry),
			result.Errors.Select(error => new LotteryFieldError(error.Field, error.Message)).ToList());
	}

	private static LotteryLedgerEntryDto ToLedgerEntryDto(LotteryPotLedgerEntry entry)
	{
		return new LotteryLedgerEntryDto(
			entry.Id,
			entry.RoundKey,
			entry.Type,
			entry.Amount,
			entry.Reason,
			FormatId(entry.ActorDiscordId),
			entry.BalanceAfter,
			entry.IdempotencyKey,
			entry.DateCreated);
	}

	private static PrizeResponse ToPrizeResponse(LotteryPrizeResult result)
	{
		return new PrizeResponse(
			result.Success,
			result.Code,
			result.Message,
			result.Prize is null ? null : ToPrizeDto(result.Prize),
			null,
			result.Errors.Select(error => new LotteryFieldError(error.Field, error.Message)).ToList());
	}

	private static PrizePoolItem ToPrizeDto(LotteryPrize prize)
	{
		return new PrizePoolItem(
			prize.Id,
			prize.Name,
			prize.Status,
			FormatId(prize.AwardedToDiscordId),
			prize.AwardedToName,
			prize.AwardedRoundKey,
			FormatId(prize.AddedByDiscordId),
			prize.AddedAt);
	}

	private static LotteryPayoutDto ToPayoutDto(LotteryPayoutRow row)
	{
		return new LotteryPayoutDto(
			row.Id,
			row.RoundKey,
			FormatId(row.DiscordId),
			row.DisplayName,
			row.Guess,
			row.GilPrize,
			row.Items,
			row.PayoutStatus,
			FormatId(row.PayoutActorDiscordId),
			row.PayoutAt,
			row.PayoutNote);
	}

	private static LotterySettingsDto ToSettingsDto(LotteryEffectiveSettings settings)
	{
		return new LotterySettingsDto(
			settings.RunDay,
			settings.RunTime,
			settings.RunTimezone,
			settings.ReminderDay,
			settings.ReminderTime,
			settings.ReminderTimezone,
			settings.RemindersEnabled,
			settings.PauseAutomation,
			settings.WinTemplate,
			settings.NoWinTemplate,
			settings.Version,
			FormatId(settings.UpdatedByDiscordId),
			settings.AutomationEnabled,
			settings.DryRun,
			settings.Status.Kind,
			settings.Status.Label,
			settings.BasePot,
			settings.WeeklyIncrement,
			settings.InvalidFields,
			new LotterySettingsDefaultsDto(
				LotterySettingsDefaults.RunDay,
				LotterySettingsDefaults.RunTime,
				LotterySettingsDefaults.RunTimezone,
				LotterySettingsDefaults.ReminderDay,
				LotterySettingsDefaults.ReminderTime,
				LotterySettingsDefaults.ReminderTimezone,
				LotterySettingsDefaults.WinTemplate,
				LotterySettingsDefaults.NoWinTemplate));
	}

	private static LotteryAdminRound? ToRoundDto(LotteryRound? round)
	{
		if (round is null) return null;

		return new LotteryAdminRound(
			round.RoundKey,
			round.Status,
			round.ScheduledAt,
			round.RunAt,
			round.WinningNumber,
			round.PotBefore,
			round.PotAfter,
			round.FailedStep,
			round.Attempts,
			round.NextRetryAt,
			round.LastError,
			round.AnnouncementMessageId);
	}

	private static LotteryAdminSettingsChange? ToSettingsChangeDto(LotterySettingsChange? change)
	{
		return change is null
			? null
			: new LotteryAdminSettingsChange(change.Version, change.ChangedAtUtc, FormatId(change.ActorDiscordId),
				change.Fields);
	}

	/// <summary>A Discord id as a string: a snowflake loses precision if a browser parses it as a number.</summary>
	private static string FormatId(ulong? id)
	{
		return (id ?? 0).ToString(CultureInfo.InvariantCulture);
	}

	#endregion
}
