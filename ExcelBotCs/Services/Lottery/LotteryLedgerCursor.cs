using System.Globalization;
using System.Text;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The opaque keyset cursor of the admin ledger table (spec §5.4, #114). The ledger is append-only and read newest
///     first, so the cursor carries the previous page's last entry — creation instant and id — and the next page resumes
///     strictly after it. It is base64url so the page treats it as opaque and cannot construct a position the ordering
///     cannot serve. The id is the tie-breaker, so two entries written in the same tick are neither skipped nor repeated.
/// </summary>
public static class LotteryLedgerCursor
{
	private const char Separator = '|';

	public static string Encode(LotteryLedgerPageAnchor anchor)
	{
		var payload = string.Join(Separator,
			anchor.DateCreatedUtc.Ticks.ToString(CultureInfo.InvariantCulture),
			anchor.Id);

		return Base64Url.Encode(Encoding.UTF8.GetBytes(payload));
	}

	/// <summary>
	///     Decodes a cursor produced by <see cref="Encode" />. <c>false</c> means the value is not a cursor this endpoint
	///     issued, which the controller answers with a typed 400 instead of silently restarting at the newest entry.
	/// </summary>
	public static bool TryDecode(string? cursor, out LotteryLedgerPageAnchor anchor)
	{
		anchor = null!;
		if (string.IsNullOrWhiteSpace(cursor)) return false;
		if (!Base64Url.TryDecode(cursor, out var bytes)) return false;

		var payload = Encoding.UTF8.GetString(bytes);
		var parts = payload.Split(Separator);
		if (parts.Length != 2) return false;

		if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)) return false;
		if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) return false;
		if (string.IsNullOrWhiteSpace(parts[1])) return false;

		anchor = new LotteryLedgerPageAnchor(new DateTime(ticks, DateTimeKind.Utc), parts[1]);
		return true;
	}
}
