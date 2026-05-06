using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Modules.Lottery;

public class LotteryResult : BaseEntity
{
	public int WinningNumber { get; set; }
	public List<LotteryGuess> Guesses { get; set; }
}
