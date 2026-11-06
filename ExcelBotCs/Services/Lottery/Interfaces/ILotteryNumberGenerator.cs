namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The draw itself: one number from 1 to 99. The production implementation wraps the existing cryptographic
///     <see cref="Prng" />; tests swap in a deterministic generator so a winning number can be forced.
/// </summary>
public interface ILotteryNumberGenerator
{
	int DrawWinningNumber();
}
