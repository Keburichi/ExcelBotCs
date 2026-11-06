using ExcelBotCs.Services.Lottery.Interfaces;

namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The one-shot lottery maintenance switches. They are the documented deployment mechanism for spec §12 steps 2
///     and 4, and they exit without starting the web server or the Discord gateway:
///     <code>
///     dotnet run --project ExcelBotCs -- --lottery-ensure-indexes
///     dotnet run --project ExcelBotCs -- --lottery-backfill
///     </code>
///     See <c>docs/lottery/operations.md</c>.
/// </summary>
public sealed record LotteryMaintenanceRequest(bool EnsureIndexes, bool Backfill)
{
	public const string EnsureIndexesFlag = "--lottery-ensure-indexes";
	public const string BackfillFlag = "--lottery-backfill";

	public bool IsRequested => EnsureIndexes || Backfill;

	public static LotteryMaintenanceRequest Parse(IEnumerable<string> args)
	{
		var flags = args.ToHashSet(StringComparer.Ordinal);
		return new LotteryMaintenanceRequest(flags.Contains(EnsureIndexesFlag), flags.Contains(BackfillFlag));
	}
}

public static class LotteryMaintenanceRunner
{
	/// <summary>Runs the requested steps and returns a process exit code.</summary>
	public static async Task<int> RunAsync(IServiceProvider services, LotteryMaintenanceRequest request)
	{
		var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(LotteryMaintenanceRunner));

		try
		{
			if (request.EnsureIndexes)
			{
				await services.GetRequiredService<ILotteryIndexInitializer>().EnsureIndexesAsync();
				logger.LogInformation("Lottery index migration finished");
			}

			if (request.Backfill)
			{
				var result = await services.GetRequiredService<ILotteryArchiveBackfillService>().BackfillAsync();
				logger.LogInformation(
					"Lottery archive backfill finished: {ResultsProcessed} results, {RoundsCreated} rounds created, " +
					"{RoundsRefreshed} refreshed, {EntriesCreated} entries created, {EntriesAlreadyPresent} already " +
					"present, {WinnersCreated} winners created",
					result.ResultsProcessed, result.RoundsCreated, result.RoundsRefreshed, result.EntriesCreated,
					result.EntriesAlreadyPresent, result.WinnersCreated);
			}

			return 0;
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Lottery maintenance failed");
			return 1;
		}
	}
}
