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

    public async Task PostInLotteryChannelAsync(string message)
    {
        if (await _discordBotService.Client.GetChannelAsync(_config.Value.LotteryChannel) is IMessageChannel
            textChannel)
            await textChannel.SendMessageAsync(message);
    }
    
    public async Task<List<IMessage>> GetAnnouncementChannelMessagesAsync()
    {
        if (await _discordBotService.Client.GetChannelAsync(_config.Value.AnnouncementChannel) is IMessageChannel
            textChannel)
        {
            var discordMessages = await textChannel.GetMessagesAsync(3, CacheMode.AllowDownload).ToListAsync();
            return discordMessages.SelectMany(x => x).ToList();
        }

        return new List<IMessage>();
    }
}