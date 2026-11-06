using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Services.Lottery.Interfaces;

namespace ExcelBotCs.Services.Lottery;

public class LotteryIndexInitializer : ILotteryIndexInitializer
{
	private readonly ILotteryAwardRepository _awards;
	private readonly ILotteryEntryRepository _entries;
	private readonly ILogger<LotteryIndexInitializer> _logger;
	private readonly ILotteryPotLedgerRepository _potLedger;
	private readonly ILotteryPrizePoolRepository _prizePool;
	private readonly ILotteryReminderOptinRepository _reminderOptins;
	private readonly ILotteryRoundRepository _rounds;
	private readonly ILotterySettingsRepository _settings;
	private readonly ILotterySettingsHistoryRepository _settingsHistory;
	private readonly ILotteryWinnerRepository _winners;

	public LotteryIndexInitializer(
		ILotteryRoundRepository rounds,
		ILotteryEntryRepository entries,
		ILotteryWinnerRepository winners,
		ILotteryPotLedgerRepository potLedger,
		ILotteryReminderOptinRepository reminderOptins,
		ILotteryAwardRepository awards,
		ILotteryPrizePoolRepository prizePool,
		ILotterySettingsRepository settings,
		ILotterySettingsHistoryRepository settingsHistory,
		ILogger<LotteryIndexInitializer> logger)
	{
		_rounds = rounds;
		_entries = entries;
		_winners = winners;
		_potLedger = potLedger;
		_reminderOptins = reminderOptins;
		_awards = awards;
		_prizePool = prizePool;
		_settings = settings;
		_settingsHistory = settingsHistory;
		_logger = logger;
	}

	public async Task EnsureIndexesAsync()
	{
		await _rounds.EnsureIndexesAsync();
		await _entries.EnsureIndexesAsync();
		await _winners.EnsureIndexesAsync();
		await _potLedger.EnsureIndexesAsync();
		await _reminderOptins.EnsureIndexesAsync();
		await _awards.EnsureIndexesAsync();
		await _prizePool.EnsureIndexesAsync();
		await _settings.EnsureIndexesAsync();
		await _settingsHistory.EnsureIndexesAsync();

		_logger.LogInformation("Lottery indexes are in place");
	}
}
