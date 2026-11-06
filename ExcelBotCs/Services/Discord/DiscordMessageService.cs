using Discord;
using ExcelBotCs.Discord;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Services.Discord.Interfaces;
using ExcelBotCs.Services.Discord.Records;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Discord;

public class DiscordMessageService : IDiscordMessageService
{
    private readonly IDiscordBotClient _discordClient;
    private readonly IOptions<DiscordBotOptions> _config;
    private readonly IDiscordMessageCreator _componentCreator;

    public DiscordMessageService(IDiscordBotClient discordClient, IOptions<DiscordBotOptions> config,
        IDiscordMessageCreator componentCreator)
    {
        _discordClient = discordClient;
        _config = config;
        _componentCreator = componentCreator;
    }

    public async Task PostInAnnouncementChannelAsync(string message)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.AnnouncementChannel);
        if (channel == null)
            return;

        await channel.SendMessageAsync(message);
    }

    public async Task PostInEventChannelAsync(string message)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.EventsChannel);
        if (channel == null)
            return;

        await channel.SendMessageAsync(message);
    }

    public async Task<IUserMessage?> PostEventSignupAsync(Event fcEvent)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.EventsChannel);
        if (channel == null)
            return null;

        var component = await _componentCreator.CreateSignupComponents(fcEvent);
        return await channel.SendMessageAsync(components: component.Build());
    }

    public async Task UpdateSignupMessage(Event fcEvent)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.EventsChannel);
        if (channel == null)
            return;

        var message = await channel.GetMessageAsync(ulong.Parse(fcEvent.SignupPostId)) as IUserMessage;
        if (message == null)
            return;

        var component = await _componentCreator.CreateSignupComponents(fcEvent);
        await message.ModifyAsync(m => m.Components = component.Build());
    }

    public async Task DeleteEventMessageAsync(string discordMessageId)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.EventsChannel);
        if (channel == null)
            return;

        var message = await channel.GetMessageAsync(ulong.Parse(discordMessageId));
        if (message == null)
            return;

        await message.DeleteAsync();
    }

    public async Task<string> GetEventSignupMessageUrl(string discordMessageId)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.EventsChannel);
        if (channel == null)
            return string.Empty;

        var message = await channel.GetMessageAsync(ulong.Parse(discordMessageId));
        if (message == null)
            return string.Empty;

        return message.GetJumpUrl();
    }

    public async Task<IUserMessage?> PostInUpcomingRosterChannelAsync(string message)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.UpcomingRosterChannel);
        if (channel == null)
            return null;

        return await channel.SendMessageAsync(message);
    }

    public async Task DeleteUpcomingRosterMessageAsync(string messageId)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.UpcomingRosterChannel);
        if (channel == null)
            return;

        var message = await channel.GetMessageAsync(ulong.Parse(messageId));
        if (message == null)
            return;

        await message.DeleteAsync();
    }

    public async Task PostInLotteryChannelAsync(string message)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.LotteryChannel);
        if (channel == null)
            return;

        await channel.SendMessageAsync(message);
    }

    public async Task PostInLogChannelAsync(string message)
    {
        // Spec §7: alerts and audit notices belong in DiscordBot:LogChannel; when it is unset (0) — the shipped
        // appsettings value — the lottery channel is the documented fallback (§5.10).
        var channel = await GetLogOrLotteryChannelAsync();
        if (channel == null)
            return;

        await channel.SendMessageAsync(message);
    }

    public async Task<IUserMessage?> PostLotteryAnnouncementAsync(string message)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.LotteryChannel);
        if (channel == null)
            return null;

        // The announcement must ping the configured lottery member role (spec §5.6); no @everyone and no stray
        // mentions, so the allowed set is explicit.
        return await channel.SendMessageAsync(message, allowedMentions: new AllowedMentions
        {
            AllowedTypes = AllowedMentionTypes.Roles | AllowedMentionTypes.Users,
            RoleIds = _config.Value.LotteryMemberRoleId != 0 ? [_config.Value.LotteryMemberRoleId] : null
        });
    }

    public async Task<IUserMessage?> PostDryRunAsync(string message)
    {
        var channel = await GetLogOrLotteryChannelAsync();
        if (channel == null)
            return null;

        // A dry run is a review copy: it must never ping the member role or a member (criterion 20).
        return await channel.SendMessageAsync(message, allowedMentions: AllowedMentions.None);
    }

    public async Task PostAdminAlertAsync(string message, ulong? adminRoleId)
    {
        var channel = await GetLogOrLotteryChannelAsync();
        if (channel == null)
            return;

        var allowedMentions = adminRoleId is { } roleId && roleId != 0
            ? new AllowedMentions { AllowedTypes = AllowedMentionTypes.Roles, RoleIds = [roleId] }
            : AllowedMentions.None;

        await channel.SendMessageAsync(message, allowedMentions: allowedMentions);
    }

    public async Task<IReadOnlyList<DiscordChannelMessage>> GetRecentLotteryChannelMessagesAsync(int limit = 50)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.LotteryChannel);
        if (channel == null)
            return [];

        var batches = await channel.GetMessagesAsync(limit, CacheMode.AllowDownload).ToListAsync();

        return batches
            .SelectMany(batch => batch)
            .Select(message => new DiscordChannelMessage(
                message.Id,
                message.Author.Id,
                message.Author.IsBot,
                message.Content,
                message.CreatedAt))
            .ToList();
    }

    private async Task<ITextChannel?> GetLogOrLotteryChannelAsync()
    {
        var channelId = _config.Value.LogChannel != 0 ? _config.Value.LogChannel : _config.Value.LotteryChannel;
        return await GetTextChannelFromChannelId(channelId) as ITextChannel;
    }

    public async Task<List<IMessage>> GetAnnouncementChannelMessagesAsync()
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.AnnouncementChannel);
        if (channel == null)
            return new List<IMessage>();

        var discordMessages = await channel.GetMessagesAsync(3, CacheMode.AllowDownload).ToListAsync();
        return discordMessages.SelectMany(x => x).ToList();
    }

    public async Task<ITextChannel?> GetLogChannelAsync()
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.LogChannel);
        if (channel == null)
            return null;

        return channel as ITextChannel;
    }

    private async Task<IMessageChannel?> GetTextChannelFromChannelId(ulong channelId)
    {
        return await _discordClient.GetChannelAsync(channelId) as ITextChannel;
    }
}