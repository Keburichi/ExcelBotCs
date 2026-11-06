namespace ExcelBotCs.Services.Lottery;

/// <summary>
///     The pot arithmetic (spec §3 and §5.3, resolution #98). Every rule here is pure and independent of storage, so
///     the rounding, the floor and the signs are pinned by unit tests instead of by a integration run.
/// </summary>
public static class LotteryPotMath
{
	/// <summary>
	///     Splits the pot between <paramref name="winnerCount" /> winners. Each share is floored to whole Gil and the
	///     whole remainder goes to the first winner — the caller passes winners ordered by the date their winning
	///     entry was recorded, so the remainder lands on the earliest guess. The shares always sum to the pot.
	/// </summary>
	public static IReadOnlyList<long> Split(long pot, int winnerCount)
	{
		if (pot < 0) throw new ArgumentOutOfRangeException(nameof(pot), pot, "The pot cannot be negative.");
		if (winnerCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(winnerCount), winnerCount, "A draw needs at least one winner.");

		var share = pot / winnerCount;
		var remainder = pot - share * winnerCount;

		var shares = new long[winnerCount];
		for (var index = 0; index < winnerCount; index++) shares[index] = share;
		shares[0] += remainder;

		return shares;
	}

	/// <summary>No winner: the pot grows by the deployment-configured increment.</summary>
	public static long AfterNoWinner(long pot, long weeklyIncrement)
	{
		if (pot < 0) throw new ArgumentOutOfRangeException(nameof(pot), pot, "The pot cannot be negative.");
		if (weeklyIncrement < 0)
			throw new ArgumentOutOfRangeException(nameof(weeklyIncrement), weeklyIncrement,
				"The weekly increment cannot be negative.");

		return pot + weeklyIncrement;
	}

	/// <summary>
	///     The amount a signed correction may actually apply: it is clamped so the balance never drops below
	///     <paramref name="floor" /> (1,000,000 Gil). A correction that would cross the floor is applied partially and
	///     a correction at the floor becomes a no-op, which the ledger still records for the audit trail.
	/// </summary>
	public static long ApplyCorrection(long balance, long requestedAmount, long floor)
	{
		var next = balance + requestedAmount;
		return next < floor ? floor - balance : requestedAmount;
	}
}

/// <summary>
///     The deterministic idempotency keys of the pot ledger (spec §5.4 and §6). A retried operation reuses its key, so
///     the unique index turns a duplicate write into a no-op instead of a double payment. The shape is
///     <c>{roundKey}:{step}</c>; the opening balance uses the fixed <c>base-pot</c> key.
/// </summary>
public static class LotteryPotLedgerKeys
{
	/// <summary>Establishes the base pot on a deployment that has no ledger yet.</summary>
	public const string BasePot = "base-pot";

	/// <summary>The +increment entry of a round without a winner.</summary>
	public static string WeeklyIncrement(string roundKey)
	{
		return $"{roundKey}:weekly_increment";
	}

	/// <summary>The −pot prize debit of a winning round.</summary>
	public static string Payout(string roundKey)
	{
		return $"{roundKey}:payout";
	}

	/// <summary>The +base-pot entry that returns the pot to its floor after a win.</summary>
	public static string Reset(string roundKey)
	{
		return $"{roundKey}:reset";
	}

	/// <summary>The compensating correction that reverses one of the round's own ledger entries when it is voided.</summary>
	public static string VoidReversal(string roundKey, string originalType)
	{
		return $"{roundKey}:void:{originalType}";
	}
}

/// <summary>
///     The retry policy of spec §5.10 as code. Draw and database writes get five attempts with the documented
///     backoff; the announcement retries with the same backoff but is bounded by a one-hour window rather than by an
///     attempt count, because a Discord outage should be ridden out instead of giving up after five minutes.
/// </summary>
public static class LotteryRetryPolicy
{
	/// <summary>The documented draw/database backoff: 30s, 1m, 2m, 5m, 10m.</summary>
	public static readonly IReadOnlyList<TimeSpan> DrawBackoff =
	[
		TimeSpan.FromSeconds(30),
		TimeSpan.FromMinutes(1),
		TimeSpan.FromMinutes(2),
		TimeSpan.FromMinutes(5),
		TimeSpan.FromMinutes(10)
	];

	/// <summary>
	///     The draw gets five retries on top of the first attempt, so the five documented delays are all used; the
	///     failure of the sixth attempt marks the round failed (spec §5.10 "5 attempts, 30s/1m/2m/5m/10m").
	/// </summary>
	public const int DrawMaxRetries = 5;

	/// <summary>How long a <c>drawn</c> round may keep retrying its announcement before it is marked failed.</summary>
	public static readonly TimeSpan AnnouncementWindow = TimeSpan.FromHours(1);

	/// <summary>
	///     A claimed draw that has been in <c>drawing</c> for longer than this without a recorded retry is treated as
	///     abandoned (the process crashed mid-claim) and may be claimed again. Normal draws take seconds.
	/// </summary>
	public static readonly TimeSpan ClaimTimeout = TimeSpan.FromMinutes(2);

	/// <summary>
	///     The wait before the next draw attempt after <paramref name="failures" /> consecutive failures, or null when
	///     the retries are exhausted.
	/// </summary>
	public static TimeSpan? NextDrawDelay(int failures)
	{
		return failures >= 1 && failures <= DrawMaxRetries ? DrawBackoff[failures - 1] : null;
	}

	/// <summary>The announcement backoff is capped at the last step; the window, not the count, ends the retries.</summary>
	public static TimeSpan NextAnnouncementDelay(int attempts)
	{
		var index = Math.Clamp(attempts - 1, 0, DrawBackoff.Count - 1);
		return DrawBackoff[index];
	}

	/// <summary>Whether an announcement may still be retried at <paramref name="now" />.</summary>
	public static bool IsAnnouncementWindowOpen(DateTime retryWindowEndsAt, DateTime now)
	{
		return now < retryWindowEndsAt;
	}
}
