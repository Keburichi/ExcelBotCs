using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     Reads and writes the singleton <c>lottery_settings</c> document. Everything the scheduler, the reminder, the
///     announcement and the admin page need comes from <see cref="GetEffectiveAsync" />, which merges the stored
///     document (validated and repaired) with the deployment config, so no caller has to re-implement precedence.
/// </summary>
public interface ILotterySettingsService
{
	/// <summary>
	///     The effective settings: env &gt; stored &gt; defaults. A missing document yields the §7 defaults; a stored
	///     value that fails validation is replaced by its default and reported in
	///     <see cref="LotteryEffectiveSettings.InvalidFields" />. Never throws for bad stored data.
	/// </summary>
	Task<LotteryEffectiveSettings> GetEffectiveAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Validates and stores an admin settings change, bumping <see cref="LotterySettings.Version" />, recording
	///     the actor and writing one <c>lottery_settings_history</c> row. A rejected change writes nothing. A change
	///     that would not alter the effective settings writes nothing either (<see cref="LotterySettingsSaveResult.Changed" />
	///     is false). The Discord audit notice is best effort: a failed post never fails the save.
	/// </summary>
	Task<LotterySettingsSaveResult> SaveAsync(LotterySettingsUpdate update, ulong? actorDiscordId,
		CancellationToken cancellationToken = default);
}
