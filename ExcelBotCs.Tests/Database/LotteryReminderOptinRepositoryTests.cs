using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class LotteryReminderOptinRepositoryTests : MongoDbTest
{
	private ILotteryReminderOptinRepository _repository = null!;

	public LotteryReminderOptinRepositoryTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_repository = new LotteryReminderOptinRepository(mongoClient, databaseOptions);
	}

	[Fact]
	public async Task GetByDiscordIdAsync_ReturnsNull_WhenMemberNeverOptedIn()
	{
		var optIn = await _repository.GetByDiscordIdAsync(ulong.Parse(GenerateRandomDiscordId()));

		optIn.ShouldBeNull();
	}

	[Fact]
	public async Task SetEnabledAsync_CreatesOptInWithEnabledFlag()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());

		await _repository.SetEnabledAsync(discordId, true);

		var optIn = await _repository.GetByDiscordIdAsync(discordId);
		optIn.ShouldNotBeNull();
		optIn!.DiscordId.ShouldBe(discordId);
		optIn.Enabled.ShouldBeTrue();
	}

	[Fact]
	public async Task SetEnabledAsync_UpdatesTheExistingOptInInsteadOfDuplicating()
	{
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.SetEnabledAsync(discordId, true);

		await _repository.SetEnabledAsync(discordId, false);

		var all = await _repository.GetAsync();
		all.Count.ShouldBe(1);
		all[0].Enabled.ShouldBeFalse();
	}

	[Fact]
	public async Task GetEnabledAsync_ReturnsOnlyEnabledOptIns()
	{
		var optedIn = ulong.Parse(GenerateRandomDiscordId());
		var optedOut = ulong.Parse(GenerateRandomDiscordId());
		await _repository.SetEnabledAsync(optedIn, true);
		await _repository.SetEnabledAsync(optedOut, false);

		var enabled = await _repository.GetEnabledAsync();

		enabled.Count.ShouldBe(1);
		enabled[0].DiscordId.ShouldBe(optedIn);
	}

	[Fact]
	public async Task CreateAsync_Fails_WhenDiscordIdIsDuplicated()
	{
		await _repository.EnsureIndexesAsync();
		var discordId = ulong.Parse(GenerateRandomDiscordId());
		await _repository.CreateAsync(new LotteryReminderOptin { DiscordId = discordId, Enabled = true });

		var duplicate = new LotteryReminderOptin { DiscordId = discordId, Enabled = false };

		await Should.ThrowAsync<MongoWriteException>(async () => await _repository.CreateAsync(duplicate));
	}
}
