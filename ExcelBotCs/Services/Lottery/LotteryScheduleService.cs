using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;

namespace ExcelBotCs.Services.Lottery;

public class LotteryScheduleService : ILotteryScheduleService
{
	private readonly ILotteryRoundRepository _rounds;
	private readonly ILotterySettingsService _settings;
	private readonly TimeProvider _time;

	public LotteryScheduleService(ILotterySettingsService settings, ILotteryRoundRepository rounds,
		TimeProvider time)
	{
		_settings = settings;
		_rounds = rounds;
		_time = time;
	}

	public async Task<LotterySchedule> GetScheduleAsync(CancellationToken cancellationToken = default)
	{
		var settings = await _settings.GetEffectiveAsync(cancellationToken);
		return LotterySchedule.CreateFrom(settings);
	}

	public async Task<LotteryScheduleSlot> GetNextSlotAsync(CancellationToken cancellationToken = default)
	{
		var schedule = await GetScheduleAsync(cancellationToken);
		return schedule.NextSlotAfter(_time.GetUtcNow());
	}

	public async Task<string> GetCurrentRoundKeyAsync(CancellationToken cancellationToken = default)
	{
		var schedule = await GetScheduleAsync(cancellationToken);
		var now = _time.GetUtcNow();
		var dueSlot = schedule.MostRecentDueSlot(now);

		var dueRound = await _rounds.GetByRoundKeyAsync(dueSlot.RoundKey);
		if (dueRound is null || dueRound.Status == LotteryRoundStatus.Scheduled) return dueSlot.RoundKey;

		return schedule.NextSlotAfter(now).RoundKey;
	}
}
