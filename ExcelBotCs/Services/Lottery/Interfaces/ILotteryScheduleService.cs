namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The live schedule: the pure <see cref="LotterySchedule" /> resolved from the effective settings and the current
///     clock. The reminder (#112), the announcement (#111) and the admin page (#114) read the next run and the current
///     round key from here instead of re-deriving the schedule.
/// </summary>
public interface ILotteryScheduleService
{
	/// <summary>The schedule from the effective settings (run day, time and timezone).</summary>
	Task<LotterySchedule> GetScheduleAsync(CancellationToken cancellationToken = default);

	/// <summary>The next run instant and round key after now.</summary>
	Task<LotteryScheduleSlot> GetNextSlotAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     The round key that the next draw will use — the round guesses and extra-guess awards belong to. It is the most
	///     recent due slot while that date has no round yet, and the next slot once it has one.
	/// </summary>
	Task<string> GetCurrentRoundKeyAsync(CancellationToken cancellationToken = default);
}
