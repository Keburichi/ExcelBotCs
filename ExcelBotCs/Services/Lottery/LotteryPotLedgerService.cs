using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using MongoDB.Driver;

namespace ExcelBotCs.Services.Lottery;

public class LotteryPotLedgerService : ILotteryPotLedgerService
{
	private readonly ILotteryPotLedgerRepository _ledger;
	private readonly ILogger<LotteryPotLedgerService> _logger;

	public LotteryPotLedgerService(ILotteryPotLedgerRepository ledger, ILogger<LotteryPotLedgerService> logger)
	{
		_ledger = ledger;
		_logger = logger;
	}

	public async Task<long> GetBalanceAsync(long basePot, CancellationToken cancellationToken = default)
	{
		var latest = (await _ledger.GetLatestAsync(1)).FirstOrDefault();
		return latest?.BalanceAfter ?? basePot;
	}

	public async Task<long> EnsureBalanceAsync(long basePot, CancellationToken cancellationToken = default)
	{
		var latest = (await _ledger.GetLatestAsync(1)).FirstOrDefault();
		if (latest is not null) return latest.BalanceAfter;

		var opening = await AppendAsync(null, LotteryPotLedgerType.Reset, basePot, basePot,
			"Opening balance: the lottery starts at the base pot.", null, LotteryPotLedgerKeys.BasePot,
			cancellationToken);

		_logger.LogInformation("Established the opening lottery pot at {Balance} Gil", opening.BalanceAfter);
		return opening.BalanceAfter;
	}

	public async Task<LotteryPotLedgerEntry?> FindAsync(string idempotencyKey,
		CancellationToken cancellationToken = default)
	{
		return await _ledger.GetByIdempotencyKeyAsync(idempotencyKey);
	}

	public async Task<List<LotteryPotLedgerEntry>> GetByRoundKeyAsync(string roundKey,
		CancellationToken cancellationToken = default)
	{
		return await _ledger.GetByRoundKeyAsync(roundKey);
	}

	public async Task<LotteryPotLedgerEntry> AppendAsync(string? roundKey, string type, long amount,
		long balanceAfter, string reason, ulong? actorDiscordId, string idempotencyKey,
		CancellationToken cancellationToken = default)
	{
		var existing = await _ledger.GetByIdempotencyKeyAsync(idempotencyKey);
		if (existing is not null) return existing;

		var entry = new LotteryPotLedgerEntry
		{
			RoundKey = roundKey,
			Type = type,
			Amount = amount,
			Reason = reason,
			ActorDiscordId = actorDiscordId,
			BalanceAfter = balanceAfter,
			IdempotencyKey = idempotencyKey
		};

		try
		{
			await _ledger.CreateAsync(entry);
			return entry;
		}
		catch (MongoWriteException exception) when (MongoDuplicateKey.IsDuplicate(exception))
		{
			// A concurrent attempt (two ticks, or a retry racing the first write) stored the entry first. The unique
			// IdempotencyKey index is the guarantee that the pot moved exactly once.
			var stored = await _ledger.GetByIdempotencyKeyAsync(idempotencyKey);
			if (stored is null) throw;

			_logger.LogInformation("Pot ledger entry {IdempotencyKey} already existed; reusing the stored entry",
				idempotencyKey);
			return stored;
		}
	}
}
