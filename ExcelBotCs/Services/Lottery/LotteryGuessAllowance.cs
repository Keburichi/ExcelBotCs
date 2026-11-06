namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The weekly guess allowance (spec §3, §5.7): one base guess plus one per extra-guess award, minus the guesses the
///     member has already used this round. The reminder audience, the member's view and the guess validation all use
///     this one calculation, so "2 guesses remaining" in a reminder means exactly what the view page shows.
/// </summary>
public static class LotteryGuessAllowance
{
	/// <summary>One base guess plus every extra-guess award.</summary>
	public static int Allowed(int awardedGuesses)
	{
		return 1 + awardedGuesses;
	}

	/// <summary>Guesses still available, never below zero (an award may have been flushed between rounds).</summary>
	public static int Remaining(int usedGuesses, int awardedGuesses)
	{
		return Math.Max(0, Allowed(awardedGuesses) - usedGuesses);
	}
}
