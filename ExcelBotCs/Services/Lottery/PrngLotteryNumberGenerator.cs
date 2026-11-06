using ExcelBotCs.Services.Lottery.Interfaces;

namespace ExcelBotCs.Services.Lottery;

public sealed class PrngLotteryNumberGenerator : ILotteryNumberGenerator
{
	private readonly Prng _prng;

	public PrngLotteryNumberGenerator(Prng prng)
	{
		_prng = prng;
	}

	public int DrawWinningNumber()
	{
		// The same call the manual run has always used (1-99). Clamped defensively: Prng.NextFloat returns the
		// full uint range divided by uint.MaxValue, so the top of the range can round to 1.0 and produce 100 for one
		// value in four billion. The lottery range and the RNG are not changed (§2 non-goals).
		return Math.Clamp(_prng.NextInt(0, 99), 1, 99);
	}
}
