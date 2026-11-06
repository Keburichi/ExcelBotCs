using Discord;
using ExcelBotCs.Discord;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Services.Discord;
using ExcelBotCs.Services.Discord.Interfaces;
using Microsoft.Extensions.Options;
using Moq;

namespace ExcelBotCs.Tests.Services.Discord;

/// <summary>
///     The lottery audit notice and the failure alerts are posted through <c>PostInLogChannelAsync</c>, which must
///     use <c>DiscordBot:LogChannel</c> and fall back to the lottery channel when it is unset (spec §7, §5.10).
/// </summary>
public class DiscordLogChannelRoutingTests
{
	private const ulong LotteryChannel = 444444444444444444;
	private const ulong LogChannel = 777777777777777777;

	private readonly Mock<IDiscordBotClient> _client = new();

	private DiscordMessageService CreateService(ulong logChannel)
	{
		_client.Setup(x => x.GetChannelAsync(It.IsAny<ulong>()))
			.ReturnsAsync((IChannel?)null);

		return new DiscordMessageService(_client.Object,
			Options.Create(new DiscordBotOptions
			{
				LotteryChannel = LotteryChannel,
				LogChannel = logChannel
			}),
			new Mock<IDiscordMessageCreator>().Object,
			Options.Create(new WebBaseUrlOptions()));
	}

	[Fact]
	public async Task PostInLogChannelAsync_WithoutAConfiguredLogChannel_PostsToTheLotteryChannel()
	{
		await CreateService(0).PostInLogChannelAsync("Lottery settings v1 updated");

		_client.Verify(x => x.GetChannelAsync(LotteryChannel), Times.Once);
		_client.Verify(x => x.GetChannelAsync(LogChannel), Times.Never);
	}

	[Fact]
	public async Task PostInLogChannelAsync_WithAConfiguredLogChannel_PostsThere()
	{
		await CreateService(LogChannel).PostInLogChannelAsync("Lottery settings v1 updated");

		_client.Verify(x => x.GetChannelAsync(LogChannel), Times.Once);
		_client.Verify(x => x.GetChannelAsync(LotteryChannel), Times.Never);
	}
}
