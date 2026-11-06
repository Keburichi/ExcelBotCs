namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     Creates the lottery collections' indexes. Collections are created by the repositories; the indexes need an
///     explicit, idempotent migration step, so this runs from the one-shot maintenance command and from tests.
/// </summary>
public interface ILotteryIndexInitializer
{
	Task EnsureIndexesAsync();
}
