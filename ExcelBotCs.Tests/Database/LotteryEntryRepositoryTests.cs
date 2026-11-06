using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class LotteryEntryRepositoryTests : MongoDbTest
{
	private ILotteryEntryRepository _repository = null!;

	public LotteryEntryRepositoryTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_repository = new LotteryEntryRepository(mongoClient, databaseOptions);
	}

	[Fact]
	public async Task GetByRoundKeyAsync_ReturnsEmpty_WhenRoundHasNoEntries()
	{
		var entries = await _repository.GetByRoundKeyAsync("2026-07-21");

		entries.ShouldBeEmpty();
	}

	[Fact]
	public async Task CreateAsync_PersistsEntryAndReadsItBackByRound()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.CreateAsync(new LotteryEntry
		{
			RoundKey = "2026-07-21",
			DiscordId = discordId,
			Number = 56
		});

		var entries = await _repository.GetByRoundKeyAsync("2026-07-21");

		entries.Count.ShouldBe(1);
		entries[0].RoundKey.ShouldBe("2026-07-21");
		entries[0].DiscordId.ShouldBe(discordId);
		entries[0].Number.ShouldBe(56);
	}

	[Fact]
	public async Task GetByRoundKeyAsync_ReturnsOnlyThatRoundsEntries()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.CreateAsync(new LotteryEntry { RoundKey = "2026-07-21", DiscordId = discordId, Number = 7 });
		await _repository.CreateAsync(new LotteryEntry
		{
			RoundKey = "2026-07-28", DiscordId = discordId, Number = 8
		});

		var entries = await _repository.GetByRoundKeyAsync("2026-07-21");

		entries.Count.ShouldBe(1);
		entries[0].Number.ShouldBe(7);
	}

	[Fact]
	public async Task GetByRoundKeyAndDiscordIdAsync_ReturnsOnlyThatMembersEntries()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		var otherDiscordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.CreateAsync(new LotteryEntry { RoundKey = "2026-07-21", DiscordId = discordId, Number = 7 });
		await _repository.CreateAsync(new LotteryEntry
		{
			RoundKey = "2026-07-21", DiscordId = discordId, Number = 8
		});
		await _repository.CreateAsync(new LotteryEntry
		{
			RoundKey = "2026-07-21", DiscordId = otherDiscordId, Number = 9
		});

		var entries = await _repository.GetByRoundKeyAndDiscordIdAsync("2026-07-21", discordId);

		entries.Count.ShouldBe(2);
		entries.Select(e => e.Number).ShouldBe(new[] { 7, 8 }, ignoreOrder: true);
	}

	[Fact]
	public async Task AddIfMissingAsync_InsertsOnce_AndIsIdempotentOnRepeat()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		var entry = new LotteryEntry { RoundKey = "2026-07-21", DiscordId = discordId, Number = 56 };

		var created = await _repository.AddIfMissingAsync(entry);
		var createdAgain = await _repository.AddIfMissingAsync(new LotteryEntry
		{
			RoundKey = "2026-07-21", DiscordId = discordId, Number = 56
		});

		created.ShouldBeTrue();
		createdAgain.ShouldBeFalse();
		var entries = await _repository.GetByRoundKeyAsync("2026-07-21");
		entries.Count.ShouldBe(1);
	}

	[Fact]
	public async Task CreateAsync_Fails_WhenRoundDiscordIdAndNumberAreDuplicated()
	{
		await _repository.EnsureIndexesAsync();
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.CreateAsync(new LotteryEntry { RoundKey = "2026-07-21", DiscordId = discordId, Number = 56 });

		var duplicate = new LotteryEntry { RoundKey = "2026-07-21", DiscordId = discordId, Number = 56 };

		await Should.ThrowAsync<MongoWriteException>(async () => await _repository.CreateAsync(duplicate));
	}

	[Fact]
	public async Task CreateAsync_AllowsSameNumberInDifferentRounds()
	{
		await _repository.EnsureIndexesAsync();
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.CreateAsync(new LotteryEntry { RoundKey = "2026-07-21", DiscordId = discordId, Number = 56 });
		await _repository.CreateAsync(new LotteryEntry { RoundKey = "2026-07-28", DiscordId = discordId, Number = 56 });

		var entries = await _repository.GetByDiscordIdAsync(discordId);

		entries.Count.ShouldBe(2);
	}
}
