using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ExcelBotCs.Tests.Database;

[Collection("MongoDB")]
public class LotterySettingsRepositoryTests : MongoDbTest
{
	private ILotterySettingsRepository _settings = null!;
	private ILotterySettingsHistoryRepository _history = null!;

	public LotterySettingsRepositoryTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_settings = new LotterySettingsRepository(mongoClient, databaseOptions);
		_history = new LotterySettingsHistoryRepository(mongoClient, databaseOptions);
	}

	[Fact]
	public async Task GetSingletonAsync_ReturnsNull_WhenNothingWasSavedYet()
	{
		var settings = await _settings.GetSingletonAsync();

		settings.ShouldBeNull();
	}

	[Fact]
	public async Task SaveAsync_CreatesTheSingletonDocument()
	{
		var updatedBy = ulong.Parse(GenerateRandomDiscordId());

		await _settings.SaveAsync(new LotterySettings
		{
			RunDay = "Tuesday",
			RunTime = "20:00",
			RunTimezone = "Europe/Berlin",
			ReminderDay = "Monday",
			ReminderTime = "20:00",
			ReminderTimezone = "Europe/Berlin",
			RemindersEnabled = true,
			PauseAutomation = false,
			WinTemplate = "Win {winners}",
			NoWinTemplate = "No win {number}",
			Version = 1,
			UpdatedByDiscordId = updatedBy
		});

		var settings = await _settings.GetSingletonAsync();
		settings.ShouldNotBeNull();
		settings!.Key.ShouldBe("lottery");
		settings.RunDay.ShouldBe("Tuesday");
		settings.RunTime.ShouldBe("20:00");
		settings.RunTimezone.ShouldBe("Europe/Berlin");
		settings.ReminderDay.ShouldBe("Monday");
		settings.ReminderTime.ShouldBe("20:00");
		settings.ReminderTimezone.ShouldBe("Europe/Berlin");
		settings.RemindersEnabled.ShouldBeTrue();
		settings.PauseAutomation.ShouldBeFalse();
		settings.WinTemplate.ShouldBe("Win {winners}");
		settings.NoWinTemplate.ShouldBe("No win {number}");
		settings.Version.ShouldBe(1);
		settings.UpdatedByDiscordId.ShouldBe(updatedBy);
	}

	[Fact]
	public async Task SaveAsync_ReplacesTheSingleton_InsteadOfCreatingASecondOne()
	{
		await _settings.SaveAsync(new LotterySettings { RunDay = "Tuesday", Version = 1 });

		await _settings.SaveAsync(new LotterySettings { RunDay = "Wednesday", Version = 2 });

		var all = await _settings.GetAsync();
		all.Count.ShouldBe(1);
		all[0].RunDay.ShouldBe("Wednesday");
		all[0].Version.ShouldBe(2);
	}

	[Fact]
	public async Task SettingsHistory_RecordsVersionsOldestFirst()
	{
		await _history.CreateAsync(new LotterySettingsHistory
		{
			SettingsKey = LotterySettings.SingletonKey,
			Version = 2,
			ActorDiscordId = ulong.Parse(GenerateRandomDiscordId()),
			ChangedAt = new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc),
			OldValues = new Dictionary<string, object?> { ["RunTime"] = "19:00" },
			NewValues = new Dictionary<string, object?> { ["RunTime"] = "20:00" }
		});
		await _history.CreateAsync(new LotterySettingsHistory
		{
			SettingsKey = LotterySettings.SingletonKey,
			Version = 1,
			ActorDiscordId = null,
			ChangedAt = new DateTime(2026, 7, 14, 18, 0, 0, DateTimeKind.Utc),
			OldValues = new Dictionary<string, object?>(),
			NewValues = new Dictionary<string, object?> { ["RunTime"] = "19:00" }
		});

		var history = await _history.GetBySettingsKeyAsync(LotterySettings.SingletonKey);

		history.Count.ShouldBe(2);
		history[0].Version.ShouldBe(1);
		history[1].Version.ShouldBe(2);
		history[1].OldValues["RunTime"].ShouldBe("19:00");
		history[1].NewValues["RunTime"].ShouldBe("20:00");
		history[1].ActorDiscordId.ShouldNotBeNull();
		history[0].ActorDiscordId.ShouldBeNull();
	}
}
