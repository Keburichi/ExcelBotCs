using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Database;

public class LotteryRoundRepository : BaseRepository<LotteryRound>, ILotteryRoundRepository
{
	private static readonly string[] BlockingStatuses =
	[
		LotteryRoundStatus.Drawing, LotteryRoundStatus.Drawn, LotteryRoundStatus.Announced,
		LotteryRoundStatus.Failed
	];

	private static readonly string[] VoidableStatuses =
	[
		LotteryRoundStatus.Drawing, LotteryRoundStatus.Drawn, LotteryRoundStatus.Announced,
		LotteryRoundStatus.Completed, LotteryRoundStatus.Failed
	];

	public LotteryRoundRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
		: base(mongoClient, databaseOptions)
	{
	}

	protected override string GetCollectionName()
	{
		return "lottery_rounds";
	}

	public override IReadOnlyList<CreateIndexModel<LotteryRound>> GetIndexModels()
	{
		return
		[
			Index("ux_lottery_rounds_RoundKey", Ascending(r => r.RoundKey), unique: true),
			Index("ix_lottery_rounds_ScheduledAt", Ascending(r => r.ScheduledAt)),
			Index("ix_lottery_rounds_Status", Ascending(r => r.Status))
		];
	}

	public async Task<LotteryRound?> GetByRoundKeyAsync(string roundKey)
	{
		return await Collection.Find(r => r.RoundKey == roundKey).FirstOrDefaultAsync();
	}

	public async Task<List<LotteryRound>> GetByStatusAsync(string status)
	{
		return await Collection.Find(r => r.Status == status)
			.SortByDescending(r => r.ScheduledAt)
			.ToListAsync();
	}

	public async Task<List<LotteryRound>> GetLatestAsync(int limit)
	{
		return await Collection.Find(_ => true)
			.SortByDescending(r => r.ScheduledAt)
			.Limit(limit)
			.ToListAsync();
	}

	public async Task<LotteryRound?> GetNewestBlockingAsync()
	{
		return await Collection.Find(r => BlockingStatuses.Contains(r.Status))
			.SortByDescending(r => r.ScheduledAt)
			.FirstOrDefaultAsync();
	}

	public async Task<LotteryRound?> GetNewestScheduledDueAsync(DateTime nowUtc)
	{
		return await Collection.Find(r => r.Status == LotteryRoundStatus.Scheduled && r.ScheduledAt <= nowUtc)
			.SortByDescending(r => r.ScheduledAt)
			.FirstOrDefaultAsync();
	}

	public async Task<LotteryRound> EnsureScheduledAsync(string roundKey, DateTime scheduledAtUtc)
	{
		var update = Builders<LotteryRound>.Update
			.SetOnInsert(r => r.RoundKey, roundKey)
			.SetOnInsert(r => r.ScheduledAt, scheduledAtUtc)
			.SetOnInsert(r => r.Status, LotteryRoundStatus.Scheduled)
			.SetOnInsert(r => r.DateCreated, DateTime.UtcNow)
			.SetOnInsert(r => r.DateModified, DateTime.UtcNow);

		try
		{
			await Collection.UpdateOneAsync(r => r.RoundKey == roundKey, update, new UpdateOptions { IsUpsert = true });
		}
		catch (MongoWriteException exception) when (MongoDuplicateKey.IsDuplicate(exception))
		{
			// Another caller created the round between our read and our upsert; its document is the winner.
		}

		var round = await GetByRoundKeyAsync(roundKey);
		return round ?? throw new InvalidOperationException($"Round {roundKey} could not be created or read.");
	}

