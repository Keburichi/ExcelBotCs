using Discord.Interactions;
using ExcelBotCs.Discord;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Services.Discord.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Discord;

public class DiscordMessageService : InteractionModuleBase<SocketInteractionContext>, IDiscordMessageService
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
        var lotteryChannel = Context.Guild.GetTextChannel(_config.Value.LotteryChannel);
        if(lotteryChannel != null)
            await lotteryChannel.SendMessageAsync(message);
    }
}