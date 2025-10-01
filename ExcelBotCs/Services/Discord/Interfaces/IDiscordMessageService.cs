using Discord;
using Discord.Interactions;

namespace ExcelBotCs.Services.Discord.Interfaces;

public interface IDiscordMessageService
{
    Task PostInLotteryChannelAsync(string message);
    Task<List<IMessage>> GetAnnouncementChannelMessagesAsync();
}