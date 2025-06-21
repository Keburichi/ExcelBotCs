using Discord.Interactions;
using Discord.WebSocket;

namespace ExcelBotCs;

public static class Extensions
{
	public static SocketGuild ExcelGuild(this DiscordSocketClient client) => client.Guilds.First(x => x.Id == Constants.GuildId);
	public static bool IsMember(this IReadOnlyCollection<SocketRole> roles) => roles.Any(role => role.Id == Constants.FcRoleId);

	public static bool IsOfficer(this IReadOnlyCollection<SocketRole> roles) =>
		roles.Any(role => role.Id == Constants.OfficerRoleId);

	public static bool IsFriendOfFc(this IReadOnlyCollection<SocketRole> roles) =>
		roles.Any(role => role.Id == Constants.FcFriendRoleId);

	public static SocketGuildUser GuildUser(this SocketInteractionContext context) =>
		context.Guild.GetUser(context.User.Id);

	public static string PrettyJoin(this List<string> list) => list.Count > 1
		? string.Join(", ", list.Take(list.Count - 1)) + " and " + list.Last()
		: list.FirstOrDefault() ?? string.Empty;
}