using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Services.Lottery.Enums;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Modules.Lottery;

[Group("lottery", "Lottery commands")]
public class LotteryInteraction : InteractionModuleBase<SocketInteractionContext>
{
	private readonly ILotteryService _lotteryService;

	public LotteryInteraction(ILotteryService lotteryService)
	{
		_lotteryService = lotteryService;
	}

	[SlashCommand("guess", "Pick a number and have a chance to win!")]
	public async Task Guess(int number)
	{
		var guessResult = await _lotteryService.GuessAsync(Context.GuildUser().Id, number);
		await RespondAsync(guessResult, ephemeral: true);
	}

	[SlashCommand("unused", "Check what numbers have not yet been used.")]
	public async Task UnusedNumbers()
	{
		var unusedNumbers = await _lotteryService.GetUnusedNumbersAsync();
		await RespondAsync($"```{unusedNumbers}```", ephemeral: true);
	}


	[SlashCommand("luckydip", "Spin the wheel and maybe you'll win!")]
	public async Task RandomGuess([Summary("number-pool", "Choose a set of numbers to use")] RandomGuessType numberPool = RandomGuessType.UnusedOnly)
	{
		var cts = new CancellationTokenSource();
		
		var task = _lotteryService.RandomGuessAsync(Context.User.Id, cts,  numberPool);
		
		await DeferAsync(true);

		if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5), cts.Token)) == task)
		{
			await cts.CancelAsync();
			var result = await task;
			await FollowupAsync(result, ephemeral: true);
		}
		else
		{
			await cts.CancelAsync();
			await FollowupAsync(
				"Picking a number took too long, try again later. If this keeps happening, let Zahrymm know.",
				ephemeral: true);
		}
	}

	[SlashCommand("change", "Change one of your current guesses")]
	public async Task Change(int old, int @new)
	{
		await RespondAsync(await _lotteryService.ChangeGuessAsync(Context.GuildUser().Id, old, @new), ephemeral: true);
	}

	[SlashCommand("whoguessed", "Check who has guessed a certain number.")]
	public async Task WhoGuessed(int number)
	{
		await RespondAsync(await _lotteryService.WhoGuessedAsync(number), ephemeral: true);
	}

	[SlashCommand("view", "Check your current guesses and see how many guesses you have left.")]
	public async Task View()
	{
		await RespondAsync(await _lotteryService.ViewAsync(Context.GuildUser().Id), ephemeral: true);
	}

	[SlashCommand("run", "Runs the lottery.")]
	public async Task Run()
	{
		if (!Context.GuildUser().Roles.IsOfficer())
		{
			await RespondAsync("Only officers can use this command!", ephemeral: true);
			return;
		}

		await RespondAsync("Lottery running...", ephemeral: true);

		await _lotteryService.RunLotteryAsync(Context.GuildUser().Id);
	}

	[SlashCommand("remind", "Reminds users to use any remaining guesses")]
	public async Task Remind()
	{
		if (!Context.GuildUser().Roles.IsOfficer())
		{
			await RespondAsync("Only officers can use this command!", ephemeral: true);
			return;
		}

		await DeferAsync(true);
		
		await _lotteryService.RemindAsync(Context.GuildUser().Id);

		// var fcMembers = Context.Guild.Users.Where(user => user.Roles.IsMember() && !user.IsBot).Select(user => user.Id);
		// var currentGuesses = (await _lotteryGuesses.Where(_ => true).ToListAsync()).Select(guess => guess.DiscordId)
		// 	.GroupBy(x => x)
		// 	.ToDictionary(x => x.Key, x => x.Count());
		// var awardedGuesses = (await _extraLotteryGuesses.Where(_ => true).ToListAsync())
		// 	.Select(award => award.DiscordId)
		// 	.GroupBy(x => x)
		// 	.ToDictionary(x => x.Key, x => x.Count());
		//
		// foreach (var fcMember in fcMembers)
		// 	currentGuesses.TryAdd(fcMember, 0);
		//
		// var remainingGuesses = new List<(ulong Id, int Remaining)>();
		//
		// foreach (var (id, current) in currentGuesses)
		// {
		// 	if (awardedGuesses.TryGetValue(id, out var remaining))
		// 	{
		// 		var count = (remaining + 1) - current;
		// 		if (count > 0)
		// 			remainingGuesses.Add((id, count));
		// 	}
		// 	else
		// 	{
		// 		if (current == 0)
		// 			remainingGuesses.Add((id, 1));
		// 	}
		// }
		//
		// var output = remainingGuesses
		// 	.GroupBy(x => x.Remaining)
		// 	.OrderBy(x => x.Key)
		// 	.Aggregate(string.Empty,
		// 		(current, guesses)
		// 			=> current + $"{guesses.Key} guess{(guesses.Key == 1 ? "" : "es")} remaining: {guesses.Select(user => $"<@{user.Id}>").ToList().PrettyJoin()}\n");
		//
		// await FollowupAsync(output, ephemeral: true);
		//
		// var previousParticipants = (await _lotteryResults.Where(_ => true).ToListAsync())
		// 	.OrderBy(result => result.DateCreated)
		// 	.Take(3)
		// 	.SelectMany(result => result.Guesses)
		// 	.Select(guess => guess.DiscordId)
		// 	.Distinct();
		//
		// var intersectionOutput = remainingGuesses
		// 	.Where(guess => previousParticipants.Contains(guess.Id))
		// 	.GroupBy(x => x.Remaining)
		// 	.OrderBy(x => x.Key)
		// 	.Aggregate("## Use your guesses before it's too late!\n",
		// 		(current, guesses)
		// 			=> current + $"{guesses.Key} guess{(guesses.Key == 1 ? "" : "es")} remaining: {guesses.Select(user => $"<@{user.Id}>").ToList().PrettyJoin()}\n");
		// await PostInLotteryChannel(intersectionOutput);
	}

	[SlashCommand("award", "Grants extra guesses for the current lottery period")]
	public async Task Award(string reason, string? postUrl = null)
	{
		if (!Context.GuildUser().Roles.IsOfficer())
		{
			await RespondAsync("Only officers can use this command!", ephemeral: true);
			return;
		}

		if (postUrl == null)
			await AwardByUi(reason);
		else
			await AwardByContents(reason, postUrl);
	}

	private async Task AwardByUi(string reason)
	{
		var awardSelection = new SelectMenuBuilder()
			.WithPlaceholder("Pick users")
			.WithCustomId($"award_selection:{reason}")
			.WithType(ComponentType.UserSelect)
			.WithMinValues(1)
			.WithMaxValues(24);

		var builder = new ComponentBuilder()
			.WithSelectMenu(awardSelection);

		await RespondAsync("Who should be awarded an extra guess for this lottery period?",
			components: builder.Build(), ephemeral: true);
	}

	private async Task AwardByContents(string reason, string postUrl)
	{
		switch (await Context.Client.GetMessageFromUrl(postUrl))
		{
			case DiscordSocketExtensions.NotValidUrlMessageResponse:
				await RespondAsync("The provided URL does not seem to be a valid Discord URL", ephemeral: true);
				break;

			case DiscordSocketExtensions.NotFoundUrlMessageResponse:
				await RespondAsync(
					"Could not find the Guild/Channel this message belongs to. Do I have permission to view it?",
					ephemeral: true);
				break;

			case DiscordSocketExtensions.SuccessMessageResponse msg:
				{
					var result = await _lotteryService.TryAwardUsersAsync(reason, msg.Message.MentionedUserIds.ToList());

					if (result is NoUsersAwardResponse)
					{
						await RespondAsync("No mentioned users could be found in the message.", ephemeral: true);
						return;
					}

					if (result is not SuccessAwardResponse success)
					{
						await RespondAsync("Something went wrong. Tell Zahrymm.", ephemeral: true);
						return;
					}

					await _lotteryService.AwardUsersAsync(success);
					await RespondAsync($"An extra lottery guess have been granted to: {success.PrettyUsersAwarded}",
						ephemeral: true);
					break;
				}
		}
	}


	[ComponentInteraction("award_selection:*", true)]
	public async Task HandleAward(string reason, IUser[] users)
	{
		if (Context.Interaction is not SocketMessageComponent socketMsg)
		{
			await RespondAsync("Something went wrong. Tell Zahrymm.");
			return;
		}

		var result = await _lotteryService.TryAwardUsersAsync(reason, users.Select(user => user.Id).ToList());

		if (result is NoUsersAwardResponse)
		{
			await RespondAsync("You did not pick any users.", ephemeral: true);
			return;
		}

		if (result is not SuccessAwardResponse success)
		{
			await RespondAsync("Something went wrong. Tell Zahrymm.", ephemeral: true);
			return;
		}

		await _lotteryService.AwardUsersAsync(success);
		await socketMsg.UpdateAsync(msg =>
		{
			msg.Components = null;
			msg.Content = $"An extra lottery guess have been granted to: {success.PrettyUsersAwarded}";
		});
	}
}