using System.Globalization;
using System.Text.RegularExpressions;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The result-template contract (spec §5.6, criterion 10). The admin edits only the result block; this class owns
///     what a template may contain:
///     <list type="bullet">
///         <item>exactly seven placeholders — <c>{number}</c>, <c>{winners}</c>, <c>{prize}</c>, <c>{bonusItems}</c>,
///         <c>{pot}</c>, <c>{nextPot}</c>, <c>{emoji}</c> — anything else is rejected;</item>
///         <item>the placeholders the message cannot work without: <c>{number}</c> in both variants, and
///         <c>{winners}</c>/<c>{prize}</c> in the winner variant, <c>{pot}</c> in the no-winner variant;</item>
///         <item>an assembled message under Discord's 2,000-character limit.</item>
///     </list>
///     The rules run when the admin saves (the save is rejected and nothing is written) and again when a run renders
///     the message (the publisher falls back to the default and alerts the admins).
/// </summary>
public static class LotteryTemplateValidator
{
	public const string Number = "{number}";
	public const string Winners = "{winners}";
	public const string Prize = "{prize}";
	public const string BonusItems = "{bonusItems}";
	public const string Pot = "{pot}";
	public const string NextPot = "{nextPot}";
	public const string Emoji = "{emoji}";

	/// <summary>Every placeholder a template may use, in the order the admin page lists them.</summary>
	public static readonly IReadOnlyList<string> AllPlaceholders =
		[Number, Winners, Prize, BonusItems, Pot, NextPot, Emoji];

	/// <summary>Without these the winner message would not name the result or the prize.</summary>
	public static readonly IReadOnlyList<string> WinRequiredPlaceholders = [Number, Winners, Prize];

	/// <summary>Without these the no-winner message would not name the result or the new pot.</summary>
	public static readonly IReadOnlyList<string> NoWinRequiredPlaceholders = [Number, Pot];

	/// <summary>Discord's per-message limit; the assembled post must stay strictly below it.</summary>
	public const int MaxMessageLength = LotteryAnnouncementComposer.MaxMessageLength;

	/// <summary>A placeholder is anything between braces; that also catches <c>{}</c> and <c>{pot }</c> typos.</summary>
	private static readonly Regex PlaceholderPattern = new(@"\{[^{}]*\}", RegexOptions.Compiled);

	public static LotteryFieldValidation ValidateWinTemplate(string? template)
	{
		return Validate(template, "The winner template", WinRequiredPlaceholders, true);
	}

	public static LotteryFieldValidation ValidateNoWinTemplate(string? template)
	{
		return Validate(template, "The no-winner template", NoWinRequiredPlaceholders, false);
	}

	private static LotteryFieldValidation Validate(string? template, string label,
		IReadOnlyList<string> requiredPlaceholders, bool isWin)
	{
		var trimmed = template?.Trim();
		if (string.IsNullOrEmpty(trimmed))
			return LotteryFieldValidation.Invalid(
				$"{label} must not be empty. Save the default template instead of a blank one.");

		var unknown = PlaceholderPattern.Matches(trimmed)
			.Select(match => match.Value)
			.Where(placeholder => !AllPlaceholders.Contains(placeholder))
			.Distinct(StringComparer.Ordinal)
			.ToList();

		if (unknown.Count > 0)
			return LotteryFieldValidation.Invalid(
				$"{label} uses the unknown placeholder {string.Join(", ", unknown)}. Allowed placeholders are " +
				$"{string.Join(", ", AllPlaceholders)}.");

		var missing = requiredPlaceholders
			.Where(placeholder => !trimmed.Contains(placeholder, StringComparison.Ordinal))
			.ToList();

		if (missing.Count > 0)
			return LotteryFieldValidation.Invalid(
				$"{label} is missing the required placeholder {string.Join(", ", missing)}.");

		// The length check renders the real post: the editable block plus every generated section. A template that
		// only looks short can still push the assembled message over the limit.
		var message = LotteryAnnouncementSamples.Compose(trimmed, isWin);
		if (message.RawLength >= MaxMessageLength)
			return LotteryFieldValidation.Invalid(
				$"{label} produces a {message.RawLength.ToString("N0", CultureInfo.InvariantCulture)}-character " +
				$"message; the assembled post must stay under {MaxMessageLength.ToString("N0", CultureInfo.InvariantCulture)} characters.");

		return LotteryFieldValidation.Valid(trimmed);
	}
}
