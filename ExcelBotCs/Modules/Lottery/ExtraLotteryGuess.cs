using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

public class ExtraLotteryGuess : BaseEntity
{
	public ulong DiscordId { get; set; }
	public string Reason { get; set; }
}
