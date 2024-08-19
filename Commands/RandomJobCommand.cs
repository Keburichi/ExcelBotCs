using Discord.Interactions;
using ExcelBotCs.Discord;

namespace ExcelBotCs.Commands;

public class RandomJobCommand : InteractionModuleBase<SocketInteractionContext>
{
	private const ulong ExcelDiscord = 797520687941943336;
	private readonly DiscordBotService _discord;

	public RandomJobCommand(DiscordBotService discord)
	{
		_discord = discord;
		discord.Client.Ready += ClientOnReady;
	}

	private async Task ClientOnReady()
	{
		await _discord.Interaction.RegisterCommandsToGuildAsync(ExcelDiscord);
	}

	[SlashCommand("test", "Does nothing but reply \"Hello!\"")]
	public async Task TestCommand()
	{
		await RespondAsync("Hello!");
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
}