using System.Globalization;

namespace ExcelBotCs.Utilities;

/// <summary>
///     A round is identified by its scheduled run date in Europe/Berlin, formatted <c>YYYY-MM-DD</c>. Every lottery
///     collection uses that string as the foreign key.
/// </summary>
public static class LotteryRoundKey
{
	public const string TimezoneId = "Europe/Berlin";
	public const string Format = "yyyy-MM-dd";

	private static readonly TimeZoneInfo BerlinTimezone = TimeZoneInfo.FindSystemTimeZoneById(TimezoneId);

	/// <summary>
	///     The round key for an instant: the instant's local date in Europe/Berlin. 23:30 UTC on 21 July is
	///     01:30 on 22 July local, so it belongs to the next round.
	/// </summary>
	public static string FromUtc(DateTime instantUtc)
	{
		var utc = instantUtc.Kind == DateTimeKind.Utc
			? instantUtc
			: DateTime.SpecifyKind(instantUtc, DateTimeKind.Utc);

		var local = TimeZoneInfo.ConvertTimeFromUtc(utc, BerlinTimezone);
		return local.ToString(Format, CultureInfo.InvariantCulture);
	}
}
