using Discord;
using Discord.Interactions;

namespace ExcelBotCs.Services.Discord.Interfaces;

public interface IDiscordMessageService
{
    Task PostInAnnouncementChannelAsync(string message);
    Task PostInEventChannelAsync(string message);
    Task PostInUpcomingRosterChannelAsync(string message);
    Task PostInLotteryChannelAsync(string message);
    Task<List<IMessage>> GetAnnouncementChannelMessagesAsync();
}