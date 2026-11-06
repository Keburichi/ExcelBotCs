using ExcelBotCs.Database;
using ExcelBotCs.Database.Interfaces;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using ExcelBotCs.TestFramework.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The effective settings reader: stored values beat the §7 defaults, a missing or unreadable value falls back to
///     its default instead of breaking the caller, and the deployment flags are never taken from the database.
/// </summary>
[Collection("MongoDB")]
public class LotterySettingsServiceTests : MongoDbTest
{
	private ILotterySettingsRepository _settingsRepository = null!;
	private ILotterySettingsHistoryRepository _historyRepository = null!;
	private Mock<IDiscordMessageService> _discordMessageService = null!;
	private Mock<ILogger<LotterySettingsService>> _logger = null!;
	private LotteryOptions _options = null!;
	private LotterySettingsService _service = null!;

	public LotterySettingsServiceTests(MongoDbFixture fixture) : base(fixture)
	{
	}

	protected override void InitializeRepository(IMongoClient mongoClient, IOptions<DatabaseOptions> databaseOptions)
	{
		_settingsRepository = new LotterySettingsRepository(mongoClient, databaseOptions);
		_historyRepository = new LotterySettingsHistoryRepository(mongoClient, databaseOptions);
	}

	protected override Task OnAfterInitializeAsync()
	{
		_options = new LotteryOptions();
		_discordMessageService = new Mock<IDiscordMessageService>();
		_logger = new Mock<ILogger<LotterySettingsService>>();

		RebuildService();

		return Task.CompletedTask;
	}

	protected override Task BeforeTearDownAsync()
	{
		_discordMessageService.VerifyAll();
		return Task.CompletedTask;
	}

	private void RebuildService()
	{
		_service = new LotterySettingsService(_settingsRepository, _historyRepository,
			Options.Create(_options), _discordMessageService.Object, _logger.Object);
	}

