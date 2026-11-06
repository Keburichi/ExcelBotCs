using Discord;
using ExcelBotCs.Models.Database.Events;
using ExcelBotCs.Services.Discord.Records;

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

    /// <summary>
    ///     Posts the real weekly announcement to <c>DiscordBot:LotteryChannel</c>, role ping included, and returns the
    ///     sent message so the round can store <c>AnnouncementMessageId</c> (spec §5.6, §5.10). <c>null</c> means the
    ///     channel could not be resolved and nothing was posted.
    /// </summary>
    Task<IUserMessage?> PostLotteryAnnouncementAsync(string message);

    /// <summary>
    ///     Posts a dry-run announcement (already prefixed with <c>[DRY RUN]</c>) to the log channel, falling back to
    ///     the lottery channel, with every mention suppressed (spec §5.6, criterion 20).
    /// </summary>
    Task<IUserMessage?> PostDryRunAsync(string message);

    /// <summary>
    ///     Posts an admin alert to the log channel (fallback lottery channel). Only
    ///     <paramref name="adminRoleId" /> is mentionable, and no ping at all when it is <c>null</c> (spec §5.10).
    /// </summary>
    Task PostAdminAlertAsync(string message, ulong? adminRoleId);

    /// <summary>
    ///     The most recent messages of <c>DiscordBot:LotteryChannel</c>, newest first — the search window for the
    ///     posting reconciliation (spec §5.10).
    /// </summary>
    Task<IReadOnlyList<DiscordChannelMessage>> GetRecentLotteryChannelMessagesAsync(int limit = 50);

    Task<List<IMessage>> GetAnnouncementChannelMessagesAsync();
    Task<ITextChannel?> GetLogChannelAsync();
}
