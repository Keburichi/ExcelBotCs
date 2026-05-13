using Discord;
using Discord.WebSocket;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Services.Discord.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Discord;

public class DiscordMessageService : IDiscordMessageService
{
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly IOptions<DiscordBotOptions> _config;
    private readonly IComponentCreator _componentCreator;

    public DiscordMessageService(DiscordSocketClient discordSocketClient, IOptions<DiscordBotOptions> config,
        IComponentCreator componentCreator)
    {
        _discordSocketClient = discordSocketClient;
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

        var message = await channel.GetMessageAsync(ulong.Parse(fcEvent.DiscordMessageId)) as IUserMessage;
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

    public async Task PostInUpcomingRosterChannelAsync(string message)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.UpcomingRosterChannel);
        if (channel == null)
            return;

        await channel.SendMessageAsync(message);
    }

    public async Task PostInLotteryChannelAsync(string message)
    {
        var channel = await GetTextChannelFromChannelId(_config.Value.LotteryChannel);
        if (channel == null)
            return;

        await channel.SendMessageAsync(message);
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
        return await _discordSocketClient.GetChannelAsync(channelId) as ITextChannel;
    }
}