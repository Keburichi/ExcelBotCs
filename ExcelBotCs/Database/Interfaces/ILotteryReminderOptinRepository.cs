using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Database.Interfaces;

public interface ILotteryReminderOptinRepository : IBaseRepository<LotteryReminderOptin>
{
	Task<LotteryReminderOptin?> GetByDiscordIdAsync(ulong discordId);

	/// <summary>Creates or updates the member's single opt-in document.</summary>
	Task SetEnabledAsync(ulong discordId, bool enabled);

	/// <summary>Every member who currently has reminders turned on.</summary>
	Task<List<LotteryReminderOptin>> GetEnabledAsync();
}
