using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class LotteryRoundRepositoryTests : MongoDbTest
{
	private ILotteryRoundRepository _repository = null!;

	public LotteryRoundRepositoryTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_repository = new LotteryRoundRepository(mongoClient, databaseOptions);
	}

	[Fact]
	public async Task GetByRoundKeyAsync_ReturnsNull_WhenRoundDoesNotExist()
	{
		var round = await _repository.GetByRoundKeyAsync("2026-07-21");

		round.ShouldBeNull();
	}

	[Fact]
	public async Task CreateAsync_PersistsRoundWithAllFields()
	{
		var scheduledAt = new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc);
		var round = new LotteryRound
		{
			RoundKey = "2026-07-21",
			ScheduledAt = scheduledAt,
			RunAt = scheduledAt.AddMinutes(3),
			Status = LotteryRoundStatus.Completed,
			WinningNumber = 56
		};

		await _repository.CreateAsync(round);

		var stored = await _repository.GetByRoundKeyAsync("2026-07-21");
		stored.ShouldNotBeNull();
		stored!.RoundKey.ShouldBe("2026-07-21");
		stored.ScheduledAt.ShouldBe(scheduledAt);
		stored.RunAt.ShouldBe(scheduledAt.AddMinutes(3));
		stored.Status.ShouldBe("completed");
		stored.WinningNumber.ShouldBe(56);
		stored.PotBefore.ShouldBeNull();
		stored.PotAfter.ShouldBeNull();
		stored.AnnouncementMessageId.ShouldBeNull();
		stored.FailedStep.ShouldBeNull();
		stored.LastError.ShouldBeNull();
		stored.NextRetryAt.ShouldBeNull();
		stored.ClaimedAt.ShouldBeNull();
		stored.ClaimOwner.ShouldBeNull();
		stored.PostingStartedAt.ShouldBeNull();
		stored.Attempts.ShouldBe(0);
	}

	[Fact]
	public async Task CreateAsync_PersistsOperationalFields()
	{
		var claimedAt = new DateTime(2026, 7, 21, 18, 1, 0, DateTimeKind.Utc);
		await _repository.CreateAsync(new LotteryRound
		{
			RoundKey = "2026-07-28",
			ScheduledAt = claimedAt,
			Status = LotteryRoundStatus.Failed,
			FailedStep = LotteryFailedStep.Announcement,
			Attempts = 2,
			NextRetryAt = claimedAt.AddMinutes(5),
			LastError = "discord 500",
			ClaimedAt = claimedAt,
			ClaimOwner = "worker-1"
		});

		var stored = await _repository.GetByRoundKeyAsync("2026-07-28");

		stored.ShouldNotBeNull();
		stored!.Status.ShouldBe("failed");
		stored.FailedStep.ShouldBe("announcement");
		stored.Attempts.ShouldBe(2);
		stored.NextRetryAt.ShouldBe(claimedAt.AddMinutes(5));
		stored.LastError.ShouldBe("discord 500");
		stored.ClaimedAt.ShouldBe(claimedAt);
		stored.ClaimOwner.ShouldBe("worker-1");
	}

	[Fact]
	public async Task CreateAsync_Fails_WhenRoundKeyIsDuplicated()
	{
		await _repository.EnsureIndexesAsync();
		var scheduledAt = new DateTime(2026, 8, 4, 18, 0, 0, DateTimeKind.Utc);
		await _repository.CreateAsync(new LotteryRound
		{
			RoundKey = "2026-08-04",
			ScheduledAt = scheduledAt,
			Status = LotteryRoundStatus.Scheduled
		});

		var duplicate = new LotteryRound
		{
			RoundKey = "2026-08-04",
			ScheduledAt = scheduledAt,
			Status = LotteryRoundStatus.Drawn
		};

		await Should.ThrowAsync<MongoWriteException>(async () => await _repository.CreateAsync(duplicate));
	}

	[Fact]
	public async Task GetByStatusAsync_ReturnsOnlyRoundsInThatStatus()
	{
		await _repository.CreateAsync(new LotteryRound
		{
			RoundKey = "2026-08-11",
			Status = LotteryRoundStatus.Scheduled,
			ScheduledAt = new DateTime(2026, 8, 11, 18, 0, 0, DateTimeKind.Utc)
		});
		await _repository.CreateAsync(new LotteryRound
		{
			RoundKey = "2026-08-18",
			Status = LotteryRoundStatus.Completed,
			ScheduledAt = new DateTime(2026, 8, 18, 18, 0, 0, DateTimeKind.Utc)
		});

		var scheduled = await _repository.GetByStatusAsync(LotteryRoundStatus.Scheduled);

		scheduled.Count.ShouldBe(1);
		scheduled[0].RoundKey.ShouldBe("2026-08-11");
	}

	[Fact]
	public async Task GetLatestAsync_ReturnsRoundsNewestFirst()
	{
		await _repository.CreateAsync(new LotteryRound
		{
			RoundKey = "2026-09-01",
			Status = LotteryRoundStatus.Completed,
			ScheduledAt = new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc)
		});
		await _repository.CreateAsync(new LotteryRound
		{
			RoundKey = "2026-09-08",
			Status = LotteryRoundStatus.Completed,
			ScheduledAt = new DateTime(2026, 9, 8, 18, 0, 0, DateTimeKind.Utc)
		});

		var latest = await _repository.GetLatestAsync(1);

		latest.Count.ShouldBe(1);
		latest[0].RoundKey.ShouldBe("2026-09-08");
	}
}
