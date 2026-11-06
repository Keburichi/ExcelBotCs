using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.Utilities;

namespace ExcelBotCs.Services.Lottery;

public class LotteryArchiveBackfillService : ILotteryArchiveBackfillService
{
	private readonly ILotteryEntryRepository _entries;
	private readonly ILogger<LotteryArchiveBackfillService> _logger;
	private readonly ILotteryResultRepository _results;
	private readonly ILotteryRoundRepository _rounds;

	public LotteryArchiveBackfillService(
		ILotteryResultRepository results,
		ILotteryRoundRepository rounds,
		ILotteryEntryRepository entries,
		ILogger<LotteryArchiveBackfillService> logger)
	{
		_results = results;
		_rounds = rounds;
		_entries = entries;
		_logger = logger;
	}

	public async Task<LotteryBackfillResult> BackfillAsync()
	{
		// Oldest first keeps the mapping deterministic when two archive documents share a local date.
		var results = (await _results.GetAsync()).OrderBy(r => r.DateCreated).ToList();

		var roundsCreated = 0;
		var roundsRefreshed = 0;
		var entriesCreated = 0;
		var entriesAlreadyPresent = 0;

		foreach (var result in results)
		{
			var roundKey = LotteryRoundKey.FromUtc(result.DateCreated);
			var runAt = DateTime.SpecifyKind(result.DateCreated, DateTimeKind.Utc);

			var created = await _rounds.ImportCompletedRoundAsync(roundKey, runAt, result.WinningNumber);
			if (created) roundsCreated++;
			else roundsRefreshed++;

			foreach (var guess in result.Guesses ?? [])
			{
				var entryCreated = await _entries.AddIfMissingAsync(new LotteryEntry
				{
					RoundKey = roundKey,
					DiscordId = guess.DiscordId,
					Number = guess.Number
				});

				if (entryCreated) entriesCreated++;
				else entriesAlreadyPresent++;
			}
		}

		_logger.LogInformation(
			"lottery_results backfill processed {ResultsProcessed} results: {RoundsCreated} rounds created, " +
			"{RoundsRefreshed} refreshed, {EntriesCreated} entries created, {EntriesAlreadyPresent} already present",
			results.Count, roundsCreated, roundsRefreshed, entriesCreated, entriesAlreadyPresent);

		return new LotteryBackfillResult(results.Count, roundsCreated, roundsRefreshed, entriesCreated,
			entriesAlreadyPresent);
	}
}
