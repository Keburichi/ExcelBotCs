using System.Globalization;
using System.Text;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The opaque keyset cursor of the member history timeline (spec §5.8, #113). It carries the last row of the
///     previous page — round key, creation instant and id — so the next page can resume strictly after it. The cursor
///     is base64url rather than the raw position so the client treats it as opaque and cannot construct a page by hand
///     that the ordering cannot serve. It also survives the row being deleted: the keyset still anchors the page.
/// </summary>
public static class LotteryHistoryCursor
{
	private const char Separator = '|';
	private const string RoundKeyFormat = "yyyy-MM-dd";

	public static string Encode(LotteryWinnerPageAnchor anchor)
	{
		var payload = string.Join(Separator,
			anchor.RoundKey,
			anchor.DateCreatedUtc.Ticks.ToString(CultureInfo.InvariantCulture),
			anchor.Id);

		return Base64Url.Encode(Encoding.UTF8.GetBytes(payload));
	}

	/// <summary>
	///     Decodes a cursor produced by <see cref="Encode" />. <c>false</c> means the value is not a cursor this
	///     endpoint issued, which the controller answers with a typed 400 instead of silently starting over.
	/// </summary>
	public static bool TryDecode(string? cursor, out LotteryWinnerPageAnchor anchor)
	{
		anchor = null!;
		if (string.IsNullOrWhiteSpace(cursor)) return false;
		if (!Base64Url.TryDecode(cursor, out var bytes)) return false;

		var payload = Encoding.UTF8.GetString(bytes);
		var parts = payload.Split(Separator);
		if (parts.Length != 3) return false;

		// A round key is a local date (YYYY-MM-DD); anything else is not a position this endpoint produced.
		if (!DateTime.TryParseExact(parts[0], RoundKeyFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
			    out _))
			return false;

		if (!long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)) return false;
		if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) return false;
		if (string.IsNullOrWhiteSpace(parts[2])) return false;

		anchor = new LotteryWinnerPageAnchor(parts[0], new DateTime(ticks, DateTimeKind.Utc), parts[2]);
		return true;
	}
}