	public async Task<LotteryRound?> TryClaimForDrawAsync(string roundKey, string claimOwner, DateTime nowUtc,
		bool fromFailed, bool ignoreRetry = false)
	{
		var filterBuilder = Builders<LotteryRound>.Filter;

		// A fresh scheduled round is always claimable. An interrupted drawing round is only claimable when its claim
		// cannot still be live: another process owns it (a restart), the claim has gone stale, or the previous attempt
		// registered its retry and that instant has come. This is what makes two parallel "run now" calls end with
		// exactly one draw instead of two.
		var claimable = filterBuilder.Eq(r => r.Status, LotteryRoundStatus.Scheduled);

		if (ignoreRetry)
		{
			claimable |= filterBuilder.Eq(r => r.Status, LotteryRoundStatus.Drawing);
		}
		else
		{
			var staleBefore = nowUtc - LotteryRetryPolicy.ClaimTimeout;
			var drawingIsFree = filterBuilder.Ne(r => r.ClaimOwner, claimOwner) |
			                    filterBuilder.Lte(r => r.ClaimedAt, staleBefore) |
			                    (filterBuilder.Ne(r => r.NextRetryAt, null) &
			                     filterBuilder.Lte(r => r.NextRetryAt, nowUtc));

			claimable |= filterBuilder.Eq(r => r.Status, LotteryRoundStatus.Drawing) & drawingIsFree;
		}

		if (fromFailed)
			claimable |= filterBuilder.Eq(r => r.Status, LotteryRoundStatus.Failed) &
			             filterBuilder.Eq(r => r.FailedStep, LotteryFailedStep.Draw);

		var filter = filterBuilder.Eq(r => r.RoundKey, roundKey) & claimable;

		var update = Builders<LotteryRound>.Update
			.Set(r => r.Status, LotteryRoundStatus.Drawing)
			.Set(r => r.ClaimedAt, nowUtc)
			.Set(r => r.ClaimOwner, claimOwner)
			.Set(r => r.DateModified, nowUtc);

		// ReturnDocument.Before makes the claim result a report of what was claimed, so the caller can tell
		// "already running" and "already run" apart from "claimed by me just now".
		return await Collection.FindOneAndUpdateAsync(filter, update,
			new FindOneAndUpdateOptions<LotteryRound> { ReturnDocument = ReturnDocument.Before });
	}

	public async Task<bool> TrySetWinningNumberAsync(string roundKey, int winningNumber, DateTime runAtUtc,
		long potBefore)
	{
		var filter = Builders<LotteryRound>.Filter.Eq(r => r.RoundKey, roundKey) &
		             Builders<LotteryRound>.Filter.Eq(r => r.WinningNumber, null);

		var update = Builders<LotteryRound>.Update
			.Set(r => r.WinningNumber, winningNumber)
			.Set(r => r.RunAt, runAtUtc)
			.Set(r => r.PotBefore, potBefore)
			.Set(r => r.Status, LotteryRoundStatus.Drawing)
			.Set(r => r.DateModified, runAtUtc);

		return await UpdateAppliedAsync(filter, update);
	}

	public async Task<bool> TryMarkDrawnAsync(string roundKey, long potAfter, DateTime retryUntilUtc)
	{
		var filter = Builders<LotteryRound>.Filter.Eq(r => r.RoundKey, roundKey) &
		             Builders<LotteryRound>.Filter.Eq(r => r.Status, LotteryRoundStatus.Drawing);

		var update = Builders<LotteryRound>.Update
			.Set(r => r.Status, LotteryRoundStatus.Drawn)
			.Set(r => r.PotAfter, potAfter)
			.Set(r => r.RetryUntil, retryUntilUtc)
			.Set(r => r.FailedStep, null)
			.Set(r => r.Attempts, 0)
			.Set(r => r.NextRetryAt, null)
			.Set(r => r.LastError, null)
			.Set(r => r.DateModified, DateTime.UtcNow);

		return await UpdateAppliedAsync(filter, update);
	}

