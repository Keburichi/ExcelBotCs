using Discord;
using ExcelBotCs.Models.Database.Events;

namespace ExcelBotCs.Services.Discord.Interfaces;

public interface IDiscordMessageService
{
    Task PostInAnnouncementChannelAsync(string message);
    Task PostInEventChannelAsync(string message);
    Task<IUserMessage?> PostEventSignupAsync(Event fcEvent);
    Task UpdateSignupMessage(Event fcEvent);
    Task DeleteEventMessageAsync(string discordMessageId);
    Task<string> GetEventSignupMessageUrl(string discordMessageId);
    Task PostInUpcomingRosterChannelAsync(string message);
    Task PostInLotteryChannelAsync(string message);
    Task<List<IMessage>> GetAnnouncementChannelMessagesAsync();
    Task<ITextChannel?> GetLogChannelAsync();
}