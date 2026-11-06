using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

/// <summary>
///     The lottery indexes are part of the data contract of spec §6: they make <c>RoundKey</c> unique, make
///     ledger retries idempotent, and keep the number statistics cheap. Later tickets (scheduler, history,
///     announcements) rely on these exact names, so the tests pin them.
/// </summary>
[Collection("MongoDB")]
public class LotteryIndexTests : MongoDbTest
{
	private IMongoClient _mongoClient = null!;
	private ILotteryRoundRepository _rounds = null!;
	private ILotteryEntryRepository _entries = null!;
	private ILotteryWinnerRepository _winners = null!;
	private ILotteryPotLedgerRepository _ledger = null!;
	private ILotteryReminderOptinRepository _optins = null!;
	private ILotteryAwardRepository _awards = null!;
	private ILotteryPrizePoolRepository _prizes = null!;
	private ILotterySettingsRepository _settings = null!;
	private ILotterySettingsHistoryRepository _settingsHistory = null!;

	public LotteryIndexTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_mongoClient = mongoClient;
		_rounds = new LotteryRoundRepository(mongoClient, databaseOptions);
		_entries = new LotteryEntryRepository(mongoClient, databaseOptions);
		_winners = new LotteryWinnerRepository(mongoClient, databaseOptions);
		_ledger = new LotteryPotLedgerRepository(mongoClient, databaseOptions);
		_optins = new LotteryReminderOptinRepository(mongoClient, databaseOptions);
		_awards = new LotteryAwardRepository(mongoClient, databaseOptions);
		_prizes = new LotteryPrizePoolRepository(mongoClient, databaseOptions);
		_settings = new LotterySettingsRepository(mongoClient, databaseOptions);
		_settingsHistory = new LotterySettingsHistoryRepository(mongoClient, databaseOptions);
	}

	[Fact]
	public async Task EnsureIndexesAsync_CreatesAllSpecIndexes()
	{
		await EnsureAllIndexesAsync();

		var rounds = await ReadIndexesAsync("lottery_rounds");
		rounds["ux_lottery_rounds_RoundKey"].Unique.ShouldBeTrue();
		rounds["ux_lottery_rounds_RoundKey"].Keys.ShouldBe(["RoundKey"]);
		rounds["ix_lottery_rounds_ScheduledAt"].Unique.ShouldBeFalse();
		rounds["ix_lottery_rounds_ScheduledAt"].Keys.ShouldBe(["ScheduledAt"]);
		rounds["ix_lottery_rounds_Status"].Keys.ShouldBe(["Status"]);

		var entries = await ReadIndexesAsync("lottery_entries");
		entries["ux_lottery_entries_RoundKey_DiscordId_Number"].Unique.ShouldBeTrue();
		entries["ux_lottery_entries_RoundKey_DiscordId_Number"].Keys
			.ShouldBe(["RoundKey", "DiscordId", "Number"]);
		entries["ix_lottery_entries_Number"].Unique.ShouldBeFalse();
		entries["ix_lottery_entries_Number"].Keys.ShouldBe(["Number"]);

		var winners = await ReadIndexesAsync("lottery_winners");
		winners["ix_lottery_winners_RoundKey"].Keys.ShouldBe(["RoundKey"]);
		winners["ix_lottery_winners_DiscordId"].Keys.ShouldBe(["DiscordId"]);
		winners["ix_lottery_winners_Guess"].Keys.ShouldBe(["Guess"]);
		winners.Values.ShouldAllBe(i => !i.Unique);

		var ledger = await ReadIndexesAsync("lottery_pot_ledger");
		ledger["ux_lottery_pot_ledger_IdempotencyKey"].Unique.ShouldBeTrue();
		ledger["ux_lottery_pot_ledger_IdempotencyKey"].Keys.ShouldBe(["IdempotencyKey"]);
		ledger["ix_lottery_pot_ledger_RoundKey_DateCreated"].Keys.ShouldBe(["RoundKey", "DateCreated"]);
		ledger["ix_lottery_pot_ledger_RoundKey_DateCreated"].Directions.ShouldBe(["1", "-1"]);
		ledger["ix_lottery_pot_ledger_Type"].Keys.ShouldBe(["Type"]);

		var optins = await ReadIndexesAsync("lottery_reminder_optins");
		optins["ux_lottery_reminder_optins_DiscordId"].Unique.ShouldBeTrue();
		optins["ux_lottery_reminder_optins_DiscordId"].Keys.ShouldBe(["DiscordId"]);

		var awards = await ReadIndexesAsync("lottery_awards");
		awards["ix_lottery_awards_RoundKey_DiscordId"].Keys.ShouldBe(["RoundKey", "DiscordId"]);

		var prizes = await ReadIndexesAsync("lottery_prize_pool");
		prizes["ix_lottery_prize_pool_Status"].Keys.ShouldBe(["Status"]);
		prizes["ix_lottery_prize_pool_AwardedRoundKey"].Keys.ShouldBe(["AwardedRoundKey"]);

		var settings = await ReadIndexesAsync("lottery_settings");
		settings["ux_lottery_settings_Key"].Unique.ShouldBeTrue();
		settings["ux_lottery_settings_Key"].Keys.ShouldBe(["Key"]);

		var settingsHistory = await ReadIndexesAsync("lottery_settings_history");
		settingsHistory["ix_lottery_settings_history_SettingsKey_Version"].Keys
			.ShouldBe(["SettingsKey", "Version"]);
	}

	[Fact]
	public async Task EnsureIndexesAsync_IsIdempotent()
	{
		await EnsureAllIndexesAsync();
		var before = (await ReadIndexesAsync("lottery_rounds")).Keys.Count;

		await EnsureAllIndexesAsync();

		var after = await ReadIndexesAsync("lottery_rounds");
		after.Keys.Count.ShouldBe(before);
	}

	private async Task EnsureAllIndexesAsync()
	{
		await _rounds.EnsureIndexesAsync();
		await _entries.EnsureIndexesAsync();
		await _winners.EnsureIndexesAsync();
		await _ledger.EnsureIndexesAsync();
		await _optins.EnsureIndexesAsync();
		await _awards.EnsureIndexesAsync();
		await _prizes.EnsureIndexesAsync();
		await _settings.EnsureIndexesAsync();
		await _settingsHistory.EnsureIndexesAsync();
	}

	private async Task<Dictionary<string, IndexInfo>> ReadIndexesAsync(string collectionName)
	{
		var database = _mongoClient.GetDatabase("TestDatabase");
		var cursor = await database.GetCollection<BsonDocument>(collectionName).Indexes.ListAsync();
		var documents = await cursor.ToListAsync();

		return documents.ToDictionary(
			d => d["name"].AsString,
			d => new IndexInfo(
				d["key"].AsBsonDocument.Names.ToList(),
				d["key"].AsBsonDocument.Elements.Select(e => e.Value.ToString()!).ToList(),
				d.TryGetValue("unique", out var unique) && unique.ToBoolean()));
	}

	private sealed record IndexInfo(List<string> Keys, List<string> Directions, bool Unique);
}