	public async Task<bool> TryMarkAnnouncedAsync(string roundKey, string? announcementMessageId)
	{
		var filter = Builders<LotteryRound>.Filter.Eq(r => r.RoundKey, roundKey) &
		             Builders<LotteryRound>.Filter.Eq(r => r.Status, LotteryRoundStatus.Drawn);

		var update = Builders<LotteryRound>.Update
			.Set(r => r.Status, LotteryRoundStatus.Announced)
			.Set(r => r.AnnouncementMessageId, announcementMessageId)
			.Set(r => r.FailedStep, null)
			.Set(r => r.Attempts, 0)
			.Set(r => r.NextRetryAt, null)
			.Set(r => r.LastError, null)
			.Set(r => r.DateModified, DateTime.UtcNow);

		return await UpdateAppliedAsync(filter, update);
	}

	public async Task<bool> TryMarkCompletedAsync(string roundKey)
	{
		var filter = Builders<LotteryRound>.Filter.Eq(r => r.RoundKey, roundKey) &
		             Builders<LotteryRound>.Filter.Eq(r => r.Status, LotteryRoundStatus.Announced);

		var update = Builders<LotteryRound>.Update
			.Set(r => r.Status, LotteryRoundStatus.Completed)
			.Set(r => r.FailedStep, null)
			.Set(r => r.Attempts, 0)
			.Set(r => r.NextRetryAt, null)
			.Set(r => r.LastError, null)
			.Set(r => r.DateModified, DateTime.UtcNow);

		return await UpdateAppliedAsync(filter, update);
	}

	public async Task<bool> TryStartPostingAsync(string roundKey, DateTime nowUtc)
	{
		var filter = Builders<LotteryRound>.Filter.Eq(r => r.RoundKey, roundKey) &
		             Builders<LotteryRound>.Filter.Eq(r => r.Status, LotteryRoundStatus.Drawn) &
		             Builders<LotteryRound>.Filter.Eq(r => r.PostingStartedAt, null);

		var update = Builders<LotteryRound>.Update
			.Set(r => r.PostingStartedAt, nowUtc)
			.Set(r => r.DateModified, nowUtc);

		return await UpdateAppliedAsync(filter, update);
	}

	public async Task<bool> RegisterFailureAsync(string roundKey, string failedStep, string status, int attempts,
		string error, DateTime? nextRetryAtUtc, DateTime? retryUntilUtc)
	{
		var filter = Builders<LotteryRound>.Filter.Eq(r => r.RoundKey, roundKey) &
		             Builders<LotteryRound>.Filter.In(r => r.Status,
		             [
			             LotteryRoundStatus.Drawing, LotteryRoundStatus.Drawn, LotteryRoundStatus.Announced,
			             LotteryRoundStatus.Scheduled
		             ]);

		var update = Builders<LotteryRound>.Update
			.Set(r => r.FailedStep, failedStep)
			.Set(r => r.Status, status)
			.Set(r => r.Attempts, attempts)
			.Set(r => r.LastError, Truncate(error))
			.Set(r => r.NextRetryAt, nextRetryAtUtc)
			.Set(r => r.RetryUntil, retryUntilUtc)
			.Set(r => r.DateModified, DateTime.UtcNow);

		return await UpdateAppliedAsync(filter, update);
	}

	public async Task<bool> TryReopenFailedAsync(string roundKey, string failedStep, string newStatus,
		DateTime? retryUntilUtc)
	{
		var filter = Builders<LotteryRound>.Filter.Eq(r => r.RoundKey, roundKey) &
		             Builders<LotteryRound>.Filter.Eq(r => r.Status, LotteryRoundStatus.Failed) &
		             Builders<LotteryRound>.Filter.Eq(r => r.FailedStep, failedStep);

		var update = Builders<LotteryRound>.Update
			.Set(r => r.Status, newStatus)
			.Set(r => r.FailedStep, null)
			.Set(r => r.Attempts, 0)
			.Set(r => r.NextRetryAt, null)
			.Set(r => r.LastError, null)
			.Set(r => r.RetryUntil, retryUntilUtc)
			.Set(r => r.ClaimedAt, null)
			.Set(r => r.ClaimOwner, null)
			.Set(r => r.DateModified, DateTime.UtcNow);

		return await UpdateAppliedAsync(filter, update);
	}

