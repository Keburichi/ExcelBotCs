using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class LotteryAwardRepositoryTests : MongoDbTest
{
	private ILotteryAwardRepository _repository = null!;

	public LotteryAwardRepositoryTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_repository = new LotteryAwardRepository(mongoClient, databaseOptions);
	}

	[Fact]
	public async Task CreateAsync_PersistsAwardAndReadsItBackByRound()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		var awardedBy = ulong.Parse(GenerateRandomDiscordId());

		await _repository.CreateAsync(new LotteryAward
		{
			RoundKey = "2026-07-21",
			DiscordId = discordId,
			Reason = "event participation",
			AwardedByDiscordId = awardedBy
		});

		var awards = await _repository.GetByRoundKeyAsync("2026-07-21");

		awards.Count.ShouldBe(1);
		awards[0].DiscordId.ShouldBe(discordId);
		awards[0].Reason.ShouldBe("event participation");
		awards[0].AwardedByDiscordId.ShouldBe(awardedBy);
	}

	[Fact]
	public async Task GetByRoundKeyAsync_ReturnsOnlyThatRoundsAwards()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.CreateAsync(new LotteryAward
		{
			RoundKey = "2026-07-21", DiscordId = discordId, Reason = "event"
		});
		await _repository.CreateAsync(new LotteryAward
		{
			RoundKey = "2026-07-28", DiscordId = discordId, Reason = "riddle"
		});

		var awards = await _repository.GetByRoundKeyAsync("2026-07-21");

		awards.Count.ShouldBe(1);
		awards[0].Reason.ShouldBe("event");
	}

	[Fact]
	public async Task GetByRoundKeyAndDiscordIdAsync_ReturnsThatMembersAwards()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.CreateAsync(new LotteryAward
		{
			RoundKey = "2026-07-21", DiscordId = discordId, Reason = "event"
		});
		await _repository.CreateAsync(new LotteryAward
		{
			RoundKey = "2026-07-21", DiscordId = discordId, Reason = "riddle"
		});
		await _repository.CreateAsync(new LotteryAward
		{
			RoundKey = "2026-07-21", DiscordId = otherDiscordId, Reason = "event"
		});

		var awards = await _repository.GetByRoundKeyAndDiscordIdAsync("2026-07-21", discordId);

		awards.Count.ShouldBe(2);
		awards.Select(a => a.Reason).ShouldBe(new[] { "event", "riddle" }, ignoreOrder: true);
	}

	[Fact]
	public async Task GetByDiscordIdAsync_ReturnsAwardsAcrossRounds()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.CreateAsync(new LotteryAward { RoundKey = "2026-07-21", DiscordId = discordId, Reason = "a" });
		await _repository.CreateAsync(new LotteryAward { RoundKey = "2026-07-28", DiscordId = discordId, Reason = "b" });

		var awards = await _repository.GetByDiscordIdAsync(discordId);

		awards.Count.ShouldBe(2);
	}
}
