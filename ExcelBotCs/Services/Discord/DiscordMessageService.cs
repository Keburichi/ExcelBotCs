using Discord;
using ExcelBotCs.Discord;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Services.Discord.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Discord;

public class DiscordMessageService : IDiscordMessageService
{
    private readonly DiscordBotService _discordBotService;
    private readonly IOptions<DiscordBotOptions> _config;

    public DiscordMessageService(DiscordBotService discordBotService, IOptions<DiscordBotOptions> config)
    {
        _discordBotService = discordBotService;
        _config = config;
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
        var channel = await GetTextChannelFromChannelId(_config.Value.EventsChannel);
        if (channel == null)
            return new List<IMessage>();
        
        var discordMessages = await channel.GetMessagesAsync(3, CacheMode.AllowDownload).ToListAsync();
        return discordMessages.SelectMany(x => x).ToList();
    }

    private async Task<IMessageChannel?> GetTextChannelFromChannelId(ulong channelId)
    {
        return await _discordBotService.Client.GetChannelAsync(channelId) as ITextChannel;
    }
}