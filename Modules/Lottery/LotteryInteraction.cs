using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Data;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace ExcelBotCs.Modules.Lottery;

[Group("lottery", "Lottery commands")]
public class LotteryInteraction : InteractionModuleBase<SocketInteractionContext>
{
	private readonly LotteryOptions _options;
	private readonly Repository<ExtraLotteryGuess> _extraLotteryGuesses;
	private readonly Repository<LotteryGuess> _lotteryGuesses;
	private readonly Repository<LotteryResult> _lotteryResults;

	public LotteryInteraction(Database database, LotteryOptions options)
	{
		_options = options;
		_extraLotteryGuesses = database.GetCollection<ExtraLotteryGuess>("extra_lottery_guesses");
		_lotteryGuesses = database.GetCollection<LotteryGuess>("lottery_guesses");
		_lotteryResults = database.GetCollection<LotteryResult>("lottery_results");
	}

	private bool CanParticipate(SocketGuildUser user)
	{
		return user.Roles.IsMember();
	}

	interface IGuessResponse
	{
	}

	record SuccessGuessResponse(IEnumerable<int> CurrentGuesses, string PrettyCurrentGuesses, int Number)
		: IGuessResponse;

	record OutOfRangeGuessResponse : IGuessResponse;

	record NotFcMemberGuessResponse : IGuessResponse;

	record AlreadyGuessedNumberGuessResponse(int Number) : IGuessResponse;

	record NotCurrentGuessedNumberGuessResponse(int Number) : IGuessResponse;

	record NoMoreGuessesGuessResponse(IEnumerable<int> CurrentGuesses, string PrettyCurrentGuesses)
		: IGuessResponse;

	private async Task<IGuessResponse> TryGuess(int number)
	{
		if (!CanParticipate(Context.GuildUser()))
			return new NotFcMemberGuessResponse();

		if (number is <= 0 or >= 100)
			return new OutOfRangeGuessResponse();

		var currentGuesses = await _lotteryGuesses
			.Where(guess => guess.DiscordId == Context.GuildUser().Id)
			.ToListAsync();

		if (currentGuesses.Any(guess => guess.Number == number))
			return new AlreadyGuessedNumberGuessResponse(number);

		var extraAwardedGuesses = await _extraLotteryGuesses
			.Where(guess => guess.DiscordId == Context.GuildUser().Id)
			.ToListAsync();

		var currentGuessAmount = currentGuesses.Count;
		var allowedGuessAmount = 1 + extraAwardedGuesses.Count;

		var numbers = currentGuesses.Select(guess => guess.Number).ToList();
		var guessesBlocked = allowedGuessAmount - currentGuessAmount <= 0;

		if (!guessesBlocked)
			numbers.Add(number);

		numbers.Sort();
		var prettyNumbers = numbers.Select(guess => guess.ToString()).ToList().PrettyJoin();

		return guessesBlocked
			? new NoMoreGuessesGuessResponse(numbers, prettyNumbers)
			: new SuccessGuessResponse(numbers, prettyNumbers, number);
	}

	private async Task<IGuessResponse> TryChangeGuess(SocketGuildUser user, int oldNumber, int newNumber)
	{
		if (!CanParticipate(Context.GuildUser()))
			return new NotFcMemberGuessResponse();

		if (newNumber is <= 0 or >= 100)
			return new OutOfRangeGuessResponse();

		var currentGuesses = await _lotteryGuesses
			.Where(guess => guess.DiscordId == Context.GuildUser().Id)
			.ToListAsync();

		if (currentGuesses.All(guess => guess.Number != oldNumber))
			return new NotCurrentGuessedNumberGuessResponse(oldNumber);

		if (currentGuesses.Any(guess => guess.Number == newNumber))
			return new AlreadyGuessedNumberGuessResponse(newNumber);

		currentGuesses.RemoveAll(guess => guess.Number == oldNumber);

		var numbers = ((List<int>)[newNumber, .. currentGuesses.Select(guess => guess.Number)]).ToList();
		numbers.Sort();
		var prettyNumbers = numbers.Select(guess => guess.ToString()).ToList().PrettyJoin();

		return new SuccessGuessResponse(numbers, prettyNumbers, newNumber);
	}

	private async Task ChangeGuess(int oldNumber, int newNumber)
	{
		await _lotteryGuesses.Delete(guess => guess.Number == oldNumber);
		await _lotteryGuesses.Insert(new LotteryGuess() { DiscordId = Context.GuildUser().Id, Number = newNumber });
	}

	private async Task InsertGuess(int number)
	{
		await _lotteryGuesses.Insert(new LotteryGuess() { DiscordId = Context.GuildUser().Id, Number = number });
	}

