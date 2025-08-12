using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using ExcelBotCs.Discord;

namespace ExcelBotCs.Modules.Misc;

[Group("mentor", "Mentor commands")]
public class MentorInteraction : InteractionModuleBase<SocketInteractionContext>
{
	private const ulong MentorChannel = 1404807083332997274;
	private const ulong MentorMessage = 1404822059950801046;

	private const ulong TankSpecialistRole = 1404817680984178709;
	private const ulong HealerSpecialistRole = 1404817727612387471;
	private const ulong MeleeSpecialistRole = 1404817754401275925;
	private const ulong CasterSpecialistRole = 1404817824827838534;
	private const ulong RangedPhysSpecialistRole = 1404817896554631301;

	private const string TankRoleEmote = "<:RoleTank:1380979172423499846>";
	private const string HealerRoleEmote = "<:RoleHealer:1380979170787721368>";
	private const string MeleeRoleEmote = "<:RoleMelee:873621778214318091>";
	private const string CasterRoleEmote = "<:RoleCaster:873621778566635540>";
	private const string RangedRoleEmote = "<:RoleRanged:873621778453368895>";

	private async Task ToggleMentorRole(string name, ulong role)
	{
		var user = Context.GuildUser();

		if (user.Roles.Any(r => r.Id == role))
		{
			await user.RemoveRoleAsync(role);
			await RespondAsync($"{name} specialist role was removed.", ephemeral: true);
		}
		else
		{
			await user.AddRoleAsync(role);
			await RespondAsync($"{name} specialist role was added.", ephemeral: true);
		}

		await UpdateUsers();
	}

	private async Task UpdateUsers()
	{
		if (Context.Guild.GetChannel(MentorChannel) is not SocketTextChannel channel)
			return;

		if (await channel.GetMessageAsync(MentorMessage) is not RestUserMessage message)
			return;

		await message.ModifyAsync(x =>
		{
			x.Content = GenerateMessage();
			x.AllowedMentions = AllowedMentions.None;
		});
	}

	private string GenerateMessage()
	{
		return "This is an **opt-in** list of our members who happen to be pretty good at their role and are happy to provide mentoring.\n" +
			   "If you're looking for tips/tricks on rotations, handling fights, optimising or even just learning a certain job, feel free to mention the roles:\n" +
			   $"<@&{TankSpecialistRole}> <@&{HealerSpecialistRole}> <@&{MeleeSpecialistRole}> <@&{CasterSpecialistRole}> <@&{RangedPhysSpecialistRole}>\n" +
			   "\n" +
			   "If you feel confident with your role and want to opt-in, use the `/mentor` command to toggle the role for yourself!\n" +
			   "\n" +
			   $"{TankRoleEmote}: {GetUsersWithRole(TankSpecialistRole)}\n" +
			   $"{HealerRoleEmote}: {GetUsersWithRole(HealerSpecialistRole)}\n" +
			   $"{MeleeRoleEmote}: {GetUsersWithRole(MeleeSpecialistRole)}\n" +
			   $"{CasterRoleEmote}: {GetUsersWithRole(CasterSpecialistRole)}\n" +
			   $"{RangedRoleEmote}: {GetUsersWithRole(RangedPhysSpecialistRole)}\n" +
			   $"";
	}

	private string GetUsersWithRole(ulong role)
	{
		var users = Context.Guild.Users
			.Where(user => user.Roles.Any(r => r.Id == role))
			.Select(user => $"<@{user.Id}>");
		return string.Join(' ', users);
	}

	[SlashCommand("tank", "Toggle tank specialist role")]
	private Task ToggleTankMentorRole() => ToggleMentorRole("Tank", TankSpecialistRole);


	[SlashCommand("healer", "Toggle healer specialist role")]
	private Task ToggleHealerMentorRole() => ToggleMentorRole("Healer", HealerSpecialistRole);


	[SlashCommand("melee", "Toggle melee specialist role")]
	private Task ToggleMeleeMentorRole() => ToggleMentorRole("Melee DPS", MeleeSpecialistRole);


	[SlashCommand("caster", "Toggle caster specialist role")]
	private Task ToggleCasterMentorRole() => ToggleMentorRole("Caster DPS", CasterSpecialistRole);

	[SlashCommand("ranged", "Toggle ranged physical specialist role")]
	private Task ToggleRangedMentorRole() => ToggleMentorRole("Physical Ranged DPS", RangedPhysSpecialistRole);
}

public class MiscInteraction : InteractionModuleBase<SocketInteractionContext>
{
	private readonly DiscordBotService _discord;
	private readonly Prng _rng;

	public MiscInteraction(DiscordBotService discord, Prng rng)
	{
		_discord = discord;
		_rng = rng;
	}

	[SlashCommand("whoisingame", "Tells you who is currently in-game")]
	public async Task WhoIsInGame()
	{
		var guild = _discord.Client.ExcelGuild();
		await guild.DownloadUsersAsync();

		var presenceUsers = guild
			.Users
			.Where(user => user.Activities.Any(activity => activity.Name == "FINAL FANTASY XIV Online"))
			.ToList();

		var users = new List<SocketGuildUser>(presenceUsers);

		if (users.Count == 0)
		{
			await RespondAsync("According to Discord, nobody is currently logged in to FFXIV.");
			return;
		}

		await RespondAsync($"According to Discord, {users.Count} user{(users.Count == 1 ? " is" : "s are")} currently logged in to FFXIV: {string.Join(", ", users.Select(user => $"<@{user.Id}>"))}");
	}

	[SlashCommand("testb", "Does whatever Zahrymm is testing")]
	public async Task TestCommand(int maxValue, int total)
	{
		await DeferAsync();

		var dict = new Dictionary<int, int>();

		for (var i = 0; i < total; i++)
		{
			var result = _rng.NextInt(0, maxValue);

			if (!dict.TryAdd(result, 1))
				dict[result]++;
		}

		await FollowupAsync($"Histogram:\n{string.Join(Environment.NewLine, dict.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
	}

	private async Task RandomJob(params string[] jobs) => await RespondAsync($"I picked {_rng.Pick(jobs).First()} for you!");

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
		var total = minutes * 60 + seconds;
		var emboldenUses = (int)Math.Floor(total / 120f);
		var manaficUses = (int)Math.Floor(total / 110f);

		await RespondAsync(emboldenUses != manaficUses
			? "You should use Manafication on cooldown to gain the maximum amount of uses."
			: "You should align Manafication with Embolden. Use it 1 or 5 seconds before Embolden to fit 6 finishers into buffs.");
	}
}