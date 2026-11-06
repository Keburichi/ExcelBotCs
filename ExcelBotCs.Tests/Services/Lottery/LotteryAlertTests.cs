using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The alert seam (spec §5.10, criterion 16): one message in the log channel (the Discord service falls back to
///     the lottery channel), an admin ping only when the round needs an admin, the round key / failed step / error /
///     attempts / next retry, and the admin page link. An unreachable Discord never throws into the runner.
/// </summary>
public class LotteryAlertTests
{
	private readonly Mock<IDiscordMessageService> _messages = new();
	private readonly Mock<ILogger<LotteryAlertService>> _logger = new();

	private LotteryAlertService BuildService(params ulong[] adminRoleIds)
	{
		var discord = new DiscordBotOptions
		{
			GuildId = 1,
			LotteryChannel = 2,
			AnnouncementChannel = 3,
			EventsChannel = 4,
			UpcomingRosterChannel = 5,
			HallOfClearsChannel = 6,
			LogChannel = 7,
			AdminRoleIds = [.. adminRoleIds]
		};

		var web = new WebBaseUrlOptions { BaseUrl = "https://lottery.test" };

		return new LotteryAlertService(_messages.Object, Options.Create(discord), Options.Create(web),
			_logger.Object);
	}

	[Fact]
	public async Task PublishAsync_ForANeedsAdminFailure_PingsTheAdminRoleAndCarriesEveryField()
	{
		string? posted = null;
		ulong? pinged = null;
		_messages.Setup(x => x.PostAdminAlertAsync(It.IsAny<string>(), It.IsAny<ulong?>()))
			.Callback<string, ulong?>((message, roleId) =>
			{
				posted = message;
				pinged = roleId;
			})
			.Returns(Task.CompletedTask);

		await BuildService(900_000_001UL).PublishAsync(new LotteryAlert(
			"The announcement failed and the round needs an admin",
			"2026-11-10",
			LotteryFailedStep.Announcement,
			"Discord is down",
			5,
			new DateTime(2026, 11, 10, 20, 30, 0, DateTimeKind.Utc)));

		posted.ShouldNotBeNull();
		posted.ShouldContain("2026-11-10");
		posted.ShouldContain("announcement");
		posted.ShouldContain("Discord is down");
		posted.ShouldContain("5");
		posted.ShouldContain("20:30");
		posted.ShouldContain("https://lottery.test/lottery/admin");
		posted.ShouldContain("needs an admin");
		pinged.ShouldBe(900_000_001UL);
	}

	[Fact]
	public async Task PublishAsync_WithoutANeedsAdminState_DoesNotPing()
	{
		ulong? pinged = null;
		_messages.Setup(x => x.PostAdminAlertAsync(It.IsAny<string>(), It.IsAny<ulong?>()))
			.Callback<string, ulong?>((_, roleId) => pinged = roleId)
			.Returns(Task.CompletedTask);

		await BuildService(900_000_001UL).PublishAsync(new LotteryAlert(
			"A retry is scheduled", "2026-11-10", LotteryFailedStep.Announcement, "Discord is down", 2,
			new DateTime(2026, 11, 10, 19, 5, 0, DateTimeKind.Utc), NeedsAdmin: false));

		pinged.ShouldBeNull();
	}

	[Fact]
	public async Task PublishAsync_WithoutAConfiguredAdminRole_DoesNotPing()
	{
		ulong? pinged = 1;
		_messages.Setup(x => x.PostAdminAlertAsync(It.IsAny<string>(), It.IsAny<ulong?>()))
			.Callback<string, ulong?>((_, roleId) => pinged = roleId)
			.Returns(Task.CompletedTask);

		await BuildService().PublishAsync(new LotteryAlert("No admin role", "2026-11-10"));

		pinged.ShouldBeNull();
	}

	[Fact]
	public async Task PublishAsync_WhenDiscordThrows_DoesNotThrow()
	{
		_messages.Setup(x => x.PostAdminAlertAsync(It.IsAny<string>(), It.IsAny<ulong?>()))
			.ThrowsAsync(new InvalidOperationException("Discord is unreachable"));

		await Should.NotThrowAsync(() =>
			BuildService(900_000_001UL).PublishAsync(new LotteryAlert("Failure", "2026-11-10")));

		_logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
			It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
	}

	[Fact]
	public async Task PublishAsync_WithoutANextRetry_SaysTheRoundIsTerminal()
	{
		string? posted = null;
		_messages.Setup(x => x.PostAdminAlertAsync(It.IsAny<string>(), It.IsAny<ulong?>()))
			.Callback<string, ulong?>((message, _) => posted = message)
			.Returns(Task.CompletedTask);

		await BuildService(900_000_001UL).PublishAsync(new LotteryAlert("The draw failed", "2026-11-10",
			LotteryFailedStep.Draw, "Mongo is down", 5));

		posted.ShouldNotBeNull();
		posted.ShouldContain("Next retry: none");
	}
}