	interface IAwardResponse
	{
	}

	record NoUsersAwardResponse : IAwardResponse;

	record SuccessAwardResponse(IEnumerable<ulong> UserIds, string PrettyUsersAwarded, string Reason)
		: IAwardResponse;

	private async Task<IAwardResponse> TryAwardUsers(string reason, List<ulong> userIds)
	{
		if (!userIds.Any())
			return new NoUsersAwardResponse();

		var text = userIds.Select(userId => $"<@{userId}>").ToList().PrettyJoin();
		return new SuccessAwardResponse(userIds, text, reason);
	}

	private async Task AwardUsers(SuccessAwardResponse success)
	{
		foreach (var userId in success.UserIds)
		{
			await _extraLotteryGuesses.Insert(new ExtraLotteryGuess()
			{ DiscordId = userId, Reason = success.Reason });
		}

		if (success.UserIds.Count() == 1)
		{
			await PostInLotteryChannel(
				$"{success.PrettyUsersAwarded} has been granted another lottery guess for {success.Reason}! Use `/lottery guess` to make your choice.");
		}
		else
		{
			await PostInLotteryChannel(
				$"{success.PrettyUsersAwarded} have all been granted another lottery guess for {success.Reason}! Use `/lottery guess` to make your choice.");
		}
	}

	[SlashCommand("guess", "Pick a number and have a chance to win!")]
	public async Task Guess(int number)
	{
		var result = await TryGuess(number);

		if (result is SuccessGuessResponse success)
		{
			await InsertGuess(success.Number);
			await PostInLotteryChannel(
				$"<@{Context.GuildUser().Id}> guessed {number}. Current guesses: {success.PrettyCurrentGuesses}");
		}

		await RespondAsync(result switch
		{
			NotFcMemberGuessResponse _ => "Only FC members can participate in the lottery",
			OutOfRangeGuessResponse _ => "You can only pick a number between 1 and 99.",
			AlreadyGuessedNumberGuessResponse _ => $"You have already guessed {number}!",
			NoMoreGuessesGuessResponse r =>
				$"You don't have any guesses left! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.",
			SuccessGuessResponse r =>
				$"Your guess for {r.Number} was recorded! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess. {(await GetRemainingGuesses(Context.GuildUser())).Output}",
			_ => throw new NotImplementedException()
		}, ephemeral: true);
	}

	[SlashCommand("unused", "Check what numbers have not yet been used.")]
	public async Task UnusedNumbers()
	{
		var guessedNumbers = await GetGuessedNumbers();
		var formattedNumbers = (List<string>)["  ", .. Enumerable.Range(1, 99).Select(num => guessedNumbers.Contains(num) ? "__" : num.ToString("D2"))];
		var output = string.Join('\n', formattedNumbers.Chunk(10).Select(subList => string.Join(' ', subList)));
		await RespondAsync($"```{output}```", ephemeral: true);
	}


	private async Task<List<int>> GetNotGuessedNumbers()
	{
		return Enumerable.Range(1, 99).Except(await GetGuessedNumbers()).ToList();
	}

	private async Task<List<int>> GetGuessedNumbers()
	{
		return (await _lotteryGuesses
			.Where(_ => true)
			.ToListAsync())
			.Select(guess => guess.Number)
			.Distinct()
			.ToList();
	}

	public enum RandomGuessType
	{
		[ChoiceDisplay("Use any random number from 1-99")] Any,
		[ChoiceDisplay("Use only numbers that have not been guessed")] UnusedOnly,
		[ChoiceDisplay("Use only numbers that have been guessed (monster!)")] UsedOnly,
	}