	[Fact]
	public async Task GetEffectiveAsync_WithoutAStoredDocument_ReturnsTheSpecDefaults()
	{
		var effective = await _service.GetEffectiveAsync();

		effective.RunDay.ShouldBe("Tuesday");
		effective.RunTime.ShouldBe("20:00");
		effective.RunTimezone.ShouldBe("Europe/Berlin");
		effective.ReminderDay.ShouldBe("Monday");
		effective.ReminderTime.ShouldBe("18:00");
		effective.ReminderTimezone.ShouldBe("Europe/Berlin");
		effective.RemindersEnabled.ShouldBeFalse();
		effective.PauseAutomation.ShouldBeFalse();
		effective.WinTemplate.ShouldBe(
			"the winning number was {number}! Congratulations to {winners}! 🎉\n" +
			"They take the {prize} pot{bonusItems}.\n" +
			"The jackpot resets to {nextPot} Gil for next week.");
		effective.NoWinTemplate.ShouldBe(
			"the winning number was {number}! So no one won this week again {emoji}\n" +
			"The jackpot goes up to {pot}!!");
		effective.Version.ShouldBe(0);
		effective.UpdatedByDiscordId.ShouldBeNull();
		effective.InvalidFields.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetEffectiveAsync_StoredValuesBeatTheDefaults()
	{
		await _settingsRepository.SaveAsync(new LotterySettings
		{
			RunDay = "Sunday",
			RunTime = "21:30",
			RunTimezone = "UTC",
			ReminderDay = "Wednesday",
			ReminderTime = "09:00",
			ReminderTimezone = "Europe/London",
			RemindersEnabled = true,
			PauseAutomation = true,
			WinTemplate = "custom win",
			NoWinTemplate = "custom no win",
			Version = 4,
			UpdatedByDiscordId = 42
		});

		var effective = await _service.GetEffectiveAsync();

		effective.RunDay.ShouldBe("Sunday");
		effective.RunTime.ShouldBe("21:30");
		effective.RunTimezone.ShouldBe("UTC");
		effective.ReminderDay.ShouldBe("Wednesday");
		effective.ReminderTime.ShouldBe("09:00");
		effective.ReminderTimezone.ShouldBe("Europe/London");
		effective.RemindersEnabled.ShouldBeTrue();
		effective.PauseAutomation.ShouldBeTrue();
		effective.WinTemplate.ShouldBe("custom win");
		effective.NoWinTemplate.ShouldBe("custom no win");
		effective.Version.ShouldBe(4);
		effective.UpdatedByDiscordId.ShouldBe(42ul);
		effective.InvalidFields.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetEffectiveAsync_InvalidStoredValuesFallBackPerField()
	{
		// A document written by hand (or by an older build) is repaired field by field: the valid reminder values
		// survive, the invalid run values do not.
		await _settingsRepository.SaveAsync(new LotterySettings
		{
			RunDay = "Funday",
			RunTime = "25:00",
			RunTimezone = "Mars/Olympus",
			ReminderDay = "thursday",
			ReminderTime = "18:45",
			ReminderTimezone = "Europe/Berlin",
			Version = 2
		});

		var effective = await _service.GetEffectiveAsync();

		effective.RunDay.ShouldBe("Tuesday");
		effective.RunTime.ShouldBe("20:00");
		effective.RunTimezone.ShouldBe("Europe/Berlin");
		effective.ReminderDay.ShouldBe("Thursday");
		effective.ReminderTime.ShouldBe("18:45");
		effective.ReminderTimezone.ShouldBe("Europe/Berlin");
		effective.Version.ShouldBe(2);
		effective.InvalidFields.ShouldBe(["RunDay", "RunTime", "RunTimezone"]);
	}

	[Fact]
	public async Task GetEffectiveAsync_BlankTemplatesFallBackToTheSpecDefaults()
	{
		await _settingsRepository.SaveAsync(new LotterySettings
		{
			RunDay = "Tuesday",
			RunTime = "20:00",
			RunTimezone = "Europe/Berlin",
			ReminderDay = "Monday",
			ReminderTime = "18:00",
			ReminderTimezone = "Europe/Berlin",
			WinTemplate = " ",
			NoWinTemplate = "",
			Version = 1
		});

		var effective = await _service.GetEffectiveAsync();

		effective.WinTemplate.ShouldBe(LotterySettingsDefaults.WinTemplate);
		effective.NoWinTemplate.ShouldBe(LotterySettingsDefaults.NoWinTemplate);
	}

	[Fact]
	public async Task GetEffectiveAsync_DeploymentFlagsComeFromConfigurationNotTheStoredDocument()
	{
		// Base pot and weekly increment are deployment config (§7): a stored document cannot change them.
		_options.Automation.Enabled = true;
		_options.Automation.DryRun = false;
		_options.Automation.BasePot = 21_000_000;
		_options.Automation.WeeklyIncrement = 2_000_000;
		RebuildService();

		await _settingsRepository.SaveAsync(new LotterySettings
		{
			RunDay = "Tuesday",
			RunTime = "20:00",
			RunTimezone = "Europe/Berlin",
			ReminderDay = "Monday",
			ReminderTime = "18:00",
			ReminderTimezone = "Europe/Berlin",
			Version = 1
		});

		var effective = await _service.GetEffectiveAsync();

		effective.AutomationEnabled.ShouldBeTrue();
		effective.DryRun.ShouldBeFalse();
		effective.BasePot.ShouldBe(21_000_000);
		effective.WeeklyIncrement.ShouldBe(2_000_000);
		effective.Status.ShouldBe(LotteryAutomationStatus.Live);
	}

	[Fact]
	public async Task GetEffectiveAsync_EnabledFalseIsTheFinalOverride()
	{
		_options.Automation.Enabled = false;
		_options.Automation.DryRun = true;
		RebuildService();

		await _settingsRepository.SaveAsync(new LotterySettings
		{
			RunDay = "Tuesday",
			RunTime = "20:00",
			RunTimezone = "Europe/Berlin",
			ReminderDay = "Monday",
			ReminderTime = "18:00",
			ReminderTimezone = "Europe/Berlin",
			PauseAutomation = true,
			Version = 1
		});

		var effective = await _service.GetEffectiveAsync();

		effective.Status.ShouldBe(LotteryAutomationStatus.Disabled);
		effective.Status.SchedulerEnabled.ShouldBeFalse();
	}

	[Fact]
	public async Task GetEffectiveAsync_StoredPauseBeatsDryRun()
	{
		_options.Automation.Enabled = true;
		_options.Automation.DryRun = true;
		RebuildService();

		await _settingsRepository.SaveAsync(new LotterySettings
		{
			RunDay = "Tuesday",
			RunTime = "20:00",
			RunTimezone = "Europe/Berlin",
			ReminderDay = "Monday",
			ReminderTime = "18:00",
			ReminderTimezone = "Europe/Berlin",
			PauseAutomation = true,
			Version = 1
		});

		var effective = await _service.GetEffectiveAsync();

		effective.Status.ShouldBe(LotteryAutomationStatus.Paused);
		effective.ToUpdate().PauseAutomation.ShouldBeTrue();
	}

	[Fact]
	public async Task SaveAsync_RejectsInvalidValuesAndWritesNothing()
	{
		var update = LotterySettingsUpdate.FromDefaults() with { RunDay = "Funday", ReminderTime = "18:00:00" };

		var result = await _service.SaveAsync(update, 123);

		result.Success.ShouldBeFalse();
		result.Changed.ShouldBeFalse();
		result.Settings.ShouldBeNull();
		result.Errors.Select(e => e.Field).ShouldBe(["RunDay", "ReminderTime"]);
		(await _settingsRepository.GetSingletonAsync()).ShouldBeNull();
		(await _historyRepository.GetBySettingsKeyAsync(LotterySettings.SingletonKey)).ShouldBeEmpty();
		_discordMessageService.Verify(x => x.PostInLogChannelAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task SaveAsync_StoresNormalizedValuesAndBumpsTheVersion()
	{
		var result = await _service.SaveAsync(LotterySettingsUpdate.FromDefaults() with
		{
			RunDay = "sunday",
			RunTime = "21:30",
			WinTemplate = ""
		}, 123);

		result.Success.ShouldBeTrue();
		result.Changed.ShouldBeTrue();
		result.Settings!.Version.ShouldBe(1);
		result.Settings.UpdatedByDiscordId.ShouldBe(123ul);
		result.Settings.RunDay.ShouldBe("Sunday");
		result.Settings.WinTemplate.ShouldBe(LotterySettingsDefaults.WinTemplate);

		var stored = await _settingsRepository.GetSingletonAsync();
		stored.ShouldNotBeNull();
		stored!.Version.ShouldBe(1);
		stored.UpdatedByDiscordId.ShouldBe(123ul);
		stored.RunDay.ShouldBe("Sunday");

		// The second save of a different value bumps the version again.
		(await _service.SaveAsync(LotterySettingsUpdate.FromDefaults() with { RunDay = "Sunday", RunTime = "22:00" }, 456))
			.Settings!.Version.ShouldBe(2);
	}

	[Fact]
	public async Task SaveAsync_WritesOneHistoryRowPerChangeWithOldAndNewValues()
	{
		var before = DateTime.UtcNow;

		await _service.SaveAsync(LotterySettingsUpdate.FromDefaults() with
		{
			RunDay = "Sunday",
			RemindersEnabled = true
		}, 123);

		var history = await _historyRepository.GetBySettingsKeyAsync(LotterySettings.SingletonKey);

		history.Count.ShouldBe(1);
		var row = history[0];
		row.Version.ShouldBe(1);
		row.ActorDiscordId.ShouldBe(123ul);
		row.ChangedAt.ShouldBeGreaterThanOrEqualTo(before);
		row.ChangedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);

		// Only the fields that changed are recorded, old and new side by side.
		row.NewValues.Keys.ShouldBe(["RunDay", "RemindersEnabled"]);
		row.OldValues["RunDay"].ShouldBe("Tuesday");
		row.NewValues["RunDay"].ShouldBe("Sunday");
		row.OldValues["RemindersEnabled"].ShouldBe(false);
		row.NewValues["RemindersEnabled"].ShouldBe(true);
	}

	[Fact]
	public async Task SaveAsync_PostsAnAuditNoticeToTheLogChannel()
	{
		string? posted = null;
		_discordMessageService.Setup(x => x.PostInLogChannelAsync(It.IsAny<string>()))
			.Callback<string>(message => posted = message)
			.Returns(Task.CompletedTask);

		await _service.SaveAsync(LotterySettingsUpdate.FromDefaults() with { PauseAutomation = true }, 123);

		posted.ShouldNotBeNull();
		posted.ShouldContain("v1");
		posted.ShouldContain("<@123>");
		posted.ShouldContain("PauseAutomation");
		_discordMessageService.Verify(x => x.PostInLogChannelAsync(It.IsAny<string>()), Times.Once);
	}

	[Fact]
	public async Task SaveAsync_APostFailureDoesNotBreakTheSave()
	{
		_discordMessageService.Setup(x => x.PostInLogChannelAsync(It.IsAny<string>()))
			.ThrowsAsync(new InvalidOperationException("Discord is unreachable"));

		var result = await _service.SaveAsync(LotterySettingsUpdate.FromDefaults() with { RunDay = "Sunday" }, 123);

		result.Success.ShouldBeTrue();
		result.Settings!.RunDay.ShouldBe("Sunday");
		(await _settingsRepository.GetSingletonAsync())!.RunDay.ShouldBe("Sunday");
		(await _historyRepository.GetBySettingsKeyAsync(LotterySettings.SingletonKey)).Count.ShouldBe(1);
		_logger.Verify(
			x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task SaveAsync_WhenNothingChanges_WritesNothing()
	{
		var update = LotterySettingsUpdate.FromDefaults() with { RunDay = "Sunday" };
		await _service.SaveAsync(update, 123);

		// The same values again, and the defaults written with different casing: neither is a change.
		var second = await _service.SaveAsync(update, 456);
		var third = await _service.SaveAsync(update with { RunDay = "sunday" }, 789);

		second.Changed.ShouldBeFalse();
		third.Changed.ShouldBeFalse();
		(await _settingsRepository.GetSingletonAsync())!.Version.ShouldBe(1);
		(await _historyRepository.GetBySettingsKeyAsync(LotterySettings.SingletonKey)).Count.ShouldBe(1);
	}

	[Fact]
	public async Task SettingsHistory_RoundTripsEveryChange()
	{
		await _service.SaveAsync(LotterySettingsUpdate.FromDefaults() with { RunDay = "Sunday" }, 111);
		await _service.SaveAsync(
			LotterySettingsUpdate.FromDefaults() with { RunDay = "Sunday", PauseAutomation = true }, 222);

		var history = await _historyRepository.GetBySettingsKeyAsync(LotterySettings.SingletonKey);

		history.Select(h => h.Version).ShouldBe([1, 2]);
		history.Select(h => h.ActorDiscordId).ShouldBe([111ul, 222ul]);
		history[1].OldValues["PauseAutomation"].ShouldBe(false);
		history[1].NewValues["PauseAutomation"].ShouldBe(true);
		history[1].NewValues.Keys.ShouldBe(["PauseAutomation"]);
	}

	[Fact]
	public async Task SaveAsync_RepairsAnInvalidStoredDocument()
	{
		// The admin page is filled from the repaired values, so saving the form as it stands must fix the stored
		// document — otherwise the degraded start would never clear.
		await _settingsRepository.SaveAsync(new LotterySettings
		{
			RunDay = "Funday",
			RunTime = "25:00",
			RunTimezone = "Mars/Olympus",
			ReminderDay = "Monday",
			ReminderTime = "18:00",
			ReminderTimezone = "Europe/Berlin",
			Version = 3
		});

		var effective = await _service.GetEffectiveAsync();
		var result = await _service.SaveAsync(effective.ToUpdate(), 123);

		result.Success.ShouldBeTrue();
		result.Changed.ShouldBeTrue();
		result.Settings!.Version.ShouldBe(4);

		var stored = await _settingsRepository.GetSingletonAsync();
		stored!.RunDay.ShouldBe("Tuesday");
		stored.RunTime.ShouldBe("20:00");
		stored.RunTimezone.ShouldBe("Europe/Berlin");
		(await _service.GetEffectiveAsync()).InvalidFields.ShouldBeEmpty();

		// The audit row records what was actually in the document, so the repair is traceable.
		var history = await _historyRepository.GetBySettingsKeyAsync(LotterySettings.SingletonKey);
		history.Count.ShouldBe(1);
		history[0].OldValues["RunDay"].ShouldBe("Funday");
		history[0].NewValues["RunDay"].ShouldBe("Tuesday");
	}

	[Fact]
	public async Task SaveAsync_RejectsAnUnknownPlaceholderWithoutWritingAnything()
	{
		var update = LotterySettingsUpdate.FromDefaults() with
		{
			WinTemplate = "the winning number was {number}! Congratulations to {winners}! They take {prize} {jackpot}."
		};

		var result = await _service.SaveAsync(update, 123);

		result.Success.ShouldBeFalse();
		result.Errors.Select(error => error.Field).ShouldBe(["WinTemplate"]);
		result.Errors[0].Message.ShouldContain("{jackpot}");
		(await _settingsRepository.GetSingletonAsync()).ShouldBeNull();
		(await _historyRepository.GetBySettingsKeyAsync(LotterySettings.SingletonKey)).ShouldBeEmpty();
		_discordMessageService.Verify(x => x.PostInLogChannelAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task SaveAsync_RejectsAMissingRequiredPlaceholder()
	{
		var update = LotterySettingsUpdate.FromDefaults() with { NoWinTemplate = "the winning number was {number}!" };

		var result = await _service.SaveAsync(update, 123);

		result.Success.ShouldBeFalse();
		result.Errors.Select(error => error.Field).ShouldBe(["NoWinTemplate"]);
		result.Errors[0].Message.ShouldContain("{pot}");
		(await _settingsRepository.GetSingletonAsync()).ShouldBeNull();
	}

	[Fact]
	public async Task SaveAsync_RejectsATemplateThatWouldProduceAnOverLimitMessage()
	{
		var update = LotterySettingsUpdate.FromDefaults() with
		{
			WinTemplate = LotterySettingsDefaults.WinTemplate + "\n" + new string('x', 1_500)
		};

		var result = await _service.SaveAsync(update, 123);

		result.Success.ShouldBeFalse();
		result.Errors.Select(error => error.Field).ShouldBe(["WinTemplate"]);
		result.Errors[0].Message.ShouldContain("2,000");
		(await _settingsRepository.GetSingletonAsync()).ShouldBeNull();
	}

	[Fact]
	public async Task SaveAsync_ResetToDefaults_RestoresTheTemplatesAndAuditsTheChange()
	{
		var custom = LotterySettingsUpdate.FromDefaults() with
		{
			WinTemplate = "the winning number was {number}! Congratulations to {winners}! They take {prize}.",
			NoWinTemplate = "the winning number was {number}! The jackpot goes up to {pot}!!"
		};
		await _service.SaveAsync(custom, 123);

		var reset = await _service.SaveAsync(
			LotterySettingsValidator.ResetTemplates((await _service.GetEffectiveAsync()).ToUpdate()), 456);

		reset.Success.ShouldBeTrue();
		reset.Changed.ShouldBeTrue();
		reset.Settings!.WinTemplate.ShouldBe(LotterySettingsDefaults.WinTemplate);
		reset.Settings.NoWinTemplate.ShouldBe(LotterySettingsDefaults.NoWinTemplate);

		var stored = await _settingsRepository.GetSingletonAsync();
		stored!.WinTemplate.ShouldBe(LotterySettingsDefaults.WinTemplate);
		stored.NoWinTemplate.ShouldBe(LotterySettingsDefaults.NoWinTemplate);

		var history = await _historyRepository.GetBySettingsKeyAsync(LotterySettings.SingletonKey);
		history.Count.ShouldBe(2);
		history[1].NewValues.Keys.ShouldBe(["WinTemplate", "NoWinTemplate"]);
	}
}
