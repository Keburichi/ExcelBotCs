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
    Task<IUserMessage?> PostInUpcomingRosterChannelAsync(string message);
    Task DeleteUpcomingRosterMessageAsync(string messageId);
    Task PostInLotteryChannelAsync(string message);

    /// <summary>
    ///     Posts to <c>DiscordBot:LogChannel</c>, falling back to the lottery channel when it is unset (0) or
    ///     unreachable. Alerts and the lottery settings audit notices use this (spec §7, §5.10).
    /// </summary>
    Task PostInLogChannelAsync(string message);

    Task<List<IMessage>> GetAnnouncementChannelMessagesAsync();
    Task<ITextChannel?> GetLogChannelAsync();
}
