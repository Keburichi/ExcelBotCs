using ExcelBotCs.Data;

namespace ExcelBotCs.Modules.Lottery;

public class LotteryResult : DatabaseObject
{
	public int WinningNumber { get; set; }
	public List<LotteryGuess> Guesses { get; set; }
}