	public async Task<bool> TryMarkSkippedAsync(string roundKey)
	{
		var filter = Builders<LotteryRound>.Filter.Eq(r => r.RoundKey, roundKey) &
		             Builders<LotteryRound>.Filter.Eq(r => r.Status, LotteryRoundStatus.Scheduled);

		var update = Builders<LotteryRound>.Update
			.Set(r => r.Status, LotteryRoundStatus.Skipped)
			.Set(r => r.DateModified, DateTime.UtcNow);

		return await UpdateAppliedAsync(filter, update);
	}

	public async Task<bool> TryMarkVoidedAsync(string roundKey, string reason, ulong? actorDiscordId,
		DateTime nowUtc)
	{
		var filter = Builders<LotteryRound>.Filter.Eq(r => r.RoundKey, roundKey) &
		             Builders<LotteryRound>.Filter.In(r => r.Status, VoidableStatuses);

		var update = Builders<LotteryRound>.Update
			.Set(r => r.Status, LotteryRoundStatus.Voided)
			.Set(r => r.VoidReason, Truncate(reason))
			.Set(r => r.VoidedAt, nowUtc)
			.Set(r => r.VoidedByDiscordId, actorDiscordId)
			.Set(r => r.FailedStep, null)
			.Set(r => r.NextRetryAt, null)
			.Set(r => r.DateModified, nowUtc);

		return await UpdateAppliedAsync(filter, update);
	}

	public async Task<bool> TryRescheduleAsync(string currentRoundKey, string newRoundKey,
		DateTime scheduledAtUtc)
	{
		var filter = Builders<LotteryRound>.Filter.Eq(r => r.RoundKey, currentRoundKey) &
		             Builders<LotteryRound>.Filter.Eq(r => r.Status, LotteryRoundStatus.Scheduled);

		var update = Builders<LotteryRound>.Update
			.Set(r => r.RoundKey, newRoundKey)
			.Set(r => r.ScheduledAt, scheduledAtUtc)
			.Set(r => r.DateModified, DateTime.UtcNow);

		try
		{
			return await UpdateAppliedAsync(filter, update);
		}
		catch (MongoWriteException exception) when (MongoDuplicateKey.IsDuplicate(exception))
		{
			// A round already exists on the target date; the unique RoundKey index rejected the move.
			return false;
		}
	}

	public async Task<bool> ImportCompletedRoundAsync(string roundKey, DateTime runAtUtc, int winningNumber)
	{
		var update = Builders<LotteryRound>.Update
			.Set(r => r.RunAt, runAtUtc)
			.Set(r => r.Status, LotteryRoundStatus.Completed)
			.Set(r => r.WinningNumber, winningNumber)
			.SetOnInsert(r => r.RoundKey, roundKey)
			.SetOnInsert(r => r.ScheduledAt, runAtUtc)
			.SetOnInsert(r => r.DateCreated, DateTime.UtcNow)
			.SetOnInsert(r => r.DateModified, DateTime.UtcNow);

		var result = await Collection.UpdateOneAsync(
			r => r.RoundKey == roundKey,
			update,
			new UpdateOptions { IsUpsert = true });

		return result.UpsertedId != null;
	}

	private async Task<bool> UpdateAppliedAsync(FilterDefinition<LotteryRound> filter,
		UpdateDefinition<LotteryRound> update)
	{
		var result = await Collection.UpdateOneAsync(filter, update);
		return result.MatchedCount > 0;
	}

	/// <summary>
	///     Bounds <c>LastError</c>/<c>VoidReason</c>: a Discord or driver exception message can be very long, and the
	///     admin view and the alert only need the cause.
	/// </summary>
	private static string Truncate(string value, int maxLength = 2_000)
	{
		return value.Length <= maxLength ? value : value[..maxLength];
	}
}