	[SlashCommand("luckydip", "Spin the wheel and maybe you'll win!")]
	public async Task RandomGuess([Summary("number-pool", "Determines whether to use a random number from 1-99, unguessed or guessed numbers (default: 1-99)")] RandomGuessType numberPool = RandomGuessType.Any)
	{
		var cts = new CancellationTokenSource();
		var task = TryRandomGuess(cts.Token, numberPool);
		await DeferAsync(true);

		if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5), cts.Token)) == task)
		{
			await cts.CancelAsync();
			var result = await task;

			if (result is SuccessGuessResponse success)
			{
				await InsertGuess(success.Number);
				await PostInLotteryChannel(
					$"<@{Context.GuildUser().Id}> used a random draw and got {success.Number}. Current guesses: {success.PrettyCurrentGuesses}");
			}

			await FollowupAsync(result switch
			{
				NotFcMemberGuessResponse => "Only FC members can participate in the lottery",
				NoMoreGuessesGuessResponse r =>
					$"You don't have any guesses left! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess. {(await GetRemainingGuesses(Context.GuildUser())).Output}",
				SuccessGuessResponse r =>
					$"Your guess for {r.Number} was recorded! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.",
				OutOfRangeGuessResponse => "This number pool has no valid numbers to use, try another number pool.",
				_ => "Something went wrong, try again later. If this keeps happening, let Zahrymm know."
			}, ephemeral: true);
		}
		else
		{
			await cts.CancelAsync();
			await FollowupAsync(
				"Picking a number took too long, try again later. If this keeps happening, let Zahrymm know.",
				ephemeral: true);
		}
	}

	private async Task<(List<LotteryGuess>? CurrentGuesses, string Output)> GetRemainingGuesses(SocketGuildUser user)
	{
		var currentGuesses = await _lotteryGuesses
			.Where(guess => guess.DiscordId == user.Id)
			.ToListAsync();

		var extraAwardedGuesses = await _extraLotteryGuesses
			.Where(guess => guess.DiscordId == user.Id)
			.ToListAsync();

		var currentGuessAmount = currentGuesses.Count;
		var allowedGuessAmount = 1 + extraAwardedGuesses.Count;
		var displayAmount = allowedGuessAmount == 1
			? (currentGuessAmount == 1
				? $"You have used your guess."
				: $"You have not used your guess.")
			: $"You have used {currentGuessAmount} of your {allowedGuessAmount} guesses.";

		return (currentGuesses, displayAmount);
	}

	private async Task<IGuessResponse> TryRandomGuess(CancellationToken token, RandomGuessType type)
	{
		var numberPool = (type switch
		{
			RandomGuessType.Any => Enumerable.Range(0, 99),
			RandomGuessType.UsedOnly => await GetGuessedNumbers(),
			RandomGuessType.UnusedOnly => await GetNotGuessedNumbers(),
			_ => throw new ArgumentException()
		}).ToList();

		if (numberPool.Count == 0)
		{
			return new OutOfRangeGuessResponse();
		}

		while (true)
		{
			if (token.IsCancellationRequested)
				return new AlreadyGuessedNumberGuessResponse(0);

			var randomNumber = numberPool.Shuffle().First();
			var result = await TryGuess(randomNumber);

			if (result is SuccessGuessResponse or NotFcMemberGuessResponse or NoMoreGuessesGuessResponse)
				return result;
		}
	}

	[SlashCommand("change", "Change one of your current guesses")]
	public async Task Change(int old, int @new)
	{
		var result = await TryChangeGuess(Context.GuildUser(), old, @new);

		if (result is SuccessGuessResponse success)
		{
			await ChangeGuess(old, @new);
			await PostInLotteryChannel(
				$"<@{Context.GuildUser().Id}> changed a guess from {old} to {@new}. Current guesses: {success.PrettyCurrentGuesses}");
		}

		await RespondAsync(result switch
		{
			NotFcMemberGuessResponse _ => "Only FC members can participate in the lottery",
			OutOfRangeGuessResponse _ => "You can only pick a number between 1 and 99.",
			AlreadyGuessedNumberGuessResponse _ => $"You have already guessed {@new}.",
			NotCurrentGuessedNumberGuessResponse _ =>
				$"You have not guessed {old}. You need to use a number you have already guessed in order to change it.",
			SuccessGuessResponse r =>
				$"Your guess for {old} was changed to {@new}! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.",
			_ => throw new NotImplementedException()
		}, ephemeral: true);
	}

	[SlashCommand("whoguessed", "Check who has guessed a certain number.")]
	public async Task WhoGuessed(int number)
	{
		var currentGuesses = await _lotteryGuesses
			.Where(guess => guess.Number == number)
			.ToListAsync();

		await RespondAsync(currentGuesses.Count switch
		{
			0 => $"Nobody has guessed {number}.",
			1 =>
				$"{currentGuesses.Select(user => $"<@{user.DiscordId}>").ToList().PrettyJoin()} has guessed {number}.",
			_ =>
				$"{currentGuesses.Select(user => $"<@{user.DiscordId}>").ToList().PrettyJoin()} have all guessed {number}."
		}, ephemeral: true);
	}

	[SlashCommand("view", "Check your current guesses and see how many guesses you have left.")]
	public async Task View()
	{
		if (!CanParticipate(Context.GuildUser()))
		{
			await RespondAsync("Only FC members can participate in the lottery", ephemeral: true);
			return;
		}

		var (currentGuesses, displayAmount) = await GetRemainingGuesses(Context.GuildUser());

		if (currentGuesses.Count == 0)
		{
			await RespondAsync(displayAmount, ephemeral: true);
		}
		else
		{
			var guesses = currentGuesses.Select(guess => guess.Number).ToList();
			guesses.Sort();
			var displayedGuesses = guesses.Select(guess => guess.ToString()).ToList().PrettyJoin();

			await RespondAsync($"Current guesses: {displayedGuesses}. {displayAmount}", ephemeral: true);
		}
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

		var randomNumber = new Random().Next(99);
		var randomNumberDisplay = $"# The winning number is {randomNumber}";

		var allResults = await _lotteryGuesses.Where(_ => true).ToListAsync();
		var winners = allResults.Where(guess => guess.Number == randomNumber).ToList();

		await SaveGuesses(randomNumber, allResults);

		if (winners.Count == 0)
		{
			await PostInLotteryChannel($"{randomNumberDisplay}.\nNobody won, better luck next time!");
		}
		else
		{
			var winnersDisplay = winners.Select(user => $"<@{user.DiscordId}>").ToList().PrettyJoin();
			await PostInLotteryChannel($"{randomNumberDisplay}. Congratulations to {winnersDisplay}!");
		}

		var grouped = allResults.GroupBy(guess => guess.DiscordId).OrderBy(group => group.Count());
		await PostInLotteryChannel(
			$"## Participants\n{string.Join('\n', grouped.Select(group => $"<@{group.Key}>: {group.Select(guess => guess.Number.ToString()).ToList().PrettyJoin()}"))}");

		await Flush();
	}

	private async Task SaveGuesses(int winningNumber, List<LotteryGuess> allResults)
	{
		var result = new LotteryResult()
		{
			WinningNumber = winningNumber,
			Guesses = allResults
		};

		await _lotteryResults.Insert(result);
	}

	private async Task Flush()
	{
		await _lotteryGuesses.DeleteAll();
		await _extraLotteryGuesses.DeleteAll();
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

		var fcMembers = Context.Guild.Users.Where(user => user.Roles.IsMember() && !user.IsBot).Select(user => user.Id);
		var currentGuesses = (await _lotteryGuesses.Where(_ => true).ToListAsync()).Select(guess => guess.DiscordId)
			.GroupBy(x => x)
			.ToDictionary(x => x.Key, x => x.Count());
		var awardedGuesses = (await _extraLotteryGuesses.Where(_ => true).ToListAsync())
			.Select(award => award.DiscordId)
			.GroupBy(x => x)
			.ToDictionary(x => x.Key, x => x.Count());

		foreach (var fcMember in fcMembers)
			currentGuesses.TryAdd(fcMember, 0);

		var remainingGuesses = new List<(ulong Id, int Remaining)>();

		foreach (var (id, current) in currentGuesses)
		{
			if (awardedGuesses.TryGetValue(id, out var remaining))
			{
				var count = (remaining + 1) - current;
				if (count > 0)
					remainingGuesses.Add((id, count));
			}
			else
			{
				if (current == 0)
					remainingGuesses.Add((id, 1));
			}
		}

		var output = remainingGuesses
			.GroupBy(x => x.Remaining)
			.OrderBy(x => x.Key)
			.Aggregate(string.Empty,
				(current, guesses)
					=> current + $"{guesses.Key} guess{(guesses.Key == 1 ? "" : "es")} remaining: {guesses.Select(user => $"<@{user.Id}>").ToList().PrettyJoin()}\n");

		await FollowupAsync(output, ephemeral: true);
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
			case Extensions.NotValidUrlMessageResponse:
				await RespondAsync("The provided URL does not seem to be a valid Discord URL", ephemeral: true);
				break;

			case Extensions.NotFoundUrlMessageResponse:
				await RespondAsync(
					"Could not find the Guild/Channel this message belongs to. Do I have permission to view it?",
					ephemeral: true);
				break;

			case Extensions.SuccessMessageResponse msg:
				{
					var result = await TryAwardUsers(reason, msg.Message.MentionedUserIds.ToList());

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

					await AwardUsers(success);
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

		var result = await TryAwardUsers(reason, users.Select(user => user.Id).ToList());

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

		await AwardUsers(success);
		await socketMsg.UpdateAsync(msg =>
		{
			msg.Components = null;
			msg.Content = $"An extra lottery guess have been granted to: {success.PrettyUsersAwarded}";
		});
	}

	private async Task PostInLotteryChannel(string message)
	{
		var lotteryChannel = Context.Guild.GetTextChannel(_options.Channel);
		await lotteryChannel.SendMessageAsync(message);
	}
}