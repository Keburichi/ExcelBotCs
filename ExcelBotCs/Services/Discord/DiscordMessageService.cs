using Discord.Interactions;
using ExcelBotCs.Discord;
using ExcelBotCs.Services.Discord.Interfaces;

namespace ExcelBotCs.Services.Discord;

public class DiscordMessageService : InteractionModuleBase<SocketInteractionContext>, IDiscordMessageService
{
    private readonly DiscordBotService _discordBotService;

    public DiscordMessageService(DiscordBotService  discordBotService)
    {
        _discordBotService = discordBotService;
    }
    
    public Task PostInLotteryChannel(ulong discordUserId, int number, string currentGuesses)
    {
        Context.Guild.GetTextChannel(0);
        return Task.CompletedTask;
    }
}