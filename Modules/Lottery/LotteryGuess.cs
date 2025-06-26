using ExcelBotCs.Data;

namespace ExcelBotCs.Modules.Lottery;

public class LotteryGuess : DatabaseObject
{
	public ulong DiscordId { get; set; }
	public int Number { get; set; }
}

public class LotteryResult : DatabaseObject
{
	public int WinningNumber { get; set; }
	public List<LotteryGuess> Guesses { get; set; }
}