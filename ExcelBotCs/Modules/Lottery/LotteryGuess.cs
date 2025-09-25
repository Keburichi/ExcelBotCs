using ExcelBotCs.Data;

namespace ExcelBotCs.Modules.Lottery;

public class LotteryGuess : DatabaseObject
{
	public ulong DiscordId { get; set; }
	public int Number { get; set; }
}