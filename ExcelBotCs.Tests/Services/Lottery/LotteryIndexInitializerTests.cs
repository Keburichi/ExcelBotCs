using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The index migration must cover every lottery collection through the dependency-injection registration, so it
///     doubles as a guard that all nine repositories are wired up.
/// </summary>
[Collection("MongoDB")]
public class LotteryIndexInitializerTests : MongoDbTest
{
	private IMongoClient _mongoClient = null!;
	private IOptions<DatabaseOptions> _databaseOptions = null!;

	public LotteryIndexInitializerTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_mongoClient = mongoClient;
		_databaseOptions = databaseOptions;
	}

	[Fact]
	public async Task EnsureIndexesAsync_CreatesTheIndexesOfEveryLotteryCollection()
	{
		await EnsureAllLotteryIndexesAsync();

		(await ReadIndexNamesAsync("lottery_rounds")).ShouldContain("ux_lottery_rounds_RoundKey");
		(await ReadIndexNamesAsync("lottery_entries")).ShouldContain("ux_lottery_entries_RoundKey_DiscordId_Number");
		(await ReadIndexNamesAsync("lottery_winners")).ShouldContain("ix_lottery_winners_Guess");
		(await ReadIndexNamesAsync("lottery_pot_ledger")).ShouldContain("ux_lottery_pot_ledger_IdempotencyKey");
		(await ReadIndexNamesAsync("lottery_reminder_optins")).ShouldContain("ux_lottery_reminder_optins_DiscordId");
		(await ReadIndexNamesAsync("lottery_awards")).ShouldContain("ix_lottery_awards_RoundKey_DiscordId");
		(await ReadIndexNamesAsync("lottery_prize_pool")).ShouldContain("ix_lottery_prize_pool_Status");
		(await ReadIndexNamesAsync("lottery_settings")).ShouldContain("ux_lottery_settings_Key");
		(await ReadIndexNamesAsync("lottery_settings_history"))
			.ShouldContain("ix_lottery_settings_history_SettingsKey_Version");
	}

	[Fact]
	public async Task EnsureIndexesAsync_CanRunTwice()
	{
		await EnsureAllLotteryIndexesAsync();
		var before = (await ReadIndexNamesAsync("lottery_rounds")).Count;

		await EnsureAllLotteryIndexesAsync();

		(await ReadIndexNamesAsync("lottery_rounds")).Count.ShouldBe(before);
	}

	[Fact]
	public async Task EnsureIndexesAsync_LeavesTheLotteryResultsArchiveWithoutIndexes()
	{
		_ = new LotteryResultRepository(_mongoClient, _databaseOptions);

		await EnsureAllLotteryIndexesAsync();

		(await ReadIndexNamesAsync("lottery_results")).ShouldBe(["_id_"]);
	}

	private async Task EnsureAllLotteryIndexesAsync()
	{
		var services = new ServiceCollection();
		services.AddSingleton(_mongoClient);
		services.AddSingleton(_databaseOptions);
		services.AddLogging();
		services.AddDatabaseRepositories();
		services.AddSingleton<ILotteryIndexInitializer, LotteryIndexInitializer>();

		await using var provider = services.BuildServiceProvider();

		// Resolving the initializer resolves all nine lottery repositories, so a missing registration fails here.
		await provider.GetRequiredService<ILotteryIndexInitializer>().EnsureIndexesAsync();
	}

	private async Task<List<string>> ReadIndexNamesAsync(string collectionName)
	{
		var database = _mongoClient.GetDatabase("TestDatabase");
		var collections = await (await database.ListCollectionNamesAsync()).ToListAsync();
		if (!collections.Contains(collectionName)) return [];

		var indexes = await (await database.GetCollection<BsonDocument>(collectionName).Indexes.ListAsync())
			.ToListAsync();

		return indexes.Select(i => i["name"].AsString).ToList();
	}
}
