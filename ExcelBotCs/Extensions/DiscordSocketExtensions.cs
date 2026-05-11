using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Extensions;

public static class DiscordSocketExtensions
{
    public enum MessageResponse
    {
        NotValidUrl,
        NotFoundUrl,
        Success
    }

    public static SocketGuild ExcelGuild(this DiscordSocketClient client)
    {
        return client.Guilds.First(x => x.Id == Constants.GuildId);
    }

    public static bool IsMember(this IReadOnlyCollection<SocketRole> roles)
    {
        return roles.Any(role => role.Id == Constants.FcRoleId);
    }

    // public static bool IsOfficer(this IReadOnlyCollection<SocketRole> roles) =>
    // 	roles.Any(role => role.Id == Constants.OfficerRoleId);

    public static bool IsOfficer(this IReadOnlyCollection<SocketRole> roles)
    {
        return true;
    }

    public static bool IsFriendOfFc(this IReadOnlyCollection<SocketRole> roles)
    {
        return roles.Any(role => role.Id == Constants.FcFriendRoleId);
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

    public static async Task<IMessageResponse> GetMessageFromUrl(this DiscordSocketClient client, string postUrl)
    {
        var regex = new Regex("discord.com/channels/(?<guildId>\\d+)/(?<channelId>\\d+)/(?<messageId>\\d+)");
        var match = regex.Matches(postUrl).FirstOrDefault();

        if (match is not { Success: true })
            return new NotFoundUrlMessageResponse();

        var guildId = ulong.Parse(match.Groups["guildId"].Value);
        var channelId = ulong.Parse(match.Groups["channelId"].Value);
        var messageId = ulong.Parse(match.Groups["messageId"].Value);

        return client.GetGuild(guildId).GetChannel(channelId) is not ITextChannel channel
            ? new NotValidUrlMessageResponse()
            : new SuccessMessageResponse(await channel.GetMessageAsync(messageId));
    }

    public interface IMessageResponse
    {
    }

    public record NotValidUrlMessageResponse : IMessageResponse;

    public record NotFoundUrlMessageResponse : IMessageResponse;

    public record SuccessMessageResponse(IMessage Message) : IMessageResponse;

    public static List<GuildEmote> GetEmotes(this SocketGuild guild)
    {
        return guild.Emotes.ToList();
    }

    public static List<GuildEmote> GetEmotes(this DiscordSocketClient discordSocketClient)
    {
        return discordSocketClient.Guilds.SelectMany(x => x.Emotes).ToList();
    }

    public static Emote? GetTankEmote(this DiscordSocketClient client)
    {
        return client.GetEmotes().FirstOrDefault(x => x.Id == 1380979172423499846);
    }

    public static Emote? GetHealerEmote(this DiscordSocketClient client)
    {
        return client.GetEmotes().FirstOrDefault(x => x.Id == 1380979170787721368);
    }

    public static Emote? GetMeleeEmote(this DiscordSocketClient client)
    {
        return client.GetEmotes().FirstOrDefault(x => x.Id == 873621778214318091);
    }

    public static Emote? GetRangedEmote(this DiscordSocketClient client)
    {
        return client.GetEmotes().FirstOrDefault(x => x.Id == 873621778453368895);
    }

    public static Emote? GetCasterEmote(this DiscordSocketClient client)
    {
        return client.GetEmotes().FirstOrDefault(x => x.Id == 873621778566635540);
    }

    public static Emote? GetEmoteById(this DiscordSocketClient client, ulong id)
    {
        return client.GetEmotes().FirstOrDefault(x => x.Id == id);
    }

    public static Emote? GetEmoteByRole(this DiscordSocketClient client, Role role)
    {
        return role switch
        {
            Role.Tank => client.GetTankEmote(),
            Role.Healer => client.GetHealerEmote(),
            Role.Melee => client.GetMeleeEmote(),
            Role.Caster => client.GetCasterEmote(),
            Role.Ranged => client.GetRangedEmote(),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }
}