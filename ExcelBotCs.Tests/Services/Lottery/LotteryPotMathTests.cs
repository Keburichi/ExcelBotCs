using ExcelBotCs.Services.Lottery;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The money rules (spec §3, §5.3 and #98): whole-Gil shares that add up to the pot, the remainder to the
///     earliest winning entry, the 1M floor on corrections, the ledger idempotency keys and the retry policy.
/// </summary>
public class LotteryPotMathTests
{
	private const long BasePot = 1_000_000;
	private const long Increment = 1_000_000;

	[Fact]
	public void Split_OneWinner_TakesTheWholePot()
	{
		LotteryPotMath.Split(21_000_000, 1).ShouldBe([21_000_000L]);
	}

	[Fact]
	public void Split_TwoWinners_SplitsEvenly()
	{
		LotteryPotMath.Split(21_000_000, 2).ShouldBe([10_500_000L, 10_500_000L]);
	}

	[Fact]
	public void Split_ThreeWinners_FloorsEveryShareAndGivesTheRemainderToTheFirst()
	{
		// 1,000,000 / 3 = 333,333.33; the floor is 333,333 and the 1 Gil remainder goes to the earliest entry.
		var shares = LotteryPotMath.Split(1_000_000, 3);

		shares.ShouldBe([333_334L, 333_333L, 333_333L]);
		shares.Sum().ShouldBe(1_000_000);
	}

	[Fact]
	public void Split_NinetyNineWinners_TheRemainderIsAtMostWinnersMinusOne()
	{
		var shares = LotteryPotMath.Split(1_000_000, 99);

		shares.Count.ShouldBe(99);
		shares.Sum().ShouldBe(1_000_000);
		shares[0].ShouldBe(1_000_000 / 99 + 1_000_000 % 99);
		shares.Skip(1).ShouldAllBe(share => share == 1_000_000 / 99);
	}

	[Fact]
	public void Split_ZeroPot_IsZeroForEveryWinner()
	{
		LotteryPotMath.Split(0, 2).ShouldBe([0L, 0L]);
	}

	[Fact]
	public void Split_RemainderGoesToTheEarliestEntry_NotToTheHighestId()
	{
		// The split only knows the order it is given; the caller orders winners by their winning entry's date.
		var shares = LotteryPotMath.Split(10, 3);
		var orderedWinners = new[] { "earliest", "middle", "latest" };
		var payouts = orderedWinners.Zip(shares, (winner, share) => (winner, share)).ToDictionary(x => x.winner,
			x => x.share);

		payouts["earliest"].ShouldBe(4);
		payouts["middle"].ShouldBe(3);
		payouts["latest"].ShouldBe(3);
		payouts.ShouldAllBe(_ => true);
	}

	[Fact]
	public void Split_RejectsAnImpossibleWinnerCountOrANegativePot()
	{
		Should.Throw<ArgumentOutOfRangeException>(() => LotteryPotMath.Split(1_000_000, 0));
		Should.Throw<ArgumentOutOfRangeException>(() => LotteryPotMath.Split(-1, 2));
	}

	[Fact]
	public void NoWinner_GrowsThePotByTheWeeklyIncrement()
	{
		LotteryPotMath.AfterNoWinner(21_000_000, Increment).ShouldBe(22_000_000);
		Should.Throw<ArgumentOutOfRangeException>(() => LotteryPotMath.AfterNoWinner(1_000_000, -1));
	}

	[Fact]
	public void ApplyCorrection_LeavesAnAdjustmentThatStaysAboveTheFloorUntouched()
	{
		LotteryPotMath.ApplyCorrection(5_000_000, -2_000_000, BasePot).ShouldBe(-2_000_000);
		LotteryPotMath.ApplyCorrection(5_000_000, 2_500_000, BasePot).ShouldBe(2_500_000);
	}

	[Fact]
	public void ApplyCorrection_ClampsADebitAtTheFloor()
	{
		// The pot is 1.5M and the correction wants to remove 1M; only 0.5M may be removed.
		LotteryPotMath.ApplyCorrection(1_500_000, -1_000_000, BasePot).ShouldBe(-500_000);
	}

	[Fact]
	public void ApplyCorrection_AtTheFloorADebitBecomesZero()
	{
		LotteryPotMath.ApplyCorrection(BasePot, -1_000_000, BasePot).ShouldBe(0);
	}

	[Fact]
	public void ApplyCorrection_NeverLeavesThePotBelowTheFloor()
	{
		var applied = LotteryPotMath.ApplyCorrection(1_200_000, -5_000_000, BasePot);

		(1_200_000 + applied).ShouldBe(BasePot);
	}

	[Fact]
	public void LedgerKeys_AreDeterministicPerRoundAndStep()
	{
		LotteryPotLedgerKeys.BasePot.ShouldBe("base-pot");
		LotteryPotLedgerKeys.WeeklyIncrement("2026-11-10").ShouldBe("2026-11-10:weekly_increment");
		LotteryPotLedgerKeys.Payout("2026-11-10").ShouldBe("2026-11-10:payout");
		LotteryPotLedgerKeys.Reset("2026-11-10").ShouldBe("2026-11-10:reset");
		LotteryPotLedgerKeys.VoidReversal("2026-11-10", "payout").ShouldBe("2026-11-10:void:payout");
	}

	[Fact]
	public void DrawBackoff_UsesTheSpecDelaysAndStopsAfterFiveRetries()
	{
		LotteryRetryPolicy.NextDrawDelay(1).ShouldBe(TimeSpan.FromSeconds(30));
		LotteryRetryPolicy.NextDrawDelay(2).ShouldBe(TimeSpan.FromMinutes(1));
		LotteryRetryPolicy.NextDrawDelay(3).ShouldBe(TimeSpan.FromMinutes(2));
		LotteryRetryPolicy.NextDrawDelay(4).ShouldBe(TimeSpan.FromMinutes(5));
		LotteryRetryPolicy.NextDrawDelay(5).ShouldBe(TimeSpan.FromMinutes(10));
		LotteryRetryPolicy.NextDrawDelay(6).ShouldBeNull();
		LotteryRetryPolicy.NextDrawDelay(0).ShouldBeNull();
	}

	[Fact]
	public void AnnouncementBackoff_KeepsRetryingAndIsBoundedByTheWindow()
	{
		LotteryRetryPolicy.NextAnnouncementDelay(1).ShouldBe(TimeSpan.FromSeconds(30));
		LotteryRetryPolicy.NextAnnouncementDelay(5).ShouldBe(TimeSpan.FromMinutes(10));
		LotteryRetryPolicy.NextAnnouncementDelay(6).ShouldBe(TimeSpan.FromMinutes(10));

		var drawnAt = new DateTime(2026, 11, 10, 19, 0, 0, DateTimeKind.Utc);
		var windowEnds = drawnAt + LotteryRetryPolicy.AnnouncementWindow;

		LotteryRetryPolicy.IsAnnouncementWindowOpen(windowEnds, drawnAt.AddMinutes(59)).ShouldBeTrue();
		LotteryRetryPolicy.IsAnnouncementWindowOpen(windowEnds, windowEnds).ShouldBeFalse();
	}
}
