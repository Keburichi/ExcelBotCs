using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     Bad or missing lottery configuration must start degraded — the scheduler off and exactly one alert — instead
///     of crashing the host (spec §5.10, §7). The alert is the error log line the existing Discord logger already
///     mirrors into the log channel, so these tests count error log calls rather than inventing a second channel.
/// </summary>
public class LotteryStartupCheckTests
{
	private readonly Mock<ILogger<LotteryStartupCheck>> _logger = new();
	private readonly Mock<ILotterySettingsService> _settingsService = new();
	private readonly List<string> _errorMessages = [];

	private DiscordBotOptions _discord = null!;
	private LotteryOptions _lottery = null!;
	private string _webBaseUrl = string.Empty;

	public LotteryStartupCheckTests()
	{
		_logger.Setup(x => x.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception?>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
			.Callback(new InvocationAction(invocation =>
				_errorMessages.Add(invocation.Arguments[2].ToString()!)));

		SetHealthyDeploymentConfig();
	}

	private LotteryStartupCheck CreateCheck()
	{
		return new LotteryStartupCheck(
			Options.Create(_discord),
			Options.Create(_lottery),
			Options.Create(new WebBaseUrlOptions { BaseUrl = _webBaseUrl }),
			_settingsService.Object,
			_logger.Object);
	}

	private void SetHealthyDeploymentConfig()
	{
		_discord = new DiscordBotOptions
		{
			Enabled = true,
			GuildId = 1,
			LotteryChannel = 2,
			RiddlesChannel = 3,
			LotteryMemberRoleId = 4,
			LotteryCustomEmojiId = 0
		};
		_lottery = new LotteryOptions();
		_webBaseUrl = "https://excel.example.com";

		_settingsService.Setup(x => x.GetEffectiveAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(StoredSettings([]));
	}

	private static LotteryEffectiveSettings StoredSettings(IReadOnlyList<string> invalidFields)
	{
		return new LotteryEffectiveSettings(
			LotterySettingsDefaults.RunDay, LotterySettingsDefaults.RunTime, LotterySettingsDefaults.RunTimezone,
			LotterySettingsDefaults.ReminderDay, LotterySettingsDefaults.ReminderTime,
			LotterySettingsDefaults.ReminderTimezone, LotterySettingsDefaults.RemindersEnabled,
			LotterySettingsDefaults.PauseAutomation, LotterySettingsDefaults.WinTemplate,
			LotterySettingsDefaults.NoWinTemplate, 1, null, false, true, LotterySettingsDefaults.BasePot,
			LotterySettingsDefaults.WeeklyIncrement, invalidFields);
	}

	[Fact]
	public async Task CheckAsync_WithCompleteConfiguration_IsHealthy()
	{
		var result = await CreateCheck().CheckAsync();

		result.IsDegraded.ShouldBeFalse();
		result.SchedulerEnabled.ShouldBeTrue();
		result.Problems.ShouldBeEmpty();
		_errorMessages.ShouldBeEmpty();
	}

	[Fact]
	public async Task CheckAsync_WithMissingDeploymentConfig_StartsDegradedAndAlertsOnce()
	{
		_discord.GuildId = 0;
		_discord.LotteryChannel = 0;
		_discord.LotteryMemberRoleId = 0;
		_discord.RiddlesChannel = 0;
		_lottery.Automation.BasePot = 0;
		_lottery.Automation.WeeklyIncrement = -1;
		_webBaseUrl = string.Empty;

		var result = await CreateCheck().CheckAsync();

		result.IsDegraded.ShouldBeTrue();
		result.SchedulerEnabled.ShouldBeFalse();
		result.Problems.Count.ShouldBe(7);
		var alert = _errorMessages.ShouldHaveSingleItem();
		alert.ShouldContain("GuildId");
		alert.ShouldContain("LotteryChannel");
		alert.ShouldContain("LotteryMemberRoleId");
		alert.ShouldContain("RiddlesChannel");
		alert.ShouldContain("BasePot");
		alert.ShouldContain("WeeklyIncrement");
		alert.ShouldContain("WebBaseUrl");
	}

	[Fact]
	public async Task CheckAsync_WithInvalidStoredSettings_StartsDegraded()
	{
		_settingsService.Setup(x => x.GetEffectiveAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(StoredSettings(["RunDay", "RunTimezone"]));

		var result = await CreateCheck().CheckAsync();

		result.IsDegraded.ShouldBeTrue();
		result.Problems.Count.ShouldBe(2);
		result.Problems[0].ShouldContain("RunDay");
		_errorMessages.Count.ShouldBe(1);
	}

	[Fact]
	public async Task CheckAsync_WhenTheSettingsCannotBeRead_DoesNotThrow()
	{
		_settingsService.Setup(x => x.GetEffectiveAsync(It.IsAny<CancellationToken>()))
			.ThrowsAsync(new TimeoutException("MongoDB is unreachable"));

		var result = await CreateCheck().CheckAsync();

		result.IsDegraded.ShouldBeTrue();
		result.Problems.ShouldHaveSingleItem().ShouldContain("MongoDB is unreachable");
		_errorMessages.Count.ShouldBe(1);
	}

	[Fact]
	public async Task CheckAsync_WithAMalformedWebBaseUrl_StartsDegraded()
	{
		_webBaseUrl = "not a url";

		var result = await CreateCheck().CheckAsync();

		result.IsDegraded.ShouldBeTrue();
		result.Problems.ShouldHaveSingleItem().ShouldContain("WebBaseUrl");
	}

	[Fact]
	public async Task CheckAsync_DoesNotTreatTheMissingEmojiIdAsDegraded()
	{
		// §5.6: without an emoji id the template falls back to the literal text, so it is not a deployment fault.
		_discord.LotteryCustomEmojiId = 0;

		var result = await CreateCheck().CheckAsync();

		result.IsDegraded.ShouldBeFalse();
	}

	[Fact]
	public async Task CheckAsync_WhenTheBotIsDisabled_DoesNotAlert()
	{
		// A botless checkout (parallel worktree, CI) has no lottery automation to degrade; alerting there is noise.
		_discord.Enabled = false;
		_discord.GuildId = 0;
		_webBaseUrl = string.Empty;

		var result = await CreateCheck().CheckAsync();

		result.IsDegraded.ShouldBeFalse();
		_errorMessages.ShouldBeEmpty();
	}
}
