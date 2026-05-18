using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Models.Config;

namespace ExcelBotCs.Extensions;

public static class DiscordSocketExtensions
{
    public static bool IsMember(this SocketGuildUser user, DiscordBotOptions discordBotOptions)
    {
        return user.Roles.Any(role => discordBotOptions.MemberRoleIds.Contains(role.Id));
    }

    public static bool IsMember(this IReadOnlyCollection<SocketRole> roles, DiscordBotOptions discordBotOptions)
    {
        return roles.Any(role => discordBotOptions.MemberRoleIds.Contains(role.Id));
    }

    public static bool IsOfficer(this SocketGuildUser user, DiscordBotOptions discordBotOptions)
    {
        return user.Roles.Any(role => discordBotOptions.AdminRoleIds.Contains(role.Id));
    }

    public static bool IsOfficer(this IReadOnlyCollection<SocketRole> roles, DiscordBotOptions discordBotOptions)
    {
        return roles.Any(role => discordBotOptions.AdminRoleIds.Contains(role.Id));
    }

    public static bool IsFriendOfFc(this SocketGuildUser user, DiscordBotOptions discordBotOptions)
    {
        return user.Roles.Any(role => discordBotOptions.FriendOfFcRoleIds.Contains(role.Id));
    }

    public static bool IsFriendOfFc(this IReadOnlyCollection<SocketRole> roles, DiscordBotOptions discordBotOptions)
    {
        return roles.Any(role => discordBotOptions.FriendOfFcRoleIds.Contains(role.Id));
    }

    public static SocketGuildUser GuildUser(this SocketInteractionContext context)
    {
        return context.Guild.GetUser(context.User.Id);
    }

    public static string PrettyJoin(this List<string> list)
    {
        return list.Count > 1
            ? string.Join(", ", list.Take(list.Count - 1)) + " and " + list.Last()
            : list.FirstOrDefault() ?? string.Empty;
    }

    public static List<GuildEmote> GetEmotes(this SocketGuild guild)
    {
        return guild.Emotes.ToList();
    }
}
