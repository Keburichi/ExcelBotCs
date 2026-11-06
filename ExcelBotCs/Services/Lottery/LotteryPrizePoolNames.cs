using ExcelBotCs.Database.Interfaces;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The non-Gil prize names still in the pool, in pool order. They are what the announcement's pot summary adds
///     after the Gil amount (spec §5.5: <c>21,000,000 Gil + 2 prizes</c>) and what the admin preview shows.
/// </summary>
internal static class LotteryPrizePoolNames
{
	public static async Task<IReadOnlyList<string>> InPoolAsync(ILotteryPrizePoolRepository prizes,
		CancellationToken cancellationToken = default)
	{
		var inPool = await prizes.GetInPoolAsync();

		return inPool
			.Select(prize => prize.Name)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.ToList();
	}
}
