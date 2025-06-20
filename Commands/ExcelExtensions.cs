using Discord.Interactions;
using Discord.WebSocket;

namespace ExcelBotCs.Commands;

public static class ExcelExtensions
{
	public const ulong GuildId = 797520687941943336;

	private const ulong OfficerRoleId = 797521763214557204;
	private const ulong FcRoleId = 797810200269684787;
	private const ulong FcFriendRoleId = 810208611472637973;

	public const ulong LotteryChannel = 1385572037245796483;

	public static SocketGuild ExcelGuild(this DiscordSocketClient client) => client.Guilds.First(x => x.Id == GuildId);
	public static bool IsMember(this IReadOnlyCollection<SocketRole> roles) => roles.Any(role => role.Id == FcRoleId);

	public static bool IsOfficer(this IReadOnlyCollection<SocketRole> roles) =>
		roles.Any(role => role.Id == OfficerRoleId);

	public static bool IsFriendOfFc(this IReadOnlyCollection<SocketRole> roles) =>
		roles.Any(role => role.Id == FcFriendRoleId);

	public static SocketGuildUser GuildUser(this SocketInteractionContext context) =>
		context.Guild.GetUser(context.User.Id);

	public static string PrettyJoin(this List<string> list) => list.Count > 1
		? string.Join(", ", list.Take(list.Count - 1)) + " and " + list.Last()
		: list.FirstOrDefault() ?? string.Empty;
}

/*public class StreamAnnouncements
{
	private readonly DiscordBotService _discord;

	public StreamAnnouncements(DiscordBotService discord)
	{
		_discord = discord;
	}

	public async Task CollectStreams()
	{
		var guild = _discord.Client.ExcelGuild();

		await guild.DownloadUsersAsync();

		var users = guild
			.Users
			.Where(user => user.Roles.IsMember())
			.Select(user => (user, user.Activities));

		foreach (var (user, activities) in users)
		{
			if (activities.Count == 0)
				continue;

			foreach (var activity in activities)
			{
				Console.WriteLine(activity.GetType().Name);
				Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(activity));
			}
		}
	}
}*/