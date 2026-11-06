using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The one allowance calculation the reminder, the view page and guess validation share (spec §3, §5.7). Expected
///     values are the spec's own arithmetic: one base guess plus awards, minus the guesses already used.
/// </summary>
public class LotteryGuessAllowanceTests
{
	[Theory]
	[InlineData(0, 0, 1)]
	[InlineData(1, 0, 0)]
	[InlineData(0, 1, 2)]
	[InlineData(1, 1, 1)]
	[InlineData(2, 1, 0)]
	[InlineData(3, 1, 0)]
	[InlineData(0, 2, 3)]
	public void Remaining_IsTheBaseGuessPlusAwardsMinusUsed(int used, int awarded, int expected)
	{
		LotteryGuessAllowance.Remaining(used, awarded).ShouldBe(expected);
	}

	[Fact]
	public void Allowed_IsOneBaseGuessPlusAwards()
	{
		LotteryGuessAllowance.Allowed(0).ShouldBe(1);
		LotteryGuessAllowance.Allowed(3).ShouldBe(4);
	}
}
