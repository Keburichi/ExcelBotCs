using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The single place that knows the lottery settings precedence (spec §7 "Precedence and validation"):
///     deployment config &gt; stored <c>lottery_settings</c> &gt; the defaults in
///     <see cref="LotterySettingsDefaults" />. Every later ticket reads settings through
///     <see cref="GetEffectiveAsync" /> instead of touching the repository, so a corrupted document can never reach
///     the scheduler.
/// </summary>
public class LotterySettingsService : ILotterySettingsService
{
	private readonly ILotterySettingsHistoryRepository _historyRepository;
	private readonly ILogger<LotterySettingsService> _logger;
	private readonly IDiscordMessageService _messageService;
	private readonly IOptions<LotteryOptions> _options;
	private readonly ILotterySettingsRepository _settingsRepository;

	public LotterySettingsService(
		ILotterySettingsRepository settingsRepository,
		ILotterySettingsHistoryRepository historyRepository,
		IOptions<LotteryOptions> options,
		IDiscordMessageService messageService,
		ILogger<LotterySettingsService> logger)
	{
		_settingsRepository = settingsRepository;
		_historyRepository = historyRepository;
		_options = options;
		_messageService = messageService;
		_logger = logger;
	}

	public async Task<LotteryEffectiveSettings> GetEffectiveAsync(CancellationToken cancellationToken = default)
	{
		var stored = await _settingsRepository.GetSingletonAsync();
		return BuildEffective(stored);
	}

	public async Task<string?> GetLastDryRunRoundKeyAsync(CancellationToken cancellationToken = default)
	{
		var stored = await _settingsRepository.GetSingletonAsync();
		return stored?.LastDryRunRoundKey;
	}

	public async Task MarkDryRunRoundAsync(string roundKey, CancellationToken cancellationToken = default)
	{
		// A dry-run marker is scheduler bookkeeping, not an operational setting: it writes no history row and no audit
		// notice, and it never changes the effective settings.
		await _settingsRepository.SetLastDryRunRoundKeyAsync(roundKey);
	}

	public async Task<LotterySettingsSaveResult> SaveAsync(LotterySettingsUpdate update, ulong? actorDiscordId,
		CancellationToken cancellationToken = default)
	{
		// A blank template means "use the default" (the admin form sends an empty field after a reset), so the
		// defaults are applied before validation: an empty field is never an invalid template.
		var request = WithTemplateDefaults(update);
		var errors = LotterySettingsValidator.Validate(request);
		if (errors.Count > 0)
			return LotterySettingsSaveResult.Rejected(errors);

		var stored = await _settingsRepository.GetSingletonAsync();
		var storedValues = LotterySettingsValidator.Normalize(request);
		var before = BuildEffective(stored);

		// A save is a no-op when it would not change an effective value (so rewriting "tuesday" as "Tuesday" is not
		// an audited change) and would not repair anything in the document. A stored document that needed repair is
		// always rewritten: the admin form is prefilled from the repaired values, so saving it unchanged must clear
		// the degraded state instead of leaving the bad values in place forever.
		if (before.ToUpdate() == storedValues && before.InvalidFields.Count == 0)
			return LotterySettingsSaveResult.Unchanged(before);

		var version = (stored?.Version ?? 0) + 1;
		var changedAt = DateTime.UtcNow;

		var settings = ToDocument(storedValues);
		settings.Version = version;
		settings.UpdatedByDiscordId = actorDiscordId;

		// The dry-run marker is scheduler bookkeeping that lives in the same singleton; a settings save must not clear it.
		settings.LastDryRunRoundKey = stored?.LastDryRunRoundKey;
		await _settingsRepository.SaveAsync(settings);

		// The audit "old" side is what was really stored, so a repair is traceable ("Funday" -> "Tuesday").
		var oldValues = stored is null ? before.ToUpdate() : ToUpdateFromDocument(stored);
		var history = BuildHistory(storedValues, oldValues, version, actorDiscordId, changedAt);
		await _historyRepository.CreateAsync(history);

		await PostAuditNoticeAsync(history);

		// The effective values come from the document that was just stored, so the caller sees the new version.
		return LotterySettingsSaveResult.Saved(BuildEffective(settings));
	}

	private LotteryEffectiveSettings BuildEffective(LotterySettings? stored)
	{
		if (stored is null)
			return BuildEffective(LotterySettingsUpdate.FromDefaults(), 0, null, []);

		var invalidFields = new List<string>();

		string Schedule(LotteryFieldValidation validation, string field, string fallback)
		{
			if (validation.IsValid) return validation.Normalized;
			invalidFields.Add(field);
			return fallback;
		}

		var values = new LotterySettingsUpdate(
			Schedule(LotterySettingsValidator.ValidateDay(stored.RunDay), "RunDay", LotterySettingsDefaults.RunDay),
			Schedule(LotterySettingsValidator.ValidateTime(stored.RunTime), "RunTime",
				LotterySettingsDefaults.RunTime),
			Schedule(LotterySettingsValidator.ValidateTimezone(stored.RunTimezone), "RunTimezone",
				LotterySettingsDefaults.RunTimezone),
			Schedule(LotterySettingsValidator.ValidateDay(stored.ReminderDay), "ReminderDay",
				LotterySettingsDefaults.ReminderDay),
			Schedule(LotterySettingsValidator.ValidateTime(stored.ReminderTime), "ReminderTime",
				LotterySettingsDefaults.ReminderTime),
			Schedule(LotterySettingsValidator.ValidateTimezone(stored.ReminderTimezone), "ReminderTimezone",
				LotterySettingsDefaults.ReminderTimezone),
			stored.RemindersEnabled,
			stored.PauseAutomation,
			// Result-template rules belong to #111; a stored template that is present is returned as stored.
			string.IsNullOrWhiteSpace(stored.WinTemplate) ? LotterySettingsDefaults.WinTemplate : stored.WinTemplate,
			string.IsNullOrWhiteSpace(stored.NoWinTemplate)
				? LotterySettingsDefaults.NoWinTemplate
				: stored.NoWinTemplate);

		return BuildEffective(values, stored.Version, stored.UpdatedByDiscordId, invalidFields);
	}

