namespace ExcelBotCs.Services.Discord.Interfaces;

public interface IDiscordMessageService
{
    Task PostInLotteryChannel(ulong discordUserId, int number, string currentGuesses);
}