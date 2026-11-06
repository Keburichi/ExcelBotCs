using System.Globalization;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The result of validating one schedule field: the normalized value to store, or the reason it was rejected.
/// </summary>
public sealed record LotteryFieldValidation(string Normalized, string? Error)
{
	public bool IsValid => Error is null;

	public static LotteryFieldValidation Valid(string normalized)
	{
		return new LotteryFieldValidation(normalized, null);
	}

	public static LotteryFieldValidation Invalid(string error)
	{
		return new LotteryFieldValidation(string.Empty, error);
	}
}

/// <summary>
///     Schedule validation for <c>lottery_settings</c> (spec §7): a run/reminder day is an English day name, a time
///     is <c>HH:mm</c> on a 24-hour clock, and a timezone is an id the runtime knows. The same rules run on save and
///     when a stored document is read, so a hand-edited or corrupted document is repaired instead of scheduled.
///     Result-template rules are #111's; here templates are only defaulted when blank.
/// </summary>
public static class LotterySettingsValidator
{
	private static readonly string[] DayNames =
	[
		"Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
	];

	private const string TimeFormat = "HH:mm";

	public static LotteryFieldValidation ValidateDay(string? value)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrEmpty(trimmed))
			return LotteryFieldValidation.Invalid("must be an English day name.");

		var match = DayNames.FirstOrDefault(day => string.Equals(day, trimmed, StringComparison.OrdinalIgnoreCase));
		return match is null
			? LotteryFieldValidation.Invalid($"'{trimmed}' is not an English day name.")
			: LotteryFieldValidation.Valid(match);
	}

	public static LotteryFieldValidation ValidateTime(string? value)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrEmpty(trimmed))
			return LotteryFieldValidation.Invalid("must be a time in HH:mm format.");

		// DateTime.TryParse would accept "8pm", "9:05" and other shapes the admin form never produces. The stored
		// format is pinned so the scheduler and the announcement can parse it without guessing.
		return DateTime.TryParseExact(trimmed, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
			out _)
			? LotteryFieldValidation.Valid(trimmed)
			: LotteryFieldValidation.Invalid($"'{trimmed}' is not a time in HH:mm format.");
	}

	public static LotteryFieldValidation ValidateTimezone(string? value)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrEmpty(trimmed))
			return LotteryFieldValidation.Invalid("must be a timezone id such as Europe/Berlin.");

		try
		{
			TimeZoneInfo.FindSystemTimeZoneById(trimmed);
			return LotteryFieldValidation.Valid(trimmed);
		}
		catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
		{
			return LotteryFieldValidation.Invalid($"'{trimmed}' is not a known timezone id.");
		}
	}

	/// <summary>Every invalid field of a save request, in a stable order; empty means the request may be stored.</summary>
	public static IReadOnlyList<LotterySettingsValidationError> Validate(LotterySettingsUpdate update)
	{
		var errors = new List<LotterySettingsValidationError>();

		AddIfInvalid(errors, "RunDay", ValidateDay(update.RunDay));
		AddIfInvalid(errors, "RunTime", ValidateTime(update.RunTime));
		AddIfInvalid(errors, "RunTimezone", ValidateTimezone(update.RunTimezone));
		AddIfInvalid(errors, "ReminderDay", ValidateDay(update.ReminderDay));
		AddIfInvalid(errors, "ReminderTime", ValidateTime(update.ReminderTime));
		AddIfInvalid(errors, "ReminderTimezone", ValidateTimezone(update.ReminderTimezone));

		return errors;
	}

	/// <summary>
	///     The normalized form of an already-valid update. Storing normalized values keeps "tuesday" and "Tuesday" a
	///     single value, so a change audit only fires when the meaning changed.
	/// </summary>
	public static LotterySettingsUpdate Normalize(LotterySettingsUpdate update)
	{
		return update with
		{
			RunDay = ValidateDay(update.RunDay).Normalized,
			RunTime = ValidateTime(update.RunTime).Normalized,
			RunTimezone = ValidateTimezone(update.RunTimezone).Normalized,
			ReminderDay = ValidateDay(update.ReminderDay).Normalized,
			ReminderTime = ValidateTime(update.ReminderTime).Normalized,
			ReminderTimezone = ValidateTimezone(update.ReminderTimezone).Normalized
		};
	}

	private static void AddIfInvalid(List<LotterySettingsValidationError> errors, string field,
		LotteryFieldValidation validation)
	{
		if (!validation.IsValid)
			errors.Add(new LotterySettingsValidationError(field, validation.Error!));
	}
}