	private LotteryEffectiveSettings BuildEffective(LotterySettingsUpdate values, int version, ulong? updatedBy,
		IReadOnlyList<string> invalidFields)
	{
		var automation = _options.Value.Automation;

		return new LotteryEffectiveSettings(
			values.RunDay,
			values.RunTime,
			values.RunTimezone,
			values.ReminderDay,
			values.ReminderTime,
			values.ReminderTimezone,
			values.RemindersEnabled,
			values.PauseAutomation,
			values.WinTemplate,
			values.NoWinTemplate,
			version,
			updatedBy,
			automation.Enabled,
			automation.DryRun,
			automation.BasePot,
			automation.WeeklyIncrement,
			invalidFields);
	}

	private static LotterySettingsUpdate WithTemplateDefaults(LotterySettingsUpdate update)
	{
		return update with
		{
			WinTemplate = string.IsNullOrWhiteSpace(update.WinTemplate)
				? LotterySettingsDefaults.WinTemplate
				: update.WinTemplate,
			NoWinTemplate = string.IsNullOrWhiteSpace(update.NoWinTemplate)
				? LotterySettingsDefaults.NoWinTemplate
				: update.NoWinTemplate
		};
	}

	private static LotterySettings ToDocument(LotterySettingsUpdate values)
	{
		return new LotterySettings
		{
			RunDay = values.RunDay,
			RunTime = values.RunTime,
			RunTimezone = values.RunTimezone,
			ReminderDay = values.ReminderDay,
			ReminderTime = values.ReminderTime,
			ReminderTimezone = values.ReminderTimezone,
			RemindersEnabled = values.RemindersEnabled,
			PauseAutomation = values.PauseAutomation,
			WinTemplate = values.WinTemplate,
			NoWinTemplate = values.NoWinTemplate
		};
	}

	/// <summary>The values as stored, without validation or normalization, for the audit "old" side.</summary>
	private static LotterySettingsUpdate ToUpdateFromDocument(LotterySettings stored)
	{
		return new LotterySettingsUpdate(
			stored.RunDay,
			stored.RunTime,
			stored.RunTimezone,
			stored.ReminderDay,
			stored.ReminderTime,
			stored.ReminderTimezone,
			stored.RemindersEnabled,
			stored.PauseAutomation,
			stored.WinTemplate,
			stored.NoWinTemplate);
	}

	private static LotterySettingsHistory BuildHistory(LotterySettingsUpdate after, LotterySettingsUpdate before,
		int version, ulong? actorDiscordId, DateTime changedAt)
	{
		var oldValues = new Dictionary<string, object?>();
		var newValues = new Dictionary<string, object?>();

		void Record(string field, object? oldValue, object? newValue)
		{
			if (Equals(oldValue, newValue)) return;
			oldValues[field] = oldValue;
			newValues[field] = newValue;
		}

		Record(nameof(before.RunDay), before.RunDay, after.RunDay);
		Record(nameof(before.RunTime), before.RunTime, after.RunTime);
		Record(nameof(before.RunTimezone), before.RunTimezone, after.RunTimezone);
		Record(nameof(before.ReminderDay), before.ReminderDay, after.ReminderDay);
		Record(nameof(before.ReminderTime), before.ReminderTime, after.ReminderTime);
		Record(nameof(before.ReminderTimezone), before.ReminderTimezone, after.ReminderTimezone);
		Record(nameof(before.RemindersEnabled), before.RemindersEnabled, after.RemindersEnabled);
		Record(nameof(before.PauseAutomation), before.PauseAutomation, after.PauseAutomation);
		Record(nameof(before.WinTemplate), before.WinTemplate, after.WinTemplate);
		Record(nameof(before.NoWinTemplate), before.NoWinTemplate, after.NoWinTemplate);

		return new LotterySettingsHistory
		{
			SettingsKey = LotterySettings.SingletonKey,
			Version = version,
			ActorDiscordId = actorDiscordId,
			ChangedAt = changedAt,
			OldValues = oldValues,
			NewValues = newValues
		};
	}

	private async Task PostAuditNoticeAsync(LotterySettingsHistory history)
	{
		try
		{
			var message = FormatAuditNotice(history);
			await _messageService.PostInLogChannelAsync(message);
		}
		catch (Exception exception)
		{
			// The audit row is the record of truth; an unreachable Discord channel must not undo a saved change.
			_logger.LogWarning(exception,
				"Lottery settings v{Version} were saved but the audit notice could not be posted", history.Version);
		}
	}

	/// <summary>Concise and secret-free: which fields changed, by whom, at which version.</summary>
	private static string FormatAuditNotice(LotterySettingsHistory history)
	{
		var actor = history.ActorDiscordId is { } discordId ? $"<@{discordId}>" : "system";
		var fields = history.NewValues.Keys.Count == 0 ? "none" : string.Join(", ", history.NewValues.Keys);

		return $"Lottery settings v{history.Version} updated by {actor}: {fields}";
	}
}
