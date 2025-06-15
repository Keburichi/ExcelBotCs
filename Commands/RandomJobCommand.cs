using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Discord;

namespace ExcelBotCs.Commands;

public static class ExcelExtensions
{
	private const ulong OfficerRoleId = 797521763214557204;
	private const ulong FcRoleId = 797810200269684787;
	private const ulong FcFriendRoleId = 810208611472637973;

	public static ulong GuildId(this DiscordSocketClient client) => 797520687941943336;
	public static SocketGuild ExcelGuild(this DiscordSocketClient client) => client.Guilds.First(x => x.Id == client.GuildId());
	public static bool IsMember(this IReadOnlyCollection<SocketRole> roles) => roles.Any(role => role.Id == FcRoleId);
	public static bool IsOfficer(this IReadOnlyCollection<SocketRole> roles) => roles.Any(role => role.Id == OfficerRoleId);
	public static bool IsFriendOfFc(this IReadOnlyCollection<SocketRole> roles) => roles.Any(role => role.Id == FcFriendRoleId);
}

public class StreamAnnouncements
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
}

public class RandomJobCommand : InteractionModuleBase<SocketInteractionContext>
{
	private readonly DiscordBotService _discord;
	private readonly StreamAnnouncements _streamAnnouncements;

	public RandomJobCommand(DiscordBotService discord)
	{
		_discord = discord;
		discord.Client.Ready += ClientOnReady;
		_streamAnnouncements = new StreamAnnouncements(discord);
	}

	private async Task ClientOnReady()
	{
		await _discord.Interaction.RegisterCommandsToGuildAsync(_discord.Client.GuildId());
	}

	[SlashCommand("whoisingame", "Tells you who is currently in-game")]
	public async Task WhoIsInGame()
	{
		var guild = _discord.Client.ExcelGuild();

		await guild.DownloadUsersAsync();

		var users = guild
			.Users
			.Where(user => user.Activities.Any(activity => activity.Name == "FINAL FANTASY XIV Online"))
			.ToList();

		if (users.Count == 0)
		{
			await RespondAsync("Nobody is currently logged in to FFXIV");
			return;
		}

		await RespondAsync($"{users.Count} user{(users.Count == 1 ? " is" : "s are")} currently logged in to FFXIV: {string.Join(", ", users.Select(user => $"<@{user.Id}>"))}");
	}

	[SlashCommand("test", "Does nothing but reply \"Hello!\" (or whatever Zahrymm is currently testing in the moment by forcing it to run)")]
	public async Task TestCommand()
	{
		await WhoIsInGame();

		//await RespondAsync("Hello!");
		//await _streamAnnouncements.CollectStreams();
	}

	private async Task RandomJob(params string[] jobs) => await RespondAsync($"I picked {jobs.PickRandom()} for you!");

	[SlashCommand("randommelee", "Gives you a random Melee DPS job")]
	public async Task RandomMelee() => await RandomJob("Dragoon", "Monk", "Ninja", "Samurai", "Reaper", "Viper");

	[SlashCommand("randomcaster", "Gives you a random Magical Ranged DPS job")]
	public async Task RandomCaster() => await RandomJob("Black Mage", "Summoner", "Red Mage", "Pictomancer");

	[SlashCommand("randomranged", "Gives you a random Physical Ranged DPS job")]
	public async Task RandomRanged() => await RandomJob("Bard", "Machinist", "Dancer");

	[SlashCommand("randomhealer", "Gives you a random Healer job")]
	public async Task RandomHealer() => await RandomJob("White Mage", "Scholar", "Astrologian", "Sage");

	[SlashCommand("randomtank", "Gives you a random Tank job")]
	public async Task RandomTank() => await RandomJob("Paladin", "Warrior", "Dark Knight", "Gunbreaker");

	[SlashCommand("randomjob", "Gives you a random job")]
	public async Task RandomJob() => await RandomJob("Dragoon", "Monk", "Ninja", "Samurai", "Reaper", "Viper",
		"Black Mage", "Summoner", "Red Mage", "Pictomancer",
		"Bard", "Machinist", "Dancer",
		"White Mage", "Scholar", "Astrologian", "Sage",
		"Paladin", "Warrior", "Dark Knight", "Gunbreaker");


	[SlashCommand("rdmmanaficoncd", "Tells you whether you should use Manafication on cooldown based on your expected kill time")]
	public async Task ShouldIManaficRush(int minutes, int seconds)
	{
		var total = (minutes * 60) + seconds;
		var emboldenUses = (int)Math.Floor(total / 120f);
		var manaficUses = (int)Math.Floor(total / 110f);

		await RespondAsync(emboldenUses != manaficUses
			? "You should use Manafication on cooldown to gain the maximum amount of uses."
			: "You should align Manafication with Embolden. Use it 1 or 5 seconds before Embolden to fit 6 finishers into buffs.");
	}
}