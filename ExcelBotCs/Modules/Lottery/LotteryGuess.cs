using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

public class LotteryGuess : BaseEntity
{
	public ulong DiscordId { get; set; }
	public int Number { get; set; }
}
