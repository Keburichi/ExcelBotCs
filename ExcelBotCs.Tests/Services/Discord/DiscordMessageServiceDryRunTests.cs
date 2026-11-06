using Discord;
using ExcelBotCs.Discord;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Services.Discord;
using ExcelBotCs.Services.Discord.Interfaces;
using Microsoft.Extensions.Options;
using Moq;

namespace ExcelBotCs.Tests.Services.Discord;

/// <summary>
///     The dry-run delivery rules of spec §5.6 / criterion 20 at the Discord seam: the preview goes to the log channel
///     (falling back to the lottery channel when unset) and it may not ping anyone. The announcement publisher tests
///     prove the <c>[DRY RUN]</c> prefix; this pins the <c>AllowedMentions</c> that makes "mentions suppressed" true.
/// </summary>
public class DiscordMessageServiceDryRunTests
{
    private const ulong LotteryChannelId = 100;
    private const ulong LogChannelId = 700;

    private readonly Mock<IDiscordBotClient> _client = new();
    private readonly Mock<ITextChannel> _logChannel = new();
    private readonly Mock<ITextChannel> _lotteryChannel = new();
    private readonly Mock<IUserMessage> _posted = new();

    private string? _postedText;
    private AllowedMentions? _postedMentions;

    public DiscordMessageServiceDryRunTests()
    {
        _posted.SetupGet(message => message.Id).Returns(42);

        _client.Setup(client => client.GetChannelAsync(LotteryChannelId)).ReturnsAsync(_lotteryChannel.Object);
        _client.Setup(client => client.GetChannelAsync(LogChannelId)).ReturnsAsync(_logChannel.Object);

        foreach (var channel in new[] { _logChannel, _lotteryChannel })
            channel.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Embed>(),
                    It.IsAny<RequestOptions>(), It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(),
                    It.IsAny<MessageComponent>(), It.IsAny<ISticker[]>(), It.IsAny<Embed[]>(),
                    It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()))
                .Callback(new InvocationAction(invocation =>
                {
                    _postedText = (string?)invocation.Arguments[0];
                    _postedMentions = (AllowedMentions?)invocation.Arguments[4];
                }))
                .ReturnsAsync(_posted.Object);
    }

    [Fact]
    public async Task PostDryRunAsync_PostsToTheLogChannelAndSuppressesEveryMention()
    {
        var service = Build(LogChannelId);

        var posted = await service.PostDryRunAsync("[DRY RUN] Hello <@&1>, winner <@2>");

        posted.ShouldNotBeNull();
        _postedText.ShouldBe("[DRY RUN] Hello <@&1>, winner <@2>");

        AssertMentionsSuppressed(_postedMentions);

        _client.Verify(client => client.GetChannelAsync(LogChannelId), Times.AtLeastOnce);
        _client.Verify(client => client.GetChannelAsync(LotteryChannelId), Times.Never);
        _lotteryChannel.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PostDryRunAsync_WithoutALogChannel_FallsBackToTheLotteryChannel()
    {
        // §5.10: LogChannel=0 means the lottery channel is the documented fallback for alerts and dry runs.
        var service = Build(0);

        var posted = await service.PostDryRunAsync("[DRY RUN] fallback");

        posted.ShouldNotBeNull();
        _postedText.ShouldBe("[DRY RUN] fallback");
        AssertMentionsSuppressed(_postedMentions);

        _client.Verify(client => client.GetChannelAsync(LotteryChannelId), Times.AtLeastOnce);
    }

    /// <summary>
    ///     <c>AllowedMentions.None</c> is Discord.Net's "no parse flags and no ids" value: with
    ///     <see cref="AllowedMentionTypes" /> unset only the ids in <c>UserIds</c>/<c>RoleIds</c> may ping, and both
    ///     are empty. That is the mention suppression criterion 20 asks for.
    /// </summary>
    private static void AssertMentionsSuppressed(AllowedMentions? allowed)
    {
        allowed.ShouldNotBeNull();
        (allowed!.AllowedTypes ?? AllowedMentionTypes.None).ShouldBe(AllowedMentionTypes.None);
        allowed.UserIds.ShouldBeEmpty();
        allowed.RoleIds.ShouldBeEmpty();
    }

    private DiscordMessageService Build(ulong logChannelId)
    {
        var options = Options.Create(new DiscordBotOptions
        {
            LotteryChannel = LotteryChannelId,
            LogChannel = logChannelId,
            LotteryMemberRoleId = 555
        });

        return new DiscordMessageService(_client.Object, options, Mock.Of<IDiscordMessageCreator>(),
            Options.Create(new WebBaseUrlOptions { BaseUrl = "https://lottery.test" }));
    }
}
