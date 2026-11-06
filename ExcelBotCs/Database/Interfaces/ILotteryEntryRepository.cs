using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotteryEntryRepository : IBaseRepository<LotteryEntry>
{
	Task<List<LotteryEntry>> GetByRoundKeyAsync(string roundKey);

	Task<List<LotteryEntry>> GetByRoundKeyAndDiscordIdAsync(string roundKey, ulong discordId);

	Task<List<LotteryEntry>> GetByDiscordIdAsync(ulong discordId);

	/// <summary>
	///     Inserts the entry if no entry with the same <c>(RoundKey, DiscordId, Number)</c> exists. The unique index
	///     backs this up. Used by the archive backfill, which must be safe to run more than once.
	/// </summary>
	/// <returns><c>true</c> when the entry was created, <c>false</c> when it was already present.</returns>
	Task<bool> AddIfMissingAsync(LotteryEntry entry);
}
