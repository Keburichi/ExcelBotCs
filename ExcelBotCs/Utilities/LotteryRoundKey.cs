using System.Globalization;

namespace ExcelBotCs.Utilities;

/// <summary>
///     A round is identified by its scheduled run date in the run timezone (Europe/Berlin by default), formatted
///     <c>YYYY-MM-DD</c>. Every lottery collection uses that string as the foreign key, so this class is the single
///     source of the key format.
/// </summary>
public static class LotteryRoundKey
{
	public const string TimezoneId = "Europe/Berlin";
	public const string Format = "yyyy-MM-dd";

	private static readonly TimeZoneInfo BerlinTimezone = TimeZoneInfo.FindSystemTimeZoneById(TimezoneId);

	/// <summary>
	///     The round key for an instant: the instant's local date in Europe/Berlin. 23:30 UTC on 21 July is
	///     01:30 on 22 July local, so it belongs to the next round. This is the archive rule; the scheduler uses
	///     <see cref="FromLocalDate" /> with the configured run timezone's local date.
	/// </summary>
	public static string FromUtc(DateTime instantUtc)
	{
		var utc = instantUtc.Kind == DateTimeKind.Utc
			? instantUtc
			: DateTime.SpecifyKind(instantUtc, DateTimeKind.Utc);

		var local = TimeZoneInfo.ConvertTimeFromUtc(utc, BerlinTimezone);
		return FromLocalDate(DateOnly.FromDateTime(local));
	}

	/// <summary>Formats the local scheduled date; this pins the string shape every collection stores.</summary>
	public static string FromLocalDate(DateOnly localDate)
	{
		return localDate.ToString(Format, CultureInfo.InvariantCulture);
	}

	/// <summary>Parses a round key back into its local date; <c>false</c> for anything not in the pinned format.</summary>
	public static bool TryParse(string? roundKey, out DateOnly localDate)
	{
		return DateOnly.TryParseExact(roundKey, Format, CultureInfo.InvariantCulture, DateTimeStyles.None,
			out localDate);
	}

	/// <summary>
	///     Orders two round keys chronologically. Fixed-width <c>YYYY-MM-DD</c> keys sort lexicographically, which lets
	///     the catch-up selection compare keys without parsing them.
	/// </summary>
	public static int Compare(string? left, string? right)
	{
		return string.CompareOrdinal(left, right);
	}
}
