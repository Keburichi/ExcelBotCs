using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace ExcelBotCs;

public static class DiscordSocketExtensions
{
	public static SocketGuild ExcelGuild(this DiscordSocketClient client) => client.Guilds.First(x => x.Id == Constants.GuildId);
	public static bool IsMember(this IReadOnlyCollection<SocketRole> roles) => roles.Any(role => role.Id == Constants.FcRoleId);

	// public static bool IsOfficer(this IReadOnlyCollection<SocketRole> roles) =>
	// 	roles.Any(role => role.Id == Constants.OfficerRoleId);
	
	public static bool IsOfficer(this IReadOnlyCollection<SocketRole> roles) =>
		true;

	public static bool IsFriendOfFc(this IReadOnlyCollection<SocketRole> roles) =>
		roles.Any(role => role.Id == Constants.FcFriendRoleId);

	public static SocketGuildUser GuildUser(this SocketInteractionContext context) =>
		context.Guild.GetUser(context.User.Id);

	public static string PrettyJoin(this List<string> list) => list.Count > 1
		? string.Join(", ", list.Take(list.Count - 1)) + " and " + list.Last()
		: list.FirstOrDefault() ?? string.Empty;

	public enum MessageResponse
	{
		NotValidUrl,
		NotFoundUrl,
		Success
	}

	public interface IMessageResponse
	{
	}

	public record NotValidUrlMessageResponse() : IMessageResponse;
	public record NotFoundUrlMessageResponse() : IMessageResponse;
	public record SuccessMessageResponse(IMessage Message) : IMessageResponse;

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
}